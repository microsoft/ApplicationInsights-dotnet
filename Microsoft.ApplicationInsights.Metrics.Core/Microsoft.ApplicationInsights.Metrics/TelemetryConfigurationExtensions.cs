using System;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Metrics.Extensibility;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary>
    /// 
    /// </summary>
    public static class TelemetryConfigurationExtensions
    {
        private static MetricManager s_defaultMetricManager = null;
        private static ConditionalWeakTable<TelemetryConfiguration, MetricManager> s_metricManagers = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="telemetryPipeline"></param>
        /// <returns></returns>
        public static MetricManager Metrics(this TelemetryConfiguration telemetryPipeline)
        {
            if (telemetryPipeline == null)
            {
                return null;
            }

            // Fast path for the default configuration:
            if (telemetryPipeline == TelemetryConfiguration.Active)
            {
                MetricManager manager = s_defaultMetricManager;
                if (manager == null)
                {
                    var pipelineAdapter = new ApplicationInsightsTelemetryPipeline(telemetryPipeline);
                    MetricManager newManager = new MetricManager(pipelineAdapter);
                    MetricManager prevManager = Interlocked.CompareExchange(ref s_defaultMetricManager, newManager, null);

                    if (prevManager == null)
                    {
                        return newManager;
                    }
                    else
                    {
                        Task fireAndForget = newManager.StopDefaultAggregationCycleAsync();
                        return prevManager;
                    }
                }

                return manager;
            }

            // Ok, we have a non-default config. Get the table:

            ConditionalWeakTable<TelemetryConfiguration, MetricManager> metricManagers = s_metricManagers;
            if (metricManagers == null)
            {
                ConditionalWeakTable<TelemetryConfiguration, MetricManager> newTable = new ConditionalWeakTable<TelemetryConfiguration, MetricManager>();
                ConditionalWeakTable<TelemetryConfiguration, MetricManager> prevTable = Interlocked.CompareExchange(ref s_metricManagers, newTable, null);
                metricManagers = prevTable ?? newTable;
            }

            // Get the manager from the table:
            {
                MetricManager manager = GetOrGreateFromTable(telemetryPipeline, metricManagers);
                return manager;
            }
        }

        private static MetricManager GetOrGreateFromTable(
                                                TelemetryConfiguration telemetryPipeline,
                                                ConditionalWeakTable<TelemetryConfiguration, MetricManager> metricManagers)
        {
            MetricManager createdManager = null;

            MetricManager chosenManager = metricManagers.GetValue(
                                                            telemetryPipeline,
                                                            (tp) =>
                                                            {
                                                                createdManager = new MetricManager(new ApplicationInsightsTelemetryPipeline(tp));
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
