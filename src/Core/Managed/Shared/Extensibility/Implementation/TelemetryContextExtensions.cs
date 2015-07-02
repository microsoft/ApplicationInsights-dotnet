namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using Microsoft.ApplicationInsights.DataContracts;

    /// <summary>
    /// Extension methods for TelemetryContext.
    /// </summary>
    public static class TelemetryContextExtensions
    {
        /// <summary>
        /// Returns TelemetryContext's Internal context.
        /// </summary>
        /// <param name="context">Telemetry context to get Internal context for.</param>
        /// <returns>Internal context for TelemetryContext.</returns>
        public static InternalContext GetInternalContext(this TelemetryContext context)
        {
            return context.Internal;
        }
    }
}
