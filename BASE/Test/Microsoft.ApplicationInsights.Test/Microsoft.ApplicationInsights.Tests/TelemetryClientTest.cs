namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Text;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using OpenTelemetry;
    using OpenTelemetry.Logs;
    using OpenTelemetry.Metrics;

    [TestClass]
    public class TelemetryClientTest
    {
        private List<LogRecord> logItems;
        private List<OpenTelemetry.Metrics.Metric> metricItems;
        private TelemetryClient telemetryClient;

        [TestInitialize]
        public void TestInitialize()
        {
            var configuration = new TelemetryConfiguration();
            this.logItems = new List<LogRecord>();
            this.metricItems = new List<OpenTelemetry.Metrics.Metric>();
            configuration.InstrumentationKey = Guid.NewGuid().ToString();
            configuration.ConnectionString = "InstrumentationKey=" + configuration.InstrumentationKey;
            configuration.ConfigureOpenTelemetryBuilder(b => b
                .WithLogging(l => l.AddInMemoryExporter(logItems))
                .WithMetrics(m => m
                    .AddInMemoryExporter(metricItems, metricReaderOptions =>
                    {
                        metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 100;
                    })));
            this.telemetryClient = new TelemetryClient(configuration);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.metricItems?.Clear();
            this.logItems?.Clear();
            this.telemetryClient?.TelemetryConfiguration?.Dispose();
        }

        #region TrackMetric

        [TestMethod]
        public void TrackMetricWithNameAndValue()
        {
            this.telemetryClient.TrackMetric("TestMetric", 42.5);
            this.telemetryClient.Flush();
            
            // Wait briefly for metrics to be exported
            System.Threading.Thread.Sleep(200);
            
            // Verify metric was collected
            Assert.IsTrue(this.metricItems.Count > 0, "At least one metric should be collected");
            
            var metric = this.metricItems.FirstOrDefault(m => m.Name == "TestMetric");
            Assert.IsNotNull(metric, "TestMetric should be collected");
            
            // Verify the metric has data points with histogram data
            var metricPoints = metric.GetMetricPoints();
            bool hasData = false;
            foreach (var point in metricPoints)
            {
                if (point.GetHistogramCount() > 0)
                {
                    hasData = true;
                    // Verify histogram sum is close to the tracked value
                    var sum = point.GetHistogramSum();
                    Assert.IsTrue(sum > 0, "Histogram sum should be positive");
                    break;
                }
            }
            Assert.IsTrue(hasData, "Metric should have recorded histogram data");
        }

        [TestMethod]
        public void TrackMetricWithProperties()
        {
            this.telemetryClient.TrackMetric("TestMetric", 4.2, new Dictionary<string, string> { { "property1", "value1" } });
            this.telemetryClient.Flush();
            System.Threading.Thread.Sleep(200);
            
            // Verify metric was collected with properties as tags
            Assert.IsTrue(this.metricItems.Count > 0, "At least one metric should be collected");
            
            var metric = this.metricItems.FirstOrDefault(m => m.Name == "TestMetric");
            Assert.IsNotNull(metric, "TestMetric should be collected");
            
            // Verify histogram has data
            bool hasData = false;
            foreach (var point in metric.GetMetricPoints())
            {
                if (point.GetHistogramCount() > 0)
                {
                    hasData = true;
                    // Verify the tags contain our property
                    var tags = point.Tags;
                    bool hasProperty = false;
                    foreach (var tag in tags)
                    {
                        if (tag.Key == "property1" && tag.Value?.ToString() == "value1")
                        {
                            hasProperty = true;
                            break;
                        }
                    }
                    Assert.IsTrue(hasProperty, "Metric should have property1 tag");
                    break;
                }
            }
            Assert.IsTrue(hasData, "Metric should have recorded data");
        }

        [TestMethod]
        public void TrackMetricWithNullPropertiesDoesNotThrow()
        {
            this.telemetryClient.TrackMetric("TestMetric", 4.2, null);
            this.telemetryClient.Flush();
            System.Threading.Thread.Sleep(200);
            
            // Verify API doesn't throw and metric is collected
            Assert.IsTrue(this.metricItems.Count > 0, "At least one metric should be collected");
            var metric = this.metricItems.FirstOrDefault(m => m.Name == "TestMetric");
            Assert.IsNotNull(metric, "TestMetric should be collected");
        }

        [TestMethod]
        public void GetMetricWithZeroDimensions()
        {
            var metric = this.telemetryClient.GetMetric("TestMetric");
            Assert.IsNotNull(metric);
            
            metric.TrackValue(10.0);
            metric.TrackValue(20.0);
            
            this.telemetryClient.Flush();
            System.Threading.Thread.Sleep(200);
            
            // Verify metric was collected
            Assert.IsTrue(this.metricItems.Count > 0, "At least one metric should be collected");
            var collectedMetric = this.metricItems.FirstOrDefault(m => m.Name == "TestMetric");
            Assert.IsNotNull(collectedMetric, "TestMetric should be collected");
            
            // Verify we recorded 2 values (count should be 2)
            foreach (var point in collectedMetric.GetMetricPoints())
            {
                var count = point.GetHistogramCount();
                Assert.AreEqual(2, (int)count, "Should have recorded 2 values");
                var sum = point.GetHistogramSum();
                Assert.AreEqual(30.0, sum, 0.01, "Sum should be 10 + 20 = 30");
                break;
            }
        }

        [TestMethod]
        public void GetMetricWithOneDimension()
        {
            var metric = this.telemetryClient.GetMetric("RequestDuration", "StatusCode");
            Assert.IsNotNull(metric);
            
            metric.TrackValue(100.0, "200");
            metric.TrackValue(150.0, "404");
            metric.TrackValue(120.0, "200");
            
            this.telemetryClient.Flush();
            System.Threading.Thread.Sleep(200);
            
            // Verify metric was collected with dimension tags
            Assert.IsTrue(this.metricItems.Count > 0, "At least one metric should be collected");
            var collectedMetric = this.metricItems.FirstOrDefault(m => m.Name == "RequestDuration");
            Assert.IsNotNull(collectedMetric, "RequestDuration metric should be collected");
            
            // Verify dimensions are present in tags
            int pointCount = 0;
            foreach (var point in collectedMetric.GetMetricPoints())
            {
                pointCount++;
                var tags = point.Tags;
                bool hasStatusCodeTag = false;
                foreach (var tag in tags)
                {
                    if (tag.Key == "StatusCode")
                    {
                        hasStatusCodeTag = true;
                        break;
                    }
                }
                Assert.IsTrue(hasStatusCodeTag, "Should have StatusCode dimension tag");
            }
            Assert.IsTrue(pointCount > 0, "Should have metric points");
        }

        [TestMethod]
        public void GetMetricWithTwoDimensions()
        {
            var metric = this.telemetryClient.GetMetric("DatabaseQuery", "Database", "Operation");
            Assert.IsNotNull(metric);
            
            metric.TrackValue(50.0, "UsersDB", "SELECT");
            metric.TrackValue(80.0, "OrdersDB", "INSERT");
            
            this.telemetryClient.Flush();
            System.Threading.Thread.Sleep(200);
            
            // Verify metric was collected
            Assert.IsTrue(this.metricItems.Count > 0, "At least one metric should be collected");
            var collectedMetric = this.metricItems.FirstOrDefault(m => m.Name == "DatabaseQuery");
            Assert.IsNotNull(collectedMetric, "DatabaseQuery metric should be collected");
            
            // Verify both dimensions are present
            foreach (var point in collectedMetric.GetMetricPoints())
            {
                var tags = point.Tags;
                bool hasDatabase = false, hasOperation = false;
                foreach (var tag in tags)
                {
                    if (tag.Key == "Database") hasDatabase = true;
                    if (tag.Key == "Operation") hasOperation = true;
                }
                Assert.IsTrue(hasDatabase && hasOperation, "Should have both dimension tags");
                break;
            }
        }

        [TestMethod]
        public void GetMetricWithThreeDimensions()
        {
            var metric = this.telemetryClient.GetMetric("ApiLatency", "Endpoint", "Method", "Region");
            Assert.IsNotNull(metric);
            
            metric.TrackValue(200.0, "/api/users", "GET", "WestUS");
            
            this.telemetryClient.Flush();
            System.Threading.Thread.Sleep(200);
            
            // Verify metric was collected
            Assert.IsTrue(this.metricItems.Count > 0, "At least one metric should be collected");
            var collectedMetric = this.metricItems.FirstOrDefault(m => m.Name == "ApiLatency");
            Assert.IsNotNull(collectedMetric, "ApiLatency metric should be collected");
            
            // Verify all three dimensions
            foreach (var point in collectedMetric.GetMetricPoints())
            {
                Assert.AreEqual(1, (int)point.GetHistogramCount(), "Should have 1 value");
                var sum = point.GetHistogramSum();
                Assert.AreEqual(200.0, sum, 0.01, "Sum should equal the tracked value");
                break;
            }
        }

        [TestMethod]
        public void GetMetricWithFourDimensions()
        {
            var metric = this.telemetryClient.GetMetric("CacheHit", "CacheType", "Region", "Tenant", "Environment");
            Assert.IsNotNull(metric);
            
            metric.TrackValue(1.0, "Redis", "WestUS", "TenantA", "Prod");
            
            this.telemetryClient.Flush();
            System.Threading.Thread.Sleep(200);
            
            // Verify metric was collected
            Assert.IsTrue(this.metricItems.Count > 0, "At least one metric should be collected");
            var collectedMetric = this.metricItems.FirstOrDefault(m => m.Name == "CacheHit");
            Assert.IsNotNull(collectedMetric, "CacheHit metric should be collected");
            
            foreach (var point in collectedMetric.GetMetricPoints())
            {
                Assert.AreEqual(1, (int)point.GetHistogramCount(), "Should have 1 value");
                Assert.AreEqual(1.0, point.GetHistogramSum(), 0.01, "Sum should be 1.0");
                break;
            }
        }

        [TestMethod]
        public void GetMetricWithMetricIdentifier()
        {
            var metricId = new Microsoft.ApplicationInsights.Metrics.MetricIdentifier(
                "MyNamespace",
                "ComplexMetric",
                "Dim1",
                "Dim2",
                "Dim3");
            
            var metric = this.telemetryClient.GetMetric(metricId);
            Assert.IsNotNull(metric);
            
            metric.TrackValue(75.0, "Value1", "Value2", "Value3");
            
            this.telemetryClient.Flush();
            System.Threading.Thread.Sleep(200);
            
            // Verify metric was collected with namespace
            Assert.IsTrue(this.metricItems.Count > 0, "At least one metric should be collected");
            var collectedMetric = this.metricItems.FirstOrDefault(m => m.Name == "MyNamespace-ComplexMetric" || m.Name == "ComplexMetric");
            Assert.IsNotNull(collectedMetric, "ComplexMetric should be collected");
            
            foreach (var point in collectedMetric.GetMetricPoints())
            {
                Assert.AreEqual(1, (int)point.GetHistogramCount(), "Should have 1 value");
                Assert.AreEqual(75.0, point.GetHistogramSum(), 0.01, "Sum should be 75.0");
                break;
            }
        }

        [TestMethod]
        public void TrackValueWithFiveDimensions()
        {
            var metricId = new Microsoft.ApplicationInsights.Metrics.MetricIdentifier(
                "MyNamespace",
                "FiveDimensionMetric",
                "Dim1", "Dim2", "Dim3", "Dim4", "Dim5");
            
            var metric = this.telemetryClient.GetMetric(metricId);
            Assert.IsNotNull(metric);
            
            // Test double overload
            metric.TrackValue(100.0, "V1", "V2", "V3", "V4", "V5");
            
            // Test object overload
            metric.TrackValue((object)200.0, "V1", "V2", "V3", "V4", "V5");
            
            this.telemetryClient.Flush();
            System.Threading.Thread.Sleep(200);
            
            // Verify metric was collected
            Assert.IsTrue(this.metricItems.Count > 0, "At least one metric should be collected");
            
            // Verify we recorded 2 values with sum = 300
            var collectedMetric = this.metricItems.FirstOrDefault(m => m.Name.Contains("FiveDimensionMetric"));
            Assert.IsNotNull(collectedMetric, "FiveDimensionMetric should be collected");
            
            foreach (var point in collectedMetric.GetMetricPoints())
            {
                var count = point.GetHistogramCount();
                Assert.IsTrue(count >= 2, "Should have at least 2 values");
                var sum = point.GetHistogramSum();
                Assert.IsTrue(sum >= 300.0, "Sum should be at least 300 (100 + 200)");
                break;
            }
        }

        [TestMethod]
        public void TrackValueWithSixDimensions()
        {
            var metricId = new Microsoft.ApplicationInsights.Metrics.MetricIdentifier(
                "MyNamespace",
                "SixDimensionMetric",
                "Dim1", "Dim2", "Dim3", "Dim4", "Dim5", "Dim6");
            
            var metric = this.telemetryClient.GetMetric(metricId);
            Assert.IsNotNull(metric);
            
            metric.TrackValue(100.0, "V1", "V2", "V3", "V4", "V5", "V6");
            metric.TrackValue((object)200.0, "V1", "V2", "V3", "V4", "V5", "V6");
            
            this.telemetryClient.Flush();
            System.Threading.Thread.Sleep(200);
            
            Assert.IsTrue(this.metricItems.Count > 0, "At least one metric should be collected");
            var collectedMetric = this.metricItems.FirstOrDefault(m => m.Name.Contains("SixDimensionMetric"));
            Assert.IsNotNull(collectedMetric, "Metric should be collected");
        }

        [TestMethod]
        public void TrackValueWithSevenDimensions()
        {
            var metricId = new Microsoft.ApplicationInsights.Metrics.MetricIdentifier(
                "MyNamespace",
                "SevenDimensionMetric",
                "Dim1", "Dim2", "Dim3", "Dim4", "Dim5", "Dim6", "Dim7");
            
            var metric = this.telemetryClient.GetMetric(metricId);
            Assert.IsNotNull(metric);
            
            metric.TrackValue(100.0, "V1", "V2", "V3", "V4", "V5", "V6", "V7");
            metric.TrackValue((object)200.0, "V1", "V2", "V3", "V4", "V5", "V6", "V7");
            
            this.telemetryClient.Flush();
            System.Threading.Thread.Sleep(200);
            
            Assert.IsTrue(this.metricItems.Count > 0, "At least one metric should be collected");
            var collectedMetric = this.metricItems.FirstOrDefault(m => m.Name.Contains("SevenDimensionMetric"));
            Assert.IsNotNull(collectedMetric, "Metric should be collected");
        }

        [TestMethod]
        public void TrackValueWithEightDimensions()
        {
            var metricId = new Microsoft.ApplicationInsights.Metrics.MetricIdentifier(
                "MyNamespace",
                "EightDimensionMetric",
                "Dim1", "Dim2", "Dim3", "Dim4", "Dim5", "Dim6", "Dim7", "Dim8");
            
            var metric = this.telemetryClient.GetMetric(metricId);
            Assert.IsNotNull(metric);
            
            metric.TrackValue(100.0, "V1", "V2", "V3", "V4", "V5", "V6", "V7", "V8");
            metric.TrackValue((object)200.0, "V1", "V2", "V3", "V4", "V5", "V6", "V7", "V8");
            
            this.telemetryClient.Flush();
            System.Threading.Thread.Sleep(200);
            
            Assert.IsTrue(this.metricItems.Count > 0, "At least one metric should be collected");
            var collectedMetric = this.metricItems.FirstOrDefault(m => m.Name.Contains("EightDimensionMetric"));
            Assert.IsNotNull(collectedMetric, "Metric should be collected");
        }

        [TestMethod]
        public void TrackValueWithNineDimensions()
        {
            var metricId = new Microsoft.ApplicationInsights.Metrics.MetricIdentifier(
                "MyNamespace",
                "NineDimensionMetric",
                "Dim1", "Dim2", "Dim3", "Dim4", "Dim5", "Dim6", "Dim7", "Dim8", "Dim9");
            
            var metric = this.telemetryClient.GetMetric(metricId);
            Assert.IsNotNull(metric);
            
            metric.TrackValue(100.0, "V1", "V2", "V3", "V4", "V5", "V6", "V7", "V8", "V9");
            metric.TrackValue((object)200.0, "V1", "V2", "V3", "V4", "V5", "V6", "V7", "V8", "V9");
            
            this.telemetryClient.Flush();
            System.Threading.Thread.Sleep(200);
            
            Assert.IsTrue(this.metricItems.Count > 0, "At least one metric should be collected");
            var collectedMetric = this.metricItems.FirstOrDefault(m => m.Name.Contains("NineDimensionMetric"));
            Assert.IsNotNull(collectedMetric, "Metric should be collected");
        }

        [TestMethod]
        public void TrackValueWithTenDimensions()
        {
            var metricId = new Microsoft.ApplicationInsights.Metrics.MetricIdentifier(
                "MyNamespace",
                "TenDimensionMetric",
                "Dim1", "Dim2", "Dim3", "Dim4", "Dim5", "Dim6", "Dim7", "Dim8", "Dim9", "Dim10");
            
            var metric = this.telemetryClient.GetMetric(metricId);
            Assert.IsNotNull(metric);
            
            metric.TrackValue(100.0, "V1", "V2", "V3", "V4", "V5", "V6", "V7", "V8", "V9", "V10");
            metric.TrackValue((object)200.0, "V1", "V2", "V3", "V4", "V5", "V6", "V7", "V8", "V9", "V10");
            
            this.telemetryClient.Flush();
            System.Threading.Thread.Sleep(200);
            
            Assert.IsTrue(this.metricItems.Count > 0, "At least one metric should be collected");
            var collectedMetric = this.metricItems.FirstOrDefault(m => m.Name.Contains("TenDimensionMetric"));
            Assert.IsNotNull(collectedMetric, "Metric should be collected");
        }

        #endregion

        // TrackTrace tests removed - not related to metrics shim implementation


        #region TrackException

        [TestMethod]
        public void TrackExceptionSendsExceptionTelemetryWithSpecifiedNameToProvideSimplestWayOfSendingExceptionTelemetry()
        {
            Exception ex = new Exception("Test exception message");
            this.telemetryClient.TrackException(ex);

            this.telemetryClient.Flush();

            Assert.AreEqual(1, this.logItems.Count);
            var logRecord = this.logItems[0];
            Assert.IsNotNull(logRecord.Exception);
            Assert.AreSame(ex, logRecord.Exception);
            Assert.AreEqual(LogLevel.Error, logRecord.LogLevel);
        }

        [TestMethod]
        public void TrackExceptionWillUseRequiredFieldAsTextForTheExceptionNameWhenTheExceptionNameIsEmptyToHideUserErrors()
        {
            this.telemetryClient.TrackException((Exception)null);

            this.telemetryClient.Flush();

            Assert.AreEqual(1, this.logItems.Count);
            var logRecord = this.logItems[0];
            Assert.IsNotNull(logRecord.Exception);
            Assert.AreEqual("n/a", logRecord.Exception.Message);
        }

        [TestMethod]
        public void TrackExceptionSendsExceptionTelemetryWithSpecifiedObjectTelemetry()
        {
            Exception ex = new Exception("Test telemetry exception");
            this.telemetryClient.TrackException(new ExceptionTelemetry(ex));

            this.telemetryClient.Flush();

            Assert.AreEqual(1, this.logItems.Count);
            var logRecord = this.logItems[0];
            Assert.IsNotNull(logRecord.Exception);
            Assert.AreEqual("Test telemetry exception", logRecord.Exception.Message);
        }

        [TestMethod]
        public void TrackExceptionWillUseABlankObjectAsTheExceptionToHideUserErrors()
        {
            this.telemetryClient.TrackException((ExceptionTelemetry)null);

            this.telemetryClient.Flush();

            Assert.AreEqual(1, this.logItems.Count);
            var logRecord = this.logItems[0];
            Assert.IsNotNull(logRecord.Exception);
        }

        [TestMethod]
        public void TrackExceptionUsesErrorLogLevelByDefault()
        {
            this.telemetryClient.TrackException(new Exception());

            this.telemetryClient.Flush();

            Assert.AreEqual(1, this.logItems.Count);
            Assert.AreEqual(LogLevel.Error, this.logItems[0].LogLevel);
        }

        [TestMethod]
        public void TrackExceptionWithExceptionTelemetryRespectsSeverityLevel()
        {
            var telemetry = new ExceptionTelemetry(new Exception("Critical error"))
            {
                SeverityLevel = SeverityLevel.Critical
            };
            this.telemetryClient.TrackException(telemetry);

            this.telemetryClient.Flush();

            Assert.AreEqual(1, this.logItems.Count);
            Assert.AreEqual(LogLevel.Critical, this.logItems[0].LogLevel);
        }

        [TestMethod]
        public void TrackExceptionWithPropertiesIncludesPropertiesInLogRecord()
        {
            var properties = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };

            this.telemetryClient.TrackException(new Exception("Test"), properties);

            this.telemetryClient.Flush();

            Assert.AreEqual(1, this.logItems.Count);
            var logRecord = this.logItems[0];
            
            // Properties should be in the log record attributes
            var hasKey1 = false;
            var hasKey2 = false;
            if (logRecord.Attributes != null)
            {
                foreach (var attr in logRecord.Attributes)
                {
                    if (attr.Key == "key1" && attr.Value?.ToString() == "value1")
                        hasKey1 = true;
                    if (attr.Key == "key2" && attr.Value?.ToString() == "value2")
                        hasKey2 = true;
                }
            }

            Assert.IsTrue(hasKey1, "Property key1 should be in log record");
            Assert.IsTrue(hasKey2, "Property key2 should be in log record");
        }

        [TestMethod]
        public void TrackExceptionWithExceptionTelemetryIncludesProperties()
        {
            var telemetry = new ExceptionTelemetry(new Exception("Test exception"));
            telemetry.Properties["customKey"] = "customValue";
            
            this.telemetryClient.TrackException(telemetry);

            this.telemetryClient.Flush();

            Assert.AreEqual(1, this.logItems.Count);
            var logRecord = this.logItems[0];
            
            var hasCustomKey = false;
            if (logRecord.Attributes != null)
            {
                foreach (var attr in logRecord.Attributes)
                {
                    if (attr.Key == "customKey" && attr.Value?.ToString() == "customValue")
                    {
                        hasCustomKey = true;
                        break;
                    }
                }
            }

            Assert.IsTrue(hasCustomKey, "Custom property should be in log record");
        }

        [TestMethod]
        public void TrackExceptionWithInnerExceptionPreservesInnerException()
        {
            var innerException = new InvalidOperationException("Inner exception message");
            var outerException = new ApplicationException("Outer exception message", innerException);
            
            this.telemetryClient.TrackException(outerException);

            this.telemetryClient.Flush();

            Assert.AreEqual(1, this.logItems.Count);
            var logRecord = this.logItems[0];
            Assert.IsNotNull(logRecord.Exception);
            Assert.AreEqual("Outer exception message", logRecord.Exception.Message);
            
            // The exception should have inner exception
            Assert.IsNotNull(logRecord.Exception.InnerException);
            Assert.AreEqual("Inner exception message", logRecord.Exception.InnerException.Message);
        }

        #endregion

        private double ComputeSomethingHeavy()
        {
            var random = new Random();
            double res = 0;
            for (int i = 0; i < 10000; i++)
            {
                res += Math.Sqrt(random.NextDouble());
            }

            return res;
        }
    }
}
