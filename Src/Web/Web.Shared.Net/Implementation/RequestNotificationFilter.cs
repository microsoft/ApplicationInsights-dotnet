namespace Microsoft.ApplicationInsights.Web.Implementation
{
    /// <summary>
    /// Allows configuration of specific <see cref="System.Web.RequestNotification"/> values in the <see cref="System.Web.HttpContext.CurrentNotification"/> property for <see cref="HandlerTelemetryProcessor"/>. 
    /// </summary>
    public sealed class RequestNotificationFilter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestNotificationFilter"/> class.
        /// </summary>
        public RequestNotificationFilter()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestNotificationFilter"/> class.
        /// </summary>
        /// <param name="value">The <see cref="System.Web.RequestNotification"/> value to filter on.</param>
        public RequestNotificationFilter(string value)
        {
            this.Value = value;
        }

        /// <summary>
        /// Gets or sets the <see cref="System.Web.RequestNotification"/> value to filter on.
        /// </summary>
        public string Value { get; set; }
    }
}
