namespace Microsoft.ApplicationInsights.WindowsServer.Channel.Implementation
{
    using System;
#if !NET40
    using System.Diagnostics.Tracing;
#endif

    using System.Globalization;
    using System.Net;
    using System.Linq;

    using Microsoft.ApplicationInsights.WindowsServer.Channel.Helpers;

#if NET40
    using Microsoft.Diagnostics.Tracing;
#endif

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Microsoft.ApplicationInsights.Channel.Implementation;
    using Microsoft.ApplicationInsights.Web.TestFramework;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation;
    
    using Assert = Xunit.Assert;

    public class BackoffLogicManagerTest
    {
        [TestClass]
        public class GetBackendResponse
        {
            [TestMethod]
            public void ReturnNullIfArgumentIsNull()
            {
                var manager = new BackoffLogicManager(TimeSpan.Zero);
                Assert.Null(manager.GetBackendResponse(null));
            }

            [TestMethod]
            public void ReturnNullIfArgumentEmpty()
            {
                var manager = new BackoffLogicManager(TimeSpan.Zero);
                Assert.Null(manager.GetBackendResponse(string.Empty));
            }

            [TestMethod]
            public void IfContentCannotBeParsedNullIsReturned()
            {
                var manager = new BackoffLogicManager(TimeSpan.Zero);
                Assert.Null(manager.GetBackendResponse("ab}{"));
            }

            [TestMethod]
            public void IfContentIsUnexpectedJsonNullIsReturned()
            {
                var manager = new BackoffLogicManager(TimeSpan.Zero);
                Assert.Null(manager.GetBackendResponse("[1,2]"));
            }

            [TestMethod]
            public void BackendResponseIsReturnedForCorrectContent()
            {
                string content = BackendResponseHelper.CreateBackendResponse(itemsReceived: 100, itemsAccepted: 1, errorCodes: new[] {"206"}, indexStartWith: 84);

                var manager = new BackoffLogicManager(TimeSpan.Zero);
                var backendResponse = manager.GetBackendResponse(content);

                Assert.Equal(1, backendResponse.ItemsAccepted);
                Assert.Equal(100, backendResponse.ItemsReceived);
                Assert.Equal(1, backendResponse.Errors.Length);
                Assert.Equal(84, backendResponse.Errors[0].Index);
                Assert.Equal(206, backendResponse.Errors[0].StatusCode);
                Assert.Equal("Explanation", backendResponse.Errors[0].Message);
            }
        }

        [TestClass]
        public class ScheduleRestore
        {
            [TestMethod]
            public void NoErrorDelayIsSameAsSlotDelay()
            {
                var manager = new BackoffLogicManager(TimeSpan.Zero);
                manager.ScheduleRestore(string.Empty, () => null);
                Assert.Equal(TimeSpan.FromSeconds(10), manager.CurrentDelay);
            }

            [TestMethod]
            public void FirstErrorDelayIsSameAsSlotDelay()
            {
                var manager = new BackoffLogicManager(TimeSpan.Zero);
                manager.ConsecutiveErrors++;
                manager.ScheduleRestore(string.Empty, () => null);
                Assert.Equal(TimeSpan.FromSeconds(10), manager.CurrentDelay);
            }

            [TestMethod]
            public void UpperBoundOfDelayIsMaxDelay()
            {
                var manager = new BackoffLogicManager(TimeSpan.Zero) { ConsecutiveErrors = int.MaxValue };
                manager.ScheduleRestore(string.Empty, () => null);
                Assert.InRange(manager.CurrentDelay, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(3600));
            }

            [TestMethod]
            public void RetryAfterFromHeadersHasMorePriorityThanExponentialRetry()
            {
                WebHeaderCollection headers = new WebHeaderCollection();
                headers.Add("Retry-After", DateTimeOffset.UtcNow.AddSeconds(30).ToString("O"));

                var manager = new BackoffLogicManager(TimeSpan.Zero);
                manager.ScheduleRestore(headers, () => null);

                Xunit.Assert.InRange(manager.CurrentDelay, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(30));
            }

            [TestMethod]
            public void AssertIfDateParseErrorCausesDefaultDelay()
            {
                WebHeaderCollection headers = new WebHeaderCollection();
                headers.Add("Retry-After", "no one can parse me");

                var manager = new BackoffLogicManager(TimeSpan.Zero);
                manager.ScheduleRestore(headers, () => null);
                Assert.Equal(TimeSpan.FromSeconds(10), manager.CurrentDelay);
            }

            [TestMethod]
            public void RetryAfterOlderThanNowCausesDefaultDelay()
            {
                // An old date
                string retryAfterDateString = DateTime.Now.AddMinutes(-1).ToString("R", CultureInfo.InvariantCulture);

                WebHeaderCollection headers = new WebHeaderCollection();
                headers.Add("Retry-After", retryAfterDateString);

                var manager = new BackoffLogicManager(TimeSpan.Zero);
                manager.ScheduleRestore(headers, () => null);
                Assert.Equal(TimeSpan.FromSeconds(10), manager.CurrentDelay);
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

                    Assert.Equal(1, traces.Count);
                    Assert.Equal(2, traces[0].EventId);
                }
            }

            [TestMethod]
            public void ReportBackoffWriteDoesNotLogMessagesBeforeIntervalPasses()
            {
                using (var listener = new TestEventListener())
                {
                    const long AllKeywords = -1;
                    listener.EnableEvents(TelemetryChannelEventSource.Log, EventLevel.Error, (EventKeywords)AllKeywords);

                    var manager = new BackoffLogicManager(TimeSpan.FromSeconds(20));

                    manager.ReportBackoffEnabled(200);
                    manager.ReportBackoffEnabled(200);

                    var traces = listener.Messages.ToList();

                    Assert.Equal(0, traces.Count);
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

                    Assert.Equal(1, traces.Count);
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
                    Assert.Equal(3, traces.Count);
                    Assert.Equal(2, traces[0].EventId);
                    Assert.Equal(1, traces[1].EventId);
                    Assert.Equal(2, traces[2].EventId);
                }
            }

            [TestMethod]
            public void DisableDoesNotLogMessageIfEnabledWasNotCalled()
            {
                using (var listener = new TestEventListener())
                {
                    const long AllKeywords = -1;
                    listener.EnableEvents(TelemetryChannelEventSource.Log, EventLevel.Error, (EventKeywords)AllKeywords);

                    var manager = new BackoffLogicManager(TimeSpan.Zero);

                    manager.ReportBackoffDisabled();
                    manager.ReportBackoffDisabled();

                    var traces = listener.Messages.ToList();
                    Assert.Equal(0, traces.Count);
                }
            }
        }
    }
}
