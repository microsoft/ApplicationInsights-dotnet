namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.StandardPerformanceCollector
{
    using System;
    using System.Collections.Generic;

    internal class StandardPerformanceCollector : IPerformanceCollector
    {
        private static readonly Tuple<PerformanceCounterData, double>[] emptyCollectResult = new Tuple<PerformanceCounterData, double>[0];

        /// <summary>
        /// Gets a collection of registered performance counters.
        /// </summary>
        public IEnumerable<PerformanceCounterData> PerformanceCounters { get; } = new PerformanceCounterData[0];

        /// <summary>
        /// Performs collection for all registered counters.
        /// </summary>
        /// <param name="onReadingFailure">Invoked when an individual counter fails to be read.</param>
        public IEnumerable<Tuple<PerformanceCounterData, double>> Collect(Action<string, Exception> onReadingFailure = null)
        {
            return StandardPerformanceCollector.emptyCollectResult;
        }

        /// <summary>
        /// Refreshes counters.
        /// </summary>
        public void RefreshCounters()
        {
        }

        /// <summary>
        /// Registers a counter using the counter name and reportAs value to the total list of counters.
        /// </summary>
        /// <param name="perfCounterName">Name of the performance counter.</param>
        /// <param name="reportAs">Report as name for the performance counter.</param>
        /// <param name="isCustomCounter">Boolean to check if the performance counter is custom defined.</param>
        /// <param name="error">Captures the error logged.</param>
        /// <param name="blockCounterWithInstancePlaceHolder">Boolean that controls the registry of the counter based on the availability of instance place holder.</param>
        public void RegisterCounter(
            string perfCounterName,
            string reportAs,
            bool isCustomCounter,
            out string error,
            bool blockCounterWithInstancePlaceHolder = false)
        {
            error = string.Empty;
        }

        /// <summary>
        /// Removes a counter.
        /// </summary>
        /// <param name="perfCounter">Name of the performance counter to remove.</param>
        /// <param name="reportAs">ReportAs value of the performance counter to remove.</param>
        public void RemoveCounter(string perfCounter, string reportAs)
        {
        }
    }
}
