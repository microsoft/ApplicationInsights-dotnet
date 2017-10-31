namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Extensibility;

    internal interface IHeartbeatProvider
    {
        string DiagnosticsInstrumentationKey { set; }

        bool AddHealthProperty(HealthHeartbeatProperty payloadItem);

        bool SetHealthProperty(HealthHeartbeatProperty payloadItem);

        bool RemoveHealthProperty(string payloadItemName);

        bool Initialize(TelemetryConfiguration configuration, string instrumentationKey, TimeSpan? heartbeatDelay = null, IEnumerable<string> disabledDefaultFields = null);
    }
}
