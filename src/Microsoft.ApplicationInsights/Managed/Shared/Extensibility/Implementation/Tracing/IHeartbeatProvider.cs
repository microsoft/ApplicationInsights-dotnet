namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Extensibility;

    internal interface IHeartbeatProvider : IHeartbeatPropertyManager, IDisposable
    {
        string InstrumentationKey { get; set; }

        void Initialize(TelemetryConfiguration configuration);
    }
}
