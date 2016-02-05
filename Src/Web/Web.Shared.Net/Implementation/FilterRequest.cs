namespace Microsoft.ApplicationInsights.Web.Implementation
{
    using System.Collections.Generic;

    /// <summary>
    /// Allows configuration of specific Request Handlers to filter telemetry from using the <see cref="RequestTrackingTelemetryModule"/>.
    /// </summary>
    public sealed class FilterRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FilterRequest"/> class.
        /// </summary>
        public FilterRequest()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterRequest"/> class.
        /// </summary>
        /// <param name="value">Request handler to apply the filter to.</param>
        public FilterRequest(string value)
        {
            this.Value = value;
        }

        /// <summary>
        /// Gets or sets the request handler to apply the filter to.
        /// </summary>
        public string Value { get; set; }
    }
}
