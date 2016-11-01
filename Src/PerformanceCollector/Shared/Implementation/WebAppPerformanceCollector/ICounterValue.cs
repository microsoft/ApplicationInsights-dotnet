namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.WebAppPerformanceCollector
{
    using Microsoft.ApplicationInsights.DataContracts;

    /// <summary>
    /// Interface represents the counter value.
    /// </summary>
    internal interface ICounterValue
    {
        /// <summary>
        /// Returns the current value of the counter as a <c ref="MetricTelemetry"/> and resets the metric.
        /// </summary>
        /// <returns>Value of the counter.</returns>
        double GetValueAndReset();
    }
}
