using System;
using System.IO;
using System.Linq;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

#if AI_ASPNETCORE_WEB
namespace Microsoft.ApplicationInsights.AspNetCore.Tests
{
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
#else
namespace Microsoft.ApplicationInsights.WorkerService.Tests
{
    using System.Reflection;
    using Microsoft.ApplicationInsights.WorkerService;
#endif

    public class ConfigurationTests
    {
        private readonly ITestOutputHelper output;
        private const string TestConnectionString = "InstrumentationKey=11111111-2222-3333-4444-555555555555;IngestionEndpoint=http://127.0.0.1";

        public ConfigurationTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void ReadsConnectionStringFromApplicationInsightsSectionInConfig()
        {
            // ARRANGE
            var jsonFullPath = Path.Combine(Directory.GetCurrentDirectory(), "content", "config-connection-string.json");
            this.output.WriteLine("json:" + jsonFullPath);
            var config = new ConfigurationBuilder().AddJsonFile(jsonFullPath).Build();
            
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);

            // ACT
#if AI_ASPNETCORE_WEB
            services.AddApplicationInsightsTelemetry();
#else
            services.AddApplicationInsightsTelemetryWorkerService();
#endif

            // VALIDATE
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<IOptions<ApplicationInsightsServiceOptions>>().Value;
            Assert.Equal(TestConnectionString, options.ConnectionString);
        }

        [Fact]
        public void ReadsEnableQuickPulseMetricStreamFromApplicationInsightsSectionInConfig()
        {
            // ARRANGE
            var jsonFullPath = Path.Combine(Directory.GetCurrentDirectory(), "content", "config-all-settings-false.json");
            this.output.WriteLine("json:" + jsonFullPath);
            var config = new ConfigurationBuilder().AddJsonFile(jsonFullPath).Build();
            
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);

            // ACT
#if AI_ASPNETCORE_WEB
            services.AddApplicationInsightsTelemetry();
#else
            services.AddApplicationInsightsTelemetryWorkerService();
#endif

            // VALIDATE
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<IOptions<ApplicationInsightsServiceOptions>>().Value;
            Assert.False(options.EnableQuickPulseMetricStream);
        }

        [Fact]
        public void ReadsApplicationVersionFromApplicationInsightsSectionInConfig()
        {
            // ARRANGE
            var jsonFullPath = Path.Combine(Directory.GetCurrentDirectory(), "content", "config-all-settings-true.json");
            this.output.WriteLine("json:" + jsonFullPath);
            var config = new ConfigurationBuilder().AddJsonFile(jsonFullPath).Build();
            
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);

            // ACT
#if AI_ASPNETCORE_WEB
            services.AddApplicationInsightsTelemetry();
#else
            services.AddApplicationInsightsTelemetryWorkerService();
#endif

            // VALIDATE
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<IOptions<ApplicationInsightsServiceOptions>>().Value;
            Assert.Equal("1.0.0", options.ApplicationVersion);
        }

        [Fact]
        public void ApplicationVersionResourceDetectorReturnsServiceVersion()
        {
            // ARRANGE & ACT
            var detector = new ApplicationVersionResourceDetector("2.5.0");
            var resource = detector.Detect();

            // VALIDATE
            Assert.NotEqual(OpenTelemetry.Resources.Resource.Empty, resource);
            var attributes = resource.Attributes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            Assert.True(attributes.ContainsKey("service.version"));
            Assert.Equal("2.5.0", attributes["service.version"]);
        }

        [Fact]
        public void ApplicationVersionResourceDetectorReturnsEmptyWhenVersionIsNull()
        {
            // ARRANGE & ACT
            var detector = new ApplicationVersionResourceDetector(null);
            var resource = detector.Detect();

            // VALIDATE
            Assert.Equal(OpenTelemetry.Resources.Resource.Empty, resource);
        }

        [Fact]
        public void ApplicationVersionResourceDetectorReturnsEmptyWhenVersionIsEmpty()
        {
            // ARRANGE & ACT
            var detector = new ApplicationVersionResourceDetector(string.Empty);
            var resource = detector.Detect();

            // VALIDATE
            Assert.Equal(OpenTelemetry.Resources.Resource.Empty, resource);
        }

        [Fact]
        public void ConfigurationFlowsFromApplicationInsightsSectionToAzureMonitorExporter()
        {
            // ARRANGE
            var jsonFullPath = Path.Combine(Directory.GetCurrentDirectory(), "content", "config-all-settings-false.json");
            this.output.WriteLine("json:" + jsonFullPath);
            var config = new ConfigurationBuilder().AddJsonFile(jsonFullPath).Build();
            
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);

            // ACT
