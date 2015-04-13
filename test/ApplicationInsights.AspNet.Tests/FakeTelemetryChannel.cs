namespace Microsoft.ApplicationInsights.AspNet.Tests
{
    using System;
    using Microsoft.ApplicationInsights.Channel;

    internal class FakeTelemetryChannel : ITelemetryChannel
    {
        public bool DeveloperMode { get; set; }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Send(ITelemetry item)
        {
            throw new NotImplementedException();
        }
    }
}