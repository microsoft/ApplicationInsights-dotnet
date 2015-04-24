namespace Microsoft.ApplicationInsights.AspNet
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNet.Builder;
    using Microsoft.AspNet.Http;

    public sealed class ApplicationInsightsExceptionMiddleware
    {
        private readonly RequestDelegate next;
        private readonly TelemetryClient telemetryClient;

        public ApplicationInsightsExceptionMiddleware(RequestDelegate next, TelemetryClient client)
        {
            this.next = next;
            this.telemetryClient = client;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            try
            {
                await this.next.Invoke(httpContext);
            }
            catch (Exception exp)
            {
                if (this.telemetryClient != null)
                {
                    var exceptionTelemetry = new ExceptionTelemetry(exp);
                    exceptionTelemetry.HandledAt = ExceptionHandledAt.Platform;
                    this.telemetryClient.Track(exceptionTelemetry);
                }

                throw;
            }
        }
    }
}