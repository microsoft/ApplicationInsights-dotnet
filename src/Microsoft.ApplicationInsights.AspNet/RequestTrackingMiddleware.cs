namespace Microsoft.ApplicationInsights.AspNet
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNet.Builder;
    using Microsoft.AspNet.Http;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.AspNet.Extensions;


    /// <summary>
    /// Sends telemetry about requests handled by the application to the Microsoft Application Insights service.
    /// </summary>
    public sealed class RequestTrackingMiddleware
    {
        private readonly RequestDelegate next;
        private readonly TelemetryClient telemetryClient;
        private readonly string sdkVersion;
        
        public RequestTrackingMiddleware(RequestDelegate next, TelemetryClient client)
        {
            this.telemetryClient = client;
            this.next = next;
            this.sdkVersion = SdkVersionUtils.VersionPrefix + SdkVersionUtils.GetAssemblyVersion();
        }

        public async Task Invoke(HttpContext httpContext, RequestTelemetry telemetry)
        {
            telemetry.Timestamp = DateTimeOffset.UtcNow;

            var sw = new Stopwatch();
            sw.Start();

            bool requestFailed = false;

            try
            {
                await this.next.Invoke(httpContext);
            }
            catch (Exception)
            {
                requestFailed = true;
            }
            finally
            {
                sw.Stop();

                telemetry.Duration = sw.Elapsed;
                telemetry.ResponseCode = httpContext.Response.StatusCode.ToString();
                telemetry.Success = (!requestFailed) && (httpContext.Response.StatusCode < 400);
                telemetry.HttpMethod = httpContext.Request.Method;
                telemetry.Url = httpContext.Request.GetUri();
                telemetry.Context.GetInternalContext().SdkVersion = this.sdkVersion;
                    
                this.telemetryClient.TrackRequest(telemetry);
            }
        }
    }
}

