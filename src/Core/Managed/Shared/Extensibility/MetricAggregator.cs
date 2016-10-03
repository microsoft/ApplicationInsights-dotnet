namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;

    /// <summary>
    /// Represents aggregator for a single time series of a given metric.
    /// </summary>
    public class MetricAggregator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetricAggregator"/> class.
        /// </summary>
        internal MetricAggregator()
        {
        }

        /// <summary>
        /// Adds a value to the time series.
        /// </summary>
        /// <param name="value">Metric value.</param>
        public void Track(double value)
        {
            throw new NotImplementedException();
        }
    }
}
