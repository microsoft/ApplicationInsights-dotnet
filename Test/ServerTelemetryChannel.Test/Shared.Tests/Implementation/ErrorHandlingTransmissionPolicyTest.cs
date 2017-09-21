namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Channel.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.WindowsServer.Channel.Helpers;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    
    using System.Text;
    using System.IO;

    public class ErrorHandlingTransmissionPolicyTest
    {
        [TestClass]
        public class HandleTransmissionSentEvent : ErrorHandlingTransmissionPolicyTest
        {
            [TestMethod]
            public void StopsTransmissionSendingWhenTransmissionTimesOut()
            {
                var policyApplied = new AutoResetEvent(false);
                var transmitter = new StubTransmitter(new TestableBackoffLogicManager(TimeSpan.FromSeconds(10)));
                transmitter.OnApplyPolicies = () =>
                {
                    policyApplied.Set();
                };

                var policy = new ErrorHandlingTransmissionPolicy();
                policy.Initialize(transmitter);

                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(new StubTransmission(), CreateException(statusCode: 408)));
                
                Assert.IsTrue(policyApplied.WaitOne(100));
                Assert.AreEqual(0, policy.MaxSenderCapacity);
            }

            [TestMethod]
            public void ResumesTransmissionSenderAfterPauseDuration()
            {
                var policyApplied = new AutoResetEvent(false);
                var transmitter = new StubTransmitter(new TestableBackoffLogicManager(TimeSpan.FromMilliseconds(1)));
                transmitter.OnApplyPolicies = () =>
                {
                    policyApplied.Set();
                };

                var policy = new ErrorHandlingTransmissionPolicy();
                policy.Initialize(transmitter);
                
                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(new StubTransmission(), CreateException(statusCode: 408)));
                
                Assert.IsTrue(policyApplied.WaitOne(100));
                Assert.IsTrue(policyApplied.WaitOne(100));
                Assert.IsNull(policy.MaxSenderCapacity);
            }

            [TestMethod]
            public void KeepsTransmissionSenderPausedWhenAdditionalTransmissionsFail()
            {
                var transmitter = new StubTransmitter(new TestableBackoffLogicManager(TimeSpan.FromMinutes(1)));
                var policy = new ErrorHandlingTransmissionPolicy();
                policy.Initialize(transmitter);
                
                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(new StubTransmission(), CreateException(statusCode: 408)));
                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(new StubTransmission(), CreateException(statusCode: 408)));

                Thread.Sleep(TimeSpan.FromMilliseconds(30));

                Assert.AreEqual(0, policy.MaxSenderCapacity);
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

                Assert.AreSame(failedTransmission, enqueuedTransmission);
            }

            [TestMethod]
            public void RetriesFailedTransmissionInfinitely()
            {
                Transmission enqueuedTransmission = null;

                var transmitter = new StubTransmitter(new TestableBackoffLogicManager(TimeSpan.FromMilliseconds(10)));
                transmitter.OnEnqueue = transmission =>
                {
                    enqueuedTransmission = transmission;
                };

                var policy = new ErrorHandlingTransmissionPolicy();
                policy.Initialize(transmitter);

                var failedTransmission = new StubTransmission();
                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(failedTransmission, CreateException(statusCode: 408)));
                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(failedTransmission, CreateException(statusCode: 408)));
                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(failedTransmission, CreateException(statusCode: 408)));
                Assert.AreSame(failedTransmission, enqueuedTransmission);
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

                Assert.IsNull(enqueuedTransmission);
                Assert.AreEqual(0, transmitter.BackoffLogicManager.ConsecutiveErrors);
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
                var policy = new ErrorHandlingTransmissionPolicy();
                var exception = CreateException(statusCode: 408);
                var transmitter = new StubTransmitter(new TestableBackoffLogicManager(TimeSpan.FromMilliseconds(1)));
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
                    var transmitter = new StubTransmitter (new BackoffLogicManager(TimeSpan.FromMilliseconds(10)))
                    {
                        OnEnqueue = transmission => { enqueuedTransmission = transmission; }
                    };

                    var policy = new ErrorHandlingTransmissionPolicy();
                    policy.Initialize(transmitter);

                    var failedTransmission = new StubTransmission();
                    var response = new HttpWebResponseWrapper {Content = BackendResponseHelper.CreateBackendResponse(2, 1, new[] { "123" })};

                    // Act:
                    transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(failedTransmission, CreateException(statusCode: 408), response));

                    // Assert:
                    var traces = listener.Messages.Where(item => item.Level == EventLevel.Warning).ToList();
                    Assert.AreEqual(2, traces.Count);
                    Assert.AreEqual(23, traces[0].EventId); // failed to send
                    Assert.AreEqual(7, traces[1].EventId); // additional trace
                    Assert.AreEqual("Explanation", traces[1].Payload[0]);
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
                    AssertEx.Contains(exception.Message, (string)error.Payload[1], StringComparison.Ordinal);
                }
            }
            
            private static WebException CreateException(int statusCode)
            {
                string content = BackendResponseHelper.CreateBackendResponse(3,1, new [] {"500"});
                var bytes = Encoding.UTF8.GetBytes(content);
                var responseStream = new MemoryStream();
                responseStream.Write(bytes, 0, bytes.Length);
                responseStream.Seek(0, SeekOrigin.Begin);

#if NETCOREAPP1_1
                System.Net.Http.HttpResponseMessage responseMessage = new System.Net.Http.HttpResponseMessage((HttpStatusCode)statusCode);
                responseMessage.Content = new System.Net.Http.StreamContent(responseStream);
                
                ConstructorInfo ctor = typeof(HttpWebResponse).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)[0];
                HttpWebResponse webResponse = (HttpWebResponse)ctor.Invoke(new object[] { responseMessage, null, null });

                return new WebException("Transmitter Error", null, WebExceptionStatus.UnknownError, webResponse);                
#else
                var mockWebResponse = new Moq.Mock<HttpWebResponse>();
                mockWebResponse.Setup(c => c.GetResponseStream()).Returns(responseStream);

                mockWebResponse.SetupGet<HttpStatusCode>((webRes) => webRes.StatusCode).Returns((HttpStatusCode)statusCode);

                return new WebException("Transmitter Error", null, WebExceptionStatus.UnknownError, mockWebResponse.Object);
#endif        
            }
        }
    }
}