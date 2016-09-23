namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Collections.Generic;
#if NET45
    using System.Diagnostics.Tracing;
#endif
    using System.Web;

    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Web.Implementation;

#if NET40
    using Microsoft.Diagnostics.Tracing;
#endif

    /// <summary>
    /// Telemetry module to collect unhandled exceptions caught by http module.
    /// </summary>
    public class ExceptionTrackingTelemetryModule : ITelemetryModule, IDisposable
    {
        private readonly EventListener listener;

        private TelemetryClient telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionTrackingTelemetryModule" /> class.
        /// </summary>
        public ExceptionTrackingTelemetryModule()
        {
            this.listener = new WebEventsSubscriber(
                new Dictionary<int, Action<EventWrittenEventArgs>>
                    {
                        { 3, this.OnError },
                    });
        }

        /// <summary>
        /// Implements on error callback of http module.
        /// </summary>
        public void OnError(EventWrittenEventArgs args)
        {
            if (this.telemetryClient == null)
            {
                throw new InvalidOperationException();
            }

            var platformContext = this.ResolvePlatformContext();

            if (platformContext == null)
            {
                WebEventSource.Log.NoHttpContextWarning();
                return;
            }

            var errors = platformContext.AllErrors;

            if (errors != null && errors.Length > 0)
            {
                foreach (Exception exp in errors)
                {
                    var exceptionTelemetry = new ExceptionTelemetry(exp);

                    if (platformContext.Response.StatusCode >= 500)
                    {
                        exceptionTelemetry.SeverityLevel = SeverityLevel.Critical;
                    }

                    this.telemetryClient.TrackException(exceptionTelemetry);
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
