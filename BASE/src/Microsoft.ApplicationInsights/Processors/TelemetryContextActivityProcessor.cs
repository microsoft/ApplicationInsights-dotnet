namespace Microsoft.ApplicationInsights.Processors
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Internal;
    using OpenTelemetry;

    /// <summary>
    /// An activity processor that applies client-level <see cref="TelemetryContext"/> properties
    /// to all activities as tags, using skip-if-present semantics.
    /// This ensures context tags are applied universally — to Track* calls, Start/Stop operations,
    /// and any OpenTelemetry API activities emitted by customer code.
    /// </summary>
    /// <remarks>
    /// Performance optimization: after a warmup period (<see cref="WarmupCountThreshold"/> calls),
    /// context properties are frozen into a compact snapshot array. This avoids repeatedly
    /// navigating the TelemetryContext object graph and skipping null/empty values on every call.
    /// This relies on the documented pattern that customers set TelemetryClient.Context
    /// properties once during initialization.
    /// </remarks>
    internal sealed class TelemetryContextActivityProcessor : BaseProcessor<Activity>
    {
        /// <summary>
        /// Minimum number of OnEnd calls before the context snapshot can be frozen.
        /// </summary>
        internal const int WarmupCountThreshold = 10;

        /// <summary>
        /// Minimum time (in milliseconds) after construction before the context snapshot
        /// can be frozen. This guards against high-throughput apps where 10 activities
        /// complete before async initialization (e.g., IHostedService, middleware) has
        /// had a chance to set TelemetryContext properties.
        /// </summary>
        internal const long WarmupTimeThresholdMs = 5_000;

        private readonly TelemetryContext context;
        private readonly long constructedTimestamp;

        /// <summary>
        /// Frozen snapshot of non-null context tags, built after warmup.
        /// Once set, this array is immutable and read lock-free.
        /// </summary>
        private volatile KeyValuePair<string, string>[] frozenTags;

        /// <summary>
        /// Counter tracking the number of OnEnd calls during warmup.
        /// </summary>
        private int warmupCounter;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryContextActivityProcessor"/> class.
        /// </summary>
        /// <param name="context">The client-level <see cref="TelemetryContext"/> to apply.</param>
        public TelemetryContextActivityProcessor(TelemetryContext context)
        {
            this.context = context;
            this.constructedTimestamp = Stopwatch.GetTimestamp();
        }

        /// <summary>
        /// Called when an activity ends. Applies client-level context tags using
        /// skip-if-present semantics: if a tag is already set (by item-level context,
        /// Track* methods, or instrumentation), it is not overwritten.
        /// This matches the 2.x SDK's <c>Tags.CopyTagValue</c> precedence behavior.
        /// </summary>
        /// <param name="activity">The activity that ended.</param>
        public override void OnEnd(Activity activity)
        {
            if (activity == null || this.context == null)
            {
                return;
            }

            var snapshot = this.frozenTags;
            if (snapshot != null)
            {
                // Fast path: apply pre-computed snapshot (only non-null values, no object graph navigation)
                ApplySnapshot(activity, snapshot);
            }
            else
            {
                // Slow path: full context evaluation during warmup
                this.SlowPathOnEnd(activity);
            }

            base.OnEnd(activity);
        }

        /// <summary>
        /// Applies the frozen snapshot of context tags to the activity.
        /// Only contains entries where the value was non-null/non-empty at snapshot time.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ApplySnapshot(Activity activity, KeyValuePair<string, string>[] snapshot)
        {
            for (int i = 0; i < snapshot.Length; i++)
            {
                ref readonly var kvp = ref snapshot[i];
                if (activity.GetTagItem(kvp.Key) == null)
                {
                    activity.SetTag(kvp.Key, kvp.Value);
                }
            }
        }

        private static void AddIfNotEmpty(List<KeyValuePair<string, string>> list, string key, string value)
        {
            if (!string.IsNullOrEmpty(value) && !ContainsKey(list, key))
            {
                list.Add(new KeyValuePair<string, string>(key, value));
            }
        }

        private static bool ContainsKey(List<KeyValuePair<string, string>> list, string key)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Key == key)
                {
                    return true;
                }
            }

            return false;
        }

        private static void SetTagIfAbsent(Activity activity, string key, string value)
        {
            if (!string.IsNullOrEmpty(value) && activity.GetTagItem(key) == null)
            {
                activity.SetTag(key, value);
            }
        }

        /// <summary>
        /// Full context evaluation path used during warmup. After <see cref="WarmupCountThreshold"/>
        /// calls, builds and freezes the snapshot for all subsequent calls.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SlowPathOnEnd(Activity activity)
        {
            // Apply client-level GlobalProperties (lowest priority — will not overwrite existing tags)
            var globalProperties = this.context.GlobalPropertiesValue;
            if (globalProperties != null)
            {
                foreach (var kvp in globalProperties)
                {
                    SetTagIfAbsent(activity, kvp.Key, kvp.Value);
                }
            }

            SetTagIfAbsent(activity, SemanticConventions.AttributeEnduserPseudoId, this.context.User?.Id);
            SetTagIfAbsent(activity, SemanticConventions.AttributeEnduserId, this.context.User?.AuthenticatedUserId);
            SetTagIfAbsent(activity, SemanticConventions.AttributeMicrosoftOperationName, this.context.Operation?.Name);
            SetTagIfAbsent(activity, SemanticConventions.AttributeMicrosoftClientIp, this.context.Location?.Ip);
            SetTagIfAbsent(activity, SemanticConventions.AttributeMicrosoftSessionId, this.context.Session?.Id);
            SetTagIfAbsent(activity, SemanticConventions.AttributeAiDeviceId, this.context.Device?.Id);
            SetTagIfAbsent(activity, SemanticConventions.AttributeAiDeviceModel, this.context.Device?.Model);
            SetTagIfAbsent(activity, SemanticConventions.AttributeAiDeviceType, this.context.Device?.Type);
            SetTagIfAbsent(activity, SemanticConventions.AttributeAiDeviceOsVersion, this.context.Device?.OperatingSystem);
            SetTagIfAbsent(activity, SemanticConventions.AttributeMicrosoftSyntheticSource, this.context.Operation?.SyntheticSource);
            SetTagIfAbsent(activity, SemanticConventions.AttributeMicrosoftUserAccountId, this.context.User?.AccountId);

            // Freeze the snapshot once BOTH conditions are met:
            //   1. At least WarmupCountThreshold calls have occurred.
            //   2. At least WarmupTimeThresholdMs has elapsed since construction.
            // The count gate ensures we don't snapshot on the very first call.
            // The time gate ensures we don't snapshot before async init (e.g.,
            // IHostedService.StartAsync, middleware) has had time to set context.
            int count = Interlocked.Increment(ref this.warmupCounter);
            if (count >= WarmupCountThreshold && this.HasTimeThresholdElapsed())
            {
                // CAS-style: only the thread that transitions from null builds the snapshot.
                if (this.frozenTags == null)
                {
                    Interlocked.CompareExchange(ref this.frozenTags, this.BuildSnapshot(), null);
                }
            }
        }

        /// <summary>
        /// Builds an immutable snapshot array of all non-null/non-empty context tags.
        /// Called once after warmup completes.
        /// </summary>
        private KeyValuePair<string, string>[] BuildSnapshot()
        {
            var list = new List<KeyValuePair<string, string>>();

            var globalProperties = this.context.GlobalPropertiesValue;
            if (globalProperties != null)
            {
                foreach (var kvp in globalProperties)
                {
                    if (!string.IsNullOrEmpty(kvp.Value))
                    {
                        list.Add(new KeyValuePair<string, string>(kvp.Key, kvp.Value));
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
        /// Uses <see cref="Stopwatch"/> for monotonic, allocation-free timing.
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
