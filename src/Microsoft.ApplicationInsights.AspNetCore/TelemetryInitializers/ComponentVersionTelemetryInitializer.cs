namespace Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers
{
    using ApplicationInsights.Extensibility;
    using Channel;
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// A telemetry initializer that populates telemetry.Context.Component.Version to the value read from configuration.
    /// </summary>
    public class ComponentVersionTelemetryInitializer : ITelemetryInitializer
    {
        private readonly string version;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentVersionTelemetryInitializer" /> class.
        /// </summary>
        /// <param name="options">Provides the Application Version to be added to the telemetry.</param>
        public ComponentVersionTelemetryInitializer(IOptions<ApplicationInsightsServiceOptions> options)
        {
             this.version = options.Value.ApplicationVersion;
        }

        /// <inheritdoc />
        public void Initialize(ITelemetry telemetry)
        {
            if (string.IsNullOrEmpty(telemetry.Context.Component.Version))
            {
                if (!string.IsNullOrEmpty(this.version))
                {
                    telemetry.Context.Component.Version = this.version;
                }
            }
        }
    }
}
