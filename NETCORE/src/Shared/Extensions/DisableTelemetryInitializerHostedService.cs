namespace Microsoft.Extensions.DependencyInjection
{
    using System;
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
        private readonly IServiceProvider serviceProvider;

        public DisableTelemetryInitializerHostedService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var telemetryConfig = this.serviceProvider.GetService<TelemetryConfiguration>();
            if (telemetryConfig != null && telemetryConfig.DisableTelemetry)
            {
                var config = this.serviceProvider.GetRequiredService<IConfiguration>();
                config["OTEL_SDK_DISABLED"] = "true";
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
