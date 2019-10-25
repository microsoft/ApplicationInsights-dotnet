namespace Shared.Implementation
{
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// No-op telemetry module that is added instead of actual one, when the actual module is disabled.
    /// </summary>
    internal class NoOpTelemetryModule : ITelemetryModule
    {
        /// <summary>
        /// This is a no-op and will do nothing.
        /// </summary>
        /// <param name="configuration">This does nothing.</param>
        public void Initialize(TelemetryConfiguration configuration)
        {
        }
    }
}
