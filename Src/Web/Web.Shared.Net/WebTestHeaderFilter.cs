namespace Microsoft.ApplicationInsights.Web
{
    /// <summary>
    /// Allow configuration of header filters and telemetry context for requests originating from web tests.
    /// </summary>
    public class WebTestHeaderFilter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebTestHeaderFilter"/> class.
        /// </summary>
        public WebTestHeaderFilter()
        {
        }

        /// <summary>
        /// Gets or sets the request header used to determine if the request originates from a web test.
        /// </summary>
        public string FilterHeader { get; set; }

        /// <summary>
        /// Gets or sets the readable name for the traffic source. If not provided, defaults to the filter header name.
        /// </summary>
        public string SourceName { get; set; }

        /// <summary>
        /// Gets or sets the request header used to set the User Id for the telemetry context if the request originates from a web test.
        /// </summary>
        public string UserIdHeader { get; set; }

        /// <summary>
        /// Gets or sets the request header used to set the Session Id for the telemetry context if the request originates from a web test.
        /// If empty, defaults to the value of <see cref="FilterHeader"/> in the request.
        /// </summary>
        public string SessionIdHeader { get; set; }
    }
}
