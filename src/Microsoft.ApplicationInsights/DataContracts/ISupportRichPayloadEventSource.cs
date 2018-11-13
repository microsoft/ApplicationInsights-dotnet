namespace Microsoft.ApplicationInsights.DataContracts
{
    using System.Diagnostics.Tracing;

    /// <summary>
    /// Represents objects that support the RichPayloadEvountSource class. Will provide information to WriteEvent.
    /// </summary>
    internal interface ISupportRichPayloadEventSource
    {
        /// <summary>
        /// Gets the keyword to be used to determine if the event source is enabled for this telemetry type.
        /// </summary>
        EventKeywords EventSourceKeyword { get; }

        /// <summary>
        /// Gets the Data to be written to an event.
        /// </summary>
        object Data { get; }

        /// <summary>
        /// Gets the Telemetry name to be written to the event.
        /// </summary>
        string TelemetryName { get;  }
    }
}
