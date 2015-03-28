namespace Microsoft.ApplicationInsights.AspNet.DataCollection
{
    using System;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNet.Http;
    
    /// <summary>
    /// Telemetry initializer populates user agent (telemetry.Context.User.UserAgent) for 
    /// all telemetry data items.
    /// </summary>
    public class WebUserAgentTelemetryInitializer : TelemetryInitializerBase
    {
        public WebUserAgentTelemetryInitializer(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        protected override void OnInitializeTelemetry(HttpContext platformContext, RequestTelemetry requestTelemetry, ITelemetry telemetry)
        {
            if (string.IsNullOrEmpty(requestTelemetry.Context.User.UserAgent))
            {
                var userAgent = platformContext.Request.Headers["User-Agent"];
                requestTelemetry.Context.User.UserAgent = userAgent;
            }

            telemetry.Context.User.UserAgent = requestTelemetry.Context.User.UserAgent;
        }
    }
}