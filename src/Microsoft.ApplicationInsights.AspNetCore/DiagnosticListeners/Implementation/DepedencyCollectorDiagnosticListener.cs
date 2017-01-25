namespace Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners
{
    using System;
    using System.Collections.Concurrent;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Extensions.DiagnosticAdapter;

    /// <summary>
    /// <see cref="IApplicationInsightDiagnosticListener"/> implementation that listens for HTTP requests from this AspNetCore application.
    /// </summary>
    internal class DependencyCollectorDiagnosticListener : IApplicationInsightDiagnosticListener
    {
        /// <summary>
        /// Source instrumentation header that is added by an application while making http requests and retrieved by the other application when processing incoming requests.
        /// </summary>
        private const string SourceInstrumentationKeyHeader = "x-ms-request-source-ikey";

        /// <summary>
        /// Target instrumentation header that is added to the response and retrieved by the calling application when processing incoming responses.
        /// </summary>
        private const string TargetInstrumentationKeyHeader = "x-ms-request-target-ikey";

        /// <summary>
        /// Standard parent Id header.
        /// </summary>
        private const string StandardParentIdHeader = "x-ms-request-id";

        /// <summary>
        /// Standard root id header.
        /// </summary>
        private const string StandardRootIdHeader = "x-ms-request-root-id";

        private readonly TelemetryClient client;
        private readonly ConcurrentDictionary<Guid, DependencyTelemetry> requestTelemetry = new ConcurrentDictionary<Guid, DependencyTelemetry>();

        /// <inheritdoc/>
        public string ListenerName { get; } = "HttpHandlerDiagnosticListener"; // This value comes from: https://github.com/dotnet/corefx/blob/bffef76f6af208e2042a2f27bc081ee908bb390b/src/Common/src/System/Net/Http/HttpHandlerLoggingStrings.cs#L12

        public DependencyCollectorDiagnosticListener(TelemetryClient client)
        {
            this.client = client;
        }

        /// <summary>
        /// Diagnostic event handler method for 'System.Net.Http.Request' event.
        /// </summary>
        [DiagnosticName("System.Net.Http.Request")]
        public void OnRequestSent(HttpRequestMessage request, Guid loggingRequestId, long timestamp)
        {
            string httpMethod = request.Method.Method;
            Uri requestUri = request.RequestUri;
            string resourceName = requestUri.AbsolutePath;
            if (!string.IsNullOrEmpty(httpMethod))
            {
                resourceName = httpMethod + " " + resourceName;
            }

            DependencyTelemetry telemetry = new DependencyTelemetry();
            telemetry.Start();
            this.client.Initialize(telemetry);

            telemetry.Name = resourceName;
            telemetry.Target = requestUri.Host;
            telemetry.Type = "Http";
            telemetry.Data = requestUri.OriginalString;

            // Add the source instrumentation key header if collection is enabled, the request host is not in the excluded list and the same header doesn't already exist
            if (!string.IsNullOrEmpty(telemetry.Context.InstrumentationKey) && !request.Headers.Contains(SourceInstrumentationKeyHeader))
            {
                request.Headers.Add(SourceInstrumentationKeyHeader, InstrumentationKeyHashLookupHelper.GetInstrumentationKeyHash(telemetry.Context.InstrumentationKey));
            }

            // Add the root ID
            string rootId = telemetry.Context.Operation.Id;
            if (!string.IsNullOrEmpty(rootId) && !request.Headers.Contains(StandardRootIdHeader))
            {
                request.Headers.Add(StandardRootIdHeader, rootId);
            }

            // Add the parent ID
            string parentId = telemetry.Id;
            if (!string.IsNullOrEmpty(parentId) && !request.Headers.Contains(StandardParentIdHeader))
            {
                request.Headers.Add(StandardParentIdHeader, parentId);
            }

            this.requestTelemetry.TryAdd(loggingRequestId, telemetry);
        }

        /// <summary>
        /// Diagnostic event handler method for 'System.Net.Http.Response' event.
        /// </summary>
        [DiagnosticName("System.Net.Http.Response")]
        public void OnResponseReceived(HttpResponseMessage response, Guid loggingRequestId, long timestamp)
        {
            DependencyTelemetry telemetry;
            if (this.requestTelemetry.TryRemove(loggingRequestId, out telemetry))
            {
                if (response.Headers.Contains(TargetInstrumentationKeyHeader))
                {
                    string targetInstrumentationKeyHash = response.Headers.GetValues(TargetInstrumentationKeyHeader).SingleOrDefault();

                    // We only add the cross component correlation key if the key does not remain the current component.
                    if (!string.IsNullOrEmpty(targetInstrumentationKeyHash) && targetInstrumentationKeyHash != InstrumentationKeyHashLookupHelper.GetInstrumentationKeyHash(telemetry.Context.InstrumentationKey))
                    {
                        telemetry.Type = "Application Insights";
                        telemetry.Target += " | " + targetInstrumentationKeyHash;
                    }
                }

                int statusCode = (int)response.StatusCode;
                telemetry.ResultCode = (0 < statusCode) ? statusCode.ToString(CultureInfo.InvariantCulture) : string.Empty;
                telemetry.Success = (0 < statusCode) && (statusCode < 400);

                telemetry.Stop();
                this.client.Track(telemetry);
            }
        }
    }
}
