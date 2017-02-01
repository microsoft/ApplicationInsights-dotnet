namespace PerfCounterCollector.FunctionalTests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;

    using Functional.Helpers;
    using AI;
    using Microsoft.ManagementServices.RealTimeDataProcessing.QuickPulseService;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public class CommonTests
    {
        private const int TestListenerWaitTimeInMs = 35000;

        public static void DefaultCounterCollection(HttpListenerObservable listener)
        {
            var counterItems = listener.ReceiveItemsOfType<TelemetryItem<MetricData>>(10, TestListenerWaitTimeInMs);

            AssertCustomCounterReported(counterItems, "\\Memory\\Available Bytes");  
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

        public static void NonExistentCounterWhichUsesPlaceHolder(HttpListenerObservable listener)
        {
            var counterItems = listener.ReceiveItemsOfType<TelemetryItem<MetricData>>(10, TestListenerWaitTimeInMs);

            AssertCustomCounterReported(counterItems, @"Custom counter with placeholder - does not exist", false);
        }
        

        public static void NonParsableCounter(HttpListenerObservable listener)
        {
            var counterItems = listener.ReceiveItemsOfType<TelemetryItem<MetricData>>(10, TestListenerWaitTimeInMs);

            AssertCustomCounterReported(counterItems, @"Custom counter - will not parse", false);
        }

        internal static void QuickPulseAggregates(QuickPulseHttpListenerObservable listener, HttpClient client)
        {
            var samples = listener.ReceiveItems(15, TestListenerWaitTimeInMs).ToList();

            Assert.IsTrue(
                samples.TrueForAll(
                    item => item.InstrumentationKey == "fafa4b10-03d3-4bb0-98f4-364f0bdf5df8" && !string.IsNullOrWhiteSpace(item.Version)
                            && !string.IsNullOrWhiteSpace(item.Instance) && item.Metrics.Any()));

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

        internal static void QuickPulseDocuments(QuickPulseHttpListenerObservable listener, SingleWebHostTestBase test)
        {
            test.SendRequest("aspx/GenerateTelemetryItems.aspx", false);

            var samples = listener.ReceiveItems(15, TestListenerWaitTimeInMs).Where(s => s.Documents.Any()).ToList();

            Assert.IsTrue(
                samples.TrueForAll(
                    item =>
                    item.InstrumentationKey == "fafa4b10-03d3-4bb0-98f4-364f0bdf5df8" && !string.IsNullOrWhiteSpace(item.Version)
                    && !string.IsNullOrWhiteSpace(item.Instance)));

            // captured failed
            Assert.IsTrue(
                samples.Any(
                    s =>
                    s.Documents.Any(
                        d => d.DocumentType == TelemetryDocumentType.Request.ToString() && ((RequestTelemetryDocument)d).Name.Contains("Failed"))));

            Assert.IsTrue(
                samples.Any(
                    s =>
                    s.Documents.Any(
                        d =>
                        d.DocumentType == TelemetryDocumentType.RemoteDependency.ToString()
                        && ((DependencyTelemetryDocument)d).Name.Contains("Failed"))));

            Assert.IsTrue(
                samples.Any(
                    s =>
                    s.Documents.Any(
                        d =>
                        d.DocumentType == TelemetryDocumentType.Exception.ToString()
                        && ((ExceptionTelemetryDocument)d).Exception.Contains("ArgumentNullException"))));

            // did not capture succeeded
            Assert.IsFalse(
                samples.Any(
                    s =>
                    s.Documents.Any(
                        d => d.DocumentType == TelemetryDocumentType.Request.ToString() && ((RequestTelemetryDocument)d).Name.Contains("Success"))));

            Assert.IsFalse(
                samples.Any(
                    s =>
                    s.Documents.Any(
                        d =>
                        d.DocumentType == TelemetryDocumentType.RemoteDependency.ToString()
                        && ((DependencyTelemetryDocument)d).Name.Contains("Success"))));
        }

        internal static void QuickPulseTopCpuProcesses(QuickPulseHttpListenerObservable listener, SingleWebHostTestBase test)
        {
            test.SendRequest("aspx/GenerateTelemetryItems.aspx", false);

            var samples = listener.ReceiveItems(15, TestListenerWaitTimeInMs).Where(s => s.Documents.Any()).ToList();

            Assert.IsTrue(samples.Count > 0);

            Assert.IsTrue(samples.Any(s => s.TopCpuProcesses.Length > 0));
            Assert.IsFalse(
                samples.Any(s => s.TopCpuProcesses.Any(p => string.IsNullOrWhiteSpace(p.ProcessName) || p.CpuPercentage < 0 || p.CpuPercentage > 100)));
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

        private static void AssertCustomCounterReported(Envelope[] counterItems, string metricName, bool reported = true)
        {
            bool counterReported = counterItems.Any(
                item =>
                {
                    var metricData = item as TelemetryItem<MetricData>;

                    return metricData != null && metricData.data.baseData.metrics[0].name == metricName;
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
