using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers
{
    using Channel;
    using ApplicationInsights.Extensibility;

    /// <summary>
    /// A telemetry initializer that populates telemetry.Context.Component.Version to the value read from configuration
    /// </summary>
    public class ComponentVersionTelemetryInitializer : ITelemetryInitializer
    {
        private string _version;

        public ComponentVersionTelemetryInitializer(IOptions<ApplicationInsightsServiceOptions> options)
        {
             this._version = options.Value.Version;
        }

        public void Initialize(ITelemetry telemetry)
        {
            if (string.IsNullOrEmpty(telemetry.Context.Component.Version))
            {
                if (!string.IsNullOrEmpty(_version))
                {
                    telemetry.Context.Component.Version = _version;
                }
            }
        }
    }
}
