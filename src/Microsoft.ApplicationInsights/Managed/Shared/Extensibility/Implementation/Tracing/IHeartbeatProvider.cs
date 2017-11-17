namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Extensibility;

    internal interface IHeartbeatProvider : IDisposable
    {
        string InstrumentationKey { get; set; }

        bool IsEnabled { get; set; }

        TimeSpan Interval { get; set; }

        IList<string> DisabledDefaultFields { get; }

        bool AddHealthProperty(string name, string value, bool isHealthy);

        bool SetHealthProperty(string name, string value = null, bool? isHealthy = null);

        void Initialize(TelemetryConfiguration configuration);
    }
}
