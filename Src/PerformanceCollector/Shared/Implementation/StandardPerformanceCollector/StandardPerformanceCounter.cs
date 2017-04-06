namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.StandardPerformanceCollector
{
    using System;
    using System.Diagnostics;
    using System.Globalization;

    /// <summary>
    /// Interface represents the counter value.
    /// </summary>
    internal class StandardPerformanceCounter : ICounterValue
    {
        PerformanceCounter performanceCounter = null;

        internal StandardPerformanceCounter(string categoryName, string counterName, string instanceName)
        {
            performanceCounter = new PerformanceCounter(categoryName, counterName, instanceName, true);
        }

        /// <summary>
        /// Returns the current value of the counter as a <c ref="MetricTelemetry"/>.
        /// </summary>
        /// <returns>Value of the counter.</returns>
        public double Collect()
        {
            try
            {
                return this.performanceCounter.NextValue();
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
    }
}
