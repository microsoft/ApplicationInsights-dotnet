namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Web;
    using Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Common;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.DependencyCollector.Implementation.Operation;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    /// <summary>
    /// Concrete class with all processing logic to generate RDD data from the callbacks
    /// received from Profiler instrumentation for HTTP or HTTP EventSource/DiagnosticSource events.   
    /// </summary>
    internal abstract class HttpProcessing
    {
        protected TelemetryClient telemetryClient;
        private readonly ApplicationInsightsUrlFilter applicationInsightsUrlFilter;
        private ICollection<string> correlationDomainExclusionList;
        private bool setCorrelationHeaders;
        private CorrelationIdLookupHelper correlationIdLookupHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpProcessing"/> class.
        /// </summary>
        public HttpProcessing(TelemetryConfiguration configuration, string sdkVersion, string agentVersion, bool setCorrelationHeaders, ICollection<string> correlationDomainExclusionList, string appIdEndpoint)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            if (correlationDomainExclusionList == null)
            {
                throw new ArgumentNullException("correlationDomainExclusionList");
            }

            this.applicationInsightsUrlFilter = new ApplicationInsightsUrlFilter(configuration);
            this.telemetryClient = new TelemetryClient(configuration);
            this.correlationDomainExclusionList = correlationDomainExclusionList;
            this.setCorrelationHeaders = setCorrelationHeaders;
            this.correlationIdLookupHelper = new CorrelationIdLookupHelper(appIdEndpoint);

            this.telemetryClient.Context.GetInternalContext().SdkVersion = sdkVersion;
            if (!string.IsNullOrEmpty(agentVersion))
            {
                this.telemetryClient.Context.GetInternalContext().AgentVersion = agentVersion;
            }
        }

        /// <summary>
        /// Gets HTTP request url.
        /// </summary>
        /// <param name="webRequest">Represents web request.</param>
        /// <returns>The url if possible otherwise empty string.</returns>
        internal Uri GetUrl(WebRequest webRequest)
        {
            Uri resource = null;
            if (webRequest != null && webRequest.RequestUri != null)
            {
                resource = webRequest.RequestUri;
            }

            return resource;
        }

        /// <summary>
        /// Simple test hook, that allows for using a stub rather than the implementation that calls the original service.
        /// </summary>
        /// <param name="correlationIdLookupHelper">Lookup header to use.</param>
        internal void OverrideCorrelationIdLookupHelper(CorrelationIdLookupHelper correlationIdLookupHelper)
        {
            this.correlationIdLookupHelper = correlationIdLookupHelper;
        }

        /// <summary>
        /// Common helper for all Begin Callbacks.
        /// </summary>
        /// <param name="thisObj">This object.</param>
        /// <param name="injectCorrelationHeaders">Flag that enables Request-Id and Correlation-Context headers injection.
        /// Should be set to true only for profiler and old versions of DiagnosticSource Http hook events.</param>
        /// <returns>Null object as all context is maintained in this class via weak tables.</returns>
        internal object OnBegin(object thisObj, bool injectCorrelationHeaders = true)
        {
            try
            {                
                if (thisObj == null)
                {
                    DependencyCollectorEventSource.Log.NotExpectedCallback(0, "OnBeginHttp", "thisObj == null");
                    return null;
                }

                WebRequest webRequest = thisObj as WebRequest;
                if (webRequest == null)
                {
                    DependencyCollectorEventSource.Log.UnexpectedCallbackParameter("WebRequest");
                }

                var url = this.GetUrl(webRequest);

                if (url == null)
                {
                    DependencyCollectorEventSource.Log.NotExpectedCallback(thisObj.GetHashCode(), "OnBeginHttp", "resourceName is empty");
                    return null;
                }

                string httpMethod = webRequest.Method;
                string resourceName = url.AbsolutePath;

                if (!string.IsNullOrEmpty(httpMethod))
                {
                    resourceName = httpMethod + " " + resourceName;
                }

                DependencyCollectorEventSource.Log.BeginCallbackCalled(thisObj.GetHashCode(), resourceName);

                if (this.applicationInsightsUrlFilter.IsApplicationInsightsUrl(url))
                {
                    // Not logging as we will be logging for all outbound AI calls
                    return null;
                }

                // If the object already exists, don't add again. This happens because either GetResponse or GetRequestStream could
                // be the starting point for the outbound call.
                DependencyTelemetry telemetry = null;
                var telemetryTuple = this.GetTupleForWebDependencies(webRequest);
                if (telemetryTuple != null)
                {
                    if (telemetryTuple.Item1 != null)
                    {
                        telemetry = telemetryTuple.Item1;
                        DependencyCollectorEventSource.Log.TrackingAnExistingTelemetryItemVerbose();
                        return null;
                    }
                }

                // Create and initialize a new telemetry object if needed
                if (telemetry == null)
                {
                    bool isCustomCreated = false;

                    telemetry = ClientServerDependencyTracker.BeginTracking(this.telemetryClient);

                    this.AddTupleForWebDependencies(webRequest, telemetry, isCustomCreated);

                    if (string.IsNullOrEmpty(telemetry.Context.InstrumentationKey))
                    {
                        // Instrumentation key is probably empty, because the context has not yet had a chance to associate the requestTelemetry to the telemetry client yet.
                        // and get they instrumentation key from all possible sources in the process. Let's do that now.
                        this.telemetryClient.Initialize(telemetry);
                    }
                }

                telemetry.Name = resourceName;
                telemetry.Target = DependencyTargetNameHelper.GetDependencyTargetName(url);
                telemetry.Type = RemoteDependencyConstants.HTTP;
                telemetry.Data = url.OriginalString;

                // Add the source instrumentation key header if collection is enabled, the request host is not in the excluded list and the same header doesn't already exist
                if (this.setCorrelationHeaders
                    && !this.correlationDomainExclusionList.Contains(url.Host))
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(telemetry.Context.InstrumentationKey)
                            && webRequest.Headers.GetNameValueHeaderValue(RequestResponseHeaders.RequestContextHeader, RequestResponseHeaders.RequestContextCorrelationSourceKey) == null)
                        {
                            string appId;
                            if (this.correlationIdLookupHelper.TryGetXComponentCorrelationId(telemetry.Context.InstrumentationKey, out appId))
                            {
                                webRequest.Headers.SetNameValueHeaderValue(RequestResponseHeaders.RequestContextHeader, RequestResponseHeaders.RequestContextCorrelationSourceKey, appId);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        AppMapCorrelationEventSource.Log.SetCrossComponentCorrelationHeaderFailed(ex.ToInvariantString());
                    }

                    // Add the root ID
                    var rootId = telemetry.Context.Operation.Id;
                    if (!string.IsNullOrEmpty(rootId) && webRequest.Headers[RequestResponseHeaders.StandardRootIdHeader] == null)
                    {
                        webRequest.Headers.Add(RequestResponseHeaders.StandardRootIdHeader, rootId);
                    }

                    // Add the parent ID
                    var parentId = telemetry.Id;
                    if (!string.IsNullOrEmpty(parentId))
                    {
                        if (webRequest.Headers[RequestResponseHeaders.StandardParentIdHeader] == null)
                        {
                            webRequest.Headers.Add(RequestResponseHeaders.StandardParentIdHeader, parentId);
                        }
                    }

                    // ApplicationInsights only need to inject Request-Id and Correlation-Context headers 
                    // for profiler instrumentation, in case of Http Desktop DiagnosticSourceListener
                    // they are injected in DiagnosticSource (with the System.Net.Http.Desktop.HttpRequestOut.Start event)
                    if (injectCorrelationHeaders)
                    {
                        if (webRequest.Headers[RequestResponseHeaders.RequestIdHeader] == null)
                        {
                            webRequest.Headers.Add(RequestResponseHeaders.RequestIdHeader, telemetry.Id);
                        }

                        if (webRequest.Headers[RequestResponseHeaders.CorrelationContextHeader] == null)
                        {
                            var currentActivity = Activity.Current;
                            if (currentActivity != null && currentActivity.Baggage.Any())
                            {
                                webRequest.Headers.SetHeaderFromNameValueCollection(RequestResponseHeaders.CorrelationContextHeader, currentActivity.Baggage);
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                DependencyCollectorEventSource.Log.CallbackError(thisObj == null ? 0 : thisObj.GetHashCode(), "OnBeginHttp", exception);
            }

            return null;
        }

        /// <summary>
        /// Common helper for all End Callbacks.
        /// </summary>        
        /// <param name="request">The HttpWebRequest instance.</param>
        /// <param name="response">The HttpWebResponse instance.</param>                
        internal void OnEndResponse(object request, object response)
        {
            try
            {
                DependencyTelemetry telemetry;
                if (this.TryGetPendingTelemetry(request, out telemetry))
                {
                    var responseObj = response as HttpWebResponse;
                    if (responseObj != null)
                    {
                        int statusCode = -1;

                        try
                        {
                            statusCode = (int)responseObj.StatusCode;
                            this.SetTarget(telemetry, responseObj.Headers);
                        }
                        catch (ObjectDisposedException)
                        {
                            // ObjectDisposedException is expected here in the following sequence: httpWebRequest.GetResponse().Dispose() -> httpWebRequest.GetResponse()
                            // on the second call to GetResponse() we cannot determine the statusCode.
                        }

                        this.SetStatusCode(telemetry, statusCode);
                    }

                    ClientServerDependencyTracker.EndTracking(this.telemetryClient, telemetry);
                }
            }
            catch (Exception ex)
            {
                DependencyCollectorEventSource.Log.CallbackError(request == null ? 0 : request.GetHashCode(), "OnEndResponse", ex);
            }
        }

        /// <summary>
        /// Common helper for all End Callbacks.
        /// </summary>        
        /// <param name="exception">The exception object if any.</param>
        /// <param name="request">HttpWebRequest instance.</param>                
        internal void OnEndException(object exception, object request)
        {
            try
            {
                DependencyTelemetry telemetry;
                if (this.TryGetPendingTelemetry(request, out telemetry))
                {
                    var webException = exception as WebException;
                    HttpWebResponse responseObj = webException?.Response as HttpWebResponse;

                    if (responseObj != null)
                    {
                        int statusCode = -1;

                        try
                        {
                            statusCode = (int)responseObj.StatusCode;
                            this.SetTarget(telemetry, responseObj.Headers);
                        }
                        catch (ObjectDisposedException)
                        {
                            // ObjectDisposedException is expected here in the following sequence: httpWebRequest.GetResponse().Dispose() -> httpWebRequest.GetResponse()
                            // on the second call to GetResponse() we cannot determine the statusCode.
                        }

                        this.SetStatusCode(telemetry, statusCode);
                    }
                    else if (exception != null)
                    {
                        if (webException != null)
                        {
                            telemetry.ResultCode = webException.Status.ToString();
                        }

                        telemetry.Success = false;
                    }

                    ClientServerDependencyTracker.EndTracking(this.telemetryClient, telemetry);
                }
            }
            catch (Exception ex)
            {
                DependencyCollectorEventSource.Log.CallbackError(request == null ? 0 : request.GetHashCode(), "OnEndException", ex);
            }
        }

        /// <summary>
        /// Common helper for all End Callbacks.
        /// </summary>        
        /// <param name="request">WebRequest object.</param>
        /// <param name="statusCode">HttpStatusCode from response.</param>                
        /// <param name="responseHeaders">Response headers.</param>
        internal void OnEndResponse(object request, object statusCode, object responseHeaders)
        {
            try
            {
                DependencyTelemetry telemetry;
                if (this.TryGetPendingTelemetry(request, out telemetry))
                {
                    if (statusCode != null)
                    {
                        this.SetStatusCode(telemetry, (int)statusCode);
                    }

                    this.SetTarget(telemetry, (WebHeaderCollection)responseHeaders);

                    ClientServerDependencyTracker.EndTracking(this.telemetryClient, telemetry);
                }
            }
            catch (Exception ex)
            {
                DependencyCollectorEventSource.Log.CallbackError(request == null ? 0 : request.GetHashCode(), "OnEndResponse", ex);
            }
        }

        /// <summary>
        /// Implemented by the derived class for adding the tuple to its specific cache.
        /// </summary>
        /// <param name="webRequest">The request which acts the key.</param>
        /// <param name="telemetry">The dependency telemetry for the tuple.</param>
        /// <param name="isCustomCreated">Boolean value that tells if the current telemetry item is being added by the customer or not.</param>
        protected abstract void AddTupleForWebDependencies(WebRequest webRequest, DependencyTelemetry telemetry, bool isCustomCreated);

        /// <summary>
        /// Implemented by the derived class for getting the tuple from its specific cache.
        /// </summary>
        /// <param name="webRequest">The request which acts as the key.</param>
        /// <returns>The tuple for the given request.</returns>
        protected abstract Tuple<DependencyTelemetry, bool> GetTupleForWebDependencies(WebRequest webRequest);

        /// <summary>
        /// Implemented by the derived class for removing the tuple from its specific cache.
        /// </summary>
        /// <param name="webRequest">The request which acts as the key.</param>
        protected abstract void RemoveTupleForWebDependencies(WebRequest webRequest);

        private bool TryGetPendingTelemetry(object request, out DependencyTelemetry telemetry)
        {
            telemetry = null;
            if (request == null)
            {
                DependencyCollectorEventSource.Log.NotExpectedCallback(0, "OnBeginHttp", "request == null");
                return false;
            }

            DependencyCollectorEventSource.Log.EndCallbackCalled(request.GetHashCode()
                .ToString(CultureInfo.InvariantCulture));

            WebRequest webRequest = request as WebRequest;
            if (webRequest == null)
            {
                DependencyCollectorEventSource.Log.UnexpectedCallbackParameter("WebRequest");
                return false;
            }

            var telemetryTuple = this.GetTupleForWebDependencies(webRequest);
            if (telemetryTuple == null)
            {
                DependencyCollectorEventSource.Log.EndCallbackWithNoBegin(request.GetHashCode()
                    .ToString(CultureInfo.InvariantCulture));
                return false;
            }

            if (telemetryTuple.Item1 == null)
            {
                DependencyCollectorEventSource.Log.EndCallbackWithNoBegin(request.GetHashCode()
                    .ToString(CultureInfo.InvariantCulture));
                return false;
            }

            // Not custom created
            if (!telemetryTuple.Item2)
            {
                telemetry = telemetryTuple.Item1;
                this.RemoveTupleForWebDependencies(webRequest);
                return true;
            }

            return false;
        }

        private void SetTarget(DependencyTelemetry telemetry, WebHeaderCollection responseHeaders)
        {
            if (responseHeaders != null)
            {
                string targetAppId = null;

                try
                {
                    targetAppId = responseHeaders.GetNameValueHeaderValue(RequestResponseHeaders.RequestContextHeader, RequestResponseHeaders.RequestContextCorrelationTargetKey);
                }
                catch (Exception ex)
                {
                    AppMapCorrelationEventSource.Log.GetCrossComponentCorrelationHeaderFailed(ex.ToInvariantString());
                }

                string currentComponentAppId;
                if (this.correlationIdLookupHelper.TryGetXComponentCorrelationId(telemetry.Context.InstrumentationKey, out currentComponentAppId))
                {
                    // We only add the cross component correlation key if the key does not remain the current component.
                    if (!string.IsNullOrEmpty(targetAppId) && targetAppId != currentComponentAppId)
                    {
                        telemetry.Type = RemoteDependencyConstants.AI;
                        telemetry.Target += " | " + targetAppId;
                    }
                }
            }
        }

        private void SetStatusCode(DependencyTelemetry telemetry, int statusCode)
        {
            telemetry.ResultCode = statusCode > 0 ? statusCode.ToString(CultureInfo.InvariantCulture) : string.Empty;
            telemetry.Success = (statusCode > 0) && (statusCode < 400);
        }
    }
}