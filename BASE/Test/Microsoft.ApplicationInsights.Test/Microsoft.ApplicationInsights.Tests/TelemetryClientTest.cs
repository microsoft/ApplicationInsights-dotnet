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
    using OpenTelemetry.Trace;

    [TestClass]
    public class TelemetryClientTest
    {
        private List<LogRecord> logItems;
        private List<OpenTelemetry.Metrics.Metric> metricItems;
        private List<Activity> activityItems;
        private TelemetryClient telemetryClient;

        [TestInitialize]
        public void TestInitialize()
        {
            var configuration = new TelemetryConfiguration();
            this.logItems = new List<LogRecord>();
            this.metricItems = new List<OpenTelemetry.Metrics.Metric>();
            this.activityItems = new List<Activity>();
            configuration.InstrumentationKey = Guid.NewGuid().ToString();
            configuration.ConnectionString = "InstrumentationKey=" + configuration.InstrumentationKey;
            configuration.ConfigureOpenTelemetryBuilder(b => b
                .WithLogging(l => l.AddInMemoryExporter(logItems))
                .WithMetrics(m => m.AddInMemoryExporter(metricItems))
                .WithTracing(t => t.AddInMemoryExporter(activityItems)));
            this.telemetryClient = new TelemetryClient(configuration);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.activityItems?.Clear();
            this.metricItems?.Clear();
            this.logItems?.Clear();
            this.telemetryClient?.TelemetryConfiguration?.Dispose();
        }

        #region TrackEvent

        [TestMethod]
        public void TrackEventSendsEventTelemetryWithSpecifiedNameToProvideSimplestWayOfSendingEventTelemetry()
        {
            this.telemetryClient.TrackEvent("TestEvent");
            this.telemetryClient.Flush();
            
            Assert.IsTrue(this.logItems.Count > 0, "At least one log should be collected");
            var logRecord = this.logItems.FirstOrDefault(l => 
                l.Attributes != null && l.Attributes.Any(a => 
                    a.Key == "microsoft.custom_event.name" && a.Value?.ToString() == "TestEvent"));
            Assert.IsNotNull(logRecord, "TestEvent should be collected");
            Assert.AreEqual(LogLevel.Information, logRecord.LogLevel);
        }

        [TestMethod]
        public void TrackEventSendsEventTelemetryWithSpecifiedObjectTelemetry()
        {
            this.telemetryClient.TrackEvent(new EventTelemetry("TestEvent"));
            this.telemetryClient.Flush();
            
            Assert.IsTrue(this.logItems.Count > 0, "At least one log should be collected");
            var logRecord = this.logItems.FirstOrDefault(l => 
                l.Attributes != null && l.Attributes.Any(a => 
                    a.Key == "microsoft.custom_event.name" && a.Value?.ToString() == "TestEvent"));
            Assert.IsNotNull(logRecord, "TestEvent should be collected");
        }

        [TestMethod]
        public void TrackEventWillSendPropertiesIfProvidedInline()
        {
            this.telemetryClient.TrackEvent("Test", new Dictionary<string, string> { { "blah", "yoyo" } });
            this.telemetryClient.Flush();
            
            Assert.IsTrue(this.logItems.Count > 0, "At least one log should be collected");
            var logRecord = this.logItems.FirstOrDefault(l => 
                l.Attributes != null && l.Attributes.Any(a => 
                    a.Key == "microsoft.custom_event.name" && a.Value?.ToString() == "Test"));
            Assert.IsNotNull(logRecord, "Test event should be collected");
            
            // Verify property is in attributes
            bool hasBlahProperty = false;
            if (logRecord.Attributes != null)
            {
                foreach (var attr in logRecord.Attributes)
                {
                    if (attr.Key == "blah" && attr.Value?.ToString() == "yoyo")
                    {
                        hasBlahProperty = true;
                        break;
                    }
                }
            }
            Assert.IsTrue(hasBlahProperty, "Property 'blah' should be 'yoyo'");
        }

        [TestMethod]
        public void TrackEventWithEventTelemetryAndProperties()
        {
            // Test EventTelemetry with properties set via Properties dictionary
            var eventTelemetry = new EventTelemetry("TestEventWithProps");
            eventTelemetry.Properties["customProp"] = "customValue";
            eventTelemetry.Properties["environment"] = "test";
            
            this.telemetryClient.TrackEvent(eventTelemetry);
            this.telemetryClient.Flush();
            
            Assert.IsTrue(this.logItems.Count > 0, "At least one log should be collected");
            var logRecord = this.logItems.FirstOrDefault(l => 
                l.Attributes != null && l.Attributes.Any(a => 
                    a.Key == "microsoft.custom_event.name" && a.Value?.ToString() == "TestEventWithProps"));
            Assert.IsNotNull(logRecord, "TestEventWithProps should be collected");
            Assert.IsNotNull(logRecord.Attributes, "LogRecord should have attributes");
            
            // Verify properties are in attributes
            bool hasCustomProp = false, hasEnvironment = false;
            foreach (var attr in logRecord.Attributes)
            {
                if (attr.Key == "customProp" && attr.Value?.ToString() == "customValue")
                    hasCustomProp = true;
                if (attr.Key == "environment" && attr.Value?.ToString() == "test")
                    hasEnvironment = true;
            }
            Assert.IsTrue(hasCustomProp, "Property customProp should be in log record");
            Assert.IsTrue(hasEnvironment, "Property environment should be in log record");
        }

        [TestMethod]
        public void TrackEventWithNullEventTelemetryHandlesGracefully()
        {
            // TrackEvent should handle null EventTelemetry gracefully without throwing
            this.telemetryClient.TrackEvent((EventTelemetry)null);
            this.telemetryClient.Flush();
            // Note: This test verifies error handling, not that an event is created
        }

        [TestMethod]
        public void TrackEventWithEmptyNameHandlesGracefully()
        {
            // TrackEvent should handle empty name gracefully without throwing
            this.telemetryClient.TrackEvent(string.Empty);
            this.telemetryClient.Flush();
            // Note: This test verifies error handling
        }

        [TestMethod]
        public void TrackEventWithNullNameHandlesGracefully()
        {
            // TrackEvent should handle null name gracefully without throwing
            this.telemetryClient.TrackEvent((string)null);
            this.telemetryClient.Flush();
            // Note: This test verifies error handling
        }

        #endregion

        #region TrackTrace

        [TestMethod]
        public void TrackTraceSendsTraceTelemetryWithSpecifiedNameToProvideSimplestWayOfSendingTraceTelemetry()
        {
            this.telemetryClient.TrackTrace("TestTrace");
            this.telemetryClient.Flush();
            
            Assert.IsTrue(this.logItems.Count > 0, "At least one log should be collected");
            var logRecord = this.logItems.FirstOrDefault(l => 
                l.FormattedMessage != null && l.FormattedMessage.Contains("TestTrace"));
            Assert.IsNotNull(logRecord, "TestTrace should be collected");
            Assert.AreEqual(LogLevel.Information, logRecord.LogLevel);
        }

        [TestMethod]
        public void TrackTraceSendsTraceTelemetryWithSpecifiedObjectTelemetry()
        {
            this.telemetryClient.TrackTrace(new TraceTelemetry { Message = "TestTrace" });
            this.telemetryClient.Flush();
            
            Assert.IsTrue(this.logItems.Count > 0, "At least one log should be collected");
            var logRecord = this.logItems.FirstOrDefault(l => 
                l.FormattedMessage != null && l.FormattedMessage.Contains("TestTrace"));
            Assert.IsNotNull(logRecord, "TestTrace should be collected");
        }

        [TestMethod]
        public void TrackTraceWillSendSeverityLevelIfProvidedInline()
        {
            this.telemetryClient.TrackTrace("Test", SeverityLevel.Error);
            this.telemetryClient.Flush();
            
            Assert.IsTrue(this.logItems.Count > 0, "At least one log should be collected");
            var logRecord = this.logItems.FirstOrDefault(l => 
                l.FormattedMessage != null && l.FormattedMessage.Contains("Test"));
            Assert.IsNotNull(logRecord, "Test trace should be collected");
            Assert.AreEqual(LogLevel.Error, logRecord.LogLevel);
        }

        [TestMethod]
        public void TrackTraceWillNotSetSeverityLevelIfCustomerProvidedOnlyName()
        {
            this.telemetryClient.TrackTrace("Test");
            this.telemetryClient.Flush();
            
            Assert.IsTrue(this.logItems.Count > 0, "At least one log should be collected");
            var logRecord = this.logItems.FirstOrDefault(l => 
                l.FormattedMessage != null && l.FormattedMessage.Contains("Test"));
            Assert.IsNotNull(logRecord, "Test trace should be collected");
            // Default log level is Information when no severity is specified
            Assert.AreEqual(LogLevel.Information, logRecord.LogLevel);
        }

        [TestMethod]
        public void TrackTraceWithMessageAndProperties()
        {
            var properties = new Dictionary<string, string>
            {
                { "prop1", "value1" },
                { "prop2", "value2" }
            };
            
            this.telemetryClient.TrackTrace("TraceWithProps", properties);
            this.telemetryClient.Flush();
            
            Assert.IsTrue(this.logItems.Count > 0, "At least one log should be collected");
            var logRecord = this.logItems.FirstOrDefault(l => 
                l.FormattedMessage != null && l.FormattedMessage.Contains("TraceWithProps"));
            Assert.IsNotNull(logRecord, "TraceWithProps should be collected");
            Assert.AreEqual(LogLevel.Information, logRecord.LogLevel);
            
            // Verify properties
            bool hasProp1 = false, hasProp2 = false;
            if (logRecord.Attributes != null)
            {
                foreach (var attr in logRecord.Attributes)
                {
                    if (attr.Key == "prop1" && attr.Value?.ToString() == "value1")
                        hasProp1 = true;
                    if (attr.Key == "prop2" && attr.Value?.ToString() == "value2")
                        hasProp2 = true;
                }
            }
            Assert.IsTrue(hasProp1, "Property prop1 should be in log record");
            Assert.IsTrue(hasProp2, "Property prop2 should be in log record");
        }

        [TestMethod]
        public void TrackTraceWithSeverityLevelAndProperties()
        {
            var properties = new Dictionary<string, string>
            {
                { "errorCode", "500" },
                { "component", "API" }
            };
            
            this.telemetryClient.TrackTrace("ErrorTrace", SeverityLevel.Error, properties);
            this.telemetryClient.Flush();
            
            Assert.IsTrue(this.logItems.Count > 0, "At least one log should be collected");
            var logRecord = this.logItems.FirstOrDefault(l => 
                l.FormattedMessage != null && l.FormattedMessage.Contains("ErrorTrace"));
            Assert.IsNotNull(logRecord, "ErrorTrace should be collected");
            Assert.AreEqual(LogLevel.Error, logRecord.LogLevel);
            
            // Verify properties
            bool hasErrorCode = false, hasComponent = false;
            if (logRecord.Attributes != null)
            {
                foreach (var attr in logRecord.Attributes)
                {
                    if (attr.Key == "errorCode" && attr.Value?.ToString() == "500")
                        hasErrorCode = true;
                    if (attr.Key == "component" && attr.Value?.ToString() == "API")
                        hasComponent = true;
                }
            }
            Assert.IsTrue(hasErrorCode, "Property errorCode should be in log record");
            Assert.IsTrue(hasComponent, "Property component should be in log record");
        }

        [TestMethod]
        public void TrackTraceWithTraceTelemetryAndProperties()
        {
            var traceTelemetry = new TraceTelemetry("TraceTelemetryWithProps");
            traceTelemetry.SeverityLevel = SeverityLevel.Warning;
            traceTelemetry.Properties["customProp"] = "customValue";
            traceTelemetry.Properties["source"] = "UnitTest";
            
            this.telemetryClient.TrackTrace(traceTelemetry);
            this.telemetryClient.Flush();
            
            Assert.IsTrue(this.logItems.Count > 0, "At least one log should be collected");
            var logRecord = this.logItems.FirstOrDefault(l => 
                l.FormattedMessage != null && l.FormattedMessage.Contains("TraceTelemetryWithProps"));
            Assert.IsNotNull(logRecord, "TraceTelemetryWithProps should be collected");
            Assert.AreEqual(LogLevel.Warning, logRecord.LogLevel);
            
            // Verify properties
            bool hasCustomProp = false, hasSource = false;
            if (logRecord.Attributes != null)
            {
                foreach (var attr in logRecord.Attributes)
                {
                    if (attr.Key == "customProp" && attr.Value?.ToString() == "customValue")
                        hasCustomProp = true;
                    if (attr.Key == "source" && attr.Value?.ToString() == "UnitTest")
                        hasSource = true;
                }
            }
            Assert.IsTrue(hasCustomProp, "Property customProp should be in log record");
            Assert.IsTrue(hasSource, "Property source should be in log record");
        }

        [TestMethod]
        public void TrackTraceWithTraceTelemetryAndAllSeverityLevels()
        {
            // Test all severity levels in sequence (Note: Verbose/Trace may be filtered by default logger configuration)
            var testData = new[]
            {
                (SeverityLevel.Information, LogLevel.Information, "Trace-Information"),
                (SeverityLevel.Warning, LogLevel.Warning, "Trace-Warning"),
                (SeverityLevel.Error, LogLevel.Error, "Trace-Error"),
                (SeverityLevel.Critical, LogLevel.Critical, "Trace-Critical")
            };
            
            foreach (var (severity, expectedLogLevel, message) in testData)
            {
                var traceTelemetry = new TraceTelemetry(message);
                traceTelemetry.SeverityLevel = severity;
                
                this.telemetryClient.TrackTrace(traceTelemetry);
            }
            
            this.telemetryClient.Flush();
            
            // Verify all logs were collected
            Assert.IsTrue(this.logItems.Count >= 4, $"Expected at least 4 logs, but got {this.logItems.Count}");
            
            // Verify each severity level was logged correctly
            foreach (var (severity, expectedLogLevel, message) in testData)
            {
                var logRecord = this.logItems.FirstOrDefault(l => 
                    l.FormattedMessage != null && l.FormattedMessage.Contains(message));
                Assert.IsNotNull(logRecord, $"{message} should be collected");
                Assert.AreEqual(expectedLogLevel, logRecord.LogLevel, $"Severity {severity} should map to {expectedLogLevel}");
            }
        }

        [TestMethod]
        public void TrackTraceWithNullTraceTelemetryCreatesDefaultTrace()
        {
            this.telemetryClient.TrackTrace((TraceTelemetry)null);
            this.telemetryClient.Flush();
            Assert.IsTrue(this.logItems.Count > 0, "At least one log should be collected for null TraceTelemetry");
            var logRecord = this.logItems[0];
            Assert.AreEqual(LogLevel.Information, logRecord.LogLevel, "Default log level should be Information");
        }

        [TestMethod]
        public void TrackTraceWithEmptyMessageInTraceTelemetry()
        {
            var traceTelemetry = new TraceTelemetry();
            traceTelemetry.Message = string.Empty;
            
            this.telemetryClient.TrackTrace(traceTelemetry);
            this.telemetryClient.Flush();
            
            Assert.IsTrue(this.logItems.Count > 0, "Log should be collected even with empty message");
            var logRecord = this.logItems[0];
            Assert.IsNotNull(logRecord, "Log record should exist");
        }

        #endregion

        #region TrackMetric

        [TestMethod]
        public void TrackMetricWithNameAndValue()
        {
            this.telemetryClient.TrackMetric("TestMetric", 42.5);
            this.telemetryClient.Flush();
            
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

        #region TrackRequest

        [TestMethod]
        public void TrackRequestCreatesActivityWithCorrectName()
        {
            var request = new RequestTelemetry("GET /api/users", DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(100), "200", true);
            
            this.telemetryClient.TrackRequest(request);
            this.telemetryClient.Flush();

            // Verify activity was created
            Assert.AreEqual(1, this.activityItems.Count, "One activity should be created");
            Assert.AreEqual("GET /api/users", this.activityItems[0].DisplayName);
            Assert.AreEqual(ActivityKind.Server, this.activityItems[0].Kind);
        }

        [TestMethod]
        public void TrackRequestWithServiceBusUrlSetsMessagingAttributes()
        {
            var request = new RequestTelemetry
            {
                Name = "ServiceBus Message",
                Url = new Uri("sb://myservicebus.servicebus.windows.net/mytopic"),
                Source = "mytopic",
                ResponseCode = "0",
                Duration = TimeSpan.FromMilliseconds(75),
                Success = true
            };

            this.telemetryClient.TrackRequest(request);
            this.telemetryClient.Flush();

            Assert.AreEqual(1, this.activityItems.Count);
            var activity = this.activityItems[0];
            
            // Verify Consumer ActivityKind for messaging
            Assert.AreEqual(ActivityKind.Consumer, activity.Kind);
            Assert.AreEqual(ActivityStatusCode.Ok, activity.Status);
            
            // Verify override attributes preserve original AI values
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.request.name" && t.Value == "ServiceBus Message"));
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.request.url" && t.Value == "sb://myservicebus.servicebus.windows.net/mytopic"));
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.request.source" && t.Value == "mytopic"));
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.request.resultCode" && t.Value == "0"));
        }

        [TestMethod]
        public void TrackRequestSetsOverrideAttributes()
        {
            var request = new RequestTelemetry
            {
                Name = "Custom Request",
                Url = new Uri("https://api.example.com/v1/resource"),
                Source = "mobile-app",
                ResponseCode = "201",
                Duration = TimeSpan.FromMilliseconds(200),
                Success = true
            };
            request.Context.Operation.Name = "CreateResource";

            this.telemetryClient.TrackRequest(request);
            this.telemetryClient.Flush();

            Assert.AreEqual(1, this.activityItems.Count);
            var activity = this.activityItems[0];
            
            // Verify override attributes
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.request.name" && t.Value == "Custom Request"));
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.request.url" && t.Value == "https://api.example.com/v1/resource"));
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.request.source" && t.Value == "mobile-app"));
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.request.resultCode" && t.Value == "201"));
            Assert.AreEqual(ActivityKind.Server, activity.Kind);
            Assert.AreEqual(ActivityStatusCode.Ok, activity.Status);
        }

        [TestMethod]
        public void TrackRequestWithPropertiesIncludesCustomProperties()
        {
            this.activityItems.Clear();

            var request = new RequestTelemetry("Test Request", DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(100), "200", true);
            request.Properties["userId"] = "12345";
            request.Properties["region"] = "us-west";
            
            this.telemetryClient.TrackRequest(request);
            this.telemetryClient.Flush();

            Assert.AreEqual(1, activityItems.Count);
            var activity = activityItems[0];
            
            // Verify custom properties are included
            Assert.AreEqual("userId", activity.Tags.FirstOrDefault(t => t.Key == "userId").Key);
            Assert.AreEqual("12345", activity.Tags.FirstOrDefault(t => t.Key == "userId").Value);
            Assert.AreEqual("region", activity.Tags.FirstOrDefault(t => t.Key == "region").Key);
            Assert.AreEqual("us-west", activity.Tags.FirstOrDefault(t => t.Key == "region").Value);
            
            // Verify standard attributes
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.request.name" && t.Value == "Test Request"));
            Assert.AreEqual(ActivityKind.Server, activity.Kind);
        }

        [TestMethod]
        public void TrackRequestWithFailedResponseCodeMarksAsError()
        {
            var request = new RequestTelemetry
            {
                Name = "GET /api/error",
                Url = new Uri("https://example.com/api/error"),
                ResponseCode = "500",
                Success = false,
                Duration = TimeSpan.FromMilliseconds(50)
            };

            this.telemetryClient.TrackRequest(request);
            this.telemetryClient.Flush();

            Assert.AreEqual(1, this.activityItems.Count);
            var activity = this.activityItems[0];
            
            // Verify error status
            Assert.AreEqual(ActivityStatusCode.Error, activity.Status);
            Assert.AreEqual(ActivityKind.Server, activity.Kind);
            
            // Verify override attributes
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.request.name" && t.Value == "GET /api/error"));
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.request.url" && t.Value == "https://example.com/api/error"));
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.request.resultCode" && t.Value == "500"));
        }

        [TestMethod]
        public void TrackRequestWithQueueSourceSetsMessagingSystem()
        {
            var request = new RequestTelemetry
            {
                Name = "Queue Message Handler",
                Source = "orders-queue",
                ResponseCode = "0",
                Duration = TimeSpan.FromMilliseconds(120),
                Success = true
            };

            this.telemetryClient.TrackRequest(request);
            this.telemetryClient.Flush();

            Assert.AreEqual(1, this.activityItems.Count);
            var activity = this.activityItems[0];
            
            // Verify Consumer ActivityKind for queue messaging
            Assert.AreEqual(ActivityKind.Consumer, activity.Kind);
            Assert.AreEqual(ActivityStatusCode.Ok, activity.Status);
            
            // Verify override attributes
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.request.name" && t.Value == "Queue Message Handler"));
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.request.source" && t.Value == "orders-queue"));
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.request.resultCode" && t.Value == "0"));
        }

        [TestMethod]
        public void TrackRequestHandlesNullRequestTelemetryGracefully()
        {
            this.telemetryClient.TrackRequest((RequestTelemetry)null);
            this.telemetryClient.Flush();

            // Should not throw exception and should not create any activity
            Assert.AreEqual(0, this.activityItems.Count);
        }

        #endregion

        #region TrackDependency

        [TestMethod]
        public void TrackDependencyWithServiceBusTypeSetsBrokerAttributes()
        {
            var dependency = new DependencyTelemetry
            {
                Type = "Azure Service Bus",
                Target = "myservicebus.servicebus.windows.net/mytopic",
                Name = "Publish event",
                Data = "sb://myservicebus.servicebus.windows.net/mytopic",
                ResultCode = "0",
                Duration = TimeSpan.FromMilliseconds(35),
                Success = true
            };

            this.telemetryClient.TrackDependency(dependency);
            this.telemetryClient.Flush();

            Assert.AreEqual(1, this.activityItems.Count);
            var activity = this.activityItems[0];
            
            // Verify Producer ActivityKind
            Assert.AreEqual(ActivityKind.Producer, activity.Kind);
            Assert.AreEqual(ActivityStatusCode.Ok, activity.Status);
            
            // Verify override attributes
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.dependency.type" && t.Value == "Azure Service Bus"));
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.dependency.target" && t.Value == "myservicebus.servicebus.windows.net/mytopic"));
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.dependency.name" && t.Value == "Publish event"));
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.dependency.data" && t.Value == "sb://myservicebus.servicebus.windows.net/mytopic"));
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.dependency.resultCode" && t.Value == "0"));
        }

        [TestMethod]
        public void TrackDependencyWithAzureSDKTypeSetsAzureNamespace()
        {
            var dependency = new DependencyTelemetry
            {
                Type = "InProc | Microsoft.Storage",
                Target = "mystorageaccount.blob.core.windows.net",
                Name = "Upload blob",
                Data = "PUT /container/blob.txt",
                ResultCode = "201",
                Duration = TimeSpan.FromMilliseconds(200),
                Success = true
            };

            this.telemetryClient.TrackDependency(dependency);
            this.telemetryClient.Flush();

            Assert.AreEqual(1, this.activityItems.Count);
            var activity = this.activityItems[0];
            
            // Verify Internal ActivityKind for InProc
            Assert.AreEqual(ActivityKind.Internal, activity.Kind);
            Assert.AreEqual(ActivityStatusCode.Ok, activity.Status);
            
            // Verify override attributes preserve Azure namespace in type
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.dependency.type" && t.Value == "InProc | Microsoft.Storage"));
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.dependency.target" && t.Value == "mystorageaccount.blob.core.windows.net"));
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.dependency.name" && t.Value == "Upload blob"));
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.dependency.data" && t.Value == "PUT /container/blob.txt"));
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.dependency.resultCode" && t.Value == "201"));
        }

        [TestMethod]
        public void TrackDependencySetsOverrideAttributes()
        {
            var dependency = new DependencyTelemetry
            {
                Type = "Http",
                Target = "api.example.com",
                Name = "POST /orders",
                Data = "https://api.example.com/orders",
                Duration = TimeSpan.FromMilliseconds(250),
                Success = true,
                ResultCode = "201"
            };

            this.telemetryClient.TrackDependency(dependency);
            this.telemetryClient.Flush();

            Assert.AreEqual(1, this.activityItems.Count);
            var activity = this.activityItems[0];
            
            // Verify ActivityKind and Status
            Assert.AreEqual(ActivityKind.Client, activity.Kind);
            Assert.AreEqual(ActivityStatusCode.Ok, activity.Status);
            
            // Verify override attributes
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.dependency.type" && t.Value == "Http"));
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.dependency.target" && t.Value == "api.example.com"));
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.dependency.name" && t.Value == "POST /orders"));
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.dependency.data" && t.Value == "https://api.example.com/orders"));
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.dependency.resultCode" && t.Value == "201"));
        }

        [TestMethod]
        public void TrackDependencyWithPropertiesIncludesCustomProperties()
        {
            this.activityItems.Clear();

            var dependency = new DependencyTelemetry("redis", "cache.example.com", "GET user:123", "GET user:123", DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(5), "200", true);
            dependency.Properties["operation"] = "query";
            dependency.Properties["cacheHit"] = "false";
            
            this.telemetryClient.TrackDependency(dependency);
            this.telemetryClient.Flush();

            Assert.AreEqual(1, activityItems.Count);
            var activity = activityItems[0];
            
            // Verify custom properties are included
            Assert.AreEqual("operation", activity.Tags.FirstOrDefault(t => t.Key == "operation").Key);
            Assert.AreEqual("query", activity.Tags.FirstOrDefault(t => t.Key == "operation").Value);
            Assert.AreEqual("cacheHit", activity.Tags.FirstOrDefault(t => t.Key == "cacheHit").Key);
            Assert.AreEqual("false", activity.Tags.FirstOrDefault(t => t.Key == "cacheHit").Value);
            
            // Verify standard attributes
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.dependency.type" && t.Value == "redis"));
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.dependency.target" && t.Value == "cache.example.com"));
            Assert.AreEqual(ActivityKind.Client, activity.Kind);
        }

        [TestMethod]
        public void TrackDependencyWithFailedCallMarksAsError()
        {
            var dependency = new DependencyTelemetry
            {
                Type = "Http",
                Target = "api.example.com",
                Name = "GET /error",
                Data = "https://api.example.com/error",
                Duration = TimeSpan.FromMilliseconds(50),
                Success = false,
                ResultCode = "500"
            };

            this.telemetryClient.TrackDependency(dependency);
            this.telemetryClient.Flush();

            Assert.AreEqual(1, this.activityItems.Count);
            var activity = this.activityItems[0];
            
            // Verify error status
            Assert.AreEqual(ActivityStatusCode.Error, activity.Status);
            Assert.AreEqual(ActivityKind.Client, activity.Kind);
            
            // Verify override attributes
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.dependency.type" && t.Value == "Http"));
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.dependency.data" && t.Value == "https://api.example.com/error"));
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.dependency.resultCode" && t.Value == "500"));
        }

        [TestMethod]
        public void TrackDependencyHandlesNullDependencyTelemetryGracefully()
        {
            this.telemetryClient.TrackDependency((DependencyTelemetry)null);
            this.telemetryClient.Flush();

            // Should not throw exception and should not create any activity
            Assert.AreEqual(0, this.activityItems.Count);
        }

        [TestMethod]
        public void TrackDependencyWithCaseInsensitiveHttpType()
        {
            var dependency = new DependencyTelemetry
            {
                Type = "HTTP", // uppercase
                Target = "api.example.com",
                Name = "GET /data",
                Data = "https://api.example.com/data",
                Duration = TimeSpan.FromMilliseconds(75),
                Success = true,
                ResultCode = "200"
            };

            this.telemetryClient.TrackDependency(dependency);
            this.telemetryClient.Flush();

            Assert.AreEqual(1, this.activityItems.Count);
            var activity = this.activityItems[0];
            
            // Verify ActivityKind detection works regardless of case
            Assert.AreEqual(ActivityKind.Client, activity.Kind);
            Assert.AreEqual(ActivityStatusCode.Ok, activity.Status);
            
            // Verify override attributes preserve original case
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.dependency.type" && t.Value == "HTTP"));
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.dependency.data" && t.Value == "https://api.example.com/data"));
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.dependency.target" && t.Value == "api.example.com"));
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.dependency.resultCode" && t.Value == "200"));
        }

        [TestMethod]
        public void TrackDependencyWithCaseInsensitiveSqlType()
        {
            var dependency = new DependencyTelemetry
            {
                Type = "sql", // lowercase
                Target = "localhost | testdb",
                Name = "Query",
                Data = "SELECT COUNT(*) FROM Orders",
                Duration = TimeSpan.FromMilliseconds(35),
                Success = true,
                ResultCode = "0"
            };

            this.telemetryClient.TrackDependency(dependency);
            this.telemetryClient.Flush();

            Assert.AreEqual(1, this.activityItems.Count);
            var activity = this.activityItems[0];
            
            // Verify ActivityKind detection works regardless of case
            Assert.AreEqual(ActivityKind.Client, activity.Kind);
            Assert.AreEqual(ActivityStatusCode.Ok, activity.Status);
            
            // Verify override attributes preserve original case
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.dependency.type" && t.Value == "sql"));
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.dependency.data" && t.Value == "SELECT COUNT(*) FROM Orders"));
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.dependency.target" && t.Value == "localhost | testdb"));
            Assert.IsTrue(activity.Tags.Any(t => t.Key == "microsoft.dependency.resultCode" && t.Value == "0"));
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
