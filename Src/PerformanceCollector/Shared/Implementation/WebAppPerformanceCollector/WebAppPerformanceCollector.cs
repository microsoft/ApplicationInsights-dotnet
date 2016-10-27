namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using DataContracts;
    using System.Text.RegularExpressions;

    internal class WebAppPerformanceCollector : IPerformanceCollector
    {
        private readonly List<PerformanceCounterData> performanceCounters = new List<PerformanceCounterData>();

        private static readonly Regex DisallowedCharsInReportAsRegex = new Regex(
            @"[^a-zA-Z()/\\_. \t-]+",
            RegexOptions.Compiled);

        private static readonly Regex MultipleSpacesRegex = new Regex(
            @"[  ]+",
            RegexOptions.Compiled);
        
        private CounterFactory factory;

        /// <summary>
        /// Dictionary to store the performance counter for each unique key - category name + counter name + instance name.
        /// </summary>
        private Dictionary<string, ICounterValue> dictionary = new Dictionary<string, ICounterValue>();
        
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
            this.factory = new CounterFactory();
        }

        /// <summary>
        /// Register a performance counter for collection.
        /// </summary>
        public void RegisterPerformanceCounter(string originalString, string reportAs, string categoryName, string counterName, string instanceName, bool usesInstanceNamePlaceholder, bool isCustomCounter)
        {
            ICounterValue counter = null;

            try
            {
                counter = this.factory.GetCounter(counterName, reportAs);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.WebAppPerformanceCounterRegistrationFailed,
                        counterName),
                    e);
            }
            bool firstReadOk = false;

            try
            {
                // perform the first read. For many counters the first read will always return 0
                // since a single sample is not enough to calculate a value
                var value = counter.GetValueAndReset();
            } 
            catch(Exception e)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.WebAppPerformanceCounterFirstReadFailed,
                        counterName),
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
                        categoryName,
                        counterName,
                        instanceName);

                string key = this.GenerateKeyForPerformanceCounter(perfData);
                if (this.dictionary.ContainsKey(key))
                {
                    this.dictionary.Remove(key);
                }

                this.dictionary.Add(key, counter);
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
                counter =>
                    {
                        float value;

                        try
                        {
                            value = CollectCounter(this.dictionary[this.GenerateKeyForPerformanceCounter(counter)]);
                        }
                        catch (InvalidOperationException e)
                        {
                            if (onReadingFailure != null)
                            {
                                // TODO: add logic to do after exception.
                                onReadingFailure(null, null);
                            }

                            return new Tuple<PerformanceCounterData, float>[] { };
                        }

                        return new[] { Tuple.Create(counter, value) };
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
                                     "{0}",
                                     pc.CounterName);

            var metricTelemetry = new MetricTelemetry(metricName, value);

            if (pc.InstanceName != null)
            {
                metricTelemetry.Properties.Add("CounterInstanceName", pc.InstanceName);
            }
            metricTelemetry.Properties.Add("CustomPerfCounter", "true");

            return metricTelemetry;
        }

        /// <summary>
        /// Refreshes counters.
        /// </summary>
        public void RefreshCounters()
        {
            var countersToRefresh =
                this.PerformanceCounters.Where(pc => pc.IsInBadState )
                    .ToList();

            countersToRefresh.ForEach(pcd => this.RefreshPerformanceCounter(pcd));

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
            error = null;

            try
            {
                string udpatedReportAs = this.SanitizeReportAs(reportAs, perfCounterName);
                udpatedReportAs = GetCounterReportAsName(perfCounterName, reportAs);
                this.RegisterPerformanceCounter(perfCounterName, udpatedReportAs, null, perfCounterName, null, false, false);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.WebAppPerformanceCounterReadFailed,
                        perfCounterName),
                    e);
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

        private string SanitizeReportAs(string reportAs, string performanceCounter)
        {
            // Strip off disallowed characters.
            var newReportAs = DisallowedCharsInReportAsRegex.Replace(reportAs, string.Empty);
            newReportAs = MultipleSpacesRegex.Replace(newReportAs, " ");
            newReportAs = newReportAs.Trim();

            // If nothing is left, use default performance counter name.
            if (string.IsNullOrWhiteSpace(newReportAs))
            {
                return performanceCounter;
            }

            return newReportAs;
        }
                
        /// <summary>
        /// Gets metric alias to be the value given by the user.
        /// </summary>
        /// <param name="counterName">Name of the counter to retrieve.</param>
        /// <param name="reportAs">Alias to report the counter.</param>
        /// <returns>Alias that will be used for the counter.</returns>
        private string GetCounterReportAsName(string counterName, string reportAs)
        {
            if (reportAs == null)
                return counterName;
            else
                return reportAs;
        }

        /// <summary>
        /// Generates Unique key for a specific performance counter data using the counter name.
        /// </summary>
        /// <param name="pcd">Target performance counter data.</param>
        /// <returns>Unique key for the specific counter data.</returns>
        private string GenerateKeyForPerformanceCounter(PerformanceCounterData pcd)
        {
            if (pcd != null)
            {
                return pcd.CounterName;
            }

            return string.Empty;
        }

        /// <summary>
        /// Collects a value for a single counter.
        /// </summary>D:\Git\dotnet-server\ApplicationInsights-dotnet-server\Src\PerformanceCollector\Shared\Implementation\PerformanceCollectorEventSource.cs
        private static float CollectCounter(ICounterValue counter)
        {
            try
            {
                return counter.GetValueAndReset();
            }
            catch (Exception e)
            {
                // TODO Logging
            }
            return 0;
        }
    }
}
