namespace Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers
{
    using System;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Hosting;

    public class OperationIdTelemetryInitializer : TelemetryInitializerBase
    {
        public OperationIdTelemetryInitializer(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        { }

        protected override void OnInitializeTelemetry(HttpContext platformContext, RequestTelemetry requestTelemetry, ITelemetry telemetry)
        {
            if (string.IsNullOrEmpty(telemetry.Context.Operation.Id))
            {
                telemetry.Context.Operation.Id = requestTelemetry.Id;
            }
        }
    }
}