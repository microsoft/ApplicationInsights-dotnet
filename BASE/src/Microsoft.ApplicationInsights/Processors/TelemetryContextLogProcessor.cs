namespace Microsoft.ApplicationInsights.Processors
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Internal;
    using OpenTelemetry;
    using OpenTelemetry.Logs;

    /// <summary>
    /// A log processor that applies client-level <see cref="TelemetryContext"/> properties
    /// to all log records as attributes, using skip-if-present semantics.
    /// This ensures context attributes are applied universally — to Track* calls
    /// and any <see cref="Microsoft.Extensions.Logging.ILogger"/> calls from customer code.
    /// </summary>
    /// <remarks>
    /// Performance optimization: after a warmup period (<see cref="WarmupCountThreshold"/> calls),
    /// context properties are frozen into a compact snapshot array. This eliminates per-call
    /// allocations of <see cref="HashSet{T}"/> and intermediate <see cref="List{T}"/> buffers,
    /// and avoids repeatedly navigating the <see cref="TelemetryContext"/> object graph.
    /// This relies on the documented pattern that customers set TelemetryClient.Context
    /// properties once during initialization.
    /// </remarks>
    internal sealed class TelemetryContextLogProcessor : BaseProcessor<LogRecord>
    {
        /// <summary>
        /// Minimum number of OnEnd calls before the context snapshot can be frozen.
        /// </summary>
        internal const int WarmupCountThreshold = 10;

        /// <summary>
        /// Minimum time (in milliseconds) after construction before the context snapshot
        /// can be frozen. This guards against high-throughput apps where activities
        /// complete before async initialization has had a chance to set TelemetryContext properties.
        /// </summary>
        internal const long WarmupTimeThresholdMs = 5_000;

        private readonly TelemetryContext context;
        private readonly long constructedTimestamp;

        /// <summary>
        /// Frozen snapshot of non-null context attributes, built after warmup.
        /// Once set, this array is immutable and read lock-free.
        /// </summary>
        private volatile KeyValuePair<string, object>[] frozenAttributes;

        /// <summary>
        /// Counter tracking the number of OnEnd calls during warmup.
        /// </summary>
        private int warmupCounter;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryContextLogProcessor"/> class.
        /// </summary>
        /// <param name="context">The client-level <see cref="TelemetryContext"/> to apply.</param>
        public TelemetryContextLogProcessor(TelemetryContext context)
        {
            this.context = context;
            this.constructedTimestamp = Stopwatch.GetTimestamp();
        }

        /// <summary>
        /// Called when a log record ends. Applies client-level context attributes using
        /// skip-if-present semantics: if an attribute key is already present in the log record,
        /// it is not overwritten. This matches the 2.x SDK's <c>Tags.CopyTagValue</c> precedence behavior.
        /// The Azure Monitor Exporter's <c>LogsHelper.ProcessLogRecordProperties</c> will pick up
        /// these attributes and route them into <c>LogContextInfo</c> for context tag mapping.
        /// </summary>
        /// <param name="logRecord">The log record being processed.</param>
        public override void OnEnd(LogRecord logRecord)
        {
            if (logRecord == null || this.context == null)
            {
                return;
            }

            var snapshot = this.frozenAttributes;
            if (snapshot != null)
            {
                // Fast path: apply pre-computed snapshot (no HashSet, no intermediate List, no object graph)
                ApplySnapshot(logRecord, snapshot);
            }
            else
            {
                // Slow path: full context evaluation during warmup
                this.SlowPathOnEnd(logRecord);
            }

            base.OnEnd(logRecord);
        }

        /// <summary>
        /// Fast path: merges the frozen snapshot attributes into the log record,
        /// skipping any keys already present.
        /// Zero-allocation when the log record has no pre-existing attributes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ApplySnapshot(LogRecord logRecord, KeyValuePair<string, object>[] snapshot)
        {
            if (snapshot.Length == 0)
            {
                return;
            }

            var existing = logRecord.Attributes;
            int existingCount = existing?.Count ?? 0;

            if (existingCount == 0)
            {
                // Most common case: no pre-existing attributes.
                // Assign the snapshot array directly — T[] implements IReadOnlyList<T>.
                // Zero allocation.
                logRecord.Attributes = snapshot;
                return;
            }

            // Count how many snapshot keys are NOT already present, to avoid
            // allocating a merged list when all keys conflict.
            int newCount = 0;
            for (int i = 0; i < snapshot.Length; i++)
            {
                if (!ContainsKey(existing, snapshot[i].Key))
                {
                    newCount++;
                }
            }

            if (newCount == 0)
            {
                return;
            }

            // Only allocate when we actually have new attributes to merge.
            var merged = new List<KeyValuePair<string, object>>(existingCount + newCount);
            foreach (var attr in existing)
            {
                merged.Add(attr);
            }

            for (int i = 0; i < snapshot.Length; i++)
            {
                ref readonly var kvp = ref snapshot[i];
                if (!ContainsKey(existing, kvp.Key))
                {
                    merged.Add(kvp);
                }
            }

            logRecord.Attributes = merged;
        }

        /// <summary>
        /// Checks whether the attribute collection contains the specified key.
        /// Uses linear scan — attribute lists are typically small (&lt;20 items).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ContainsKey(IReadOnlyList<KeyValuePair<string, object>> attributes, string key)
        {
            if (attributes == null || attributes.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < attributes.Count; i++)
            {
                if (attributes[i].Key == key)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddIfAbsent(
            List<KeyValuePair<string, object>> contextAttributes,
            IReadOnlyList<KeyValuePair<string, object>> existingAttributes,
            string key,
            string value)
        {
            if (!string.IsNullOrEmpty(value)
                && !ContainsKey(existingAttributes, key)
                && !ContainsKey(contextAttributes, key))
            {
                contextAttributes.Add(new KeyValuePair<string, object>(key, value));
            }
        }

        private static void AddIfNotEmpty(List<KeyValuePair<string, object>> list, string key, string value)
        {
            if (!string.IsNullOrEmpty(value) && !ContainsKey(list, key))
            {
                list.Add(new KeyValuePair<string, object>(key, value));
            }
        }

        /// <summary>
        /// Full context evaluation path used during warmup. After both thresholds are met,
        /// builds and freezes the snapshot for all subsequent calls.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SlowPathOnEnd(LogRecord logRecord)
        {
            var existing = logRecord.Attributes;

            // Build list of context attributes to add (only those not already present).
            // No HashSet needed — linear scan on the small attributes list is faster and allocation-free.
            var contextAttributes = new List<KeyValuePair<string, object>>();
            var globalProperties = this.context.GlobalPropertiesValue;
            if (globalProperties != null)
            {
                foreach (var kvp in globalProperties)
                {
                    AddIfAbsent(contextAttributes, existing, kvp.Key, kvp.Value);
                }
            }

            AddIfAbsent(contextAttributes, existing, SemanticConventions.AttributeEnduserPseudoId, this.context.User?.Id);
            AddIfAbsent(contextAttributes, existing, SemanticConventions.AttributeEnduserId, this.context.User?.AuthenticatedUserId);
            AddIfAbsent(contextAttributes, existing, SemanticConventions.AttributeMicrosoftOperationName, this.context.Operation?.Name);
            AddIfAbsent(contextAttributes, existing, SemanticConventions.AttributeMicrosoftClientIp, this.context.Location?.Ip);
            AddIfAbsent(contextAttributes, existing, SemanticConventions.AttributeMicrosoftSessionId, this.context.Session?.Id);
            AddIfAbsent(contextAttributes, existing, SemanticConventions.AttributeAiDeviceId, this.context.Device?.Id);
            AddIfAbsent(contextAttributes, existing, SemanticConventions.AttributeAiDeviceModel, this.context.Device?.Model);
            AddIfAbsent(contextAttributes, existing, SemanticConventions.AttributeAiDeviceType, this.context.Device?.Type);
            AddIfAbsent(contextAttributes, existing, SemanticConventions.AttributeAiDeviceOsVersion, this.context.Device?.OperatingSystem);
            AddIfAbsent(contextAttributes, existing, SemanticConventions.AttributeMicrosoftSyntheticSource, this.context.Operation?.SyntheticSource);
            AddIfAbsent(contextAttributes, existing, SemanticConventions.AttributeMicrosoftUserAccountId, this.context.User?.AccountId);

            if (contextAttributes.Count > 0)
            {
                int existingCount = existing?.Count ?? 0;
                var merged = new List<KeyValuePair<string, object>>(existingCount + contextAttributes.Count);

                if (existing != null)
                {
                    foreach (var attr in existing)
                    {
                        merged.Add(attr);
                    }
                }

                merged.AddRange(contextAttributes);
                logRecord.Attributes = merged;
            }

            // Freeze the snapshot once BOTH conditions are met:
            //   1. At least WarmupCountThreshold calls have occurred.
            //   2. At least WarmupTimeThresholdMs has elapsed since construction.
            int count = Interlocked.Increment(ref this.warmupCounter);
            if (count >= WarmupCountThreshold && this.HasTimeThresholdElapsed())
            {
                if (this.frozenAttributes == null)
                {
                    Interlocked.CompareExchange(ref this.frozenAttributes, this.BuildSnapshot(), null);
                }
            }
        }

        /// <summary>
        /// Builds an immutable snapshot array of all non-null/non-empty context attributes.
        /// Called once after warmup completes.
        /// </summary>
        private KeyValuePair<string, object>[] BuildSnapshot()
        {
            var list = new List<KeyValuePair<string, object>>();

            var globalProperties = this.context.GlobalPropertiesValue;
            if (globalProperties != null)
            {
                foreach (var kvp in globalProperties)
                {
                    if (!string.IsNullOrEmpty(kvp.Value))
                    {
                        list.Add(new KeyValuePair<string, object>(kvp.Key, kvp.Value));
                    }
                }
            }

            AddIfNotEmpty(list, SemanticConventions.AttributeEnduserPseudoId, this.context.User?.Id);
            AddIfNotEmpty(list, SemanticConventions.AttributeEnduserId, this.context.User?.AuthenticatedUserId);
            AddIfNotEmpty(list, SemanticConventions.AttributeMicrosoftOperationName, this.context.Operation?.Name);
            AddIfNotEmpty(list, SemanticConventions.AttributeMicrosoftClientIp, this.context.Location?.Ip);
            AddIfNotEmpty(list, SemanticConventions.AttributeMicrosoftSessionId, this.context.Session?.Id);
            AddIfNotEmpty(list, SemanticConventions.AttributeAiDeviceId, this.context.Device?.Id);
            AddIfNotEmpty(list, SemanticConventions.AttributeAiDeviceModel, this.context.Device?.Model);
            AddIfNotEmpty(list, SemanticConventions.AttributeAiDeviceType, this.context.Device?.Type);
            AddIfNotEmpty(list, SemanticConventions.AttributeAiDeviceOsVersion, this.context.Device?.OperatingSystem);
            AddIfNotEmpty(list, SemanticConventions.AttributeMicrosoftSyntheticSource, this.context.Operation?.SyntheticSource);
            AddIfNotEmpty(list, SemanticConventions.AttributeMicrosoftUserAccountId, this.context.User?.AccountId);

            return list.ToArray();
        }

        /// <summary>
        /// Returns true if at least <see cref="WarmupTimeThresholdMs"/> milliseconds
        /// have elapsed since this processor was constructed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasTimeThresholdElapsed()
        {
            long elapsedTicks = Stopwatch.GetTimestamp() - this.constructedTimestamp;
            long elapsedMs = (elapsedTicks * 1000) / Stopwatch.Frequency;
            return elapsedMs >= WarmupTimeThresholdMs;
        }
    }
}
