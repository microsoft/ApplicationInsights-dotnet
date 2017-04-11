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

        private readonly object lockObject = new object();
        private bool isInitialized = false;
        private int processorsCount = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="NormalizedCPUPercentageGauge"/> class.
        /// </summary>
        /// <param name="name"> Name of the SumUpCountersGauge.</param>
        /// <param name="value"> Gauges to sum.</param>
        public NormalizedCPUPercentageGauge(string name, ICounterValue value) : base(name, value)
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

            double result = 0;
            if (this.processorsCount >= 1)
            {
                double value = base.Collect();
                result = value / this.processorsCount;
            }

            return result;
        }

        private int GetProcessorsCount()
        {
            int count = -1;
            try
            {
                string countString = Environment.GetEnvironmentVariable(ProcessorsCounterEnvironmentVariable);
                if (!int.TryParse(countString, out count) || count < 1 || count > 1000)
                {
                    count = -1;
                    string message = string.Format(CultureInfo.CurrentCulture, Resources.WebAppProcessorsCountReadFailed, countString);
                    PerformanceCollectorEventSource.Log.AccessingEnvironmentVariableFailedWarning(ProcessorsCounterEnvironmentVariable, message);

                    throw new InvalidOperationException(message);
                }
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                count = -1;
                PerformanceCollectorEventSource.Log.AccessingEnvironmentVariableFailedWarning(ProcessorsCounterEnvironmentVariable, ex.ToString());

                throw new InvalidOperationException(ex.Message, ex);
            }

            return count;
        }
    }
}
