namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents aggregator for a single time series of a given metric.
    /// </summary>
    public class MetricAggregator
    {
        private MetricAggregatorManager manager;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricAggregator"/> class.
        /// </summary>
        internal MetricAggregator(
            MetricAggregatorManager manager,
            string metricName, 
            IDictionary<string, string> dimensions = null)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }

            this.manager = manager;
            this.MetricName = metricName;
            this.Dimensions = dimensions;
        }

        internal string MetricName { get; private set; }

        internal IDictionary<string, string> Dimensions { get; private set; }

        /// <summary>
        /// Adds a value to the time series.
        /// </summary>
        /// <param name="value">Metric value.</param>
        public void Track(double value)
        {
            var metricStats = this.manager.GetSimpleStatisticsAggregator(this);
            metricStats.Track(value);

            this.manager.ForwardToProcessors(this.MetricName, value, this.Dimensions);
        }
    }
}
