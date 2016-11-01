namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Represents aggregator for a single time series of a given metric.
    /// </summary>
    internal class SimpleMetricStatisticsAggregator
    {
        /// <summary>
        /// Lock to make Track() method thread-safe.
        /// </summary>
        private SpinLock trackLock = new SpinLock();

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleMetricStatisticsAggregator"/> class.
        /// </summary>
        /// <param name="metricName">Metric name.</param>
        /// <param name="dimensions">Metric dimensions.</param>
        internal SimpleMetricStatisticsAggregator(
            string metricName,
            IDictionary<string, string> dimensions = null)
        {
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
        }
    }
}
