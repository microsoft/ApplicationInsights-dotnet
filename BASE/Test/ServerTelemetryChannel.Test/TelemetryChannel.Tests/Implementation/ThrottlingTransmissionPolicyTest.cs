namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Diagnostics.Tracing;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public class ThrottlingTransmissionPolicyTest
    {
        [TestClass]
        public class HandleTransmissionSentEvent : ErrorHandlingTransmissionPolicyTest
        {
            private const int ResponseCodeTooManyRequests = 429;
            private const int ResponseCodeTooManyRequestsOverExtendedTime = 439;
            private const int ResponseCodePaymentRequired = 402;
            private const int ResponseCodeUnsupported = 0;

            [TestMethod]
            public void AssertTooManyRequestsStopsSending()
            {
                this.PositiveTest(ResponseCodeTooManyRequests, 0, null, null, false);
            }

            [TestMethod]
            public void AssertTooManyRequestsStopsSendingWithFlushAsyncTask()
            {
                this.PositiveTest(ResponseCodeTooManyRequests, 0, 0, null, true);
            }

            [TestMethod]
            public void AssertTooManyRequestsOverExtendedTimeStopsSendingAndCleansCache()
            {
                this.PositiveTest(ResponseCodeTooManyRequestsOverExtendedTime, 0, 0, 0, false);
            }

            [TestMethod]
            public void AssertPaymentRequiredDoesntChangeCapacity()
            {
                var transmitter = new StubTransmitter();
                transmitter.OnApplyPolicies = () =>
                {
                    throw new Exception("Apply shouldn't be called because unsupported response code was passed");
                };

                var policy = new ThrottlingTransmissionPolicy();
                policy.Initialize(transmitter);

                transmitter.OnTransmissionSent(
                    new TransmissionProcessedEventArgs(
                        new StubTransmission(), null, new HttpWebResponseWrapper()
                        {
                            StatusCode = ResponseCodePaymentRequired
                        }));

                Assert.IsNull(policy.MaxSenderCapacity);
                Assert.IsNull(policy.MaxBufferCapacity);
                Assert.IsNull(policy.MaxStorageCapacity);
            }

            [TestMethod]
            public void AssertUnsupportedResponseCodeDoesnotChangeCapacity()
            {
                var transmitter = new StubTransmitter();
                transmitter.OnApplyPolicies = () =>
                {
                    throw new Exception("Apply shouldn't be called because unsupported response code was passed");
                };

                var policy = new ThrottlingTransmissionPolicy();
                policy.Initialize(transmitter);

                transmitter.OnTransmissionSent(
                    new TransmissionProcessedEventArgs(
                        new StubTransmission(), null, new HttpWebResponseWrapper()
                        {
                            StatusCode = ResponseCodeUnsupported
                        }));

                Assert.IsNull(policy.MaxSenderCapacity);
                Assert.IsNull(policy.MaxBufferCapacity);
                Assert.IsNull(policy.MaxStorageCapacity);
            }

            [TestMethod]
            public void CannotParseRetryAfterWritesToEventSource()
            {
                const string unparsableDate = "no one can parse me! :)";

                var transmitter = new StubTransmitter();
                var policy = new ThrottlingTransmissionPolicy();
                policy.Initialize(transmitter);

                using (var listener = new TestEventListener())
                {
                    const long AllKeyword = -1;
                    listener.EnableEvents(TelemetryChannelEventSource.Log, EventLevel.LogAlways, (EventKeywords)AllKeyword);

                    transmitter.OnTransmissionSent(
                        new TransmissionProcessedEventArgs(
                            new StubTransmission(),null, new HttpWebResponseWrapper()
                            {
                                StatusCode = ResponseStatusCodes.ResponseCodeTooManyRequests,
                                RetryAfterHeader = unparsableDate
                            })
                        );
                    EventWrittenEventArgs trace = listener.Messages.First(args => args.EventId == 24);
                    Assert.AreEqual(unparsableDate, (string)trace.Payload[0]);
                }
            }

            private void PositiveTest(int responseCode, int? expectedSenderCapacity, int? expectedBufferCapacity, int? expectedStorageCapacity, bool hasFlushTask)
            {
                const int RetryAfterSeconds = 2;
                string retryAfter = DateTime.Now.ToUniversalTime().AddSeconds(RetryAfterSeconds).ToString("R", CultureInfo.InvariantCulture);
                const int WaitForTheFirstApplyAsync = 100;
                int waitForTheSecondApplyAsync = (RetryAfterSeconds * 1000) /*to milliseconds*/ +
                    500 /**magic number to wait for other code before/after 
                         * timer which calls 2nd ApplyAsync
                         **/;

                var policyApplied = new AutoResetEvent(false);
                var transmitter = new StubTransmitter();
                transmitter.OnApplyPolicies = () =>
                {
                    policyApplied.Set();
                };

                var policy = new ThrottlingTransmissionPolicy();
                policy.Initialize(transmitter);

                string statusDescription = null;

                transmitter.OnTransmissionSent(
                    new TransmissionProcessedEventArgs(
                        new StubTransmission() { IsFlushAsyncInProgress = hasFlushTask }, null, new HttpWebResponseWrapper()
                        {
                            StatusCode = responseCode,
                            StatusDescription = statusDescription,
                            RetryAfterHeader = retryAfter
                        }));

                Assert.IsTrue(policyApplied.WaitOne(WaitForTheFirstApplyAsync));
                
                Assert.AreEqual(expectedSenderCapacity, policy.MaxSenderCapacity);
                Assert.AreEqual(expectedBufferCapacity, policy.MaxBufferCapacity);
                Assert.AreEqual(expectedStorageCapacity, policy.MaxStorageCapacity);

                Assert.IsTrue(policyApplied.WaitOne(waitForTheSecondApplyAsync));

                // Check that it resets after retry-after interval
                Assert.IsNull(policy.MaxSenderCapacity);
                Assert.IsNull(policy.MaxBufferCapacity);
                Assert.IsNull(policy.MaxStorageCapacity);
            }
        }
    }
}