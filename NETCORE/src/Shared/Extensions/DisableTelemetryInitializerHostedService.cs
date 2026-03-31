namespace Microsoft.Extensions.DependencyInjection
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;

    /// <summary>
    /// A hosted service that runs before OpenTelemetry's TelemetryHostedService to set
    /// OTEL_SDK_DISABLED in IConfiguration when TelemetryConfiguration.DisableTelemetry is true.
    /// This ensures the OTel SDK sees the disabled flag before constructing providers.
    /// </summary>
    internal sealed class DisableTelemetryInitializerHostedService : IHostedService
    {
        private readonly TelemetryConfiguration telemetryConfiguration;
        private readonly IConfiguration configuration;

        public DisableTelemetryInitializerHostedService(
            TelemetryConfiguration telemetryConfiguration,
            IConfiguration configuration)
        {
            this.telemetryConfiguration = telemetryConfiguration;
            this.configuration = configuration;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (this.telemetryConfiguration.DisableTelemetry)
            {
                this.configuration["OTEL_SDK_DISABLED"] = "true";
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
