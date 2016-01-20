namespace Microsoft.ApplicationInsights.Web
{
    using System.Web;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Web.Implementation;

    /// <summary>
    /// A telemetry initializer that will update the User, Session and Operation contexts if request is synthetic.
    /// </summary>
    public class SyntheticTelemetryInitializer : WebTelemetryInitializerBase
    {
        private SyntheticTrafficManager syntheticTrafficManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyntheticTelemetryInitializer"/> class.
        /// </summary>
        public SyntheticTelemetryInitializer()
        {
            this.syntheticTrafficManager = new SyntheticTrafficManager();
        }

        /// <summary>
        /// Implements initialization logic.
        /// </summary>
        /// <param name="platformContext">Http context.</param>
        /// <param name="requestTelemetry">Request telemetry object associated with the current request.</param>
        /// <param name="telemetry">Telemetry item to initialize.</param>
        protected override void OnInitializeTelemetry(
            HttpContext platformContext,
            RequestTelemetry requestTelemetry,
            ITelemetry telemetry)
        {
            if (this.syntheticTrafficManager.IsSynthetic(platformContext))
            {
                if (string.IsNullOrEmpty(telemetry.Context.Operation.SyntheticSource))
                {
                    telemetry.Context.Operation.SyntheticSource = this.syntheticTrafficManager.GetSyntheticSource(platformContext);
                }

                if (string.IsNullOrEmpty(telemetry.Context.User.Id))
                {
                    telemetry.Context.User.Id = this.syntheticTrafficManager.GetUserId(platformContext);
                }

                if (string.IsNullOrEmpty(telemetry.Context.Session.Id))
                {
                    telemetry.Context.Session.Id = this.syntheticTrafficManager.GetSessionId(platformContext);
                }
            }
        }
    }
}