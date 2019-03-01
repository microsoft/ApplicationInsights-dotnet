namespace Microsoft.ApplicationInsights.Extensibility.W3C
{
    using System;
    using System.ComponentModel;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// Telemetry Initializer that sets correlation ids for W3C.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("123")]
    public class W3COperationCorrelationTelemetryInitializer : ITelemetryInitializer
    {
        /// <summary>
        /// Initializes telemetry item.
        /// </summary>
        /// <param name="telemetry">Telemetry item.</param>
        public void Initialize(ITelemetry telemetry)
        {
        }
    }
}
