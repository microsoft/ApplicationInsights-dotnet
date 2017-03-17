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
    public class DependencyMetricExtractorTest
    {
        private const string TestMetricValueKey = "Test-MetricValue";
        private const string TestMetricName = "Test-Metric";


        [TestMethod]
        public void CanConstruct()
        {
            var extractor = new DependencyMetricExtractor(null);
            Assert.Equal("Microsoft.ApplicationInsights.Metrics.MetricIsAutocollected", extractor.MetricTelemetryMarkerKey);
            Assert.Equal(Boolean.TrueString, extractor.MetricTelemetryMarkerValue);
            Assert.Equal(DependencyMetricExtractor.MaxDependenctTypesToDiscoverDefault, extractor.MaxDependencyTypesToDiscover);
        }

        [TestMethod]
        public void TelemetryMarkedAsProcessedCorrectly()
        {
            List<ITelemetry> telemetrySentToChannel = new List<ITelemetry>();
            Func<ITelemetryProcessor, MetricExtractorTelemetryProcessorBase> extractorFactory = (nextProc) => new DependencyMetricExtractor(nextProc) { MaxDependencyTypesToDiscover = 0 };

            TelemetryConfiguration telemetryConfig = BaseTest.CreateTelemetryConfigWithExtractor(telemetrySentToChannel, extractorFactory);
            using (telemetryConfig)
            {
                TelemetryClient client = new TelemetryClient(telemetryConfig);
                client.TrackRequest("Test Request", DateTimeOffset.Now, TimeSpan.FromMilliseconds(10), "200", success: true);
                client.TrackDependency("Test Dependency Call 1", "Test Command", DateTimeOffset.Now, TimeSpan.FromMilliseconds(10), success: true);
                client.TrackDependency("Test Dependency Type", "Test Target", "Test Dependency Call 2", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(11), "201", success: true);
            }

            Assert.Equal(4, telemetrySentToChannel.Count);

            Assert.IsType(typeof(RequestTelemetry), telemetrySentToChannel[0]);
            Assert.Equal("Test Request", ((RequestTelemetry) telemetrySentToChannel[0]).Name);
            Assert.Equal(false, ((RequestTelemetry) telemetrySentToChannel[0]).Properties.ContainsKey("Microsoft.ApplicationInsights.Metrics.Extraction.ProcessedByExtractors"));

            Assert.IsType(typeof(DependencyTelemetry), telemetrySentToChannel[1]);
            Assert.Equal("Test Dependency Call 1", ((DependencyTelemetry) telemetrySentToChannel[1]).Name);
            Assert.Equal(true, ((DependencyTelemetry) telemetrySentToChannel[1]).Properties.ContainsKey("Microsoft.ApplicationInsights.Metrics.Extraction.ProcessedByExtractors"));
            Assert.Equal($"(Name:{typeof(DependencyMetricExtractor).FullName}, Ver:{"1.0"})",
                         ((DependencyTelemetry) telemetrySentToChannel[1]).Properties["Microsoft.ApplicationInsights.Metrics.Extraction.ProcessedByExtractors"]);

            Assert.IsType(typeof(DependencyTelemetry), telemetrySentToChannel[2]);
            Assert.Equal("Test Dependency Call 2", ((DependencyTelemetry) telemetrySentToChannel[2]).Name);
            Assert.Equal(true, ((DependencyTelemetry) telemetrySentToChannel[2]).Properties.ContainsKey("Microsoft.ApplicationInsights.Metrics.Extraction.ProcessedByExtractors"));
            Assert.Equal($"(Name:{typeof(DependencyMetricExtractor).FullName}, Ver:{"1.0"})",
                         ((DependencyTelemetry) telemetrySentToChannel[2]).Properties["Microsoft.ApplicationInsights.Metrics.Extraction.ProcessedByExtractors"]);

            Assert.IsType(typeof(MetricTelemetry), telemetrySentToChannel[3]);
        }

        [TestMethod]
        public void CanSetMaxDependencyTypesToDiscoverBeforeInitialization()
        {
            var extractor = new DependencyMetricExtractor(null);

            Assert.Equal(DependencyMetricExtractor.MaxDependenctTypesToDiscoverDefault, extractor.MaxDependencyTypesToDiscover);

            extractor.MaxDependencyTypesToDiscover = 1000;
            Assert.Equal(1000, extractor.MaxDependencyTypesToDiscover);

            extractor.MaxDependencyTypesToDiscover = 5;
            Assert.Equal(5, extractor.MaxDependencyTypesToDiscover);

            extractor.MaxDependencyTypesToDiscover = 1;
            Assert.Equal(1, extractor.MaxDependencyTypesToDiscover);

            extractor.MaxDependencyTypesToDiscover = 0;
            Assert.Equal(0, extractor.MaxDependencyTypesToDiscover);

            try
            {
                extractor.MaxDependencyTypesToDiscover = -1;
                Assert.True(false, "An ArgumentOutOfRangeException was expected");
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }

        [TestMethod]
        public void CanSetMaxDependencyTypesToDiscoverAfterInitialization()
        {
            DependencyMetricExtractor extractor = null;

            List<ITelemetry> telemetrySentToChannel = new List<ITelemetry>();
            Func<ITelemetryProcessor, MetricExtractorTelemetryProcessorBase> extractorFactory = (nextProc)
                                                                                                =>
                                                                                                {
                                                                                                    extractor = new DependencyMetricExtractor(nextProc)
                                                                                                            {
                                                                                                                MaxDependencyTypesToDiscover = 0
                                                                                                            };
                                                                                                    return extractor;
                                                                                                };

            TelemetryConfiguration telemetryConfig = BaseTest.CreateTelemetryConfigWithExtractor(telemetrySentToChannel, extractorFactory);
            using (telemetryConfig)
            {

                Assert.Equal(DependencyMetricExtractor.MaxDependenctTypesToDiscoverDefault, extractor.MaxDependencyTypesToDiscover);

                extractor.MaxDependencyTypesToDiscover = 1000;
                Assert.Equal(1000, extractor.MaxDependencyTypesToDiscover);

                extractor.MaxDependencyTypesToDiscover = 5;
                Assert.Equal(5, extractor.MaxDependencyTypesToDiscover);

                extractor.MaxDependencyTypesToDiscover = 1;
                Assert.Equal(1, extractor.MaxDependencyTypesToDiscover);

                extractor.MaxDependencyTypesToDiscover = 0;
                Assert.Equal(0, extractor.MaxDependencyTypesToDiscover);

                try
                {
                    extractor.MaxDependencyTypesToDiscover = -1;
                    Assert.True(false, "An ArgumentOutOfRangeException was expected");
                }
                catch (ArgumentOutOfRangeException)
                {
                }
            }
        }

        [TestMethod]
        public void CorrectlyExtractsMetricWhenGroupingByTypeDisabled()
        {
            List<ITelemetry> telemetrySentToChannel = new List<ITelemetry>();
            Func<ITelemetryProcessor, MetricExtractorTelemetryProcessorBase> extractorFactory = (nextProc) => new DependencyMetricExtractor(nextProc) { MaxDependencyTypesToDiscover = 0 };

            TelemetryConfiguration telemetryConfig = BaseTest.CreateTelemetryConfigWithExtractor(telemetrySentToChannel, extractorFactory);
            using (telemetryConfig)
            {
                TelemetryClient client = new TelemetryClient(telemetryConfig);

                client.TrackEvent("Test Event 1");

                client.TrackDependency("Test Dependency Type A", "Test Target 1", "Test Dependency Call 1", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(5), "201", success: true);
                client.TrackDependency("Test Dependency Type B", "Test Target 2", "Test Dependency Call 2", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(10), "202", success: true);
                client.TrackDependency("Test Dependency Type A", "Test Target 3", "Test Dependency Call 3", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(15), "203", success: true);
                client.TrackDependency(null,                     "Test Target 4", "Test Dependency Call 4", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(20), "204", success: true);

                client.TrackDependency("Test Dependency Type A", "Test Target 5", "Test Dependency Call 5", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(50), "501", success: false);
                client.TrackDependency("Test Dependency Type B", "Test Target 6", "Test Dependency Call 6", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(100), "502", success: false);
                client.TrackDependency("", "                      Test Target 7", "Test Dependency Call 7", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(150), "504", success: false);
            }

            Assert.Equal(10, telemetrySentToChannel.Count);

            Assert.NotNull(telemetrySentToChannel[8]);
            Assert.IsType(typeof(MetricTelemetry), telemetrySentToChannel[8]);
            MetricTelemetry metricT = (MetricTelemetry) telemetrySentToChannel[8];

            Assert.Equal("Dependency duration", metricT.Name);
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

            Assert.Equal("Dependency duration", metricF.Name);
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
        public void CorrectlyExtractsMetricWhenGroupingByTypeEnabled()
        {
            List<ITelemetry> telemetrySentToChannel = new List<ITelemetry>();
            Func<ITelemetryProcessor, MetricExtractorTelemetryProcessorBase> extractorFactory = (nextProc) => new DependencyMetricExtractor(nextProc) { MaxDependencyTypesToDiscover = 3 };

            TelemetryConfiguration telemetryConfig = BaseTest.CreateTelemetryConfigWithExtractor(telemetrySentToChannel, extractorFactory);
            using (telemetryConfig)
            {
                TelemetryClient client = new TelemetryClient(telemetryConfig);

                client.TrackEvent("Test Event");

                client.TrackDependency("Test Type A", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(5), "201", success: true);
                client.TrackDependency("Test Type A", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(10), "202", success: true);
                client.TrackDependency("Test Type A", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(15), "203", success: true);
                client.TrackDependency("Test Type A", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(20), "204", success: true);

                client.TrackDependency("Test Type B", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(105), "201", success: true);
                client.TrackDependency("Test Type B", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(110), "202", success: true);
                client.TrackDependency("Test Type B", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(115), "203", success: true);
                client.TrackDependency("Test Type B", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(120), "204", success: true);

                client.TrackDependency("", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(305), "201", success: true);
                client.TrackDependency(null, "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(310), "202", success: true);
                client.TrackDependency("Test Dependency Call", "Test Command Name", DateTimeOffset.Now, TimeSpan.FromMilliseconds(315), success: true);

                client.TrackDependency("Test Type A", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(1050), "501", success: false);
                client.TrackDependency("Test Type A", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(1100), "502", success: false);

                client.TrackEvent("Another Test Event");

                client.TrackDependency("Test Type C", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(2050), "501", success: false);
                client.TrackDependency("Test Type C", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(2100), "502", success: false);
                client.TrackDependency("Test Type C", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(2150), "502", success: false);

                client.TrackDependency("Test Type D", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(505), "201", success: true);
                client.TrackDependency("Test Type D", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(510), "202", success: true);

                client.TrackDependency("Test Type A", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(1050), "501", success: false);
                client.TrackDependency("Test Type A", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(1100), "502", success: false);

                client.TrackDependency("Test Type E", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(605), "201", success: true);

                client.TrackDependency("Test Type E", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(3010), "202", success: false);

                client.TrackDependency("", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(4005), "201", success: false);
                client.TrackDependency(null, "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(4010), "202", success: false);
                client.TrackDependency("Test Dependency Call", "Test Command Name", DateTimeOffset.Now, TimeSpan.FromMilliseconds(4015), success: false);
            }

            //// The following metric documents are expected:
            ////   - Format: number) Type, Success: Count
            //// 
            //// 1)  A, true: 4
            //// 2)  B, true: 4
            //// 3)  Unknown, true: 3
            //// 4)  A, false: 2
            //// 5)  C, false: 3
            //// 6)  D, true ==> Other, true: 2
            //// 4)* A, false: +2 = 4
            //// 6)* E, true ==> Other, true: +1 = 3
            //// 7)  E, false ==> Other, false: 1
            //// 8)  Unknown, false: 3


            Assert.Equal(27 + 8, telemetrySentToChannel.Count);
            for (int i = 0; i < 35; i++)
            {
                Assert.NotNull(telemetrySentToChannel[i]);
                if (i == 0 || i == 14)
                {
                    Assert.IsType(typeof(EventTelemetry), telemetrySentToChannel[i]);
                }
                else if (i <= 26)
                {
                    Assert.IsType(typeof(DependencyTelemetry), telemetrySentToChannel[i]);
                }
                else
                {
                    Assert.IsType(typeof(MetricTelemetry), telemetrySentToChannel[i]);
                    MetricTelemetry metric = (MetricTelemetry) telemetrySentToChannel[i];

                    Assert.Equal("Dependency duration", metric.Name);
                    Assert.NotNull(metric.Properties);
                    Assert.True(metric.Properties.ContainsKey("Type"));
                    Assert.True(metric.Properties.ContainsKey("IntervalDurationMs"));
                    Assert.True(metric.Properties.ContainsKey("Microsoft.ApplicationInsights.Metrics.MetricIsAutocollected"));
                    Assert.Equal("True", metric.Properties["Microsoft.ApplicationInsights.Metrics.MetricIsAutocollected"]);
                    Assert.True(metric.Properties.ContainsKey("Success"));
                    Assert.False(string.IsNullOrWhiteSpace(metric.Properties["Success"]));
                }
            }

            {
                IEnumerable<ITelemetry> metrics = telemetrySentToChannel.Where(
                            (t)
                            =>
                            {
                                IDictionary<string, string> p = (t as MetricTelemetry)?.Properties;
                                return (p != null) && "Test Type A".Equals(p["Type"]) && "True".Equals(p["Success"]);
                            });
                Assert.Equal(1, metrics.Count());
                MetricTelemetry metric = (MetricTelemetry) metrics.First();

                Assert.Equal(4, metric.Count);
                Assert.Equal(20, metric.Max);
                Assert.Equal(5, metric.Min);
                Assert.Equal(true, Math.Abs(metric.StandardDeviation.Value - 5.590169943749474) < 0.0000001);
                Assert.Equal(50, metric.Sum);
            }
        }

        [TestMethod]
        public void CorrectlyExtractsMetric()
        {
            List<ITelemetry> telemetrySentToChannel = new List<ITelemetry>();
            Func<ITelemetryProcessor, MetricExtractorTelemetryProcessorBase> extractorFactory = (nextProc) => new DependencyMetricExtractor(nextProc);

            TelemetryConfiguration telemetryConfig = BaseTest.CreateTelemetryConfigWithExtractor(telemetrySentToChannel, extractorFactory);
            using (telemetryConfig)
            {
                TelemetryClient client = new TelemetryClient(telemetryConfig);

                client.TrackEvent("Test Event 1");

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
                        extractor = new DependencyMetricExtractor(nextProc);
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
