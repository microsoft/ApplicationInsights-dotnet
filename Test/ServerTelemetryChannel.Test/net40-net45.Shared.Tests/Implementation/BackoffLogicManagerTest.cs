namespace Microsoft.ApplicationInsights.WindowsServer.Channel.Implementation
{
    using System;
#if !NET40
    using System.Diagnostics.Tracing;
#endif

    using System.Globalization;
    using System.Net;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights.WindowsServer.Channel.Helpers;

#if NET40
    using Microsoft.Diagnostics.Tracing;
#endif

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Microsoft.ApplicationInsights.Channel.Implementation;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation;
    
    

#if !NET40
    using TaskEx = System.Threading.Tasks.Task;
#endif

    public class BackoffLogicManagerTest
    {
        [TestClass]
        public class DefaultBackoffEnabledReportingInterval
        {
            [TestMethod]
            public void DefaultReportingIntervalInMinIs30Min()
            {
                Assert.AreEqual(30, new BackoffLogicManager().DefaultBackoffEnabledReportingInterval.TotalMinutes);
            }
        }

        [TestClass]
        public class GetBackendResponse
        {
            [TestMethod]
            public void ReturnNullIfArgumentIsNull()
            {
                var manager = new BackoffLogicManager(TimeSpan.Zero);
                Assert.IsNull(manager.GetBackendResponse(null));
            }

            [TestMethod]
            public void ReturnNullIfArgumentEmpty()
            {
                var manager = new BackoffLogicManager(TimeSpan.Zero);
                Assert.IsNull(manager.GetBackendResponse(string.Empty));
            }

            [TestMethod]
            public void IfContentCannotBeParsedNullIsReturned()
            {
                var manager = new BackoffLogicManager(TimeSpan.Zero);
                Assert.IsNull(manager.GetBackendResponse("ab}{"));
            }

            [TestMethod]
            public void IfContentIsUnexpectedJsonNullIsReturned()
            {
                var manager = new BackoffLogicManager(TimeSpan.Zero);
                Assert.IsNull(manager.GetBackendResponse("[1,2]"));
            }

            [TestMethod]
            public void BackendResponseIsReturnedForCorrectContent()
            {
                string content = BackendResponseHelper.CreateBackendResponse(itemsReceived: 100, itemsAccepted: 1, errorCodes: new[] {"206"}, indexStartWith: 84);

                var manager = new BackoffLogicManager(TimeSpan.Zero);
                var backendResponse = manager.GetBackendResponse(content);

                Assert.AreEqual(1, backendResponse.ItemsAccepted);
                Assert.AreEqual(100, backendResponse.ItemsReceived);
                Assert.AreEqual(1, backendResponse.Errors.Length); // Even though accepted number of items is 1 out of 99 we get only 1 error back. We do not expect same in production but SDK should handle it correctly.
                Assert.AreEqual(84, backendResponse.Errors[0].Index);
                Assert.AreEqual(206, backendResponse.Errors[0].StatusCode);
                Assert.AreEqual("Explanation", backendResponse.Errors[0].Message);
            }
        }

        [TestClass]
        public class ScheduleRestore
        {
            [TestMethod]
            public void NoErrorDelayIsSameAsSlotDelay()
            {
                var manager = new BackoffLogicManager(TimeSpan.Zero);
                manager.GetBackOffTimeInterval(string.Empty);
                Assert.AreEqual(TimeSpan.FromSeconds(10), manager.CurrentDelay);
            }

            [TestMethod]
            public void FirstErrorDelayIsSameAsSlotDelay()
            {
                var manager = new BackoffLogicManager(TimeSpan.Zero);
                manager.ReportBackoffEnabled(500);
                manager.GetBackOffTimeInterval(string.Empty);
                Assert.AreEqual(TimeSpan.FromSeconds(10), manager.CurrentDelay);
            }

            [TestMethod]
            public void UpperBoundOfDelayIsMaxDelay()
            {
                var manager = new BackoffLogicManager(TimeSpan.Zero, TimeSpan.Zero);

                PrivateObject wrapper = new PrivateObject(manager);
                wrapper.SetField("consecutiveErrors", int.MaxValue);

                manager.GetBackOffTimeInterval(string.Empty);

                Assert.InRange(manager.CurrentDelay, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(3600));
            }

            [TestMethod]
            public void RetryAfterFromHeadersHasMorePriorityThanExponentialRetry()
            {                
                var manager = new BackoffLogicManager(TimeSpan.Zero);
                manager.GetBackOffTimeInterval(DateTimeOffset.UtcNow.AddSeconds(30).ToString("O"));

                Assert.InRange(manager.CurrentDelay, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(30));
            }

            [TestMethod]
            public void AssertIfDateParseErrorCausesDefaultDelay()
            {
                var manager = new BackoffLogicManager(TimeSpan.Zero);
                manager.GetBackOffTimeInterval("no one can parse me");
                Assert.AreEqual(TimeSpan.FromSeconds(10), manager.CurrentDelay);
            }

            [TestMethod]
            public void RetryAfterOlderThanNowCausesDefaultDelay()
            {
                // An old date
                string retryAfterDateString = DateTime.Now.AddMinutes(-1).ToString("R", CultureInfo.InvariantCulture);

                var manager = new BackoffLogicManager(TimeSpan.Zero);
                manager.GetBackOffTimeInterval(retryAfterDateString);
                Assert.AreEqual(TimeSpan.FromSeconds(10), manager.CurrentDelay);
            }
        }

        [TestClass]
        public class ReportDiagnosticMessage
        {
            [TestMethod]
            public void ReportBackoffWriteMessageOnce()
            {
                using (var listener = new TestEventListener())
                {
                    const long AllKeywords = -1;
                    listener.EnableEvents(TelemetryChannelEventSource.Log, EventLevel.Error, (EventKeywords)AllKeywords);

                    var manager = new BackoffLogicManager(TimeSpan.Zero);
                    manager.ReportBackoffEnabled(200);
                    manager.ReportBackoffEnabled(200);

                    var traces = listener.Messages.ToList();

                    Assert.AreEqual(1, traces.Count);
                    Assert.AreEqual(2, traces[0].EventId);
                }
            }

            [TestMethod]
            public void ReportBackoffWriteDoesNotLogMessagesBeforeIntervalPasses()
            {
                // this test fails when run in parallel with other tests
                using (var listener = new TestEventListener(waitForDelayedEvents: false))
                {
                    const long AllKeywords = -1;
                    listener.EnableEvents(TelemetryChannelEventSource.Log, EventLevel.Error, (EventKeywords)AllKeywords);

                    var manager = new BackoffLogicManager(TimeSpan.FromSeconds(20));

                    manager.ReportBackoffEnabled(200);
                    manager.ReportBackoffEnabled(200);

                    var traces = listener.Messages.ToList();

                    Assert.AreEqual(0, traces.Count);
                }
            }

            [TestMethod]
            public void ReportBackoffWritesLogMessagesAfterIntervalPasses()
            {
                using (var listener = new TestEventListener())
                {
                    const long AllKeywords = -1;
                    listener.EnableEvents(TelemetryChannelEventSource.Log, EventLevel.Error, (EventKeywords)AllKeywords);

                    var manager = new BackoffLogicManager(TimeSpan.FromMilliseconds(10));

                    System.Threading.Thread.Sleep(10);

                    manager.ReportBackoffEnabled(200);
                    manager.ReportBackoffEnabled(200);

                    var traces = listener.Messages.ToList();

                    Assert.AreEqual(1, traces.Count);
                }
            }

            [TestMethod]
            public void ReportBackoffWriteIsLoggedAgainAfterReportDisabledWasCalled()
            {
                using (var listener = new TestEventListener())
                {
                    const long AllKeywords = -1;
                    listener.EnableEvents(TelemetryChannelEventSource.Log, EventLevel.Error, (EventKeywords)AllKeywords);

                    var manager = new BackoffLogicManager(TimeSpan.Zero);

                    manager.ReportBackoffEnabled(200);
                    manager.ReportBackoffEnabled(200);

                    manager.ReportBackoffDisabled();
                    manager.ReportBackoffDisabled();

                    manager.ReportBackoffEnabled(200);
                    manager.ReportBackoffEnabled(200);

                    var traces = listener.Messages.ToList();
                    Assert.AreEqual(3, traces.Count);
                    Assert.AreEqual(2, traces[0].EventId);
                    Assert.AreEqual(1, traces[1].EventId);
                    Assert.AreEqual(2, traces[2].EventId);
                }
            }

            [TestMethod]
            public void DisableDoesNotLogMessageIfEnabledWasNotCalled()
            {
                // this test may fail when other tests running in parallel
                using (var listener = new TestEventListener(waitForDelayedEvents: false))
                {
                    const long AllKeywords = -1;
                    listener.EnableEvents(TelemetryChannelEventSource.Log, EventLevel.Error, (EventKeywords)AllKeywords);

                    var manager = new BackoffLogicManager(TimeSpan.Zero);

                    manager.ReportBackoffDisabled();
                    manager.ReportBackoffDisabled();

                    var traces = listener.Messages.ToList();
                    Assert.AreEqual(0, traces.Count);
                }
            }
        }

        [TestClass]
        public class ConsecutiveErrors
        {
            [TestMethod]
            public void DoNotIncrementConsecutiveErrorsMoreOftenThanOnceInminIntervalToUpdateConsecutiveErrors()
            {
                BackoffLogicManager manager = new BackoffLogicManager(TimeSpan.Zero, TimeSpan.FromDays(1));

                Task[] tasks = new Task[10];
                for (int i = 0; i < 10; ++i)
                {
                    tasks[i] = TaskEx.Run(() => manager.ReportBackoffEnabled(500));
                }

                Task.WaitAll(tasks);

                Assert.AreEqual(1, manager.ConsecutiveErrors);
            }

            [TestMethod]
            public void IncrementConsecutiveErrorsAfterMinIntervalToUpdateConsecutiveErrorsPassed()
            {
                BackoffLogicManager manager = new BackoffLogicManager(TimeSpan.Zero, TimeSpan.FromMilliseconds(1));

                manager.ReportBackoffEnabled(500);
                Thread.Sleep(1);
                manager.ReportBackoffEnabled(500);

                Assert.AreEqual(2, manager.ConsecutiveErrors);
            }

            [TestMethod]
            public void ConsecutiveErrorsCanAlwaysBeResetTo0()
            {
                BackoffLogicManager manager = new BackoffLogicManager(TimeSpan.Zero, TimeSpan.FromDays(1));

                manager.ReportBackoffEnabled(500);
                manager.ResetConsecutiveErrors();

                Assert.AreEqual(0, manager.ConsecutiveErrors);
            }
        }
    }
}
