namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using DataContracts;

    internal class StandardPerformanceCollector : IPerformanceCollector
    {
        private readonly List<PerformanceCounterData> performanceCounters = new List<PerformanceCounterData>();

        /// <summary>
        /// Dictionary to store the performance counter for each unique key - category name + counter name + instance name.
        /// </summary>
        private Dictionary<string, PerformanceCounter> dictionary = new Dictionary<string, PerformanceCounter>();

        private IEnumerable<string> win32Instances;
        private IEnumerable<string> clrInstances;

        /// <summary>
        /// Gets a collection of registered performance counters.
        /// </summary>
        public IEnumerable<PerformanceCounterData> PerformanceCounters
        {
            get { return this.performanceCounters; }
        }

        /// <summary>
        /// Loads instances that are used in performance counter computation.
        /// </summary>
        public void LoadDependentInstances()
        {
            this.win32Instances = PerformanceCounterUtility.GetWin32ProcessInstances();
            this.clrInstances = PerformanceCounterUtility.GetClrProcessInstances();
        }

        /// <summary>
        /// Register a performance counter for collection.
        /// </summary>
        public void RegisterPerformanceCounter(string originalString, string reportAs, string categoryName, string counterName, string instanceName, bool usesInstanceNamePlaceholder, bool isCustomCounter)
        {
            PerformanceCounter performanceCounter = null;

            try
            {
                performanceCounter = new PerformanceCounter(categoryName, counterName, instanceName, true);
            }
            catch (Exception e)
            {
                // we want to have another crack at it if instance placeholder is used,
                // notably due to the fact that CLR process ID counter only starts returning values after the first garbage collection
                if (!usesInstanceNamePlaceholder)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            Resources.PerformanceCounterRegistrationFailed,
                            categoryName,
                            counterName,
                            instanceName),
                        e);
                }
            }

            bool firstReadOk = false;
            try
            {
                // perform the first read. For many counters the first read will always return 0
                // since a single sample is not enough to calculate a value
                performanceCounter.NextValue();

                firstReadOk = true;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.PerformanceCounterFirstReadFailed,
                        categoryName,
                        counterName,
                        instanceName),
                    e);
            }
            finally
            {
                PerformanceCounterData perfData = new PerformanceCounterData(
                        originalString,
                        reportAs,
                        usesInstanceNamePlaceholder,
                        isCustomCounter,
                        !firstReadOk,
                        performanceCounter.CategoryName,
                        performanceCounter.CounterName,
                        performanceCounter.InstanceName);

                string key = this.GenerateKeyForPerformanceCounter(perfData);
                if (this.dictionary.ContainsKey(key))
                {
                    this.dictionary.Remove(key);
                }

                this.dictionary.Add(key, performanceCounter);
                this.performanceCounters.Add(perfData);
            }
        }
        
        /// <summary>
        /// Performs collection for all registered counters.
        /// </summary>
        /// <param name="onReadingFailure">Invoked when an individual counter fails to be read.</param>
        public IEnumerable<Tuple<PerformanceCounterData, float>> Collect(
            Action<string, Exception> onReadingFailure = null)
        {
            return this.performanceCounters.Where(pc => !pc.IsInBadState).SelectMany(
                pc =>
                    {
                        float value;

                        try
                        {
                            value = CollectCounter(this.dictionary[this.GenerateKeyForPerformanceCounter(pc)]);
                        }
                        catch (InvalidOperationException e)
                        {
                            if (onReadingFailure != null)
                            {
                                onReadingFailure(
                                    PerformanceCounterUtility.FormatPerformanceCounter(this.dictionary[this.GenerateKeyForPerformanceCounter(pc)]),
                                    e);
                            }

                            return new Tuple<PerformanceCounterData, float>[] { };
                        }

                        return new[] { Tuple.Create(pc, value) };
                    });
        }

        /// <summary>
        /// Creates a metric telemetry associated with the PerformanceCounterData, with the respective float value.
        /// </summary>
        /// <param name="pc">PerformanceCounterData for which we are generating the telemetry.</param>
        /// <param name="value">The metric value for the respective performance counter data.</param>
        /// <returns>Metric telemetry object associated with the specific counter.</returns>
        public MetricTelemetry CreateTelemetry(PerformanceCounterData pc, float value)
        {
            var metricName = !string.IsNullOrWhiteSpace(pc.ReportAs)
                                 ? pc.ReportAs
                                 : string.Format(
                                     CultureInfo.InvariantCulture,
                                     "{0} - {1}",
                                     pc.CategoryName,
                                     pc.CounterName);

            var metricTelemetry = new MetricTelemetry(metricName, value);

            metricTelemetry.Properties.Add("CounterInstanceName", pc.InstanceName);
            metricTelemetry.Properties.Add("CustomPerfCounter", "true");

            return metricTelemetry;
        }

        /// <summary>
        /// Refreshes counters.
        /// </summary>
        public void RefreshCounters()
        {
            // we need to refresh counters in bad state and counters with placeholders in instance names
            var countersToRefresh =
                this.PerformanceCounters.Where(pc => pc.IsInBadState || pc.UsesInstanceNamePlaceholder)
                    .ToList();

            countersToRefresh.ForEach(pcd => this.RefreshCounter(pcd));

            PerformanceCollectorEventSource.Log.CountersRefreshedEvent(countersToRefresh.Count.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Registers a counter using the counter name and reportAs value to the total list of counters.
        /// </summary>
        /// <param name="perfCounterName">Name of the performance counter.</param>
        /// <param name="reportAs">Report as name for the performance counter.</param>
        /// <param name="isCustomCounter">Boolean to check if the performance counter is custom defined.</param>
        /// <param name="error">Captures the error logged.</param>
        public void RegisterCounter(
            string perfCounterName,
            string reportAs,
            bool isCustomCounter,
            out string error)
        {
            bool usesInstanceNamePlaceholder;
            var pc = this.CreateAndValidateCounter(
                perfCounterName,
                out usesInstanceNamePlaceholder,
                out error);

            if (pc != null)
            {
                this.RegisterCounter(perfCounterName, reportAs, pc, isCustomCounter, usesInstanceNamePlaceholder, out error);
            }
        }

        /// <summary>
        /// Rebinds performance counters to Windows resources.
        /// </summary>
        public void RefreshPerformanceCounter(PerformanceCounterData pcd)
        {
            this.performanceCounters.Remove(pcd);
            if (this.dictionary.ContainsKey(this.GenerateKeyForPerformanceCounter(pcd)))
            {
                this.dictionary.Remove(this.GenerateKeyForPerformanceCounter(pcd));
            }

            this.RegisterPerformanceCounter(
                pcd.OriginalString,
                pcd.ReportAs,
                pcd.CategoryName,
                pcd.CounterName,
                pcd.InstanceName,
                pcd.UsesInstanceNamePlaceholder,
                pcd.IsCustomCounter);
        }
        
        /// <summary>
        /// Collects a value for a single counter.
        /// </summary>
        private static float CollectCounter(PerformanceCounter pc)
        {
            try
            {
                return pc.NextValue();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.PerformanceCounterReadFailed,
                        PerformanceCounterUtility.FormatPerformanceCounter(pc)),
                    e);
            }
        }

        /// <summary>
        /// Validates the counter by parsing.
        /// </summary>
        /// <param name="perfCounterName">Performance counter name to validate.</param>
        /// <param name="usesInstanceNamePlaceholder">Boolean to check if it is using an instance name place holder.</param>
        /// <param name="error">Error message.</param>
        /// <returns>Performance counter.</returns>
        private PerformanceCounter CreateAndValidateCounter(
            string perfCounterName,
            out bool usesInstanceNamePlaceholder,
            out string error)
        {
            error = null;

            try
            {
                return PerformanceCounterUtility.ParsePerformanceCounter(
                    perfCounterName,
                    this.win32Instances,
                    this.clrInstances,
                    out usesInstanceNamePlaceholder);
            }
            catch (Exception e)
            {
                usesInstanceNamePlaceholder = false;
                PerformanceCollectorEventSource.Log.CounterParsingFailedEvent(e.Message, perfCounterName);
                error = e.Message;

                return null;
            }
        }

        /// <summary>
        /// Refreshes the counter associated with a specific performance counter data.
        /// </summary>
        /// <param name="pcd">Target performance counter data to refresh.</param>
        private void RefreshCounter(PerformanceCounterData pcd)
        {
            string dummy;

            bool usesInstanceNamePlaceholder;
            var pc = this.CreateAndValidateCounter(
                pcd.OriginalString,
                out usesInstanceNamePlaceholder,
                out dummy);

            try
            {
                this.RefreshPerformanceCounter(pcd);

                PerformanceCollectorEventSource.Log.CounterRegisteredEvent(
                        PerformanceCounterUtility.FormatPerformanceCounter(pc));
            }
            catch (InvalidOperationException e)
            {
                PerformanceCollectorEventSource.Log.CounterRegistrationFailedEvent(
                    e.Message,
                    PerformanceCounterUtility.FormatPerformanceCounter(pc));
            }
        }

        /// <summary>
        /// Registers the counter to the existing list of counters.
        /// </summary>
        /// <param name="originalString">Counter original string.</param>
        /// <param name="reportAs">Counter report as.</param>
        /// <param name="pc">Performance counter.</param>
        /// <param name="isCustomCounter">Boolean to check if it is a custom counter.</param>
        /// <param name="usesInstanceNamePlaceholder">Uses Instance Name Place holder boolean.</param>
        /// <param name="error">Error message.</param>
        private void RegisterCounter(
            string originalString,
            string reportAs,
            PerformanceCounter pc,
            bool isCustomCounter,
            bool usesInstanceNamePlaceholder,
            out string error)
        {
            error = null;

            try
            {
                this.RegisterPerformanceCounter(
                    originalString,
                    reportAs,
                    pc.CategoryName,
                    pc.CounterName,
                    pc.InstanceName,
                    usesInstanceNamePlaceholder,
                    isCustomCounter);

                PerformanceCollectorEventSource.Log.CounterRegisteredEvent(
                    PerformanceCounterUtility.FormatPerformanceCounter(pc));
            }
            catch (InvalidOperationException e)
            {
                PerformanceCollectorEventSource.Log.CounterRegistrationFailedEvent(
                    e.Message,
                    PerformanceCounterUtility.FormatPerformanceCounter(pc));
                error = e.Message;
            }
        }

        /// <summary>
        /// Generates Unique key for a specific performance counter data using the category name, counter name and instance name.
        /// </summary>
        /// <param name="pcd">Target performance counter data.</param>
        /// <returns>Unique key for the specific counter data.</returns>
        private string GenerateKeyForPerformanceCounter(PerformanceCounterData pcd)
        {
            if (pcd != null)
            {
                return pcd.CategoryName + pcd.CounterName + pcd.InstanceName;
            }

            return string.Empty;
        }
    }
}
