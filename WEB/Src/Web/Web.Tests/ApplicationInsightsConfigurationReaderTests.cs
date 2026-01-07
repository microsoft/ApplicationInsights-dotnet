namespace Microsoft.ApplicationInsights.Web.Tests
{
    using System;
    using System.IO;
    using System.Xml.Linq;
    using Microsoft.ApplicationInsights.Web.Implementation;
    using Xunit;

    public class ApplicationInsightsConfigurationReaderTests : IDisposable
    {
        private readonly string configFilePath;

        public ApplicationInsightsConfigurationReaderTests()
        {
            // Create config file in AppDomain base directory where the reader expects it
            configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ApplicationInsights.config");
            
            // Delete existing config if present
            if (File.Exists(configFilePath))
            {
                File.Delete(configFilePath);
            }
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
        }

        [Fact]
        public void GetConfigurationOptions_ReturnsNull_WhenConfigFileDoesNotExist()
        {
            // Act
            var options = ApplicationInsightsConfigurationReader.GetConfigurationOptions();

            // Assert
            Assert.Null(options);
        }

        [Fact]
        public void GetConfigurationOptions_ReturnsAllProperties_WhenFullyPopulated()
        {
            // Arrange
            string configContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ApplicationInsights xmlns=""http://schemas.microsoft.com/ApplicationInsights/2013/Settings"">
    <ConnectionString>InstrumentationKey=12345678-1234-1234-1234-123456789012;IngestionEndpoint=https://test.in.applicationinsights.azure.com/</ConnectionString>
    <DisableTelemetry>false</DisableTelemetry>
    <SamplingRatio>0.5</SamplingRatio>
    <TracesPerSecond>10.5</TracesPerSecond>
    <StorageDirectory>C:\Temp\AI</StorageDirectory>
    <DisableOfflineStorage>true</DisableOfflineStorage>
    <EnableTraceBasedLogsSampler>true</EnableTraceBasedLogsSampler>
    <EnableQuickPulseMetricStream>false</EnableQuickPulseMetricStream>
    <EnablePerformanceCounterCollectionModule>true</EnablePerformanceCounterCollectionModule>
    <AddAutoCollectedMetricExtractor>false</AddAutoCollectedMetricExtractor>
    <EnableDependencyTrackingTelemetryModule>true</EnableDependencyTrackingTelemetryModule>
    <ApplicationVersion>1.2.3.4</ApplicationVersion>
</ApplicationInsights>";

            CreateTestConfigFile(configContent);

            // Act
            var options = ReadConfigFromTestDirectory();

            // Assert
            Assert.NotNull(options);
            Assert.Equal("InstrumentationKey=12345678-1234-1234-1234-123456789012;IngestionEndpoint=https://test.in.applicationinsights.azure.com/", options.ConnectionString);
            Assert.False(options.DisableTelemetry);
            Assert.Equal(0.5f, options.SamplingRatio);
            Assert.Equal(10.5, options.TracesPerSecond);
            Assert.Equal(@"C:\Temp\AI", options.StorageDirectory);
            Assert.True(options.DisableOfflineStorage);
            Assert.True(options.EnableTraceBasedLogsSampler);
            Assert.False(options.EnableQuickPulseMetricStream);
            Assert.True(options.EnablePerformanceCounterCollectionModule);
            Assert.False(options.AddAutoCollectedMetricExtractor);
            Assert.True(options.EnableDependencyTrackingTelemetryModule);
            Assert.Equal("1.2.3.4", options.ApplicationVersion);
        }

        [Fact]
        public void GetConfigurationOptions_ReturnsNull_ForMissingProperties()
        {
            // Arrange
            string configContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ApplicationInsights xmlns=""http://schemas.microsoft.com/ApplicationInsights/2013/Settings"">
    <ConnectionString>InstrumentationKey=test</ConnectionString>
</ApplicationInsights>";

            CreateTestConfigFile(configContent);

            // Act
            var options = ReadConfigFromTestDirectory();

            // Assert
            Assert.NotNull(options);
            Assert.Equal("InstrumentationKey=test", options.ConnectionString);
            Assert.Null(options.DisableTelemetry);
            Assert.Null(options.SamplingRatio);
            Assert.Null(options.TracesPerSecond);
            Assert.Null(options.StorageDirectory);
            Assert.Null(options.DisableOfflineStorage);
            Assert.Null(options.EnableTraceBasedLogsSampler);
            Assert.Null(options.EnableQuickPulseMetricStream);
            Assert.Null(options.EnablePerformanceCounterCollectionModule);
            Assert.Null(options.AddAutoCollectedMetricExtractor);
            Assert.Null(options.EnableDependencyTrackingTelemetryModule);
            Assert.Null(options.ApplicationVersion);
        }

        [Fact]
        public void GetConfigurationOptions_IgnoresCommentedElements()
        {
            // Arrange
            string configContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ApplicationInsights xmlns=""http://schemas.microsoft.com/ApplicationInsights/2013/Settings"">
    <ConnectionString>InstrumentationKey=test</ConnectionString>
    <!--<SamplingRatio>0.5</SamplingRatio>-->
    <!--<DisableTelemetry>true</DisableTelemetry>-->
    <EnableQuickPulseMetricStream>true</EnableQuickPulseMetricStream>
</ApplicationInsights>";

            CreateTestConfigFile(configContent);

            // Act
            var options = ReadConfigFromTestDirectory();

            // Assert
            Assert.NotNull(options);
            Assert.Equal("InstrumentationKey=test", options.ConnectionString);
            Assert.Null(options.SamplingRatio);
            Assert.Null(options.DisableTelemetry);
            Assert.True(options.EnableQuickPulseMetricStream);
        }

        [Fact]
        public void GetConfigurationOptions_UsesCultureInvariantParsing_ForFloatValues()
        {
            // Arrange - use period as decimal separator regardless of current culture
            string configContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ApplicationInsights xmlns=""http://schemas.microsoft.com/ApplicationInsights/2013/Settings"">
    <ConnectionString>test</ConnectionString>
    <SamplingRatio>0.75</SamplingRatio>
    <TracesPerSecond>15.25</TracesPerSecond>
</ApplicationInsights>";

            CreateTestConfigFile(configContent);

            // Act
            var options = ReadConfigFromTestDirectory();

            // Assert
            Assert.NotNull(options);
            Assert.Equal(0.75f, options.SamplingRatio);
            Assert.Equal(15.25, options.TracesPerSecond);
        }

        [Fact]
        public void GetConfigurationOptions_ReturnsNull_ForMalformedBooleanValues()
        {
            // Arrange
            string configContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ApplicationInsights xmlns=""http://schemas.microsoft.com/ApplicationInsights/2013/Settings"">
    <ConnectionString>test</ConnectionString>
    <DisableTelemetry>not-a-bool</DisableTelemetry>
    <EnableQuickPulseMetricStream>yes</EnableQuickPulseMetricStream>
</ApplicationInsights>";

            CreateTestConfigFile(configContent);

            // Act
            var options = ReadConfigFromTestDirectory();

            // Assert
            Assert.NotNull(options);
            Assert.Null(options.DisableTelemetry);
            Assert.Null(options.EnableQuickPulseMetricStream);
        }

        [Fact]
        public void GetConfigurationOptions_ReturnsNull_ForMalformedNumericValues()
        {
            // Arrange
            string configContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ApplicationInsights xmlns=""http://schemas.microsoft.com/ApplicationInsights/2013/Settings"">
    <ConnectionString>test</ConnectionString>
    <SamplingRatio>not-a-number</SamplingRatio>
    <TracesPerSecond>invalid</TracesPerSecond>
</ApplicationInsights>";

            CreateTestConfigFile(configContent);

            // Act
            var options = ReadConfigFromTestDirectory();

            // Assert
            Assert.NotNull(options);
            Assert.Null(options.SamplingRatio);
            Assert.Null(options.TracesPerSecond);
        }

        [Fact]
        public void GetConfigurationOptions_ReturnsNull_ForInvalidXml()
        {
            // Arrange - malformed XML
            string configContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ApplicationInsights xmlns=""http://schemas.microsoft.com/ApplicationInsights/2013/Settings"">
    <ConnectionString>test
</ApplicationInsights>";

            CreateTestConfigFile(configContent);

            // Act
            var options = ReadConfigFromTestDirectory();

            // Assert
            Assert.Null(options);
        }

        [Fact]
        public void GetConfigurationOptions_ReturnsNull_WhenAllPropertiesAreEmpty()
        {
            // Arrange
            string configContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ApplicationInsights xmlns=""http://schemas.microsoft.com/ApplicationInsights/2013/Settings"">
    <ConnectionString></ConnectionString>
    <DisableTelemetry></DisableTelemetry>
</ApplicationInsights>";

            CreateTestConfigFile(configContent);

            // Act
            var options = ReadConfigFromTestDirectory();

            // Assert - should return null when no valid properties found
            Assert.Null(options);
        }

        [Fact]
        public void GetConfigurationOptions_TrimsWhitespace_FromStringValues()
        {
            // Arrange
            string configContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ApplicationInsights xmlns=""http://schemas.microsoft.com/ApplicationInsights/2013/Settings"">
    <ConnectionString>  InstrumentationKey=test  </ConnectionString>
    <StorageDirectory>  C:\Temp  </StorageDirectory>
    <ApplicationVersion>  1.0.0  </ApplicationVersion>
</ApplicationInsights>";

            CreateTestConfigFile(configContent);

            // Act
            var options = ReadConfigFromTestDirectory();

            // Assert
            Assert.NotNull(options);
            Assert.Equal("InstrumentationKey=test", options.ConnectionString);
            Assert.Equal(@"C:\Temp", options.StorageDirectory);
            Assert.Equal("1.0.0", options.ApplicationVersion);
        }

        [Fact]
        public void GetConfigurationOptions_HandlesBooleanVariations()
        {
            // Arrange
            string configContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ApplicationInsights xmlns=""http://schemas.microsoft.com/ApplicationInsights/2013/Settings"">
    <ConnectionString>test</ConnectionString>
    <DisableTelemetry>True</DisableTelemetry>
    <DisableOfflineStorage>FALSE</DisableOfflineStorage>
    <EnableTraceBasedLogsSampler>true</EnableTraceBasedLogsSampler>
</ApplicationInsights>";

            CreateTestConfigFile(configContent);

            // Act
            var options = ReadConfigFromTestDirectory();

            // Assert
            Assert.NotNull(options);
            Assert.True(options.DisableTelemetry);
            Assert.False(options.DisableOfflineStorage);
            Assert.True(options.EnableTraceBasedLogsSampler);
        }

        [Fact]
        public void GetConnectionString_ReturnsConnectionString_WhenPresent()
        {
            // Arrange
            string configContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ApplicationInsights xmlns=""http://schemas.microsoft.com/ApplicationInsights/2013/Settings"">
    <ConnectionString>InstrumentationKey=12345678-1234-1234-1234-123456789012</ConnectionString>
</ApplicationInsights>";

            CreateTestConfigFile(configContent);

            // Act
            var connectionString = ReadConnectionStringFromTestDirectory();

            // Assert
            Assert.Equal("InstrumentationKey=12345678-1234-1234-1234-123456789012", connectionString);
        }

        [Fact]
        public void GetConnectionString_ReturnsNull_WhenConfigMissing()
        {
            // Act
            var connectionString = ApplicationInsightsConfigurationReader.GetConnectionString();

            // Assert
            Assert.Null(connectionString);
        }

        private void CreateTestConfigFile(string content)
        {
            File.WriteAllText(configFilePath, content);
        }

        private ApplicationInsightsConfigOptions ReadConfigFromTestDirectory()
        {
            return ApplicationInsightsConfigurationReader.GetConfigurationOptions();
        }

        private string ReadConnectionStringFromTestDirectory()
        {
            return ApplicationInsightsConfigurationReader.GetConnectionString();
        }
    }
}
