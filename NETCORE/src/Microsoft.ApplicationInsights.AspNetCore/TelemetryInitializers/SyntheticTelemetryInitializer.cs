namespace Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers
{
    using System;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// This will allow to mark synthetic traffic from availability tests or configured User-Agent filters.
    /// </summary>
    public class SyntheticTelemetryInitializer : TelemetryInitializerBase
    {
        private const string SyntheticTestRunId = "SyntheticTest-RunId";
        private const string SyntheticTestLocation = "SyntheticTest-Location";
        private const string SyntheticSourceHeaderValue = "Application Insights Availability Monitoring";

        private const string UserAgentHeader = "User-Agent";
        private const string SyntheticUserAgentFiltersFromConfig = "ApplicationInsights:SyntheticUserAgentFilters";
        private const string SyntheticUserAgentSourceName = "Bot";
        private const string SyntheticUserAgentSourceNameKey = "Microsoft.ApplicationInsights.RequestTelemetry.SyntheticUserAgentSource";
        private const string SyntheticUserAgentDefaultFilters = "search|spider|crawl|Bot|Monitor|AlwaysOn";
        private string syntheticUserAgentFilters;
        private string[] syntheticUserAgentFilterPatterns;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyntheticTelemetryInitializer" /> class.
        /// </summary>
        /// <param name="httpContextAccessor">Accessor to provide HttpContext corresponding to telemetry items.</param>
        /// <param name="configuration">Configuration to get synthetic user agent filters.</param>
        public SyntheticTelemetryInitializer(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
             : base(httpContextAccessor)
        {
            if (configuration is null)
            {
                this.SyntheticUserAgentFilters = SyntheticUserAgentDefaultFilters;
            }
            else
            {
                var configurationFilters = configuration[SyntheticUserAgentFiltersFromConfig];
                if (string.IsNullOrWhiteSpace(configurationFilters))
                {
                    this.SyntheticUserAgentFilters = SyntheticUserAgentDefaultFilters;
                }
                else
                {
                    this.SyntheticUserAgentFilters = configurationFilters;
                }
            }
        }

        /// <summary>
        /// Gets or sets the configured patterns for matching synthetic traffic filters through user agent string.
        /// </summary>
        public string SyntheticUserAgentFilters
        {
            get => this.syntheticUserAgentFilters;

            set
            {
                if (value != null)
                {
                    this.syntheticUserAgentFilters = value;
                    this.syntheticUserAgentFilterPatterns = value.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
                }
            }
        }

        /// <inheritdoc />
        protected override void OnInitializeTelemetry(HttpContext platformContext, RequestTelemetry requestTelemetry, ITelemetry telemetry)
        {
            if (telemetry == null)
            {
                throw new ArgumentNullException(nameof(telemetry));
            }

            InitializeTelemetryIfApplicationInsightsAvailabilityMonitoring(platformContext, telemetry);

            this.InitializeTelemetryIfSyntheticUserAgent(platformContext, telemetry);
        }

        private static void InitializeTelemetryIfApplicationInsightsAvailabilityMonitoring(HttpContext platformContext, ITelemetry telemetry)
        {
            if (string.IsNullOrEmpty(telemetry.Context.Operation.SyntheticSource))
            {
                if (platformContext == null)
                {
                    throw new ArgumentNullException(nameof(platformContext));
                }

                var runIdHeader = platformContext.Request?.Headers[SyntheticTestRunId];
                var locationHeader = platformContext.Request?.Headers[SyntheticTestLocation];

                if (!string.IsNullOrEmpty(runIdHeader) &&
                    !string.IsNullOrEmpty(locationHeader))
                {
                    telemetry.Context.Operation.SyntheticSource = SyntheticSourceHeaderValue;

                    telemetry.Context.User.Id = locationHeader + "_" + runIdHeader;
                    telemetry.Context.Session.Id = runIdHeader;
                }
            }
        }

        private void InitializeTelemetryIfSyntheticUserAgent(HttpContext platformContext, ITelemetry telemetry)
        {
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
                        foreach (var filterPattern in this.syntheticUserAgentFilterPatterns)
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
