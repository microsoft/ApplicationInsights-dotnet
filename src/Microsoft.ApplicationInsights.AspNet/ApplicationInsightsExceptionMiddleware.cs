namespace Microsoft.ApplicationInsights.AspNet
{
    using Microsoft.AspNet.Builder;
    using Microsoft.AspNet.Http;
    using System;
    using System.Threading.Tasks;

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
                    this.telemetryClient.TrackException(exp);
                }

                throw;
            }
        }
    }
}
