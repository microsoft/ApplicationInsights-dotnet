namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.StandardPerformanceCollector
{
    using System;

    /// <summary>
    /// Factory to create different counters.
    /// </summary>
    internal class CounterFactory
    {
        /// <summary>
        /// Gets a counter.
        /// </summary>
        /// <param name="originalString">Original string definition of the counter.</param>
        /// <param name="categoryName">Category name.</param>
        /// <param name="counterName">Counter name.</param>
        /// <param name="instanceName">Instance name.</param>
        /// <returns>The counter identified by counter name.</returns>
        internal ICounterValue GetCounter(string originalString, string categoryName, string counterName, string instanceName)
        {
            switch (originalString)
            {
                case @"\Process(??APP_WIN32_PROC??)\% Processor Time Normalized":
                    return new NormalizedProcessCPUPerformanceCounter(categoryName, counterName, instanceName);
                default:
                    return new StandardPerformanceCounter(categoryName, counterName, instanceName);
            }
        }
    }
}