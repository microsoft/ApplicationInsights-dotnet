namespace Microsoft.ApplicationInsights.AspNet
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNet.Builder;
    using Microsoft.AspNet.Http;

    /// <summary>
    /// Sends telemetry about exceptions thrown by the application to the Microsoft Application Insights service.
    /// </summary>
    public sealed class ExceptionTrackingMiddleware
    {
        private readonly RequestDelegate next;
        private readonly TelemetryClient telemetryClient;

        public ExceptionTrackingMiddleware(RequestDelegate next, TelemetryClient client)
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
            catch (Exception exception)
            {
                var exceptionTelemetry = new ExceptionTelemetry(exception);
                exceptionTelemetry.HandledAt = ExceptionHandledAt.Platform;
                this.telemetryClient.Track(exceptionTelemetry);

                throw;
            }
        }
    }
}