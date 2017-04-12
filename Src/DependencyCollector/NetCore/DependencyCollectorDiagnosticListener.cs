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
    using System.Threading.Tasks;
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
    internal class DependencyCollectorDiagnosticListener : IObserver<DiagnosticListener>
    {
        private const string DependencyErrorPropertyKey = "Error";
        private const string RequestExceptionPropertyKey = "Microsoft.ApplicationInsights.Exception";
        private readonly ApplicationInsightsUrlFilter applicationInsightsUrlFilter;
        private readonly TelemetryClient client;
        private readonly TelemetryConfiguration configuration;
        private readonly bool setComponentCorrelationHttpHeaders;
        private readonly IEnumerable<string> correlationDomainExclusionList;
        private readonly ICorrelationIdLookupHelper correlationIdLookupHelper;

        private readonly ConcurrentDictionary<string, IOperationHolder<DependencyTelemetry>> pendingTelemetry = new ConcurrentDictionary<string, IOperationHolder<DependencyTelemetry>>();

        private readonly ConcurrentDictionary<string, Exception> pendingExceptions = new ConcurrentDictionary<string, Exception>();

        internal DependencyCollectorDiagnosticListener(TelemetryConfiguration configuration, bool setComponentCorrelationHttpHeaders = true, IEnumerable<string> correlationDomainExclusionList = null, ICorrelationIdLookupHelper correlationIdLookupHelper = null)
        {
            this.client = new TelemetryClient(configuration);
            this.configuration = configuration;

            this.client.Context.GetInternalContext().SdkVersion = SdkVersionUtils.GetSdkVersion("rddd");

            this.applicationInsightsUrlFilter = new ApplicationInsightsUrlFilter(configuration);

            this.setComponentCorrelationHttpHeaders = setComponentCorrelationHttpHeaders;

            this.correlationDomainExclusionList = correlationDomainExclusionList ?? Enumerable.Empty<string>();

            this.correlationIdLookupHelper = correlationIdLookupHelper ?? new CorrelationIdLookupHelper(configuration.TelemetryChannel.EndpointAddress);
        }

        /// <summary>
        /// Get the DependencyTelemetry objects that are still waiting for a response from the dependency. This will most likely only be used for testing purposes.
        /// </summary>
        internal IEnumerable<IOperationHolder<DependencyTelemetry>> PendingDependencyTelemetry
        {
            get { return pendingTelemetry.Values; }
        }

        /// <summary>
        /// This method gets called once for each existing DiagnosticListener when this
        /// DiagnosticListener is added to the list of DiagnosticListeners
        /// (<see cref="System.Diagnostics.DiagnosticListener.AllListeners"/>). This method will
        /// also be called for each subsequent DiagnosticListener that is added to the list of
        /// DiagnosticListeners.
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
        /// Enables HttpClient instrumentation with activity. It is never called and allows Microsoft.Extensions.DiagnosticAdapter to know that event is enabled.
        /// </summary>
        [DiagnosticName("System.Net.Http.HttpRequestOut")]
        public void IsActivityEnabled()
        {
        }

        /// <summary>
        /// Handler for Exception event, it is sent when request processing cause an exception (e.g. because of DNS or network issues) on netcoreapp2.0 only
        /// Stop event will be sent anyway with null response.
        /// </summary>
        [DiagnosticName("System.Net.Http.Exception")]
        public void OnException(Exception exception, HttpRequestMessage request)
        {
            Activity currentActivity = Activity.Current;
            if (currentActivity == null)
            {
                DependencyCollectorEventSource.Log.CurrentActivityIsNull();
                return;
            }

            this.pendingExceptions.TryAdd(currentActivity.Id, exception);
            this.client.TrackException(exception);
        }

        /// <summary>
        /// Handler for Activity start event (outgoing request is about to be sent).
        /// </summary>
        [DiagnosticName("System.Net.Http.HttpRequestOut.Start")]
        public void OnActivityStart(HttpRequestMessage request)
        {
            if (Activity.Current == null)
            {
                DependencyCollectorEventSource.Log.CurrentActivityIsNull();
                return;
            }

            if (request != null &&
                !this.applicationInsightsUrlFilter.IsApplicationInsightsUrl(request.RequestUri.ToString()))
            {
                this.InjectRequestHeaders(request, this.configuration.InstrumentationKey);
            }
        }

        /// <summary>
        /// Handler for Activity stop event (response is received for the outgoing request).
        /// </summary>
        [DiagnosticName("System.Net.Http.HttpRequestOut.Stop")]
        public void OnActivityStop(HttpResponseMessage response, HttpRequestMessage request, TaskStatus requestTaskStatus)
        {
            Activity currentActivity = Activity.Current;
            if (currentActivity == null)
            {
                DependencyCollectorEventSource.Log.CurrentActivityIsNull();
                return;
            }

            if (request != null && request.RequestUri != null &&
                !this.applicationInsightsUrlFilter.IsApplicationInsightsUrl(request.RequestUri.ToString()))
            {
                Uri requestUri = request.RequestUri;
                var resourceName = request.Method.Method + " " + requestUri.AbsolutePath;

                DependencyTelemetry telemetry = new DependencyTelemetry();

                // properly fill dependency telemetry operation context: OperationCorrelationTelemetryInitializer initializes child telemetry
                telemetry.Context.Operation.Id = currentActivity.RootId;
                telemetry.Context.Operation.ParentId = currentActivity.ParentId;
                telemetry.Id = currentActivity.Id;
                foreach (var item in currentActivity.Baggage)
                {
                    if (!telemetry.Context.Properties.ContainsKey(item.Key))
                    {
                        telemetry.Context.Properties[item.Key] = item.Value;
                    }
                }

                this.client.Initialize(telemetry);

                telemetry.Name = resourceName;
                telemetry.Target = requestUri.Host;
                telemetry.Type = RemoteDependencyConstants.HTTP;
                telemetry.Data = requestUri.OriginalString;
                telemetry.Duration = currentActivity.Duration;
                if (response != null)
                {
                    this.ParseResponse(response, telemetry);
                }
                else
                {
                    Exception exception;
                    if (this.pendingExceptions.TryRemove(currentActivity.Id, out exception))
                    {
                        telemetry.Context.Properties[DependencyErrorPropertyKey] = exception.GetBaseException().Message;
                    }

                    telemetry.ResultCode = requestTaskStatus.ToString();
                    telemetry.Success = false;
                }
                this.client.Track(telemetry);
            }
        }

        /// <summary>
        /// Diagnostic event handler method for 'System.Net.Http.Request' event. This event will be fired if application is .net core app prior to netcoreapp 2.0. 
        /// See https://github.com/dotnet/corefx/blob/release/1.0.0-rc2/src/Common/src/System/Net/Http/HttpHandlerDiagnosticListenerExtensions.cs.
        /// </summary>
        [DiagnosticName("System.Net.Http.Request")]
        public void OnRequest(HttpRequestMessage request, Guid loggingRequestId)
        {
            if (request != null && request.RequestUri != null &&
                !this.applicationInsightsUrlFilter.IsApplicationInsightsUrl(request.RequestUri.ToString()))
            {
                Uri requestUri = request.RequestUri;
                var resourceName = request.Method.Method + " " + requestUri.AbsolutePath;

                var dependency = this.client.StartOperation<DependencyTelemetry>(resourceName);
                dependency.Telemetry.Target = requestUri.Host;
                dependency.Telemetry.Type = RemoteDependencyConstants.HTTP;
                dependency.Telemetry.Data = requestUri.OriginalString;
                this.pendingTelemetry.TryAdd(loggingRequestId.ToString(), dependency);

                this.InjectRequestHeaders(request, dependency.Telemetry.Context.InstrumentationKey, true);
            }
        }

        /// <summary>
        /// Diagnostic event handler method for 'System.Net.Http.Response' event.
        /// This event will be fired if application is .net core app prior to netcoreapp 2.0 and only if response was received (and not called for faulted or cancelled requests)
        /// See https://github.com/dotnet/corefx/blob/release/1.0.0-rc2/src/Common/src/System/Net/Http/HttpHandlerDiagnosticListenerExtensions.cs.
        /// </summary>
        [DiagnosticName("System.Net.Http.Response")]
        public void OnResponse(HttpResponseMessage response, Guid loggingRequestId)
        {
            if (response != null)
            {
                IOperationHolder<DependencyTelemetry> dependency;
                if (this.pendingTelemetry.TryRemove(loggingRequestId.ToString(), out dependency))
                {
                    this.ParseResponse(response, dependency.Telemetry);
                    this.client.StopOperation(dependency);
                }
            }
        }

        private void InjectRequestHeaders(HttpRequestMessage request, string instrumentationKey, bool isLegacyEvent = false)
        {
            try
            {
                var currentActivity = Activity.Current;

                HttpRequestHeaders requestHeaders = request.Headers;
                if (requestHeaders != null && this.setComponentCorrelationHttpHeaders && !this.correlationDomainExclusionList.Contains(request.RequestUri.Host))
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(instrumentationKey) && !HttpHeadersUtilities.ContainsRequestContextKeyValue(requestHeaders, RequestResponseHeaders.RequestContextCorrelationSourceKey))
                        {
                            string sourceApplicationId;
                            if (this.correlationIdLookupHelper.TryGetXComponentCorrelationId(instrumentationKey, out sourceApplicationId))
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
                    string rootId = currentActivity.RootId;
                    if (!string.IsNullOrEmpty(rootId) &&
                        !requestHeaders.Contains(RequestResponseHeaders.StandardRootIdHeader))
                    {
                        requestHeaders.Add(RequestResponseHeaders.StandardRootIdHeader, rootId);
                    }

                    // Add the parent ID
                    string parentId = currentActivity.Id;
                    if (!string.IsNullOrEmpty(parentId) &&
                        !requestHeaders.Contains(RequestResponseHeaders.StandardParentIdHeader))
                    {
                        requestHeaders.Add(RequestResponseHeaders.StandardParentIdHeader, parentId);
                        if (isLegacyEvent)
                        {
                            requestHeaders.Add(RequestResponseHeaders.RequestIdHeader, parentId);
                        }
                    }

                    if (isLegacyEvent)
                    {
                        //we expect baggage to be empty or contain a few items
                        using (IEnumerator<KeyValuePair<string, string>> e = currentActivity.Baggage.GetEnumerator())
                        {
                            if (e.MoveNext())
                            {
                                var baggage = new List<string>();
                                do
                                {
                                    KeyValuePair<string, string> item = e.Current;
                                    baggage.Add(new NameValueHeaderValue(item.Key, item.Value).ToString());
                                }
                                while (e.MoveNext());
                                request.Headers.Add(RequestResponseHeaders.CorrelationContextHeader, baggage);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                AppMapCorrelationEventSource.Log.UnknownError(ExceptionUtilities.GetExceptionDetailString(e));
            }
        }

        private void ParseResponse(HttpResponseMessage response, DependencyTelemetry telemetry)
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
        }
    }
}