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
        private const string versionConfigurationOption = "dependencies:Microsoft.ApplicationInsights.AspNet";

        public ComponentVersionTelemetryInitializer(IHttpContextAccessor httpContextAccessor):base(httpContextAccessor)
        {
            //No need to initialize anything
        }

        protected override void OnInitializeTelemetry(HttpContext platformContext, RequestTelemetry requestTelemetry, ITelemetry telemetry)
        {
            if (string.IsNullOrEmpty(telemetry.Context.Component.Version))
            {
                var config = new ConfigurationBuilder().AddJsonFile("project.json").Build();

                if (config == null) {                
                    telemetry.Context.Component.Version = null;
                } else {
                    telemetry.Context.Component.Version = config[versionConfigurationOption].ToString();
                }
            }
        }
    }
}
