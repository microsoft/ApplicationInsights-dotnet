namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading;

    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    /// <summary>
    /// Represents aggregator for a single time series of a given metric.
    /// </summary>
    public class MetricAggregator
    {
        /// <summary>
        /// Aggregator manager for the aggregator.
        /// </summary>
        private MetricAggregatorManager manager;

        /// <summary>
        /// Metric aggregator id to look for in the aggregator dictionary.
        /// </summary>
        private string aggregatorId;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricAggregator"/> class.
        /// </summary>
        /// <param name="manager">Aggregator manager handling this instance.</param>
        /// <param name="metricName">Metric name.</param>
        /// <param name="dimensions">Metric dimensions.</param>
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

            this.aggregatorId = MetricAggregator.GetAggregatorId(metricName, dimensions);
        }

        /// <summary>
        /// Gets metric name.
        /// </summary>
        internal string MetricName { get; private set; }

        /// <summary>
        /// Gets a set of metric dimensions and their values.
        /// </summary>
        internal IDictionary<string, string> Dimensions { get; private set; }

        /// <summary>
        /// Adds a value to the time series.
        /// </summary>
        /// <param name="value">Metric value.</param>
        public void Track(double value)
        {
            SimpleMetricStatisticsAggregator aggregator = this.manager.AggregatorDictionary.GetOrAdd(
                this.aggregatorId,
                (aid) => { return new SimpleMetricStatisticsAggregator(this.MetricName, this.Dimensions); });

            aggregator.Track(value);

            this.ForwardToProcessors(value);
        }

        /// <summary>
        /// Generates id of the aggregator serving time series specified in the parameters.
        /// </summary>
        /// <param name="metricName">Metric name.</param>
        /// <param name="dimensions">Optional metric dimensions.</param>
        /// <returns>Aggregator id that can be used to get aggregator.</returns>
        private static string GetAggregatorId(string metricName, IDictionary<string, string> dimensions = null)
        {
            StringBuilder aggregatorIdBuilder = new StringBuilder(metricName ?? string.Empty);

            if (dimensions != null)
            {
                var sortedDimensions = dimensions.OrderBy((pair) => { return pair.Key; });

                foreach (KeyValuePair<string, string> pair in sortedDimensions)
                {
                    aggregatorIdBuilder.AppendFormat(CultureInfo.InvariantCulture, "\n{0}\t{1}", pair.Key ?? string.Empty, pair.Value ?? string.Empty);
                }
            }

            return aggregatorIdBuilder.ToString();
        }

        /// <summary>
        /// Forwards value to metric processors.
        /// </summary>
        /// <param name="value">Value tracked on time series.</param>
        private void ForwardToProcessors(double value)
        {
            // create a local reference to metric processor collection
            // if collection changes after that - it will be copied not affecting local reference
            IList<IMetricProcessor> metricProcessors = this.manager.Client.TelemetryConfiguration.MetricProcessors;

            if (metricProcessors != null)
            {
                foreach (IMetricProcessor processor in metricProcessors)
                {
                    try
                    {
                        processor.Track(this.MetricName, value, this.Dimensions);
                    }
                    catch (Exception ex)
                    {
                        CoreEventSource.Log.FailedToRunMetricProcessor(ex.ToString());
                    }
                }
            }
        }
    }
}
