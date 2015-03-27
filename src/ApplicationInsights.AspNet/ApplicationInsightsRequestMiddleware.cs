namespace Microsoft.ApplicationInsights.AspNet
{
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.AspNet.Implementation;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNet.Builder;
    using Microsoft.AspNet.Hosting;
    using Microsoft.AspNet.Http;
    using Microsoft.AspNet.RequestContainer;
    using Microsoft.Framework.ConfigurationModel;
    using Microsoft.Framework.DependencyInjection;
    using Microsoft.Framework.Logging;
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    public sealed class ApplicationInsightsRequestMiddleware
    {
        private readonly RequestDelegate next;
        private readonly TelemetryClient telemetryClient;
        private readonly IServiceProvider serviceProvider;

        public ApplicationInsightsRequestMiddleware(IServiceProvider svcs, RequestDelegate next, TelemetryClient client)
        {
            this.serviceProvider = svcs;
            this.telemetryClient = client;
            this.next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            this.serviceProvider.GetService<HttpContextHolder>().Context = httpContext;

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

                    var telemetry = this.serviceProvider.GetService<RequestTelemetry>();
                    telemetry.Name = httpContext.Request.Method + " " + httpContext.Request.Path.Value;
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

