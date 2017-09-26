namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System.Collections.Generic;

    /// <summary>
    /// Extension point for the HealthHeartbeat sent by the Application Insights SDK. Users can extend the heartbeat by adding their
    /// own properites to the payload, and indicating a healthy/unhealthy status for the properties added.
    /// </summary>
    public interface IHealthHeartbeatProperty
    {
        /// <summary>
        /// Gets a set of properties being added to the HealthHeartbeat payload.
        /// </summary>
        KeyValuePair<string, object>[] Properties { get; }

        /// <summary>
        /// Gets the number of properties that represent an unhealthy state.
        /// </summary>
        int UnhealthyCount { get; }
    }
}