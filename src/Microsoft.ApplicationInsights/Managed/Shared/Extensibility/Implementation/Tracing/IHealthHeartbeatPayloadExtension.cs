namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System.Collections.Generic;

    /// <summary>
    /// Extension point for the HealthHeartbeat sent by the Application Insights SDK. Users can extend the heartbeat by adding their
    /// own properites to the payload, and indicating a healthy/unhealthy status for the properties added.
    /// </summary>
    public interface IHealthHeartbeatPayloadExtension
    {
        /// <summary>
        /// Gets the number of properties that represent an unhealthy state.
        /// </summary>
        int CurrentUnhealthyCount { get; }

        /// <summary>
        /// Gets the name of the payload extension provider. A good choice would be the name of the implementing class, keeping in mind that shorter names are better, space-efficiency wise.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets a set of properties to append to the HealthHeartbeat payload.
        /// </summary>
        IEnumerable<KeyValuePair<string, object>> GetPayloadProperties();
    }
}