namespace Microsoft.ApplicationInsights.AspNetCore
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Sends telemetry about exceptions thrown by the application to the Microsoft Application Insights service.
    /// </summary>
    public sealed class ExceptionTrackingMiddleware
    {
        private readonly RequestDelegate next;
        private readonly TelemetryClient telemetryClient;
        private readonly string sdkVersion;

        public ExceptionTrackingMiddleware(RequestDelegate next, TelemetryClient client)
        {
            this.next = next;
            this.telemetryClient = client;
            this.sdkVersion = SdkVersionUtils.VersionPrefix + SdkVersionUtils.GetFrameworkType() + SdkVersionUtils.GetAssemblyVersion();
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
                exceptionTelemetry.Context.GetInternalContext().SdkVersion = this.sdkVersion;
                this.telemetryClient.Track(exceptionTelemetry);

                throw;
            }
        }
    }
}