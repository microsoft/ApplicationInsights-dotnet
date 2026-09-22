using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
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

        [Theory]
        [InlineData("default", false)]
        [InlineData("one", false)]
        [InlineData("one", true)]
        [InlineData("all", false)]
        [InlineData("all", true)]
        [InlineData("drop", false)]
        [InlineData("drop", true)]
        [InlineData("customize", false)]
        [InlineData("customize", true)]
        public void HttpClientMetricsRespectCustomerViews(string configuration, bool configureBeforeDistro)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());
            Action<MeterProviderBuilder> configureMetrics = metrics => metrics.AddView(instrument =>
            {
                if (instrument.Meter.Name != "System.Net.Http")
                {
                    return null;
                }

                if (configuration == "all" || (configuration == "one" && instrument.Name == "http.client.open_connections"))
                {
                    return new MetricStreamConfiguration();
                }

                if (instrument.Name == "http.client.request.duration")
                {
                    if (configuration == "drop")
                    {
                        return MetricStreamConfiguration.Drop;
                    }

                    if (configuration == "customize")
                    {
                        return new ExplicitBucketHistogramConfiguration
                        {
                            Name = "custom.duration",
                            Boundaries = new[] { 0.05, 0.5 },
                            TagKeys = new[] { "kept" },
                        };
                    }
                }

                return null;
            });

            if (configureBeforeDistro)
            {
                services.ConfigureOpenTelemetryMeterProvider(configureMetrics);
            }

#if AI_ASPNETCORE_WEB
            services.AddApplicationInsightsTelemetry(options =>
#else
            services.AddApplicationInsightsTelemetryWorkerService(options =>
#endif
            {
                options.ConnectionString = TestConnectionString;
                options.EnableQuickPulseMetricStream = false;
            });

            if (!configureBeforeDistro)
            {
                services.ConfigureOpenTelemetryMeterProvider(configureMetrics);
            }

            var exportedMetrics = new List<OpenTelemetry.Metrics.Metric>();
            services.ConfigureOpenTelemetryMeterProvider(metrics => metrics
                .SetResourceBuilder(ResourceBuilder.CreateEmpty())
                .AddMeter("HttpClientMetrics.Tests", "System.Net.NameResolution")
                .AddReader(new BaseExportingMetricReader(new CollectingMetricExporter(exportedMetrics))));

            using var serviceProvider = services.BuildServiceProvider();
            var meterProvider = serviceProvider.GetRequiredService<MeterProvider>();
            using var meter = new Meter("System.Net.Http", "HttpClientMetrics.Tests");
            meter.CreateHistogram<double>("http.client.request.duration", "s").Record(
                0.1, new KeyValuePair<string, object>("kept", "value"), new KeyValuePair<string, object>("removed", "value"));
            foreach (var name in new[]
            {
                "http.client.active_requests",
                "http.client.open_connections",
                "http.client.connection.duration",
                "http.client.request.time_in_queue",
                "http.client.future_metric",
            })
            {
                if (name == "http.client.active_requests" || name == "http.client.open_connections")
                {
                    meter.CreateUpDownCounter<long>(name).Add(1);
                }
                else
                {
                    meter.CreateHistogram<double>(name).Record(1);
                }
            }

            using var customMeter = new Meter("HttpClientMetrics.Tests");
            using var serverMeter = new Meter("Microsoft.AspNetCore.Hosting", "HttpClientMetrics.Tests");
            using var dnsMeter = new Meter("System.Net.NameResolution", "HttpClientMetrics.Tests");
            customMeter.CreateCounter<long>("http.client.open_connections").Add(1);
            serverMeter.CreateUpDownCounter<long>("http.server.active_requests").Add(1);
            dnsMeter.CreateHistogram<double>("dns.lookup.duration").Record(0.1);

            meterProvider.ForceFlush();

            // Meter version isolates this test's synthetic instruments from any real System.Net.Http meter in the process.
            var httpMetrics = exportedMetrics.Where(metric => metric.MeterName == meter.Name && metric.MeterVersion == meter.Version).ToList();
            Assert.Equal(configuration == "all" ? 6 : configuration == "one" ? 2 : configuration == "drop" ? 0 : 1, httpMetrics.Count);
            if (configuration != "drop")
            {
                var durationMetric = Assert.Single(httpMetrics, metric => metric.Name == (configuration == "customize" ? "custom.duration" : "http.client.request.duration"));
                Assert.Equal("s", durationMetric.Unit);
                foreach (ref readonly var point in durationMetric.GetMetricPoints())
                {
                    Assert.Equal(1, point.GetHistogramCount());
                    Assert.Equal(0.1, point.GetHistogramSum());
                    if (configuration == "customize")
                    {
                        Assert.Equal(1, point.Tags.Count);
                        var bounds = new List<double>();
                        foreach (var bucket in point.GetHistogramBuckets())
                        {
                            bounds.Add(bucket.ExplicitBound);
                        }

                        Assert.Equal(new[] { 0.05, 0.5, double.PositiveInfinity }, bounds);
                    }
                }
            }

            Assert.Contains(exportedMetrics, metric => metric.MeterName == customMeter.Name);
            Assert.Contains(exportedMetrics, metric => metric.MeterName == dnsMeter.Name);
#if AI_ASPNETCORE_WEB
            Assert.Contains(exportedMetrics, metric => metric.MeterName == serverMeter.Name);
#else
            Assert.DoesNotContain(exportedMetrics, metric => metric.MeterName == serverMeter.Name);
#endif
        }

        [Fact]
        public async Task HttpClientMetricsCollectOnlyRequestDurationFromRealRequests()
        {
            using var server = new HttpListener();
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            server.Prefixes.Add($"http://localhost:{port}/");
            server.Start();
            _ = Task.Run(async () =>
            {
                try
                {
                    var context = await server.GetContextAsync();
                    context.Response.StatusCode = 200;
                    context.Response.Close();
                }
                catch
                {
                    // The listener is stopped at the end of the test.
                }
            });

            var exportedMetrics = new List<OpenTelemetry.Metrics.Metric>();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());
