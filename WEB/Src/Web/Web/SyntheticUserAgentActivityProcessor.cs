namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Diagnostics;
    using System.Web;
    using Microsoft.ApplicationInsights.Web.Implementation;
    using OpenTelemetry;

    /// <summary>
    /// An activity processor that determines if the request came from a synthetic source based on the user agent string.
    /// </summary>
    internal class SyntheticUserAgentActivityProcessor : BaseProcessor<Activity>
    {
        private const string SyntheticSourceNameKey = "Microsoft.ApplicationInsights.RequestTelemetry.SyntheticSource";
        private const string SyntheticSourceName = "Bot";
        
        // Default bot/synthetic traffic patterns
        private const string DefaultFilters = "search|spider|crawl|Bot|Monitor|BrowserMob|PhantomJS|HeadlessChrome|Selenium|URLNormalization";
        
        private string filters = string.Empty;
        private string[] filterPatterns;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyntheticUserAgentActivityProcessor"/> class.
        /// </summary>
        public SyntheticUserAgentActivityProcessor()
        {
            // Initialize with default filters
            this.Filters = DefaultFilters;
        }

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

                    // We expect customers to configure the processor before they add it to active configuration
                    // So we will not protect it with locks (to improve perf)
                    this.filterPatterns = value.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
                }
            }
        }

        /// <summary>
        /// Called when an activity ends. Sets synthetic source tag if user agent matches filters.
        /// </summary>
        /// <param name="activity">The activity that ended.</param>
        public override void OnEnd(Activity activity)
        {
            if (activity == null)
            {
                return;
            }

            // Only process if synthetic source is not already set
            var existingSyntheticSource = activity.GetTagItem("ai.operation.syntheticSource");
            if (existingSyntheticSource == null || string.IsNullOrEmpty(existingSyntheticSource?.ToString()))
            {
                var context = HttpContext.Current;
                if (context != null)
                {
                    if (context.Items.Contains(SyntheticSourceNameKey))
                    {
                        // The value does not really matter.
                        activity.SetTag("ai.operation.syntheticSource", SyntheticSourceName);
                    }
                    else
                    {
                        var request = context.GetRequest();
                        string userAgent = request?.UserAgent;
                        if (!string.IsNullOrEmpty(userAgent) && this.filterPatterns != null)
                        {
                            // We expect customers to configure the processor before they add it to active configuration
                            // So we will not protect filterPatterns array with locks (to improve perf)
                            for (int i = 0; i < this.filterPatterns.Length; i++)
                            {
                                if (userAgent.IndexOf(this.filterPatterns[i], StringComparison.OrdinalIgnoreCase) != -1)
                                {
                                    activity.SetTag("ai.operation.syntheticSource", SyntheticSourceName);
                                    context.Items.Add(SyntheticSourceNameKey, SyntheticSourceName);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            base.OnEnd(activity);
        }
    }
}
