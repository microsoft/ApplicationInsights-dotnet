namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
#if NET45
    using System.Diagnostics.Tracing;
#endif
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.WindowsServer.Channel.Helpers;
    using Microsoft.ApplicationInsights.Web.TestFramework;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Helpers;
#if NET40
    using Microsoft.Diagnostics.Tracing;
#endif
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;
    using System.Text;
    using System.IO;
#if NET45
    using TaskEx = System.Threading.Tasks.Task;
#endif

    public class ErrorHandlingTransmissionPolicyTest
    {
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

            [TestMethod]
            public void LogsAdditionalTracesIfResponseIsProvided()
            {
                using (var listener = new TestEventListener())
                {
                    // Arrange:
                    const long AllKeywords = -1;
                    listener.EnableEvents(TelemetryChannelEventSource.Log, EventLevel.LogAlways, (EventKeywords)AllKeywords);

                    Transmission enqueuedTransmission = null;
                    var transmitter = new StubTransmitter
                    {
                        OnEnqueue = transmission => { enqueuedTransmission = transmission; }
                    };

                    var policy = new TestableErrorHandlingTransmissionPolicy
                    {
                        BackOffTime = TimeSpan.FromMilliseconds(10)
                    };

                    policy.Initialize(transmitter);

                    var failedTransmission = new StubTransmission();
                    var response = new HttpWebResponseWrapper {Content = BackendResponseHelper.CreateBackendResponse(2, 1, new[] { "123" })};

                    // Act:
                    transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(failedTransmission, CreateException(statusCode: 408), response));

                    // Assert:
                    var traces = listener.Messages.ToList();
                    Assert.True(traces.Count > 2);
                    Assert.Equal(23, traces[0].EventId); // failed to send
                    Assert.Equal(7, traces[1].EventId); // additional trace
                    Assert.Equal("Explanation", traces[1].Payload[0]);
                }
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
                string content = BackendResponseHelper.CreateBackendResponse(3,1, new [] {"500"});
                var bytes = Encoding.UTF8.GetBytes(content);
                var responseStream = new MemoryStream();
                responseStream.Write(bytes, 0, bytes.Length);
                responseStream.Seek(0, SeekOrigin.Begin);

                var mockWebResponse = new Moq.Mock<HttpWebResponse>();
                mockWebResponse.Setup(c => c.GetResponseStream()).Returns(responseStream);

                mockWebResponse.SetupGet<HttpStatusCode>((webRes) => webRes.StatusCode).Returns((HttpStatusCode)statusCode);

                return new WebException("Transmitter Error", null, WebExceptionStatus.UnknownError, mockWebResponse.Object);
            }
        }

        private class TestableErrorHandlingTransmissionPolicy : ErrorHandlingTransmissionPolicy
        {
            public TimeSpan BackOffTime { get; set; }

            protected override TimeSpan GetBackOffTime(NameValueCollection headers = null)
            {
                return this.BackOffTime;
            }
        }
    }
}