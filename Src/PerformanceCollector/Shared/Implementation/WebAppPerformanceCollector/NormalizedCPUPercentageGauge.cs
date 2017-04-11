namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.WebAppPerformanceCollector
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Gauge that computes normalized CPU percentage utilized by a process by utilizing the last computed time (divided by the processors count).
    /// </summary>
    internal class NormalizedCPUPercentageGauge : CPUPercenageGauge
    {
        /// <summary>Specific environment variable for Azure App Services.</summary>
        private const string ProcessorsCounterEnvironmentVariable = "NUMBER_OF_PROCESSORS";

        private bool isInitialized = false;
        private int processorsCount = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="NormalizedCPUPercentageGauge"/> class.
        /// </summary>
        /// <param name="name"> Name of the SumUpCountersGauge.</param>
        /// <param name="value"> Gauges to sum.</param>
        public NormalizedCPUPercentageGauge(string name, ICounterValue value) : base(name, value)
        {
            string countString = string.Empty;
            try
            {
                countString = Environment.GetEnvironmentVariable(ProcessorsCounterEnvironmentVariable);
            }
            catch (Exception ex)
            {
                PerformanceCollectorEventSource.Log.ProcessorsCountIncorrectValueError(ex.ToString());
                return;
            }

            if (!int.TryParse(countString, out this.processorsCount))
            {
                PerformanceCollectorEventSource.Log.ProcessorsCountIncorrectValueError(countString);
                return;
            }

            if (this.processorsCount < 1 || this.processorsCount > 1000)
            {
                PerformanceCollectorEventSource.Log.ProcessorsCountIncorrectValueError(this.processorsCount.ToString(CultureInfo.InvariantCulture));
                return;
            }

            this.isInitialized = true;
        }

        /// <summary>
        /// Returns the normalized percentage of the CPU process utilization time divided by the number of processors with respect to the total duration.
        /// </summary>
        /// <returns>The value of the target metric.</returns>
        protected override double Collect()
        {
            if (!this.isInitialized)
            {
                return 0;
            }

            double result = 0;
            if (this.processorsCount >= 1)
            {
                double value = base.Collect();
                result = value / this.processorsCount;
            }

            return result;
        }
    }
}
