namespace Microsoft.ApplicationInsights.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Extensions.Logging;
    using OpenTelemetry;
    using OpenTelemetry.Logs;
    using OpenTelemetry.Metrics;
    using OpenTelemetry.Trace;
    using Xunit;

    /// <summary>
    /// Tests for DisableTelemetry property in TelemetryConfiguration.
    /// Verifies that when DisableTelemetry is set to true, the OTEL_SDK_DISABLED environment variable
    /// is set and telemetry data does not flow through the OpenTelemetry SDK.
    /// </summary>
    [Collection("TelemetryClientTests")]
    public class TelemetryConfigurationOtelSdkDisabledTests
    {
        private const string OtelSdkDisabledEnvVar = "OTEL_SDK_DISABLED";

        [Fact]
        public void WhenDisableTelemetryIsTrue_TelemetryDataIsNotExported()
        {
            // Save original value to restore after test
            var originalEnvValue = Environment.GetEnvironmentVariable(OtelSdkDisabledEnvVar);

            try
            {
                // Arrange
                var logItems = new List<LogRecord>();
                var activityItems = new List<Activity>();
                var configuration = new TelemetryConfiguration();
                configuration.ConnectionString = "InstrumentationKey=" + Guid.NewGuid().ToString();
                configuration.DisableTelemetry = true;
                configuration.ConfigureOpenTelemetryBuilder(b => b
                    .WithLogging(l => l.AddInMemoryExporter(logItems))
                    .WithTracing(t => t.AddInMemoryExporter(activityItems)));

                var telemetryClient = new TelemetryClient(configuration);

                // Act
                telemetryClient.TrackEvent("TestEvent");
                telemetryClient.TrackTrace("TestTrace");
                telemetryClient.TrackDependency("HTTP", "example.com", "GET /api", DateTimeOffset.Now, TimeSpan.FromMilliseconds(100), true);
                telemetryClient.Flush();

                // Assert - No data should be exported when telemetry is disabled
                Assert.Empty(logItems);
                Assert.Empty(activityItems);
                Assert.Equal("true", Environment.GetEnvironmentVariable(OtelSdkDisabledEnvVar));

                // Cleanup
                configuration.Dispose();
            }
            finally
            {
                // Restore original environment variable value
                Environment.SetEnvironmentVariable(OtelSdkDisabledEnvVar, originalEnvValue);
            }
        }

        [Fact]
        public void WhenDisableTelemetryIsFalse_TelemetryDataIsExported()
        {
            // Save original value to restore after test
            var originalEnvValue = Environment.GetEnvironmentVariable(OtelSdkDisabledEnvVar);

            try
            {
                // Arrange
                var logItems = new List<LogRecord>();
                var configuration = new TelemetryConfiguration();
                configuration.SamplingRatio = 1.0f;
                configuration.ConnectionString = "InstrumentationKey=" + Guid.NewGuid().ToString();
                configuration.DisableTelemetry = false;
                configuration.ConfigureOpenTelemetryBuilder(b => b
                    .WithLogging(l => l.AddInMemoryExporter(logItems)));

                var telemetryClient = new TelemetryClient(configuration);

                // Act
                telemetryClient.TrackEvent("TestEvent");
                telemetryClient.Flush();

                // Assert - Data should be exported when telemetry is enabled
                Assert.NotEmpty(logItems);
                var logRecord = logItems.FirstOrDefault(l =>
                    l.Attributes != null && l.Attributes.Any(a =>
                        a.Key == "microsoft.custom_event.name" && a.Value?.ToString() == "TestEvent"));
                Assert.NotNull(logRecord);

                // Cleanup
                configuration.Dispose();
            }
            finally
            {
                // Restore original environment variable value
                Environment.SetEnvironmentVariable(OtelSdkDisabledEnvVar, originalEnvValue);
            }
        }
    }
}