#if AI_ASPNETCORE_WEB
            services.AddApplicationInsightsTelemetry(options =>
#else
            services.AddApplicationInsightsTelemetryWorkerService(options =>
#endif
            {
                options.ConnectionString = TestConnectionString;
                options.EnableQuickPulseMetricStream = false;
            });

            services.ConfigureOpenTelemetryMeterProvider(metrics => metrics
                .SetResourceBuilder(ResourceBuilder.CreateEmpty())
                .AddReader(new BaseExportingMetricReader(new CollectingMetricExporter(exportedMetrics))));

            using var serviceProvider = services.BuildServiceProvider();
            var meterProvider = serviceProvider.GetRequiredService<MeterProvider>();

            using (var httpClient = new HttpClient())
            {
                using var response = await httpClient.GetAsync($"http://localhost:{port}/probe");
            }

            meterProvider.ForceFlush();
            server.Stop();

            // Catches the SDK's instrument name drifting from the name the runtime actually emits.
            Assert.Equal(
                new[] { "http.client.request.duration" },
                exportedMetrics.Where(metric => metric.MeterName == "System.Net.Http").Select(metric => metric.Name).Distinct().ToArray());
        }

        private sealed class CollectingMetricExporter : BaseExporter<OpenTelemetry.Metrics.Metric>
        {
            private readonly List<OpenTelemetry.Metrics.Metric> metrics;

            public CollectingMetricExporter(List<OpenTelemetry.Metrics.Metric> metrics)
            {
                this.metrics = metrics;
            }

            public override ExportResult Export(in Batch<OpenTelemetry.Metrics.Metric> batch)
            {
                foreach (var metric in batch)
                {
                    this.metrics.Add(metric);
                }

                return ExportResult.Success;
            }
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

        [Theory]
        [InlineData("2.5.0", "2.5.0")]
        [InlineData(null, null)]
        [InlineData("", null)]
        [InlineData("   ", null)]
        public void ApplicationVersionResourceDetectorReturnsExpectedResource(string inputVersion, string expectedVersion)
        {
            // ARRANGE & ACT
            var detector = new ApplicationVersionResourceDetector(inputVersion);
            var resource = detector.Detect();

            // VALIDATE
            if (expectedVersion == null)
            {
                Assert.Equal(OpenTelemetry.Resources.Resource.Empty, resource);
            }
            else
            {
                Assert.NotEqual(OpenTelemetry.Resources.Resource.Empty, resource);
                var attributes = resource.Attributes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                Assert.True(attributes.ContainsKey("service.version"));
                Assert.Equal(expectedVersion, attributes["service.version"]);
            }
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
