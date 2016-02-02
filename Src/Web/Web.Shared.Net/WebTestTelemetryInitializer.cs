namespace Microsoft.ApplicationInsights.Web
{
    using System.Collections.Generic;
    using System.Web;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Web.Implementation;
    
    /// <summary>
    /// A telemetry initializer that will update the User, Session and Operation contexts if request originates from a web test.
    /// </summary>
    public class WebTestTelemetryInitializer : WebTelemetryInitializerBase
    {
        private readonly IList<WebTestHeaderFilter> filters = new List<WebTestHeaderFilter>();

        /// <summary>
        /// Initializes a new instance of the <see cref="WebTestTelemetryInitializer"/> class.
        /// </summary>
        public WebTestTelemetryInitializer()
        {
        }

        /// <summary>
        /// Gets the configured headers for recognizing and setting telemetry context for requests originating from web tests.
        /// </summary>
        public IList<WebTestHeaderFilter> Filters
        {
            get
            {
                return this.filters;
            }
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
            if (platformContext != null)
            {
                var request = platformContext.GetRequest();
                if (request != null)
                {
                    foreach (var filter in this.filters)
                    {
                        var filterHeader = request.UnvalidatedGetHeader(filter.FilterHeader);
                        if (!string.IsNullOrEmpty(filterHeader))
                        {
                            if (string.IsNullOrEmpty(telemetry.Context.Operation.SyntheticSource))
                            {
                                telemetry.Context.Operation.SyntheticSource = !string.IsNullOrEmpty(filter.SourceName) ? filter.SourceName : filter.FilterHeader;
                            }

                            if (string.IsNullOrEmpty(telemetry.Context.User.Id))
                            {
                                telemetry.Context.User.Id = request.UnvalidatedGetHeader(filter.UserIdHeader);
                            }

                            if (string.IsNullOrEmpty(telemetry.Context.Session.Id))
                            {
                                telemetry.Context.Session.Id = !string.IsNullOrEmpty(filter.SessionIdHeader) ? 
                                    request.UnvalidatedGetHeader(filter.SessionIdHeader) :
                                    filterHeader;
                            }
                        }
                    }
                }
            }
        }
    }
}