#if AI_ASPNETCORE_WEB
            services.AddApplicationInsightsTelemetry();
#else
            services.AddApplicationInsightsTelemetryWorkerService();
#endif

            // VALIDATE
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            
            // Verify ApplicationInsightsServiceOptions
            var aiOptions = serviceProvider.GetRequiredService<IOptions<ApplicationInsightsServiceOptions>>().Value;
            Assert.Equal("InstrumentationKey=22222222-2222-3333-4444-555555555555", aiOptions.ConnectionString);
            Assert.False(aiOptions.EnableQuickPulseMetricStream);
            
            // Verify AzureMonitorExporterOptions gets the values
            var exporterOptions = serviceProvider.GetRequiredService<IOptions<AzureMonitorExporterOptions>>().Value;
            Assert.Equal("InstrumentationKey=22222222-2222-3333-4444-555555555555", exporterOptions.ConnectionString);
            Assert.Equal(1.0F, exporterOptions.SamplingRatio);
            Assert.Equal(5, exporterOptions.TracesPerSecond);
            Assert.False(exporterOptions.EnableLiveMetrics);
        }

        [Fact]
        public void ReadsTracesPerSecondFromApplicationInsightsSectionInConfig()
        {
            // ARRANGE
            var jsonFullPath = Path.Combine(Directory.GetCurrentDirectory(), "content", "config-sampling-tracespersecond.json");
            this.output.WriteLine("json:" + jsonFullPath);
            var config = new ConfigurationBuilder().AddJsonFile(jsonFullPath).Build();
            
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);

            // ACT
#if AI_ASPNETCORE_WEB
            services.AddApplicationInsightsTelemetry();
#else
            services.AddApplicationInsightsTelemetryWorkerService();
#endif

            // VALIDATE
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<IOptions<ApplicationInsightsServiceOptions>>().Value;
            Assert.Equal(10.0, options.TracesPerSecond);

            // Verify it flows to exporter options
            var exporterOptions = serviceProvider.GetRequiredService<IOptions<AzureMonitorExporterOptions>>().Value;
            Assert.Equal(10.0, exporterOptions.TracesPerSecond);
        }

        [Fact]
        public void ReadsSamplingRatioFromApplicationInsightsSectionInConfig()
        {
            // ARRANGE
            var jsonFullPath = Path.Combine(Directory.GetCurrentDirectory(), "content", "config-samplingratio.json");
            this.output.WriteLine("json:" + jsonFullPath);
            var config = new ConfigurationBuilder().AddJsonFile(jsonFullPath).Build();
            
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);

            // ACT
#if AI_ASPNETCORE_WEB
            services.AddApplicationInsightsTelemetry();
#else
            services.AddApplicationInsightsTelemetryWorkerService();
