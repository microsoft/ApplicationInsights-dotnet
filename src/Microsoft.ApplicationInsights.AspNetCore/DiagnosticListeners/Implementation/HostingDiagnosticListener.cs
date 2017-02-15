namespace Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners
{
    using System;
    using System.Diagnostics;
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DiagnosticAdapter;
    using Microsoft.Extensions.Primitives;

    /// <summary>
    /// <see cref="IApplicationInsightDiagnosticListener"/> implementation that listens for events specific to AspNetCore hosting layer.
    /// </summary>
    internal class HostingDiagnosticListener : IApplicationInsightDiagnosticListener
    {
        private readonly TelemetryClient client;
        private readonly ContextData<long> beginRequestTimestamp = new ContextData<long>();
        private readonly string sdkVersion;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:HostingDiagnosticListener"/> class.
        /// </summary>
        /// <param name="client"><see cref="TelemetryClient"/> to post traces to.</param>
        public HostingDiagnosticListener(TelemetryClient client)
        {
            this.client = client;
            this.sdkVersion = SdkVersionUtils.VersionPrefix + SdkVersionUtils.GetAssemblyVersion();
        }

        /// <inheritdoc/>
        public string ListenerName { get; } = "Microsoft.AspNetCore";

        /// <summary>
        /// Diagnostic event handler method for 'Microsoft.AspNetCore.Hosting.BeginRequest' event.
        /// </summary>
        [DiagnosticName("Microsoft.AspNetCore.Hosting.BeginRequest")]
        public void OnBeginRequest(HttpContext httpContext, long timestamp)
        {
            if (this.client.IsEnabled())
            {
                RequestTelemetry requestTelemetry = new RequestTelemetry();
                this.client.Initialize(requestTelemetry);
                requestTelemetry.Id = httpContext.TraceIdentifier;
                httpContext.Features.Set(requestTelemetry);

                this.beginRequestTimestamp.Value = timestamp;
                this.client.Context.Operation.Id = requestTelemetry.Id;

                IHeaderDictionary responseHeaders = httpContext.Response?.Headers;
                if (responseHeaders != null && !responseHeaders.ContainsKey(RequestResponseHeaders.TargetInstrumentationKeyHeader) && !string.IsNullOrEmpty(requestTelemetry.Context.InstrumentationKey))
                {
                    responseHeaders.Add(RequestResponseHeaders.TargetInstrumentationKeyHeader, new StringValues(InstrumentationKeyHashLookupHelper.GetInstrumentationKeyHash(requestTelemetry.Context.InstrumentationKey)));
                }
            }
        }

        /// <summary>
        /// Diagnostic event handler method for 'Microsoft.AspNetCore.Hosting.EndRequest' event.
        /// </summary>
        [DiagnosticName("Microsoft.AspNetCore.Hosting.EndRequest")]
        public void OnEndRequest(HttpContext httpContext, long timestamp)
        {
            EndRequest(httpContext, timestamp);
        }

        /// <summary>
        /// Diagnostic event handler method for 'Microsoft.AspNetCore.Hosting.UnhandledException' event.
        /// </summary>
        [DiagnosticName("Microsoft.AspNetCore.Hosting.UnhandledException")]
        public void OnHostingException(HttpContext httpContext, Exception exception)
        {
            this.OnException(httpContext, exception);
            this.EndRequest(httpContext, Stopwatch.GetTimestamp());
        }

        /// <summary>
        /// Diagnostic event handler method for 'Microsoft.AspNetCore.Hosting.HandledException' event.
        /// </summary>
        [DiagnosticName("Microsoft.AspNetCore.Diagnostics.HandledException")]
        public void OnDiagnosticsHandledException(HttpContext httpContext, Exception exception)
        {
            this.OnException(httpContext, exception);
        }

        /// <summary>
        /// Diagnostic event handler method for 'Microsoft.AspNetCore.Diagnostics.UnhandledException' event.
        /// </summary>
        [DiagnosticName("Microsoft.AspNetCore.Diagnostics.UnhandledException")]
        public void OnDiagnosticsUnhandledException(HttpContext httpContext, Exception exception)
        {
            this.OnException(httpContext, exception);
        }

        private void EndRequest(HttpContext httpContext, long timestamp)
        {
            if (this.client.IsEnabled())
            {
                var telemetry = httpContext?.Features.Get<RequestTelemetry>();

                if (telemetry == null)
                {
                    return;
                }

                telemetry.Duration = new TimeSpan(timestamp - this.beginRequestTimestamp.Value);
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

                IHeaderDictionary requestHeaders = httpContext.Request?.Headers;
                if (requestHeaders != null)
                {
                    string sourceIkey = requestHeaders[RequestResponseHeaders.SourceInstrumentationKeyHeader];

                    // If the source header is present on the incoming request, and it is an external component (not the same ikey as the one used by the current component), populate the source field.
                    if (!string.IsNullOrEmpty(sourceIkey) && sourceIkey != InstrumentationKeyHashLookupHelper.GetInstrumentationKeyHash(telemetry.Context.InstrumentationKey))
                    {
                        telemetry.Source = sourceIkey;
                    }
                }

                if (string.IsNullOrEmpty(telemetry.Name))
                {
                    telemetry.Name = httpContext.Request.Method + " " + httpContext.Request.Path.Value;
                }

                telemetry.HttpMethod = httpContext.Request.Method;
                telemetry.Url = httpContext.Request.GetUri();
                telemetry.Context.GetInternalContext().SdkVersion = this.sdkVersion;
                this.client.TrackRequest(telemetry);
            }
        }

        private void OnException(HttpContext httpContext, Exception exception)
        {
            if (this.client.IsEnabled())
            {
                var telemetry = httpContext?.Features.Get<RequestTelemetry>();
                if (telemetry != null)
                {
                    telemetry.Success = false;
                }

                var exceptionTelemetry = new ExceptionTelemetry(exception);
                exceptionTelemetry.HandledAt = ExceptionHandledAt.Platform;
                exceptionTelemetry.Context.GetInternalContext().SdkVersion = this.sdkVersion;
                this.client.Track(exceptionTelemetry);
            }
        }
    }
}