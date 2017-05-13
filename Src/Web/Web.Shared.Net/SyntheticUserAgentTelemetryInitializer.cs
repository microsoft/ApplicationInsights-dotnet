namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Web;
    using Implementation;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;

    /// <summary>
    /// A telemetry initializer that determines if the request came from a synthetic source based on the user agent string.
    /// </summary>
    public class SyntheticUserAgentTelemetryInitializer : WebTelemetryInitializerBase
    {
        private const string SyntheticSourceNameKey = "Microsoft.ApplicationInsights.RequestTelemetry.SyntheticSource";
        private const string SyntheticSourceName = "Bot";
        private string filters = string.Empty;
        private string[] filterPatterns;        

        /// <summary>
        /// Gets or sets the configured patterns for matching synthetic traffic filters through user agent string.
        /// </summary>
        public string Filters
        {
            get
            {
                return this.filters;
            }

            set
            {
                if (value != null)
                {
                    this.filters = value;

                    // We expect customers to configure telemetry initializer before they add it to active configuration
                    // So we will not protect it with locks (to improve perf)
                    this.filterPatterns = value.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
                }
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
            if (string.IsNullOrEmpty(telemetry.Context.Operation.SyntheticSource))
            {
                if (platformContext != null)
                {
                    if (platformContext.Items.Contains(SyntheticSourceNameKey))
                    {
                        telemetry.Context.Operation.SyntheticSource = platformContext.Items[SyntheticSourceNameKey].ToString();
                    }
                    else
                    {
                        var request = platformContext.GetRequest();
                        if (request != null && !string.IsNullOrEmpty(request.UserAgent))
                        {
                            // We expect customers to configure telemetry initializer before they add it to active configuration
                            // So we will not protect fiterPatterns array with locks (to improve perf)
                            string userAgent = request.UserAgent;
                            for (int i = 0; i < this.filterPatterns.Length; i++)
                            {
                                if (userAgent.IndexOf(this.filterPatterns[i], StringComparison.OrdinalIgnoreCase) != -1)
                                {
                                    telemetry.Context.Operation.SyntheticSource = SyntheticSourceName;
                                    platformContext.Items.Add(SyntheticSourceNameKey, SyntheticSourceName);
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