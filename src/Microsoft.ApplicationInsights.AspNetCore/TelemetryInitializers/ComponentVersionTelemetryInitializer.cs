namespace Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers
{
    using Channel;
    using Microsoft.Extensions.Configuration;
    using ApplicationInsights.Extensibility;

    /// <summary>
    /// A telemetry initializer that populates telemetry.Context.Component.Version to the value read from configuration
    /// </summary>
    public class ComponentVersionTelemetryInitializer : ITelemetryInitializer
    {
        private const string _versionConfigurationOption = "version";
        private IConfiguration _configuration;

        public ComponentVersionTelemetryInitializer(IConfiguration configuration)
        {
            if (configuration != null)
            {
                this._configuration = configuration;
            }
        }

        public void Initialize(ITelemetry telemetry)
        {
            if (string.IsNullOrEmpty(telemetry.Context.Component.Version))
            {
                if (this._configuration != null)
                {
                    string version = this._configuration[_versionConfigurationOption];
                    if (!string.IsNullOrEmpty(version))
                    {
                        telemetry.Context.Component.Version = version;
                    }
                }
            }
        }
    }
}
