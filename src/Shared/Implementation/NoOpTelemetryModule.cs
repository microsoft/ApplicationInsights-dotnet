namespace Shared.Implementation
{
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// No-op telemetry module that is added instead of actual one, when the actual module is disabled.
    /// </summary>
    internal class NoOpTelemetryModule : ITelemetryModule
    {
        public void Initialize(TelemetryConfiguration configuration)
        {
        }
    }
}
