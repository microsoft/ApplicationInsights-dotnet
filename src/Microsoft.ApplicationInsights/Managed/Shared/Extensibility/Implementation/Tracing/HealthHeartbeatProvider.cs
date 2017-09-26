namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// Implementation of health heartbeat functionality.
    /// </summary>
    internal class HealthHeartbeatProvider : IHeartbeatProvider
    {
        private TelemetryConfiguration configuration;

        public HealthHeartbeatProvider()
        {
        }

        public bool Initialize(TelemetryConfiguration config)
        {
            this.configuration = config;
            return true;
        }

        public void RegisterHeartbeatPayload(IHealthHeartbeatProperty payloadProvider)
        {
            throw new NotImplementedException();
        }

        public bool UpdateSettings(TelemetryConfiguration config)
        {
            this.configuration = config;
            return true;
        }
    }
}
