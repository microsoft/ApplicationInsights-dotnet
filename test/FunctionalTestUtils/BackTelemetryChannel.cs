
namespace FunctionalTestUtils
{
    using Microsoft.ApplicationInsights.Channel;
    using System.Collections.Generic;

    internal class BackTelemetryChannel : ITelemetryChannel
    {
        internal IList<ITelemetry> buffer;

        public BackTelemetryChannel()
        {
        }

        public bool DeveloperMode { get; set; }

        public void Dispose()
        {
        }

        public void Send(ITelemetry item)
        {
            if (this.buffer != null)
            {
                this.buffer.Add(item);
            }
        }
    }
}