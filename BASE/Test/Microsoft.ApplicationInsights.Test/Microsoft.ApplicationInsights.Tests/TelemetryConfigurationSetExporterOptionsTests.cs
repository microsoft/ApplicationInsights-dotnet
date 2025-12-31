namespace Microsoft.ApplicationInsights.Tests
{
    using System;
    using System.Reflection;
    using Azure.Core;
    using Azure.Monitor.OpenTelemetry.Exporter;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using OpenTelemetry;
    using Xunit;

    /// <summary>
    /// Tests for Azure Monitor Exporter configuration methods in TelemetryConfiguration.
    /// </summary>
    public class TelemetryConfigurationSetExporterOptionsTests : IDisposable
    {
        private TelemetryConfiguration telemetryConfiguration;

        public TelemetryConfigurationSetExporterOptionsTests()
        {
            this.telemetryConfiguration = new TelemetryConfiguration();
        }

        public void Dispose()
        {
            this.telemetryConfiguration?.Dispose();
        }

        /// <summary>
        /// Helper method to get AzureMonitorExporterOptions from the built OpenTelemetrySdk.
        /// </summary>
        private AzureMonitorExporterOptions GetExporterOptions(OpenTelemetrySdk sdk)
        {
            // Use reflection to access the internal Services property
            var servicesProperty = typeof(OpenTelemetrySdk).GetProperty(
                "Services",
                BindingFlags.NonPublic | BindingFlags.Instance);

            var serviceProvider = servicesProperty?.GetValue(sdk) as IServiceProvider;
            Assert.NotNull(serviceProvider);

            var options = serviceProvider.GetService<IOptions<AzureMonitorExporterOptions>>();
            Assert.NotNull(options);

            return options.Value;
        }

        /// <summary>
        /// Helper method to build the configuration by creating a TelemetryClient.
        /// Creating a TelemetryClient triggers the Build() method internally.
        /// </summary>
        private OpenTelemetrySdk BuildConfiguration()
        {
            // Creating a TelemetryClient triggers configuration.Build() internally
            var client = new TelemetryClient(this.telemetryConfiguration);

            // Access the private 'sdk' field from TelemetryClient via reflection
            var sdkField = typeof(TelemetryClient).GetField(
                "sdk",
                BindingFlags.NonPublic | BindingFlags.Instance);

            return sdkField?.GetValue(client) as OpenTelemetrySdk;
        }

        #region SetSamplingRatio Tests

        [Fact]
        public void SetSamplingRatio_WithValidRatio_SetsSamplingRatioInExporterOptions()
        {
            // Arrange
            this.telemetryConfiguration.ConnectionString = "InstrumentationKey=test-ikey";

            // Act
            this.telemetryConfiguration.SetSamplingRatio(0.5F);

            // Build the configuration by creating a TelemetryClient
            var sdk = this.BuildConfiguration();
            var exporterOptions = this.GetExporterOptions(sdk);

            // Assert - verify the actual value
            Assert.Equal(0.5F, exporterOptions.SamplingRatio);
        }

        [Fact]
        public void SetSamplingRatio_AfterBuild_ThrowsInvalidOperationException()
        {
            // Arrange
            this.telemetryConfiguration.ConnectionString = "InstrumentationKey=test-ikey";

            // Build the configuration by creating a TelemetryClient
            _ = new TelemetryClient(this.telemetryConfiguration);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                this.telemetryConfiguration.SetSamplingRatio(0.5F));
        }

        [Fact]
        public void SetSamplingRatio_WithRatioGreaterThanOne_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            this.telemetryConfiguration.ConnectionString = "InstrumentationKey=test-ikey";

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                this.telemetryConfiguration.SetSamplingRatio(1.5F));
        }

        [Fact]
        public void SetSamplingRatio_WithNegativeRatio_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            this.telemetryConfiguration.ConnectionString = "InstrumentationKey=test-ikey";

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                this.telemetryConfiguration.SetSamplingRatio(-0.5F));
        }

        #endregion

        #region SetTracesPerSecond Tests

        [Fact]
        public void SetTracesPerSecond_WithValidValue_SetsTracesPerSecondInExporterOptions()
        {
            // Arrange
            this.telemetryConfiguration.ConnectionString = "InstrumentationKey=test-ikey";

            // Act
            this.telemetryConfiguration.SetTracesPerSecond(1.5);

            // Build the configuration by creating a TelemetryClient
            var sdk = this.BuildConfiguration();
            var exporterOptions = this.GetExporterOptions(sdk);

            // Assert - verify the actual value
            Assert.Equal(1.5, exporterOptions.TracesPerSecond);
        }

        [Fact]
        public void SetTracesPerSecond_WithNegativeValue_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            this.telemetryConfiguration.ConnectionString = "InstrumentationKey=test-ikey";

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                this.telemetryConfiguration.SetTracesPerSecond(-1.0));
        }

        [Fact]
        public void SetTracesPerSecond_AfterBuild_ThrowsInvalidOperationException()
        {
            // Arrange
            this.telemetryConfiguration.ConnectionString = "InstrumentationKey=test-ikey";

            // Build the configuration by creating a TelemetryClient
            _ = new TelemetryClient(this.telemetryConfiguration);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                this.telemetryConfiguration.SetTracesPerSecond(1.5));
        }

        [Fact]
        public void SetTracesPerSecond_WithValidValue_AndDisableLiveMetrics_BeforeBuild()
        {
            // Arrange
            this.telemetryConfiguration.ConnectionString = "InstrumentationKey=test-ikey";

            // Act - chain multiple configuration calls
            this.telemetryConfiguration.SetTracesPerSecond(1.5);
            this.telemetryConfiguration.DisableLiveMetrics();

            // Build the configuration by creating a TelemetryClient
            var sdk = this.BuildConfiguration();
            var exporterOptions = this.GetExporterOptions(sdk);

            // Assert - verify both values
            Assert.Equal(1.5, exporterOptions.TracesPerSecond);
            Assert.False(exporterOptions.EnableLiveMetrics);
        }

        #endregion

        #region SetStorageDirectory Tests

        [Fact]
        public void SetStorageDirectory_WithValidPath_SetsStorageDirectoryInExporterOptions()
        {
            // Arrange
            this.telemetryConfiguration.ConnectionString = "InstrumentationKey=test-ikey";

            // Act
            this.telemetryConfiguration.SetStorageDirectory("C:\\TelemetryStorage");

            // Build the configuration by creating a TelemetryClient
            var sdk = this.BuildConfiguration();
            var exporterOptions = this.GetExporterOptions(sdk);

            // Assert - verify the actual value
            Assert.Equal("C:\\TelemetryStorage", exporterOptions.StorageDirectory);
        }

        [Fact]
        public void SetStorageDirectory_WithNullPath_ThrowsArgumentException()
        {
            // Arrange
            this.telemetryConfiguration.ConnectionString = "InstrumentationKey=test-ikey";

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                this.telemetryConfiguration.SetStorageDirectory(null));
        }

        [Fact]
        public void SetStorageDirectory_WithEmptyPath_ThrowsArgumentException()
        {
            // Arrange
            this.telemetryConfiguration.ConnectionString = "InstrumentationKey=test-ikey";

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                this.telemetryConfiguration.SetStorageDirectory(string.Empty));
        }

        [Fact]
        public void SetStorageDirectory_AfterBuild_ThrowsInvalidOperationException()
        {
            // Arrange
            this.telemetryConfiguration.ConnectionString = "InstrumentationKey=test-ikey";

            // Build the configuration by creating a TelemetryClient
            _ = new TelemetryClient(this.telemetryConfiguration);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                this.telemetryConfiguration.SetStorageDirectory("C:\\TelemetryStorage"));
        }

        #endregion

        #region DisableOfflineStorage Tests

        [Fact]
        public void DisableOfflineStorage_SetsDisableOfflineStorageInExporterOptions()
        {
            // Arrange
            this.telemetryConfiguration.ConnectionString = "InstrumentationKey=test-ikey";

            // Act
            this.telemetryConfiguration.DisableOfflineStorage();

            // Build the configuration by creating a TelemetryClient
            var sdk = this.BuildConfiguration();
            var exporterOptions = this.GetExporterOptions(sdk);

            // Assert - verify the actual value
            Assert.True(exporterOptions.DisableOfflineStorage);
        }

        [Fact]
        public void DisableOfflineStorage_AfterBuild_ThrowsInvalidOperationException()
        {
            // Arrange
            this.telemetryConfiguration.ConnectionString = "InstrumentationKey=test-ikey";

            // Build the configuration by creating a TelemetryClient
            _ = new TelemetryClient(this.telemetryConfiguration);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                this.telemetryConfiguration.DisableOfflineStorage());
        }

        #endregion

        #region DisableLiveMetrics Tests

        [Fact]
        public void DisableLiveMetrics_SetsEnableLiveMetricsFalseInExporterOptions()
        {
            // Arrange
            this.telemetryConfiguration.ConnectionString = "InstrumentationKey=test-ikey";

            // Act
            this.telemetryConfiguration.DisableLiveMetrics();

            // Build the configuration by creating a TelemetryClient
            var sdk = this.BuildConfiguration();
            var exporterOptions = this.GetExporterOptions(sdk);

            // Assert - verify the actual value
            Assert.False(exporterOptions.EnableLiveMetrics);
        }

        [Fact]
        public void DisableLiveMetrics_AfterBuild_ThrowsInvalidOperationException()
        {
            // Arrange
            this.telemetryConfiguration.ConnectionString = "InstrumentationKey=test-ikey";

            // Build the configuration by creating a TelemetryClient
            _ = new TelemetryClient(this.telemetryConfiguration);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                this.telemetryConfiguration.DisableLiveMetrics());
        }

        #endregion

        #region DisableTraceBasedLogsSampling Tests

        [Fact]
        public void DisableTraceBasedLogsSampling_SetsEnableTraceBasedLogsSamplerFalseInExporterOptions()
        {
            // Arrange
            this.telemetryConfiguration.ConnectionString = "InstrumentationKey=test-ikey";

            // Act
            this.telemetryConfiguration.DisableTraceBasedLogsSampling();

            // Build the configuration by creating a TelemetryClient
            var sdk = this.BuildConfiguration();
            var exporterOptions = this.GetExporterOptions(sdk);

            // Assert - verify the actual value
            Assert.False(exporterOptions.EnableTraceBasedLogsSampler);
        }

        [Fact]
        public void DisableTraceBasedLogsSampling_AfterBuild_ThrowsInvalidOperationException()
        {
            // Arrange
            this.telemetryConfiguration.ConnectionString = "InstrumentationKey=test-ikey";

            // Build the configuration by creating a TelemetryClient
            _ = new TelemetryClient(this.telemetryConfiguration);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                this.telemetryConfiguration.DisableTraceBasedLogsSampling());
        }

        #endregion
    }
}
