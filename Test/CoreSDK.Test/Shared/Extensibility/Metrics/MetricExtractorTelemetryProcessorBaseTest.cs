namespace Microsoft.ApplicationInsights.Extensibility.Metrics
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

    
    [TestClass]
    public class MetricExtractorTelemetryProcessorBaseTest
    {
        private const string SkipExtractionTestKey = "Test-SkipExctraction";
        private const string TestMetricValueKey = "Test-MetricValue";
        private const string TestMetricName = "Test-Metric";

        private class TestableMetricExtractorTelemetryProcessor : MetricExtractorTelemetryProcessorBase
        {
            private Metric testMetric = null;

            public TestableMetricExtractorTelemetryProcessor(ITelemetryProcessor nextProcessorInPipeline)
                : this(nextProcessorInPipeline, "Test-MetricTelemetryMarkerKey", "Test-MetricTelemetryMarkerValue")
            {
            }

            public TestableMetricExtractorTelemetryProcessor(ITelemetryProcessor nextProcessorInPipeline,
                                                             string metricTelemetryMarkerKey,
                                                             string metricTelemetryMarkerValue)
                : base(nextProcessorInPipeline, metricTelemetryMarkerKey, metricTelemetryMarkerValue)
            {
            }

            protected override string ExtractorVersion
            {
                get { return "Test Version"; }
            }

            public new MetricManager MetricManager
            {
                get { return base.MetricManager; }
            }

            public new string ExtractorName
            {
                get { return base.ExtractorName; }
            }

            public override void InitializeExtractor(TelemetryConfiguration configuration)
            {
                testMetric = this.MetricManager.CreateMetric(TestMetricName);
            }

            public override void ExtractMetrics(ITelemetry fromItem, out bool isItemProcessed)
            {
                if (ShouldSkipExtraction(fromItem))
                {
                    isItemProcessed = false;
                    return;
                }

                string metricValueString;
                bool hasMetricValue = fromItem.Context.Properties.TryGetValue(TestMetricValueKey, out metricValueString);

                if (hasMetricValue)
                {
                    double metricValue = 0;

                    try
                    {
                        metricValue = Double.Parse(metricValueString, CultureInfo.InvariantCulture);
                    }
                    catch { }

                    TrackMetric(metricValue);
                }

                isItemProcessed = true;
            }

            private void TrackMetric(double metricValue)
            {
                Metric metric = this.testMetric;
                if (metric == null)
                {
                    throw new NullReferenceException($"The metric handler '{nameof(this.testMetric)}' is not initialized."
                                                   + " Likely, InitializeExtractor(..) method was not invoked before attempting to extract metrics.");
                }

                metric.Track(metricValue);
            }

            private bool ShouldSkipExtraction(ITelemetry fromItem)
            {
                if (fromItem == null)
                {
                    return true;
                }

                bool skipExtraction = false;
                string skipExtractionFlag;
                if (fromItem.Context.Properties.TryGetValue(SkipExtractionTestKey, out skipExtractionFlag))
                {
                    try
                    {
                        skipExtraction = Boolean.Parse(skipExtractionFlag);
                    }
                    catch { }
                }

                return skipExtraction;
            }

        }  // class TestableMetricExtractorTelemetryProcessor

        [TestMethod]
        public void CanConstructTelemetryMarker()
        {
            var extractor = new TestableMetricExtractorTelemetryProcessor(null);
            Assert.Equal("Test-MetricTelemetryMarkerKey", extractor.MetricTelemetryMarkerKey);
            Assert.Equal("Test-MetricTelemetryMarkerValue", extractor.MetricTelemetryMarkerValue);
            Assert.Equal(typeof(TestableMetricExtractorTelemetryProcessor).FullName, extractor.ExtractorName);
        }

        [TestMethod]
        public void TelemetryMarkedAsProcessedCorrectly()
        {
            List<ITelemetry> telemetrySentToChannel = new List<ITelemetry>();
            Func<ITelemetryProcessor, MetricExtractorTelemetryProcessorBase> extractorFactory = (nextProc) => new TestableMetricExtractorTelemetryProcessor(nextProc);

            TelemetryConfiguration telemetryConfig = CreateTelemetryConfigWithExtractor(telemetrySentToChannel, extractorFactory);
            using (telemetryConfig)
            {
                TelemetryClient client = new TelemetryClient(telemetryConfig);
                client.TrackEvent("Test Event 1", new Dictionary<string, string>() { [SkipExtractionTestKey] = Boolean.FalseString });
                client.TrackEvent("Test Event 2", new Dictionary<string, string>() { [SkipExtractionTestKey] = Boolean.TrueString });
            }

            Assert.Equal(2, telemetrySentToChannel.Count);

            Assert.IsType(typeof(EventTelemetry), telemetrySentToChannel[0]);
            Assert.Equal("Test Event 1", ((EventTelemetry) telemetrySentToChannel[0]).Name);
            Assert.Equal(true, ((EventTelemetry) telemetrySentToChannel[0]).Properties.ContainsKey("Microsoft.ApplicationInsights.Metrics.Extraction.ProcessedByExtractors"));
            Assert.Equal($"(Name:{typeof(TestableMetricExtractorTelemetryProcessor).FullName}, Ver:{"Test Version"})",
                        ((EventTelemetry) telemetrySentToChannel[0]).Properties["Microsoft.ApplicationInsights.Metrics.Extraction.ProcessedByExtractors"]);

            Assert.IsType(typeof(EventTelemetry), telemetrySentToChannel[1]);
            Assert.Equal("Test Event 2", ((EventTelemetry) telemetrySentToChannel[1]).Name);
            Assert.Equal(false, ((EventTelemetry) telemetrySentToChannel[1]).Properties.ContainsKey("Microsoft.ApplicationInsights.Metrics.Extraction.ProcessedByExtractors"));
        }

        [TestMethod]
        public void CanInitializeMetricManager()
        {
            var extractor = new TestableMetricExtractorTelemetryProcessor(null);
            Assert.Equal(null, extractor.MetricManager);

            extractor.Initialize(null);
            Assert.NotNull(extractor.MetricManager);
        }

        [TestMethod]
        public void CorrectlyExtractsMetric()
        {
            List<ITelemetry> telemetrySentToChannel = new List<ITelemetry>();
            Func<ITelemetryProcessor, MetricExtractorTelemetryProcessorBase> extractorFactory = (nextProc) => new TestableMetricExtractorTelemetryProcessor(nextProc);

            TelemetryConfiguration telemetryConfig = CreateTelemetryConfigWithExtractor(telemetrySentToChannel, extractorFactory);
            using (telemetryConfig)
            {
                TelemetryClient client = new TelemetryClient(telemetryConfig);

                client.TrackEvent(
                        "Test Event 1",
                        new Dictionary<string, string>()
                        {
                            [SkipExtractionTestKey] = Boolean.FalseString,
                            [TestMetricValueKey] = 5.0.ToString()
                        });

                client.TrackEvent(
                        "Test Event 2",
                        new Dictionary<string, string>()
                        {
                            [SkipExtractionTestKey] = Boolean.FalseString,
                            [TestMetricValueKey] = 10.0.ToString()
                        });

                client.TrackEvent(
                        "Test Event X",
                        new Dictionary<string, string>()
                        {
                            [SkipExtractionTestKey] = Boolean.FalseString,
                            [TestMetricValueKey] = 15.0.ToString()
                        });

                client.TrackEvent(
                        "Test Event X",
                        new Dictionary<string, string>() {
                            [SkipExtractionTestKey] = Boolean.FalseString,
                            [TestMetricValueKey] = 20.0.ToString()
                        });
            }

            Assert.Equal(5, telemetrySentToChannel.Count);

            Assert.NotNull(telemetrySentToChannel[4]);
            Assert.IsType(typeof(MetricTelemetry), telemetrySentToChannel[4]);

            MetricTelemetry metric = (MetricTelemetry) telemetrySentToChannel[4];

            Assert.Equal(TestMetricName, metric.Name);
            Assert.Equal(4, metric.Count);
            Assert.Equal(20, metric.Max);
            Assert.Equal(5, metric.Min);

            //// Looks like MetricManager employes a biased method for variance computation. Unbiased would be 6.454972244.
            Assert.Equal(true, Math.Abs(metric.StandardDeviation.Value - 5.590169943749474) < 0.0000001);  
            Assert.Equal(50, metric.Sum);
        }

        [TestMethod]
        public void ExtractedMetricHasCorrectMarkerKeys()
        {
            {
                MetricTelemetry metric = ProduceOneMetricAggregation(metricTelemetryMarkerKey: "Foo", metricTelemetryMarkerValue: "Bar");

                Assert.Equal(3, metric.Properties.Count);
                Assert.True(metric.Properties.ContainsKey("IntervalDurationMs"));
                Assert.True(metric.Properties.ContainsKey("Microsoft.ApplicationInsights.Metrics.Extraction.ProcessedByExtractors"));

                Assert.True(metric.Properties.ContainsKey("Foo"));
                Assert.Equal("Bar", metric.Properties["Foo"]);
            }

            {
                MetricTelemetry metric = ProduceOneMetricAggregation(metricTelemetryMarkerKey: "Foo", metricTelemetryMarkerValue: "");

                Assert.Equal(3, metric.Properties.Count);
                Assert.True(metric.Properties.ContainsKey("IntervalDurationMs"));
                metricTelemetryMarkerValue: 
                Assert.True(metric.Properties.ContainsKey("Microsoft.ApplicationInsights.Metrics.Extraction.ProcessedByExtractors"));

                Assert.True(metric.Properties.ContainsKey("Foo"));
                Assert.Equal("", metric.Properties["Foo"]);
            }

            {
                MetricTelemetry metric = ProduceOneMetricAggregation(metricTelemetryMarkerKey: "Foo", metricTelemetryMarkerValue: null);

                Assert.Equal(3, metric.Properties.Count);
                Assert.True(metric.Properties.ContainsKey("IntervalDurationMs"));
                Assert.True(metric.Properties.ContainsKey("Microsoft.ApplicationInsights.Metrics.Extraction.ProcessedByExtractors"));

                Assert.True(metric.Properties.ContainsKey("Foo"));
                Assert.Equal("", metric.Properties["Foo"]);
            }

            {
                MetricTelemetry metric = ProduceOneMetricAggregation(metricTelemetryMarkerKey: null, metricTelemetryMarkerValue: "Bar");

                Assert.Equal(2, metric.Properties.Count);
                Assert.True(metric.Properties.ContainsKey("IntervalDurationMs"));
                Assert.True(metric.Properties.ContainsKey("Microsoft.ApplicationInsights.Metrics.Extraction.ProcessedByExtractors"));
            }

            {
                MetricTelemetry metric = ProduceOneMetricAggregation(metricTelemetryMarkerKey: "", metricTelemetryMarkerValue: "Bar");

                Assert.Equal(2, metric.Properties.Count);
                Assert.True(metric.Properties.ContainsKey("IntervalDurationMs"));
                Assert.True(metric.Properties.ContainsKey("Microsoft.ApplicationInsights.Metrics.Extraction.ProcessedByExtractors"));

                Assert.False(metric.Properties.ContainsKey(""));
            }

        }

        [TestMethod]
        public void DisposeIsIdempotent()
        {
            MetricExtractorTelemetryProcessorBase extractor = null;

            List<ITelemetry> telemetrySentToChannel = new List<ITelemetry>();
            Func<ITelemetryProcessor, MetricExtractorTelemetryProcessorBase> extractorFactory =
                    (nextProc) =>
                    {
                        extractor = new TestableMetricExtractorTelemetryProcessor(nextProc);
                        return extractor;
                    };

            TelemetryConfiguration telemetryConfig = CreateTelemetryConfigWithExtractor(telemetrySentToChannel, extractorFactory);
            using (telemetryConfig)
            {
                ;
            }

            extractor.Dispose();
            extractor.Dispose();
        }

        private static MetricTelemetry ProduceOneMetricAggregation(string metricTelemetryMarkerKey, string metricTelemetryMarkerValue)
        {
            List<ITelemetry> telemetrySentToChannel = new List<ITelemetry>();

            Func<ITelemetryProcessor, MetricExtractorTelemetryProcessorBase> extractorFactory;
            extractorFactory = (nextProc) => new TestableMetricExtractorTelemetryProcessor(nextProc, metricTelemetryMarkerKey, metricTelemetryMarkerValue);

            TelemetryConfiguration telemetryConfig = CreateTelemetryConfigWithExtractor(telemetrySentToChannel, extractorFactory);
            using (telemetryConfig)
            {
                TelemetryClient client = new TelemetryClient(telemetryConfig);

                client.TrackEvent(
                        "Test Event",
                        new Dictionary<string, string>() {
                            [SkipExtractionTestKey] = Boolean.FalseString,
                            [TestMetricValueKey] = 1.0.ToString()
                        });
            }

            Assert.Equal(2, telemetrySentToChannel.Count);

            Assert.NotNull(telemetrySentToChannel[1]);
            Assert.IsType(typeof(MetricTelemetry), telemetrySentToChannel[1]);

            MetricTelemetry metric = (MetricTelemetry) telemetrySentToChannel[1];
            return metric;
        }


        internal static TelemetryConfiguration CreateTelemetryConfigWithExtractor(IList<ITelemetry> telemetrySentToChannel,
                                                                                  Func<ITelemetryProcessor, MetricExtractorTelemetryProcessorBase> extractorFactory)
        {
            ITelemetryChannel channel = new StubTelemetryChannel { OnSend = (t) => telemetrySentToChannel.Add(t) };
            string iKey = Guid.NewGuid().ToString("D");
            TelemetryConfiguration telemetryConfig = new TelemetryConfiguration(iKey, channel);

            var channelBuilder = new TelemetryProcessorChainBuilder(telemetryConfig);
            channelBuilder.Use(extractorFactory);
            channelBuilder.Build();

            TelemetryProcessorChain processors = telemetryConfig.TelemetryProcessorChain;
            foreach (ITelemetryProcessor processor in processors.TelemetryProcessors)
            {
                ITelemetryModule m = processor as ITelemetryModule;
                if (m != null)
                {
                    m.Initialize(telemetryConfig);
                }
            }

            
            return telemetryConfig;
        }
    }
}
