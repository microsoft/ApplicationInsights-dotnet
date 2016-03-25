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
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// A telemetry initializer that populates telemetry.Context.Component.Version to the value read from project.json
    /// </summary>
    public class ComponentVersionTelemetryInitializer : ITelemetryInitializer
    {
        private const string _versionConfigurationOption = "version";
        private IConfiguration _configuration;

        public ComponentVersionTelemetryInitializer(IConfiguration configuration)
        {
            if (configuration != null)
            {
                _configuration = configuration;
            }
        }

        public void Initialize(ITelemetry telemetry)
        {
            if (string.IsNullOrEmpty(telemetry.Context.Component.Version))
            {
                if (_configuration != null)
                {
                    if (!string.IsNullOrEmpty(_configuration[_versionConfigurationOption]))
                    {
                        telemetry.Context.Component.Version = _configuration[_versionConfigurationOption].ToString();
                    }
                }
            }
        }
    }
}
