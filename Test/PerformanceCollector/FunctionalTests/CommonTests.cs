namespace PerfCounterCollector.FunctionalTests
{
    using System.Linq;
    using Functional.Helpers;
    using Microsoft.Developer.Analytics.DataCollection.Model.v2;
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
