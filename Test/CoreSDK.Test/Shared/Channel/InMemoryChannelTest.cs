namespace Microsoft.ApplicationInsights.Channel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.ApplicationInsights.TestFramework;

#if !NET40
    using System.Diagnostics.Tracing;
#endif

    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

#if NET40
    using Microsoft.Diagnostics.Tracing;
#endif

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Assert = Xunit.Assert;
    using DataContracts;

    [TestClass]
    public class InMemoryChannelTest
    {
        [TestMethod]
        public void WhenSendIsCalledTheEventIsBeingQueuedInTheBuffer()
        {
            var telemetryBuffer = new TelemetryBuffer();
            var channel = new InMemoryChannel(telemetryBuffer, new InMemoryTransmitter(telemetryBuffer));
            var sentTelemetry = new StubTelemetry();
            sentTelemetry.Context.InstrumentationKey = Guid.NewGuid().ToString();

            channel.Send(sentTelemetry);
            IEnumerable<ITelemetry> telemetries = telemetryBuffer.Dequeue();

            Assert.Equal(1, telemetries.Count());
            Assert.Same(sentTelemetry, telemetries.First());
        }

        [TestMethod]
        public void TelemetryWithNoInstrumentationKeyIsDropped()
        {
            var telemetryBuffer = new TelemetryBuffer();
            var channel = new InMemoryChannel(telemetryBuffer, new InMemoryTransmitter(telemetryBuffer));
            var sentTelemetry = new StubTelemetry();
            // No instrumentation key

            using (TestEventListener listener = new TestEventListener())
            {
                listener.EnableEvents(CoreEventSource.Log, EventLevel.Verbose);

                channel.Send(sentTelemetry);
                IEnumerable<ITelemetry> telemetries = telemetryBuffer.Dequeue();

                Assert.Null(telemetries);

                var expectedMessage = listener.Messages.First();
                Assert.Equal(35, expectedMessage.EventId);
            }
        }

#if !NETCOREAPP1_1

        [TestMethod]
        public void FlushCanBeAborted()
        {
            var telemetryBuffer = new TelemetryBuffer();
            var channel = new InMemoryChannel(telemetryBuffer, new InMemoryTransmitter(telemetryBuffer))
            {
                SendingInterval = TimeSpan.FromDays(1),
                EndpointAddress = "http://localhost/bad"
            };
            
            var telemetry = new TraceTelemetry("test");
            telemetry.Context.InstrumentationKey = Guid.NewGuid().ToString();
            channel.Send(telemetry); // Send telemetry so that it sets next send interval and does not interfere with Flush
            channel.Flush();

            telemetry = new TraceTelemetry("test");
            telemetry.Context.InstrumentationKey = Guid.NewGuid().ToString();
            channel.Send(telemetry);

            using (TestEventListener listener = new TestEventListener())
            {
                listener.EnableEvents(CoreEventSource.Log, EventLevel.Warning);
                channel.Flush(TimeSpan.FromSeconds(1));

                var expectedMessage = listener.Messages.First();
                Assert.Equal(24, expectedMessage.EventId);
            }
        }
#endif
    }
}
