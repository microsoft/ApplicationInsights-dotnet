using System;
using System.IO;
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
        public void ReadsEnableAdaptiveSamplingFromApplicationInsightsSectionInConfig()
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
            Assert.False(options.EnableAdaptiveSampling);

            // Verify underlying AzureMonitorExporterOptions
            var exporterOptions = serviceProvider.GetRequiredService<IOptions<AzureMonitorExporterOptions>>().Value;
            Assert.Equal(1.0F, exporterOptions.SamplingRatio);
            Assert.Null(exporterOptions.TracesPerSecond);
        }

        [Fact]
        public void DefaultEnableAdaptiveSampling_ExporterHasDefaultSamplingValues()
        {
            // ARRANGE - use config that doesn't specify EnableAdaptiveSampling
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
            
            // Verify ApplicationInsightsServiceOptions has default EnableAdaptiveSampling = true
            var options = serviceProvider.GetRequiredService<IOptions<ApplicationInsightsServiceOptions>>().Value;
            Assert.True(options.EnableAdaptiveSampling);
            
            // Verify underlying AzureMonitorExporterOptions for adaptive sampling
            var exporterOptions = serviceProvider.GetRequiredService<IOptions<AzureMonitorExporterOptions>>().Value;
            Assert.Equal(1.0F, exporterOptions.SamplingRatio);
            Assert.Equal(5.0, exporterOptions.TracesPerSecond);
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
            Assert.False(aiOptions.EnableAdaptiveSampling);
            Assert.False(aiOptions.EnableQuickPulseMetricStream);
            
            // Verify AzureMonitorExporterOptions gets the values
            var exporterOptions = serviceProvider.GetRequiredService<IOptions<AzureMonitorExporterOptions>>().Value;
            Assert.Equal("InstrumentationKey=22222222-2222-3333-4444-555555555555", exporterOptions.ConnectionString);
            Assert.Equal(1.0F, exporterOptions.SamplingRatio); // No sampling when EnableAdaptiveSampling is false
            Assert.False(exporterOptions.EnableLiveMetrics);
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

#if AI_ASPNETCORE_WEB
        [Fact]
        public void ReadsRequestCollectionOptionsFromApplicationInsightsSectionInConfig()
        {
            // ARRANGE
            var jsonFullPath = Path.Combine(Directory.GetCurrentDirectory(), "content", "config-all-settings-false.json");
            this.output.WriteLine("json:" + jsonFullPath);
            var config = new ConfigurationBuilder().AddJsonFile(jsonFullPath).Build();
            
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);

            // ACT
            services.AddApplicationInsightsTelemetry();

            // VALIDATE
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<IOptions<ApplicationInsightsServiceOptions>>().Value;
            Assert.False(options.RequestCollectionOptions.InjectResponseHeaders);
            Assert.False(options.RequestCollectionOptions.TrackExceptions);
        }
#endif
    }
}
