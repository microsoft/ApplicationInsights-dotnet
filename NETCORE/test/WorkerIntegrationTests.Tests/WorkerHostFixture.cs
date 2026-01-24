using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Azure.Core.Pipeline;
using Azure.Monitor.OpenTelemetry.Exporter;
using IntegrationTests.WorkerApp;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.WorkerService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using Xunit;

#nullable enable

namespace IntegrationTests.Tests
{
    public sealed class WorkerHostFixture : IAsyncLifetime
    {
        private const string TestConnectionString = "InstrumentationKey=ikey";
        private IHost? host;

        private IHost Host => this.host ?? throw new InvalidOperationException("Worker host has not been initialized.");

        internal TelemetryCollector Telemetry { get; } = new TelemetryCollector();

        public IBackgroundTaskQueue TaskQueue => this.Host.Services.GetRequiredService<IBackgroundTaskQueue>();

        public async Task InitializeAsync()
        {
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            Activity.ForceDefaultIdFormat = true;

            var builder = Program.CreateHostBuilder(Array.Empty<string>());

            builder.ConfigureLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Trace);
            });

            builder.ConfigureServices(services =>
            {
                services.AddApplicationInsightsTelemetryWorkerService();
                services.AddSingleton<TelemetryCollector>(_ => this.Telemetry);
                services.AddSingleton<HttpPipelineTransport>(provider =>
                    new RecordingTransport(provider.GetRequiredService<TelemetryCollector>()));

                services.AddOptions<AzureMonitorExporterOptions>()
                        .Configure<HttpPipelineTransport>((options, transport) =>
                        {
                            options.Transport = transport;
                            options.DisableOfflineStorage = true;
                        });

                services.Configure<ApplicationInsightsServiceOptions>(options =>
                {
                    options.ConnectionString = TestConnectionString;
                    options.EnableQuickPulseMetricStream = false;
                    options.AddAutoCollectedMetricExtractor = false;
                });

                services.ConfigureOpenTelemetryTracerProvider(tracer =>
                {
                    tracer.AddSource(WorkerDiagnostics.BackgroundWorkSourceName);
                });
            });

            this.host = builder.Build();
            await this.host.StartAsync().ConfigureAwait(false);

            var exporterOptions = this.Host.Services.GetRequiredService<IOptions<AzureMonitorExporterOptions>>().Value;
            if (exporterOptions.Transport is not RecordingTransport)
            {
                throw new InvalidOperationException("Recording transport was not applied to Azure Monitor exporter.");
            }

            var telemetryClient = this.Host.Services.GetRequiredService<TelemetryClient>();
            telemetryClient.TrackTrace("worker-fixture-warmup");
            await this.WaitForTelemetryAsync(expectedItemCount: 1).ConfigureAwait(false);
            this.Telemetry.Clear();
        }

        public async Task DisposeAsync()
        {
            if (this.host != null)
            {
                await this.host.StopAsync().ConfigureAwait(false);
                this.host.Dispose();
            }
        }

        public async Task WaitForTelemetryAsync(int expectedItemCount, TimeSpan? timeout = null)
        {
            var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
            while (DateTime.UtcNow < deadline)
            {
                if (this.Telemetry.GetTotalItemCount() >= expectedItemCount)
                {
                    return;
                }

                await Task.Delay(100).ConfigureAwait(false);
            }

            await Task.Delay(200).ConfigureAwait(false);
        }
    }
}

