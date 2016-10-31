namespace Unit.Tests
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// PerformanceCollector test base.
    /// </summary>
    public class PerformanceCollectorTestBase
    { 
        internal void PerformanceCollectorSanityTest(IPerformanceCollector collector)
        {
            const int CounterCount = 3;
            const string CategoryName = "Processor";
            const string CounterName = "% Processor Time";
            const string InstanceName = "_Total";

            for (int i = 0; i < CounterCount; i++)
            {
                string error = null;

                collector.RegisterCounter(
                    @"\Processor(_Total)\% Processor Time",
                    null,
                    true,
                    out error,
                    false);
            }

            var results = collector.Collect().ToList();

            Assert.AreEqual(CounterCount, results.Count);

            foreach (var result in results)
            {
                var value = result.Item2;

                Assert.AreEqual(CategoryName,  result.Item1.CategoryName);
                Assert.AreEqual(CounterName,  result.Item1.CounterName);
                Assert.AreEqual(InstanceName,  result.Item1.InstanceName);

                Assert.IsTrue(value >= 0 && value <= 100);
            }
        }

        internal void PerformanceCollectorRefreshTest(IPerformanceCollector collector)
        {
            var counters = new PerformanceCounter[]
                               {
                                   new PerformanceCounter("Processor", "% Processor Time", "_Total"),
                                   new PerformanceCounter("Processor", "% Processor Time", "_Total") 
                               };

            var newCounter = new PerformanceCounterData(@"\Memory\Committed Bytes", "Committed Bytes", false, false, false, "Memory", "Committed Bytes", string.Empty);

            foreach (var pc in counters)
            {
                string error = null;
                collector.RegisterCounter(
                    PerformanceCounterUtility.FormatPerformanceCounter(pc), 
                    null,
                    true,
                    out error,
                    false);
            }

            collector.RefreshPerformanceCounter(newCounter);

            Assert.IsTrue(collector.PerformanceCounters.Last().CategoryName == newCounter.CategoryName);
            Assert.IsTrue(collector.PerformanceCounters.Last().CounterName == newCounter.CounterName);
            Assert.IsTrue(collector.PerformanceCounters.Last().InstanceName == newCounter.InstanceName);
        }

        internal void PerformanceCollectorBadStateTest(IPerformanceCollector collector)
        {
            var counters = new PerformanceCounter[]
                               {
                                   new PerformanceCounter("Processor", "% Processor Time", "_Total123blabla"),
                                   new PerformanceCounter("Processor", "% Processor Time", "_Total") 
                               };

            foreach (var pc in counters)
            {
                try
                {
                    string error = null;
                    collector.RegisterCounter(
                        PerformanceCounterUtility.FormatPerformanceCounter(pc), 
                        null,
                        true,
                        out error,
                        false);
                }
                catch (Exception)
                {
                }
            }

            Assert.IsTrue(collector.PerformanceCounters.First().IsInBadState);
            Assert.IsFalse(collector.PerformanceCounters.Last().IsInBadState);
        }
    }
}