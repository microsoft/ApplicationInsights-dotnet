namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
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
    using Xunit;
    using OpenTelemetry;
    using OpenTelemetry.Logs;
    using OpenTelemetry.Metrics;
    using OpenTelemetry.Trace;

    [Collection("TelemetryClientTests")]
    public class TelemetryClientTest : IDisposable
    {
        private List<LogRecord> logItems;
        private List<OpenTelemetry.Metrics.Metric> metricItems;
        private List<Activity> activityItems;
        private TelemetryClient telemetryClient;

        public TelemetryClientTest()
        {
            var configuration = new TelemetryConfiguration();
            configuration.SamplingRatio = 1.0f;
            this.logItems = new List<LogRecord>();
            this.metricItems = new List<OpenTelemetry.Metrics.Metric>();
            this.activityItems = new List<Activity>();
            var instrumentationKey = Guid.NewGuid().ToString();
            configuration.ConnectionString = "InstrumentationKey=" + instrumentationKey;
            configuration.ConfigureOpenTelemetryBuilder(b => b
                .WithLogging(l => l.AddInMemoryExporter(logItems))
                .WithMetrics(m => m.AddInMemoryExporter(metricItems))
                .WithTracing(t => t.AddInMemoryExporter(activityItems)));
            this.telemetryClient = new TelemetryClient(configuration);
        }

        public void Dispose()
        {
            this.activityItems?.Clear();
            this.metricItems?.Clear();
            this.logItems?.Clear();
            this.telemetryClient?.TelemetryConfiguration?.Dispose();
        }

        [Fact]
        public void TelemetryClientInitializesFeatureReporter()
        {
            Assert.NotNull(this.telemetryClient.Configuration.FeatureReporter);
        }

        #region TrackEvent

        [Fact]
        public void TrackEventSendsEventTelemetryWithSpecifiedNameToProvideSimplestWayOfSendingEventTelemetry()
        {
            this.telemetryClient.TrackEvent("TestEvent");
            this.telemetryClient.Flush();
            
            Assert.True(this.logItems.Count > 0, "At least one log should be collected");
            var logRecord = this.logItems.FirstOrDefault(l => 
                l.Attributes != null && l.Attributes.Any(a => 
                    a.Key == "microsoft.custom_event.name" && a.Value?.ToString() == "TestEvent"));
            Assert.NotNull(logRecord);
            Assert.Equal(LogLevel.Information, logRecord.LogLevel);
        }

        [Fact]
        public void TrackEventSendsEventTelemetryWithSpecifiedObjectTelemetry()
        {
            this.telemetryClient.TrackEvent(new EventTelemetry("TestEvent"));
            this.telemetryClient.Flush();
            
            Assert.True(this.logItems.Count > 0);
            var logRecord = this.logItems.FirstOrDefault(l => 
                l.Attributes != null && l.Attributes.Any(a => 
                    a.Key == "microsoft.custom_event.name" && a.Value?.ToString() == "TestEvent"));
            Assert.NotNull(logRecord);
        }

        [Fact]
        public void TrackEventWillSendPropertiesIfProvidedInline()
        {
            this.telemetryClient.TrackEvent("Test", new Dictionary<string, string> { { "blah", "yoyo" } });
            this.telemetryClient.Flush();
            
            Assert.True(this.logItems.Count > 0, "At least one log should be collected");
            var logRecord = this.logItems.FirstOrDefault(l => 
                l.Attributes != null && l.Attributes.Any(a => 
                    a.Key == "microsoft.custom_event.name" && a.Value?.ToString() == "Test"));
            Assert.NotNull(logRecord);
            
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
            Assert.True(hasBlahProperty, "Property 'blah' should be 'yoyo'");
        }

        [Fact]
        public void TrackEventWithEventTelemetryAndProperties()
        {
            // Test EventTelemetry with properties set via Properties dictionary
            var eventTelemetry = new EventTelemetry("TestEventWithProps");
            eventTelemetry.Properties["customProp"] = "customValue";
            eventTelemetry.Properties["environment"] = "test";
            
            this.telemetryClient.TrackEvent(eventTelemetry);
            this.telemetryClient.Flush();
            
            Assert.True(this.logItems.Count > 0, "At least one log should be collected");
            var logRecord = this.logItems.FirstOrDefault(l => 
                l.Attributes != null && l.Attributes.Any(a => 
                    a.Key == "microsoft.custom_event.name" && a.Value?.ToString() == "TestEventWithProps"));
            Assert.NotNull(logRecord);
            Assert.NotNull(logRecord.Attributes);
            
            // Verify properties are in attributes
            bool hasCustomProp = false, hasEnvironment = false;
            foreach (var attr in logRecord.Attributes)
            {
                if (attr.Key == "customProp" && attr.Value?.ToString() == "customValue")
                    hasCustomProp = true;
                if (attr.Key == "environment" && attr.Value?.ToString() == "test")
                    hasEnvironment = true;
            }
            Assert.True(hasCustomProp, "Property customProp should be in log record");
            Assert.True(hasEnvironment, "Property environment should be in log record");
        }

        [Fact]
        public void TrackEventWithNullEventTelemetryHandlesGracefully()
        {
            // TrackEvent should handle null EventTelemetry gracefully without throwing
            this.telemetryClient.TrackEvent((EventTelemetry)null);
            this.telemetryClient.Flush();
            // Note: This test verifies error handling, not that an event is created
        }

        [Fact]
        public void TrackEventWithEmptyNameHandlesGracefully()
        {
            // TrackEvent should handle empty name gracefully without throwing
            this.telemetryClient.TrackEvent(string.Empty);
            this.telemetryClient.Flush();
            // Note: This test verifies error handling
        }

        [Fact]
        public void TrackEventWithNullNameHandlesGracefully()
        {
            // TrackEvent should handle null name gracefully without throwing
            this.telemetryClient.TrackEvent((string)null);
            this.telemetryClient.Flush();
            // Note: This test verifies error handling
        }

        [Fact]
        public void TrackEventDoesNotMutatePropertiesDictionary()
        {
            var properties = new Dictionary<string, string> { { "key1", "value1" } };
            var originalCount = properties.Count;

            this.telemetryClient.TrackEvent("TestEvent", properties);
            this.telemetryClient.Flush();

            // The caller's dictionary must not be modified
            Assert.Equal(originalCount, properties.Count);
            Assert.False(properties.ContainsKey("microsoft.custom_event.name"),
                "Internal attribute should not leak into caller's dictionary");
        }

        [Fact]
        public void TrackEventAcceptsReadOnlyDictionary()
        {
            var inner = new Dictionary<string, string> { { "key1", "value1" } };
            var readOnly = new ReadOnlyDictionary<string, string>(inner);

            // Must not throw NotSupportedException
            this.telemetryClient.TrackEvent("TestEvent", readOnly);
            this.telemetryClient.Flush();

            var logRecord = this.logItems.FirstOrDefault(l =>
                l.Attributes != null && l.Attributes.Any(a =>
                    a.Key == "microsoft.custom_event.name" && a.Value?.ToString() == "TestEvent"));
            Assert.NotNull(logRecord);

            // Verify user property is still present
            Assert.True(logRecord.Attributes.Any(a => a.Key == "key1" && a.Value?.ToString() == "value1"));
        }

        [Fact]
        public void TrackEventCanBeCalledTwiceWithSameDictionary()
        {
            var properties = new Dictionary<string, string> { { "key1", "value1" } };

            // Both calls should succeed without ArgumentException from duplicate keys
            this.telemetryClient.TrackEvent("Event1", properties);
            this.telemetryClient.TrackEvent("Event2", properties);
            this.telemetryClient.Flush();

            Assert.True(this.logItems.Count >= 2, "Both events should be recorded");
        }

        [Fact]
        public void TrackEventWithEventTelemetryDoesNotMutateProperties()
        {
            var eventTelemetry = new EventTelemetry("TestEvent");
            eventTelemetry.Properties["userProp"] = "userValue";
            var originalCount = eventTelemetry.Properties.Count;

            this.telemetryClient.TrackEvent(eventTelemetry);
            this.telemetryClient.Flush();

            // The telemetry object's Properties must not be modified
            Assert.Equal(originalCount, eventTelemetry.Properties.Count);
            Assert.False(eventTelemetry.Properties.ContainsKey("microsoft.custom_event.name"),
                "Internal attribute should not leak into telemetry's Properties");
        }

        #endregion

        #region TrackAvailability

        [Fact]
        public void TrackAvailabilitySendsAvailabilityTelemetryWithAllParameters()
        {
            var name = "MyAvailabilityTest";
            var timeStamp = DateTimeOffset.UtcNow;
            var duration = TimeSpan.FromSeconds(5);
            var runLocation = "West US";
            var success = true;
            var message = "Test passed";
            var properties = new Dictionary<string, string> { { "Environment", "Production" } };

            this.telemetryClient.TrackAvailability(name, timeStamp, duration, runLocation, success, message, properties);
            this.telemetryClient.Flush();

            Assert.True(this.logItems.Count > 0, "At least one log should be collected");
            var logRecord = this.logItems.FirstOrDefault(l =>
                l.Attributes != null && l.Attributes.Any(a =>
                    a.Key == "microsoft.availability.name" && a.Value?.ToString() == name));
            Assert.NotNull(logRecord);
            Assert.Equal(LogLevel.Information, logRecord.LogLevel);

            // Verify availability attributes
            Assert.Contains(logRecord.Attributes, a => a.Key == "microsoft.availability.name" && a.Value?.ToString() == name);
            Assert.Contains(logRecord.Attributes, a => a.Key == "microsoft.availability.duration" && a.Value?.ToString() == duration.ToString());
            Assert.Contains(logRecord.Attributes, a => a.Key == "microsoft.availability.success" && a.Value?.ToString() == success.ToString());
            Assert.Contains(logRecord.Attributes, a => a.Key == "microsoft.availability.runLocation" && a.Value?.ToString() == runLocation);
            Assert.Contains(logRecord.Attributes, a => a.Key == "microsoft.availability.message" && a.Value?.ToString() == message);
            Assert.Contains(logRecord.Attributes, a => a.Key == "Environment" && a.Value?.ToString() == "Production");
        }

        [Fact]
        public void TrackAvailabilitySendsAvailabilityTelemetryWithMinimalParameters()
        {
            var name = "MinimalTest";
            var timeStamp = DateTimeOffset.UtcNow;
            var duration = TimeSpan.FromSeconds(2);
            var runLocation = "East US";
            var success = false;

            this.telemetryClient.TrackAvailability(name, timeStamp, duration, runLocation, success);
            this.telemetryClient.Flush();

            Assert.True(this.logItems.Count > 0, "At least one log should be collected");
            var logRecord = this.logItems.FirstOrDefault(l =>
                l.Attributes != null && l.Attributes.Any(a =>
                    a.Key == "microsoft.availability.name" && a.Value?.ToString() == name));
            Assert.NotNull(logRecord);
            
            // Verify required attributes are present
            Assert.Contains(logRecord.Attributes, a => a.Key == "microsoft.availability.name");
            Assert.Contains(logRecord.Attributes, a => a.Key == "microsoft.availability.duration");
            Assert.Contains(logRecord.Attributes, a => a.Key == "microsoft.availability.success" && a.Value?.ToString() == "False");
        }

        [Fact]
        public void TrackAvailabilityWithAvailabilityTelemetryObject()
        {
            var availabilityTelemetry = new AvailabilityTelemetry("TestWithObject", DateTimeOffset.UtcNow, TimeSpan.FromSeconds(3), "North Europe", true, "Success");
            availabilityTelemetry.Properties["CustomProp"] = "CustomValue";

            this.telemetryClient.TrackAvailability(availabilityTelemetry);
            this.telemetryClient.Flush();

            Assert.True(this.logItems.Count > 0, "At least one log should be collected");
            var logRecord = this.logItems.FirstOrDefault(l =>
                l.Attributes != null && l.Attributes.Any(a =>
                    a.Key == "microsoft.availability.name" && a.Value?.ToString() == "TestWithObject"));
            Assert.NotNull(logRecord);
            
            // Verify custom property
            Assert.Contains(logRecord.Attributes, a => a.Key == "CustomProp" && a.Value?.ToString() == "CustomValue");
        }

        [Fact]
        public void TrackAvailabilityWithNullTelemetryHandlesGracefully()
        {
            // Should not throw
            this.telemetryClient.TrackAvailability((AvailabilityTelemetry)null);
            this.telemetryClient.Flush();
            
            // No log record should be created for null telemetry
            var availabilityLogs = this.logItems.Where(l =>
                l.Attributes != null && l.Attributes.Any(a => a.Key.StartsWith("microsoft.availability.")));
            Assert.Empty(availabilityLogs);
        }

        [Fact]
        public void TrackAvailabilityWithCustomProperties()
        {
            var availabilityTelemetry = new AvailabilityTelemetry("PropertiesTest", DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1), "Local", true);
            availabilityTelemetry.Properties["Environment"] = "Production";
            availabilityTelemetry.Properties["Region"] = "WestUS";
            
            this.telemetryClient.TrackAvailability(availabilityTelemetry);
            this.telemetryClient.Flush();

            Assert.True(this.logItems.Count > 0, "At least one log should be collected");
            var logRecord = this.logItems.FirstOrDefault(l =>
                l.Attributes != null && l.Attributes.Any(a =>
                    a.Key == "microsoft.availability.name" && a.Value?.ToString() == "PropertiesTest"));
            Assert.NotNull(logRecord);

            // Verify custom properties are included
            Assert.Contains(logRecord.Attributes, a => a.Key == "Environment" && a.Value?.ToString() == "Production");
            Assert.Contains(logRecord.Attributes, a => a.Key == "Region" && a.Value?.ToString() == "WestUS");
        }

        [Fact]
        public void TrackAvailabilityGeneratesIdIfNotProvided()
        {
            var availabilityTelemetry = new AvailabilityTelemetry("AutoIdTest", DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1), "Local", true);
            // Don't set Id, it should be auto-generated

            this.telemetryClient.TrackAvailability(availabilityTelemetry);
            this.telemetryClient.Flush();

            Assert.True(this.logItems.Count > 0, "At least one log should be collected");
            var logRecord = this.logItems.FirstOrDefault(l =>
                l.Attributes != null && l.Attributes.Any(a =>
                    a.Key == "microsoft.availability.name" && a.Value?.ToString() == "AutoIdTest"));
            Assert.NotNull(logRecord);

            // Verify ID attribute exists and is a valid GUID
            var idAttr = logRecord.Attributes.FirstOrDefault(a => a.Key == "microsoft.availability.id");
            Assert.NotNull(idAttr.Value);
            Assert.True(Guid.TryParse(idAttr.Value?.ToString(), out _), "ID should be a valid GUID");
        }

        #endregion

        #region TrackTrace

        [Fact]
        public void TrackTraceSendsTraceTelemetryWithSpecifiedNameToProvideSimplestWayOfSendingTraceTelemetry()
        {
            this.telemetryClient.TrackTrace("TestTrace");
            this.telemetryClient.Flush();
            
            Assert.True(this.logItems.Count > 0, "At least one log should be collected");
            var logRecord = this.logItems.FirstOrDefault(l => 
                l.FormattedMessage != null && l.FormattedMessage.Contains("TestTrace"));
            Assert.NotNull(logRecord);
            Assert.Equal(LogLevel.Information, logRecord.LogLevel);
        }

        [Fact]
        public void TrackTraceSendsTraceTelemetryWithSpecifiedObjectTelemetry()
        {
            this.telemetryClient.TrackTrace(new TraceTelemetry { Message = "TestTrace" });
            this.telemetryClient.Flush();
            
            Assert.True(this.logItems.Count > 0);
            var logRecord = this.logItems.FirstOrDefault(l => 
                l.FormattedMessage != null && l.FormattedMessage.Contains("TestTrace"));
            Assert.NotNull(logRecord);
        }

        [Fact]
        public void TrackTraceWillSendSeverityLevelIfProvidedInline()
        {
            this.telemetryClient.TrackTrace("Test", SeverityLevel.Error);
            this.telemetryClient.Flush();
            
            Assert.True(this.logItems.Count > 0, "At least one log should be collected");
            var logRecord = this.logItems.FirstOrDefault(l => 
                l.FormattedMessage != null && l.FormattedMessage.Contains("Test"));
            Assert.NotNull(logRecord);
            Assert.Equal(LogLevel.Error, logRecord.LogLevel);
        }

        [Fact]
        public void TrackTraceWillNotSetSeverityLevelIfCustomerProvidedOnlyName()
        {
            this.telemetryClient.TrackTrace("Test");
            this.telemetryClient.Flush();
            
            Assert.True(this.logItems.Count > 0);
            var logRecord = this.logItems.FirstOrDefault(l => 
                l.FormattedMessage != null && l.FormattedMessage.Contains("Test"));
            Assert.NotNull(logRecord);
            // Default log level is Information when no severity is specified
            Assert.Equal(LogLevel.Information, logRecord.LogLevel);
        }

        [Fact]
        public void TrackTraceWithMessageAndProperties()
        {
            var properties = new Dictionary<string, string>
            {
                { "prop1", "value1" },
                { "prop2", "value2" }
            };
            
            this.telemetryClient.TrackTrace("TraceWithProps", properties);
            this.telemetryClient.Flush();
            
            Assert.True(this.logItems.Count > 0, "At least one log should be collected");
            var logRecord = this.logItems.FirstOrDefault(l => 
                l.FormattedMessage != null && l.FormattedMessage.Contains("TraceWithProps"));
            Assert.NotNull(logRecord);
            Assert.Equal(LogLevel.Information, logRecord.LogLevel);
            
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
            Assert.True(hasProp1, "Property prop1 should be in log record");
            Assert.True(hasProp2, "Property prop2 should be in log record");
        }

        [Fact]
        public void TrackTraceWithSeverityLevelAndProperties()
        {
            var properties = new Dictionary<string, string>
            {
                { "errorCode", "500" },
                { "component", "API" }
            };
            
            this.telemetryClient.TrackTrace("ErrorTrace", SeverityLevel.Error, properties);
            this.telemetryClient.Flush();
            
            Assert.True(this.logItems.Count > 0, "At least one log should be collected");
            var logRecord = this.logItems.FirstOrDefault(l => 
                l.FormattedMessage != null && l.FormattedMessage.Contains("ErrorTrace"));
            Assert.NotNull(logRecord);
            Assert.Equal(LogLevel.Error, logRecord.LogLevel);
            
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
            Assert.True(hasErrorCode, "Property errorCode should be in log record");
            Assert.True(hasComponent, "Property component should be in log record");
        }

        [Fact]
        public void TrackTraceWithTraceTelemetryAndProperties()
        {
            var traceTelemetry = new TraceTelemetry("TraceTelemetryWithProps");
            traceTelemetry.SeverityLevel = SeverityLevel.Warning;
            traceTelemetry.Properties["customProp"] = "customValue";
            traceTelemetry.Properties["source"] = "UnitTest";
            
            this.telemetryClient.TrackTrace(traceTelemetry);
            this.telemetryClient.Flush();
            
            Assert.True(this.logItems.Count > 0, "At least one log should be collected");
            var logRecord = this.logItems.FirstOrDefault(l => 
                l.FormattedMessage != null && l.FormattedMessage.Contains("TraceTelemetryWithProps"));
            Assert.NotNull(logRecord);
            Assert.Equal(LogLevel.Warning, logRecord.LogLevel);
            
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
            Assert.True(hasCustomProp, "Property customProp should be in log record");
            Assert.True(hasSource, "Property source should be in log record");
        }

        [Fact]
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
            Assert.True(this.logItems.Count >= 4, $"Expected at least 4 logs, but got {this.logItems.Count}");
            
            // Verify each severity level was logged correctly
            foreach (var (severity, expectedLogLevel, message) in testData)
            {
                var logRecord = this.logItems.FirstOrDefault(l => 
                    l.FormattedMessage != null && l.FormattedMessage.Contains(message));
                Assert.NotNull(logRecord);
                Assert.Equal(expectedLogLevel, logRecord.LogLevel);
            }
        }

        [Fact]
        public void TrackTraceWithNullTraceTelemetryCreatesDefaultTrace()
        {
            this.telemetryClient.TrackTrace((TraceTelemetry)null);
            this.telemetryClient.Flush();
            Assert.True(this.logItems.Count > 0, "At least one log should be collected for null TraceTelemetry");
            var logRecord = this.logItems[0];
            Assert.Equal(LogLevel.Information, logRecord.LogLevel);
        }

        [Fact]
        public void TrackTraceWithEmptyMessageInTraceTelemetry()
        {
            var traceTelemetry = new TraceTelemetry();
            traceTelemetry.Message = string.Empty;
            
            this.telemetryClient.TrackTrace(traceTelemetry);
            this.telemetryClient.Flush();
            
            Assert.True(this.logItems.Count > 0, "Log should be collected even with empty message");
            var logRecord = this.logItems[0];
            Assert.NotNull(logRecord);
        }

        [Fact]
        public void TrackTraceDoesNotMutatePropertiesDictionary()
        {
            var properties = new Dictionary<string, string> { { "key1", "value1" } };
            var originalCount = properties.Count;

            this.telemetryClient.TrackTrace("Test message", properties);
            this.telemetryClient.Flush();

            Assert.Equal(originalCount, properties.Count);
        }

        [Fact]
        public void TrackTraceAcceptsReadOnlyDictionary()
        {
            var inner = new Dictionary<string, string> { { "key1", "value1" } };
            var readOnly = new ReadOnlyDictionary<string, string>(inner);

            // Must not throw NotSupportedException
            this.telemetryClient.TrackTrace("Test message", readOnly);
            this.telemetryClient.Flush();

            Assert.True(this.logItems.Count > 0, "Log should be collected");
        }

        [Fact]
        public void TrackTraceWithTraceTelemetryDoesNotMutateProperties()
        {
            var traceTelemetry = new TraceTelemetry("Test message");
            traceTelemetry.Properties["userProp"] = "userValue";
            var originalCount = traceTelemetry.Properties.Count;

            this.telemetryClient.TrackTrace(traceTelemetry);
            this.telemetryClient.Flush();

            Assert.Equal(originalCount, traceTelemetry.Properties.Count);
            Assert.False(traceTelemetry.Properties.ContainsKey("microsoft.client.ip"),
                "Internal attributes should not leak into telemetry's Properties");
        }

        #endregion

        #region TrackMetric

        [Fact]
        public void TrackMetricWithNameAndValue()
        {
            this.telemetryClient.TrackMetric("TestMetric", 42.5);
            this.telemetryClient.Flush();
            
            Assert.True(this.metricItems.Count > 0, "At least one metric should be collected");
            
            var metric = this.metricItems.FirstOrDefault(m => m.Name == "TestMetric");
            Assert.NotNull(metric);
            
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
                    Assert.True(sum > 0, "Histogram sum should be positive");
                    break;
                }
            }
            Assert.True(hasData, "Metric should have recorded histogram data");
        }

        [Fact]
        public void TrackMetricWithProperties()
        {
            this.telemetryClient.TrackMetric("TestMetric", 4.2, new Dictionary<string, string> { { "property1", "value1" } });
            this.telemetryClient.Flush();
            Assert.True(this.metricItems.Count > 0, "At least one metric should be collected");
            
            var metric = this.metricItems.FirstOrDefault(m => m.Name == "TestMetric");
            Assert.NotNull(metric);
            
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
                    Assert.True(hasProperty, "Metric should have property1 tag");
                    break;
                }
            }
            Assert.True(hasData, "Metric should have recorded data");
        }

        [Fact]
        public void TrackMetricWithNullPropertiesDoesNotThrow()
        {
            this.telemetryClient.TrackMetric("TestMetric", 4.2, null);
            this.telemetryClient.Flush();
            Assert.True(this.metricItems.Count > 0, "At least one metric should be collected");
            var metric = this.metricItems.FirstOrDefault(m => m.Name == "TestMetric");
            Assert.NotNull(metric);
        }

        [Fact]
        public void GetMetricWithZeroDimensions()
        {
            var metric = this.telemetryClient.GetMetric("TestMetric");
            Assert.NotNull(metric);
            
            metric.TrackValue(10.0);
            metric.TrackValue(20.0);
            
            this.telemetryClient.Flush();
            Assert.True(this.metricItems.Count > 0);
            var collectedMetric = this.metricItems.FirstOrDefault(m => m.Name == "TestMetric");
            Assert.NotNull(collectedMetric);
            
            // Verify we recorded 2 values (count should be 2)
            foreach (var point in collectedMetric.GetMetricPoints())
            {
                var count = point.GetHistogramCount();
                Assert.Equal(2, (int)count);
                var sum = point.GetHistogramSum();
                Assert.Equal(30.0, sum, 2);
                break;
            }
        }

        [Fact]
        public void GetMetricWithOneDimension()
        {
            var metric = this.telemetryClient.GetMetric("RequestDuration", "StatusCode");
            Assert.NotNull(metric);
            
            metric.TrackValue(150.0, "404");
            metric.TrackValue(120.0, "200");
            
            this.telemetryClient.Flush();
            Assert.True(this.metricItems.Count > 0, "At least one metric should be collected");
            var collectedMetric = this.metricItems.FirstOrDefault(m => m.Name == "RequestDuration");
            Assert.NotNull(collectedMetric);
            
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
                Assert.True(hasStatusCodeTag, "Should have StatusCode dimension tag");
            }
            Assert.True(pointCount > 0, "Should have metric points");
        }

        [Fact]
        public void GetMetricWithTwoDimensions()
        {
            var metric = this.telemetryClient.GetMetric("DatabaseQuery", "Database", "Operation");
            Assert.NotNull(metric);
            
            metric.TrackValue(50.0, "UsersDB", "SELECT");
            metric.TrackValue(80.0, "OrdersDB", "INSERT");
            
            this.telemetryClient.Flush();
            Assert.True(this.metricItems.Count > 0, "At least one metric should be collected");
            var collectedMetric = this.metricItems.FirstOrDefault(m => m.Name == "DatabaseQuery");
            Assert.NotNull(collectedMetric);
            
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
                Assert.True(hasDatabase && hasOperation, "Should have both dimension tags");
                break;
            }
        }

        [Fact]
        public void GetMetricWithThreeDimensions()
        {
            var metric = this.telemetryClient.GetMetric("ApiLatency", "Endpoint", "Method", "Region");
            Assert.NotNull(metric);
            
            metric.TrackValue(200.0, "/api/users", "GET", "WestUS");
            
            this.telemetryClient.Flush();
            Assert.True(this.metricItems.Count > 0, "At least one metric should be collected");
            var collectedMetric = this.metricItems.FirstOrDefault(m => m.Name == "ApiLatency");
            Assert.NotNull(collectedMetric);
            
            // Verify all three dimensions
            foreach (var point in collectedMetric.GetMetricPoints())
            {
                Assert.Equal(1, (int)point.GetHistogramCount());
                var sum = point.GetHistogramSum();
                Assert.Equal(200.0, sum, 2);
                break;
            }
        }

        [Fact]
        public void GetMetricWithFourDimensions()
        {
            var metric = this.telemetryClient.GetMetric("CacheHit", "CacheType", "Region", "Tenant", "Environment");
            Assert.NotNull(metric);
            
            metric.TrackValue(1.0, "Redis", "WestUS", "TenantA", "Prod");
            
            this.telemetryClient.Flush();
            Assert.True(this.metricItems.Count > 0, "At least one metric should be collected");
            var collectedMetric = this.metricItems.FirstOrDefault(m => m.Name == "CacheHit");
            Assert.NotNull(collectedMetric);
            
            foreach (var point in collectedMetric.GetMetricPoints())
            {
                Assert.Equal(1, (int)point.GetHistogramCount());
                Assert.Equal(1.0, point.GetHistogramSum(), 2);
                break;
            }
        }

        [Fact]
        public void GetMetricWithMetricIdentifier()
        {
            var metricId = new Microsoft.ApplicationInsights.Metrics.MetricIdentifier(
                "MyNamespace",
                "ComplexMetric",
                "Dim1",
                "Dim2",
                "Dim3");
            
            var metric = this.telemetryClient.GetMetric(metricId);
            Assert.NotNull(metric);
            
            metric.TrackValue(75.0, "Value1", "Value2", "Value3");
            
            this.telemetryClient.Flush();
            Assert.True(this.metricItems.Count > 0, "At least one metric should be collected");
            var collectedMetric = this.metricItems.FirstOrDefault(m => m.Name == "MyNamespace-ComplexMetric" || m.Name == "ComplexMetric");
            Assert.NotNull(collectedMetric);
            
            foreach (var point in collectedMetric.GetMetricPoints())
            {
                Assert.Equal(1, (int)point.GetHistogramCount());
                Assert.Equal(75.0, point.GetHistogramSum(), 2);
                break;
            }
        }

        [Fact]
        public void TrackValueWithFiveDimensions()
        {
            var metricId = new Microsoft.ApplicationInsights.Metrics.MetricIdentifier(
                "MyNamespace",
                "FiveDimensionMetric",
                "Dim1", "Dim2", "Dim3", "Dim4", "Dim5");
            
            var metric = this.telemetryClient.GetMetric(metricId);
            Assert.NotNull(metric);
            
            // Test double overload
            metric.TrackValue(100.0, "V1", "V2", "V3", "V4", "V5");
            
            // Test object overload
            metric.TrackValue((object)200.0, "V1", "V2", "V3", "V4", "V5");
            
            this.telemetryClient.Flush();
            Assert.True(this.metricItems.Count > 0, "At least one metric should be collected");
            
            // Verify we recorded 2 values with sum = 300
            var collectedMetric = this.metricItems.FirstOrDefault(m => m.Name.Contains("FiveDimensionMetric"));
            Assert.NotNull(collectedMetric);
            
            foreach (var point in collectedMetric.GetMetricPoints())
            {
                var count = point.GetHistogramCount();
                Assert.True(count >= 2, "Should have at least 2 values");
                var sum = point.GetHistogramSum();
                Assert.True(sum >= 300.0, "Sum should be at least 300 (100 + 200)");
                break;
            }
        }

        [Fact]
        public void TrackValueWithSixDimensions()
        {
            var metricId = new Microsoft.ApplicationInsights.Metrics.MetricIdentifier(
                "MyNamespace",
                "SixDimensionMetric",
                "Dim1", "Dim2", "Dim3", "Dim4", "Dim5", "Dim6");
            
            var metric = this.telemetryClient.GetMetric(metricId);
            Assert.NotNull(metric);
            
            metric.TrackValue(100.0, "V1", "V2", "V3", "V4", "V5", "V6");
            metric.TrackValue((object)200.0, "V1", "V2", "V3", "V4", "V5", "V6");
            
            this.telemetryClient.Flush();
            
            Assert.True(this.metricItems.Count > 0, "At least one metric should be collected");
            var collectedMetric = this.metricItems.FirstOrDefault(m => m.Name.Contains("SixDimensionMetric"));
            Assert.NotNull(collectedMetric);
        }

        [Fact]
        public void TrackValueWithSevenDimensions()
        {
            var metricId = new Microsoft.ApplicationInsights.Metrics.MetricIdentifier(
                "MyNamespace",
                "SevenDimensionMetric",
                "Dim1", "Dim2", "Dim3", "Dim4", "Dim5", "Dim6", "Dim7");
            
            var metric = this.telemetryClient.GetMetric(metricId);
            Assert.NotNull(metric);
            
            metric.TrackValue(100.0, "V1", "V2", "V3", "V4", "V5", "V6", "V7");
            metric.TrackValue((object)200.0, "V1", "V2", "V3", "V4", "V5", "V6", "V7");
            
            this.telemetryClient.Flush();
            
            Assert.True(this.metricItems.Count > 0, "At least one metric should be collected");
            var collectedMetric = this.metricItems.FirstOrDefault(m => m.Name.Contains("SevenDimensionMetric"));
            Assert.NotNull(collectedMetric);
        }

        [Fact]
        public void TrackValueWithEightDimensions()
        {
            var metricId = new Microsoft.ApplicationInsights.Metrics.MetricIdentifier(
                "MyNamespace",
                "EightDimensionMetric",
                "Dim1", "Dim2", "Dim3", "Dim4", "Dim5", "Dim6", "Dim7", "Dim8");
            
            var metric = this.telemetryClient.GetMetric(metricId);
            Assert.NotNull(metric);
            
            metric.TrackValue(100.0, "V1", "V2", "V3", "V4", "V5", "V6", "V7", "V8");
            metric.TrackValue((object)200.0, "V1", "V2", "V3", "V4", "V5", "V6", "V7", "V8");
            
            this.telemetryClient.Flush();
            
            Assert.True(this.metricItems.Count > 0, "At least one metric should be collected");
            var collectedMetric = this.metricItems.FirstOrDefault(m => m.Name.Contains("EightDimensionMetric"));
            Assert.NotNull(collectedMetric);
        }

        [Fact]
        public void TrackValueWithNineDimensions()
        {
            var metricId = new Microsoft.ApplicationInsights.Metrics.MetricIdentifier(
                "MyNamespace",
                "NineDimensionMetric",
                "Dim1", "Dim2", "Dim3", "Dim4", "Dim5", "Dim6", "Dim7", "Dim8", "Dim9");
            
            var metric = this.telemetryClient.GetMetric(metricId);
            Assert.NotNull(metric);
            
            metric.TrackValue(100.0, "V1", "V2", "V3", "V4", "V5", "V6", "V7", "V8", "V9");
            metric.TrackValue((object)200.0, "V1", "V2", "V3", "V4", "V5", "V6", "V7", "V8", "V9");
            
            this.telemetryClient.Flush();
            
            Assert.True(this.metricItems.Count > 0, "At least one metric should be collected");
            var collectedMetric = this.metricItems.FirstOrDefault(m => m.Name.Contains("NineDimensionMetric"));
            Assert.NotNull(collectedMetric);
        }

        [Fact]
        public void TrackValueWithTenDimensions()
        {
            var metricId = new Microsoft.ApplicationInsights.Metrics.MetricIdentifier(
                "MyNamespace",
                "TenDimensionMetric",
                "Dim1", "Dim2", "Dim3", "Dim4", "Dim5", "Dim6", "Dim7", "Dim8", "Dim9", "Dim10");
            
            var metric = this.telemetryClient.GetMetric(metricId);
            Assert.NotNull(metric);
            
            metric.TrackValue(100.0, "V1", "V2", "V3", "V4", "V5", "V6", "V7", "V8", "V9", "V10");
            metric.TrackValue((object)200.0, "V1", "V2", "V3", "V4", "V5", "V6", "V7", "V8", "V9", "V10");
            
            this.telemetryClient.Flush();
            
            Assert.True(this.metricItems.Count > 0, "At least one metric should be collected");
            var collectedMetric = this.metricItems.FirstOrDefault(m => m.Name.Contains("TenDimensionMetric"));
            Assert.NotNull(collectedMetric);
        }

        #endregion

        // TrackTrace tests removed - not related to metrics shim implementation


        #region TrackException

        [Fact]
        public void TrackExceptionSendsExceptionTelemetryWithSpecifiedNameToProvideSimplestWayOfSendingExceptionTelemetry()
        {
            Exception ex = new Exception("Test exception message");
            this.telemetryClient.TrackException(ex);

            this.telemetryClient.Flush();

            Assert.Equal(1, this.logItems.Count);
            var logRecord = this.logItems[0];
            Assert.NotNull(logRecord.Exception);
            Assert.Same(ex, logRecord.Exception);
            Assert.Equal(LogLevel.Error, logRecord.LogLevel);
        }

        [Fact]
        public void TrackExceptionWillUseRequiredFieldAsTextForTheExceptionNameWhenTheExceptionNameIsEmptyToHideUserErrors()
        {
            this.telemetryClient.TrackException((Exception)null);

            this.telemetryClient.Flush();

            Assert.Equal(1, this.logItems.Count);
            var logRecord = this.logItems[0];
            Assert.NotNull(logRecord.Exception);
            Assert.Equal("n/a", logRecord.Exception.Message);
        }

        [Fact]
        public void TrackExceptionSendsExceptionTelemetryWithSpecifiedObjectTelemetry()
        {
            Exception ex = new Exception("Test telemetry exception");
            this.telemetryClient.TrackException(new ExceptionTelemetry(ex));

            this.telemetryClient.Flush();

            Assert.Equal(1, this.logItems.Count);
            var logRecord = this.logItems[0];
            Assert.NotNull(logRecord.Exception);
            Assert.Equal("Test telemetry exception", logRecord.Exception.Message);
        }

        [Fact]
        public void TrackExceptionWillUseABlankObjectAsTheExceptionToHideUserErrors()
        {
            this.telemetryClient.TrackException((ExceptionTelemetry)null);

            this.telemetryClient.Flush();

            Assert.Equal(1, this.logItems.Count);
            var logRecord = this.logItems[0];
            Assert.NotNull(logRecord.Exception);
        }

        [Fact]
        public void TrackExceptionUsesErrorLogLevelByDefault()
        {
            this.telemetryClient.TrackException(new Exception());

            this.telemetryClient.Flush();

            Assert.Equal(1, this.logItems.Count);
            Assert.Equal(LogLevel.Error, this.logItems[0].LogLevel);
        }

        [Fact]
        public void TrackExceptionWithExceptionTelemetryRespectsSeverityLevel()
        {
            var telemetry = new ExceptionTelemetry(new Exception("Critical error"))
            {
                SeverityLevel = SeverityLevel.Critical
            };
            this.telemetryClient.TrackException(telemetry);

            this.telemetryClient.Flush();

            Assert.Equal(1, this.logItems.Count);
            Assert.Equal(LogLevel.Critical, this.logItems[0].LogLevel);
        }

        [Fact]
        public void TrackExceptionWithPropertiesIncludesPropertiesInLogRecord()
        {
            var properties = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };

            this.telemetryClient.TrackException(new Exception("Test"), properties);

            this.telemetryClient.Flush();

            Assert.Equal(1, this.logItems.Count);
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

            Assert.True(hasKey1);
            Assert.True(hasKey2, "Property key2 should be in log record");
        }

        [Fact]
        public void TrackExceptionWithExceptionTelemetryIncludesProperties()
        {
            var telemetry = new ExceptionTelemetry(new Exception("Test exception"));
            telemetry.Properties["customKey"] = "customValue";
            
            this.telemetryClient.TrackException(telemetry);

            this.telemetryClient.Flush();

            Assert.Equal(1, this.logItems.Count);
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

            Assert.True(hasCustomKey);
        }

        [Fact]
        public void TrackExceptionWithInnerExceptionPreservesInnerException()
        {
            var innerException = new InvalidOperationException("Inner exception message");
            var outerException = new ApplicationException("Outer exception message", innerException);
            
            this.telemetryClient.TrackException(outerException);

            this.telemetryClient.Flush();

            Assert.Equal(1, this.logItems.Count);
            var logRecord = this.logItems[0];
            Assert.NotNull(logRecord.Exception);
            Assert.Equal("Outer exception message", logRecord.Exception.Message);
            
            // The exception should have inner exception
            Assert.NotNull(logRecord.Exception.InnerException);
            Assert.Equal("Inner exception message", logRecord.Exception.InnerException.Message);
        }

        [Fact]
        public void TrackExceptionDoesNotMutatePropertiesDictionary()
        {
            var properties = new Dictionary<string, string> { { "key1", "value1" } };
            var originalCount = properties.Count;
            var exception = new InvalidOperationException("Test");

            this.telemetryClient.TrackException(exception, properties);
            this.telemetryClient.Flush();

            Assert.Equal(originalCount, properties.Count);
        }

        [Fact]
        public void TrackExceptionAcceptsReadOnlyDictionary()
        {
            var inner = new Dictionary<string, string> { { "key1", "value1" } };
            var readOnly = new ReadOnlyDictionary<string, string>(inner);
            var exception = new InvalidOperationException("Test");

            // Must not throw NotSupportedException
            this.telemetryClient.TrackException(exception, readOnly);
            this.telemetryClient.Flush();

            Assert.True(this.logItems.Count > 0, "Log should be collected");
        }

        [Fact]
        public void TrackExceptionWithExceptionTelemetryDoesNotMutateProperties()
        {
            var telemetry = new ExceptionTelemetry(new InvalidOperationException("Test"));
            telemetry.Properties["userProp"] = "userValue";
            var originalCount = telemetry.Properties.Count;

            this.telemetryClient.TrackException(telemetry);
            this.telemetryClient.Flush();

            Assert.Equal(originalCount, telemetry.Properties.Count);
            Assert.False(telemetry.Properties.ContainsKey("microsoft.client.ip"),
                "Internal attributes should not leak into telemetry's Properties");
        }

        #endregion

        #region TrackRequest

        [Fact]
        public void TrackRequestCreatesActivityWithCorrectName()
        {
            var request = new RequestTelemetry("GET /api/users", DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(100), "200", true);
            
            this.telemetryClient.TrackRequest(request);
            this.telemetryClient.Flush();

            // Verify activity was created
            Assert.Equal(1, this.activityItems.Count);
            Assert.Equal("GET /api/users", this.activityItems[0].DisplayName);
            Assert.Equal(ActivityKind.Server, this.activityItems[0].Kind);
        }

        [Fact]
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

            Assert.Equal(1, this.activityItems.Count);
            var activity = this.activityItems[0];
            
            // Verify Consumer ActivityKind for messaging
            Assert.Equal(ActivityKind.Consumer, activity.Kind);
            Assert.Equal(ActivityStatusCode.Ok, activity.Status);
            
            // Verify override attributes preserve original AI values
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.request.name" && t.Value == "ServiceBus Message"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.request.url" && t.Value == "sb://myservicebus.servicebus.windows.net/mytopic"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.request.source" && t.Value == "mytopic"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.request.resultCode" && t.Value == "0"));
        }

        [Fact]
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

            Assert.Equal(1, this.activityItems.Count);
            var activity = this.activityItems[0];
            
            // Verify override attributes
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.request.name" && t.Value == "Custom Request"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.request.url" && t.Value == "https://api.example.com/v1/resource"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.request.source" && t.Value == "mobile-app"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.request.resultCode" && t.Value == "201"));
            Assert.Equal(ActivityKind.Server, activity.Kind);
            Assert.Equal(ActivityStatusCode.Ok, activity.Status);
        }

        [Fact]
        public void TrackRequestWithPropertiesIncludesCustomProperties()
        {
            this.activityItems.Clear();

            var request = new RequestTelemetry("Test Request", DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(100), "200", true);
            request.Properties["userId"] = "12345";
            request.Properties["region"] = "us-west";
            
            this.telemetryClient.TrackRequest(request);
            this.telemetryClient.Flush();

            Assert.Equal(1, activityItems.Count);
            var activity = activityItems[0];
            
            // Verify custom properties are included
            Assert.Equal("userId", activity.Tags.FirstOrDefault(t => t.Key == "userId").Key);
            Assert.Equal("12345", activity.Tags.FirstOrDefault(t => t.Key == "userId").Value);
            Assert.Equal("region", activity.Tags.FirstOrDefault(t => t.Key == "region").Key);
            Assert.Equal("us-west", activity.Tags.FirstOrDefault(t => t.Key == "region").Value);
            
            // Verify standard attributes
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.request.name" && t.Value == "Test Request"));
            Assert.Equal(ActivityKind.Server, activity.Kind);
        }

        [Fact]
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

            Assert.Equal(1, this.activityItems.Count);
            var activity = this.activityItems[0];
            
            // Verify error status
            Assert.Equal(ActivityStatusCode.Error, activity.Status);
            Assert.Equal(ActivityKind.Server, activity.Kind);
            
            // Verify override attributes
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.request.name" && t.Value == "GET /api/error"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.request.url" && t.Value == "https://example.com/api/error"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.request.resultCode" && t.Value == "500"));
        }

        [Fact]
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

            Assert.Equal(1, this.activityItems.Count);
            var activity = this.activityItems[0];
            
            // Verify Consumer ActivityKind for queue messaging
            Assert.Equal(ActivityKind.Consumer, activity.Kind);
            Assert.Equal(ActivityStatusCode.Ok, activity.Status);
            
            // Verify override attributes
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.request.name" && t.Value == "Queue Message Handler"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.request.source" && t.Value == "orders-queue"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.request.resultCode" && t.Value == "0"));
        }

        [Fact]
        public void TrackRequestHandlesNullRequestTelemetryGracefully()
        {
            this.telemetryClient.TrackRequest((RequestTelemetry)null);
            this.telemetryClient.Flush();

            // Should not throw exception and should not create any activity
            Assert.Equal(0, this.activityItems.Count);
        }

        #endregion

        #region TrackDependency

        [Fact]
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

            Assert.Equal(1, this.activityItems.Count);
            var activity = this.activityItems[0];
            
            // Verify Producer ActivityKind
            Assert.Equal(ActivityKind.Producer, activity.Kind);
            Assert.Equal(ActivityStatusCode.Ok, activity.Status);
            
            // Verify override attributes
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.dependency.type" && t.Value == "Azure Service Bus"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.dependency.target" && t.Value == "myservicebus.servicebus.windows.net/mytopic"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.dependency.name" && t.Value == "Publish event"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.dependency.data" && t.Value == "sb://myservicebus.servicebus.windows.net/mytopic"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.dependency.resultCode" && t.Value == "0"));
        }

        [Fact]
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

            Assert.Equal(1, this.activityItems.Count);
            var activity = this.activityItems[0];
            
            // Verify Internal ActivityKind for InProc
            Assert.Equal(ActivityKind.Internal, activity.Kind);
            Assert.Equal(ActivityStatusCode.Ok, activity.Status);
            
            // Verify override attributes preserve Azure namespace in type
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.dependency.type" && t.Value == "InProc | Microsoft.Storage"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.dependency.target" && t.Value == "mystorageaccount.blob.core.windows.net"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.dependency.name" && t.Value == "Upload blob"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.dependency.data" && t.Value == "PUT /container/blob.txt"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.dependency.resultCode" && t.Value == "201"));
        }

        [Fact]
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

            Assert.Equal(1, this.activityItems.Count);
            var activity = this.activityItems[0];
            
            // Verify ActivityKind and Status
            Assert.Equal(ActivityKind.Client, activity.Kind);
            Assert.Equal(ActivityStatusCode.Ok, activity.Status);
            
            // Verify override attributes
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.dependency.type" && t.Value == "Http"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.dependency.target" && t.Value == "api.example.com"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.dependency.name" && t.Value == "POST /orders"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.dependency.data" && t.Value == "https://api.example.com/orders"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.dependency.resultCode" && t.Value == "201"));
        }

        [Fact]
        public void TrackDependencyWithPropertiesIncludesCustomProperties()
        {
            this.activityItems.Clear();

            var dependency = new DependencyTelemetry("redis", "cache.example.com", "GET user:123", "GET user:123", DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(5), "200", true);
            dependency.Properties["operation"] = "query";
            dependency.Properties["cacheHit"] = "false";
            
            this.telemetryClient.TrackDependency(dependency);
            this.telemetryClient.Flush();

            Assert.Equal(1, activityItems.Count);
            var activity = activityItems[0];
            
            // Verify custom properties are included
            Assert.Equal("operation", activity.Tags.FirstOrDefault(t => t.Key == "operation").Key);
            Assert.Equal("query", activity.Tags.FirstOrDefault(t => t.Key == "operation").Value);
            Assert.Equal("cacheHit", activity.Tags.FirstOrDefault(t => t.Key == "cacheHit").Key);
            Assert.Equal("false", activity.Tags.FirstOrDefault(t => t.Key == "cacheHit").Value);
            
            // Verify standard attributes
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.dependency.type" && t.Value == "redis"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.dependency.target" && t.Value == "cache.example.com"));
            Assert.Equal(ActivityKind.Client, activity.Kind);
        }

        [Fact]
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

            Assert.Equal(1, this.activityItems.Count);
            var activity = this.activityItems[0];
            
            // Verify error status
            Assert.Equal(ActivityStatusCode.Error, activity.Status);
            Assert.Equal(ActivityKind.Client, activity.Kind);
            
            // Verify override attributes
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.dependency.type" && t.Value == "Http"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.dependency.data" && t.Value == "https://api.example.com/error"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.dependency.resultCode" && t.Value == "500"));
        }

        [Fact]
        public void TrackDependencyHandlesNullDependencyTelemetryGracefully()
        {
            this.telemetryClient.TrackDependency((DependencyTelemetry)null);
            this.telemetryClient.Flush();

            // Should not throw exception and should not create any activity
            Assert.Equal(0, this.activityItems.Count);
        }

        [Fact]
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

            Assert.Equal(1, this.activityItems.Count);
            var activity = this.activityItems[0];
            
            // Verify ActivityKind detection works regardless of case
            Assert.Equal(ActivityKind.Client, activity.Kind);
            Assert.Equal(ActivityStatusCode.Ok, activity.Status);
            
            // Verify override attributes preserve original case
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.dependency.type" && t.Value == "HTTP"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.dependency.data" && t.Value == "https://api.example.com/data"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.dependency.target" && t.Value == "api.example.com"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.dependency.resultCode" && t.Value == "200"));
        }

        [Fact]
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

            Assert.Equal(1, this.activityItems.Count);
            var activity = this.activityItems[0];
            
            // Verify ActivityKind detection works regardless of case
            Assert.Equal(ActivityKind.Client, activity.Kind);
            Assert.Equal(ActivityStatusCode.Ok, activity.Status);
            
            // Verify override attributes preserve original case
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.dependency.type" && t.Value == "sql"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.dependency.data" && t.Value == "SELECT COUNT(*) FROM Orders"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.dependency.target" && t.Value == "localhost | testdb"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.dependency.resultCode" && t.Value == "0"));
        }

        #endregion

        #region GlobalProperties

        [Fact]
        public void TrackEvent_IncludesGlobalProperties()
        {
            // Arrange
            this.telemetryClient.Context.GlobalProperties["Environment"] = "Test";
            this.telemetryClient.Context.GlobalProperties["Version"] = "1.0";

            // Act
            this.telemetryClient.TrackEvent("TestEvent");
            this.telemetryClient.Flush();

            // Assert
            Assert.True(this.logItems.Count > 0);
            var logRecord = this.logItems.FirstOrDefault(l => 
                l.Attributes != null && l.Attributes.Any(a => 
                    a.Key == "microsoft.custom_event.name" && a.Value?.ToString() == "TestEvent"));
            Assert.NotNull(logRecord);
            
            var attributes = logRecord.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());
            Assert.True(attributes.ContainsKey("Environment"));
            Assert.Equal("Test", attributes["Environment"]);
            Assert.True(attributes.ContainsKey("Version"));
            Assert.Equal("1.0", attributes["Version"]);
        }

        [Fact]
        public void TrackEvent_ItemPropertiesOverrideGlobalProperties()
        {
            // Arrange
            this.telemetryClient.Context.GlobalProperties["Environment"] = "Test";
            this.telemetryClient.Context.GlobalProperties["Version"] = "1.0";

            // Act - override Environment with item property
            this.telemetryClient.TrackEvent("TestEvent", new Dictionary<string, string>
            {
                { "Environment", "Production" },  // Override
                { "CustomProp", "CustomValue" }
            });
            this.telemetryClient.Flush();

            // Assert
            var logRecord = this.logItems.FirstOrDefault(l => 
                l.Attributes != null && l.Attributes.Any(a => 
                    a.Key == "microsoft.custom_event.name" && a.Value?.ToString() == "TestEvent"));
            Assert.NotNull(logRecord);
            
            var attributes = logRecord.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());
            Assert.Equal("Production", attributes["Environment"]);
            Assert.Equal("1.0", attributes["Version"]);
            Assert.Equal("CustomValue", attributes["CustomProp"]);
        }

        [Fact]
        public void TrackTrace_IncludesGlobalProperties()
        {
            // Arrange
            this.telemetryClient.Context.GlobalProperties["Component"] = "Auth";
            this.telemetryClient.Context.GlobalProperties["DataCenter"] = "WestUS";

            // Act
            this.telemetryClient.TrackTrace("Test message");
            this.telemetryClient.Flush();

            // Assert
            Assert.True(this.logItems.Count > 0);
            var logRecord = this.logItems.Last();
            var attributes = logRecord.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());
            
            Assert.True(attributes.ContainsKey("Component"));
            Assert.Equal("Auth", attributes["Component"]);
            Assert.True(attributes.ContainsKey("DataCenter"));
            Assert.Equal("WestUS", attributes["DataCenter"]);
        }

        [Fact]
        public void TrackTrace_WithSeverity_IncludesGlobalProperties()
        {
            // Arrange
            this.telemetryClient.Context.GlobalProperties["RequestId"] = "req-123";

            // Act
            this.telemetryClient.TrackTrace("Warning message", SeverityLevel.Warning);
            this.telemetryClient.Flush();

            // Assert
            var logRecord = this.logItems.FirstOrDefault(l => l.LogLevel == LogLevel.Warning);
            Assert.NotNull(logRecord);
            var attributes = logRecord.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());
            Assert.Equal("req-123", attributes["RequestId"]);
        }

        [Fact]
        public void TrackException_IncludesGlobalProperties()
        {
            // Arrange
            this.telemetryClient.Context.GlobalProperties["UserId"] = "user-456";
            this.telemetryClient.Context.GlobalProperties["SessionId"] = "session-789";

            // Act
            var exception = new InvalidOperationException("Test exception");
            this.telemetryClient.TrackException(exception);
            this.telemetryClient.Flush();

            // Assert
            var logRecord = this.logItems.FirstOrDefault(l => l.LogLevel == LogLevel.Error);
            Assert.NotNull(logRecord);
            var attributes = logRecord.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());
            Assert.Equal("user-456", attributes["UserId"]);
            Assert.Equal("session-789", attributes["SessionId"]);
        }

        [Fact]
        public void TrackException_WithProperties_MergesWithGlobalProperties()
        {
            // Arrange
            this.telemetryClient.Context.GlobalProperties["AppVersion"] = "2.0";

            // Act
            var exception = new InvalidOperationException("Test");
            this.telemetryClient.TrackException(exception, new Dictionary<string, string>
            {
                { "Operation", "Payment" },
                { "AppVersion", "2.1" }  // Override global
            });
            this.telemetryClient.Flush();

            // Assert
            var logRecord = this.logItems.FirstOrDefault(l => l.LogLevel == LogLevel.Error);
            Assert.NotNull(logRecord);
            var attributes = logRecord.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());
            Assert.Equal("2.1", attributes["AppVersion"]);
            Assert.Equal("Payment", attributes["Operation"]);
        }

        [Fact]
        public void TrackDependency_IncludesGlobalPropertiesAsTags()
        {
            // Arrange
            this.telemetryClient.Context.GlobalProperties["TenantId"] = "tenant-123";
            this.telemetryClient.Context.GlobalProperties["Region"] = "US-West";

            // Act
            var dependency = new DependencyTelemetry
            {
                Type = "HTTP",
                Name = "GET /api/data",
                Data = "https://api.example.com/data",
                Target = "api.example.com",
                Duration = TimeSpan.FromMilliseconds(100),
                Success = true
            };
            this.telemetryClient.TrackDependency(dependency);
            this.telemetryClient.Flush();

            // Assert
            Assert.Equal(1, this.activityItems.Count);
            var activity = this.activityItems[0];
            Assert.True(activity.Tags.Any(t => t.Key == "TenantId" && t.Value == "tenant-123"));
            Assert.True(activity.Tags.Any(t => t.Key == "Region" && t.Value == "US-West"));
        }

        [Fact]
        public void TrackDependency_ItemPropertiesOverrideGlobalProperties()
        {
            // Arrange
            this.telemetryClient.Context.GlobalProperties["Environment"] = "Dev";
            this.telemetryClient.Context.GlobalProperties["BuildId"] = "100";

            // Act
            var dependency = new DependencyTelemetry
            {
                Type = "SQL",
                Name = "Query",
                Data = "SELECT * FROM Users",
                Duration = TimeSpan.FromMilliseconds(50),
                Success = true
            };
            dependency.Properties["Environment"] = "Staging";  // Override
            dependency.Properties["QueryType"] = "Select";
            
            this.telemetryClient.TrackDependency(dependency);
            this.telemetryClient.Flush();

            // Assert
            var activity = this.activityItems[0];
            Assert.True(activity.Tags.Any(t => t.Key == "Environment" && t.Value == "Staging"), 
                "Item property should override global");
            Assert.True(activity.Tags.Any(t => t.Key == "BuildId" && t.Value == "100"),
                "Non-overridden global should remain");
            Assert.True(activity.Tags.Any(t => t.Key == "QueryType" && t.Value == "Select"));
        }

        [Fact]
        public void TrackRequest_IncludesGlobalPropertiesAsTags()
        {
            // Arrange
            this.telemetryClient.Context.GlobalProperties["ServiceName"] = "WebAPI";
            this.telemetryClient.Context.GlobalProperties["InstanceId"] = "instance-1";

            // Act
            var request = new RequestTelemetry
            {
                Name = "GET /users",
                Duration = TimeSpan.FromMilliseconds(75),
                ResponseCode = "200",
                Success = true
            };
            this.telemetryClient.TrackRequest(request);
            this.telemetryClient.Flush();

            // Assert
            Assert.Equal(1, this.activityItems.Count);
            var activity = this.activityItems[0];
            Assert.True(activity.Tags.Any(t => t.Key == "ServiceName" && t.Value == "WebAPI"));
            Assert.True(activity.Tags.Any(t => t.Key == "InstanceId" && t.Value == "instance-1"));
        }

        [Fact]
        public void TrackRequest_ItemPropertiesOverrideGlobalProperties()
        {
            // Arrange
            this.telemetryClient.Context.GlobalProperties["Controller"] = "Default";
            this.telemetryClient.Context.GlobalProperties["Version"] = "1.0";

            // Act
            var request = new RequestTelemetry
            {
                Name = "POST /orders",
                Duration = TimeSpan.FromMilliseconds(120),
                ResponseCode = "201",
                Success = true
            };
            request.Properties["Controller"] = "OrderController";  // Override
            request.Properties["Action"] = "Create";
            
            this.telemetryClient.TrackRequest(request);
            this.telemetryClient.Flush();

            // Assert
            var activity = this.activityItems[0];
            Assert.True(activity.Tags.Any(t => t.Key == "Controller" && t.Value == "OrderController"),
                "Item property should override global");
            Assert.True(activity.Tags.Any(t => t.Key == "Version" && t.Value == "1.0"),
                "Non-overridden global should remain");
            Assert.True(activity.Tags.Any(t => t.Key == "Action" && t.Value == "Create"));
        }

        [Fact]
        public void GlobalProperties_EmptyDictionary_DoesNotCauseErrors()
        {
            // Arrange - Don't add any global properties
            
            // Act & Assert - Should not throw
            this.telemetryClient.TrackEvent("TestEvent");
            this.telemetryClient.TrackTrace("Test message");
            this.telemetryClient.TrackException(new Exception("Test"));
            this.telemetryClient.Flush();

            // Verify telemetry was recorded
            Assert.True(this.logItems.Count >= 3);
        }

        [Fact]
        public void TelemetryContextGlobalProperties_OverrideClientGlobalProperties()
        {
            // Arrange
            this.telemetryClient.Context.GlobalProperties["Source"] = "Client";
            
            var eventTelemetry = new EventTelemetry("TestEvent");
            eventTelemetry.Context.GlobalProperties["Source"] = "Telemetry";  // Should override
            eventTelemetry.Context.GlobalProperties["Extra"] = "Value";

            // Act
            this.telemetryClient.TrackEvent(eventTelemetry);
            this.telemetryClient.Flush();

            // Assert
            var logRecord = this.logItems.FirstOrDefault(l => 
                l.Attributes != null && l.Attributes.Any(a => 
                    a.Key == "microsoft.custom_event.name" && a.Value?.ToString() == "TestEvent"));
            Assert.NotNull(logRecord);
            var attributes = logRecord.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());
            Assert.Equal("Telemetry", attributes["Source"]);
            Assert.Equal("Value", attributes["Extra"]);
        }

        #endregion

        #region Context Properties (User, Location, Operation)

        [Fact]
        public void TrackEvent_IncludesUserContextAsEnduserIdAttribute()
        {
            // Arrange
            var eventTelemetry = new EventTelemetry("UserAction");
            eventTelemetry.Context.User.Id = "user-12345";

            // Act
            this.telemetryClient.TrackEvent(eventTelemetry);
            this.telemetryClient.Flush();

            // Assert
            var logRecord = this.logItems.FirstOrDefault(l => 
                l.Attributes != null && l.Attributes.Any(a => 
                    a.Key == "microsoft.custom_event.name" && a.Value?.ToString() == "UserAction"));
            Assert.NotNull(logRecord);
            var attributes = logRecord.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());
            Assert.True(attributes.ContainsKey("enduser.pseudo.id"));
            Assert.Equal("user-12345", attributes["enduser.pseudo.id"]);
        }

        [Fact]
        public void TrackEvent_IncludesLocationContextAsClientIpAttribute()
        {
            // Arrange
            var eventTelemetry = new EventTelemetry("PageView");
            eventTelemetry.Context.Location.Ip = "192.168.1.1";

            // Act
            this.telemetryClient.TrackEvent(eventTelemetry);
            this.telemetryClient.Flush();

            // Assert
            var logRecord = this.logItems.FirstOrDefault(l => 
                l.Attributes != null && l.Attributes.Any(a => 
                    a.Key == "microsoft.custom_event.name" && a.Value?.ToString() == "PageView"));
            Assert.NotNull(logRecord);
            var attributes = logRecord.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());
            Assert.True(attributes.ContainsKey("microsoft.client.ip"));
            Assert.Equal("192.168.1.1", attributes["microsoft.client.ip"]);
        }

        [Fact]
        public void TrackTrace_IncludesUserAndLocationContext()
        {
            // Arrange
            var traceTelemetry = new TraceTelemetry("Trace message");
            traceTelemetry.Context.User.Id = "user-999";
            traceTelemetry.Context.Location.Ip = "10.0.0.1";

            // Act
            this.telemetryClient.TrackTrace(traceTelemetry);
            this.telemetryClient.Flush();

            // Assert
            var logRecord = this.logItems.Last();
            var attributes = logRecord.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());
            Assert.Equal("user-999", attributes["enduser.pseudo.id"]);
            Assert.Equal("10.0.0.1", attributes["microsoft.client.ip"]);
        }

        [Fact]
        public void TrackException_IncludesUserAndLocationContext()
        {
            // Arrange
            var exceptionTelemetry = new ExceptionTelemetry(new Exception("Test"));
            exceptionTelemetry.Context.User.Id = "user-abc";
            exceptionTelemetry.Context.Location.Ip = "172.16.0.1";

            // Act
            this.telemetryClient.TrackException(exceptionTelemetry);
            this.telemetryClient.Flush();

            // Assert
            var logRecord = this.logItems.FirstOrDefault(l => l.LogLevel == LogLevel.Error);
            Assert.NotNull(logRecord);
            var attributes = logRecord.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());
            Assert.Equal("user-abc", attributes["enduser.pseudo.id"]);
            Assert.Equal("172.16.0.1", attributes["microsoft.client.ip"]);
        }

        [Fact]
        public void TrackDependency_IncludesUserAndLocationContextAsTags()
        {
            // Arrange
            var dependency = new DependencyTelemetry
            {
                Type = "HTTP",
                Name = "GET /api",
                Duration = TimeSpan.FromMilliseconds(100),
                Success = true
            };
            dependency.Context.User.Id = "user-dep-123";
            dependency.Context.Location.Ip = "192.168.100.50";

            // Act
            this.telemetryClient.TrackDependency(dependency);
            this.telemetryClient.Flush();

            // Assert
            Assert.Equal(1, this.activityItems.Count);
            var activity = this.activityItems[0];
            Assert.True(activity.Tags.Any(t => t.Key == "enduser.pseudo.id" && t.Value == "user-dep-123"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.client.ip" && t.Value == "192.168.100.50"));
        }

        [Fact]
        public void TrackRequest_IncludesUserAndLocationContextAsTags()
        {
            // Arrange
            var request = new RequestTelemetry
            {
                Name = "POST /api/orders",
                Duration = TimeSpan.FromMilliseconds(200),
                ResponseCode = "200",
                Success = true
            };
            request.Context.User.Id = "user-req-456";
            request.Context.Location.Ip = "203.0.113.1";

            // Act
            this.telemetryClient.TrackRequest(request);
            this.telemetryClient.Flush();

            // Assert
            Assert.Equal(1, this.activityItems.Count);
            var activity = this.activityItems[0];
            Assert.True(activity.Tags.Any(t => t.Key == "enduser.pseudo.id" && t.Value == "user-req-456"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.client.ip" && t.Value == "203.0.113.1"));
        }

        [Fact]
        public void TrackRequest_IncludesOperationNameAsOverrideAttribute()
        {
            // Arrange
            var request = new RequestTelemetry
            {
                Name = "GET /api/users",
                Duration = TimeSpan.FromMilliseconds(50),
                ResponseCode = "200",
                Success = true
            };
            request.Context.Operation.Name = "GetAllUsers";

            // Act
            this.telemetryClient.TrackRequest(request);
            this.telemetryClient.Flush();

            // Assert
            Assert.Equal(1, this.activityItems.Count);
            var activity = this.activityItems[0];
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.operation_name" && t.Value == "GetAllUsers"));
        }

        [Fact]
        public void ContextProperties_NullValues_DoNotCauseErrors()
        {
            // Arrange
            var eventTelemetry = new EventTelemetry("Test");
            eventTelemetry.Context.User.Id = null;
            eventTelemetry.Context.Location.Ip = null;

            // Act & Assert - Should not throw
            this.telemetryClient.TrackEvent(eventTelemetry);
            this.telemetryClient.Flush();

            // Verify telemetry was recorded
            Assert.True(this.logItems.Count > 0);
        }

        [Fact]
        public void TrackEvent_UserId_MapsToEnduserPseudoId()
        {
            // Arrange
            var eventTelemetry = new EventTelemetry("TestEvent");
            eventTelemetry.Context.User.Id = "anonymous-user-123";

            // Act
            this.telemetryClient.TrackEvent(eventTelemetry);
            this.telemetryClient.Flush();

            // Assert
            var logRecord = this.logItems.FirstOrDefault(l => l.Attributes != null && l.Attributes.Any(a => a.Key == "microsoft.custom_event.name"));
            Assert.NotNull(logRecord);
            var attributes = logRecord.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());
            Assert.True(attributes.ContainsKey("enduser.pseudo.id"));
            Assert.Equal("anonymous-user-123", attributes["enduser.pseudo.id"]);
        }

        [Fact]
        public void TrackException_AuthenticatedUserId_MapsToEnduserId()
        {
            // Arrange
            var exception = new ExceptionTelemetry(new InvalidOperationException("Test"));
            exception.Context.User.AuthenticatedUserId = "authenticated-user-456";

            // Act
            this.telemetryClient.TrackException(exception);
            this.telemetryClient.Flush();

            // Assert
            var logRecord = this.logItems.FirstOrDefault(l => l.LogLevel == LogLevel.Error);
            Assert.NotNull(logRecord);
            var attributes = logRecord.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());
            Assert.True(attributes.ContainsKey("enduser.id"));
            Assert.Equal("authenticated-user-456", attributes["enduser.id"]);
        }

        [Fact]
        public void TrackRequest_UserAgent_MapsToUserAgentOriginal()
        {
            // Arrange
            var request = new RequestTelemetry
            {
                Name = "GET /api/test",
                Duration = TimeSpan.FromMilliseconds(100),
                ResponseCode = "200",
                Success = true
            };
            request.Context.User.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";

            // Act
            this.telemetryClient.TrackRequest(request);
            this.telemetryClient.Flush();

            // Assert
            Assert.Equal(1, this.activityItems.Count);
            var activity = this.activityItems[0];
            Assert.True(activity.Tags.Any(t => t.Key == "user_agent.original" && t.Value == "Mozilla/5.0 (Windows NT 10.0; Win64; x64)"));
        }

        [Fact]
        public void TrackDependency_UserAgent_DoesNotMapToUserAgentOriginal()
        {
            // Arrange
            var dependency = new DependencyTelemetry
            {
                Type = "HTTP",
                Name = "GET /external",
                Duration = TimeSpan.FromMilliseconds(50),
                Success = true
            };
            dependency.Context.User.UserAgent = "TestAgent/1.0";

            // Act
            this.telemetryClient.TrackDependency(dependency);
            this.telemetryClient.Flush();

            // Assert - UserAgent should NOT be mapped for non-request telemetry
            Assert.Equal(1, this.activityItems.Count);
            var activity = this.activityItems[0];
            Assert.False(activity.Tags.Any(t => t.Key == "user_agent.original"));
        }

        [Fact]
        public void TrackEvent_UserAgent_DoesNotMapToUserAgentOriginal()
        {
            // Arrange
            var eventTelemetry = new EventTelemetry("TestEvent");
            eventTelemetry.Context.User.UserAgent = "TestAgent/2.0";

            // Act
            this.telemetryClient.TrackEvent(eventTelemetry);
            this.telemetryClient.Flush();

            // Assert - UserAgent should NOT be mapped for events
            var logRecord = this.logItems.FirstOrDefault(l => l.Attributes != null && l.Attributes.Any(a => a.Key == "microsoft.custom_event.name"));
            Assert.NotNull(logRecord);
            var attributes = logRecord.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());
            Assert.False(attributes.ContainsKey("user_agent.original"));
        }

        [Fact]
        public void TrackRequest_BothUserIdAndAuthenticatedUserId_MapsToBothAttributes()
        {
            // Arrange
            var request = new RequestTelemetry
            {
                Name = "POST /api/secure",
                Duration = TimeSpan.FromMilliseconds(150),
                ResponseCode = "201",
                Success = true
            };
            request.Context.User.Id = "anonymous-789";
            request.Context.User.AuthenticatedUserId = "auth-user-789";

            // Act
            this.telemetryClient.TrackRequest(request);
            this.telemetryClient.Flush();

            // Assert
            Assert.Equal(1, this.activityItems.Count);
            var activity = this.activityItems[0];
            Assert.True(activity.Tags.Any(t => t.Key == "enduser.pseudo.id" && t.Value == "anonymous-789"));
            Assert.True(activity.Tags.Any(t => t.Key == "enduser.id" && t.Value == "auth-user-789"));
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

