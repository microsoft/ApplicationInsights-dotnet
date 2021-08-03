using System;

using Microsoft.ApplicationInsights.Channel;

namespace IntegrationTests.Tests.TestFramework
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
        }

        public void Flush()
        {
        }

        public void Send(ITelemetry item)
        {
            this.OnSend(item);
        }
    }
}