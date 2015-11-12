using System.Text;
using Microsoft.ApplicationInsights.DataContracts;

namespace Microsoft.ApplicationInsights.Channel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.ApplicationInsights.TestFramework;
#if WINDOWS_PHONE || WINDOWS_STORE
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
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

        [TestMethod]
        public void SendSanitizesTelemetryItem()
        {
            string name = new string('Z', 10000);

            EventTelemetry t = new EventTelemetry(name);

            new InMemoryChannel().Send(t);

            Assert.Equal(512, t.Name.Length);
        }
    }
}
