namespace Microsoft.ApplicationInsights.AspNetCore.Logging
{
    /// <summary>
    /// Options for configuration of <see cref="ApplicationInsightsLogger"/>.
    /// </summary>
    public class ApplicationInsightsLoggerOptions
    {
        /// <summary>
        /// Gets or sets value indicating, whether EventId and EventName properties should be included in telemetry.
        /// </summary>
        public bool IncludeEventId { get; set; }
    }
}
