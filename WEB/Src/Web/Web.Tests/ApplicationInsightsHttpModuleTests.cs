namespace Microsoft.ApplicationInsights.Web.Tests
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.ExceptionServices;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using Azure.Monitor.OpenTelemetry.Exporter;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Web.Implementation;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using OpenTelemetry;
    using Xunit;

    [Collection("ApplicationInsightsHttpModule")] // Prevent parallel execution due to static state
    public class ApplicationInsightsHttpModuleTests : IDisposable
    {
        private readonly string configFilePath;

        public ApplicationInsightsHttpModuleTests()
        {
            // Create config file in AppDomain base directory
            configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ApplicationInsights.config");
            
            // Delete existing config if present
            if (File.Exists(configFilePath))
            {
                File.Delete(configFilePath);
            }

            // Reset static state before each test
            ResetStaticState();
        }

        public void Dispose()
        {
            // Cleanup config file
            try
            {
                if (File.Exists(configFilePath))
                {
                    File.Delete(configFilePath);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }

            // Reset static state after test
            ResetStaticState();
        }

        [Fact]
        public void Init_ThrowsArgumentNullException_WhenContextIsNull()
        {
            // Arrange
            var module = new ApplicationInsightsHttpModule();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => module.Init(null));
        }

        [Fact]
        public void Init_InitializesTelemetryConfiguration_WithConnectionStringFromConfig()
        {
            // Arrange
            string configContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ApplicationInsights xmlns=""http://schemas.microsoft.com/ApplicationInsights/2013/Settings"">
    <ConnectionString>InstrumentationKey=12345678-1234-1234-1234-123456789012</ConnectionString>
</ApplicationInsights>";

            CreateConfigInTestDirectory(configContent);


            var module = new ApplicationInsightsHttpModule();
            var httpApp = CreateMockHttpApplication();

            // Act
            module.Init(httpApp);

            // Assert
            var config = GetTelemetryConfigurationFromModule(module);
            Assert.NotNull(config);
            Assert.Equal("InstrumentationKey=12345678-1234-1234-1234-123456789012", config.ConnectionString);
        }

        [Fact]
        public void Init_SetsSamplingRatio_FromConfig()
        {
            // Arrange
            string configContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ApplicationInsights xmlns=""http://schemas.microsoft.com/ApplicationInsights/2013/Settings"">
    <ConnectionString>InstrumentationKey=test</ConnectionString>
    <SamplingRatio>0.25</SamplingRatio>
</ApplicationInsights>";

            CreateConfigInTestDirectory(configContent);


            var module = new ApplicationInsightsHttpModule();
            var httpApp = CreateMockHttpApplication();

            // Act
            module.Init(httpApp);

            // Assert
            var config = GetTelemetryConfigurationFromModule(module);
            Assert.NotNull(config);
            Assert.Equal(0.25f, config.SamplingRatio);
        }

        [Fact]
        public void Init_SetsTracesPerSecond_FromConfig()
        {
            // Arrange
            string configContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ApplicationInsights xmlns=""http://schemas.microsoft.com/ApplicationInsights/2013/Settings"">
    <ConnectionString>InstrumentationKey=test</ConnectionString>
    <TracesPerSecond>5.5</TracesPerSecond>
</ApplicationInsights>";

            CreateConfigInTestDirectory(configContent);


            var module = new ApplicationInsightsHttpModule();
            var httpApp = CreateMockHttpApplication();

            // Act
            module.Init(httpApp);

            // Assert
            var config = GetTelemetryConfigurationFromModule(module);
            Assert.NotNull(config);
            Assert.Equal(5.5, config.TracesPerSecond);
        }

        [Fact]
        public void Init_SetsStorageDirectory_FromConfig()
        {
            // Arrange
            string configContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ApplicationInsights xmlns=""http://schemas.microsoft.com/ApplicationInsights/2013/Settings"">
    <ConnectionString>InstrumentationKey=test</ConnectionString>
    <StorageDirectory>C:\Temp\AIStorage</StorageDirectory>
</ApplicationInsights>";

            CreateConfigInTestDirectory(configContent);


            var module = new ApplicationInsightsHttpModule();
            var httpApp = CreateMockHttpApplication();

            // Act
            module.Init(httpApp);

            // Assert
            var config = GetTelemetryConfigurationFromModule(module);
            Assert.NotNull(config);
            Assert.Equal(@"C:\Temp\AIStorage", config.StorageDirectory);
        }

        [Fact]
        public void Init_SetsDisableOfflineStorage_FromConfig()
        {
            // Arrange
            string configContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ApplicationInsights xmlns=""http://schemas.microsoft.com/ApplicationInsights/2013/Settings"">
    <ConnectionString>InstrumentationKey=test</ConnectionString>
    <DisableOfflineStorage>true</DisableOfflineStorage>
</ApplicationInsights>";

            CreateConfigInTestDirectory(configContent);


            var module = new ApplicationInsightsHttpModule();
            var httpApp = CreateMockHttpApplication();

            // Act
            module.Init(httpApp);

            // Assert
            var config = GetTelemetryConfigurationFromModule(module);
            Assert.NotNull(config);
            Assert.True(config.DisableOfflineStorage);
        }

        [Fact]
        public void Init_SetsEnableTraceBasedLogsSampler_FromConfig()
        {
            // Arrange
            string configContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ApplicationInsights xmlns=""http://schemas.microsoft.com/ApplicationInsights/2013/Settings"">
    <ConnectionString>InstrumentationKey=test</ConnectionString>
    <EnableTraceBasedLogsSampler>false</EnableTraceBasedLogsSampler>
</ApplicationInsights>";

            CreateConfigInTestDirectory(configContent);


            var module = new ApplicationInsightsHttpModule();
            var httpApp = CreateMockHttpApplication();

            // Act
            module.Init(httpApp);

            // Assert
            var config = GetTelemetryConfigurationFromModule(module);
            Assert.NotNull(config);
            Assert.False(config.EnableTraceBasedLogsSampler);
        }

        [Fact]
        public void Init_SetsEnableLiveMetrics_FromConfig()
        {
            // Arrange
            string configContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ApplicationInsights xmlns=""http://schemas.microsoft.com/ApplicationInsights/2013/Settings"">
    <ConnectionString>InstrumentationKey=test</ConnectionString>
    <EnableQuickPulseMetricStream>false</EnableQuickPulseMetricStream>
</ApplicationInsights>";

            CreateConfigInTestDirectory(configContent);


            var module = new ApplicationInsightsHttpModule();
            var httpApp = CreateMockHttpApplication();

            // Act
            module.Init(httpApp);

            // Assert
            var config = GetTelemetryConfigurationFromModule(module);
            Assert.NotNull(config);
            Assert.False(config.EnableLiveMetrics);
        }

        [Fact]
        public void Init_FallsBackToDefault_WhenConfigFileMissing()
        {
            // Arrange - no config file


            var module = new ApplicationInsightsHttpModule();
            var httpApp = CreateMockHttpApplication();

            // Act
            module.Init(httpApp);

            // Assert - should not throw, configuration should still be initialized
            var config = GetTelemetryConfigurationFromModule(module);
            Assert.NotNull(config);
        }

        [Fact]
        public void Init_OnlyInitializesOnce_AcrossMultipleInstances()
        {
            // Arrange
            string configContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ApplicationInsights xmlns=""http://schemas.microsoft.com/ApplicationInsights/2013/Settings"">
    <ConnectionString>InstrumentationKey=test</ConnectionString>
</ApplicationInsights>";

            CreateConfigInTestDirectory(configContent);


            var module1 = new ApplicationInsightsHttpModule();
            var module2 = new ApplicationInsightsHttpModule();
            var httpApp1 = CreateMockHttpApplication();
            var httpApp2 = CreateMockHttpApplication();

            // Act
            module1.Init(httpApp1);
            module2.Init(httpApp2);

            // Assert - both modules should share the same configuration instance
            var config1 = GetTelemetryConfigurationFromModule(module1);
            var config2 = GetTelemetryConfigurationFromModule(module2);
            
            Assert.NotNull(config1);
            Assert.NotNull(config2);
            Assert.Same(config1, config2); // Should be the same instance
        }

        [Fact]
        public void OnBeginRequest_ReusesSharedTelemetryClient_AcrossModuleInstances()
        {
            // Arrange
            string configContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ApplicationInsights xmlns=""http://schemas.microsoft.com/ApplicationInsights/2013/Settings"">
    <ConnectionString>InstrumentationKey=test</ConnectionString>
</ApplicationInsights>";

            CreateConfigInTestDirectory(configContent);

            var module1 = new ApplicationInsightsHttpModule();
            var module2 = new ApplicationInsightsHttpModule();
            module1.Init(CreateMockHttpApplication());
            module2.Init(CreateMockHttpApplication());

            // Act
            var firstException = Record.Exception(() => InvokeOnBeginRequest(module1));
            var clientAfterFirstRequest = GetSharedTelemetryClient();
            var secondException = Record.Exception(() => InvokeOnBeginRequest(module2));
            var clientAfterSecondRequest = GetSharedTelemetryClient();

            // Assert
            Assert.Null(firstException);
            Assert.Null(secondException);
            Assert.NotNull(clientAfterFirstRequest);
            Assert.Same(clientAfterFirstRequest, clientAfterSecondRequest);
        }

        [Fact]
        public void OnBeginRequest_PublishesOneSharedTelemetryClient_UnderParallelLoad()
        {
            // Arrange
            string configContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ApplicationInsights xmlns=""http://schemas.microsoft.com/ApplicationInsights/2013/Settings"">
    <ConnectionString>InstrumentationKey=test</ConnectionString>
</ApplicationInsights>";

            CreateConfigInTestDirectory(configContent);

            var modules = Enumerable.Range(0, 16)
                .Select(_ =>
                {
                    var module = new ApplicationInsightsHttpModule();
                    module.Init(CreateMockHttpApplication());
                    return module;
                })
                .ToArray();
            var observedClients = new ConcurrentBag<TelemetryClient>();

            // Act
            var exception = Record.Exception(() =>
                Parallel.For(0, 64, i =>
                {
                    InvokeOnBeginRequest(modules[i % modules.Length]);
                    observedClients.Add(GetSharedTelemetryClient());
                }));

            var sharedClient = GetSharedTelemetryClient();

            // Assert
            Assert.Null(exception);
            Assert.NotNull(sharedClient);
            Assert.Single(observedClients.Distinct());
            var trackException = Record.Exception(() => sharedClient.TrackEvent("ParallelRace"));
            Assert.Null(trackException);
        }

        [Fact]
        public void OnBeginRequest_RetriesTelemetryClientConstruction_AfterFailure()
        {
            // Arrange
            string configContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ApplicationInsights xmlns=""http://schemas.microsoft.com/ApplicationInsights/2013/Settings"">
    <ConnectionString>InstrumentationKey=test</ConnectionString>
</ApplicationInsights>";

            CreateConfigInTestDirectory(configContent);

            var module = new ApplicationInsightsHttpModule();
            module.Init(CreateMockHttpApplication());
            var initialConfiguration = GetTelemetryConfigurationFromModule(module);
            ForceBuildTelemetryConfiguration(initialConfiguration);

            // Act
            var firstException = Record.Exception(() => InvokeOnBeginRequest(module));
            var clientAfterFailure = GetSharedTelemetryClient();

            var replacementConfiguration = new TelemetryConfiguration
            {
                ConnectionString = "InstrumentationKey=test",
            };

            SetTelemetryConfigurationOnModule(module, replacementConfiguration);
            var secondException = Record.Exception(() => InvokeOnBeginRequest(module));
            var clientAfterRetry = GetSharedTelemetryClient();

            // Assert
            Assert.IsType<InvalidOperationException>(firstException);
            Assert.Null(clientAfterFailure);
            Assert.Null(secondException);
            Assert.NotNull(clientAfterRetry);
        }

        [Fact]
        public void Init_ConfiguresOpenTelemetryBuilder_WhenConfigOptionsProvided()
        {
            // Arrange
            string configContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ApplicationInsights xmlns=""http://schemas.microsoft.com/ApplicationInsights/2013/Settings"">
    <ConnectionString>InstrumentationKey=test</ConnectionString>
    <EnableDependencyTrackingTelemetryModule>false</EnableDependencyTrackingTelemetryModule>
    <ApplicationVersion>1.0.0</ApplicationVersion>
</ApplicationInsights>";

            CreateConfigInTestDirectory(configContent);


            var module = new ApplicationInsightsHttpModule();
            var httpApp = CreateMockHttpApplication();

            // Act
            module.Init(httpApp);

            // Assert - configuration should be initialized (actual OpenTelemetry validation would require more complex setup)
            var config = GetTelemetryConfigurationFromModule(module);
            Assert.NotNull(config);
        }

        [Fact]
        public void Init_SetsSamplingRatio_FlowsToExporterOptions()
        {
            // Arrange
            string configContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
        <ApplicationInsights xmlns=""http://schemas.microsoft.com/ApplicationInsights/2013/Settings"">
            <ConnectionString>InstrumentationKey=test</ConnectionString>
            <SamplingRatio>0.25</SamplingRatio>
        </ApplicationInsights>";

            CreateConfigInTestDirectory(configContent);

            var module = new ApplicationInsightsHttpModule();
            var httpApp = CreateMockHttpApplication();

            // Act
            module.Init(httpApp);

            // Get TelemetryConfiguration and create TelemetryClient to trigger Build()
            var config = GetTelemetryConfigurationFromModule(module);
            var client = new TelemetryClient(config);

            // Get OpenTelemetrySdk from TelemetryClient via reflection
            var sdkField = typeof(TelemetryClient).GetField("sdk", BindingFlags.NonPublic | BindingFlags.Instance);
            var sdk = sdkField?.GetValue(client) as OpenTelemetrySdk;

            // Get IServiceProvider from OpenTelemetrySdk via reflection
            var servicesProperty = typeof(OpenTelemetrySdk).GetProperty("Services", BindingFlags.NonPublic | BindingFlags.Instance);
            var serviceProvider = servicesProperty?.GetValue(sdk) as IServiceProvider;

            // Assert - verify exporter options
            Assert.NotNull(serviceProvider);
            var exporterOptions = serviceProvider.GetService<IOptions<AzureMonitorExporterOptions>>();
            Assert.NotNull(exporterOptions);
            Assert.Equal(0.25f, exporterOptions.Value.SamplingRatio);
            Assert.Null(exporterOptions.Value.TracesPerSecond); // Should be null when only SamplingRatio is set
        }

        [Fact]
        public void Init_SetsTracesPerSecond_FlowsToExporterOptions()
        {
            // Arrange
            string configContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
        <ApplicationInsights xmlns=""http://schemas.microsoft.com/ApplicationInsights/2013/Settings"">
            <ConnectionString>InstrumentationKey=test</ConnectionString>
            <TracesPerSecond>5.5</TracesPerSecond>
        </ApplicationInsights>";

            CreateConfigInTestDirectory(configContent);

            var module = new ApplicationInsightsHttpModule();
            var httpApp = CreateMockHttpApplication();

            // Act
            module.Init(httpApp);

            // Get TelemetryConfiguration and create TelemetryClient to trigger Build()
            var config = GetTelemetryConfigurationFromModule(module);
            var client = new TelemetryClient(config);

            // Get OpenTelemetrySdk from TelemetryClient via reflection
            var sdkField = typeof(TelemetryClient).GetField("sdk", BindingFlags.NonPublic | BindingFlags.Instance);
            var sdk = sdkField?.GetValue(client) as OpenTelemetrySdk;

            // Get IServiceProvider from OpenTelemetrySdk via reflection
            var servicesProperty = typeof(OpenTelemetrySdk).GetProperty("Services", BindingFlags.NonPublic | BindingFlags.Instance);
            var serviceProvider = servicesProperty?.GetValue(sdk) as IServiceProvider;

            // Assert - verify exporter options
            Assert.NotNull(serviceProvider);
            var exporterOptions = serviceProvider.GetService<IOptions<AzureMonitorExporterOptions>>();
            Assert.NotNull(exporterOptions);
            Assert.Equal(5.5, exporterOptions.Value.TracesPerSecond);
        }

        [Fact]
        public void Init_NoSamplingTagsSet_UseDefaultExporterValues()
        {
            // Arrange
            string configContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
        <ApplicationInsights xmlns=""http://schemas.microsoft.com/ApplicationInsights/2013/Settings"">
            <ConnectionString>InstrumentationKey=test</ConnectionString>
        </ApplicationInsights>";

            CreateConfigInTestDirectory(configContent);

            var module = new ApplicationInsightsHttpModule();
            var httpApp = CreateMockHttpApplication();

            // Act
            module.Init(httpApp);

            // Get TelemetryConfiguration and create TelemetryClient to trigger Build()
            var config = GetTelemetryConfigurationFromModule(module);
            var client = new TelemetryClient(config);

            // Get OpenTelemetrySdk from TelemetryClient via reflection
            var sdkField = typeof(TelemetryClient).GetField("sdk", BindingFlags.NonPublic | BindingFlags.Instance);
            var sdk = sdkField?.GetValue(client) as OpenTelemetrySdk;

            // Get IServiceProvider from OpenTelemetrySdk via reflection
            var servicesProperty = typeof(OpenTelemetrySdk).GetProperty("Services", BindingFlags.NonPublic | BindingFlags.Instance);
            var serviceProvider = servicesProperty?.GetValue(sdk) as IServiceProvider;

            // Assert - verify exporter options
            Assert.NotNull(serviceProvider);
            var exporterOptions = serviceProvider.GetService<IOptions<AzureMonitorExporterOptions>>();
            Assert.NotNull(exporterOptions);
            Assert.Equal(5.0, exporterOptions.Value.TracesPerSecond);
            Assert.Equal(1.0f, exporterOptions.Value.SamplingRatio);
        }

        private void CreateConfigInTestDirectory(string content)
        {
            File.WriteAllText(configFilePath, content);
        }

        private HttpApplication CreateMockHttpApplication()
        {
            // Create a minimal HttpApplication instance for testing
            // We can't fully mock HttpApplication as it's sealed, but we can use reflection
            // For Init() method, we mainly need a non-null instance
            var httpApp = (HttpApplication)Activator.CreateInstance(typeof(HttpApplication), true);
            return httpApp;
        }

        private TelemetryConfiguration GetTelemetryConfigurationFromModule(ApplicationInsightsHttpModule module)
        {
            // Use reflection to access private telemetryConfiguration field
            var field = typeof(ApplicationInsightsHttpModule).GetField("telemetryConfiguration", BindingFlags.Instance | BindingFlags.NonPublic);
            return (TelemetryConfiguration)field?.GetValue(module);
        }

        private TelemetryClient GetSharedTelemetryClient()
        {
            var field = typeof(ApplicationInsightsHttpModule).GetField("sharedTelemetryClient", BindingFlags.Static | BindingFlags.NonPublic);
            return (TelemetryClient)field?.GetValue(null);
        }

        private void SetTelemetryConfigurationOnModule(ApplicationInsightsHttpModule module, TelemetryConfiguration configuration)
        {
            var field = typeof(ApplicationInsightsHttpModule).GetField("telemetryConfiguration", BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(module, configuration);
        }

        private void InvokeOnBeginRequest(ApplicationInsightsHttpModule module)
        {
            var method = typeof(ApplicationInsightsHttpModule).GetMethod("OnBeginRequest", BindingFlags.Instance | BindingFlags.NonPublic);

            try
            {
                method?.Invoke(module, new object[] { module, EventArgs.Empty });
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
        }

        private void ForceBuildTelemetryConfiguration(TelemetryConfiguration configuration)
        {
            var method = typeof(TelemetryConfiguration).GetMethod("Build", BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(configuration, null);
        }

        private void ResetStaticState()
        {
            // Use reflection to reset static state for test isolation
            var type = typeof(ApplicationInsightsHttpModule);
            
            var sharedConfigField = type.GetField("sharedTelemetryConfiguration", BindingFlags.Static | BindingFlags.NonPublic);
            if (sharedConfigField != null)
            {
                sharedConfigField.SetValue(null, null);
            }

            var sharedClientField = type.GetField("sharedTelemetryClient", BindingFlags.Static | BindingFlags.NonPublic);
            if (sharedClientField != null)
            {
                sharedClientField.SetValue(null, null);
            }

            var isInitializedField = type.GetField("isInitialized", BindingFlags.Static | BindingFlags.NonPublic);
            if (isInitializedField != null)
            {
                isInitializedField.SetValue(null, false);
            }

            var initCountField = type.GetField("initializationCount", BindingFlags.Static | BindingFlags.NonPublic);
            if (initCountField != null)
            {
                initCountField.SetValue(null, 0);
            }

            // This next block is added because of tests that check the value of exporter options for sampling settings.
            // Also reset the TelemetryConfiguration.DefaultInstance static Lazy field
            // This is necessary because CreateDefault() returns a singleton that can only be built once
            var telemetryConfigType = typeof(TelemetryConfiguration);
            var defaultInstanceField = telemetryConfigType.GetField("DefaultInstance", BindingFlags.Static | BindingFlags.NonPublic);
            if (defaultInstanceField != null)
            {
                // Create a new Lazy<TelemetryConfiguration> instance to replace the existing one
                var lazyType = typeof(Lazy<TelemetryConfiguration>);
                var newLazy = new Lazy<TelemetryConfiguration>(() => new TelemetryConfiguration(), LazyThreadSafetyMode.ExecutionAndPublication);
                defaultInstanceField.SetValue(null, newLazy);
            }
        }
    }
}
