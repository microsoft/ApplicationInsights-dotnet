namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a hub for metric aggregation procedures.
    /// </summary>
    public class MetricAggregatorManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetricAggregatorManager"/> class.
        /// </summary>
        public MetricAggregatorManager()
            : this(new TelemetryClient())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricAggregatorManager"/> class.
        /// </summary>
        /// <param name="client">Telemetry client to use to output aggregated metric data.</param>
        public MetricAggregatorManager(TelemetryClient client)
        {
        }

        /// <summary>
        /// Creates single time series data aggregator.
        /// </summary>
        /// <param name="metricName">Name of the metric.</param>
        /// <param name="dimensions">Optional dimensions.</param>
        /// <returns>Value aggregator for the metric specified.</returns>
        public MetricAggregator CreateMetricAggregator(string metricName, IDictionary<string, string> dimensions = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Flushes the in-memory aggregation buffers.
        /// </summary>
        public void Flush()
        {
            throw new NotImplementedException();
        }
    }
}
