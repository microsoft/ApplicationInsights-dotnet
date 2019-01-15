using System.Net.Http;

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
                StopsTransmissionSendingForGivenResponseCode(ResponseStatusCodes.RequestTimeout);
            }

            [TestMethod]
            public void StopsTransmissionSendingWhenTransmissionGetServerError()
            {
                StopsTransmissionSendingForGivenResponseCode(ResponseStatusCodes.InternalServerError);
            }

            [TestMethod]
            public void StopsTransmissionSendingWhenTransmissionGetServerUnavailable()
            {
                StopsTransmissionSendingForGivenResponseCode(ResponseStatusCodes.ServiceUnavailable);
            }

            [TestMethod]
            public void StopsTransmissionSendingForUnknownNetworkError()
            {
                StopsTransmissionSendingForGivenResponseCode(ResponseStatusCodes.UnknownNetworkError);
            }

            [TestMethod]
            public void ResumesTransmissionSenderAfterPauseDurationWhenTransmissionTimesOut()
            {
                ResumesTransmissionSenderAfterPauseDuration(ResponseStatusCodes.RequestTimeout);
            }

            [TestMethod]
            public void ResumesTransmissionSenderAfterPauseDurationWhenTransmissionGetServerError()
            {
                ResumesTransmissionSenderAfterPauseDuration(ResponseStatusCodes.InternalServerError);
            }

            [TestMethod]
            public void ResumesTransmissionSenderAfterPauseDurationWhenTransmissionGetServerUnavailable()
            {
                ResumesTransmissionSenderAfterPauseDuration(ResponseStatusCodes.ServiceUnavailable);
            }

            [TestMethod]
            public void ResumesTransmissionSenderAfterPauseDurationForUnknownNetworkError()
            {
                ResumesTransmissionSenderAfterPauseDuration(ResponseStatusCodes.UnknownNetworkError);
            }

            [TestMethod]
            public void KeepsTransmissionSenderPausedWhenAdditionalTransmissionsFail()
            {
                var transmitter = new StubTransmitter(new TestableBackoffLogicManager(TimeSpan.FromMinutes(1)));
                var policy = new ErrorHandlingTransmissionPolicy();
                policy.Initialize(transmitter);
                
                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(new StubTransmission(), new Exception("Error"), new HttpWebResponseWrapper() {StatusCode = ResponseStatusCodes.InternalServerError}));
                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(new StubTransmission(), new Exception("Error"), new HttpWebResponseWrapper() { StatusCode = ResponseStatusCodes.InternalServerError }));

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
                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(failedTransmission, new Exception("Error"), new HttpWebResponseWrapper() { StatusCode = ResponseStatusCodes.InternalServerError }));

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
                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(failedTransmission, new Exception("Error"), new HttpWebResponseWrapper() { StatusCode = ResponseStatusCodes.InternalServerError }));
                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(failedTransmission, new Exception("Error"), new HttpWebResponseWrapper() { StatusCode = ResponseStatusCodes.InternalServerError }));
                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(failedTransmission, new Exception("Error"), new HttpWebResponseWrapper() { StatusCode = ResponseStatusCodes.InternalServerError }));
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
                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(successfulTransmission,null, new HttpWebResponseWrapper(){StatusCode = 200}));

                Assert.IsNull(enqueuedTransmission);
                Assert.AreEqual(0, transmitter.BackoffLogicManager.ConsecutiveErrors);
            }

            [TestMethod]
            public void DoesNotRetryTransmissionForUnknownResponseCode()
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
                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(successfulTransmission, null, null));

                Assert.IsNull(enqueuedTransmission);
                Assert.AreEqual(0, transmitter.BackoffLogicManager.ConsecutiveErrors);
            }

            [TestMethod]
            public void CatchesAndLogsSynchronousExceptionsThrownByTransmitterWhenPausingTransmission()
            {
                var policy = new ErrorHandlingTransmissionPolicy();
                var exception = new HttpRequestException("http request error");
                var transmitter = new StubTransmitter { OnApplyPolicies = () => { throw exception; } };
                CatchesAndLogsExceptionThrownByTransmitter(policy, transmitter, exception);
            }

            [TestMethod, Timeout(1000)]
            public void CatchesAndLogsAsynchronousExceptionsThrownByTransmitterWhenPausingTransmission()
            {
                var policy = new ErrorHandlingTransmissionPolicy();
                var exception = new HttpRequestException("http request error");
                var transmitter = new StubTransmitter { OnApplyPolicies = () => ThrowAsync(exception) };
                CatchesAndLogsExceptionThrownByTransmitter(policy, transmitter, exception);
            }

            [TestMethod, Timeout(1000)]
            public void CatchesAndLogsSynchronousExceptionsThrownByTransmitterWhenResumingTransmission()
            {
                var policy = new ErrorHandlingTransmissionPolicy();
                var exception = new HttpRequestException("http request error");
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
            public void LogsDataLossEventsWhenExceptionisNotNull()
            {
                using (var listener = new TestEventListener())
                {
                    // Arrange:
                    const long AllKeywords = -1;
                    listener.EnableEvents(TelemetryChannelEventSource.Log, EventLevel.LogAlways, (EventKeywords)AllKeywords);

                    Transmission enqueuedTransmission = null;
                    var transmitter = new StubTransmitter(new BackoffLogicManager(TimeSpan.FromMilliseconds(10)))
                    {
                        OnEnqueue = transmission => { enqueuedTransmission = transmission; }
                    };

                    var policy = new ErrorHandlingTransmissionPolicy();
                    policy.Initialize(transmitter);

                    var failedTransmission = new StubTransmission();                    

                    // Act:
                    transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(failedTransmission, new Exception("Data loss"), null));

                    // Assert:
                    var traces = listener.Messages.Where(item => item.Level == EventLevel.Error).ToList();
                    Assert.AreEqual(1, traces.Count);
                    Assert.AreEqual(69, traces[0].EventId); // failed to send
                    Assert.AreEqual("Data loss", traces[0].Payload[1]);
                }
            }

            [TestMethod]
            public void LogsDataLossEventsWhenExceptionisNull()
            {
                using (var listener = new TestEventListener())
                {
                    // Arrange:
                    const long AllKeywords = -1;
                    listener.EnableEvents(TelemetryChannelEventSource.Log, EventLevel.LogAlways, (EventKeywords)AllKeywords);

                    Transmission enqueuedTransmission = null;
                    var transmitter = new StubTransmitter(new BackoffLogicManager(TimeSpan.FromMilliseconds(10)))
                    {
                        OnEnqueue = transmission => { enqueuedTransmission = transmission; }
                    };

                    var policy = new ErrorHandlingTransmissionPolicy();
                    policy.Initialize(transmitter);

                    var failedTransmission = new StubTransmission();

                    // Act:
                    transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(failedTransmission, null, null));

                    // Assert:
                    var traces = listener.Messages.Where(item => item.Level == EventLevel.Error).ToList();
                    Assert.AreEqual(1, traces.Count);
                    Assert.AreEqual(69, traces[0].EventId); // failed to send
                    Assert.AreEqual("Unknown Exception Message", traces[0].Payload[1]);
                }
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
                    transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(failedTransmission, null, response));

                    // Assert:
                    var traces = listener.Messages.Where(item => item.Level == EventLevel.Warning).ToList();
                    Assert.AreEqual(1, traces.Count);
                    Assert.AreEqual(7, traces[0].EventId); // failed to send                    
                    Assert.AreEqual("Explanation", traces[0].Payload[0]);
                }
            }

            private static Task ThrowAsync(Exception e)
            {
                var tcs = new TaskCompletionSource<object>(null);
                tcs.SetException(e);
                return tcs.Task;
            }

            private void StopsTransmissionSendingForGivenResponseCode(int responseStatusCode)
            {
                var policyApplied = new AutoResetEvent(false);
                var transmitter = new StubTransmitter(new TestableBackoffLogicManager(TimeSpan.FromSeconds(10)));
                transmitter.OnApplyPolicies = () =>
                {
                    policyApplied.Set();
                };

                var policy = new ErrorHandlingTransmissionPolicy();
                policy.Initialize(transmitter);

                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(new StubTransmission(), new Exception("Error"), new HttpWebResponseWrapper() { StatusCode = responseStatusCode }));


                Assert.IsTrue(policyApplied.WaitOne(100));
                Assert.AreEqual(0, policy.MaxSenderCapacity);
            }

            private void ResumesTransmissionSenderAfterPauseDuration(int responseStatusCode)
            {
                var policyApplied = new AutoResetEvent(false);
                var transmitter = new StubTransmitter(new TestableBackoffLogicManager(TimeSpan.FromMilliseconds(1)));
                transmitter.OnApplyPolicies = () => { policyApplied.Set(); };

                var policy = new ErrorHandlingTransmissionPolicy();
                policy.Initialize(transmitter);

                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(new StubTransmission(),
                    new Exception("Error"),
                    new HttpWebResponseWrapper() {StatusCode = responseStatusCode }));

                Assert.IsTrue(policyApplied.WaitOne(100));
                Assert.IsTrue(policyApplied.WaitOne(100));
                Assert.IsNull(policy.MaxSenderCapacity);
            }

            private static void CatchesAndLogsExceptionThrownByTransmitter(ErrorHandlingTransmissionPolicy policy, StubTransmitter transmitter, Exception exception)
            {
                policy.Initialize(transmitter);

                using (var listener = new TestEventListener())
                {
                    const long AllKeywords = -1;
                    listener.EnableEvents(TelemetryChannelEventSource.Log, EventLevel.Warning, (EventKeywords)AllKeywords);

                    transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(new StubTransmission(), null, new HttpWebResponseWrapper(){StatusCode = ResponseStatusCodes.UnknownNetworkError}));

                    EventWrittenEventArgs error = listener.Messages.First(args => args.EventId == 45);
                    AssertEx.Contains(exception.Message, (string)error.Payload[0], StringComparison.Ordinal);
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