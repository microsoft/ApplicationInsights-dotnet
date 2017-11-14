
namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.Mocks
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    class HealthHeartbeatProviderMock : HeartbeatProvider
    {
        public List<MetricTelemetry> sentMessages = new List<MetricTelemetry>();

        private bool disableHeartbeatTimer;

        public HealthHeartbeatProviderMock(bool disableBaseHeartbeatTimer = true) : base()
        {
            this.disableHeartbeatTimer = disableBaseHeartbeatTimer;
        }

        public void SimulateSend()
        {
            this.Send();
        }

        public UInt64 SequenceCounter
        {
            get => this.heartbeatsSent;
            set => this.heartbeatsSent = value;
        }

        protected new void Send()
        {
            var heartbeat = this.GatherData();
            this.sentMessages.Add(heartbeat);
        }

        public override bool Initialize(TelemetryConfiguration configuration)
        {
            if (this.disableHeartbeatTimer)
            {
                this.HeartbeatTimer = new Timer(this.MockTimerCallback, this.sentMessages, Timeout.Infinite, Timeout.Infinite);
            }
            return base.Initialize(configuration);
        }

        private void MockTimerCallback(object state)
        {
            throw new NotSupportedException("Called MockTimerCallback on HealthHeartbeatProviderMOCK class - this isn't expected nor supported.");
        }

        public IDictionary<string,string> GetGatheredDataProperties()
        {
            var heartbeatData = base.GatherData();
            return heartbeatData.Properties;
        }
    }
}
