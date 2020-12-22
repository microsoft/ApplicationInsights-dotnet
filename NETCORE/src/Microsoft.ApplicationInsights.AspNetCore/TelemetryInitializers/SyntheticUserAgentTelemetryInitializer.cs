namespace Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers
{
    using System;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// This will allow to mark synthetic traffic from user agent content.
    /// </summary>
    public class SyntheticUserAgentTelemetryInitializer : TelemetryInitializerBase
    {
        private const string SyntheticUserAgentFiltersFromConfig = "ApplicationInsights:SyntheticUserAgentFilters";
        private const string SyntheticUserAgentSourceName = "Bot";
        private const string SyntheticUserAgentSourceNameKey = "Microsoft.ApplicationInsights.RequestTelemetry.SyntheticUserAgentSource";
        private const string UserAgentHeader = "User-Agent";
        private const string DefaultFilters = "search|spider|crawl|Bot|Monitor|AlwaysOn";
        private string filters = string.Empty;
        private string[] filterPatterns;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyntheticUserAgentTelemetryInitializer" /> class.
        /// </summary>
        /// <param name="httpContextAccessor">Accessor to provide HttpContext corresponding to telemetry items.</param>
        /// <param name="configuration">Configuration to get synthetic user agent filters.</param>
        public SyntheticUserAgentTelemetryInitializer(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
                : base(httpContextAccessor)
        {
            if (configuration is null)
            {
                this.Filters = DefaultFilters;
            }
            else
            {
                var configurationFilters = configuration[SyntheticUserAgentFiltersFromConfig];
                if (string.IsNullOrWhiteSpace(configurationFilters))
                {
                    this.Filters = DefaultFilters;
                }
                else
                {
                    this.Filters = configurationFilters;
                }
            }
        }

        /// <summary>
        /// Gets or sets the configured patterns for matching synthetic traffic filters through user agent string.
        /// </summary>
        public string Filters
        {
            get => this.filters;

            set
            {
                if (value != null)
                {
                    this.filters = value;
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
            if (telemetry == null)
            {
                throw new ArgumentNullException(nameof(telemetry));
            }

            if (string.IsNullOrEmpty(telemetry.Context.Operation.SyntheticSource))
            {
                if (platformContext == null)
                {
                    throw new ArgumentNullException(nameof(platformContext));
                }

                if (platformContext.Items.ContainsKey(SyntheticUserAgentSourceNameKey))
                {
                    telemetry.Context.Operation.SyntheticSource = SyntheticUserAgentSourceName;
                }
                else
                {
                    string userAgent = platformContext.Request?.Headers[UserAgentHeader];
                    if (!string.IsNullOrEmpty(userAgent))
                    {
                        foreach (var filterPattern in this.filterPatterns)
                        {
                            if (userAgent.IndexOf(filterPattern, StringComparison.OrdinalIgnoreCase) != -1)
                            {
                                telemetry.Context.Operation.SyntheticSource = SyntheticUserAgentSourceName;
                                platformContext.Items.Add(SyntheticUserAgentSourceNameKey, SyntheticUserAgentSourceName);
                                return;
                            }
                        }
                    }
                    else
                    {
                        telemetry.Context.Operation.SyntheticSource = SyntheticUserAgentSourceName;
                    }
                }
            }
        }
    }
}
