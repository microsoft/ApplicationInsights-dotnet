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
            this.telemetryClient.Context.GetInternalContext().SdkVersion = "web: " + SdkVersionUtils.GetAssemblyVersion();
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
    }
}