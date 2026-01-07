namespace Microsoft.ApplicationInsights.Web.Tests
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Web;
    using Azure.Monitor.OpenTelemetry.Exporter;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Web.Implementation;
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

        private void ResetStaticState()
        {
            // Use reflection to reset static state for test isolation
            var type = typeof(ApplicationInsightsHttpModule);
            
            var sharedConfigField = type.GetField("sharedTelemetryConfiguration", BindingFlags.Static | BindingFlags.NonPublic);
            if (sharedConfigField != null)
            {
                sharedConfigField.SetValue(null, null);
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
        }
    }
}
