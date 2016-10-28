namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading;

    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    /// <summary>
    /// Represents aggregator for a single time series of a given metric.
    /// </summary>
    public class MetricAggregator
    {
        /// <summary>
        /// Telemetry configuration for the aggregator.
        /// </summary>
        private TelemetryConfiguration telemetryConfiguration;

        /// <summary>
        /// Lock to make Track() method thread-safe.
        /// </summary>
        private SpinLock trackLock = new SpinLock();

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricAggregator"/> class.
        /// </summary>
        internal MetricAggregator(
            TelemetryConfiguration telemetryConfiguration,
            string metricName, 
            IDictionary<string, string> dimensions = null)
        {
            if (telemetryConfiguration == null)
            {
                throw new ArgumentNullException("telemetryConfiguration");
            }

            this.telemetryConfiguration = telemetryConfiguration;
            this.MetricName = metricName;
            this.Dimensions = dimensions;
        }

        /// <summary>
        /// Gets sample count.
        /// </summary>
        internal int Count { get; private set; }

        /// <summary>
        /// Gets sum of the samples.
        /// </summary>
        internal double Sum { get; private set; }

        /// <summary>
        /// Gets sum of squares of the samples.
        /// </summary>
        internal double SumOfSquares { get; private set; }

        /// <summary>
        /// Gets minimum sample value.
        /// </summary>
        internal double Min { get; private set; }

        /// <summary>
        /// Gets maximum sample value.
        /// </summary>
        internal double Max { get; private set; }

        /// <summary>
        /// Gets arithmetic average value in the population.
        /// </summary>
        internal double Average
        {
            get
            {
                return this.Count == 0 ? 0 : this.Sum / this.Count;
            }
        }

        /// <summary>
        /// Gets variance of the values in the population.
        /// </summary>
        internal double Variance
        {
            get
            {
                return this.Count == 0 ? 0 : (this.SumOfSquares / this.Count) - (this.Average * this.Average);
            }
        }

        /// <summary>
        /// Gets standard deviation of the values in the population.
        /// </summary>
        internal double StandardDeviation
        {
            get
            {
                return Math.Sqrt(this.Variance);
            }
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
            bool lockAcquired = false;

            try
            {
                this.trackLock.Enter(ref lockAcquired);

                if ((this.Count == 0) || (value < this.Min))
                {
                    this.Min = value;
                }

                if ((this.Count == 0) || (value > this.Max))
                {
                    this.Max = value;
                }

                this.Count++;
                this.Sum += value;
                this.SumOfSquares += value * value;
            }
            finally
            {
                if (lockAcquired)
                {
                    this.trackLock.Exit();
                }
            }

            this.ForwardToProcessors(value);
        }

        /// <summary>
        /// Generates id of the aggregator serving time series specified in the parameters.
        /// </summary>
        /// <param name="metricName">Metric name.</param>
        /// <param name="dimensions">Optional metric dimensions.</param>
        /// <returns>Aggregator id that can be used to get aggregator.</returns>
        internal static string GetAggregatorId(string metricName, IDictionary<string, string> dimensions = null)
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
            IList<IMetricProcessor> metricProcessors = this.telemetryConfiguration.MetricProcessors;

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
