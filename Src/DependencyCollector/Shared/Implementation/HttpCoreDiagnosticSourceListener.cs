namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Common;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Web.Implementation;

    internal class HttpCoreDiagnosticSourceListener : IObserver<KeyValuePair<string, object>>, IDisposable
    {
        private const string DependencyErrorPropertyKey = "Error";
        private const string HttpOutEventName = "System.Net.Http.HttpRequestOut";
        private const string HttpOutStartEventName = "System.Net.Http.HttpRequestOut.Start";
        private const string HttpOutStopEventName = "System.Net.Http.HttpRequestOut.Stop";
        private const string HttpExceptionEventName = "System.Net.Http.Exception";
        private const string DeprecatedRequestEventName = "System.Net.Http.Request";
        private const string DeprecatedResponseEventName = "System.Net.Http.Response";

        private readonly IEnumerable<string> correlationDomainExclusionList;
        private readonly ApplicationInsightsUrlFilter applicationInsightsUrlFilter;
        private readonly bool setComponentCorrelationHttpHeaders;
        private readonly ICorrelationIdLookupHelper correlationIdLookupHelper;
        private readonly TelemetryClient client;
        private readonly TelemetryConfiguration configuration;
        private readonly HttpCoreDiagnosticSourceSubscriber subscriber;
        #region fetchers

        private readonly PropertyFetcher startRequestFetcher = new PropertyFetcher("Request");
        private readonly PropertyFetcher stopRequestFetcher = new PropertyFetcher("Request");
        private readonly PropertyFetcher exceptiontFetcher = new PropertyFetcher("Exception");
        private readonly PropertyFetcher stopResponseFetcher = new PropertyFetcher("Response");
        private readonly PropertyFetcher stopRequestStatusFetcher = new PropertyFetcher("RequestTaskStatus");
        private readonly PropertyFetcher deprecatedRequestFetcher = new PropertyFetcher("Request");
        private readonly PropertyFetcher deprecatedResponseFetcher = new PropertyFetcher("Response");
        private readonly PropertyFetcher deprecatedRequestGuidFetcher = new PropertyFetcher("LoggingRequestId");
        private readonly PropertyFetcher deprecatedResponseGuidFetcher = new PropertyFetcher("LoggingRequestId");

        #endregion

        private readonly ConditionalWeakTable<HttpRequestMessage, IOperationHolder<DependencyTelemetry>> pendingTelemetry = 
            new ConditionalWeakTable<HttpRequestMessage, IOperationHolder<DependencyTelemetry>>();

        private readonly ConcurrentDictionary<string, Exception> pendingExceptions =
            new ConcurrentDictionary<string, Exception>();

        public HttpCoreDiagnosticSourceListener(
            TelemetryConfiguration configuration,
            string effectiveProfileQueryEndpoint,
            bool setComponentCorrelationHttpHeaders,
            IEnumerable<string> correlationDomainExclusionList,
            ICorrelationIdLookupHelper correlationIdLookupHelper)
        {
            this.client = new TelemetryClient(configuration);
            this.client.Context.GetInternalContext().SdkVersion = SdkVersionUtils.GetSdkVersion("rdd" + RddSource.DiagnosticSourceCore + ":");

            this.configuration = configuration;
            this.applicationInsightsUrlFilter = new ApplicationInsightsUrlFilter(configuration);
            this.setComponentCorrelationHttpHeaders = setComponentCorrelationHttpHeaders;
            this.correlationIdLookupHelper = correlationIdLookupHelper ?? new CorrelationIdLookupHelper(effectiveProfileQueryEndpoint);
            this.correlationDomainExclusionList = correlationDomainExclusionList ?? Enumerable.Empty<string>();

            this.subscriber = new HttpCoreDiagnosticSourceSubscriber(this, this.applicationInsightsUrlFilter);
        }

        /// <summary>
        /// Get the DependencyTelemetry objects that are still waiting for a response from the dependency. This will most likely only be used for testing purposes.
        /// </summary>
        internal ConditionalWeakTable<HttpRequestMessage, IOperationHolder<DependencyTelemetry>> PendingDependencyTelemetry => this.pendingTelemetry;

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

        public void OnNext(KeyValuePair<string, object> evnt)
        {
            switch (evnt.Key)
            {
                case HttpOutStartEventName:
                {
                    this.OnActivityStart((HttpRequestMessage)this.startRequestFetcher.Fetch(evnt.Value));
                    break;
                }

                case HttpOutStopEventName:
                {
                    this.OnActivityStop(
                        (HttpResponseMessage)this.stopResponseFetcher.Fetch(evnt.Value),
                        (HttpRequestMessage)this.stopRequestFetcher.Fetch(evnt.Value),
                        (TaskStatus)this.stopRequestStatusFetcher.Fetch(evnt.Value));
                    break;
                }

                case HttpExceptionEventName:
                {
                    this.OnException((Exception)this.exceptiontFetcher.Fetch(evnt.Value));
                    break;
                }

                case DeprecatedRequestEventName:
                {
                    this.OnRequest(
                        (HttpRequestMessage)this.deprecatedRequestFetcher.Fetch(evnt.Value),
                        (Guid)this.deprecatedRequestGuidFetcher.Fetch(evnt.Value));
                    break;
                }

                case DeprecatedResponseEventName:
                {
                    this.OnResponse(
                        (HttpResponseMessage)this.deprecatedResponseFetcher.Fetch(evnt.Value),
                        (Guid)this.deprecatedResponseGuidFetcher.Fetch(evnt.Value));
                    break;
                }
            }
        }

        public void Dispose()
        {
            if (this.subscriber != null)
            {
                this.subscriber.Dispose();
            }
        }

        //// netcoreapp 2.0 event

        /// <summary>
        /// Handler for Exception event, it is sent when request processing cause an exception (e.g. because of DNS or network issues)
        /// Stop event will be sent anyway with null response.
        /// </summary>
        internal void OnException(Exception exception)
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

        //// netcoreapp 2.0 event

        /// <summary>
        /// Handler for Activity start event (outgoing request is about to be sent).
        /// </summary>
        internal void OnActivityStart(HttpRequestMessage request)
        {
            if (Activity.Current == null)
            {
                DependencyCollectorEventSource.Log.CurrentActivityIsNull();
                return;
            }

            this.InjectRequestHeaders(request, this.configuration.InstrumentationKey);
        }

        //// netcoreapp 2.0 event

        /// <summary>
        /// Handler for Activity stop event (response is received for the outgoing request).
        /// </summary>
        internal void OnActivityStop(HttpResponseMessage response, HttpRequestMessage request, TaskStatus requestTaskStatus)
        {
            Activity currentActivity = Activity.Current;
            if (currentActivity == null)
            {
                DependencyCollectorEventSource.Log.CurrentActivityIsNull();
                return;
            }

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

        //// netcoreapp1.1 and prior event. See https://github.com/dotnet/corefx/blob/release/1.0.0-rc2/src/Common/src/System/Net/Http/HttpHandlerDiagnosticListenerExtensions.cs.

        /// <summary>
        /// Diagnostic event handler method for 'System.Net.Http.Request' event.
        /// </summary>
        internal void OnRequest(HttpRequestMessage request, Guid loggingRequestId)
        {
            if (request != null && request.RequestUri != null &&
                !this.applicationInsightsUrlFilter.IsApplicationInsightsUrl(request.RequestUri))
            {
                Uri requestUri = request.RequestUri;
                var resourceName = request.Method.Method + " " + requestUri.AbsolutePath;

                var dependency = this.client.StartOperation<DependencyTelemetry>(resourceName);
                dependency.Telemetry.Target = requestUri.Host;
                dependency.Telemetry.Type = RemoteDependencyConstants.HTTP;
                dependency.Telemetry.Data = requestUri.OriginalString;
                this.pendingTelemetry.Add(request, dependency);

                this.InjectRequestHeaders(request, dependency.Telemetry.Context.InstrumentationKey, true);
            }
        }

        //// netcoreapp1.1 and prior event. See https://github.com/dotnet/corefx/blob/release/1.0.0-rc2/src/Common/src/System/Net/Http/HttpHandlerDiagnosticListenerExtensions.cs.

        /// <summary>
        /// Diagnostic event handler method for 'System.Net.Http.Response' event.
        /// This event will be fired only if response was received (and not called for faulted or cancelled requests).
        /// </summary>
        internal void OnResponse(HttpResponseMessage response, Guid loggingRequestId)
        {
            if (response != null)
            {
                var request = response.RequestMessage;
                IOperationHolder<DependencyTelemetry> dependency;
                if (request != null && this.pendingTelemetry.TryGetValue(request, out dependency))
                {
                    this.ParseResponse(response, dependency.Telemetry);
                    this.client.StopOperation(dependency);
                    this.pendingTelemetry.Remove(request);
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
                        // we expect baggage to be empty or contain a few items
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
                string targetApplicationId = HttpHeadersUtilities.GetRequestContextKeyValue(response.Headers, RequestResponseHeaders.RequestContextCorrelationTargetKey);
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
            telemetry.ResultCode = (statusCode > 0) ? statusCode.ToString(CultureInfo.InvariantCulture) : string.Empty;
            telemetry.Success = (statusCode > 0) && (statusCode < 400);
        }

        /// <summary>
        /// Diagnostic listener implementation that listens for events specific to outgoing dependency requests.
        /// </summary>
        private class HttpCoreDiagnosticSourceSubscriber : IObserver<DiagnosticListener>, IDisposable
        {
            private readonly HttpCoreDiagnosticSourceListener httpDiagnosticListener;
            private readonly IDisposable listenerSubscription;
            private readonly ApplicationInsightsUrlFilter applicationInsightsUrlFilter;
            private readonly bool isNetCore20HttpClient;

            private IDisposable eventSubscription;

            internal HttpCoreDiagnosticSourceSubscriber(HttpCoreDiagnosticSourceListener listener, ApplicationInsightsUrlFilter applicationInsightsUrlFilter)
            {
                this.httpDiagnosticListener = listener;
                this.applicationInsightsUrlFilter = applicationInsightsUrlFilter;
                this.listenerSubscription = DiagnosticListener.AllListeners.Subscribe(this);

                var httpClientVersion = typeof(HttpClient).GetTypeInfo().Assembly.GetName().Version;
                this.isNetCore20HttpClient = httpClientVersion.CompareTo(new Version(4, 2)) >= 0;
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
                        this.eventSubscription = value.Subscribe(
                            this.httpDiagnosticListener,
                            (evnt, r, _) =>
                            {
                                if (isNetCore20HttpClient)
                                {
                                    if (evnt == HttpExceptionEventName)
                                    {
                                        return true;
                                    }

                                    if (!evnt.StartsWith(HttpOutEventName, StringComparison.Ordinal))
                                    {
                                        return false;
                                    }

                                    if (evnt == HttpOutEventName && r != null)
                                    {
                                        var request = (HttpRequestMessage)r;
                                        return !this.applicationInsightsUrlFilter.IsApplicationInsightsUrl(request.RequestUri);
                                    }
                                }

                                return true;
                            });
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

            public void Dispose()
            {
                if (this.eventSubscription != null)
                {
                    this.eventSubscription.Dispose();
                }

                if (this.listenerSubscription != null)
                {
                    this.listenerSubscription.Dispose();
                }
            }
        }
    }
}