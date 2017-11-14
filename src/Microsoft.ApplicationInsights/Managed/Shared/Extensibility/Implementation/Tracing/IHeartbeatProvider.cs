namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Extensibility;

    internal interface IHeartbeatProvider
    {
        string DiagnosticsInstrumentationKey { set; }

        bool IsEnabled { get; set; }

        TimeSpan Interval { get; set; }

        bool AddHealthProperty(string name, string value, bool isHealthy);

        bool SetHealthProperty(string name, string value = null, bool? isHealthy = null);

        bool RemoveHealthProperty(string payloadItemName);

        bool Initialize(TelemetryConfiguration configuration, string instrumentationKey, TimeSpan? heartbeatDelay = null, IEnumerable<string> disabledDefaultFields = null, bool isEnabled = true);
    }
}
