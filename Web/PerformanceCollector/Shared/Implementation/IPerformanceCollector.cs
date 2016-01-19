namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    using PerformanceCounterReading = System.Tuple<PerformanceCounterData, float>;
    
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
        IEnumerable<PerformanceCounterReading> Collect(Action<string, Exception> onReadingFailure = null);

        /// <summary>
        /// Rebinds performance counters to Windows resources.
        /// </summary>
        /// <param name="pcd">Performance counter to refresh.</param>
        /// <param name="pc">Updated performance counter object.</param>
        void RefreshPerformanceCounter(PerformanceCounterData pcd, PerformanceCounter pc);
    }
}
