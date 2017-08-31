namespace FunctionalTestUtils
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Channel;

    public class BackTelemetryChannel : ITelemetryChannel
    {
        private IList<ITelemetry> buffer;

        public BackTelemetryChannel()
        {
            this.buffer = new List<ITelemetry>();
        }

        public IList<ITelemetry> Buffer
        {
            get
            {
                return this.buffer;
            }
        }

        public bool? DeveloperMode
        {
            get
            {
                return true;
            }
            set
            {
            }
        }

        public string EndpointAddress
        {
            get
            {
                return "https://dc.services.visualstudio.com/v2/track";
            }

            set
            {
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
            this.buffer.Add(item);
        }
    }
}