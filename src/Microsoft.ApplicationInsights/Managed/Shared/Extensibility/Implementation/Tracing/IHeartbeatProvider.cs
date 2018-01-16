namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Extensibility;

    internal interface IHeartbeatProvider : IDisposable
    {
        string InstrumentationKey { get; set; }

        bool IsHeartbeatEnabled { get; set; }

        TimeSpan HeartbeatInterval { get; set; }

        IList<string> ExcludedHeartbeatPropertyProviders { get; }

        IList<string> ExcludedHeartbeatProperties { get; }

        void Initialize(TelemetryConfiguration configuration);

        bool AddHeartbeatProperty(string propertyName, bool overrideDefaultField, string propertyValue, bool isHealthy);

        bool SetHeartbeatProperty(string propertyName, bool overrideDefaultField, string propertyValue = null, bool? isHealthy = null);
    }
}
