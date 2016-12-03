namespace Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers
{
    using System;
    using System.Diagnostics;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Http;

    internal class OperationNameTelemetryInitializer : TelemetryInitializerBase
    {
        public OperationNameTelemetryInitializer(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
        }

        protected override void OnInitializeTelemetry(HttpContext platformContext, RequestTelemetry requestTelemetry, ITelemetry telemetry)
        {
            if (string.IsNullOrEmpty(telemetry.Context.Operation.Name))
            {
                if (!string.IsNullOrEmpty(requestTelemetry.Name))
                {
                    telemetry.Context.Operation.Name = requestTelemetry.Name;
                }
                else
                {
                    // We didn't get BeforeAction notification
                    string name = platformContext.Request.Method + " " + platformContext.Request.Path.Value;
                    telemetry.Context.Operation.Name = name;
                }
            }
        }
    }
}