namespace Microsoft.ApplicationInsights.AspNet.TelemetryInitializers
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNet.Http;
    using Microsoft.AspNet.Http.Features;

    /// <summary>
    /// This will allow to mark synthetic traffic from availability tests
    /// </summary>
    public class SyntheticTelemetryInitializer : TelemetryInitializerBase
    {
        private const string HeaderNameDefault = "GsmSyntheticTestRunId";
        private const string SyntheticTestRunId = "SyntheticTest-RunId";
        private const string SyntheticTestLocation = "SyntheticTest-Location";

        private const string SyntheticSourceHeaderValue = "Application Insights Availability Monitoring";

        public SyntheticTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
             : base(httpContextAccessor)
        {

        }

        protected override void OnInitializeTelemetry(HttpContext platformContext, RequestTelemetry requestTelemetry, ITelemetry telemetry)
        {
            if (string.IsNullOrEmpty(telemetry.Context.Operation.SyntheticSource))
            {
                var headerValue = platformContext.Request.Headers[HeaderNameDefault];
                if (!string.IsNullOrEmpty(headerValue))
                {
                    telemetry.Context.Operation.SyntheticSource = SyntheticSourceHeaderValue;

                    var locationHeader = platformContext.Request.Headers[SyntheticTestLocation];
                    if (!string.IsNullOrEmpty(headerValue))
                    {
                        telemetry.Context.User.Id = locationHeader;
                    }

                    var runIdHeader = platformContext.Request.Headers[SyntheticTestRunId];
                    if (!string.IsNullOrEmpty(headerValue))
                    {
                        telemetry.Context.Session.Id = runIdHeader;
                    }
                } else
                {
                    telemetry.Context.Operation.SyntheticSource = null;
                }
            } else
            {
                AspNetEventSource.Instance.SyntheticTelemetryInitializerOnInitializeTelemetryHeaderNotPresent();
            }
        }
    }
}