#endif

            // VALIDATE
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<IOptions<ApplicationInsightsServiceOptions>>().Value;
            Assert.Equal(0.5f, options.SamplingRatio);

            // Verify it flows to exporter options
            var exporterOptions = serviceProvider.GetRequiredService<IOptions<AzureMonitorExporterOptions>>().Value;
            Assert.Equal(0.5f, exporterOptions.SamplingRatio);
        }

        [Fact]
        public void TracesPerSecondIgnoresNonPositiveValues()
        {
            // ARRANGE
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
            services.AddSingleton<IConfiguration>(config);

            // ACT
#if AI_ASPNETCORE_WEB
            services.AddApplicationInsightsTelemetry(options =>
#else
            services.AddApplicationInsightsTelemetryWorkerService(options =>
#endif
            {
                options.ConnectionString = "InstrumentationKey=11111111-2222-3333-4444-555555555555";
                options.TracesPerSecond = -1.0; // Invalid value
            });

            // VALIDATE
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var exporterOptions = serviceProvider.GetRequiredService<IOptions<AzureMonitorExporterOptions>>().Value;

            // TracesPerSecond should not be set to a negative value - it should remain at default
            Assert.Equal(5.0, exporterOptions.TracesPerSecond);
        }

        [Fact]
        public void SamplingRatioIgnoresInvalidValues()
        {
            // ARRANGE
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
            services.AddSingleton<IConfiguration>(config);

            // ACT
#if AI_ASPNETCORE_WEB
            services.AddApplicationInsightsTelemetry(options =>
#else
            services.AddApplicationInsightsTelemetryWorkerService(options =>
#endif
            {
                options.ConnectionString = "InstrumentationKey=11111111-2222-3333-4444-555555555555";
                options.SamplingRatio = 1.5f; // Invalid value (> 1.0)
            });

            // VALIDATE
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var exporterOptions = serviceProvider.GetRequiredService<IOptions<AzureMonitorExporterOptions>>().Value;

            // SamplingRatio should not be set to an invalid value
            Assert.NotEqual(1.5f, exporterOptions.SamplingRatio);
        }

        [Fact]
        public void EnvironmentVariablesTakePrecedenceOverAppSettings()
        {
            // ARRANGE
            const string envConnectionString = "InstrumentationKey=AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE;IngestionEndpoint=http://env-endpoint";
            Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING", envConnectionString);
            
            try
            {
                var jsonFullPath = Path.Combine(Directory.GetCurrentDirectory(), "content", "config-connection-string.json");
                this.output.WriteLine("json:" + jsonFullPath);
                var config = new ConfigurationBuilder().AddJsonFile(jsonFullPath).AddEnvironmentVariables().Build();
                
                var services = new ServiceCollection();
                services.AddSingleton<IConfiguration>(config);

                // ACT
#if AI_ASPNETCORE_WEB
                services.AddApplicationInsightsTelemetry();
#else
                services.AddApplicationInsightsTelemetryWorkerService();
#endif

                // VALIDATE
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var options = serviceProvider.GetRequiredService<IOptions<ApplicationInsightsServiceOptions>>().Value;
                Assert.Equal(envConnectionString, options.ConnectionString);
            }
            finally
            {
                Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING", null);
            }
        }

        [Fact]
        public void ExplicitConfigurationTakesPrecedenceOverDefaultConfiguration()
        {
            // ARRANGE
            const string explicitConnectionString = "InstrumentationKey=CCCCCCCC-DDDD-EEEE-FFFF-111111111111;IngestionEndpoint=http://explicit-endpoint";
            var jsonFullPath = Path.Combine(Directory.GetCurrentDirectory(), "content", "config-connection-string.json");
            this.output.WriteLine("json:" + jsonFullPath);
            var config = new ConfigurationBuilder().AddJsonFile(jsonFullPath).Build();
            
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);

            // ACT - Pass explicit options
#if AI_ASPNETCORE_WEB
            services.AddApplicationInsightsTelemetry(options =>
#else
            services.AddApplicationInsightsTelemetryWorkerService(options =>
#endif
            {
                options.ConnectionString = explicitConnectionString;
            });

            // VALIDATE
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<IOptions<ApplicationInsightsServiceOptions>>().Value;
            Assert.Equal(explicitConnectionString, options.ConnectionString);
        }

        [Fact]
        public void EnableTraceBasedLogsSamplerFlowsToExporterOptions()
        {
            // ARRANGE
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
            services.AddSingleton<IConfiguration>(config);

            // ACT
#if AI_ASPNETCORE_WEB
            services.AddApplicationInsightsTelemetry(options =>
#else
            services.AddApplicationInsightsTelemetryWorkerService(options =>
#endif
            {
                options.ConnectionString = TestConnectionString;
                options.EnableTraceBasedLogsSampler = false;
            });

            // VALIDATE
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var aiOptions = serviceProvider.GetRequiredService<IOptions<ApplicationInsightsServiceOptions>>().Value;
            Assert.False(aiOptions.EnableTraceBasedLogsSampler);

            var exporterOptions = serviceProvider.GetRequiredService<IOptions<AzureMonitorExporterOptions>>().Value;
            Assert.False(exporterOptions.EnableTraceBasedLogsSampler);
        }

        [Fact]
        public void StorageOptionsFlowFromTelemetryConfigurationToExporterOptions()
        {
            // ARRANGE
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
            services.AddSingleton<IConfiguration>(config);

            // ACT
#if AI_ASPNETCORE_WEB
            services.AddApplicationInsightsTelemetry(options =>
#else
            services.AddApplicationInsightsTelemetryWorkerService(options =>
#endif
            {
                options.ConnectionString = TestConnectionString;
            });

            // Configure TelemetryConfiguration with storage options
            services.Configure<Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration>(tc =>
            {
                tc.StorageDirectory = @"C:\CustomStorage";
                tc.DisableOfflineStorage = true;
            });

            // VALIDATE
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var exporterOptions = serviceProvider.GetRequiredService<IOptions<AzureMonitorExporterOptions>>().Value;

            Assert.Equal(@"C:\CustomStorage", exporterOptions.StorageDirectory);
            Assert.True(exporterOptions.DisableOfflineStorage);
        }
    }
}
