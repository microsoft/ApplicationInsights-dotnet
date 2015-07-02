namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
#if CORE_PCL || NET45 || WINRT
    using System.Diagnostics.Tracing;
#endif
    using System.Globalization;
    using System.Linq;
#if NET35 || NET40
    using Microsoft.Diagnostics.Tracing;
#endif
#if WINDOWS_PHONE || WINDOWS_STORE
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
    using Mocks;

    [TestClass]
    public sealed class DiagnoisticsEventThrottlingManagerTest : IDisposable
    {
        private const uint SampleIntervalInMinutes = 10;

        private const int SampleEventId = 1;
        private const int SampleKeywords = 0;

        private const int ThrottlingStartedEventId = 20;
        private const int ThrottlingResetEventId = 30;

        private readonly DiagnoisticsEventThrottlingMock throttleAllContainer = new DiagnoisticsEventThrottlingMock(
            throttleAll: true,
            signalJustExceeded: true,
            sampleCounters: new Dictionary<int, DiagnoisticsEventCounters>());

        private readonly DiagnoisticsEventThrottlingMock notThrottleContainer = new DiagnoisticsEventThrottlingMock(
            throttleAll: false,
            signalJustExceeded: false,
            sampleCounters: new Dictionary<int, DiagnoisticsEventCounters>());

        private readonly DiagnoisticsEventThrottlingSchedulerMock scheduler = new DiagnoisticsEventThrottlingSchedulerMock();

        private readonly DiagnoisticsEventThrottlingManager<DiagnoisticsEventThrottlingMock> throttleFirstCallManager;
        private readonly DiagnoisticsEventThrottlingManager<DiagnoisticsEventThrottlingMock> notThrottleManager;

        private readonly DiagnosticsEventCollectingMock sender = new DiagnosticsEventCollectingMock();

        private readonly DiagnosticsListener listener;

        public DiagnoisticsEventThrottlingManagerTest()
        {
            this.throttleFirstCallManager = new DiagnoisticsEventThrottlingManager<DiagnoisticsEventThrottlingMock>(
                this.throttleAllContainer,
                this.scheduler,
                SampleIntervalInMinutes);

            this.notThrottleManager = new DiagnoisticsEventThrottlingManager<DiagnoisticsEventThrottlingMock>(
                this.notThrottleContainer,
                this.scheduler,
                SampleIntervalInMinutes);

            this.listener = new DiagnosticsListener(new List<IDiagnosticsSender> { this.sender });

#if SILVERLIGHT
            CoreEventSource.Log.EnableEventListener(this.listener);
#endif
            this.listener.LogLevel = EventLevel.Verbose;
        }

        public void Dispose()
        {
            this.listener.Dispose();
        }

        [TestMethod]
        public void TestEventThrottlingOnFirstCall()
        {
            Assert.IsTrue(this.throttleFirstCallManager.ThrottleEvent(SampleEventId, SampleKeywords));

            var snapshot = this.throttleAllContainer.CollectSnapshot();
            Assert.AreEqual(1, snapshot.Count, "Unexpected number of snapshot records");
            Assert.AreEqual(SampleEventId, snapshot.First().Key, "Incorrect event id in snapshot item");

            Assert.AreEqual(1, this.sender.EventList.Count, "Unexpected count of trace records");

            var evt = this.sender.EventList.First();
            Assert.AreEqual(ThrottlingStartedEventId, evt.MetaData.EventId, "Unexpected trace event id");

            Assert.AreEqual(2, evt.Payload.Length, "Unexpected payload items count");

            Assert.IsNotNull(evt.Payload[0], "Payload item[0] is null");
            Assert.IsInstanceOfType(evt.Payload[0], typeof(int), "Payload item[0] has wrong type");
            var eventId = Convert.ToInt32(evt.Payload[0], CultureInfo.InvariantCulture);
            Assert.AreEqual(SampleEventId, eventId, "Unexpected event Id in payload item");

            Assert.IsNotNull(evt.Payload[1], "Payload item[1] is null");
            Assert.IsInstanceOfType(evt.Payload[1], typeof(string), "Payload item[1] has wrong type");

            Assert.AreEqual(
                "Diagnostics event throttling has been started for the event {0}",
                evt.MetaData.MessageFormat,
                "Unexpected event message format");
        }

        [TestMethod]
        public void TestEventThrottlingIsNotHappeningOnFirstCall()
        {
            Assert.IsFalse(this.notThrottleManager.ThrottleEvent(SampleEventId, SampleKeywords));

            Assert.AreEqual(0, this.sender.EventList.Count, "Unexpected count of trace records");
        }

        [TestMethod]
        public void TestResetThrottling()
        {
            this.notThrottleManager.ThrottleEvent(SampleEventId, SampleKeywords);

            // executing throttling reset routine
            foreach (var item in this.scheduler.Items)
            {
                item.Action();
            }

            Assert.AreEqual(1, this.sender.EventList.Count, "Unexpected count of trace records");

            var evt = this.sender.EventList.First();
            Assert.AreEqual(ThrottlingResetEventId, evt.MetaData.EventId, "Unexpected trace event id");

            Assert.AreEqual(3, evt.Payload.Length, "Unexpected payload items count");

            Assert.IsNotNull(evt.Payload[0], "Payload item[0] is null");
            Assert.IsInstanceOfType(evt.Payload[0], typeof(int), "Payload item[0] has wrong type");
            var eventId = Convert.ToInt32(evt.Payload[0], CultureInfo.InvariantCulture);
            Assert.AreEqual(SampleEventId, eventId, "Unexpected event Id in payload item");

            Assert.IsNotNull(evt.Payload[1], "Payload item[1] is null");
            Assert.IsInstanceOfType(evt.Payload[1], typeof(int), "Payload item[1] has wrong type");
            var executionCount = Convert.ToInt32(evt.Payload[1], CultureInfo.InvariantCulture);
            Assert.AreEqual(1, executionCount, "Unexpected execution count for the event");

            Assert.IsNotNull(evt.Payload[2], "Payload item[2] is null");
            Assert.IsInstanceOfType(evt.Payload[2], typeof(string), "Payload item[2] has wrong type");

            Assert.AreEqual(
                "Diagnostics event throttling has been reset for the event {0}, event was fired {1} times during last interval",
                evt.MetaData.MessageFormat,
                "Unexpected event message format");
        }
    }
}