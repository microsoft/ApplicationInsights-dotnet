namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Net;
#if !NET40
    using System.Web;
#endif
    using Microsoft.ApplicationInsights.Common;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.DependencyCollector.Implementation.Operation;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Web.Implementation;
    
    /// <summary>
    /// Concrete class with all processing logic to generate RDD data from the calls backs
    /// received from Profiler instrumentation for HTTP .   
    /// </summary>
    internal sealed class ProfilerHttpProcessing
    {
        internal ObjectInstanceBasedOperationHolder TelemetryTable;
        private readonly ApplicationInsightsUrlFilter applicationInsightsUrlFilter;
        private TelemetryClient telemetryClient;
        private ICollection<string> correlationDomainExclusionList;
        private bool setCorrelationHeaders;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfilerHttpProcessing"/> class.
        /// </summary>
        public ProfilerHttpProcessing(TelemetryConfiguration configuration, string agentVersion, ObjectInstanceBasedOperationHolder telemetryTupleHolder, bool setCorrelationHeaders, ICollection<string> correlationDomainExclusionList)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            if (telemetryTupleHolder == null)
            {
                throw new ArgumentNullException("telemetryTupleHolder");
            }

            if (correlationDomainExclusionList == null)
            {
                throw new ArgumentNullException("correlationDomainExclusionList");
            }

            this.applicationInsightsUrlFilter = new ApplicationInsightsUrlFilter(configuration);
            this.TelemetryTable = telemetryTupleHolder;
            this.telemetryClient = new TelemetryClient(configuration);
            this.correlationDomainExclusionList = correlationDomainExclusionList;
            this.setCorrelationHeaders = setCorrelationHeaders;

            // Since dependencySource is no longer set, sdk version is prepended with information which can identify whether RDD was collected by profiler/framework
            // For directly using TrackDependency(), version will be simply what is set by core
            string prefix = "rdd" + RddSource.Profiler + ":";
            this.telemetryClient.Context.GetInternalContext().SdkVersion = SdkVersionUtils.GetSdkVersion(prefix);
            if (!string.IsNullOrEmpty(agentVersion))
            {
                this.telemetryClient.Context.GetInternalContext().AgentVersion = agentVersion;
            }
        }

#region Http callbacks
      
        /// <summary>
        /// On begin callback for GetResponse.
        /// </summary>
        /// <param name="thisObj">This object.</param>
        /// <returns>The context for end callback.</returns>
        public object OnBeginForGetResponse(object thisObj)
        {
            return this.OnBegin(thisObj, false);
        }

        /// <summary>
        /// On end callback for GetResponse.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="returnValue">The return value.</param>
        /// <param name="thisObj">This object.</param>
        /// <returns>The resulting return value.</returns>
        public object OnEndForGetResponse(object context, object returnValue, object thisObj)
        {
            this.OnEnd(null, thisObj, returnValue);
            return returnValue;
        }

        /// <summary>
        /// On exception callback for GetResponse callback.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="exception">The exception object.</param>
        /// <param name="thisObj">This object.</param>        
        public void OnExceptionForGetResponse(object context, object exception, object thisObj)
        {
            this.OnEnd(exception, thisObj, null);
        }
        
        /// <summary>
        /// On begin callback for GetRequestStream callback.
        /// </summary>
        /// <param name="thisObj">This object.</param>
        /// <param name="transportContext">The transport context parameter.</param>
        /// <returns>The context for end callback.</returns>
        public object OnBeginForGetRequestStream(object thisObj, object transportContext)
        {
            return this.OnBegin(thisObj, false);
        }       

        /// <summary>
        /// On exception for GetRequestStream callback.
        /// Note: There is no call back required for GetRequestStream except on exception cases.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="thisObj">This object.</param>
        /// <param name="transportContext">The transport context parameter.</param>
        public void OnExceptionForGetRequestStream(object context, object exception, object thisObj, object transportContext)
        {
            this.OnEnd(exception, thisObj, null);
        }

        /// <summary>
        /// On begin for BeginGetResponse callback.
        /// </summary>
        /// <param name="thisObj">This object.</param>
        /// <param name="callback">The callback parameter.</param>
        /// <param name="state">The state parameter.</param>
        /// <returns>The context for end callback.</returns>
        public object OnBeginForBeginGetResponse(object thisObj, object callback, object state)
        {
            return this.OnBegin(thisObj, true);
        }

        /// <summary>
        /// On end for EndGetResponse callbacks.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="returnValue">The return value.</param>
        /// <param name="thisObj">This object.</param>
        /// <param name="asyncResult">The asyncResult parameter.</param>
        /// <returns>The return value passed.</returns>
        public object OnEndForEndGetResponse(object context, object returnValue, object thisObj, object asyncResult)
        {
            this.OnEnd(null, thisObj, returnValue);
            return returnValue;
        }

        /// <summary>
        /// On exception for EndGetResponse callbacks.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="thisObj">This object.</param>
        /// <param name="asyncResult">The asyncResult parameter.</param>
        public void OnExceptionForEndGetResponse(object context, object exception, object thisObj, object asyncResult)
        {
            this.OnEnd(exception, thisObj, null);
        }

        /// <summary>
        /// On begin for BeginGetRequestStream callback.
        /// </summary>
        /// <param name="thisObj">This object.</param>
        /// <param name="callback">The callback parameter.</param>
        /// <param name="state">The state parameter.</param>
        /// <returns>The context for end callback.</returns>
        public object OnBeginForBeginGetRequestStream(object thisObj, object callback, object state)
        {
            return this.OnBegin(thisObj, true);
        }

        /// <summary>
        /// On exception for EndGetRequestStream callback.
        /// Note: There is no call back required for EndGetRequestStream except on exception cases.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="thisObj">This object.</param>
        /// <param name="asyncResult">The asyncResult parameter.</param>
        /// <param name="transportContext">The transportContext parameter.</param>
        public void OnExceptionForEndGetRequestStream(object context, object exception, object thisObj, object asyncResult, object transportContext)
        {
            this.OnEnd(exception, thisObj, null);
        }

