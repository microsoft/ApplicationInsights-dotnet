namespace Unit.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation;

    using CounterData = System.Tuple<Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.PerformanceCounterData, System.Collections.Generic.List<float>>;

    /// <summary>
    /// Mock to test clients of PerformanceCollector.
    /// </summary>
    internal class PerformanceCollectorMock : IPerformanceCollector
    {
        public object Sync = new object();

        private readonly List<CounterData> counters = new List<CounterData>();

        public List<CounterData> Counters
        {
            get
            {
                lock (this.Sync)
                {
                    return this.counters;
                }
            }
        }

        public IEnumerable<PerformanceCounterData> PerformanceCounters
        {
            get
            {
                lock (this.Sync)
                {
                    return this.counters.Select(c => c.Item1);
                }
            }
        }

        public void RegisterPerformanceCounter(
            string originalString,
            string reportAs,
            string categoryName,
            string counterName,
            string instanceName,
            bool usesInstanceNamePlaceholder,
            bool isCustomCounter)
        {
            lock (this.Sync)
            {
                this.counters.Add(
                    Tuple.Create(
                        new PerformanceCounterData(
                            originalString,
                            reportAs,
                            new PerformanceCounter()
                                {
                                    CategoryName = categoryName,
                                    CounterName = counterName,
                                    InstanceName = instanceName
                                },
                            usesInstanceNamePlaceholder,
                            isCustomCounter),
                        new List<float>()));
            }
        }

        public IEnumerable<Tuple<PerformanceCounterData, float>> Collect(Action<string, Exception> onReadingFailure)
        {
            lock (this.Sync)
            {
                foreach (var counter in this.counters)
                {
                    var value =
                        (float)
                        (counter.Item1.PerformanceCounter.CategoryName.GetHashCode() + counter.Item1.PerformanceCounter.CounterName.GetHashCode()
                         + counter.Item1.PerformanceCounter.InstanceName.GetHashCode());

                    var result =
                        Tuple.Create(
                            new PerformanceCounterData(
                                counter.Item1.OriginalString,
                                counter.Item1.ReportAs,
                                new PerformanceCounter()
                                    {
                                        CategoryName = counter.Item1.PerformanceCounter.CategoryName,
                                        CounterName = counter.Item1.PerformanceCounter.CounterName,
                                        InstanceName = counter.Item1.PerformanceCounter.InstanceName
                                    },
                                counter.Item1.UsesInstanceNamePlaceholder,
                                counter.Item1.IsCustomCounter),
                            value);

                    counter.Item2.Add(value);

                    yield return result;
                }
            }
        }

        public void RefreshPerformanceCounter(PerformanceCounterData pcd, PerformanceCounter pc)
        {
        }
    }
}
