namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Web;

    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Web.Implementation;

    /// <summary>
    /// Telemetry module to collect unhandled exceptions caught by http module.
    /// </summary>
    public class ExceptionTrackingTelemetryModule : ITelemetryModule
    {
        private TelemetryClient telemetryClient;

        /// <summary>
        /// Implements on error callback of http module.
        /// </summary>
        public void OnError(HttpContext context)
        {
            if (this.telemetryClient == null)
            {
                throw new InvalidOperationException();
            }

            if (context == null)
            {
                WebEventSource.Log.NoHttpContextWarning();
                return;
            }

            var errors = context.AllErrors;

            if (errors != null && errors.Length > 0)
            {
                foreach (Exception exp in errors)
                {
                    var exceptionTelemetry = new ExceptionTelemetry(exp);

                    if (context.Response.StatusCode >= 500)
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
    }
}
