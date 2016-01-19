namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Web;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Web.Implementation;

    /// <summary>
    /// Represents a telemetry processor for filtering out requests by user agent.
    /// </summary>
    public sealed class UserAgentTelemetryProcessor : ITelemetryProcessor
    {
        private readonly IList<FilterRequest> userAgentsToFilter = new List<FilterRequest>();

        /// <summary>
        /// Initializes a new instance of the <see cref="UserAgentTelemetryProcessor" /> class.
        /// </summary>
        /// <param name="next">Next TelemetryProcessor in call chain.</param>
        public UserAgentTelemetryProcessor(ITelemetryProcessor next)
        {
            if (next == null)
            {
                throw new ArgumentNullException("next");
            }

            this.Next = next;
        }

        /// <summary>
        /// Gets the list of UserAgent strings for which requests telemetry will not be collected
        /// if request was successful.
        /// </summary>
        public IList<FilterRequest> UserAgents
        {
            get
            {
                return this.userAgentsToFilter;
            }
        }

        /// <summary>
        /// Gets or sets the next TelemetryProcessor in call chain.
        /// </summary>
        private ITelemetryProcessor Next { get; set; }

        /// <summary>
        /// Processes telemetry item.
        /// </summary>
        /// <param name="item">Telemetry item to process.</param>
        public void Process(Channel.ITelemetry item)
        {
            if (HttpContext.Current != null)
            {
                var response = HttpContext.Current.GetResponse();
                if (response != null && response.StatusCode < 400)
                {
                    var request = HttpContext.Current.GetRequest();
                    if (request != null && this.userAgentsToFilter.Any(
                        t => t.Value.Equals(
                            request.UserAgent,
                            StringComparison.OrdinalIgnoreCase)))
                    {
                        WebEventSource.Log.WebRequestFilteredOutByUserAgent();
                        return;
                    }
                }
            }

            this.Next.Process(item);
        }
    }
}
