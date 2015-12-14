namespace Microsoft.ApplicationInsights.Channel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Assert = Xunit.Assert;
    
    [TestClass]
    public class InMemoryChannelTest
    {
        [TestMethod]
        public void WhenSendIsCalledTheEventIsBeingQueuedInTheBuffer()
        {
            var telemetryBuffer = new TelemetryBuffer();
            var channel = new InMemoryChannel(telemetryBuffer, new InMemoryTransmitter(telemetryBuffer));
            var sentTelemetry = new StubTelemetry();

            channel.Send(sentTelemetry);
            IEnumerable<ITelemetry> telemetries = telemetryBuffer.Dequeue();

            Assert.Equal(1, telemetries.Count());
            Assert.Same(sentTelemetry, telemetries.First());
        }
    }
}
