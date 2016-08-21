namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests the rich payload event source tracking.
    /// </summary>
    [TestClass]
    public class RichPayloadEventSourceTest
    {
        /// <summary>
        /// Tests tracking request telemetry.
        /// </summary>
        [TestMethod]
        public void RichPayloadEventSourceRequestSentTest()
        {
            this.DoTracking(
                RichPayloadEventSource.Keywords.Requests,
                new RequestTelemetry("TestRequest", DateTimeOffset.Now, TimeSpan.FromMilliseconds(10), "200", true),
                (client, item) => { client.TrackRequest((RequestTelemetry)item); });
        }

        /// <summary>
        /// Tests tracking trace telemetry.
        /// </summary>
        [TestMethod]
        public void RichPayloadEventSourceTraceSentTest()
        {
            this.DoTracking(
                RichPayloadEventSource.Keywords.Traces,
                new TraceTelemetry("TestTrace", SeverityLevel.Information),
                (client, item) => { client.TrackTrace((TraceTelemetry)item); });
        }

        /// <summary>
        /// Tests tracking event telemetry.
        /// </summary>
        [TestMethod]
        public void RichPayloadEventSourceEventSentTest()
        {
            this.DoTracking(
                RichPayloadEventSource.Keywords.Events,
                new EventTelemetry("TestEvent"),
                (client, item) => { client.TrackEvent((EventTelemetry)item); });
        }

        /// <summary>
        /// Tests tracking exception telemetry.
        /// </summary>
        [TestMethod]
        public void RichPayloadEventSourceExceptionSentTest()
        {
            this.DoTracking(
                RichPayloadEventSource.Keywords.Exceptions,
                new ExceptionTelemetry(new SystemException("Test")),
                (client, item) => { client.TrackException((ExceptionTelemetry)item); });
        }

        /// <summary>
        /// Tests tracking metric telemetry.
        /// </summary>
        [TestMethod]
        public void RichPayloadEventSourceMetricSentTest()
        {
            this.DoTracking(
                RichPayloadEventSource.Keywords.Metrics,
                new MetricTelemetry("TestMetric", 1),
                (client, item) => { client.TrackMetric((MetricTelemetry)item); });
        }

        /// <summary>
        /// Tests tracking dependency telemetry.
        /// </summary>
        [TestMethod]
        public void RichPayloadEventSourceDependencySentTest()
        {
            this.DoTracking(
                RichPayloadEventSource.Keywords.Dependencies,
                new DependencyTelemetry("Custom", "Target", "TestDependency", "TestCommand", DateTimeOffset.Now, TimeSpan.Zero, "200", true),
                (client, item) => { client.TrackDependency((DependencyTelemetry)item); });
        }

        /// <summary>
        /// Tests tracking page view telemetry.
        /// </summary>
        [TestMethod]
        public void RichPayloadEventSourcePageViewSentTest()
        {
            this.DoTracking(
                RichPayloadEventSource.Keywords.PageViews,
                new PageViewTelemetry("TestPage"),
                (client, item) => { client.TrackPageView((PageViewTelemetry)item); });
        }

        /// <summary>
        /// Helper method to setup shared context and call the desired tracking for testing.
        /// </summary>
        /// <param name="keywords">The event keywords to enable.</param>
        /// <param name="item">The telemetry item to track.</param>
        /// <param name="track">The tracking callback to execute.</param>
        private void DoTracking(EventKeywords keywords, ITelemetry item, Action<TelemetryClient, ITelemetry> track)
        {
            var client = new TelemetryClient();
            client.InstrumentationKey = Guid.NewGuid().ToString();

            using (var listener = new Microsoft.ApplicationInsights.TestFramework.TestEventListener())
            {
                listener.EnableEvents(RichPayloadEventSource.Log.EventSourceInternal, EventLevel.Verbose, keywords);

                item.Context.Properties.Add("property1", "value1");
                item.Context.User.Id = "testUserId";
                item.Context.Operation.Id = Guid.NewGuid().ToString();

                track(client, item);

                var actualEvent = listener.Messages.FirstOrDefault();

                Assert.IsNotNull(actualEvent);
                Assert.AreEqual(client.InstrumentationKey, actualEvent.Payload[0]);

                int keysFound = 0;
                object[] tags = actualEvent.Payload[1] as object[];
                foreach (object tagObject in tags)
                {
                    Dictionary<string, object> tag = (Dictionary<string, object>)tagObject;
                    Assert.IsNotNull(tag);
                    string key = (string)tag["Key"];
                    object value = tag["Value"];
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        if (key == "ai.user.id")
                        {
                            Assert.AreEqual("testUserId", value);
                            ++keysFound;
                        }
                        else if (key == "ai.operation.id")
                        {
                            Assert.AreEqual(item.Context.Operation.Id, value);
                            ++keysFound;
                        }
                    }
                }

                Assert.AreEqual(2, keysFound);
                Assert.IsNotNull(actualEvent.Payload[2]);
            }
        }
    }
}