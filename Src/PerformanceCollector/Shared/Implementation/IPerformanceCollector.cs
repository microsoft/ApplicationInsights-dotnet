namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation
{
    using System;
    using System.Collections.Generic;
    using DataContracts;

    internal interface IPerformanceCollector
    {
        /// <summary>
        /// Gets a collection of counters that are currently registered with the collector.
        /// </summary>
        IEnumerable<PerformanceCounterData> PerformanceCounters { get; }

        /// <summary>
        /// Register a performance counter for collection.
        /// </summary>
        /// <param name="originalString">Original string definition of the counter.</param>
        /// <param name="reportAs">Alias to report the counter as.</param>
        /// <param name="categoryName">Category name.</param>
        /// <param name="counterName">Counter name.</param>
        /// <param name="instanceName">Instance name.</param>
        /// <param name="usesInstanceNamePlaceholder">Indicates whether the counter uses a placeholder in the instance name.</param>
        /// <param name="isCustomCounter">Indicates whether the counter is a custom counter.</param>
        void RegisterPerformanceCounter(string originalString, string reportAs, string categoryName, string counterName, string instanceName, bool usesInstanceNamePlaceholder, bool isCustomCounter);
        
        /// <summary>
        /// Performs collection for all registered counters.
        /// </summary>
        /// <param name="onReadingFailure">Invoked when an individual counter fails to be read.</param>
        IEnumerable<Tuple<PerformanceCounterData, float>> Collect(Action<string, Exception> onReadingFailure = null);

        /// <summary>
        /// Refreshes and rebinds all the set of counters that are intended to be collected.
        /// </summary>
        void RefreshCounters();

        /// <summary>
        /// Loads instances that are used in performance counter computation - example: WIN32 and CLR instances.
        /// </summary>
        void LoadDependentInstances();

        /// <summary>
        /// Registers a counter using the counter name and reportAs value to the total list of counters.
        /// </summary>
        /// <param name="perfCounterName">Name of the performance counter.</param>
        /// <param name="reportAs">Report as name for the performance counter.</param>
        /// <param name="isCustomCounter">Boolean to check if the performance counter is custom defined.</param>
        /// <param name="error">Captures the error logged.</param>
        void RegisterCounter(string perfCounterName, string reportAs, bool isCustomCounter, out string error);

        /// <summary>
        /// Creates a metric telemetry associated with the PerformanceCounterData, with the respective float value.
        /// </summary>
        /// <param name="perfData">PerformanceCounterData for which we are generating the telemetry.</param>
        /// <param name="value">The metric value for the respective performance counter data.</param>
        /// <returns>Metric Telemetry object associated with the specific counter.</returns>
        MetricTelemetry CreateTelemetry(PerformanceCounterData perfData, float value);

        /// <summary>
        /// Rebinds performance counters to Windows resources.
        /// </summary>
        /// <param name="pcd">Performance counter to refresh.</param> 
        void RefreshPerformanceCounter(PerformanceCounterData pcd);
    }
}
