namespace Microsoft.ApplicationInsights.AspNet.TelemetryInitializers
{
    using System;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNet.Http;

    public class WebOperationIdTelemetryInitializer : TelemetryInitializerBase
    {
        public WebOperationIdTelemetryInitializer(IServiceProvider serviceProvider) : base(serviceProvider)
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