using System.Diagnostics;
using Azure.Core.Pipeline;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IntegrationTests.Tests
{
    public class CustomWebApplicationFactory<TStartup>
    : WebApplicationFactory<TStartup> where TStartup : class
    {
        internal TelemetryCollector Telemetry { get; } = new TelemetryCollector();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            Activity.ForceDefaultIdFormat = true;

            builder.ConfigureServices(services =>
            {
                services.AddLogging(loggingBuilder =>
                    loggingBuilder.AddFilter(
                        "Microsoft.AspNetCore.DataProtection.KeyManagement.XmlKeyManager",
                        LogLevel.None)
                                   .AddFilter(
                                       "IntegrationTests.WebApp.Controllers.HomeController",
                                       LogLevel.Warning));

                services.AddSingleton<TelemetryCollector>(_ => this.Telemetry);
                services.AddSingleton<HttpPipelineTransport>(provider =>
                    new RecordingTransport(provider.GetRequiredService<TelemetryCollector>()));

                services.AddOptions<AzureMonitorExporterOptions>()
                        .Configure<HttpPipelineTransport>((options, transport) =>
                        {
                            options.Transport = transport;
                            options.DisableOfflineStorage = true;
                        });

                services.AddApplicationInsightsTelemetry(options =>
                {
                    options.AddAutoCollectedMetricExtractor = false;
                    options.EnableQuickPulseMetricStream = false;
                    options.EnableAdaptiveSampling = false;
                    options.ConnectionString = "InstrumentationKey=ikey";
                });
            });
        }
    }
}
