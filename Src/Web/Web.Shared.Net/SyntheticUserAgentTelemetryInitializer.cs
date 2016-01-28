namespace Microsoft.ApplicationInsights.Web
{
    using System.Collections.Generic;
    using System.Web;
    using Implementation;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;

    /// <summary>
    /// A telemetry initializer that determines if the request came from a synthetic source based on the user agent string.
    /// </summary>
    public class SyntheticUserAgentTelemetryInitializer : WebTelemetryInitializerBase
    {
        private readonly IList<SyntheticUserAgentFilter> filterPatterns = new List<SyntheticUserAgentFilter>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SyntheticUserAgentTelemetryInitializer" /> class.
        /// </summary>
        public SyntheticUserAgentTelemetryInitializer()
        {
        }

        /// <summary>
        /// Gets the configured patterns for matching synthetic traffic filters through user agent string.
        /// </summary>
        public IList<SyntheticUserAgentFilter> Filters
        {
            get
            {
                return this.filterPatterns;
            }
        }

        /// <summary>
        /// Implements initialization logic.
        /// </summary>
        /// <param name="platformContext">Http context.</param>
        /// <param name="requestTelemetry">Request telemetry object associated with the current request.</param>
        /// <param name="telemetry">Telemetry item to initialize.</param>
        protected override void OnInitializeTelemetry(HttpContext platformContext, RequestTelemetry requestTelemetry, ITelemetry telemetry)
        {
            if (platformContext != null)
            {
                var request = platformContext.GetRequest();

                if (request != null)
                {
                    foreach (var pattern in this.filterPatterns)
                    {
                        if (pattern.RegularExpression != null)
                        {
                            var match = pattern.RegularExpression.Match(request.UserAgent);
                            if (match.Success)
                            {
                                if (string.IsNullOrEmpty(telemetry.Context.Operation.SyntheticSource))
                                {
                                    telemetry.Context.Operation.SyntheticSource = !string.IsNullOrWhiteSpace(pattern.SourceName) ? pattern.SourceName : match.Value;
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}