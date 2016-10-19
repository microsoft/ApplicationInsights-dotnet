namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Threading;

    /// <summary>
    /// Stream aggregates metric samples to produce basic statistical parameters of the population.
    /// </summary>
    internal class SimpleMetricStatsAggregator
    {
        /// <summary>
        /// Lock to make Track() method thread-safe.
        /// </summary>
        private SpinLock trackLock = new SpinLock();

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleMetricStatsAggregator"/> class.
        /// </summary>
        public SimpleMetricStatsAggregator()
        {
        }

        /// <summary>
        /// Gets sample count.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Gets sum of the samples.
        /// </summary>
        public double Sum { get; private set; }

        /// <summary>
        /// Gets sum of squares of the samples.
        /// </summary>
        public double SumOfSquares { get; private set; }

        /// <summary>
        /// Gets minimum sample value.
        /// </summary>
        public double Min { get; private set; }

        /// <summary>
        /// Gets maximum sample value.
        /// </summary>
        public double Max { get; private set; }

        /// <summary>
        /// Gets arithmetic average value in the population.
        /// </summary>
        public double Average
        {
            get
            {
                return this.Count == 0 ? 0 : this.Sum / this.Count;
            }
        }

        /// <summary>
        /// Gets variance of the values in the population.
        /// </summary>
        public double Variance
        {
            get
            {
                return this.Count == 0 ? 0 : (this.SumOfSquares / this.Count) - (this.Average * this.Average);
            }
        }

        /// <summary>
        /// Gets standard deviation of the values in the population.
        /// </summary>
        public double StandardDeviation
        {
            get
            {
                return Math.Sqrt(this.Variance);
            }
        }

        /// <summary>
        /// Adds metric sample to the population.
        /// </summary>
        /// <param name="value">Metric sample value.</param>
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
