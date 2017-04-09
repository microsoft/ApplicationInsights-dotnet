namespace Microsoft.ApplicationInsights.DependencyCollector
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using Microsoft.ApplicationInsights.Common;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.DependencyCollector.Implementation;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Web.Implementation;
    using Microsoft.Extensions.DiagnosticAdapter;

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

        private readonly ApplicationInsightsUrlFilter applicationInsightsUrlFilter;
        private readonly TelemetryClient client;
        private readonly ICorrelationIdLookupHelper correlationIdLookupHelper;
        private readonly ConcurrentDictionary<Guid, DependencyTelemetry> pendingTelemetry = new ConcurrentDictionary<Guid, DependencyTelemetry>();

        internal DependencyCollectorDiagnosticListener(TelemetryConfiguration configuration, ICorrelationIdLookupHelper correlationIdLookupHelper = null)
        {
            this.client = new TelemetryClient(configuration);
            this.client.Context.GetInternalContext().SdkVersion = SdkVersionUtils.GetSdkVersion("rddf");

            this.applicationInsightsUrlFilter = new ApplicationInsightsUrlFilter(configuration);

            if (correlationIdLookupHelper == null)
            {
                correlationIdLookupHelper = new CorrelationIdLookupHelper(configuration.TelemetryChannel.EndpointAddress);
            }
            this.correlationIdLookupHelper = correlationIdLookupHelper;
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
            if (value != null)
            {
                // Comes from https://github.com/dotnet/corefx/blob/master/src/System.Net.Http/src/System/Net/Http/DiagnosticsHandlerLoggingStrings.cs#L12
                if (value.Name == "HttpHandlerDiagnosticListener")
                {
                    value.SubscribeWithAdapter(this);
                }
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
            try
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
                    telemetry.Type = RemoteDependencyConstants.HTTP;
                    telemetry.Data = requestUri.OriginalString;
                    this.pendingTelemetry.TryAdd(loggingRequestId, telemetry);

                    HttpRequestHeaders requestHeaders = request.Headers;
                    if (requestHeaders != null)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(telemetry.Context.InstrumentationKey) && !HttpHeadersUtilities.ContainsRequestContextKeyValue(requestHeaders, RequestResponseHeaders.RequestContextCorrelationSourceKey))
                            {
                                string sourceApplicationId;
                                if (this.correlationIdLookupHelper.TryGetXComponentCorrelationId(telemetry.Context.InstrumentationKey, out sourceApplicationId))
                                {
                                    HttpHeadersUtilities.SetRequestContextKeyValue(requestHeaders, RequestResponseHeaders.RequestContextCorrelationSourceKey, sourceApplicationId);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            AppMapCorrelationEventSource.Log.UnknownError(ExceptionUtilities.GetExceptionDetailString(e));
                        }

                        // Add the root ID
                        string rootId = telemetry.Context.Operation.Id;
                        if (!string.IsNullOrEmpty(rootId) && !requestHeaders.Contains(RequestResponseHeaders.StandardRootIdHeader))
                        {
                            requestHeaders.Add(RequestResponseHeaders.StandardRootIdHeader, rootId);
                        }

                        // Add the parent ID
                        string parentId = telemetry.Id;
                        if (!string.IsNullOrEmpty(parentId) && !requestHeaders.Contains(RequestResponseHeaders.StandardParentIdHeader))
                        {
                            requestHeaders.Add(RequestResponseHeaders.StandardParentIdHeader, parentId);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                AppMapCorrelationEventSource.Log.UnknownError(ExceptionUtilities.GetExceptionDetailString(e));
            }
        }

        /// <summary>
        /// Diagnostic event handler method for 'System.Net.Http.Response' event. Even in the case of an exception, this will still be called.
        /// See https://github.com/dotnet/corefx/blob/master/src/System.Net.Http/src/System/Net/Http/DiagnosticsHandler.cs#L71 for more information.
        /// </summary>
        [DiagnosticName("System.Net.Http.Response")]
        public void OnResponse(HttpResponseMessage response, Guid loggingRequestId)
        {
            try
            {
                if (response != null)
                {
                    DependencyTelemetry telemetry;
                    if (this.pendingTelemetry.TryRemove(loggingRequestId, out telemetry))
                    {
                        try
                        {
                            string targetApplicationId = HttpHeadersUtilities.GetRequestContextKeyValue(response.Headers, RequestResponseHeaders.RequestContextCorrleationTargetKey);
                            if (!string.IsNullOrEmpty(targetApplicationId) && !string.IsNullOrEmpty(telemetry.Context.InstrumentationKey))
                            {
                                // We only add the cross component correlation key if the key does not represent the current component.
                                string sourceApplicationId;
                                if (this.correlationIdLookupHelper.TryGetXComponentCorrelationId(telemetry.Context.InstrumentationKey, out sourceApplicationId) &&
                                    targetApplicationId != sourceApplicationId)
                                {
                                    telemetry.Type = RemoteDependencyConstants.AI;
                                    telemetry.Target += " | " + targetApplicationId;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            AppMapCorrelationEventSource.Log.UnknownError(ExceptionUtilities.GetExceptionDetailString(e));
                        }

                        int statusCode = (int)response.StatusCode;
                        telemetry.ResultCode = (0 < statusCode) ? statusCode.ToString(CultureInfo.InvariantCulture) : string.Empty;
                        telemetry.Success = (0 < statusCode) && (statusCode < 400);

                        telemetry.Stop();
                        this.client.Track(telemetry);
                    }
                }
            }
            catch (Exception e)
            {
                AppMapCorrelationEventSource.Log.UnknownError(ExceptionUtilities.GetExceptionDetailString(e));
            }
        }
    }
}