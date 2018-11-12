namespace Microsoft.ApplicationInsights.Channel
{
    /// <summary>
    /// This interface defines Telemetry objects that are intended to be serialized for the Application Insights Breeze ingestion endpoint.
    /// </summary>
    internal interface IAiSerializableTelemetry
    {
        /// <summary>
        /// Gets the name of the Telemetry. Used internally for serialization.
        /// </summary>
        string TelemetryName { get; }

        /// <summary>
        /// Gets the name of the TelemetryType. Used internally for serialization.
        /// </summary>
        string BaseType { get; }
    }
}
