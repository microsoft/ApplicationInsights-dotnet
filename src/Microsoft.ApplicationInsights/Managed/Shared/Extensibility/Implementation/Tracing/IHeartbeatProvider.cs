namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Extensibility;

    internal interface IHeartbeatProvider
    {
        bool RegisterHeartbeatPayload(IHealthHeartbeatPayloadExtension payloadProvider);

        bool Initialize(TelemetryConfiguration configuration, TimeSpan? heartbeatDelay = null, IEnumerable<string> disabledDefaultFields = null);
    }
}
