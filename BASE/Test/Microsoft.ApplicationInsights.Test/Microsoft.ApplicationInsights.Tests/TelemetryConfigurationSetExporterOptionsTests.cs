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
    /// Tests for Azure Monitor Exporter configuration properties in TelemetryConfiguration.
    /// </summary>
    public class TelemetryConfigurationSetExporterOptionsTests : IDisposable
    {
        private TelemetryConfiguration telemetryConfiguration;

        public TelemetryConfigurationSetExporterOptionsTests()
        {
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

        #region SamplingRatio Tests

        [Fact]
        public void SamplingRatio_SetsSamplingRatioInExporterOptions()
        {
            // Arrange
            this.telemetryConfiguration = new TelemetryConfiguration();
            this.telemetryConfiguration.ConnectionString = "InstrumentationKey=test-ikey";

            // Act
            this.telemetryConfiguration.SamplingRatio = 0.5F;

            // Build the configuration by creating a TelemetryClient
            var sdk = this.BuildConfiguration();
            var exporterOptions = this.GetExporterOptions(sdk);

            // Assert - verify the actual value
            Assert.Equal(0.5F, exporterOptions.SamplingRatio);
        }

        [Fact]
        public void SamplingRatio_AfterBuild_ThrowsInvalidOperationException()
        {
            // Arrange
            this.telemetryConfiguration = new TelemetryConfiguration();
            this.telemetryConfiguration.ConnectionString = "InstrumentationKey=test-ikey";

            // Build the configuration by creating a TelemetryClient
            _ = new TelemetryClient(this.telemetryConfiguration);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                this.telemetryConfiguration.SamplingRatio = 0.5F);
        }

        #endregion

        #region TracesPerSecond Tests

        [Fact]
        public void TracesPerSecond_SetsTracesPerSecondInExporterOptions()
        {
            // Arrange
            this.telemetryConfiguration = new TelemetryConfiguration();
            this.telemetryConfiguration.ConnectionString = "InstrumentationKey=test-ikey";

            // Act
            this.telemetryConfiguration.TracesPerSecond = 1.5;

            // Build the configuration by creating a TelemetryClient
            var sdk = this.BuildConfiguration();
            var exporterOptions = this.GetExporterOptions(sdk);

            // Assert - verify the actual value
            Assert.Equal(1.5, exporterOptions.TracesPerSecond);
        }

        [Fact]
        public void TracesPerSecond_AfterBuild_ThrowsInvalidOperationException()
        {
            // Arrange
            this.telemetryConfiguration = new TelemetryConfiguration();
            this.telemetryConfiguration.ConnectionString = "InstrumentationKey=test-ikey";

            // Build the configuration by creating a TelemetryClient
            _ = new TelemetryClient(this.telemetryConfiguration);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                this.telemetryConfiguration.TracesPerSecond = 1.5);
        }

        #endregion

        #region StorageDirectory Tests

        [Fact]
        public void StorageDirectory_SetsStorageDirectoryInExporterOptions()
        {
            // Arrange
            this.telemetryConfiguration = new TelemetryConfiguration();
            this.telemetryConfiguration.ConnectionString = "InstrumentationKey=test-ikey";

            // Act
            this.telemetryConfiguration.StorageDirectory = "C:\\TelemetryStorage";

            // Build the configuration by creating a TelemetryClient
            var sdk = this.BuildConfiguration();
            var exporterOptions = this.GetExporterOptions(sdk);

            // Assert - verify the actual value
            Assert.Equal("C:\\TelemetryStorage", exporterOptions.StorageDirectory);
        }

        [Fact]
        public void StorageDirectory_AfterBuild_ThrowsInvalidOperationException()
        {
            // Arrange
            this.telemetryConfiguration = new TelemetryConfiguration();
            this.telemetryConfiguration.ConnectionString = "InstrumentationKey=test-ikey";

            // Build the configuration by creating a TelemetryClient
            _ = new TelemetryClient(this.telemetryConfiguration);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                this.telemetryConfiguration.StorageDirectory = "C:\\TelemetryStorage");
        }

        #endregion

        #region DisableOfflineStorage Tests

        [Fact]
        public void DisableOfflineStorage_SetsDisableOfflineStorageInExporterOptions()
        {
            // Arrange
            this.telemetryConfiguration = new TelemetryConfiguration();
            this.telemetryConfiguration.ConnectionString = "InstrumentationKey=test-ikey";

            // Act
            this.telemetryConfiguration.DisableOfflineStorage = true;

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
            this.telemetryConfiguration = new TelemetryConfiguration();
            this.telemetryConfiguration.ConnectionString = "InstrumentationKey=test-ikey";

            // Build the configuration by creating a TelemetryClient
            _ = new TelemetryClient(this.telemetryConfiguration);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                this.telemetryConfiguration.DisableOfflineStorage = true);
        }

        #endregion

        #region EnableLiveMetrics Tests

        [Fact]
        public void EnableLiveMetrics_SetsEnableLiveMetricsFalseInExporterOptions()
        {
            // Arrange
            this.telemetryConfiguration = new TelemetryConfiguration();
            this.telemetryConfiguration.ConnectionString = "InstrumentationKey=test-ikey";

            // Act
            this.telemetryConfiguration.EnableLiveMetrics = false;

            // Build the configuration by creating a TelemetryClient
            var sdk = this.BuildConfiguration();
            var exporterOptions = this.GetExporterOptions(sdk);

            // Assert - verify the actual value
            Assert.False(exporterOptions.EnableLiveMetrics);
        }

        [Fact]
        public void EnableLiveMetrics_AfterBuild_ThrowsInvalidOperationException()
        {
            // Arrange
            this.telemetryConfiguration = new TelemetryConfiguration();
            this.telemetryConfiguration.ConnectionString = "InstrumentationKey=test-ikey";

            // Build the configuration by creating a TelemetryClient
            _ = new TelemetryClient(this.telemetryConfiguration);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                this.telemetryConfiguration.EnableLiveMetrics = false);
        }

        #endregion

        #region EnableTraceBasedLogsSampler Tests

        [Fact]
        public void EnableTraceBasedLogsSampler_SetsEnableTraceBasedLogsSamplerFalseInExporterOptions()
        {
            // Arrange
            this.telemetryConfiguration = new TelemetryConfiguration();
            this.telemetryConfiguration.ConnectionString = "InstrumentationKey=test-ikey";

            // Act
            this.telemetryConfiguration.EnableTraceBasedLogsSampler = false;

            // Build the configuration by creating a TelemetryClient
            var sdk = this.BuildConfiguration();
            var exporterOptions = this.GetExporterOptions(sdk);

            // Assert - verify the actual value
            Assert.False(exporterOptions.EnableTraceBasedLogsSampler);
        }

        [Fact]
        public void EnableTraceBasedLogsSampler_AfterBuild_ThrowsInvalidOperationException()
        {
            // Arrange
            this.telemetryConfiguration = new TelemetryConfiguration();
            this.telemetryConfiguration.ConnectionString = "InstrumentationKey=test-ikey";

            // Build the configuration by creating a TelemetryClient
            _ = new TelemetryClient(this.telemetryConfiguration);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                this.telemetryConfiguration.EnableTraceBasedLogsSampler = false);
        }

        #endregion

        #region Combined Options Tests

        [Fact]
        public void AllOptions_BeforeBuild_SetsAllValuesInExporterOptions()
        {
            // Arrange
            this.telemetryConfiguration = TelemetryConfiguration.CreateDefault();
            this.telemetryConfiguration.ConnectionString = "InstrumentationKey=test-ikey";

            // Act - set all properties
            this.telemetryConfiguration.SamplingRatio = 0.75F;
            this.telemetryConfiguration.TracesPerSecond = 1.5;
            this.telemetryConfiguration.StorageDirectory = "C:\\TelemetryStorage";
            this.telemetryConfiguration.DisableOfflineStorage = true;
            this.telemetryConfiguration.EnableLiveMetrics = false;
            this.telemetryConfiguration.EnableTraceBasedLogsSampler = false;

            // Build the configuration by creating a TelemetryClient
            var sdk = this.BuildConfiguration();
            var exporterOptions = this.GetExporterOptions(sdk);

            // Assert - verify all values
            Assert.Equal(0.75F, exporterOptions.SamplingRatio);
            Assert.Equal(1.5, exporterOptions.TracesPerSecond);
            Assert.Equal("C:\\TelemetryStorage", exporterOptions.StorageDirectory);
            Assert.True(exporterOptions.DisableOfflineStorage);
            Assert.False(exporterOptions.EnableLiveMetrics);
            Assert.False(exporterOptions.EnableTraceBasedLogsSampler);
        }

        #endregion
    }
}