#endregion // Http callbacks

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
        /// Common helper for all Begin Callbacks.
        /// </summary>
        /// <param name="thisObj">This object.</param>        
        /// <param name="isAsyncCall">Indicates if the method used is async or not.</param>        
        /// <returns>Null object as all context is maintained in this class via weak tables.</returns>
        private object OnBegin(object thisObj, bool isAsyncCall)
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

                string httMethod = webRequest.Method;
                string resourceName = url.AbsolutePath;

                if (!string.IsNullOrEmpty(httMethod))
                {
                    resourceName = httMethod + " " + resourceName;
                }                

                DependencyCollectorEventSource.Log.BeginCallbackCalled(thisObj.GetHashCode(), resourceName);

                if (this.applicationInsightsUrlFilter.IsApplicationInsightsUrl(url.ToString()))
                {
                    // Not logging as we will be logging for all outbound AI calls
                    return null;
                }

                // If the object already exists, don't add again. This happens because either GetResponse or GetRequestStream could
                // be the starting point for the outbound call.
                var telemetryTuple = this.TelemetryTable.Get(thisObj);
                if (telemetryTuple != null)
                {
                    if (telemetryTuple.Item1 != null)
                    {
                        DependencyCollectorEventSource.Log.TrackingAnExistingTelemetryItemVerbose();
                        return null;
                    }
                }

                bool isCustomCreated = false;

                var telemetry = ClientServerDependencyTracker.BeginTracking(this.telemetryClient);

                telemetry.Name = resourceName;
                telemetry.Target = url.Host;
                telemetry.Type = RemoteDependencyConstants.HTTP;
                telemetry.Data = url.OriginalString;

                this.TelemetryTable.Store(thisObj, new Tuple<DependencyTelemetry, bool>(telemetry, isCustomCreated));

                if (string.IsNullOrEmpty(telemetry.Context.InstrumentationKey))
                {
                    // Instrumentation key is probably empty, because the context has not yet had a chance to associate the requestTelemetry to the telemetry client yet.
                    // and get they instrumentation key from all possible sources in the process. Let's do that now.
                    this.telemetryClient.Initialize(telemetry);
                }

                // Add the source instrumentation key header if collection is enabled, the request host is not in the excluded list and the same header doesn't already exist
                if (this.setCorrelationHeaders
                    && !this.correlationDomainExclusionList.Contains(url.Host)
                    && !string.IsNullOrEmpty(telemetry.Context.InstrumentationKey)
                    && webRequest.Headers[RequestResponseHeaders.SourceInstrumentationKeyHeader] == null)
                {
                    webRequest.Headers.Add(RequestResponseHeaders.SourceInstrumentationKeyHeader, InstrumentationKeyHashLookupHelper.GetInstrumentationKeyHash(telemetry.Context.InstrumentationKey));
                }

                // Add the root ID
                var rootId = telemetry.Context.Operation.Id;
                if (!string.IsNullOrEmpty(rootId) && webRequest.Headers[RequestResponseHeaders.StandardRootIdHeader] == null)
                {
                    webRequest.Headers.Add(RequestResponseHeaders.StandardRootIdHeader, rootId);
                }

                // Add the parent ID
                var parentId = telemetry.Id;
                if (!string.IsNullOrEmpty(parentId) && webRequest.Headers[RequestResponseHeaders.StandardParentIdHeader] == null)
                {
                    webRequest.Headers.Add(RequestResponseHeaders.StandardParentIdHeader, parentId);
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
        /// <param name="exception">The exception object if any.</param>
        /// <param name="thisObj">This object.</param>                
        /// <param name="returnValue">Return value of the function if any.</param>
        private void OnEnd(object exception, object thisObj, object returnValue)
        {
            try
            {  
                if (thisObj == null)
                {
                    DependencyCollectorEventSource.Log.NotExpectedCallback(0, "OnBeginHttp", "thisObj == null");
                    return;
                }

                DependencyCollectorEventSource.Log.EndCallbackCalled(thisObj.GetHashCode().ToString(CultureInfo.InvariantCulture));

                Tuple<DependencyTelemetry, bool> telemetryTuple = this.TelemetryTable.Get(thisObj);

                if (telemetryTuple == null)
                {
                    DependencyCollectorEventSource.Log.EndCallbackWithNoBegin(thisObj.GetHashCode().ToString(CultureInfo.InvariantCulture));
                    return;
                }

                if (telemetryTuple.Item1 == null)
                {
                    DependencyCollectorEventSource.Log.EndCallbackWithNoBegin(thisObj.GetHashCode().ToString(CultureInfo.InvariantCulture));
                    return;
                }

                // Not custom created
                if (!telemetryTuple.Item2)
                {
                    this.TelemetryTable.Remove(thisObj);
                    DependencyTelemetry telemetry = telemetryTuple.Item1;

                    int statusCode = -1;

                    var responseObj = returnValue as HttpWebResponse;

                    if (responseObj == null && exception != null)
                    {
                        var webException = exception as WebException;

                        if (webException != null)
                        {
                            responseObj = webException.Response as HttpWebResponse;
                        }
#if !NET40
                        if (responseObj == null)
                        {
                            var httpException = exception as HttpException;

                            if (httpException != null)
                            {
                                statusCode = httpException.GetHttpCode();
                            }
                        }
#endif
                    }

                    if (responseObj != null)
                    {
                        try
                        {
                            statusCode = (int)responseObj.StatusCode;

                            if (responseObj.Headers != null)
                            {
                                var targetIkeyHash = responseObj.Headers[RequestResponseHeaders.TargetInstrumentationKeyHeader];
                                
                                // We only add the cross component correlation key if the key does not remain the current component.
                                if (!string.IsNullOrEmpty(targetIkeyHash) && targetIkeyHash != InstrumentationKeyHashLookupHelper.GetInstrumentationKeyHash(telemetry.Context.InstrumentationKey))
                                {
                                    telemetry.Type = RemoteDependencyConstants.AI;
                                    telemetry.Target += " | " + targetIkeyHash;
                                }
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                            // ObjectDisposedException is expected here in the following sequence: httpWebRequest.GetResponse().Dispose() -> httpWebRequest.GetResponse()
                            // on the second call to GetResponse() we cannot determine the statusCode.
                        }
                    }
                    
                    telemetry.ResultCode = statusCode > 0 ? statusCode.ToString(CultureInfo.InvariantCulture) : string.Empty;
                    telemetry.Success = (statusCode > 0) && (statusCode < 400);

                    ClientServerDependencyTracker.EndTracking(this.telemetryClient, telemetry);
                }
            }
            catch (Exception ex)
            {
                DependencyCollectorEventSource.Log.CallbackError(thisObj == null ? 0 : thisObj.GetHashCode(), "OnBeginHttp", ex);
            }                
        }
    }
}