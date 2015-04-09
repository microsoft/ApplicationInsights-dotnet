namespace Microsoft.ApplicationInsights.AspNet
{
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNet.Builder;
    using Microsoft.AspNet.Http;
    using Microsoft.Framework.DependencyInjection;
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    public sealed class ApplicationInsightsRequestMiddleware
    {
        private readonly RequestDelegate next;
        private readonly TelemetryClient telemetryClient;
        
        public ApplicationInsightsRequestMiddleware(RequestDelegate next, TelemetryClient client)
        {
            this.telemetryClient = client;
            this.next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var sw = new Stopwatch();
            sw.Start();

            var now = DateTimeOffset.UtcNow;

            try
            {
                await this.next.Invoke(httpContext);
            }
            finally
            {
                if (this.telemetryClient != null)
                {
                    sw.Stop();

                    RequestTelemetry telemetry;
                    if (httpContext.RequestServices == null)
                    {
                        // TODO: diagnostics
                        // TELEMETRY INITIALIZERS WON'T WORK
                        telemetry = new RequestTelemetry();
                    }
                    else
                    {
                        telemetry = httpContext.RequestServices.GetService<RequestTelemetry>();
                    }

                    telemetry.Timestamp = now;
                    telemetry.Duration = sw.Elapsed;
                    telemetry.ResponseCode = httpContext.Response.StatusCode.ToString();
                    telemetry.Success = httpContext.Response.StatusCode < 400;

                    this.telemetryClient.TrackRequest(telemetry);
                }
            }
        }
    }
}

