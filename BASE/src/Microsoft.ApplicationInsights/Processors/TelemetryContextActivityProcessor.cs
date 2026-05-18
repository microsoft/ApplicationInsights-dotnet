namespace Microsoft.ApplicationInsights.Processors
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using Microsoft.ApplicationInsights.DataContracts;
    using OpenTelemetry;

    /// <summary>
    /// An activity processor that applies client-level <see cref="TelemetryContext"/> properties
    /// to all activities as tags, using skip-if-present semantics.
    /// This ensures context tags are applied universally — to Track* calls, Start/Stop operations,
    /// and any OpenTelemetry API activities emitted by customer code.
    /// </summary>
    internal sealed class TelemetryContextActivityProcessor : BaseProcessor<Activity>
    {
        internal const int WarmupCountThreshold = WarmupGate.CountThreshold;
        internal const long WarmupTimeThresholdMs = WarmupGate.TimeThresholdMs;

        private readonly TelemetryContext context;
        private readonly long constructedTimestamp;
        private volatile ContextSnapshot frozenSnapshot;
        private volatile KeyValuePair<string, string>[] frozenTags;
        private int warmupCounter;

        public TelemetryContextActivityProcessor(TelemetryContext context)
        {
            this.context = context;
            this.constructedTimestamp = Stopwatch.GetTimestamp();
        }

        public override void OnEnd(Activity activity)
        {
            if (activity == null || this.context == null)
            {
                return;
            }

            var snapshot = this.frozenSnapshot;
            if (snapshot != null)
            {
                TelemetryContextEnricher.ApplySnapshotToActivity(activity, snapshot);
            }
            else
            {
                TelemetryContextEnricher.ApplyToActivity(activity, this.context);

                if (WarmupGate.ShouldFreeze(ref this.warmupCounter, this.constructedTimestamp) && this.frozenSnapshot == null)
                {
                    var builtSnapshot = TelemetryContextEnricher.BuildSnapshot(this.context);
                    if (Interlocked.CompareExchange(ref this.frozenSnapshot, builtSnapshot, null) == null)
                    {
                        this.frozenTags = builtSnapshot.Tags;
                    }
                }
            }

            base.OnEnd(activity);
        }
    }
}
