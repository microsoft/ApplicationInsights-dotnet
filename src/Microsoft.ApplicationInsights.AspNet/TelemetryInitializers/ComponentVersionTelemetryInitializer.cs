namespace Microsoft.ApplicationInsights.AspNet.TelemetryInitializers
{
    using System;
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
        private bool isConfigBuilt = false;
        private IConfiguration _configuration;
        private string appVersion;

        public ComponentVersionTelemetryInitializer(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            try
            {
                if (!this.isConfigBuilt)
                {
                    var config = new ConfigurationBuilder()
                        .AddJsonFile("project.json")
                        .Build();
                    this.appVersion = config[_versionConfigurationOption];
                    this.isConfigBuilt = true;
                }
            }
            catch(Exception e)
            {
                // Add logging through event source. We may not want to through the exception here?
            }
        }

        protected override void OnInitializeTelemetry(HttpContext platformContext, RequestTelemetry requestTelemetry, ITelemetry telemetry)
        {
            if (string.IsNullOrEmpty(telemetry.Context.Component.Version) && !string.IsNullOrEmpty(this.appVersion))
            {
               telemetry.Context.Component.Version = this.appVersion;
            }
        }
    }
}
