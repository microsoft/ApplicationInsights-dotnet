
namespace FunctionalTestUtils
{
    using Microsoft.ApplicationInsights.Channel;
    using System.Collections.Generic;
    using System;

    internal class BackTelemetryChannel : ITelemetryChannel
    {
        internal IList<ITelemetry> buffer;

        public BackTelemetryChannel()
        {
        }

        public bool DeveloperMode { get; set; }

        public string EndpointAddress
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public void Dispose()
        {
        }

        public void Flush()
        {
            throw new NotImplementedException();
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