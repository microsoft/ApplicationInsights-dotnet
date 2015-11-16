namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using System.Linq;

    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]

    public class RichPayloadEventSourceTest
    {
        [TestMethod]
        public void RichPayloadEventSourceRequestSentTest()
        {
            var client = new TelemetryClient();
            client.InstrumentationKey = Guid.NewGuid().ToString();

            using (var listener = new Microsoft.ApplicationInsights.TestFramework.TestEventListener())
            {
                listener.EnableEvents(RichPayloadEventSource.Log.eventSource, EventLevel.Verbose, RichPayloadEventSource.Keywords.Requests);

                var item = new RequestTelemetry("TestRequest", DateTimeOffset.Now, TimeSpan.FromMilliseconds(10), "200", true);
                item.Context.Properties.Add("property1", "value1");
                item.Context.User.Id = "testUserId";
                item.Context.Operation.Id = Guid.NewGuid().ToString();

                client.TrackRequest(item);

                var actualEvent = listener.Messages.FirstOrDefault();

                Assert.IsNotNull(actualEvent);
                Assert.AreEqual(client.InstrumentationKey, actualEvent.Payload[0]);

                object[] tags = actualEvent.Payload[1] as object[];
                Assert.AreEqual("ai.user.id", ((Dictionary<string, object>)(tags[0]))["Key"]);
                Assert.AreEqual("testUserId", ((Dictionary<string, object>)(tags[0]))["Value"]);

                Assert.AreEqual("ai.operation.id", ((Dictionary<string, object>)(tags[1]))["Key"]);
                Assert.AreEqual(item.Context.Operation.Id, ((Dictionary<string, object>)(tags[1]))["Value"]);

                Assert.IsNotNull(actualEvent.Payload[2]);
            }
        }

        [TestMethod]
        public void RichPayloadEventSourceTraceSentTest()
        {
            var client = new TelemetryClient();
            client.InstrumentationKey = Guid.NewGuid().ToString();

            using (var listener = new Microsoft.ApplicationInsights.TestFramework.TestEventListener())
            {
                listener.EnableEvents(RichPayloadEventSource.Log.eventSource, EventLevel.Verbose, RichPayloadEventSource.Keywords.Traces);

                var item = new TraceTelemetry("TestTrace", SeverityLevel.Information);
                item.Context.Properties.Add("property1", "value1");
                item.Context.User.Id = "testUserId";
                item.Context.Operation.Id = Guid.NewGuid().ToString();

                client.TrackTrace(item);

                var actualEvent = listener.Messages.FirstOrDefault();

                Assert.IsNotNull(actualEvent);
                Assert.AreEqual(client.InstrumentationKey, actualEvent.Payload[0]);

                object[] tags = actualEvent.Payload[1] as object[];
                Assert.AreEqual("ai.user.id", ((Dictionary<string, object>)(tags[0]))["Key"]);
                Assert.AreEqual("testUserId", ((Dictionary<string, object>)(tags[0]))["Value"]);

                Assert.AreEqual("ai.operation.id", ((Dictionary<string, object>)(tags[1]))["Key"]);
                Assert.AreEqual(item.Context.Operation.Id, ((Dictionary<string, object>)(tags[1]))["Value"]);

                Assert.IsNotNull(actualEvent.Payload[2]);
            }
        }

        [TestMethod]
        public void RichPayloadEventSourceEventSentTest()
        {
            var client = new TelemetryClient();
            client.InstrumentationKey = Guid.NewGuid().ToString();

            using (var listener = new Microsoft.ApplicationInsights.TestFramework.TestEventListener())
            {
                listener.EnableEvents(RichPayloadEventSource.Log.eventSource, EventLevel.Verbose, RichPayloadEventSource.Keywords.Events);

                var item = new EventTelemetry("TestEvent");
                item.Context.Properties.Add("property1", "value1");
                item.Context.User.Id = "testUserId";
                item.Context.Operation.Id = Guid.NewGuid().ToString();

                client.TrackEvent(item);

                var actualEvent = listener.Messages.FirstOrDefault();

                Assert.IsNotNull(actualEvent);
                Assert.AreEqual(client.InstrumentationKey, actualEvent.Payload[0]);

                object[] tags = actualEvent.Payload[1] as object[];
                Assert.AreEqual("ai.user.id", ((Dictionary<string, object>)(tags[0]))["Key"]);
                Assert.AreEqual("testUserId", ((Dictionary<string, object>)(tags[0]))["Value"]);

                Assert.AreEqual("ai.operation.id", ((Dictionary<string, object>)(tags[1]))["Key"]);
                Assert.AreEqual(item.Context.Operation.Id, ((Dictionary<string, object>)(tags[1]))["Value"]);

                Assert.IsNotNull(actualEvent.Payload[2]);
            }
        }

        [TestMethod]
        public void RichPayloadEventSourceExceptionSentTest()
        {
            var client = new TelemetryClient();
            client.InstrumentationKey = Guid.NewGuid().ToString();

            using (var listener = new Microsoft.ApplicationInsights.TestFramework.TestEventListener())
            {
                listener.EnableEvents(RichPayloadEventSource.Log.eventSource, EventLevel.Verbose, RichPayloadEventSource.Keywords.Exceptions);

                var item = new ExceptionTelemetry(new SystemException("Test"));
                item.Context.Properties.Add("property1", "value1");
                item.Context.User.Id = "testUserId";
                item.Context.Operation.Id = Guid.NewGuid().ToString();

                client.TrackException(item);

                var actualEvent = listener.Messages.FirstOrDefault();

                Assert.IsNotNull(actualEvent);
                Assert.AreEqual(client.InstrumentationKey, actualEvent.Payload[0]);

                object[] tags = actualEvent.Payload[1] as object[];
                Assert.AreEqual("ai.user.id", ((Dictionary<string, object>)(tags[0]))["Key"]);
                Assert.AreEqual("testUserId", ((Dictionary<string, object>)(tags[0]))["Value"]);

                Assert.AreEqual("ai.operation.id", ((Dictionary<string, object>)(tags[1]))["Key"]);
                Assert.AreEqual(item.Context.Operation.Id, ((Dictionary<string, object>)(tags[1]))["Value"]);

                Assert.IsNotNull(actualEvent.Payload[2]);
            }
        }

        [TestMethod]
        public void RichPayloadEventSourceMetricSentTest()
        {
            var client = new TelemetryClient();
            client.InstrumentationKey = Guid.NewGuid().ToString();

            using (var listener = new Microsoft.ApplicationInsights.TestFramework.TestEventListener())
            {
                listener.EnableEvents(RichPayloadEventSource.Log.eventSource, EventLevel.Verbose, RichPayloadEventSource.Keywords.Metrics);

                var item = new MetricTelemetry("TestMetric", 1);
                item.Context.Properties.Add("property1", "value1");
                item.Context.User.Id = "testUserId";
                item.Context.Operation.Id = Guid.NewGuid().ToString();

                client.TrackMetric(item);

                var actualEvent = listener.Messages.FirstOrDefault();

                Assert.IsNotNull(actualEvent);
                Assert.AreEqual(client.InstrumentationKey, actualEvent.Payload[0]);

                object[] tags = actualEvent.Payload[1] as object[];
                Assert.AreEqual("ai.user.id", ((Dictionary<string, object>)(tags[0]))["Key"]);
                Assert.AreEqual("testUserId", ((Dictionary<string, object>)(tags[0]))["Value"]);

                Assert.AreEqual("ai.operation.id", ((Dictionary<string, object>)(tags[1]))["Key"]);
                Assert.AreEqual(item.Context.Operation.Id, ((Dictionary<string, object>)(tags[1]))["Value"]);

                Assert.IsNotNull(actualEvent.Payload[2]);
            }
        }

        [TestMethod]
        public void RichPayloadEventSourceDependencySentTest()
        {
            var client = new TelemetryClient();
            client.InstrumentationKey = Guid.NewGuid().ToString();

            using (var listener = new Microsoft.ApplicationInsights.TestFramework.TestEventListener())
            {
                listener.EnableEvents(RichPayloadEventSource.Log.eventSource, EventLevel.Verbose, RichPayloadEventSource.Keywords.Dependencies);

                var item = new DependencyTelemetry("TestDependency", "TestCommand", DateTimeOffset.Now, TimeSpan.Zero, true);
                item.Context.Properties.Add("property1", "value1");
                item.Context.User.Id = "testUserId";
                item.Context.Operation.Id = Guid.NewGuid().ToString();

                client.TrackDependency(item);

                var actualEvent = listener.Messages.FirstOrDefault();

                Assert.IsNotNull(actualEvent);
                Assert.AreEqual(client.InstrumentationKey, actualEvent.Payload[0]);

                object[] tags = actualEvent.Payload[1] as object[];
                Assert.AreEqual("ai.user.id", ((Dictionary<string, object>)(tags[0]))["Key"]);
                Assert.AreEqual("testUserId", ((Dictionary<string, object>)(tags[0]))["Value"]);

                Assert.AreEqual("ai.operation.id", ((Dictionary<string, object>)(tags[1]))["Key"]);
                Assert.AreEqual(item.Context.Operation.Id, ((Dictionary<string, object>)(tags[1]))["Value"]);

                Assert.IsNotNull(actualEvent.Payload[2]);
            }
        }

        [TestMethod]
        public void RichPayloadEventSourcePageViewSentTest()
        {
            var client = new TelemetryClient();
            client.InstrumentationKey = Guid.NewGuid().ToString();

            using (var listener = new Microsoft.ApplicationInsights.TestFramework.TestEventListener())
            {
                listener.EnableEvents(RichPayloadEventSource.Log.eventSource, EventLevel.Verbose, RichPayloadEventSource.Keywords.PageViews);

                var item = new PageViewTelemetry("TestPage");
                item.Context.Properties.Add("property1", "value1");
                item.Context.User.Id = "testUserId";
                item.Context.Operation.Id = Guid.NewGuid().ToString();

                client.TrackPageView(item);

                var actualEvent = listener.Messages.FirstOrDefault();

                Assert.IsNotNull(actualEvent);
                Assert.AreEqual(client.InstrumentationKey, actualEvent.Payload[0]);

                object[] tags = actualEvent.Payload[1] as object[];
                Assert.AreEqual("ai.user.id", ((Dictionary<string, object>)(tags[0]))["Key"]);
                Assert.AreEqual("testUserId", ((Dictionary<string, object>)(tags[0]))["Value"]);

                Assert.AreEqual("ai.operation.id", ((Dictionary<string, object>)(tags[1]))["Key"]);
                Assert.AreEqual(item.Context.Operation.Id, ((Dictionary<string, object>)(tags[1]))["Value"]);

                Assert.IsNotNull(actualEvent.Payload[2]);
            }
        }
    }
}