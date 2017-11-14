namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Extensibility;

    internal interface IHeartbeatProvider
    {
        string InstrumentationKey { get; set; }

        bool IsEnabled { get; set; }

        TimeSpan Interval { get; set; }

        IEnumerable<string> DisabledDefaultFields { get; set; }

        bool AddHealthProperty(string name, string value, bool isHealthy);

        bool SetHealthProperty(string name, string value = null, bool? isHealthy = null);

        bool RemoveHealthProperty(string payloadItemName);

        bool Initialize(TelemetryConfiguration configuration);
    }
}
