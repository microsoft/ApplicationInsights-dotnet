namespace Microsoft.ApplicationInsights.Channel
{
    using System;
    using Microsoft.ApplicationInsights.DataContracts;

    /// <summary>
    /// The base telemetry type for application insights.
    /// </summary>
    public interface ITelemetry
    {
        /// <summary>
        /// Gets or sets date and time when telemetry was recorded.
        /// </summary>
        DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Gets the context associated with this telemetry instance.
        /// </summary>
        TelemetryContext Context { get; }
    }
}
