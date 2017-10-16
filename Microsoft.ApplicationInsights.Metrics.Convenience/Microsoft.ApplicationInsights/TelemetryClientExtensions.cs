using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Metrics;
using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights
{
    /// <summary>
    /// 
    /// </summary>
    public static class TelemetryClientExtensions
    {
        private static ConditionalWeakTable<TelemetryClient, MetricManager> s_metricManagersForTelemetryClients;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="telemetryClient"></param>
        /// <param name="metricId"></param>
        /// <returns></returns>
        public static Metric GetMetric(
                                    this TelemetryClient telemetryClient,
                                    string metricId)
        {
            return GetOrCreateMetric(
                                telemetryClient,
                                MetricAggregationScope.TelemetryConfiguration,
                                metricId,
                                dimension1Name: null,
                                dimension2Name: null,
                                metricConfiguration: null);
        }

        /// <summary>
        /// </summary>
        /// <param name="telemetryClient"></param>
        /// <param name="metricId"></param>
        /// <param name="metricConfiguration"></param>
        /// <returns></returns>
        public static Metric GetMetric(
                                    this TelemetryClient telemetryClient,
                                    string metricId,
                                    IMetricConfiguration metricConfiguration)
        {
            return GetOrCreateMetric(
                                telemetryClient,
                                MetricAggregationScope.TelemetryConfiguration,
                                metricId,
                                dimension1Name: null,
                                dimension2Name: null,
                                metricConfiguration: metricConfiguration);
            
        }

        /// <summary>
        /// </summary>
        /// <param name="telemetryClient"></param>
        /// <param name="metricId"></param>
        /// <param name="metricConfiguration"></param>
        /// <param name="aggregationScope">The scope across which the values for the metric are to be aggregated in memory. See <see cref="MetricAggregationScope" />.</param>
        /// <returns></returns>
        public static Metric GetMetric(
                                    this TelemetryClient telemetryClient,
                                    string metricId,
                                    IMetricConfiguration metricConfiguration,
                                    MetricAggregationScope aggregationScope)
        {
            return GetOrCreateMetric(
                                telemetryClient,
                                aggregationScope,
                                metricId,
                                dimension1Name: null,
                                dimension2Name: null,
                                metricConfiguration: metricConfiguration);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="telemetryClient"></param>
        /// <param name="metricId"></param>
        /// <param name="dimension1Name"></param>
        /// <returns></returns>
        public static Metric GetMetric(
                                    this TelemetryClient telemetryClient,
                                    string metricId,
                                    string dimension1Name)
        {
            Util.ValidateNotNullOrWhitespace(dimension1Name, nameof(dimension1Name));

            return GetOrCreateMetric(
                                telemetryClient,
                                MetricAggregationScope.TelemetryConfiguration,
                                metricId,
                                dimension1Name,
                                dimension2Name: null,
                                metricConfiguration: null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="telemetryClient"></param>
        /// <param name="metricId"></param>
        /// <param name="dimension1Name"></param>
        /// <param name="metricConfiguration"></param>
        /// <returns></returns>
        public static Metric GetMetric(
                                    this TelemetryClient telemetryClient,
                                    string metricId,
                                    string dimension1Name,
                                    IMetricConfiguration metricConfiguration)
        {
            Util.ValidateNotNullOrWhitespace(dimension1Name, nameof(dimension1Name));

            return GetOrCreateMetric(
                                telemetryClient,
                                MetricAggregationScope.TelemetryConfiguration, 
                                metricId,
                                dimension1Name,
                                dimension2Name: null,
                                metricConfiguration: metricConfiguration);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="telemetryClient"></param>
        /// <param name="metricId"></param>
        /// <param name="dimension1Name"></param>
        /// <param name="metricConfiguration"></param>
        /// <param name="aggregationScope">The scope across which the values for the metric are to be aggregated in memory. See <see cref="MetricAggregationScope" />.</param>
        /// <returns></returns>
        public static Metric GetMetric(
                                    this TelemetryClient telemetryClient,
                                    string metricId,
                                    string dimension1Name,
                                    IMetricConfiguration metricConfiguration,
                                    MetricAggregationScope aggregationScope)
        {
            Util.ValidateNotNullOrWhitespace(dimension1Name, nameof(dimension1Name));

            return GetOrCreateMetric(
                                telemetryClient,
                                aggregationScope,
                                metricId,
                                dimension1Name,
                                dimension2Name: null,
                                metricConfiguration: metricConfiguration);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="telemetryClient"></param>
        /// <param name="metricId"></param>
        /// <param name="dimension1Name"></param>
        /// <param name="dimension2Name"></param>
        /// <returns></returns>
        public static Metric GetMetric(
                                    this TelemetryClient telemetryClient,
                                    string metricId,
                                    string dimension1Name,
                                    string dimension2Name)
        {
            Util.ValidateNotNullOrWhitespace(dimension1Name, nameof(dimension1Name));
            Util.ValidateNotNullOrWhitespace(dimension2Name, nameof(dimension2Name));

            return GetOrCreateMetric(
                                telemetryClient,
                                MetricAggregationScope.TelemetryConfiguration,
                                metricId,
                                dimension1Name,
                                dimension2Name,
                                metricConfiguration: null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="telemetryClient"></param>
        /// <param name="metricId"></param>
        /// <param name="dimension1Name"></param>
        /// <param name="dimension2Name"></param>
        /// <param name="metricConfiguration"></param>
        /// <returns></returns>
        public static Metric GetMetric(
                                    this TelemetryClient telemetryClient,
                                    string metricId,
                                    string dimension1Name,
                                    string dimension2Name,
                                    IMetricConfiguration metricConfiguration)
        {
            Util.ValidateNotNullOrWhitespace(dimension1Name, nameof(dimension1Name));
            Util.ValidateNotNullOrWhitespace(dimension2Name, nameof(dimension2Name));

            return GetOrCreateMetric(
                                telemetryClient,
                                MetricAggregationScope.TelemetryConfiguration, 
                                metricId, 
                                dimension1Name, 
                                dimension2Name, 
                                metricConfiguration);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="telemetryClient"></param>
        /// <param name="metricId"></param>
        /// <param name="dimension1Name"></param>
        /// <param name="dimension2Name"></param>
        /// <param name="metricConfiguration"></param>
        /// <param name="aggregationScope">The scope across which the values for the metric are to be aggregated in memory. See <see cref="MetricAggregationScope" />.</param>
        /// <returns></returns>
        public static Metric GetMetric(
                                    this TelemetryClient telemetryClient,
                                    string metricId,
                                    string dimension1Name,
                                    string dimension2Name,
                                    IMetricConfiguration metricConfiguration,
                                    MetricAggregationScope aggregationScope)
        {
            Util.ValidateNotNullOrWhitespace(dimension1Name, nameof(dimension1Name));
            Util.ValidateNotNullOrWhitespace(dimension2Name, nameof(dimension2Name));

            return GetOrCreateMetric(
                                telemetryClient,
                                aggregationScope,
                                metricId,
                                dimension1Name,
                                dimension2Name,
                                metricConfiguration);
        }

        private static Metric GetOrCreateMetric(
                                    TelemetryClient telemetryClient,
                                    MetricAggregationScope aggregationScope,
                                    string metricId,
                                    string dimension1Name,
                                    string dimension2Name,
                                    IMetricConfiguration metricConfiguration)
        {
            Util.ValidateNotNull(telemetryClient, nameof(telemetryClient));

            MetricManager metricManager;

            switch(aggregationScope)
            {
                case MetricAggregationScope.TelemetryConfiguration:
                    TelemetryConfiguration pipeline = Util.GetTelemetryConfiguration(telemetryClient);
                    metricManager = pipeline.Metrics();
                    break;

                case MetricAggregationScope.TelemetryClient:
                    metricManager = GetOrCreateMetricManager(telemetryClient);
                    break;

                default:
                    throw new ArgumentException($"Invalid value of {nameof(aggregationScope)} ({aggregationScope}). Only the following values are supported:"
                                              + $" ['{nameof(MetricAggregationScope)}.{MetricAggregationScope.TelemetryClient.ToString()}',"
                                              + $" '{nameof(MetricAggregationScope)}.{MetricAggregationScope.TelemetryConfiguration.ToString()}'].");
            }

            MetricsCache cache = metricManager.GetOrCreateExtensionState(MetricsCache.CreateNewInstance);

            if (cache == null)
            {
                throw new InvalidOperationException($"telemetryConfiguration.Metrics().GetOrCreateExtensionState(..) unexpectedly returned null."
                                                  + $" This indicates that multiple extensions attempt to use"
                                                  + $" the \"Cache\" extension point of the {nameof(MetricManager)} in a conflicting manner.");
            }

            Metric metric = cache.GetOrCreateMetric(metricId, dimension1Name, dimension2Name, metricConfiguration);
            return metric;
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
