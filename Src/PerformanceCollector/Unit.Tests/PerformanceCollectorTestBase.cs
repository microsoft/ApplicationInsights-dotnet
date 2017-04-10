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
        internal void PerformanceCollectorSanityTest(IPerformanceCollector collector, string counter, string categoryName, string counterName, string instanceName)
        {
            const int CounterCount = 3;

            for (int i = 0; i < CounterCount; i++)
            {
                string error = null;

                collector.RegisterCounter(
                    counter,
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

                Assert.AreEqual(categoryName, result.Item1.PerformanceCounter.CategoryName);
                Assert.AreEqual(counterName, result.Item1.PerformanceCounter.CounterName);

                if (instanceName != null)
                {
                    Assert.AreEqual(instanceName, result.Item1.PerformanceCounter.InstanceName);
                }

                Assert.IsTrue(value >= 0 && value <= 100);
            }
        }

        internal void PerformanceCollectorRefreshCountersTest(IPerformanceCollector collector)
        {
            var counters = new PerformanceCounter[]
                               {
                                   new PerformanceCounter("Processor", "% Processor Time", "_Total123blabla"),
                                   new PerformanceCounter("Processor", "% Processor Time", "_Total"),
                                   new PerformanceCounter("Processor", "% Processor Time", "_Total123afadfdsdf"),
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

            collector.RefreshCounters();

            // All bad state counters are removed and added later through register counter, and as a result, the order of the performance coutners is changed.
            Assert.AreEqual(collector.PerformanceCounters.First().PerformanceCounter.InstanceName, "_Total");
            Assert.AreEqual(collector.PerformanceCounters.Last().PerformanceCounter.InstanceName, "_Total123afadfdsdf");
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

        internal void PerformanceCollectorNormalizedCpuTest(IPerformanceCollector collector)
        {
            string error = null;
            collector.RegisterCounter(
                    @"\Process(??APP_WIN32_PROC??)\% Processor Time Normalized",
                    null,
                    true,
                    out error,
                    false);

            collector.RegisterCounter(
                    @"\Process(??APP_WIN32_PROC??)\% Processor Time",
                    null,
                    true,
                    out error,
                    false);
                       
            var results = collector.Collect().ToList();
            Assert.AreEqual(2, results.Count);

            Assert.AreEqual("Process", results[0].Item1.PerformanceCounter.CategoryName);
            Assert.AreEqual("% Processor Time Normalized", results[0].Item1.PerformanceCounter.CounterName);

            Assert.AreEqual("Process", results[1].Item1.PerformanceCounter.CategoryName);
            Assert.AreEqual("% Processor Time", results[1].Item1.PerformanceCounter.CounterName);

            Assert.AreEqual(results[0].Item1.PerformanceCounter.InstanceName, results[1].Item1.PerformanceCounter.InstanceName);

            Assert.IsTrue(results[0].Item2 >= 0 && results[0].Item2 <= 100);
            Assert.IsTrue(Math.Abs(results[0].Item2 - (results[1].Item2 * Environment.ProcessorCount)) < 0.05);
        }
    }
}