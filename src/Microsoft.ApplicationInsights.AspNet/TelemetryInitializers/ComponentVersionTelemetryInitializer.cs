namespace Microsoft.ApplicationInsights.AspNet.TelemetryInitializers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Channel;
    using DataContracts;
    using Microsoft.AspNet.Http;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// A telemetry initializer that populates telemetry.Context.Component.Version to the value read from project.json
    /// </summary>
    public class ComponentVersionTelemetryInitializer : TelemetryInitializerBase
    {
        private const string _versionConfigurationOption = "version";
        private IConfiguration _configuration;

        public ComponentVersionTelemetryInitializer(IHttpContextAccessor httpContextAccessor, IConfiguration configuration):base(httpContextAccessor)
        {
            if (configuration != null)
            {
                _configuration = configuration;
            }
        }

        protected override void OnInitializeTelemetry(HttpContext platformContext, RequestTelemetry requestTelemetry, ITelemetry telemetry)
        {
            if (string.IsNullOrEmpty(telemetry.Context.Component.Version))
            {
                if (_configuration != null) {                
                    telemetry.Context.Component.Version = _configuration[_versionConfigurationOption].ToString();
                }
            }
        }
    }
}
