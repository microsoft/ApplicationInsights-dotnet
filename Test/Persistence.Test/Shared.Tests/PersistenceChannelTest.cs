namespace Microsoft.ApplicationInsights.Channel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;    
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Platform;
    using Microsoft.ApplicationInsights.TestFramework;    
#if WINDOWS_PHONE || WINDOWS_PHONE_APP || WINDOWS_STORE
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
    
#if WINRT
    using TaskEx = System.Threading.Tasks.Task;
#endif

    public class PersistenceChannelTest : AsyncTest
    {
        private readonly TelemetryConfiguration configuration;

        public PersistenceChannelTest()
        {
            this.configuration = new TelemetryConfiguration();            
        }

        protected override void Dispose(bool disposing)
        {
            this.configuration.Dispose();            
            PlatformSingleton.Current = null;
        }

        [TestClass]
        public class DeveloperMode : PersistenceChannelTest
        {
            [TestMethod]
            public void DeveloperModeIsFalseByDefault()
            {
                var channel = new PersistenceChannel();
                Assert.IsFalse(channel.DeveloperMode.Value);
            }

            [TestMethod]
            public void DeveloperModeCanBeModifiedByConfiguration()
            {
                var channel = new PersistenceChannel();
                channel.DeveloperMode = true;
                Assert.IsTrue(channel.DeveloperMode.Value);
            }

            [TestMethod]
            public void WhenSetToTrueChangesTelemetryBufferCapacityToOneForImmediateTransmission()
            {
                var channel = new PersistenceChannel();
                channel.DeveloperMode = true;
                Assert.AreEqual(1, channel.TelemetryBuffer.Capacity);
            }

            [TestMethod]
            public void WhenSetToFalseChangesTelemetryBufferCapacityToOriginalValueForBufferedTransmission()
            {
                var channel = new PersistenceChannel();
                int originalTelemetryBufferSize = channel.TelemetryBuffer.Capacity;

                channel.DeveloperMode = true;
                channel.DeveloperMode = false;

                Assert.AreEqual(originalTelemetryBufferSize, channel.TelemetryBuffer.Capacity);
            }
        }

        [TestClass]
        public class EndpointAddress : PersistenceChannelTest
        {
            [TestMethod]
            public void EndpointAddressCanBeModifiedByConfiguration()
            {
                var channel = new PersistenceChannel();
               
                Uri expectedEndpoint = new Uri("http://abc.com");
                channel.EndpointAddress = expectedEndpoint.AbsoluteUri;

                Assert.AreEqual(expectedEndpoint, new Uri(channel.EndpointAddress));
            }
        }

        [TestClass]
        public class MaxTelemetryBufferCapacity : PersistenceChannelTest
        {
            [TestMethod]
            public void GetterReturnsTelemetryBufferCapacity()
            {
                var channel = new PersistenceChannel();
                channel.TelemetryBuffer.Capacity = 42;
                Assert.AreEqual(42, channel.MaxTelemetryBufferCapacity);
            }

            [TestMethod]
            public void SetterChangesTelemetryBufferCapacity()
            {
                var channel = new PersistenceChannel();
                channel.MaxTelemetryBufferCapacity = 42;
                Assert.AreEqual(42, channel.TelemetryBuffer.Capacity);
            }
        }

        [TestClass]
        public class Send : PersistenceChannelTest
        {
            [TestMethod]
            public void PassesTelemetryToMemoryBufferChannel()
            {
                var channel = new PersistenceChannel();

                var telemetry = new StubTelemetry();
                channel.Send(telemetry);

                IEnumerable<ITelemetry> actual = channel.TelemetryBuffer.Dequeue();
                Assert.AreEqual(telemetry, actual.First());
            }
        }
    }
}
