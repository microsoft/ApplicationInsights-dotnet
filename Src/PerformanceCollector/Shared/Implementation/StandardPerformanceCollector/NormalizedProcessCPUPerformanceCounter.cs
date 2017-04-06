namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.StandardPerformanceCollector
{
    using System;
    using System.Diagnostics;
    using System.Globalization;

    /// <summary>
    /// Interface represents normalized value of CPU Utilization by Process counter value.
    /// </summary>
    internal class NormalizedProcessCPUPerformanceCounter : ICounterValue, IDisposable
    {
        private PerformanceCounter performanceCounter = null;

        /// <summary>
        ///  Initializes a new instance of the <see cref="NormalizedProcessCPUPerformanceCounter" /> class.
        /// </summary>
        /// <param name="categoryName">The counter category name.</param>
        /// <param name="counterName">The counter name.</param>
        /// <param name="instanceName">The instance name.</param>
        internal NormalizedProcessCPUPerformanceCounter(string categoryName, string counterName, string instanceName)
        {
            this.performanceCounter = new PerformanceCounter("Process", "% Processor Time", instanceName, true);
        }

        /// <summary>
        /// Returns the current value of the counter as a <c ref="MetricTelemetry"/>.
        /// </summary>
        /// <returns>Value of the counter.</returns>
        public double Collect()
        {
            try
            {
                return this.performanceCounter.NextValue() / Environment.ProcessorCount;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.PerformanceCounterReadFailed,
                        PerformanceCounterUtility.FormatPerformanceCounter(this.performanceCounter)),
                    e);
            }
        }

        /// <summary>
        /// Disposes resources allocated by this type.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose implementation.
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.performanceCounter != null)
                {
                    this.performanceCounter.Dispose();
                    this.performanceCounter = null;
                }
            }
        }
    }
}
