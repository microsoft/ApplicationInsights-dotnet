namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.Mocks
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    class HealthHeartbeatPayloadMock : IHealthHeartbeatPayloadExtension
    {
        public Stack<KeyValuePair<string, object>> customProperties = new Stack<KeyValuePair<string, object>>();
        public int currentUnhealthyCount = 0;

        public IEnumerable<KeyValuePair<string, object>> GetPayloadProperties()
        {
            return this.customProperties.ToArray();
        }

        public int CurrentUnhealthyCount => this.GetUnhealthyCountAndReset();

        public string Name => "TestHeartbeatPayload";

        private int GetUnhealthyCountAndReset()
        {
            int unhealthyCountThisTime = this.currentUnhealthyCount;
            this.currentUnhealthyCount = 0;
            return unhealthyCountThisTime;
        }
    }
}
