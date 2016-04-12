namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Threading;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Web.TestFramework;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;

    public class TransmissionSenderTest
    {
        [TestClass]
        public class MaxNumberOfTransmissions : TransmissionSenderTest
        {
            [TestMethod]
            public void DefaultValueIsAppropriateForMostApps()
            {
                var sender = new TransmissionSender();
                Assert.Equal(3, sender.Capacity);
            }

            [TestMethod]
            public void CanBeSetToZeroToDisableSendingOfTransmissions()
            {
                var sender = new TransmissionSender();
                sender.Capacity = 0;
                Assert.Equal(0, sender.Capacity);
            }

            [TestMethod]
            public void ThrowsArgumentOutOfRangeExceptionWhenNewValueLessThanZero()
            {
                var sender = new TransmissionSender();
                Assert.Throws<ArgumentOutOfRangeException>(() => sender.Capacity = -1);
            }
        }

        [TestClass]
        public class EnqueueAsync : TransmissionSenderTest
        {
            [TestMethod]
            public void StartsSendingTransmissionAndReturnsImmediatelyToUnblockCallingThread()
            {
                var transmissionCanFinishSending = new ManualResetEventSlim();
                var transmission = new StubTransmission { OnSend = () => { transmissionCanFinishSending.Wait(); return null; } };
                var sender = new TransmissionSender();

                sender.Enqueue(() => transmission);

                transmissionCanFinishSending.Set();
            }

            [TestMethod]
            public void ReturnsTrueWhenNewTransmissionDoesNotExceedMaxNumber()
            {
                var sender = new TransmissionSender { Capacity = 1 };
                bool result = sender.Enqueue(() => new StubTransmission());
                Assert.True(result);
            }

            [TestMethod]
            public void ReturnsFalseWhenNewTransmissionExceedsMaxNumber()
            {
                var sender = new TransmissionSender { Capacity = 0 };
                bool transmissionSent = sender.Enqueue(() => new StubTransmission());
                Assert.False(transmissionSent);
            }

            [TestMethod]
            public void DoesNotCountRejectedTransmissionsAgainstMaxNumber()
            {
                var sender = new TransmissionSender { Capacity = 0 };
                Transmission transmission1 = new StubTransmission();
                sender.Enqueue(() => transmission1);

                sender.Capacity = 1;

                Transmission transmission2 = new StubTransmission();
                Assert.True(sender.Enqueue(() => transmission2));
            }

            [TestMethod]
            public void AllowsNewTransmissionsToBeSentAsPreviousTransmissionsAreCompleted()
            {
                var sender = new TransmissionSender { Capacity = 1 };
                sender.Enqueue(() => new StubTransmission());
                Thread.Sleep(50);
                Assert.True(sender.Enqueue(() => new StubTransmission()));
            }

            [TestMethod]
            public void DoesNotInvokeTransmissionGetterWhenMaxNumberOfTransmissionsIsExceededToKeepItBuffered()
            {
                bool transmissionGetterInvoked = false;
                Func<Transmission> transmissionGetter = () =>
                {
                    transmissionGetterInvoked = true;
                    return new StubTransmission();
                };
                var sender = new TransmissionSender { Capacity = 0 };

                sender.Enqueue(transmissionGetter);

                Assert.False(transmissionGetterInvoked);
            }

            [TestMethod]
            public void ReturnsFalseWhenTransmissionGetterReturnedNullIndicatingEmptyBuffer()
            {
                var sender = new TransmissionSender();
                Assert.False(sender.Enqueue(() => null));
            }

            [TestMethod]
            public void DoesNotCountNullTransmissionsReturnedFromEmptyBufferAgainstMaxNumber()
            {
                var sender = new TransmissionSender { Capacity = 1 };
                sender.Enqueue(() => null);

                Transmission transmission2 = new StubTransmission();
                Assert.True(sender.Enqueue(() => transmission2));
            }

            [TestMethod]
            public void DoesNotCountTransmissionsSentWithExceptionsAgainstMaxNumber()
            {
                var sender = new TransmissionSender { Capacity = 1 };

                Transmission transmission1 = new StubTransmission { OnSend = () => { throw new TimeoutException(); } };
                sender.Enqueue(() => transmission1);
                Thread.Sleep(10);
                
                Transmission transmission2 = new StubTransmission();
                bool transmission2Sent = sender.Enqueue(() => transmission2);

                Assert.True(transmission2Sent);
            }

            [TestMethod]
            public void DoesNotContinueAsyncOperationsOnCapturedSynchronizationContextToImprovePerformance()
            {
                bool postedBack = false;
                using (var context = new StubSynchronizationContext())
                {
                    context.OnPost = (callback, state) =>
                    {
                        postedBack = true;
                        callback(state);
                    };

                    var buffer = new TransmissionSender();
                    buffer.Enqueue(() => new StubTransmission());
                }

                Assert.False(postedBack);
            }
        }

        [TestClass]
        public class TransmissionSent : TransmissionSenderTest
        {
            [TestMethod]
            public void IsRaisedWhenTransmissionIsSentSuccessfully()
            {
                var sender = new TransmissionSender();

                var eventIsRaised = new ManualResetEventSlim();
                object eventSender = null;
                TransmissionProcessedEventArgs eventArgs = null;
                sender.TransmissionSent += (s, a) =>
                {
                    eventSender = s;
                    eventArgs = a;
                    eventIsRaised.Set();
                };

                Transmission transmission = new StubTransmission();
                sender.Enqueue(() => transmission);

                Assert.True(eventIsRaised.Wait(50));
                Assert.Same(sender, eventSender);
                Assert.Same(transmission, eventArgs.Transmission);
            }

            [TestMethod]
            public void IsRaisedWhenTransmissionThrownExceptionWhileSending()
            {
                var sender = new TransmissionSender();

                var eventIsRaised = new ManualResetEventSlim();
                object eventSender = null;
                TransmissionProcessedEventArgs eventArgs = null;
                sender.TransmissionSent += (s, a) =>
                {
                    eventSender = s;
                    eventArgs = a;
                    eventIsRaised.Set();
                };

                var exception = new TimeoutException();
                Transmission transmission = new StubTransmission { OnSend = () => { throw exception; } };
                sender.Enqueue(() => transmission);

                Assert.True(eventIsRaised.Wait(5000));
                Assert.Same(sender, eventSender);
                Assert.Same(transmission, eventArgs.Transmission);
                Assert.Same(exception, eventArgs.Exception);
            }

            [TestMethod]
            public void IsRaisedWhenTransmissionReturnsPartialSuccessResult()
            {
                var sender = new TransmissionSender();

                var eventIsRaised = new ManualResetEventSlim();
                object eventSender = null;
                TransmissionProcessedEventArgs eventArgs = null;
                sender.TransmissionSent += (s, a) =>
                {
                    eventSender = s;
                    eventArgs = a;
                    eventIsRaised.Set();
                };

                var wrapper = new HttpWebResponseWrapper();
                Transmission transmission = new StubTransmission { OnSend = () => wrapper };
                sender.Enqueue(() => transmission);

                Assert.True(eventIsRaised.Wait(50));
                Assert.Same(sender, eventSender);
                Assert.Same(transmission, eventArgs.Transmission);
                Assert.Same(wrapper, eventArgs.Response);
            }
        }
    }
}
