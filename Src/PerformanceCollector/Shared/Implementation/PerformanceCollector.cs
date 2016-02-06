namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;

    internal class PerformanceCollector : IPerformanceCollector
    {
        private readonly List<PerformanceCounterData> performanceCounters = new List<PerformanceCounterData>();

        /// <summary>
        /// Gets a collection of registered performance counters.
        /// </summary>
        public IEnumerable<PerformanceCounterData> PerformanceCounters => this.performanceCounters;

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
                this.performanceCounters.Add(
                    new PerformanceCounterData(
                        originalString,
                        reportAs,
                        performanceCounter,
                        usesInstanceNamePlaceholder,
                        isCustomCounter,
                        !firstReadOk));
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
                            value = CollectCounter(pc.PerformanceCounter);
                        }
                        catch (InvalidOperationException e)
                        {
                            if (onReadingFailure != null)
                            {
                                onReadingFailure(
                                    PerformanceCounterUtility.FormatPerformanceCounter(pc.PerformanceCounter),
                                    e);
                            }

                            return new Tuple<PerformanceCounterData, float>[] { };
                        }

                        return new[] { Tuple.Create(pc, value) };
                    });
        }

        /// <summary>
        /// Rebinds performance counters to Windows resources.
        /// </summary>
        public void RefreshPerformanceCounter(PerformanceCounterData pcd, PerformanceCounter pc)
        {
            this.performanceCounters.Remove(pcd);

            this.RegisterPerformanceCounter(
                pcd.OriginalString,
                pcd.ReportAs,
                pc.CategoryName,
                pc.CounterName,
                pc.InstanceName,
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
    }
}
