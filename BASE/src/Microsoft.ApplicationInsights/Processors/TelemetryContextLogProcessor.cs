namespace Microsoft.ApplicationInsights.Processors
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using Microsoft.ApplicationInsights.DataContracts;
    using OpenTelemetry;
    using OpenTelemetry.Logs;

    /// <summary>
    /// A log processor that applies client-level <see cref="TelemetryContext"/> properties
    /// to all log records as attributes, using skip-if-present semantics.
    /// This ensures context attributes are applied universally — to Track* calls
    /// and any <see cref="Microsoft.Extensions.Logging.ILogger"/> calls from customer code.
    /// </summary>
    internal sealed class TelemetryContextLogProcessor : BaseProcessor<LogRecord>
    {
        internal const int WarmupCountThreshold = WarmupGate.CountThreshold;
        internal const long WarmupTimeThresholdMs = WarmupGate.TimeThresholdMs;

        private readonly TelemetryContext context;
        private readonly long constructedTimestamp;
        private volatile ContextSnapshot frozenSnapshot;
        private volatile KeyValuePair<string, object>[] frozenAttributes;
        private int warmupCounter;

        public TelemetryContextLogProcessor(TelemetryContext context)
        {
            this.context = context;
            this.constructedTimestamp = Stopwatch.GetTimestamp();
        }

        public override void OnEnd(LogRecord logRecord)
        {
            if (logRecord == null || this.context == null)
            {
                return;
            }

            var snapshot = this.frozenSnapshot;
            if (snapshot != null)
            {
                ApplySnapshot(logRecord, this.frozenAttributes ?? ConvertToObjectAttributes(snapshot.Tags));
            }
            else
            {
                ApplyContext(logRecord, this.context);

                if (WarmupGate.ShouldFreeze(ref this.warmupCounter, this.constructedTimestamp) && this.frozenSnapshot == null)
                {
                    var builtSnapshot = TelemetryContextEnricher.BuildSnapshot(this.context);
                    if (Interlocked.CompareExchange(ref this.frozenSnapshot, builtSnapshot, null) == null)
                    {
                        this.frozenAttributes = ConvertToObjectAttributes(builtSnapshot.Tags);
                    }
                }
            }

            base.OnEnd(logRecord);
        }

        private static void ApplyContext(LogRecord logRecord, TelemetryContext context)
        {
            ApplySnapshot(logRecord, ConvertToObjectAttributes(TelemetryContextEnricher.BuildSnapshot(context).Tags));
        }

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
                logRecord.Attributes = snapshot;
                return;
            }

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

        private static KeyValuePair<string, object>[] ConvertToObjectAttributes(KeyValuePair<string, string>[] tags)
        {
            if (tags.Length == 0)
            {
                return Array.Empty<KeyValuePair<string, object>>();
            }

            var attributes = new KeyValuePair<string, object>[tags.Length];
            for (int i = 0; i < tags.Length; i++)
            {
                attributes[i] = new KeyValuePair<string, object>(tags[i].Key, tags[i].Value);
            }

            return attributes;
        }
    }
}
