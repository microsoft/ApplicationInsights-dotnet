using Microsoft.ApplicationInsights.Channel;
using System;

namespace TestApp30.Tests
{
    internal class StubChannel : ITelemetryChannel
    {
        public Action<ITelemetry> OnSend = t => { };

        public string EndpointAddress
        {
            get;
            set;
        }

        public bool? DeveloperMode { get; set; }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Flush()
        {
            throw new NotImplementedException();
        }

        public void Send(ITelemetry item)
        {
            this.OnSend(item);
        }
    }
}