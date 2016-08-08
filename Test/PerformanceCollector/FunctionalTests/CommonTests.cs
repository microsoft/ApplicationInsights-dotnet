namespace PerfCounterCollector.FunctionalTests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;

    using Functional.Helpers;
    using Microsoft.Developer.Analytics.DataCollection.Model.v2;
    using Microsoft.ManagementServices.RealTimeDataProcessing.QuickPulseService;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public class CommonTests
    {
        private const int TestListenerWaitTimeInMs = 35000;

        public static void DefaultCounterCollection(HttpListenerObservable listener)
        {
            var counterItems = listener.ReceiveItemsOfType<TelemetryItem<PerformanceCounterData>>(10, TestListenerWaitTimeInMs);

            AssertDefaultCounterReported(counterItems, "Memory", "Available Bytes");  
        }

        public static void CustomCounterCollection(HttpListenerObservable listener)
        {
            var counterItems = listener.ReceiveItemsOfType<TelemetryItem<MetricData>>(10, TestListenerWaitTimeInMs);

            AssertCustomCounterReported(counterItems, "Custom counter one");
            AssertCustomCounterReported(counterItems, "Custom counter two");
        }

        public static void NonExistentCounter(HttpListenerObservable listener)
        {
            var counterItems = listener.ReceiveItemsOfType<TelemetryItem<MetricData>>(10, TestListenerWaitTimeInMs);

            AssertCustomCounterReported(counterItems, @"Custom counter - does not exist", false);
        }

        public static void NonParsableCounter(HttpListenerObservable listener)
        {
            var counterItems = listener.ReceiveItemsOfType<TelemetryItem<MetricData>>(10, TestListenerWaitTimeInMs);

            AssertCustomCounterReported(counterItems, @"Custom counter - will not parse", false);
        }

        internal static void QuickPulseAggregates(QuickPulseHttpListenerObservable listener, HttpClient client)
        {
            var result = client.GetAsync(string.Empty).Result;

            var samples = listener.ReceiveItems(15, TestListenerWaitTimeInMs).ToList();

            Assert.IsTrue(
                samples.TrueForAll(
                    item =>
                        {
                            return item.InstrumentationKey == "fafa4b10-03d3-4bb0-98f4-364f0bdf5df8" && !string.IsNullOrWhiteSpace(item.Version)
                                   && !string.IsNullOrWhiteSpace(item.Instance) && item.Metrics.Any();
                        }));

            AssertSingleSampleWithNonZeroMetric(samples, @"\ApplicationInsights\Requests/Sec");
            AssertSingleSampleWithNonZeroMetric(samples, @"\ApplicationInsights\Request Duration");
            AssertSingleSampleWithNonZeroMetric(samples, @"\ApplicationInsights\Requests Failed/Sec");
            AssertNoSamplesWithNonZeroMetric(samples, @"\ApplicationInsights\Requests Succeeded/Sec");
            AssertNoSamplesWithNonZeroMetric(samples, @"\ApplicationInsights\Dependency Calls/Sec");
            AssertNoSamplesWithNonZeroMetric(samples, @"\ApplicationInsights\Dependency Call Duration");
            AssertNoSamplesWithNonZeroMetric(samples, @"\ApplicationInsights\Dependency Calls Failed/Sec");
            AssertNoSamplesWithNonZeroMetric(samples, @"\ApplicationInsights\Dependency Calls Succeeded/Sec");
            AssertSingleSampleWithNonZeroMetric(samples, @"\ApplicationInsights\Exceptions/Sec");

            AssertNoSamplesWithNonZeroMetric(samples, @"\ASP.NET Applications(__Total__)\Requests In Application Queue");
            AssertAllSamplesWithNonZeroMetric(samples, @"\Memory\Committed Bytes");
            AssertSomeSamplesWithNonZeroMetric(samples, @"\Processor(_Total)\% Processor Time");
        }

        private static void AssertSingleSampleWithNonZeroMetric(List<MonitoringDataPoint> samples, string metricName)
        {
            Assert.IsNotNull(samples.SingleOrDefault(item => item.Metrics.Any(m => m.Name == metricName && m.Value > 0)));
        }

        private static void AssertNoSamplesWithNonZeroMetric(List<MonitoringDataPoint> samples, string metricName)
        {
            Assert.IsFalse(samples.Any(item => item.Metrics.Any(m => m.Name == metricName && m.Value > 0)));
        }

        private static void AssertAllSamplesWithNonZeroMetric(List<MonitoringDataPoint> samples, string metricName)
        {
            Assert.IsTrue(samples.All(item => item.Metrics.Any(m => m.Name == metricName && m.Value > 0)));
        }

        private static void AssertSomeSamplesWithNonZeroMetric(List<MonitoringDataPoint> samples, string metricName)
        {
            Assert.IsTrue(samples.Any(item => item.Metrics.Any(m => m.Name == metricName && m.Value > 0)));
        }

        private static void AssertDefaultCounterReported(TelemetryItem[] counterItems, string categoryName, string counterName, bool reported = true)
        {
            bool counterReported = counterItems.Any(
                item =>
                {
                    var perfData = item as TelemetryItem<PerformanceCounterData>;

                    return perfData != null && perfData.Data.BaseData.CategoryName == categoryName
                           && perfData.Data.BaseData.CounterName == counterName;
                });

            if (reported)
            {
                Assert.IsTrue(counterReported);
            }
            else
            {
                Assert.IsFalse(counterReported);
            }
        }

        private static void AssertCustomCounterReported(TelemetryItem[] counterItems, string metricName, bool reported = true)
        {
            bool counterReported = counterItems.Any(
                item =>
                {
                    var metricData = item as TelemetryItem<MetricData>;

                    return metricData != null && metricData.Data.BaseData.Metrics[0].Name == metricName;
                });

            if (reported)
            {
                Assert.IsTrue(counterReported);
            }
            else
            {
                Assert.IsFalse(counterReported);
            }
        }
    }
}
