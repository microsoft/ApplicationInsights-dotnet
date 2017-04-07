namespace Microsoft.ApplicationInsights.Web
{
    using System;
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

            var telemetry = context.ReadOrCreateRequestTelemetryPrivate();

            // NB! Whatever is saved in RequestTelemetry on Begin is not guaranteed to be sent because Begin may not be called; Keep it in context
            // In WCF there will be 2 Begins and 1 End. We need time from the first one
            if (telemetry.Timestamp == DateTimeOffset.MinValue)
            {
                telemetry.Start();
            }
        }

        /// <summary>
        /// Implements on PreRequestHandlerExecute callback of http module
        /// that is executed right before the handler and restores any execution context 
        /// if it was lost in native/managed thread switches.
        /// </summary>
        public void OnPreRequestHandlerExecute(HttpContext context)
        {
            if (this.telemetryClient == null)
            {
                // TODO: write event log, when https://github.com/Microsoft/ApplicationInsights-dotnet-server/pull/390/commits/c38d6d48e666114fedba90b8df6a4f36571cbb66 is merged to 2.4.0
                return;
            }

            if (context == null)
            {
                WebEventSource.Log.NoHttpContextWarning();
                return;
            }

            ActivityHelpers.RestoreActivityIfLost(context);
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

            // we store Activity/Call context to initialize child telemtery within the scope of this request,
            // so it's time to stop it
            ActivityHelpers.StopRequestActivity();
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
                string sourceAppId = null;

                try
                {
                    sourceAppId = context.Request.UnvalidatedGetHeaders().GetNameValueHeaderValue(RequestResponseHeaders.RequestContextHeader, RequestResponseHeaders.RequestContextSourceKey);
                }
                catch (Exception ex)
                {
                    CrossComponentCorrelationEventSource.Log.GetHeaderFailed(ex.ToInvariantString());
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
                    requestTelemetry.Source = sourceAppId;
                }
            }

            this.telemetryClient.TrackRequest(requestTelemetry);
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
                    && context.Response.Headers.GetNameValueHeaderValue(RequestResponseHeaders.RequestContextHeader, RequestResponseHeaders.RequestContextTargetKey) == null
                    && correlationIdHelperInitialized)
                {
                    string correlationId;

                    if (this.correlationIdLookupHelper.TryGetXComponentCorrelationId(requestTelemetry.Context.InstrumentationKey, out correlationId))
                    {
                        context.Response.Headers.SetNameValueHeaderValue(RequestResponseHeaders.RequestContextHeader, RequestResponseHeaders.RequestContextTargetKey, correlationId);
                    }
                }
            }
            catch (Exception ex)
            {
                CrossComponentCorrelationEventSource.Log.SetHeaderFailed(ex.ToInvariantString());
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
    }
}