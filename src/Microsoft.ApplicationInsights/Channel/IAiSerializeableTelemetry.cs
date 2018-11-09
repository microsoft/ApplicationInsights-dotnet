namespace Microsoft.ApplicationInsights.Channel
{
    using System.Collections.Generic;

    public interface IAiSerializeableTelemetry
    {
        /// <summary>
        /// Gets the name of the Telemetry. Used internally for serialization.
        /// </summary>
        string TelemetryName { get; }

        /// <summary>
        /// Gets the name of the TelemetryType. Used internally for serialization.
        /// </summary>
        string BaseType { get; }

        /// <summary>
        /// Gets the internal collection of properties. Used internally for serialization.
        /// </summary>
        /// <returns></returns>
        IDictionary<string, string> GetInternalDataProperties();

    }
}
