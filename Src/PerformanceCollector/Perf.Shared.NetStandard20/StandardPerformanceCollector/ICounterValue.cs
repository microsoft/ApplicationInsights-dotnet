namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.StandardPerformanceCollector
{
    /// <summary>
    /// Interface represents the counter value.
    /// </summary>
    internal interface ICounterValue
    {
        /// <summary>
        /// Returns the current value of the counter as a <c ref="MetricTelemetry"/>.
        /// </summary>
        /// <returns>Value of the counter.</returns>
        double Collect();
    }
}
