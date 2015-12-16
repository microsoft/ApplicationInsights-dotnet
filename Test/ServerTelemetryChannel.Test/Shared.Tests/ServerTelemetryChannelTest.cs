namespace Microsoft.ApplicationInsights.WindowsServer.Channel
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Web.TestFramework;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Helpers;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Assert = Xunit.Assert;
    
#if NET45
    using TaskEx = System.Threading.Tasks.Task;
#endif

    public class ServerTelemetryChannelTest : IDisposable
    {
        private readonly TelemetryConfiguration configuration;

        public ServerTelemetryChannelTest()
        {
            this.configuration = new TelemetryConfiguration();
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            this.configuration.Dispose();            
        }

        [TestClass]
        public class Constructor : ServerTelemetryChannelTest
        {
            [TestMethod]
            public void InitializesTransmitterWithNetworkAvailabilityPolicy()
            {
                var network = new StubNetwork { OnIsAvailable = () => false };

                var channel = new ServerTelemetryChannel(network, new StubApplicationLifecycle());
                channel.Initialize(new TelemetryConfiguration());
                Thread.Sleep(50);

                Assert.Equal(0, channel.Transmitter.Sender.Capacity);
            }
        }

        [TestClass]
        public class DeveloperMode : ServerTelemetryChannelTest
        {
            [TestMethod]
            public void DeveloperModeIsNullByDefault()
            {
                var channel = new ServerTelemetryChannel();
                Assert.Null(channel.DeveloperMode);
            }

            [TestMethod]
            public void DeveloperModeCanBeModifiedByConfiguration()
            {
                var channel = new ServerTelemetryChannel();             
                channel.DeveloperMode = true;
                Assert.True(channel.DeveloperMode.Value);
            }

            [TestMethod]
            public void WhenSetToTrueChangesTelemetryBufferCapacityToOneForImmediateTransmission()
            {
                var channel = new ServerTelemetryChannel();
                channel.DeveloperMode = true;
                Assert.Equal(1, channel.TelemetryBuffer.Capacity);
            }

            [TestMethod]
            public void WhenSetToFalseChangesTelemetryBufferCapacityToOriginalValueForBufferedTransmission()
            {
                var channel = new ServerTelemetryChannel();
                int originalTelemetryBufferSize = channel.TelemetryBuffer.Capacity;

                channel.DeveloperMode = true;
                channel.DeveloperMode = false;

                Assert.Equal(originalTelemetryBufferSize, channel.TelemetryBuffer.Capacity);
            }

            [TestMethod]
            public void DoesNotModifyComponentConfigurationWhenNewValueIsSameAsOldValue()
            {
                var channel = new ServerTelemetryChannel();
                int oldTelemetryBufferSize = channel.TelemetryBuffer.Capacity;

                channel.DeveloperMode = false;

                Assert.Equal(oldTelemetryBufferSize, channel.TelemetryBuffer.Capacity);
            }
        }

        [TestClass]
        public class EndpointAddress : ServerTelemetryChannelTest
        {
            [TestMethod]
            public void EndpointAddressCanBeModifiedByConfiguration()
            {
                var channel = new ServerTelemetryChannel();
               
                Uri expectedEndpoint = new Uri("http://abc.com");
                channel.EndpointAddress = expectedEndpoint.AbsoluteUri;

                Assert.Equal(expectedEndpoint, new Uri(channel.EndpointAddress));
            }

            [TestMethod]
            public void EndpointAddressIsStoredBySerializer()
            {
                var channel = new ServerTelemetryChannel();
                
                Uri expectedEndpoint = new Uri("http://abc.com");
                channel.EndpointAddress = expectedEndpoint.AbsoluteUri;
                
                Assert.Equal(expectedEndpoint, channel.TelemetrySerializer.EndpointAddress);
            }
        }

        [TestClass]
        public class MaxTelemetryBufferDelay : ServerTelemetryChannelTest
        {
            [TestMethod]
            public void DataUploadIntervalInSecondsIsStoredByTelemetryBuffer()
            {
                var channel = new ServerTelemetryChannel();
               
                TimeSpan expectedUploadInterval = TimeSpan.FromSeconds(42);
                channel.MaxTelemetryBufferDelay = expectedUploadInterval;

                Assert.Equal(expectedUploadInterval, channel.MaxTelemetryBufferDelay);
            }
        }

        [TestClass]
        public class MaxTelemetryBufferCapacity : ServerTelemetryChannelTest
        {
            [TestMethod]
            public void GetterReturnsTelemetryBufferCapacity()
            {
                var channel = new ServerTelemetryChannel();
                channel.TelemetryBuffer.Capacity = 42;
                Assert.Equal(42, channel.MaxTelemetryBufferCapacity);
            }

            [TestMethod]
            public void SetterChangesTelemetryBufferCapacity()
            {
                var channel = new ServerTelemetryChannel();
                channel.MaxTelemetryBufferCapacity = 42;
                Assert.Equal(42, channel.TelemetryBuffer.Capacity);
            }
        }

        [TestClass]
        public class MaxTransmissionBufferCapacity : ServerTelemetryChannelTest
        {
            [TestMethod]
            public void ReturnsMaxBufferCapacityOfTransmitter()
            {
                var channel = new ServerTelemetryChannel { Transmitter = new StubTransmitter() };
                channel.Transmitter.MaxBufferCapacity = 42;
                Assert.Equal(42, channel.MaxTransmissionBufferCapacity);
            }

            [TestMethod]
            public void ChangesMaxBufferCapacityOfTransmitter()
            {
                var channel = new ServerTelemetryChannel { Transmitter = new StubTransmitter() };
                channel.MaxTransmissionBufferCapacity = 42;
                Assert.Equal(42, channel.Transmitter.MaxBufferCapacity);
            }
        }

        [TestClass]
        public class MaxTransmissionSenderCapacity : ServerTelemetryChannelTest
        {
            [TestMethod]
            public void ReturnsMaxSenderCapacityOfTransmitter()
            {
                var channel = new ServerTelemetryChannel { Transmitter = new StubTransmitter() };
                channel.Transmitter.MaxSenderCapacity = 42;
                Assert.Equal(42, channel.MaxTransmissionSenderCapacity);
            }

            [TestMethod]
            public void ChangesMaxSenderCapacityOfTransmitter()
            {
                var channel = new ServerTelemetryChannel { Transmitter = new StubTransmitter() };
                channel.MaxTransmissionSenderCapacity = 42;
                Assert.Equal(42, channel.Transmitter.MaxSenderCapacity);
            }
        }

        [TestClass]
        public class MaxTransmissionStorageCapacity : ServerTelemetryChannelTest
        {
            [TestMethod]
            public void GetterReturnsMaxStorageCapacityOfTransmitter()
            {
                var channel = new ServerTelemetryChannel { Transmitter = new StubTransmitter() };
                channel.Transmitter.MaxStorageCapacity = 42000;
                Assert.Equal(42000, channel.MaxTransmissionStorageCapacity);
            }

            [TestMethod]
            public void SetterChangesStorageCapacityOfTransmitter()
            {
                var channel = new ServerTelemetryChannel { Transmitter = new StubTransmitter() };
                channel.MaxTransmissionStorageCapacity = 42000;
                Assert.Equal(42000, channel.Transmitter.MaxStorageCapacity);
            }
        }

        [TestClass]
        public class StorageFolder : ServerTelemetryChannelTest
        {
            [TestMethod]
            public void GetterReturnsStorageFolderOfTransmitter()
            {
                var channel = new ServerTelemetryChannel { Transmitter = new StubTransmitter() };
                channel.Transmitter.StorageFolder = "test";
                Assert.Equal("test", channel.StorageFolder);
            }

            [TestMethod]
            public void SetterChangesStorageFolderOfTransmitter()
            {
                var channel = new ServerTelemetryChannel { Transmitter = new StubTransmitter() };
                channel.StorageFolder = "test";
                Assert.Equal("test", channel.Transmitter.StorageFolder);
            }
        }

        [TestClass]
        public class Flush : ServerTelemetryChannelTest
        {
            [TestMethod]
            public void FlushesTelemetryBuffer()
            {
                var mockTelemetryBuffer = new Mock<TelemetryBuffer>();
                var channel = new ServerTelemetryChannel { TelemetryBuffer = mockTelemetryBuffer.Object };
                channel.Initialize(TelemetryConfiguration.CreateDefault());

                channel.Flush();

                mockTelemetryBuffer.Verify(x => x.FlushAsync());
            }

            [TestMethod]
            public void WaitsForAsynchronousFlushToCompleteAndAllowsItsExceptionsToBubbleUp()
            {
                var expectedException = new Exception();
                var tcs = new TaskCompletionSource<object>();
                tcs.SetException(expectedException);
                var mockTelemetryBuffer = new Mock<TelemetryBuffer>();
                mockTelemetryBuffer.Setup(x => x.FlushAsync()).Returns(tcs.Task);
                var channel = new ServerTelemetryChannel { TelemetryBuffer = mockTelemetryBuffer.Object };
                channel.Initialize(TelemetryConfiguration.CreateDefault());

                var actualException = Assert.Throws<Exception>(() => channel.Flush());

                Assert.Same(expectedException, actualException);
            }

            [TestMethod]
            public void FlushesThrowsIfInitializeWasNotCalled()
            {
                var channel = new ServerTelemetryChannel();
                
                Assert.Throws<InvalidOperationException>(() => channel.Flush());
            }
        }

        [TestClass]
        public class Initialize : ServerTelemetryChannelTest
        {
            [TestMethod]
            public void AppliesTransmissionPoliciesToBeginSendingStoredTelemetry()
            {
                var transmissionPoliciesApplied = new ManualResetEvent(false);
                var transmitter = new StubTransmitter();
                transmitter.OnApplyPolicies = () =>
                {
                    transmissionPoliciesApplied.Set();
                };
                var channel = new ServerTelemetryChannel { Transmitter = transmitter };
                channel.Initialize(TelemetryConfiguration.CreateDefault());

                var initializedConfiguration = new TelemetryConfiguration();
                channel.Initialize(initializedConfiguration);

                Assert.True(transmissionPoliciesApplied.WaitOne(1000));
            }

            [TestMethod]
            public void InitializeCallsTransmitterInitialize()
            {
                var transmitterInitialized = new ManualResetEvent(false);
                var transmitter = new StubTransmitter();
                transmitter.OnInitialize = () =>
                {
                    transmitterInitialized.Set();
                };
                var channel = new ServerTelemetryChannel { Transmitter = transmitter };

                var initializedConfiguration = new TelemetryConfiguration();
                channel.Initialize(initializedConfiguration);

                Assert.True(transmitterInitialized.WaitOne(1000));
            }
        }

        [TestClass]
        public class Send : ServerTelemetryChannelTest
        {
            [TestMethod]
            public void PassesTelemetryToTelemetryProcessor()
            {
                ITelemetry sentTelemetry = null;
                var channel = new ServerTelemetryChannel();
                channel.Initialize(TelemetryConfiguration.CreateDefault());
                channel.TelemetryProcessor = new StubTelemetryProcessor(null) { OnProcess = (t) => sentTelemetry = t };

                var telemetry = new StubTelemetry();
                channel.Send(telemetry);

                Assert.Equal(telemetry, sentTelemetry);
            }

            [TestMethod]
            public void ThrowsExceptionIfInitializeWasNotCalled()
            {
                ITelemetry sentTelemetry = null;
                var channel = new ServerTelemetryChannel();
                channel.TelemetryProcessor = new StubTelemetryProcessor(null) { OnProcess = t => sentTelemetry = t };

                var telemetry = new StubTelemetry();
                Assert.Throws<InvalidOperationException>(() => channel.Send(telemetry));
            }
        }
    }
}
