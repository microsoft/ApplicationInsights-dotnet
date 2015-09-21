namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
#if NET45
    using System.Diagnostics.Tracing;
#endif
    using System.Linq;
    using System.Net.Sockets;
    using Microsoft.ApplicationInsights.Web.TestFramework;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Helpers;

#if NET40
    using Microsoft.Diagnostics.Tracing;
#endif
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;
#if NET45
    using TaskEx = System.Threading.Tasks.Task;
#endif

    [TestClass]
    public class NetworkAvailabilityTransmissionPolicyTest
    {
        [TestClass]
        public class Class : NetworkAvailabilityTransmissionPolicyTest
        {
            [TestMethod]
            public void ConstructorDoesNotSubscribeToNetworkChangeEventsToPreventExceptionsBeforePolicyIsInitialized()
            {
                var network = new StubNetwork { OnIsAvailable = () => false };
                var policy = new NetworkAvailabilityTransmissionPolicy(network);

                network.OnStatusChanged(EventArgs.Empty);

                Assert.Null(policy.MaxSenderCapacity);
                Assert.Null(policy.MaxBufferCapacity);
            }
        }

        [TestClass]
        public class Initialize : NetworkAvailabilityTransmissionPolicyTest
        {
            [TestMethod]
            public void SetsMaxSenderAndBufferCapacitiesToZeroWhenNetworkIsUnavailable()
            {
                var network = new StubNetwork { OnIsAvailable = () => false };
                var policy = new NetworkAvailabilityTransmissionPolicy(network);

                policy.Initialize(new StubTransmitter());

                Assert.Equal(0, policy.MaxSenderCapacity);
                Assert.Equal(0, policy.MaxBufferCapacity);
            }

            [TestMethod]
            public void DoesNotSetMaxSenderAndBufferCapacitiesToZeroWhenNetworkIsAvailable()
            {
                var network = new StubNetwork { OnIsAvailable = () => true };
                var policy = new NetworkAvailabilityTransmissionPolicy(network);

                policy.Initialize(new StubTransmitter());

                Assert.Null(policy.MaxSenderCapacity);
                Assert.Null(policy.MaxBufferCapacity);
            }

            [TestMethod]
            public void DoesNotSetMaxSenderAndBufferCapacitiesToZeroWhenNetworkAvailabilityCheckFailsHopingThatTransmissionsThemselvesMaySucceed()
            {
                var network = new StubNetwork { OnIsAvailable = () => { throw new Exception("NetworkInformationException"); } };
                var policy = new NetworkAvailabilityTransmissionPolicy(network);

                policy.Initialize(new StubTransmitter());

                Assert.Null(policy.MaxSenderCapacity);
                Assert.Null(policy.MaxBufferCapacity);
            }

            [TestMethod]
            public void HandlesExceptionsThrownByNetworkWhenAddingAddressChangedEventHandler()
            {
                using (var listener = new TestEventListener())
                {
                    const long AllKeywords = -1;
                    listener.EnableEvents(TelemetryChannelEventSource.Log, EventLevel.Warning, (EventKeywords)AllKeywords);
                    
                    var exception = new SocketException();
                    var network = new StubNetwork { OnAddAddressChangedEventHandler = handler => { throw exception; } };
                    var policy = new NetworkAvailabilityTransmissionPolicy(network);

                    policy.Initialize(new StubTransmitter());

                    EventWrittenEventArgs error = listener.Messages.First(arg => arg.EventId == 38);
                    Assert.Contains(exception.Message, (string)error.Payload[0], StringComparison.CurrentCulture);
                }
            }
        }

        [TestClass]
        public class HandleNetworkStatusChangedEvent : NetworkAvailabilityTransmissionPolicyTest
        {
            [TestMethod]
            public void SetsMaxSenderAndBufferCapacitiesToZeroWhenNetworkBecomesUnavailable()
            {
                bool isNetworkAvailable = true;
                var network = new StubNetwork { OnIsAvailable = () => isNetworkAvailable };
                var policy = new NetworkAvailabilityTransmissionPolicy(network);
                policy.Initialize(new StubTransmitter());

                isNetworkAvailable = false;
                network.OnStatusChanged(EventArgs.Empty);

                Assert.Equal(0, policy.MaxSenderCapacity);
                Assert.Equal(0, policy.MaxBufferCapacity);
            }

            [TestMethod]
            public void ResetsMaxSenderAndBufferCapacitiesToNullWhenNetworkBecomesAvailable()
            {
                bool isNetworkAvailable = false;
                var network = new StubNetwork { OnIsAvailable = () => isNetworkAvailable };
                var policy = new NetworkAvailabilityTransmissionPolicy(network);
                policy.Initialize(new StubTransmitter());

                isNetworkAvailable = true;
                network.OnStatusChanged(EventArgs.Empty);

                Assert.Null(policy.MaxSenderCapacity);
                Assert.Null(policy.MaxBufferCapacity);
            }

            [TestMethod]
            public void AsksTransmitterToApplyPoliciesWhenNetworkAvailabilityChanges()
            {
                bool policiesApplied = false;
                var transmitter = new StubTransmitter();
                transmitter.OnApplyPolicies = () => 
                {
                    policiesApplied = true;
                };

                var network = new StubNetwork();
                var policy = new NetworkAvailabilityTransmissionPolicy(network);
                policy.Initialize(transmitter);

                network.OnStatusChanged(EventArgs.Empty);

                Assert.True(policiesApplied);
            }

            [TestMethod]
            public void HandlesExceptionsThrownByNetworkIsAvailable()
            {
                using (var listener = new TestEventListener())
                {
                    const long AllKeywords = -1;
                    listener.EnableEvents(TelemetryChannelEventSource.Log, EventLevel.Warning, (EventKeywords)AllKeywords);

                    var exception = new Exception("Socket Error");
                    var network = new StubNetwork { OnIsAvailable = () => { throw exception; } };
                    var policy = new NetworkAvailabilityTransmissionPolicy(network);
                    policy.Initialize(new StubTransmitter());

                    network.OnStatusChanged(EventArgs.Empty);

                    EventWrittenEventArgs error = listener.Messages.First();
                    Assert.Contains(exception.Message, (string)error.Payload[0], StringComparison.Ordinal);
                }
            }
        }
    }
}
