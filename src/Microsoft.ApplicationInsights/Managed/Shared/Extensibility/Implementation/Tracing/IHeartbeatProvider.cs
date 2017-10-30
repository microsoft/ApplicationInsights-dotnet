namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Extensibility;

    internal interface IHeartbeatProvider
    {
        bool AddHealthProperty(HealthHeartbeatProperty payloadItem);

        bool SetHealthProperty(HealthHeartbeatProperty payloadItem);

        bool Initialize(TelemetryConfiguration configuration, TimeSpan? heartbeatDelay = null, IEnumerable<string> disabledDefaultFields = null);
    }
}
