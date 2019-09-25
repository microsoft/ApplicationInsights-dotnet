using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Implementation
{
    /// <summary>
    /// No-op telemetry module that is added instead of actual one, when the actual module is disabled
    /// </summary>
    internal class NoOpTelemetryModule : ITelemetryModule
    {
        public void Initialize(TelemetryConfiguration configuration)
        {
        }
    }
}
