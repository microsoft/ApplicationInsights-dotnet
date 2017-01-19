namespace Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Features;

    /// <summary>
    /// This will allow to mark synthetic traffic from availability tests
    /// </summary>
    internal class SyntheticTelemetryInitializer : TelemetryInitializerBase
    {
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
                var runIdHeader = platformContext.Request.Headers[SyntheticTestRunId];
                var locationHeader = platformContext.Request.Headers[SyntheticTestLocation];

                if (!string.IsNullOrEmpty(runIdHeader) &&
                    !string.IsNullOrEmpty(locationHeader))
                {
                    telemetry.Context.Operation.SyntheticSource = SyntheticSourceHeaderValue;

                    telemetry.Context.User.Id = locationHeader + "_" + runIdHeader;
                    telemetry.Context.Session.Id = runIdHeader;
                }
            }
        }
    }
}
