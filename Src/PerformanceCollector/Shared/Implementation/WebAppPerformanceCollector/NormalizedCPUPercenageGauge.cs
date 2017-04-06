namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.WebAppPerformanceCollector
{
    using System;
    using System.Globalization;
    using System.Threading;

    /// <summary>
    /// Gauge that computes normalized CPU percentage utilized by a process by utilizing the last computed time.
    /// </summary>
    internal class NormalizedCPUPercenageGauge : CPUPercenageGauge
    {
        /// <summary>Specific environment variable for Azure App Services.</summary>
        private const string ProcessorsCounterEnvironmentVariable = "NUMBER_OF_PROCESSORS";

        private readonly object lockObject = new object();
        private bool isInitialized = false;
        private int processorsCount = 0;


        /// <summary>
        /// Initializes a new instance of the <see cref="NormalizedCPUPercenageGauge"/> class.
        /// </summary>
        /// <param name="name"> Name of the SumUpCountersGauge.</param>
        /// <param name="value"> Gauges to sum.</param>
        public NormalizedCPUPercenageGauge(string name, ICounterValue value) : base (name, value)
        {
        }

        /// <summary>
        /// Returns the normalized percentage of the CPU process utilization time divided by the number of processors with respect to the total duration.
        /// </summary>
        /// <returns>The value of the target metric.</returns>
        protected override double Collect()
        {
            if (!this.isInitialized)
            {
                lock (this.lockObject)
                {
                    if (!this.isInitialized)
                    {
                        this.processorsCount = this.GetProcessorsCount();
                        this.isInitialized = true;
                    }
                }
            }

            if (this.processorsCount < 1)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Invalid value for processorsCount: {0}", this.processorsCount.ToString(CultureInfo.InvariantCulture));
            }

            double value = base.Collect();
            return value / this.processorsCount;
        }

        private int GetProcessorsCount()
        {
            int count = 0;
            try
            {
                string countString = Environment.GetEnvironmentVariable(ProcessorsCounterEnvironmentVariable);
                if (!int.TryParse(countString, out count) || count < 1)
                {
                    throw new InvalidCastException(string.Format(CultureInfo.InvariantCulture, "Invalid value for NUMBER_OF_PROCESSORS: {0}", countString));
                }
            }
            catch (Exception ex)
            {
                PerformanceCollectorEventSource.Log.AccessingEnvironmentVariableFailedWarning(ProcessorsCounterEnvironmentVariable, ex.ToString());
            }

            return count;
        }
    }
}
