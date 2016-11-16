namespace Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers
{
    using System;
    using System.Diagnostics;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Http;

    public class OperationNameTelemetryInitializer : TelemetryInitializerBase
    {

        public OperationNameTelemetryInitializer(IHttpContextAccessor httpContextAccessor, DiagnosticListener telemetryListener)
            : base(httpContextAccessor)
        {
            if (telemetryListener == null)
            {
                throw new ArgumentNullException("telemetryListener");
            }

            telemetryListener.SubscribeWithAdapter(this);
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
                    requestTelemetry.Name = name;
                    telemetry.Context.Operation.Name = name;
                }
            }
        }
    }
}