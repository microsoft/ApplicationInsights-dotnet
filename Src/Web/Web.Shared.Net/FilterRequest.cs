namespace Microsoft.ApplicationInsights.Web
{
    using System.Collections.Generic;

    /// <summary>
    /// Allows configuration of specific Request Handlers to filter telemetry from using the <see cref="HandlerTelemetryProcessor"/>.
    /// </summary>
    public sealed class FilterRequest
    {
        private IList<RequestNotificationFilter> requestNotifications = new List<RequestNotificationFilter>();

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

        /// <summary>
        /// Gets the <see cref="System.Web.RequestNotification"/> values. 
        /// If specified, the request is only filtered out if the Request Handler and the <see cref="System.Web.HttpContext.CurrentNotification" /> property matches.
        /// </summary>
        public IList<RequestNotificationFilter> RequestNotifications
        {
            get
            {
                return this.requestNotifications;
            }
        }
    }
}
