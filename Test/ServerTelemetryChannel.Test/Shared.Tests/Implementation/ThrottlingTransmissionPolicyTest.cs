namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
#if NET45
    using System.Diagnostics.Tracing;
#endif
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Helpers;
#if NET40
    using Microsoft.Diagnostics.Tracing;
#endif
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;
#if !NET40
    using TaskEx = System.Threading.Tasks.Task;
#endif

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
                this.PositiveTest(ResponseCodeTooManyRequests, 0, null, null);
            }

            [TestMethod]
            public void AssertTooManyRequestsOverExtendedTimeStopsSendingAndCleansCache()
            {
                this.PositiveTest(ResponseCodeTooManyRequestsOverExtendedTime, 0, 0, 0);
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
                        new StubTransmission(),
                    CreateThrottledResponse(ResponseCodePaymentRequired, 1)));

                Assert.Null(policy.MaxSenderCapacity);
                Assert.Null(policy.MaxBufferCapacity);
                Assert.Null(policy.MaxStorageCapacity);
            }

            [TestMethod]
            public void AssertUnsupportedResponseCodeDoesntChangeCapacity()
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
                        new StubTransmission(),
                    CreateThrottledResponse(ResponseCodeUnsupported, 1)));

                Assert.Null(policy.MaxSenderCapacity);
                Assert.Null(policy.MaxBufferCapacity);
                Assert.Null(policy.MaxStorageCapacity);
            }

            [TestMethod]
            public void CannotParseRetryAfterWritesToEventSource()
            {
                const string UnparsableDate = "no one can parse me! :)";

                var transmitter = new StubTransmitter();
                var policy = new ThrottlingTransmissionPolicy();
                policy.Initialize(transmitter);

                using (var listener = new TestEventListener())
                {
                    const long AllKeyword = -1;
                    listener.EnableEvents(TelemetryChannelEventSource.Log, EventLevel.LogAlways, (EventKeywords)AllKeyword);

                    transmitter.OnTransmissionSent(
                        new TransmissionProcessedEventArgs(
                            new StubTransmission(),
                        CreateThrottledResponse(ResponseCodeTooManyRequests, UnparsableDate)));

                    EventWrittenEventArgs trace = listener.Messages.First(args => args.EventId == 24);
                    Assert.Equal(UnparsableDate, (string)trace.Payload[0]);
                }
            }

            private static WebException CreateThrottledResponse(int throttledStatusCode, int retryAfter)
            {
                return CreateThrottledResponse(throttledStatusCode, retryAfter.ToString(CultureInfo.InvariantCulture));
            }

            private static WebException CreateThrottledResponse(int throttledStatusCode, string retryAfter)
            {
                var mockWebResponse = new Moq.Mock<HttpWebResponse>();

                var responseHeaders = new WebHeaderCollection();
                responseHeaders.Add(HttpResponseHeader.RetryAfter, retryAfter);

                mockWebResponse.SetupGet<HttpStatusCode>((webRes) => webRes.StatusCode).Returns((HttpStatusCode)throttledStatusCode);
                mockWebResponse.SetupGet<WebHeaderCollection>((webRes) => webRes.Headers).Returns((WebHeaderCollection)responseHeaders);

                return new WebException("Transmitter Error", null, WebExceptionStatus.UnknownError, mockWebResponse.Object);
            }

            private void PositiveTest(int responseCode, int? expectedSenderCapacity, int? expectedBufferCapacity, int? expectedStorageCapacity)
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

                transmitter.OnTransmissionSent(
                    new TransmissionProcessedEventArgs(
                        new StubTransmission(),
                    CreateThrottledResponse(responseCode, retryAfter)));

                Assert.True(policyApplied.WaitOne(WaitForTheFirstApplyAsync));
                
                Assert.Equal(expectedSenderCapacity, policy.MaxSenderCapacity);
                Assert.Equal(expectedBufferCapacity, policy.MaxBufferCapacity);
                Assert.Equal(expectedStorageCapacity, policy.MaxStorageCapacity);

                Assert.True(policyApplied.WaitOne(waitForTheSecondApplyAsync));

                // Check that it resets after retry-after interval
                Assert.Null(policy.MaxSenderCapacity);
                Assert.Null(policy.MaxBufferCapacity);
                Assert.Null(policy.MaxStorageCapacity);
            }
        }
    }
}