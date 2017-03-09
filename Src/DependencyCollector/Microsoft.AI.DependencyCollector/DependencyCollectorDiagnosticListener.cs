namespace Microsoft.ApplicationInsights.DependencyCollector
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.DependencyCollector.Implementation;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.Extensions.DiagnosticAdapter;
    using Web.Implementation;
    using Common;

    /// <summary>
    /// Diagnostic listener implementation that listens for events specific to outgoing depedency requests.
    /// </summary>
    public class DependencyCollectorDiagnosticListener : IObserver<DiagnosticListener>
    {
        /// <summary>
        /// Add Application Insights Dependency Collector services to this .NET Core application.
        /// </summary>
        /// <returns>
        /// An IDisposable that can be disposed to disable the DependencyCollectorDiagnosticListener.
        /// </returns>
        public static IDisposable Enable(TelemetryConfiguration configuration = null)
        {
            if (configuration == null)
            {
                configuration = TelemetryConfiguration.Active;
            }

            return DiagnosticListener.AllListeners.Subscribe(new DependencyCollectorDiagnosticListener(configuration));
        }

        /// <summary>
        /// Source instrumentation header that is added by an application while making http requests and retrieved by the other application when processing incoming requests.
        /// </summary>
        internal const string SourceInstrumentationKeyHeader = "x-ms-request-source-ikey";

        /// <summary>
        /// Target instrumentation header that is added to the response and retrieved by the calling application when processing incoming responses.
        /// </summary>
        internal const string TargetInstrumentationKeyHeader = "x-ms-request-target-ikey";

        /// <summary>
        /// Standard parent Id header.
        /// </summary>
        internal const string StandardParentIdHeader = "x-ms-request-id";

        /// <summary>
        /// Standard root id header.
        /// </summary>
        internal const string StandardRootIdHeader = "x-ms-request-root-id";

        private readonly ApplicationInsightsUrlFilter applicationInsightsUrlFilter;
        private readonly TelemetryClient client;
        private readonly ConcurrentDictionary<Guid, DependencyTelemetry> pendingTelemetry = new ConcurrentDictionary<Guid, DependencyTelemetry>();

        internal DependencyCollectorDiagnosticListener(TelemetryConfiguration configuration)
        {
            this.client = new TelemetryClient(configuration);
            this.client.Context.GetInternalContext().SdkVersion = SdkVersionUtils.GetSdkVersion("rddf");

            this.applicationInsightsUrlFilter = new ApplicationInsightsUrlFilter(configuration);
        }

        /// <summary>
        /// This method gets called once for each existing DiagnosticListener when this
        /// DiagnosticListener is added (<see cref="Enable(TelemetryConfiguration)"/> to the list
        /// of DiagnosticListeners (<see cref="System.Diagnostics.DiagnosticListener.AllListeners"/>).
        /// This method will also be called for each subsequent DiagnosticListener that is added to
        /// the list of DiagnosticListeners.
        /// <seealso cref="IObserver{T}.OnNext(T)"/>
        /// </summary>
        /// <param name="value">The DiagnosticListener that exists when this listener was added to
        /// the list, or a DiagnosticListener that got added after this listener was added.</param>
        public void OnNext(DiagnosticListener value)
        {
            // Comes from https://github.com/dotnet/corefx/blob/master/src/System.Net.Http/src/System/Net/Http/DiagnosticsHandlerLoggingStrings.cs#L12
            if (value.Name == "HttpHandlerDiagnosticListener")
            {
                value.SubscribeWithAdapter(this);
            }
        }

        /// <summary>
        /// Notifies the observer that the provider has finished sending push-based notifications.
        /// <seealso cref="IObserver{T}.OnCompleted()"/>
        /// </summary>
        public void OnCompleted()
        {
        }

        /// <summary>
        /// Notifies the observer that the provider has experienced an error condition.
        /// <seealso cref="IObserver{T}.OnError(Exception)"/>
        /// </summary>
        /// <param name="error">An object that provides additional information about the error.</param>
        public void OnError(Exception error)
        {
        }

        /// <summary>
        /// Get the DependencyTelemetry objects that are still waiting for a response from the dependency. This will most likely only be used for testing purposes.
        /// </summary>
        internal IEnumerable<DependencyTelemetry> PendingDependencyTelemetry
        {
            get { return pendingTelemetry.Values; }
        }

        /// <summary>
        /// Diagnostic event handler method for 'System.Net.Http.Request' event.
        /// </summary>
        [DiagnosticName("System.Net.Http.Request")]
        public void OnRequest(HttpRequestMessage request, Guid loggingRequestId)
        {
            if (request != null && request.RequestUri != null && !this.applicationInsightsUrlFilter.IsApplicationInsightsUrl(request.RequestUri.ToString()))
            {
                string httpMethod = request.Method.Method;
                Uri requestUri = request.RequestUri;
                string resourceName = requestUri.AbsolutePath;
                if (!string.IsNullOrEmpty(httpMethod))
                {
                    resourceName = httpMethod + " " + resourceName;
                }

                DependencyTelemetry telemetry = new DependencyTelemetry();
                this.client.Initialize(telemetry);
                telemetry.Start();
                telemetry.Name = resourceName;
                telemetry.Target = requestUri.Host;
                telemetry.Type = "Http";
                telemetry.Data = requestUri.OriginalString;
                this.pendingTelemetry.TryAdd(loggingRequestId, telemetry);

                if (!request.Headers.Contains(SourceInstrumentationKeyHeader))
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
            }
        }

        /// <summary>
        /// Diagnostic event handler method for 'System.Net.Http.Response' event. Even in the case of an exception, this will still be called.
        /// See https://github.com/dotnet/corefx/blob/master/src/System.Net.Http/src/System/Net/Http/DiagnosticsHandler.cs#L71 for more information.
        /// </summary>
        [DiagnosticName("System.Net.Http.Response")]
        public void OnResponse(HttpResponseMessage response, Guid loggingRequestId)
        {
            if (response != null)
            {
                DependencyTelemetry telemetry;
                if (this.pendingTelemetry.TryRemove(loggingRequestId, out telemetry))
                {
                    if (response.Headers.Contains(TargetInstrumentationKeyHeader))
                    {
                        string targetInstrumentationKeyHash = response.Headers.GetValues(TargetInstrumentationKeyHeader).SingleOrDefault();

                        // We only add the cross component correlation key if the key does not represent the current component.
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
}