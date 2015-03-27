
namespace FunctionalTestUtils
{
    using Microsoft.ApplicationInsights.Channel;
    using System.Collections.Generic;

    internal class BackTelemetryChannel : ITelemetryChannel
    {
        IList<ITelemetry> buffer;

        public BackTelemetryChannel(IList<ITelemetry> buffer)
        {
            this.buffer = buffer;
        }

        public bool DeveloperMode { get; set; }

        public void Dispose()
        {
        }

        public void Send(ITelemetry item)
        {
            this.buffer.Add(item);
        }
    }
}