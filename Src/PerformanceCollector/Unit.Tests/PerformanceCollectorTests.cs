namespace Unit.Tests
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// PerformanceCollector tests.
    /// </summary>
    [TestClass]
    public class PerformanceCollectorTests
    {
        [TestMethod]
        [TestCategory("RequiresPerformanceCounters")]
        public void PerformanceCollectorSanityTest()
        {
            const int CounterCount = 3;
            const string CategoryName = "Processor";
            const string CounterName = "% Processor Time";
            const string InstanceName = "_Total";

            IPerformanceCollector collector = new StandardPerformanceCollector();

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

        [TestMethod]
        [TestCategory("RequiresPerformanceCounters")]
        public void PerformanceCollectorRefreshTest()
        {
            var counters = new PerformanceCounter[]
                               {
                                   new PerformanceCounter("Processor", "% Processor Time", "_Total"),
                                   new PerformanceCounter("Processor", "% Processor Time", "_Total") 
                               };

            var newCounter = new PerformanceCounterData("Available Bytes", "Available Bytes", false, false, false, "Memory", "Available Bytes", string.Empty);

            IPerformanceCollector collector = new StandardPerformanceCollector();

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

        [TestMethod]
        [TestCategory("RequiresPerformanceCounters")]
        public void PerformanceCollectorRefreshCountersTest()
        {
            var counters = new PerformanceCounter[]
                               {
                                   new PerformanceCounter("Processor", "% Processor Time", "_Total123blabla"),
                                   new PerformanceCounter("Processor", "% Processor Time", "_Total"),
                                   new PerformanceCounter("Processor", "% Processor Time", "_Total123afadfdsdf"), 
                               };

            IPerformanceCollector collector = new StandardPerformanceCollector();

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

            collector.RefreshCounters();
            
            //// All bad state counters are removed and added later through register counter, and as a result, the order of the performance coutners is changed.
            Assert.AreEqual(collector.PerformanceCounters.First().InstanceName, "_Total");
            Assert.AreEqual(collector.PerformanceCounters.Last().InstanceName, "_Total123afadfdsdf");
        }

        [TestMethod]
        [TestCategory("RequiresPerformanceCounters")]
        public void PerformanceCollectorBadStateTest()
        {
            var counters = new PerformanceCounter[]
                               {
                                   new PerformanceCounter("Processor", "% Processor Time", "_Total123blabla"),
                                   new PerformanceCounter("Processor", "% Processor Time", "_Total") 
                               };

            IPerformanceCollector collector = new StandardPerformanceCollector();

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