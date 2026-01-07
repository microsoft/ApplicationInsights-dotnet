namespace Microsoft.ApplicationInsights.Web.Tests
{
    using System;
    using System.IO;
    using System.Xml.Linq;
    using Microsoft.ApplicationInsights.Web.Implementation;
    using Xunit;

    [Collection("ApplicationInsightsHttpModule")] // Same collection as ApplicationInsightsHttpModuleTests to avoid file conflicts
    public class SampleConfigValidationTests
    {
        [Fact]
        public void SampleConfig_IsValidXml()
        {
            // Arrange - Find the sample config file
            string sampleConfigPath = FindSampleConfigFile();
            Assert.True(File.Exists(sampleConfigPath), $"Sample config file not found at: {sampleConfigPath}");

            // Act & Assert - Should parse without throwing
            var doc = XDocument.Load(sampleConfigPath);
            Assert.NotNull(doc);
            Assert.NotNull(doc.Root);
        }

        [Fact]
        public void SampleConfig_AllCommentedPropertiesParseCorrectly()
        {
            // Arrange - Find and read the sample config
            string sampleConfigPath = FindSampleConfigFile();
            string content = File.ReadAllText(sampleConfigPath);

            // Uncomment all properties
            content = content.Replace("<!-- <DisableTelemetry>false</DisableTelemetry> -->", "<DisableTelemetry>false</DisableTelemetry>");
            content = content.Replace("<!-- <ApplicationVersion>1.0.0</ApplicationVersion> -->", "<ApplicationVersion>1.0.0</ApplicationVersion>");
            content = content.Replace("<!-- <SamplingRatio>1.0</SamplingRatio> -->", "<SamplingRatio>1.0</SamplingRatio>");
            content = content.Replace("<!-- <TracesPerSecond>5.0</TracesPerSecond> -->", "<TracesPerSecond>5.0</TracesPerSecond>");
            content = content.Replace("<!-- <StorageDirectory>C:\\temp\\applicationinsights</StorageDirectory> -->", "<StorageDirectory>C:\\temp\\applicationinsights</StorageDirectory>");
            content = content.Replace("<!-- <DisableOfflineStorage>false</DisableOfflineStorage> -->", "<DisableOfflineStorage>false</DisableOfflineStorage>");
            content = content.Replace("<!-- <EnableTraceBasedLogsSampler>true</EnableTraceBasedLogsSampler> -->", "<EnableTraceBasedLogsSampler>true</EnableTraceBasedLogsSampler>");
            content = content.Replace("<!-- <EnableQuickPulseMetricStream>true</EnableQuickPulseMetricStream> -->", "<EnableQuickPulseMetricStream>true</EnableQuickPulseMetricStream>");
            content = content.Replace("<!-- <EnablePerformanceCounterCollectionModule>true</EnablePerformanceCounterCollectionModule> -->", "<EnablePerformanceCounterCollectionModule>true</EnablePerformanceCounterCollectionModule>");
            content = content.Replace("<!-- <AddAutoCollectedMetricExtractor>true</AddAutoCollectedMetricExtractor> -->", "<AddAutoCollectedMetricExtractor>true</AddAutoCollectedMetricExtractor>");
            content = content.Replace("<!-- <EnableDependencyTrackingTelemetryModule>true</EnableDependencyTrackingTelemetryModule> -->", "<EnableDependencyTrackingTelemetryModule>true</EnableDependencyTrackingTelemetryModule>");

            // Write to temp config file
            string tempConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ApplicationInsights.config");
            try
            {
                File.WriteAllText(tempConfigPath, content);

                // Act - Parse the config
                var options = ApplicationInsightsConfigurationReader.GetConfigurationOptions();

                // Assert - All uncommented properties should be parsed
                Assert.NotNull(options);
                Assert.Contains("InstrumentationKey=00000000-0000-0000-0000-000000000000", options.ConnectionString);
                Assert.False(options.DisableTelemetry);
                Assert.Equal("1.0.0", options.ApplicationVersion);
                Assert.Equal(1.0f, options.SamplingRatio);
                Assert.Equal(5.0, options.TracesPerSecond);
                Assert.Equal(@"C:\temp\applicationinsights", options.StorageDirectory);
                Assert.False(options.DisableOfflineStorage);
                Assert.True(options.EnableTraceBasedLogsSampler);
                Assert.True(options.EnableQuickPulseMetricStream);
                Assert.True(options.EnablePerformanceCounterCollectionModule);
                Assert.True(options.AddAutoCollectedMetricExtractor);
                Assert.True(options.EnableDependencyTrackingTelemetryModule);
            }
            finally
            {
                if (File.Exists(tempConfigPath))
                {
                    File.Delete(tempConfigPath);
                }
            }
        }

        [Fact]
        public void SampleConfig_HasCorrectNamespace()
        {
            // Arrange
            string sampleConfigPath = FindSampleConfigFile();
            var doc = XDocument.Load(sampleConfigPath);

            // Assert
            Assert.Equal("http://schemas.microsoft.com/ApplicationInsights/2013/Settings", doc.Root.Name.NamespaceName);
        }

        [Fact]
        public void SampleConfig_ContainsRequiredConnectionString()
        {
            // Arrange
            string sampleConfigPath = FindSampleConfigFile();
            var doc = XDocument.Load(sampleConfigPath);
            XNamespace ns = "http://schemas.microsoft.com/ApplicationInsights/2013/Settings";

            // Act
            var connectionStringElement = doc.Root.Element(ns + "ConnectionString");

            // Assert
            Assert.NotNull(connectionStringElement);
            Assert.False(string.IsNullOrWhiteSpace(connectionStringElement.Value));
        }

        [Fact]
        public void SampleConfig_AllPropertiesMatchImplementation()
        {
            // Arrange
            string sampleConfigPath = FindSampleConfigFile();
            string content = File.ReadAllText(sampleConfigPath);

            // Act & Assert - Verify all properties mentioned in sample exist in implementation
            var implementationProperties = typeof(ApplicationInsightsConfigOptions).GetProperties();
            var propertyNames = new string[implementationProperties.Length];
            for (int i = 0; i < implementationProperties.Length; i++)
            {
                propertyNames[i] = implementationProperties[i].Name;
            }

            // Check that sample contains documentation for all properties
            Assert.Contains("ConnectionString", propertyNames);
            Assert.Contains("DisableTelemetry", propertyNames);
            Assert.Contains("SamplingRatio", propertyNames);
            Assert.Contains("TracesPerSecond", propertyNames);
            Assert.Contains("StorageDirectory", propertyNames);
            Assert.Contains("DisableOfflineStorage", propertyNames);
            Assert.Contains("EnableTraceBasedLogsSampler", propertyNames);
            Assert.Contains("EnableQuickPulseMetricStream", propertyNames);
            Assert.Contains("EnablePerformanceCounterCollectionModule", propertyNames);
            Assert.Contains("AddAutoCollectedMetricExtractor", propertyNames);
            Assert.Contains("EnableDependencyTrackingTelemetryModule", propertyNames);
            Assert.Contains("ApplicationVersion", propertyNames);

            // Verify sample file mentions these properties
            Assert.Contains("ConnectionString", content);
            Assert.Contains("DisableTelemetry", content);
            Assert.Contains("SamplingRatio", content);
            Assert.Contains("TracesPerSecond", content);
            Assert.Contains("StorageDirectory", content);
            Assert.Contains("DisableOfflineStorage", content);
            Assert.Contains("EnableTraceBasedLogsSampler", content);
            Assert.Contains("EnableQuickPulseMetricStream", content);
            Assert.Contains("EnablePerformanceCounterCollectionModule", content);
            Assert.Contains("AddAutoCollectedMetricExtractor", content);
            Assert.Contains("EnableDependencyTrackingTelemetryModule", content);
            Assert.Contains("ApplicationVersion", content);
        }

        private string FindSampleConfigFile()
        {
            // Try multiple potential locations
            var potentialPaths = new[]
            {
                // Relative to test assembly
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Web", "applicationinsights.config.sample"),
                // Relative to repo root
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "..", "WEB", "Src", "Web", "Web", "applicationinsights.config.sample"),
                // Direct path from solution root
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "Web", "applicationinsights.config.sample"),
            };

            foreach (var path in potentialPaths)
            {
                string normalized = Path.GetFullPath(path);
                if (File.Exists(normalized))
                {
                    return normalized;
                }
            }

            // Last resort - search upward from test assembly location
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            while (currentDir != null)
            {
                string candidatePath = Path.Combine(currentDir, "WEB", "Src", "Web", "Web", "applicationinsights.config.sample");
                if (File.Exists(candidatePath))
                {
                    return candidatePath;
                }

                candidatePath = Path.Combine(currentDir, "applicationinsights.config.sample");
                if (File.Exists(candidatePath))
                {
                    return candidatePath;
                }

                currentDir = Directory.GetParent(currentDir)?.FullName;
            }

            throw new FileNotFoundException("Could not find applicationinsights.config.sample file");
        }
    }
}
