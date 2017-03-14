namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using Assert = Xunit.Assert;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using BaseTest = MetricExtractorTelemetryProcessorBaseTest;

    [TestClass]
    public class RequestMetricExtractorTest
    {
        private const string SkipExtractionTestKey = "Test-SkipExctraction";
        private const string TestMetricValueKey = "Test-MetricValue";
        private const string TestMetricName = "Test-Metric";


        [TestMethod]
        public void CanConstruct()
        {
            var extractor = new RequestMetricExtractor(null);
            Assert.Equal("Microsoft.ApplicationInsights.Metrics.MetricIsAutocollected", extractor.MetricTelemetryMarkerKey);
            Assert.Equal(Boolean.TrueString, extractor.MetricTelemetryMarkerValue);
        }

        [TestMethod]
        public void TelemetryMarkedAsProcessedCorrectly()
        {
            List<ITelemetry> telemetrySentToChannel = new List<ITelemetry>();
            Func<ITelemetryProcessor, MetricExtractorTelemetryProcessorBase> extractorFactory = (nextProc) => new RequestMetricExtractor(nextProc);

            TelemetryConfiguration telemetryConfig = BaseTest.CreateTelemetryConfigWithExtractor(telemetrySentToChannel, extractorFactory);
            using (telemetryConfig)
            {
                TelemetryClient client = new TelemetryClient(telemetryConfig);
                client.TrackEvent("Test Event", new Dictionary<string, string>() { [SkipExtractionTestKey] = Boolean.FalseString });
                client.TrackRequest("Test Request 1", DateTimeOffset.Now, TimeSpan.FromMilliseconds(10), "200", success: true);
                client.TrackRequest("Test Request 2", DateTimeOffset.Now, TimeSpan.FromMilliseconds(11), "201", success: true);
            }

            Assert.Equal(4, telemetrySentToChannel.Count);

            Assert.IsType(typeof(EventTelemetry), telemetrySentToChannel[0]);
            Assert.Equal("Test Event", ((EventTelemetry) telemetrySentToChannel[0]).Name);
            Assert.Equal(false, ((EventTelemetry) telemetrySentToChannel[0]).Properties.ContainsKey("Microsoft.ApplicationInsights.Metrics.Extraction.ProcessedByExtractors"));

            Assert.IsType(typeof(RequestTelemetry), telemetrySentToChannel[1]);
            Assert.Equal("Test Request 1", ((RequestTelemetry) telemetrySentToChannel[1]).Name);
            Assert.Equal(true, ((RequestTelemetry) telemetrySentToChannel[1]).Properties.ContainsKey("Microsoft.ApplicationInsights.Metrics.Extraction.ProcessedByExtractors"));
            Assert.Equal($"(Name:{typeof(RequestMetricExtractor).FullName}, Ver:{"1.0"})",
                         ((RequestTelemetry) telemetrySentToChannel[1]).Properties["Microsoft.ApplicationInsights.Metrics.Extraction.ProcessedByExtractors"]);

            Assert.IsType(typeof(RequestTelemetry), telemetrySentToChannel[2]);
            Assert.Equal("Test Request 2", ((RequestTelemetry) telemetrySentToChannel[2]).Name);
            Assert.Equal(true, ((RequestTelemetry) telemetrySentToChannel[2]).Properties.ContainsKey("Microsoft.ApplicationInsights.Metrics.Extraction.ProcessedByExtractors"));
            Assert.Equal($"(Name:{typeof(RequestMetricExtractor).FullName}, Ver:{"1.0"})",
                         ((RequestTelemetry) telemetrySentToChannel[2]).Properties["Microsoft.ApplicationInsights.Metrics.Extraction.ProcessedByExtractors"]);

            Assert.IsType(typeof(MetricTelemetry), telemetrySentToChannel[3]);
        }

        [TestMethod]
        public void CorrectlyExtractsMetric()
        {
            List<ITelemetry> telemetrySentToChannel = new List<ITelemetry>();
            Func<ITelemetryProcessor, MetricExtractorTelemetryProcessorBase> extractorFactory = (nextProc) => new RequestMetricExtractor(nextProc);

            TelemetryConfiguration telemetryConfig = BaseTest.CreateTelemetryConfigWithExtractor(telemetrySentToChannel, extractorFactory);
            using (telemetryConfig)
            {
                TelemetryClient client = new TelemetryClient(telemetryConfig);

                client.TrackEvent(
                            "Test Event 1",
                            new Dictionary<string, string>() { [SkipExtractionTestKey] = Boolean.FalseString, [TestMetricValueKey] = 5.0.ToString() });

                client.TrackRequest("Test Request 1", DateTimeOffset.Now, TimeSpan.FromMilliseconds(5), "201", success: true);
                client.TrackRequest("Test Request 2", DateTimeOffset.Now, TimeSpan.FromMilliseconds(10), "202", success: true);
                client.TrackRequest("Test Request 3", DateTimeOffset.Now, TimeSpan.FromMilliseconds(15), "203", success: true);
                client.TrackRequest("Test Request 4", DateTimeOffset.Now, TimeSpan.FromMilliseconds(20), "204", success: true);

                client.TrackRequest("Test Request 1", DateTimeOffset.Now, TimeSpan.FromMilliseconds(50), "501", success: false);
                client.TrackRequest("Test Request 2", DateTimeOffset.Now, TimeSpan.FromMilliseconds(100), "502", success: false);
                client.TrackRequest("Test Request 3", DateTimeOffset.Now, TimeSpan.FromMilliseconds(150), "503", success: false);
            }

            Assert.Equal(10, telemetrySentToChannel.Count);

            Assert.NotNull(telemetrySentToChannel[8]);
            Assert.IsType(typeof(MetricTelemetry), telemetrySentToChannel[8]);
            MetricTelemetry metricT = (MetricTelemetry) telemetrySentToChannel[8];

            Assert.Equal("Server response time", metricT.Name);
            Assert.Equal(4, metricT.Count);
            Assert.Equal(20, metricT.Max);
            Assert.Equal(5, metricT.Min);
            Assert.Equal(true, Math.Abs(metricT.StandardDeviation.Value - 5.590169943749474) < 0.0000001);
            Assert.Equal(50, metricT.Sum);

            Assert.Equal(3, metricT.Properties.Count);
            Assert.True(metricT.Properties.ContainsKey("IntervalDurationMs"));
            Assert.True(metricT.Properties.ContainsKey("Microsoft.ApplicationInsights.Metrics.MetricIsAutocollected"));
            Assert.Equal("True", metricT.Properties["Microsoft.ApplicationInsights.Metrics.MetricIsAutocollected"]);
            Assert.Equal(true, metricT.Properties.ContainsKey("Success"));
            Assert.Equal(Boolean.TrueString, metricT.Properties["Success"]);

            Assert.NotNull(telemetrySentToChannel[9]);
            Assert.IsType(typeof(MetricTelemetry), telemetrySentToChannel[9]);
            MetricTelemetry metricF = (MetricTelemetry) telemetrySentToChannel[9];

            Assert.Equal("Server response time", metricF.Name);
            Assert.Equal(3, metricF.Count);
            Assert.Equal(150, metricF.Max);
            Assert.Equal(50, metricF.Min);
            Assert.Equal(true, Math.Abs(metricF.StandardDeviation.Value - 40.8248290) < 0.0000001);
            Assert.Equal(300, metricF.Sum);

            Assert.Equal(3, metricF.Properties.Count);
            Assert.True(metricF.Properties.ContainsKey("IntervalDurationMs"));
            Assert.True(metricF.Properties.ContainsKey("Microsoft.ApplicationInsights.Metrics.MetricIsAutocollected"));
            Assert.Equal("True", metricF.Properties["Microsoft.ApplicationInsights.Metrics.MetricIsAutocollected"]);
            Assert.Equal(true, metricF.Properties.ContainsKey("Success"));
            Assert.Equal(Boolean.FalseString, metricF.Properties["Success"]);
        }

        
        [TestMethod]
        public void DisposeIsIdempotent()
        {
            MetricExtractorTelemetryProcessorBase extractor = null;

            List<ITelemetry> telemetrySentToChannel = new List<ITelemetry>();
            Func<ITelemetryProcessor, MetricExtractorTelemetryProcessorBase> extractorFactory =
                    (nextProc) =>
                    {
                        extractor = new RequestMetricExtractor(nextProc);
                        return extractor;
                    };

            TelemetryConfiguration telemetryConfig = BaseTest.CreateTelemetryConfigWithExtractor(telemetrySentToChannel, extractorFactory);
            using (telemetryConfig)
            {
                ;
            }

            extractor.Dispose();
            extractor.Dispose();
        }
    }
}
