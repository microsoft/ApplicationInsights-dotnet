using System;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DiagnosticAdapter;
using Microsoft.ApplicationInsights.Extensibility.Implementation;

namespace Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners
{
    public class AspNetCoreHostingDiagnosticListener: IApplicationInsightDiagnosticListener
    {
        private readonly TelemetryClient _client;
        private readonly ContextData<long> _beginRequestTimestamp = new ContextData<long>();
        private readonly string _sdkVersion;

        public AspNetCoreHostingDiagnosticListener(TelemetryClient client)
        {
            _client = client;
            _sdkVersion = SdkVersionUtils.VersionPrefix + SdkVersionUtils.GetAssemblyVersion();
        }

        public string ListenerName { get; } = "Microsoft.AspNetCore";

        [DiagnosticName("Microsoft.AspNetCore.Hosting.BeginRequest")]
        public void OnBeginRequest(HttpContext httpContext, long timestamp)
        {
            httpContext.Features.Set(new RequestTelemetry());

            _beginRequestTimestamp.Value = timestamp;
            _client.Context.Operation.Id = httpContext.TraceIdentifier;

            Console.WriteLine("OnBeginRequest");
        }

        [DiagnosticName("Microsoft.AspNetCore.Hosting.EndRequest")]
        public void OnEndRequest(HttpContext httpContext, long timestamp)
        {
            var telemetry = httpContext.Features.Get<RequestTelemetry>();

            telemetry.Duration = new TimeSpan(timestamp - _beginRequestTimestamp.Value);
            telemetry.Timestamp = DateTime.Now - telemetry.Duration;
            telemetry.ResponseCode = httpContext.Response.StatusCode.ToString();

            var successExitCode = httpContext.Response.StatusCode < 400;
            if (telemetry.Success == null)
            {
                telemetry.Success = successExitCode;
            }
            else
            {
                telemetry.Success &= successExitCode;
            }

            if (string.IsNullOrEmpty(telemetry.Name))
            {
                telemetry.Name = httpContext.Request.Method + " " + httpContext.Request.Path.Value;
            }
            telemetry.HttpMethod = httpContext.Request.Method;
            telemetry.Url = httpContext.Request.GetUri();
            telemetry.Context.GetInternalContext().SdkVersion = _sdkVersion;
            _client.TrackRequest(telemetry);

            Console.WriteLine("OnEndRequest");
        }

        [DiagnosticName("Microsoft.AspNetCore.Hosting.UnhandledException")]
        public void OnHostingException(HttpContext httpContext, Exception exception)
        {
            OnException(httpContext, exception);
        }

        [DiagnosticName("Microsoft.AspNetCore.Diagnostics.HandledException")]
        public void OnDiagnosticsHandledException(HttpContext httpContext, Exception exception)
        {
            OnException(httpContext, exception);
        }

        [DiagnosticName("Microsoft.AspNetCore.Diagnostics.UnhandledException")]
        public void OnDiagnosticsUnhandledException(HttpContext httpContext, Exception exception)
        {
            OnException(httpContext, exception);
        }

        private void OnException(HttpContext httpContext, Exception exception)
        {
            var telemetry = httpContext?.Features.Get<RequestTelemetry>();
            if (telemetry != null)
            {
                telemetry.Success = false;
            }

            var exceptionTelemetry = new ExceptionTelemetry(exception);
            exceptionTelemetry.HandledAt = ExceptionHandledAt.Platform;
            exceptionTelemetry.Context.GetInternalContext().SdkVersion = _sdkVersion;
            _client.Track(exceptionTelemetry);
        }
    }
}
