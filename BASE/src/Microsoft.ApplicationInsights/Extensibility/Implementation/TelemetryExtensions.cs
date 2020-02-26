namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using Microsoft.ApplicationInsights.Channel;

    /// <summary>
    /// Extensions for ITelemetry interface.
    /// </summary>
    public static class TelemetryExtensions
    {
        /// <summary>
        /// Sets envelope name for ITelemetry object.
        /// </summary>
        /// <param name="telemetry">ITelemetry object to set envelope name for.</param>
        /// <param name="envelopeName">Envelope name to use for ITelemetry object.</param>
        /// <exception cref="ArgumentException">Concrete implementation of ITelemetry object does not expose envelope name.</exception>
        public static void SetEnvelopeName(this ITelemetry telemetry, string envelopeName)
        {
            if (telemetry is IAiSerializableTelemetry aiSerializableTelemetry)
            {
                aiSerializableTelemetry.TelemetryName = envelopeName;
            }
            else
            {
                throw new ArgumentException("Provided telemetry object does not support envelope name.", nameof(telemetry));
            }
        }
    }
}
