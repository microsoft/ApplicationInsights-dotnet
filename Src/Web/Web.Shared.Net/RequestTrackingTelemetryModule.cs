namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Collections.Generic;
#if NET45
    using System.Diagnostics.Tracing;
#endif
    using System.Globalization;
    using System.Web;

    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Web.Implementation;

#if NET40
    using Microsoft.Diagnostics.Tracing;
#endif

    /// <summary>
    /// Telemetry module tracking requests using http module.
    /// </summary>
    public class RequestTrackingTelemetryModule : ITelemetryModule, IDisposable
    {
        private readonly EventListener listener;
        private readonly IList<string> handlersToFilter = new List<string>();
        private TelemetryClient telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestTrackingTelemetryModule" /> class.
        /// </summary>
        public RequestTrackingTelemetryModule()
        {
            this.listener = new WebEventsSubscriber(
                new Dictionary<int, Action<EventWrittenEventArgs>>
                    {
                        { 1, this.OnBeginRequest },
                        { 2, this.OnEndRequest }
                    });
        }

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
        /// Implements on begin callback of http module.
        /// </summary>
        public void OnBeginRequest(EventWrittenEventArgs args)
        {
            if (this.telemetryClient == null)
            {
                throw new InvalidOperationException();
            }

            var platformContext = this.ResolvePlatformContext();
            var requestTelemetry = platformContext.ReadOrCreateRequestTelemetryPrivate();

            // NB! Whatever is saved in RequestTelemetry on Begin is not guaranteed to be sent because Begin may not be called; Keep it in context
            // In WCF there will be 2 Begins and 1 End. We need time from the first one
            if (requestTelemetry.StartTime == DateTimeOffset.MinValue)
            {
                requestTelemetry.Start();
            }
        }

        /// <summary>
        /// Implements on end callback of http module.
        /// </summary>
        public void OnEndRequest(EventWrittenEventArgs args)
        {
            if (this.telemetryClient == null)
            {
                throw new InvalidOperationException();
            }

            var platformContext = this.ResolvePlatformContext();
            if (!this.NeedProcessRequest(platformContext))
            {
                return;
            }

            var requestTelemetry = platformContext.ReadOrCreateRequestTelemetryPrivate();
            requestTelemetry.Stop();

            // Success will be set in Sanitize on the base of ResponseCode 
            if (string.IsNullOrEmpty(requestTelemetry.ResponseCode))
            {
                requestTelemetry.ResponseCode = platformContext.Response.StatusCode.ToString(CultureInfo.InvariantCulture);
            }

            if (requestTelemetry.Url == null)
            {
                requestTelemetry.Url = platformContext.Request.UnvalidatedGetUrl();
            }

            if (string.IsNullOrEmpty(requestTelemetry.HttpMethod))
            {
                requestTelemetry.HttpMethod = platformContext.Request.HttpMethod;
            }

            this.telemetryClient.TrackRequest(requestTelemetry);
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
        /// Dispose method.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
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
        /// Returns current HttpContext.
        /// </summary>
        /// <returns>Current HttpContext.</returns>
        protected virtual HttpContext ResolvePlatformContext()
        {
            return HttpContext.Current;
        }

        private void Dispose(bool dispose)
        {
            if (dispose && this.listener != null)
            {
                this.listener.Dispose();
            }
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