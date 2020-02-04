namespace Microsoft.ApplicationInsights.Extensibility.HostingStartup
{
    using System;

    /// <summary>
    /// Diagnostics telemetry module for azure web sites.
    /// </summary>
    [Obsolete("Please use Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.FileDiagnosticsTelemetryModule")]
    public class FileDiagnosticsTelemetryModule : Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.FileDiagnosticsTelemetryModule
    {
    }
}
