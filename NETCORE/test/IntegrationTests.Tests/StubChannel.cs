using Microsoft.ApplicationInsights.Channel;
using System;

namespace IntegrationTests.Tests
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
#if NETCOREAPP3_1
            throw new NotImplementedException();
#endif
        }

        public void Send(ITelemetry item)
        {
            this.OnSend(item);
        }
    }
}