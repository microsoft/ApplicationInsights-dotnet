namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Web;

    using Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Common;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Web.Implementation;

    /// <summary>
    /// Telemetry module tracking requests using http module.
    /// </summary>
    public class RequestTrackingTelemetryModule : ITelemetryModule
    {
        private readonly IList<string> handlersToFilter = new List<string>();
        private TelemetryClient telemetryClient;
        private bool initializationErrorReported;
        private bool correlationHeadersEnabled = true;
        private string telemetryChannelEnpoint;
        private CorrelationIdLookupHelper correlationIdLookupHelper;
        private ChildRequestTrackingSuppressionModule childRequestTrackingSuppressionModule = null;

        /// <summary>
        /// Gets or sets a value indicating whether child request suppression is enabled or disabled. 
        /// True by default.
        /// This value is evaluated in Initialize().
        /// </summary>
        /// <remarks>
        /// See also <see cref="ChildRequestTrackingSuppressionModule" />.
        /// Child requests caused by <see cref="System.Web.Handlers.TransferRequestHandler" />.
        /// Unit tests should disable this.
        /// </remarks>
        public bool EnableChildRequestTrackingSuppression { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating the size of internal tracking dictionary.
        /// Must be a positive integer.
        /// </summary>
        /// <remarks>
        /// See also <see cref="ChildRequestTrackingSuppressionModule" />.
        /// </remarks>
        public int ChildRequestTrackingInternalDictionarySize { get; set; }
        
        /// <summary>
        /// Gets the list of handler types for which requests telemetry will not be collected
        /// if request was successful.
        /// </summary>
        public IList<string> Handlers
        {
            get
            {
                return this.handlersToFilter;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the component correlation headers would be set on http responses.
        /// </summary>
        public bool SetComponentCorrelationHttpHeaders
        {
            get
            {
                return this.correlationHeadersEnabled;
            }

            set
            {
                this.correlationHeadersEnabled = value;
            }
        }

        /// <summary>
        /// Gets or sets the endpoint that is to be used to get the application insights resource's profile (appId etc.).
        /// </summary>
        public string ProfileQueryEndpoint { get; set; }

        internal string EffectiveProfileQueryEndpoint
        {
            get
            {
                return string.IsNullOrEmpty(this.ProfileQueryEndpoint) ? this.telemetryChannelEnpoint : this.ProfileQueryEndpoint;
            }
        }
        
        /// <summary>
        /// Implements on begin callback of http module.
        /// </summary>
        public void OnBeginRequest(HttpContext context)
        {
            if (this.telemetryClient == null)
            {
                if (!this.initializationErrorReported)
                {
                    this.initializationErrorReported = true;
                    WebEventSource.Log.InitializeHasNotBeenCalledOnModuleYetError();
                }
                else
                {
                    WebEventSource.Log.InitializeHasNotBeenCalledOnModuleYetVerbose();
                }

                return;
            }

            if (context == null)
            {
                WebEventSource.Log.NoHttpContextWarning();
                return;
            }

            this.childRequestTrackingSuppressionModule?.OnBeginRequest_IdRequest(context);

            var telemetry = context.ReadOrCreateRequestTelemetryPrivate();

            // NB! Whatever is saved in RequestTelemetry on Begin is not guaranteed to be sent because Begin may not be called; Keep it in context
            // In WCF there will be 2 Begins and 1 End. We need time from the first one
            if (telemetry.Timestamp == DateTimeOffset.MinValue)
            {
                telemetry.Start();
            }
        }

        /// <summary>
        /// Implements on end callback of http module.
        /// </summary>
        public void OnEndRequest(HttpContext context)
        {
            if (this.telemetryClient == null)
            {
                if (!this.initializationErrorReported)
                {
                    this.initializationErrorReported = true;
                    WebEventSource.Log.InitializeHasNotBeenCalledOnModuleYetError();
                }
                else
                {
                    WebEventSource.Log.InitializeHasNotBeenCalledOnModuleYetVerbose();
                }

                return;
            }

            if (!this.NeedProcessRequest(context))
            {
                return;
            }

            var requestTelemetry = context.ReadOrCreateRequestTelemetryPrivate();
            requestTelemetry.Stop();

            var success = true;
            if (string.IsNullOrEmpty(requestTelemetry.ResponseCode))
            {
                var statusCode = context.Response.StatusCode;
                requestTelemetry.ResponseCode = statusCode.ToString(CultureInfo.InvariantCulture);

                if (statusCode >= 400 && statusCode != 401)
                {
                    success = false;
                }
            }

            if (!requestTelemetry.Success.HasValue)
            {
                requestTelemetry.Success = success;
            }

            if (requestTelemetry.Url == null)
            {
                requestTelemetry.Url = context.Request.UnvalidatedGetUrl();
            }

            if (string.IsNullOrEmpty(requestTelemetry.Context.InstrumentationKey))
            {
                // Instrumentation key is probably empty, because the context has not yet had a chance to associate the requestTelemetry to the telemetry client yet.
                // and get they instrumentation key from all possible sources in the process. Let's do that now.
                this.telemetryClient.Initialize(requestTelemetry);
            }

            if (string.IsNullOrEmpty(requestTelemetry.Source) && context.Request.Headers != null)
            {
                string telemetrySource = string.Empty;
                string sourceAppId = null;

                try
                {
                    sourceAppId = context.Request.UnvalidatedGetHeaders().GetNameValueHeaderValue(RequestResponseHeaders.RequestContextHeader, RequestResponseHeaders.RequestContextCorrelationSourceKey);
                }
                catch (Exception ex)
                {
                    AppMapCorrelationEventSource.Log.GetCrossComponentCorrelationHeaderFailed(ex.ToInvariantString());
                }
                
                bool correlationIdLookupHelperInitialized = this.TryInitializeCorrelationHelperIfNotInitialized();

                string currentComponentAppId = string.Empty;
                bool foundMyAppId = false;
                if (!string.IsNullOrEmpty(requestTelemetry.Context.InstrumentationKey) && correlationIdLookupHelperInitialized)
                {
                    foundMyAppId = this.correlationIdLookupHelper.TryGetXComponentCorrelationId(requestTelemetry.Context.InstrumentationKey, out currentComponentAppId);
                }

                // If the source header is present on the incoming request,
                // and it is an external component (not the same ikey as the one used by the current component),
                // then populate the source field.
                if (!string.IsNullOrEmpty(sourceAppId)
                    && foundMyAppId
                    && sourceAppId != currentComponentAppId)
                {
                    telemetrySource = sourceAppId;
                }

                string sourceRoleName = null;

                try
                {
                    sourceRoleName = context.Request.UnvalidatedGetHeaders().GetNameValueHeaderValue(RequestResponseHeaders.RequestContextHeader, RequestResponseHeaders.RequestContextSourceRoleNameKey);
                }
                catch (Exception ex)
                {
                    AppMapCorrelationEventSource.Log.GetComponentRoleNameHeaderFailed(ex.ToInvariantString());
                }

                if (!string.IsNullOrEmpty(sourceRoleName))
                {
                    if (string.IsNullOrEmpty(telemetrySource))
                    {
                        telemetrySource = "roleName:" + sourceRoleName;
                    }
                    else
                    {
                        telemetrySource += " | roleName:" + sourceRoleName;
                    }
                }

                requestTelemetry.Source = telemetrySource;
            }

            if (this.childRequestTrackingSuppressionModule?.OnEndRequest_ShouldLog(context) ?? true)
            {
                this.telemetryClient.TrackRequest(requestTelemetry);
            }
            else
            {
                WebEventSource.Log.RequestTrackingTelemetryModuleRequestWasNotLoggedInformational();
            }
        }

        /// <summary>
        /// Adds target response header response object.
        /// </summary>
        public void AddTargetHashForResponseHeader(HttpContext context)
        {
            if (this.telemetryClient == null)
            {
                throw new InvalidOperationException();
            }

            var requestTelemetry = context.GetRequestTelemetry();

            if (string.IsNullOrEmpty(requestTelemetry.Context.InstrumentationKey))
            {
                // Instrumentation key is probably empty, because the context has not yet had a chance to associate the requestTelemetry to the telemetry client yet.
                // and get they instrumentation key from all possible sources in the process. Let's do that now.
                this.telemetryClient.Initialize(requestTelemetry);
            }

            bool correlationIdHelperInitialized = this.TryInitializeCorrelationHelperIfNotInitialized();

            try
            {
                if (!string.IsNullOrEmpty(requestTelemetry.Context.InstrumentationKey)
                    && context.Response.Headers.GetNameValueHeaderValue(RequestResponseHeaders.RequestContextHeader, RequestResponseHeaders.RequestContextCorrelationTargetKey) == null
                    && correlationIdHelperInitialized)
                {
                    string correlationId;

                    if (this.correlationIdLookupHelper.TryGetXComponentCorrelationId(requestTelemetry.Context.InstrumentationKey, out correlationId))
                    {
                        context.Response.Headers.SetNameValueHeaderValue(RequestResponseHeaders.RequestContextHeader, RequestResponseHeaders.RequestContextCorrelationTargetKey, correlationId);
                    }
                }
            }
            catch (Exception ex)
            {
                AppMapCorrelationEventSource.Log.SetCrossComponentCorrelationHeaderFailed(ex.ToInvariantString());
            }
        }

        /// <summary>
        /// Initializes the telemetry module.
        /// </summary>
        /// <param name="configuration">Telemetry configuration to use for initialization.</param>
        public void Initialize(TelemetryConfiguration configuration)
        {
            this.telemetryClient = new TelemetryClient(configuration);
            this.telemetryClient.Context.GetInternalContext().SdkVersion = SdkVersionUtils.GetSdkVersion("web:");

            if (configuration != null && configuration.TelemetryChannel != null)
            {
                this.telemetryChannelEnpoint = configuration.TelemetryChannel.EndpointAddress;
            }

            // Headers will be read-only in a classic iis pipeline
            // Exception System.PlatformNotSupportedException: This operation requires IIS integrated pipeline mode.
            if (HttpRuntime.UsingIntegratedPipeline && this.EnableChildRequestTrackingSuppression)
            {
                this.childRequestTrackingSuppressionModule = new ChildRequestTrackingSuppressionModule(maxRequestsTracked: this.ChildRequestTrackingInternalDictionarySize);
            }
        }

        /// <summary>
        /// Verifies context to detect whether or not request needs to be processed.
        /// </summary>
        /// <param name="httpContext">Current http context.</param>
        /// <returns>True if request needs to be processed, otherwise - False.</returns>
        internal bool NeedProcessRequest(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                WebEventSource.Log.NoHttpContextWarning();
                return false;
            }

            if (httpContext.Response.StatusCode < 400)
            {
                if (this.IsHandlerToFilter(httpContext.Handler))
                {
                    return false;
                }
            }

            return true;
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
        /// Checks whether or not handler is a transfer handler.
        /// </summary>
        /// <param name="handler">An instance of handler to validate.</param>
        /// <returns>True if handler is a transfer handler, otherwise - False.</returns>
        private bool IsHandlerToFilter(IHttpHandler handler)
        {
            if (handler != null)
            {
                var handlerName = handler.GetType().FullName;
                foreach (var h in this.Handlers)
                {
                    if (string.Equals(handlerName, h, StringComparison.Ordinal))
                    {
                        WebEventSource.Log.WebRequestFilteredOutByRequestHandler();
                        return true;
                    }
                }
            }

            return false;
        }

        private bool TryInitializeCorrelationHelperIfNotInitialized()
        {
            try
            {
                if (this.correlationIdLookupHelper == null)
                {
                    this.correlationIdLookupHelper = new CorrelationIdLookupHelper(this.EffectiveProfileQueryEndpoint);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// <see cref="System.Web.Handlers.TransferRequestHandler"/> can create a Child request to route extension-less requests to a controller.
        /// (ex: site/home -> site/HomeController.cs)
        /// We do not want duplicate telemetry logged for both the Parent and Child requests, so the activeRequests will be created OnBeginRequest.
        /// When the child request OnEndRequest, the id will be removed from this dictionary and telemetry will not be logged for the parent.
        /// </summary>
        /// <remarks>
        /// Unit tests should disable the ChildRequestTrackingSuppressionModule.
        /// Unit test projects cannot create an [internal] IIS7WorkerRequest object.
        /// Without this object, we cannot modify the Request.Headers without throwing a PlatformNotSupportedException.
        /// Unit tests will have to initialize the RequestIdHeader.
        /// The second IF will ensure the id is added to the activeRequests.
        /// </remarks>
        /// <remarks>
        /// IIS Classic Pipeline should disable the ChildRequestTrackingSuppressionModule.
        /// Classic does not create IIS7WorkerRequest object and Headers will be read-only.
        /// (Exception System.PlatformNotSupportedException: This operation requires IIS integrated pipeline mode.)
        /// </remarks>
        private class ChildRequestTrackingSuppressionModule
        {
            private const int DEFAULTMAXVALUE = 100000;

            private const string HeaderRootRequestId = "ApplicationInsights-RequestTrackingTelemetryModule-RootRequest-Id";
#if DEBUG
            private const string HeaderParentRequestId = "ApplicationInsights-RequestTrackingTelemetryModule-ParentRequest-Id";
            private const string HeaderRequestId = "ApplicationInsights-RequestTrackingTelemetryModule-Request-Id";
#endif

            private static object semaphore = new object();

            /// <summary>
            /// Using this as a hash-set of current active requests. The value of the Dictionary is not used.
            /// </summary>
            private static ConcurrentDictionary<string, bool> activeRequestsA = new ConcurrentDictionary<string, bool>(System.Environment.ProcessorCount, DEFAULTMAXVALUE);
            private static ConcurrentDictionary<string, bool> activeRequestsB = new ConcurrentDictionary<string, bool>(System.Environment.ProcessorCount, DEFAULTMAXVALUE);

            /// <summary>
            /// Initializes a new instance of the <see cref="ChildRequestTrackingSuppressionModule" /> class.
            /// </summary>
            /// <param name="maxRequestsTracked">The maximum number of active requests to be tracked before resetting the dictionary.</param>
            internal ChildRequestTrackingSuppressionModule(int maxRequestsTracked)
            {
                this.MAXSIZE = maxRequestsTracked > 0 ? maxRequestsTracked : DEFAULTMAXVALUE;
            }

            /// <summary>
            /// Gets the Max number of request ids to cache.
            /// </summary>
            internal int MAXSIZE { get; private set; }

            /// <summary>
            /// Request will be tagged with an id to identify if it should be logged later.
            /// </summary>
            internal void OnBeginRequest_IdRequest(HttpContext context)
            {
                if (context?.Request?.Headers == null)
                {
                    return;
                }

                try
                {
                    this.TagRequest(context);
                }
                catch (Exception ex)
                {
                   WebEventSource.Log.ChildRequestUnknownException(nameof(this.OnBeginRequest_IdRequest), ex.ToInvariantString());
                }
            }

            /// <summary>
            /// OnEndRequest - Should this request be logged?
            /// Will compare a request id against a hash-set of known requests.
            /// If this request is not known, add it to hash-set and return true (safe to log).
            /// If this request is known, return false (do not log twice).
            /// Additional requests with the same id will return false.
            /// </summary>
            internal bool OnEndRequest_ShouldLog(HttpContext context)
            {
                var headers = context?.Request?.Headers;
                if (headers == null)
                {
                    return false;
                }

                try
                {
                    var rootRequestId = headers[HeaderRootRequestId];
                    rootRequestId = StringUtilities.EnforceMaxLength(rootRequestId, InjectionGuardConstants.RequestHeaderMaxLength);
                    if (rootRequestId != null)
                    {
                        if (!this.IsRequestKnown(rootRequestId))
                        {
                            // doesn't exist add to dictionary and return true
                            this.AddRequestToDictionary(rootRequestId);
                            return true;
                        }
                        else
                        {
                            WebEventSource.Log.RequestTrackingTelemetryModuleRequestWasNotLoggedVerbose(rootRequestId, "Request is already known");
                        }
                    }
                    else
                    {
                        WebEventSource.Log.RequestTrackingTelemetryModuleRequestWasNotLoggedVerbose(rootRequestId, "Request id is null");
                    }
                }
                catch (Exception ex)
                {
                    WebEventSource.Log.ChildRequestUnknownException(nameof(this.OnEndRequest_ShouldLog), ex.ToInvariantString());
                }

                return false;
            }

            /// <summary>
            /// Tag new requests.
            /// Transfer Ids to parent requests.
            /// </summary>
            private void TagRequest(HttpContext context)
            {
                var headers = context.Request.Headers;

                if (headers[HeaderRootRequestId] == null)
                {
                    headers[HeaderRootRequestId] = Guid.NewGuid().ToString();
                }

#if DEBUG
                // additional ids help developer watch request hierarchy while debugging.
                if (headers[HeaderRequestId] != null)
                {
                    headers[HeaderParentRequestId] = headers[HeaderRequestId];
                    headers[HeaderRequestId] = Guid.NewGuid().ToString();
                }
                else
                {
                    headers[HeaderRequestId] = headers[HeaderRootRequestId];
                }
#endif
            }

            /// <summary>
            /// Has this request been tracked.
            /// </summary>
            private bool IsRequestKnown(string requestId)
            {
                return activeRequestsA.ContainsKey(requestId) || activeRequestsB.ContainsKey(requestId);
            }

            /// <summary>
            /// Track this requestId.
            /// </summary>
            /// <remarks>
            /// Dictionary A will be read/write.
            /// When dictionary A is full, move to B and create new A.
            /// Dictionary B will be read-only.
            /// </remarks>
            private void AddRequestToDictionary(string requestId)
            {
                if (activeRequestsA.Count >= this.MAXSIZE)
                {
                    // only lock around the edge case to avoid locking EVERY request thread
                    lock (semaphore)
                    {
                        // in the event that multiple threads step into the first if, 
                        // check condition again to avoid repeat operations.
                        if (activeRequestsA.Count >= this.MAXSIZE)
                        {
                            activeRequestsB = activeRequestsA;
                            activeRequestsA = new ConcurrentDictionary<string, bool>(System.Environment.ProcessorCount, this.MAXSIZE);
                        }
                    }
                }

                activeRequestsA.TryAdd(requestId, false);
            }
        }
    }
}