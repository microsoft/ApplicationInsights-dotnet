namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.StandardPerfCollector
{
    using System;
    using System.Collections.Generic;

    internal class StandardPerformanceCollectorStub : IPerformanceCollector
    {
        private static readonly Tuple<PerformanceCounterData, double>[] emptyCollectResult = Array.Empty<Tuple<PerformanceCounterData, double>>();

        /// <summary>
        /// Gets a collection of registered performance counters.
        /// </summary>
        public IEnumerable<PerformanceCounterData> PerformanceCounters { get; } = Array.Empty<PerformanceCounterData>();

        /// <summary>
        /// Initializes a new instance of the <see cref="StandardPerfCollector"/> class.
        /// </summary>
        public StandardPerformanceCollectorStub()
        {
            PerformanceCollectorEventSource.Log.PerfCounterNetCoreOnlyOnAzureWebApp();
        }

        /// <summary>
        /// Performs collection for all registered counters.
        /// </summary>
        /// <param name="onReadingFailure">Invoked when an individual counter fails to be read.</param>
        public IEnumerable<Tuple<PerformanceCounterData, double>> Collect(Action<string, Exception> onReadingFailure = null)
        {
            return StandardPerformanceCollectorStub.emptyCollectResult;
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
        /// <param name="error">Captures the error logged.</param>
        /// <param name="blockCounterWithInstancePlaceHolder">Boolean that controls the registry of the counter based on the availability of instance place holder.</param>
        public void RegisterCounter(
            string perfCounterName,
            string reportAs,
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
