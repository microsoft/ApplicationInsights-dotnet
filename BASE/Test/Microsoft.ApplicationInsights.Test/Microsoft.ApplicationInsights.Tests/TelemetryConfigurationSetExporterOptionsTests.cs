namespace Microsoft.ApplicationInsights.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using Azure.Core;
    using Azure.Monitor.OpenTelemetry.Exporter;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Internal;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using OpenTelemetry;
    using OpenTelemetry.Logs;
    using OpenTelemetry.Trace;
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

        private static TelemetryConfiguration CreateTelemetryConfiguration()
        {
            var configuration = new TelemetryConfiguration();
            configuration.ConnectionString = "InstrumentationKey=" + Guid.NewGuid();
            return configuration;
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

        private static T GetSdkService<T>(OpenTelemetrySdk sdk)
            where T : class
        {
            var servicesProperty = typeof(OpenTelemetrySdk).GetProperty(
                "Services",
                BindingFlags.NonPublic | BindingFlags.Instance);

            var serviceProvider = servicesProperty?.GetValue(sdk) as IServiceProvider;
            Assert.NotNull(serviceProvider);

            var service = serviceProvider.GetService<T>();
            Assert.NotNull(service);
            return service;
        }

        private static Action<IOpenTelemetryBuilder> GetBuilderConfiguration(TelemetryConfiguration configuration)
        {
            var builderConfigurationField = typeof(TelemetryConfiguration).GetField(
                "builderConfiguration",
                BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.NotNull(builderConfigurationField);
            var builderConfiguration = builderConfigurationField.GetValue(configuration) as Action<IOpenTelemetryBuilder>;
            Assert.NotNull(builderConfiguration);
            return builderConfiguration;
        }

        [Fact]
        public void Build_RegistersDefaultContextActivityProcessor_BeforeUserExporter()
        {
            using var configuration = CreateTelemetryConfiguration();
            configuration.SamplingRatio = 1.0F;
            configuration.DefaultContext.Operation.Name = "operation-X";

            var activityItems = new List<Activity>();
            configuration.ConfigureOpenTelemetryBuilder(builder =>
                builder.WithTracing(tracing => tracing.AddInMemoryExporter(activityItems)));

            using var sdk = configuration.Build();
            using var activitySource = new ActivitySource(TelemetryConfiguration.ApplicationInsightsActivitySourceName);
            using (var activity = activitySource.StartActivity("test-activity"))
            {
                Assert.NotNull(activity);
                activity.Stop();
            }

            var capturedActivity = Assert.Single(activityItems);
            Assert.Equal("operation-X", capturedActivity.GetTagItem(SemanticConventions.AttributeMicrosoftOperationName));
        }

        [Fact]
        public void Build_RegistersDefaultContextLogProcessor_BeforeUserExporter()
        {
            using var configuration = CreateTelemetryConfiguration();
            configuration.DefaultContext.Operation.Name = "operation-X";

            var logItems = new List<LogRecord>();
            configuration.ConfigureOpenTelemetryBuilder(builder =>
                builder.WithLogging(logging => logging.AddInMemoryExporter(logItems)));

            using var sdk = configuration.Build();
            var loggerFactory = GetSdkService<ILoggerFactory>(sdk);
            var logger = loggerFactory.CreateLogger("Layer3bTests");
            logger.LogInformation("layer3b-log-message");

            var capturedLog = Assert.Single(logItems.Where(l => l.FormattedMessage == "layer3b-log-message"));
            var attributes = capturedLog.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());
            Assert.Equal("operation-X", attributes[SemanticConventions.AttributeMicrosoftOperationName]);
        }

        [Fact]
        public void Build_WithoutDefaultContextValues_NoSpuriousTags()
        {
            using var configuration = CreateTelemetryConfiguration();
            configuration.SamplingRatio = 1.0F;

            var activityItems = new List<Activity>();
            configuration.ConfigureOpenTelemetryBuilder(builder =>
                builder.WithTracing(tracing => tracing.AddInMemoryExporter(activityItems)));

            using var sdk = configuration.Build();
            using var activitySource = new ActivitySource(TelemetryConfiguration.ApplicationInsightsActivitySourceName);
            using (var activity = activitySource.StartActivity("test-activity"))
            {
                Assert.NotNull(activity);
                activity.SetTag("custom-tag", "custom-value");
                activity.Stop();
            }

            var capturedActivity = Assert.Single(activityItems);
            Assert.Equal("custom-value", capturedActivity.GetTagItem("custom-tag"));
            Assert.Null(capturedActivity.GetTagItem(SemanticConventions.AttributeMicrosoftOperationName));
            Assert.Null(capturedActivity.GetTagItem(SemanticConventions.AttributeEnduserPseudoId));
        }

        [Fact]
        public void Build_DefaultContextMutationAfterBuild_IsVisibleOnNextActivity()
        {
            using var configuration = CreateTelemetryConfiguration();
            configuration.SamplingRatio = 1.0F;

            var activityItems = new List<Activity>();
            configuration.ConfigureOpenTelemetryBuilder(builder =>
                builder.WithTracing(tracing => tracing.AddInMemoryExporter(activityItems)));

            using var sdk = configuration.Build();
            configuration.DefaultContext.Operation.Name = "later";

            using var activitySource = new ActivitySource(TelemetryConfiguration.ApplicationInsightsActivitySourceName);
            using (var activity = activitySource.StartActivity("test-activity"))
            {
                Assert.NotNull(activity);
                activity.Stop();
            }

            var capturedActivity = Assert.Single(activityItems);
            Assert.Equal("later", capturedActivity.GetTagItem(SemanticConventions.AttributeMicrosoftOperationName));
        }

        [Fact]
        public void TelemetryClient_NonDI_DoesNotMutateConfiguration()
        {
            using var configuration = CreateTelemetryConfiguration();
            var builderConfigurationBefore = GetBuilderConfiguration(configuration);

            _ = new TelemetryClient(configuration);
            _ = new TelemetryClient(configuration);

            var builderConfigurationAfter = GetBuilderConfiguration(configuration);
            Assert.Same(builderConfigurationBefore, builderConfigurationAfter);
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
            Assert.Null(exporterOptions.TracesPerSecond);
        }

        [Fact]
        public void WhenNoSamplingPropertiesSet_DefaultExporterValuesArePresent()
        {
            // Arrange
            this.telemetryConfiguration = new TelemetryConfiguration();
            this.telemetryConfiguration.ConnectionString = "InstrumentationKey=test-ikey";

            // Build the configuration by creating a TelemetryClient
            var sdk = this.BuildConfiguration();
            var exporterOptions = this.GetExporterOptions(sdk);

            // Assert - verify the actual value
            Assert.Equal(1.0F, exporterOptions.SamplingRatio);
            Assert.Equal(5.0, exporterOptions.TracesPerSecond);
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
            Assert.Equal(1.0F, exporterOptions.SamplingRatio);
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

        [Fact]
        public void TryPrependOpenTelemetryBuilderConfiguration_AppendsWhenNotBuilt()
        {
            this.telemetryConfiguration = CreateTelemetryConfiguration();
            var invocationOrder = new List<string>();

            this.telemetryConfiguration.ConfigureOpenTelemetryBuilder(_ => invocationOrder.Add("existing"));

            var result = this.telemetryConfiguration.TryPrependOpenTelemetryBuilderConfiguration(_ => invocationOrder.Add("prepended"));

            Assert.True(result);

            _ = this.telemetryConfiguration.Build();

            Assert.Equal(new[] { "prepended", "existing" }, invocationOrder);
        }

        [Fact]
        public void TryPrependOpenTelemetryBuilderConfiguration_ReturnsFalseWhenBuilt()
        {
            this.telemetryConfiguration = CreateTelemetryConfiguration();

            _ = this.telemetryConfiguration.Build();

            var result = this.telemetryConfiguration.TryPrependOpenTelemetryBuilderConfiguration(_ => { });

            Assert.False(result);
        }

        [Fact]
        public void TryPrependOpenTelemetryBuilderConfiguration_NullThrowsArgumentNullException()
        {
            this.telemetryConfiguration = CreateTelemetryConfiguration();

            Assert.Throws<ArgumentNullException>(() => this.telemetryConfiguration.TryPrependOpenTelemetryBuilderConfiguration(null));
        }

        [Fact]
        public void Build_IsIdempotent()
        {
            this.telemetryConfiguration = CreateTelemetryConfiguration();

            var firstSdk = this.telemetryConfiguration.Build();
            var secondSdk = this.telemetryConfiguration.Build();

            Assert.Same(firstSdk, secondSdk);
        }

        [Fact]
        public void Build_WithSkipDefaultBuilderConfiguration_DoesNotThrow()
        {
            // Regression test: TelemetryConfiguration created with skipDefaultBuilderConfiguration: true
            // (the DI path) leaves builderConfiguration null. Build() must tolerate that without NRE
            // so that `new TelemetryClient(diResolvedConfig)` works (the contract advertised by the
            // migration guidance and exercised by NETCORE DiTelemetryClientParityTests).
            using var configuration = new TelemetryConfiguration(skipDefaultBuilderConfiguration: true);
            configuration.ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000";

            var sdk = configuration.Build();

            Assert.NotNull(sdk);
        }

        [Fact]
        public void Build_AfterConfigureOpenTelemetryBuilderOnSkipDefaultConfig_DoesNotThrow()
        {
            // Regression test: when builderConfiguration starts null (skipDefaultBuilderConfiguration: true),
            // calling ConfigureOpenTelemetryBuilder / Prepend / TryPrepend must not capture that null
            // and dereference it later inside the composite delegate during Build(). The public API
            // ConfigureOpenTelemetryBuilder (and its callers like SetAzureTokenCredential) must be
            // safe to call on a DI-resolved configuration.
            using var configuration = new TelemetryConfiguration(skipDefaultBuilderConfiguration: true);
            configuration.ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000";

            var userActionInvoked = false;
            configuration.ConfigureOpenTelemetryBuilder(_ => userActionInvoked = true);

            var sdk = configuration.Build();

            Assert.NotNull(sdk);
            Assert.True(userActionInvoked, "User ConfigureOpenTelemetryBuilder action should have run during Build().");
        }

        [Fact]
        public void Build_AfterTryPrependOnSkipDefaultConfig_DoesNotThrow()
        {
            // Regression test: same as above but via TryPrependOpenTelemetryBuilderConfiguration.
            using var configuration = new TelemetryConfiguration(skipDefaultBuilderConfiguration: true);
            configuration.ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000";

            var prependInvoked = false;
            var prepended = configuration.TryPrependOpenTelemetryBuilderConfiguration(_ => prependInvoked = true);
            Assert.True(prepended);

            var sdk = configuration.Build();

            Assert.NotNull(sdk);
            Assert.True(prependInvoked, "TryPrependOpenTelemetryBuilderConfiguration action should have run during Build().");
        }

        [Fact]
        public void TelemetryConfiguration_DefaultContext_IsNonNullAndStableReference()
        {
            using var firstConfiguration = new TelemetryConfiguration();
            using var secondConfiguration = new TelemetryConfiguration();

            Assert.NotNull(firstConfiguration.DefaultContext);
            Assert.NotNull(secondConfiguration.DefaultContext);
            Assert.Same(firstConfiguration.DefaultContext, firstConfiguration.DefaultContext);
            Assert.NotSame(firstConfiguration.DefaultContext, secondConfiguration.DefaultContext);
        }

        [Fact]
        public void TelemetryConfiguration_DefaultContext_MutationIsVisibleAfterBuild()
        {
            this.telemetryConfiguration = CreateTelemetryConfiguration();

            _ = this.telemetryConfiguration.Build();

            this.telemetryConfiguration.DefaultContext.Cloud.RoleName = "x";

            Assert.Equal("x", this.telemetryConfiguration.DefaultContext.Cloud.RoleName);
        }

        [Fact]
        public void TelemetryConfiguration_DefaultContext_GlobalPropertiesIsMutable()
        {
            using var configuration = new TelemetryConfiguration();

            configuration.DefaultContext.GlobalProperties["k"] = "v";

            Assert.Equal("v", configuration.DefaultContext.GlobalProperties["k"]);
        }
    }
}
