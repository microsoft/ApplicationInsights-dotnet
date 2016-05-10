namespace Microsoft.ApplicationInsights.AspNet.TelemetryInitializers
{
    using System;
    using Channel;
    using DataContracts;
    using Extensibility.Implementation.Tracing;
    using Microsoft.AspNet.Http;
    using Microsoft.Extensions.Configuration;
    using ApplicationInsights.Extensibility;

    /// <summary>
    /// A telemetry initializer that populates telemetry.Context.Component.Version to the value read from project.json
    /// </summary>
    public class ComponentVersionTelemetryInitializer : ITelemetryInitializer
    {
        private const string _versionConfigurationOption = "version";
        private bool _isConfigBuilt = false;
        private IConfiguration _configuration;
        private string _appVersion;

        public ComponentVersionTelemetryInitializer()
        {
            try
            {
                if (!this._isConfigBuilt)
                {
                    var config = new ConfigurationBuilder()
                        .AddJsonFile("project.json")
                        .Build();
                    this._appVersion = config[_versionConfigurationOption];
                    this._isConfigBuilt = true;
                }
            }
            catch(Exception e)
            {
                AspNetEventSource.Instance.LogComponentVersionTelemetryInitializerFailsToAccessProjectJson();
            }
        }

        public void Initialize(ITelemetry telemetry)
        {
            if (string.IsNullOrEmpty(telemetry.Context.Component.Version) && !string.IsNullOrEmpty(this._appVersion))
            {
               telemetry.Context.Component.Version = this._appVersion;
            }
        }
    }
}
