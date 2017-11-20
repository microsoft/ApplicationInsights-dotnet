using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Metrics;
using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    /// <summary>
    /// 
    /// </summary>
    public static class TelemetryClientExtensionsAdvanced
    {
        private static ConditionalWeakTable<TelemetryClient, MetricManager> s_metricManagersForTelemetryClients;

        /// <summary>
        /// </summary>
        /// <param name="telemetryClient"></param>
        /// <param name="aggregationScope"></param>
        /// <returns></returns>
        public static MetricManager Metrics(this TelemetryClient telemetryClient, MetricAggregationScope aggregationScope)
        {
            Util.ValidateNotNull(telemetryClient, nameof(telemetryClient));

            switch(aggregationScope)
            {
                case MetricAggregationScope.TelemetryConfiguration:
                    TelemetryConfiguration pipeline = Util.GetTelemetryConfiguration(telemetryClient);
                    return pipeline.Metrics();

                case MetricAggregationScope.TelemetryClient:
                    MetricManager manager = GetOrCreateMetricManager(telemetryClient);
                    return manager;

                default:
                    throw new ArgumentException($"Invalid value of {nameof(aggregationScope)} ({aggregationScope}). Only the following values are supported:"
                                              + $" ['{nameof(MetricAggregationScope)}.{MetricAggregationScope.TelemetryClient.ToString()}',"
                                              + $" '{nameof(MetricAggregationScope)}.{MetricAggregationScope.TelemetryConfiguration.ToString()}'].");
            }
        }

        private static MetricManager GetOrCreateMetricManager(TelemetryClient telemetryClient)
        {
            ConditionalWeakTable<TelemetryClient, MetricManager> metricManagers = s_metricManagersForTelemetryClients;
            if (metricManagers == null)
            {
                ConditionalWeakTable<TelemetryClient, MetricManager> newTable = new ConditionalWeakTable<TelemetryClient, MetricManager>();
                ConditionalWeakTable<TelemetryClient, MetricManager> prevTable = Interlocked.CompareExchange(ref s_metricManagersForTelemetryClients, newTable, null);
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
