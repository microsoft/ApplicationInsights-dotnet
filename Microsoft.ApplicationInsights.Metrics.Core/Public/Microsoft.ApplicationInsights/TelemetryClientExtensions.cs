using System;

using Microsoft.ApplicationInsights.Metrics;
using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights
{
    /// <summary>
    /// 
    /// </summary>
    public static class TelemetryClientExtensions
    {
        /// <summary>
        /// Gets or creates a metric container that you can use to track, aggregate and send metric values.<br />
        /// Optionally specify a metric configuration to control how the tracked values are aggregated.
        /// </summary>
        /// <param name="telemetryClient">The aggregated values will be sent to the <c>TelemetryConfiguration</c>
        /// associated with this client.<br />
        /// The aggregation scope of the fetched<c>Metric</c> is <c>TelemetryConfiguration</c>; this
        /// means that all values tracked for a given metric ID and dimensions will be aggregated together
        /// across all clients that share the same <c>TelemetryConfiguration</c>.</param>
        /// <param name="metricId">The ID (name) of the metric.</param>
        /// <returns>A <c>Metric</c> with the specified ID and dimensions. If you call this method several times
        /// with the same metric ID and dimensions for a given aggregation scope, you will receive the same
        /// instance of <c>Metric</c>.</returns>
        /// <exception cref="ArgumentException">If you previously created a metric with the same ID, dimensions
        /// and aggregation scope, but with a different configuration. When calling this method to get a previously
        /// created metric, you can simply avoid specifying any configuration (or specify null) to imply the
        /// configuration used earlier.</exception>
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
        /// Gets or creates a metric container that you can use to track, aggregate and send metric values.<br />
        /// Optionally specify a metric configuration to control how the tracked values are aggregated.
        /// </summary>
        /// <param name="telemetryClient">The aggregated values will be sent to the <c>TelemetryConfiguration</c>
        /// associated with this client.<br />
        /// The aggregation scope of the fetched<c>Metric</c> is <c>TelemetryConfiguration</c>; this
        /// means that all values tracked for a given metric ID and dimensions will be aggregated together
        /// across all clients that share the same <c>TelemetryConfiguration</c>.</param>
        /// <param name="metricId">The ID (name) of the metric.</param>
        /// <param name="metricConfiguration">Determines how tracked values will be aggregated. <br />
        /// Use presets in <see cref="MetricConfigurations.Common"/> or specify your own settings. </param>
        /// <returns>A <c>Metric</c> with the specified ID and dimensions. If you call this method several times
        /// with the same metric ID and dimensions for a given aggregation scope, you will receive the same
        /// instance of <c>Metric</c>.</returns>
        /// <exception cref="ArgumentException">If you previously created a metric with the same ID, dimensions
        /// and aggregation scope, but with a different configuration. When calling this method to get a previously
        /// created metric, you can simply avoid specifying any configuration (or specify null) to imply the
        /// configuration used earlier.</exception>
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
        /// Gets or creates a metric container that you can use to track, aggregate and send metric values.<br />
        /// Optionally specify a metric configuration to control how the tracked values are aggregated.
        /// </summary>
        /// <param name="telemetryClient">The aggregated values will be sent using the specified client.</param>
        /// <param name="metricId">The ID (name) of the metric.</param>
        /// <param name="metricConfiguration">Determines how tracked values will be aggregated. <br />
        /// Use presets in <see cref="MetricConfigurations.Common"/> or specify your own settings. </param>
        /// <returns>A <c>Metric</c> with the specified ID and dimensions. If you call this method several times
        /// with the same metric ID and dimensions for a given aggregation scope, you will receive the same
        /// instance of <c>Metric</c>.</returns>
        /// <exception cref="ArgumentException">If you previously created a metric with the same ID, dimensions
        /// and aggregation scope, but with a different configuration. When calling this method to get a previously
        /// created metric, you can simply avoid specifying any configuration (or specify null) to imply the
        /// configuration used earlier.</exception>
        /// <param name="aggregationScope">The scope across which the values for the metric are to be aggregated in memory.
        /// See <see cref="MetricAggregationScope" /> for more info.</param>
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
        /// Gets or creates a metric container that you can use to track, aggregate and send metric values.<br />
        /// Optionally specify a metric configuration to control how the tracked values are aggregated.
        /// </summary>
        /// <param name="telemetryClient">The aggregated values will be sent to the <c>TelemetryConfiguration</c>
        /// associated with this client.<br />
        /// The aggregation scope of the fetched<c>Metric</c> is <c>TelemetryConfiguration</c>; this
        /// means that all values tracked for a given metric ID and dimensions will be aggregated together
        /// across all clients that share the same <c>TelemetryConfiguration</c>.</param>
        /// <param name="metricId">The ID (name) of the metric.</param>
        /// <param name="dimension1Name">The name of the first dimension.</param>
        /// <exception cref="ArgumentException">If you previously created a metric with the same ID, dimensions
        /// and aggregation scope, but with a different configuration. When calling this method to get a previously
        /// created metric, you can simply avoid specifying any configuration (or specify null) to imply the
        /// configuration used earlier.</exception>
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
        /// Gets or creates a metric container that you can use to track, aggregate and send metric values.<br />
        /// Optionally specify a metric configuration to control how the tracked values are aggregated.
        /// </summary>
        /// <param name="telemetryClient">The aggregated values will be sent to the <c>TelemetryConfiguration</c>
        /// associated with this client.<br />
        /// The aggregation scope of the fetched<c>Metric</c> is <c>TelemetryConfiguration</c>; this
        /// means that all values tracked for a given metric ID and dimensions will be aggregated together
        /// across all clients that share the same <c>TelemetryConfiguration</c>.</param>
        /// <param name="metricId">The ID (name) of the metric.</param>
        /// <param name="dimension1Name">The name of the first dimension.</param>
        /// <param name="metricConfiguration">Determines how tracked values will be aggregated. <br />
        /// Use presets in <see cref="MetricConfigurations.Common"/> or specify your own settings. </param>
        /// <returns>A <c>Metric</c> with the specified ID and dimensions. If you call this method several times
        /// with the same metric ID and dimensions for a given aggregation scope, you will receive the same
        /// instance of <c>Metric</c>.</returns>
        /// <exception cref="ArgumentException">If you previously created a metric with the same ID, dimensions
        /// and aggregation scope, but with a different configuration. When calling this method to get a previously
        /// created metric, you can simply avoid specifying any configuration (or specify null) to imply the
        /// configuration used earlier.</exception>
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
        /// Gets or creates a metric container that you can use to track, aggregate and send metric values.<br />
        /// Optionally specify a metric configuration to control how the tracked values are aggregated.
        /// </summary>
        /// <param name="telemetryClient">The aggregated values will be sent using the specified client.</param>
        /// <param name="metricId">The ID (name) of the metric.</param>
        /// <param name="dimension1Name">The name of the first dimension.</param>
        /// <param name="metricConfiguration">Determines how tracked values will be aggregated. <br />
        /// Use presets in <see cref="MetricConfigurations.Common"/> or specify your own settings. </param>
        /// <returns>A <c>Metric</c> with the specified ID and dimensions. If you call this method several times
        /// with the same metric ID and dimensions for a given aggregation scope, you will receive the same
        /// instance of <c>Metric</c>.</returns>
        /// <exception cref="ArgumentException">If you previously created a metric with the same ID, dimensions
        /// and aggregation scope, but with a different configuration. When calling this method to get a previously
        /// created metric, you can simply avoid specifying any configuration (or specify null) to imply the
        /// configuration used earlier.</exception>
        /// <param name="aggregationScope">The scope across which the values for the metric are to be aggregated in memory.
        /// See <see cref="MetricAggregationScope" /> for more info.</param>
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
        /// Gets or creates a metric container that you can use to track, aggregate and send metric values.<br />
        /// Optionally specify a metric configuration to control how the tracked values are aggregated.
        /// </summary>
        /// <param name="telemetryClient">The aggregated values will be sent to the <c>TelemetryConfiguration</c>
        /// associated with this client.<br />
        /// The aggregation scope of the fetched<c>Metric</c> is <c>TelemetryConfiguration</c>; this
        /// means that all values tracked for a given metric ID and dimensions will be aggregated together
        /// across all clients that share the same <c>TelemetryConfiguration</c>.</param>
        /// <param name="metricId">The ID (name) of the metric.</param>
        /// <param name="dimension1Name">The name of the first dimension.</param>
        /// <param name="dimension2Name">The name of the second dimension.</param>
        /// <exception cref="ArgumentException">If you previously created a metric with the same ID, dimensions
        /// and aggregation scope, but with a different configuration. When calling this method to get a previously
        /// created metric, you can simply avoid specifying any configuration (or specify null) to imply the
        /// configuration used earlier.</exception>
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
        /// Gets or creates a metric container that you can use to track, aggregate and send metric values.<br />
        /// Optionally specify a metric configuration to control how the tracked values are aggregated.
        /// </summary>
        /// <param name="telemetryClient">The aggregated values will be sent to the <c>TelemetryConfiguration</c>
        /// associated with this client.<br />
        /// The aggregation scope of the fetched<c>Metric</c> is <c>TelemetryConfiguration</c>; this
        /// means that all values tracked for a given metric ID and dimensions will be aggregated together
        /// across all clients that share the same <c>TelemetryConfiguration</c>.</param>
        /// <param name="metricId">The ID (name) of the metric.</param>
        /// <param name="dimension1Name">The name of the first dimension.</param>
        /// <param name="dimension2Name">The name of the second dimension.</param>
        /// <param name="metricConfiguration">Determines how tracked values will be aggregated. <br />
        /// Use presets in <see cref="MetricConfigurations.Common"/> or specify your own settings. </param>
        /// <returns>A <c>Metric</c> with the specified ID and dimensions. If you call this method several times
        /// with the same metric ID and dimensions for a given aggregation scope, you will receive the same
        /// instance of <c>Metric</c>.</returns>
        /// <exception cref="ArgumentException">If you previously created a metric with the same ID, dimensions
        /// and aggregation scope, but with a different configuration. When calling this method to get a previously
        /// created metric, you can simply avoid specifying any configuration (or specify null) to imply the
        /// configuration used earlier.</exception>
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
        /// Gets or creates a metric container that you can use to track, aggregate and send metric values.<br />
        /// Optionally specify a metric configuration to control how the tracked values are aggregated.
        /// </summary>
        /// <param name="telemetryClient">The aggregated values will be sent using the specified client.</param>
        /// <param name="metricId">The ID (name) of the metric.</param>
        /// <param name="dimension1Name">The name of the first dimension.</param>
        /// <param name="dimension2Name">The name of the second dimension.</param>
        /// <param name="metricConfiguration">Determines how tracked values will be aggregated. <br />
        /// Use presets in <see cref="MetricConfigurations.Common"/> or specify your own settings. </param>
        /// <returns>A <c>Metric</c> with the specified ID and dimensions. If you call this method several times
        /// with the same metric ID and dimensions for a given aggregation scope, you will receive the same
        /// instance of <c>Metric</c>.</returns>
        /// <exception cref="ArgumentException">If you previously created a metric with the same ID, dimensions
        /// and aggregation scope, but with a different configuration. When calling this method to get a previously
        /// created metric, you can simply avoid specifying any configuration (or specify null) to imply the
        /// configuration used earlier.</exception>
        /// <param name="aggregationScope">The scope across which the values for the metric are to be aggregated in memory.
        /// See <see cref="MetricAggregationScope" /> for more info.</param>
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

            MetricManager metricManager = telemetryClient.GetMetricManager(aggregationScope);
            MetricsCache cache = metricManager.GetOrCreateExtensionState(MetricsCache.CreateNewInstance);

            if (cache == null)
            {
                throw new InvalidOperationException($"telemetryConfiguration.GetMetricManager().GetOrCreateExtensionState(..) unexpectedly returned null."
                                                  + $" This indicates that multiple extensions attempt to use"
                                                  + $" the \"Cache\" extension point of the {nameof(MetricManager)} in a conflicting manner.");
            }

            Metric metric = cache.GetOrCreateMetric(metricId, dimension1Name, dimension2Name, metricConfiguration);
            return metric;
        }
    }
}