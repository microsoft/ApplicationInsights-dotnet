namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Web;

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
        private bool correlationHeadersEnabled = true;

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
        /// Implements on begin callback of http module.
        /// </summary>
        public void OnBeginRequest(HttpContext context)
        {
            if (this.telemetryClient == null)
            {
                throw new InvalidOperationException("Initialize has not been called on this module yet.");
            }

            if (context == null)
            {
                WebEventSource.Log.NoHttpContextWarning();
                return;
            }

            var requestTelemetry = context.ReadOrCreateRequestTelemetryPrivate();

            // NB! Whatever is saved in RequestTelemetry on Begin is not guaranteed to be sent because Begin may not be called; Keep it in context
            // In WCF there will be 2 Begins and 1 End. We need time from the first one
            if (requestTelemetry.Timestamp == DateTimeOffset.MinValue)
            {
                requestTelemetry.Start();
            }
        }

        /// <summary>
        /// Implements on end callback of http module.
        /// </summary>
        public void OnEndRequest(HttpContext context)
        {
            if (this.telemetryClient == null)
            {
                throw new InvalidOperationException("Initialize has not been called on this module yet.");
            }

            if (!this.NeedProcessRequest(context))
            {
                return;
            }

            var requestTelemetry = context.ReadOrCreateRequestTelemetryPrivate();
            requestTelemetry.Stop();

            // Success will be set in Sanitize on the base of ResponseCode 
            if (string.IsNullOrEmpty(requestTelemetry.ResponseCode))
            {
                requestTelemetry.ResponseCode = context.Response.StatusCode.ToString(CultureInfo.InvariantCulture);
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

            if (!string.IsNullOrEmpty(requestTelemetry.Context.InstrumentationKey)
                && context.Response.Headers[RequestResponseHeaders.TargetAppIdHeader] == null)
            {
                string appId;

                if (CorelationIdLookupHelper.TryGetAppId(requestTelemetry.Context.InstrumentationKey, out appId))
                {
                    context.Response.Headers[RequestResponseHeaders.TargetAppIdHeader] = appId;
                }
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
    }
}