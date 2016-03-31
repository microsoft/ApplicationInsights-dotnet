namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
#if NET45
    using System.Diagnostics.Tracing;
#endif
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
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

    public class ErrorHandlingTransmissionPolicyTest
    {
        [TestClass]
        public class GetBackOffTime : ErrorHandlingTransmissionPolicyTest
        {
            [TestMethod]
            public void NoErrorDelayIsSameAsSlotDelay()
            {
                var policy = new ErrorHandlingTransmissionPolicy();
                TimeSpan delay = policy.GetBackOffTime();
                Assert.Equal(TimeSpan.FromSeconds(10), delay);
            }

            [TestMethod]
            public void FirstErrorDelayIsSameAsSlotDelay()
            {
                var policy = new ErrorHandlingTransmissionPolicy();
                policy.ConsecutiveErrors = 1;
                TimeSpan delay = policy.GetBackOffTime();
                Assert.Equal(TimeSpan.FromSeconds(10), delay);
            }

            [TestMethod]
            public void UpperBoundOfDelayIsMaxDelay()
            {
                var policy = new ErrorHandlingTransmissionPolicy();
                policy.ConsecutiveErrors = int.MaxValue;
                TimeSpan delay = policy.GetBackOffTime();
                Assert.InRange(delay, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(3600));
            }
        }

        [TestClass]
        public class HandleTransmissionSentEvent : ErrorHandlingTransmissionPolicyTest
        {
            [TestMethod]
            public void StopsTransmissionSendingWhenTransmissionTimesOut()
            {
                var policyApplied = new AutoResetEvent(false);
                var transmitter = new StubTransmitter();
                transmitter.OnApplyPolicies = () =>
                {
                    policyApplied.Set();
                };

                var policy = new TestableErrorHandlingTransmissionPolicy();
                policy.Initialize(transmitter);

                policy.BackOffTime = TimeSpan.FromSeconds(10);
                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(new StubTransmission(), CreateException(statusCode: 408)));
                
                Assert.True(policyApplied.WaitOne(100));
                Assert.Equal(0, policy.MaxSenderCapacity);
            }

            [TestMethod]
            public void ResumesTransmissionSenderAfterPauseDuration()
            {
                var policyApplied = new AutoResetEvent(false);
                var transmitter = new StubTransmitter();
                transmitter.OnApplyPolicies = () =>
                {
                    policyApplied.Set();
                };

                var policy = new TestableErrorHandlingTransmissionPolicy();
                policy.Initialize(transmitter);
                
                policy.BackOffTime = TimeSpan.FromMilliseconds(1);
                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(new StubTransmission(), CreateException(statusCode: 408)));
                
                Assert.True(policyApplied.WaitOne(100));
                Assert.True(policyApplied.WaitOne(100));
                Assert.Null(policy.MaxSenderCapacity);
            }

            [TestMethod]
            public void ForPartialSuccessIfItemIsRejectedOnlyThisItemIsUploadedBack()
            {
                IList<Transmission> enqueuedTransmissions = new List<Transmission>();
                var transmitter = new StubTransmitter
                {
                    OnEnqueue = t => { enqueuedTransmissions.Add(t); }
                };

                var policy = new ErrorHandlingTransmissionPolicy();
                policy.Initialize(transmitter);

                var items = new List<ITelemetry> { new EventTelemetry(), new EventTelemetry() };
                Transmission transmission = new Transmission(new Uri("http://uri"), items, "type", "encoding");

                string breezeResponse =
                   "{" +
                    "\"itemsReceived\": 2," +
                    "\"itemsAccepted\": 1," +
                    "\"errors\": [" +
                                    "{" +
                                      "\"index\": 0," +
                                      "\"statusCode\": 402," +
                                      "\"message\": \"Explanation\"" +
                                    "}" +
                   "]}";

                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(transmission, null, breezeResponse));

                Assert.Equal(1, enqueuedTransmissions.Count);
            }

            [TestMethod]
            public void ForPartialSuccessIfItemsAreRejectedTheyAreUploadedBackGroupedByStatusCode()
            {
                IList<Transmission> enqueuedTransmissions = new List<Transmission>();
                var transmitter = new StubTransmitter
                {
                    OnEnqueue = t => { enqueuedTransmissions.Add(t); }
                };

                var policy = new ErrorHandlingTransmissionPolicy();
                policy.Initialize(transmitter);

                var items = new List<ITelemetry> { new EventTelemetry(), new EventTelemetry() };
                Transmission transmission = new Transmission(new Uri("http://uri"), items, "type", "encoding");

                string breezeResponse =
                   "{" +
                    "\"itemsReceived\": 2," +
                    "\"itemsAccepted\": 0," +
                    "\"errors\": [" +
                                    "{" +
                                      "\"index\": 0," +
                                      "\"statusCode\": 408," +
                                      "\"message\": \"Explanation\"" +
                                    "}," +
                                    "{" +
                                      "\"index\": 1," +
                                      "\"statusCode\": 408," +
                                      "\"message\": \"Explanation\"" +
                                    "}" +
                   "]}";

                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(transmission, null, breezeResponse));

                Assert.Equal(1, enqueuedTransmissions.Count);
            }

            [TestMethod]
            public void ForPartialSuccessIfNumberOfRecievedItemsEqualsToNumberOfAcceptedErrorsListIsIgnored()
            {
                var transmitter = new StubTransmitter();

                var policy = new ErrorHandlingTransmissionPolicy();
                policy.Initialize(transmitter);

                var items = new List<ITelemetry> { new EventTelemetry(), new EventTelemetry() };
                Transmission transmission = new Transmission(new Uri("http://uri"), items, "type", "encoding");

                string breezeResponse =
                   "{" +
                    "\"itemsReceived\": 2," +
                    "\"itemsAccepted\": 2," +
                    "\"errors\": [" +
                                    "{" +
                                      "\"index\": 0," +
                                      "\"statusCode\": 429," +
                                      "\"message\": \"Explanation\"" +
                                    "}," +
                                    "{" +
                                      "\"index\": 1," +
                                      "\"statusCode\": 429," +
                                      "\"message\": \"Explanation\"" +
                                    "}" +
                   "]}";

                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(transmission, null, breezeResponse));

                Assert.Equal(0, policy.ConsecutiveErrors);
            }

            [TestMethod]
            public void ForPartialSuccessNewTransmissionCreatedByIndexesFromBreezeResponse()
            {
                IList<Transmission> enqueuedTransmissions = new List<Transmission>();
                var transmitter = new StubTransmitter
                {
                    OnEnqueue = t => { enqueuedTransmissions.Add(t); }
                };

                var policy = new ErrorHandlingTransmissionPolicy();
                policy.Initialize(transmitter);

                var items = new List<ITelemetry>
                {
                    new EventTelemetry("1"),
                    new EventTelemetry("2"),
                    new EventTelemetry("3"),
                };
                Transmission transmission = new Transmission(new Uri("http://uri"), items, "type", "encoding");

                string breezeResponse =
                   "{" +
                    "\"itemsReceived\": 3," +
                    "\"itemsAccepted\": 1," +
                    "\"errors\": [" +
                                    "{" +
                                      "\"index\": 0," +
                                      "\"statusCode\": 439," +
                                      "\"message\": \"Explanation\"" +
                                    "}," +
                                    "{" +
                                      "\"index\": 2," +
                                      "\"statusCode\": 439," +
                                      "\"message\": \"Explanation\"" +
                                    "}" +
                   "]}";

                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(transmission, null, breezeResponse));

                string[] newItems = JsonSerializer
                    .Deserialize(enqueuedTransmissions[0].Content)
                    .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                Assert.Equal(2, newItems.Length);
                Assert.True(newItems[0].Contains("\"name\":\"1\""));
                Assert.True(newItems[1].Contains("\"name\":\"3\""));
            }

            [TestMethod]
            public void ForPartialSuccessIfMultipleItemsAreRejectedNumberOfErrorsIsIncreasedByOne()
            {
                // Number of errors determine backoff timeout. 
                // When we get several bad items in one batch we want to increase errors by 1 only since it is one attempt to access Breeze

                var transmitter = new StubTransmitter();

                var policy = new ErrorHandlingTransmissionPolicy();
                policy.Initialize(transmitter);

                var items = new List<ITelemetry> { new EventTelemetry(), new EventTelemetry() };
                Transmission transmission = new Transmission(new Uri("http://uri"), items, "type", "encoding");

                string breezeResponse =
                   "{" +
                    "\"itemsReceived\": 2," +
                    "\"itemsAccepted\": 0," +
                    "\"errors\": [" +
                                    "{" +
                                      "\"index\": 0," +
                                      "\"statusCode\": 500," +
                                      "\"message\": \"Explanation\"" +
                                    "}," +
                                    "{" +
                                      "\"index\": 1," +
                                      "\"statusCode\": 503," +
                                      "\"message\": \"Explanation\"" +
                                    "}" +
                   "]}";

                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(transmission, null, breezeResponse));

                Assert.Equal(1, policy.ConsecutiveErrors);
            }

            [TestMethod]
            public void ForPartialSuccessIfBreezeResponseIsBadJsonWeDoNotIncreaseErrorCount()
            {
                IList<Transmission> enqueuedTransmissions = new List<Transmission>();
                var transmitter = new StubTransmitter
                {
                    OnEnqueue = t => { enqueuedTransmissions.Add(t); }
                };

                var policy = new ErrorHandlingTransmissionPolicy();
                policy.Initialize(transmitter);

                var items = new List<ITelemetry> { new EventTelemetry(), new EventTelemetry() };
                Transmission transmission = new Transmission(new Uri("http://uri"), items, "type", "encoding");

                string breezeResponse = "[,]";

                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(transmission, null, breezeResponse));

                Assert.Equal(0, policy.ConsecutiveErrors);
                Assert.Equal(0, enqueuedTransmissions.Count);
            }

            [TestMethod]
            public void ForPartialSuccessIfBreezeIndexMoreThanItemsInTransmissionIgnoreError()
            {
                IList<Transmission> enqueuedTransmissions = new List<Transmission>();
                var transmitter = new StubTransmitter
                {
                    OnEnqueue = t => { enqueuedTransmissions.Add(t); }
                };

                var policy = new ErrorHandlingTransmissionPolicy();
                policy.Initialize(transmitter);

                var items = new List<ITelemetry> { new EventTelemetry() };
                Transmission transmission = new Transmission(new Uri("http://uri"), items, "type", "encoding");

                // Index is 0-based
                string breezeResponse =
                   "{" +
                    "\"itemsReceived\": 2," +
                    "\"itemsAccepted\": 1," +
                    "\"errors\": [" +
                                    "{" +
                                      "\"index\": 1," +
                                      "\"statusCode\": 402," +
                                      "\"message\": \"Explanation\"" +
                                    "}" +
                   "]}";

                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(transmission, null, breezeResponse));

                Assert.Equal(0, policy.ConsecutiveErrors);
                Assert.Equal(0, enqueuedTransmissions.Count);
            }

            [TestMethod]
            public void KeepsTransmissionSenderPausedWhenAdditionalTransmissionsFail()
            {
                var transmitter = new StubTransmitter();
                var policy = new TestableErrorHandlingTransmissionPolicy();
                policy.Initialize(transmitter);
                
                policy.BackOffTime = TimeSpan.FromMilliseconds(10);
                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(new StubTransmission(), CreateException(statusCode: 408)));
                
                policy.BackOffTime = TimeSpan.FromMilliseconds(50);
                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(new StubTransmission(), CreateException(statusCode: 408)));

                Thread.Sleep(TimeSpan.FromMilliseconds(30));

                Assert.Equal(0, policy.MaxSenderCapacity);
            }

            [TestMethod]
            public void RetriesFailedTransmissionIfItsNumberOfAttemptsDidNotReachMaximum()
            {
                Transmission enqueuedTransmission = null;
                var transmitter = new StubTransmitter();
                transmitter.OnEnqueue = transmission =>
                {
                    enqueuedTransmission = transmission;
                };

                var policy = new ErrorHandlingTransmissionPolicy();
                policy.Initialize(transmitter);

                var failedTransmission = new StubTransmission();
                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(failedTransmission, CreateException(statusCode: 408)));

                Assert.Same(failedTransmission, enqueuedTransmission);
            }

            [TestMethod]
            public void RetriesFailedTransmissionInfinitely()
            {
                Transmission enqueuedTransmission = null;
                var transmitter = new StubTransmitter();
                transmitter.OnEnqueue = transmission =>
                {
                    enqueuedTransmission = transmission;
                };

                var policy = new TestableErrorHandlingTransmissionPolicy();
                policy.BackOffTime = TimeSpan.FromMilliseconds(10);
                policy.Initialize(transmitter);

                var failedTransmission = new StubTransmission();
                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(failedTransmission, CreateException(statusCode: 408)));
                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(failedTransmission, CreateException(statusCode: 408)));
                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(failedTransmission, CreateException(statusCode: 408)));
                Assert.Same(failedTransmission, enqueuedTransmission);
            }

            [TestMethod]
            public void DoesNotRetrySuccessfulTransmission()
            {
                Transmission enqueuedTransmission = null;
                var transmitter = new StubTransmitter();
                transmitter.OnEnqueue = transmission =>
                {
                    enqueuedTransmission = transmission;
                };

                var policy = new ErrorHandlingTransmissionPolicy();
                policy.Initialize(transmitter);

                var successfulTransmission = new StubTransmission();
                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(successfulTransmission));

                Assert.Null(enqueuedTransmission);
                Assert.Equal(0, policy.ConsecutiveErrors);
            }

            [TestMethod]
            public void CatchesAndLogsSynchronousExceptionsThrownByTransmitterWhenPausingTransmission()
            {
                var policy = new ErrorHandlingTransmissionPolicy();
                var exception = CreateException(statusCode: 408);
                var transmitter = new StubTransmitter { OnApplyPolicies = () => { throw exception; } };
                CatchesAndLogsExceptionThrownByTransmitter(policy, transmitter, exception);
            }

            [TestMethod, Timeout(1000)]
            public void CatchesAndLogsAsynchronousExceptionsThrownByTransmitterWhenPausingTransmission()
            {
                var policy = new ErrorHandlingTransmissionPolicy();
                var exception = CreateException(statusCode: 408);
                var transmitter = new StubTransmitter { OnApplyPolicies = () => ThrowAsync(exception) };
                CatchesAndLogsExceptionThrownByTransmitter(policy, transmitter, exception);
            }

            [TestMethod, Timeout(1000)]
            public void CatchesAndLogsSynchronousExceptionsThrownByTransmitterWhenResumingTransmission()
            {
                var policy = new TestableErrorHandlingTransmissionPolicy { BackOffTime = TimeSpan.FromMilliseconds(1) };
                var exception = CreateException(statusCode: 408);
                var transmitter = new StubTransmitter();
                transmitter.OnApplyPolicies = () =>
                {
                    if (policy.MaxSenderCapacity == null)
                    {
                        throw exception;
                    }
                };
                CatchesAndLogsExceptionThrownByTransmitter(policy, transmitter, exception);
            }

            private static Task ThrowAsync(Exception e)
            {
                var tcs = new TaskCompletionSource<object>(null);
                tcs.SetException(e);
                return tcs.Task;
            }

            private static void CatchesAndLogsExceptionThrownByTransmitter(ErrorHandlingTransmissionPolicy policy, StubTransmitter transmitter, Exception exception)
            {
                policy.Initialize(transmitter);

                using (var listener = new TestEventListener())
                {
                    const long AllKeywords = -1;
                    listener.EnableEvents(TelemetryChannelEventSource.Log, EventLevel.Warning, (EventKeywords)AllKeywords);

                    transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(new StubTransmission(), CreateException(statusCode: 408)));

                    EventWrittenEventArgs error = listener.Messages.First(args => args.EventId == 23);
                    Assert.Contains(exception.Message, (string)error.Payload[1], StringComparison.Ordinal);
                }
            }
            
            private static WebException CreateException(int statusCode)
            {
                var mockWebResponse = new Moq.Mock<HttpWebResponse>();

                mockWebResponse.SetupGet<HttpStatusCode>((webRes) => webRes.StatusCode).Returns((HttpStatusCode)statusCode);

                return new WebException("Transmitter Error", null, WebExceptionStatus.UnknownError, mockWebResponse.Object);
            }
        }

        private class TestableErrorHandlingTransmissionPolicy : ErrorHandlingTransmissionPolicy
        {
            public TimeSpan BackOffTime { get; set; }

            internal override TimeSpan GetBackOffTime()
            {
                return this.BackOffTime;
            }
        }
    }
}