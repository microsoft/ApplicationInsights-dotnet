namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Helpers
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation;

    internal class StubTelemetrySerializer : TelemetrySerializer
    {
        public Action<IEnumerable<ITelemetry>> OnSerialize = _ => { };

        public override void Serialize(IEnumerable<ITelemetry> items)
        {
            this.OnSerialize(items);
        }
    }
}
