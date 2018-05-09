namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Extensibility;

    using static System.FormattableString;

    /// <summary>@ToDo: Complete documentation before stable release. {526}</summary>
    public static class TelemetryClientExtensions
    {
        private static ConditionalWeakTable<TelemetryClient, MetricManager> metricManagersForTelemetryClients;

        /// <summary>@ToDo: Complete documentation before stable release. {811}</summary>
        /// <param name="telemetryClient">@ToDo: Complete documentation before stable release. {225}</param>
        /// <param name="aggregationScope">@ToDo: Complete documentation before stable release. {281}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {736}</returns>
        public static MetricManager GetMetricManager(this TelemetryClient telemetryClient, MetricAggregationScope aggregationScope)
        {
            Util.ValidateNotNull(telemetryClient, nameof(telemetryClient));

            switch (aggregationScope)
            {
                case MetricAggregationScope.TelemetryConfiguration:
                    TelemetryConfiguration pipeline = telemetryClient.TelemetryConfiguration;
                    return pipeline.GetMetricManager();

                case MetricAggregationScope.TelemetryClient:
                    MetricManager manager = GetOrCreateMetricManager(telemetryClient);
                    return manager;

                default:
                    throw new ArgumentException(Invariant($"Invalid value of {nameof(aggregationScope)} ({aggregationScope}). Only the following values are supported:")
                                              + Invariant($" ['{nameof(MetricAggregationScope)}.{MetricAggregationScope.TelemetryClient.ToString()}',")
                                              + Invariant($" '{nameof(MetricAggregationScope)}.{MetricAggregationScope.TelemetryConfiguration.ToString()}']."));
            }
        }

        private static MetricManager GetOrCreateMetricManager(TelemetryClient telemetryClient)
        {
            ConditionalWeakTable<TelemetryClient, MetricManager> metricManagers = metricManagersForTelemetryClients;
            if (metricManagers == null)
            {
                ConditionalWeakTable<TelemetryClient, MetricManager> newTable = new ConditionalWeakTable<TelemetryClient, MetricManager>();
                ConditionalWeakTable<TelemetryClient, MetricManager> prevTable = Interlocked.CompareExchange(ref metricManagersForTelemetryClients, newTable, null);
                metricManagers = prevTable ?? newTable;
            }

            // Get the manager from the table:
            MetricManager createdManager = null;
            MetricManager chosenManager = metricManagers.GetValue(
                                                            telemetryClient,
                                                            (tc) =>
                                                            {
                                                                createdManager = new MetricManager(new ApplicationInsightsTelemetryPipeline(tc));
                                                                return createdManager;
                                                            });

            // If there was a race and we did not end up returning the manager we just created, we will notify it to give up its agregation cycle thread.
            if (createdManager != null && false == Object.ReferenceEquals(createdManager, chosenManager))
            {
                Task fireAndForget = createdManager.StopDefaultAggregationCycleAsync();
            }

            return chosenManager;
        }
    }
}
