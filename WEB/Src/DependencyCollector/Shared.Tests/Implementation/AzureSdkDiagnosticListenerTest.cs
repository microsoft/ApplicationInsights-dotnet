namespace Microsoft.ApplicationInsights.DependencyCollector
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Web.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;

    [TestClass]
    public class AzureSdkDiagnosticListenerTest
    {
        private TelemetryConfiguration configuration;
        private List<ITelemetry> sentItems;

        [TestInitialize]
        public void TestInitialize()
        {
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            Activity.ForceDefaultIdFormat = true;
            this.configuration = new TelemetryConfiguration();
            this.configuration.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());
            this.sentItems = new List<ITelemetry>();
            this.configuration.TelemetryChannel = new StubTelemetryChannel { OnSend = item => this.sentItems.Add(item), EndpointAddress = "https://dc.services.visualstudio.com/v2/track" };
            this.configuration.InstrumentationKey = Guid.NewGuid().ToString();
        }

        [TestCleanup]
        public void CleanUp()
        {
            while (Activity.Current != null)
            {
                Activity.Current.Stop();
            }
        }

        [TestMethod]
        public void AzureClientSpansNotCollectedWhenDisabled()
        {
            using (var listener = new DiagnosticListener("Azure.SomeClient"))
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.EnableAzureSdkTelemetryListener = false;
                module.Initialize(this.configuration);

                Activity sendActivity = new Activity("Azure.SomeClient.Send");
                listener.StartActivity(sendActivity, null);
                listener.StopActivity(sendActivity, null);

                Assert.AreEqual(0, this.sentItems.Count);
            }
        }

        [TestMethod]
        public void AzureClientSpansAreCollected()
        {
            using (var listener = new DiagnosticListener("Azure.SomeClient"))
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.Initialize(this.configuration);

                Activity sendActivity = null;
                Activity parentActivity = new Activity("parent").AddBaggage("k1", "v1").Start();
                parentActivity.TraceStateString = "state=some";
                var telemetry = this.TrackOperation<DependencyTelemetry>(
                    listener,
                    "Azure.SomeClient.Send",
                    null,
                    () => sendActivity = Activity.Current);

                Assert.IsNotNull(telemetry);
                Assert.AreEqual("SomeClient.Send", telemetry.Name);
                Assert.AreEqual("InProc", telemetry.Type);
                Assert.IsTrue(telemetry.Success.Value);

                Assert.AreEqual(sendActivity.ParentSpanId.ToHexString(), telemetry.Context.Operation.ParentId);
                Assert.AreEqual(sendActivity.TraceId.ToHexString(), telemetry.Context.Operation.Id);
                Assert.AreEqual(sendActivity.SpanId.ToHexString(), telemetry.Id);

                Assert.AreEqual("v1", telemetry.Properties["k1"]);
                
                Assert.IsTrue(telemetry.Properties.TryGetValue("tracestate", out var tracestate));
                Assert.AreEqual("state=some", tracestate);
            }
        }

        [TestMethod]
        public void AzureClientSpansAreCollectedClientKind()
        {
            using (var listener = new DiagnosticListener("Azure.SomeClient"))
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.Initialize(this.configuration);

                Activity sendActivity = new Activity("Azure.SomeClient.Send");
                sendActivity.AddTag("kind", "client");

                listener.StartActivity(sendActivity, null);
                listener.StopActivity(sendActivity, null);

                var telemetry = this.sentItems.Last() as DependencyTelemetry;

                Assert.IsNotNull(telemetry);
                Assert.AreEqual("SomeClient.Send", telemetry.Name);
                Assert.AreEqual(string.Empty, telemetry.Type);
                Assert.IsTrue(telemetry.Success.Value);

                Assert.IsNull(telemetry.Context.Operation.ParentId);
                Assert.AreEqual(sendActivity.TraceId.ToHexString(), telemetry.Context.Operation.Id);
                Assert.AreEqual(sendActivity.SpanId.ToHexString(), telemetry.Id);
            }
        }

        [TestMethod]
        public void AzureClientSpansAreCollectedServerKind()
        {
            using (var listener = new DiagnosticListener("Azure.SomeClient"))
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.Initialize(this.configuration);

                Activity sendActivity = new Activity("Azure.SomeClient.Send");
                sendActivity.AddTag("kind", "server");

                listener.StartActivity(sendActivity, null);
                listener.StopActivity(sendActivity, null);

                var telemetry = this.sentItems.Last() as RequestTelemetry;

                Assert.IsNotNull(telemetry);
                Assert.AreEqual("SomeClient.Send", telemetry.Name);
                Assert.IsTrue(telemetry.Success.Value);

                Assert.IsNull(telemetry.Context.Operation.ParentId);
                Assert.AreEqual(sendActivity.TraceId.ToHexString(), telemetry.Context.Operation.Id);
                Assert.AreEqual(sendActivity.SpanId.ToHexString(), telemetry.Id);
            }
        }

        [TestMethod]
        public void AzureClientSpansAreCollectedLinks()
        {
            using (var listener = new DiagnosticListener("Azure.SomeClient"))
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.Initialize(this.configuration);

                var sendActivity = new Activity("Azure.SomeClient.Send");
                var link0TraceId = "70545f717a9aa6a490d820438b9d2bf6";
                var link1TraceId = "c5aa06717eef0c4592af26323ade92f7";
                var link0SpanId = "8b0b2fb40c84e64a";
                var link1SpanId = "3a69ce690411bb4f";

                var payload = new PayloadWithLinks
                {
                    Links = new List<Activity>
                    {
                        new Activity("link0").SetParentId($"00-{link0TraceId}-{link0SpanId}-01"),
                        new Activity("link1").SetParentId($"00-{link1TraceId}-{link1SpanId}-01"),
                    },
                };

                listener.StartActivity(sendActivity, payload);
                listener.StopActivity(sendActivity, null);

                var telemetry = this.sentItems.Last() as DependencyTelemetry;

                Assert.IsNotNull(telemetry);
                Assert.AreEqual("SomeClient.Send", telemetry.Name);
                Assert.IsTrue(telemetry.Success.Value);

                Assert.IsNull(telemetry.Context.Operation.ParentId);
                Assert.AreEqual(sendActivity.TraceId.ToHexString(), telemetry.Context.Operation.Id);
                Assert.AreEqual(sendActivity.SpanId.ToHexString(), telemetry.Id);

                // does not throw
                var jsonSettingThrowOnError = new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error,
                    ReferenceLoopHandling = ReferenceLoopHandling.Error,
                    NullValueHandling = NullValueHandling.Include,
                    DefaultValueHandling = DefaultValueHandling.Include,
                };

                Assert.IsTrue(telemetry.Properties.TryGetValue("_MS.links", out var linksStr));
                var actualLinks = JsonConvert.DeserializeObject<ApplicationInsightsLink[]>(linksStr, jsonSettingThrowOnError);

                Assert.IsNotNull(actualLinks);
                Assert.AreEqual(2, actualLinks.Length);

                Assert.AreEqual(link0TraceId, actualLinks[0].OperationId);
                Assert.AreEqual(link1TraceId, actualLinks[1].OperationId);

                Assert.AreEqual(link0SpanId, actualLinks[0].Id);
                Assert.AreEqual(link1SpanId, actualLinks[1].Id);

                Assert.AreEqual($"[{{\"operation_Id\":\"{link0TraceId}\",\"id\":\"{link0SpanId}\"}},{{\"operation_Id\":\"{link1TraceId}\",\"id\":\"{link1SpanId}\"}}]", linksStr);
            }
        }

        [TestMethod]
        public void AzureClientSpansAreMarkedAsFailed()
        {
            using (var listener = new DiagnosticListener("Azure.SomeClient"))
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.Initialize(this.configuration);

                var exception = new InvalidOperationException();
                Activity sendActivity = null;

                var telemetry = this.TrackOperation<DependencyTelemetry>(
                    listener,
                    "Azure.SomeClient.Send",
                    null,
                    () =>
                    {
                        sendActivity = Activity.Current;
                        listener.Write("Azure.SomeClient.Send.Exception", exception);
                    });

                Assert.IsNotNull(telemetry);
                Assert.AreEqual("SomeClient.Send", telemetry.Name);
                Assert.IsFalse(telemetry.Success.Value);

                Assert.AreEqual(exception.ToInvariantString(), telemetry.Properties["Error"]);

                Assert.IsNull(telemetry.Context.Operation.ParentId);
                Assert.AreEqual(sendActivity.TraceId.ToHexString(), telemetry.Context.Operation.Id);
                Assert.AreEqual(sendActivity.SpanId.ToHexString(), telemetry.Id);
            }
        }

        [TestMethod]
        public void AzureClientSpansAreCollectedForHttp()
        {
            using (var listener = new DiagnosticListener("Azure.SomeClient"))
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.Initialize(this.configuration);

                Activity httpActivity = new Activity("Azure.SomeClient.Http.Request")
                    .AddTag("http.method", "PATCH")
                    .AddTag("http.url", "http://host:8080/path?query#fragment")
                    .AddTag("requestId", "client-request-id");

                var payload = new HttpRequestMessage();
                listener.StartActivity(httpActivity, payload);
                httpActivity
                    .AddTag("http.status_code", "206")
                    .AddTag("serviceRequestId", "service-request-id");

                listener.StopActivity(httpActivity, payload);
                
                var telemetry = this.sentItems.Last() as DependencyTelemetry;

                Assert.IsNotNull(telemetry);
                Assert.AreEqual("PATCH /path", telemetry.Name);
                Assert.AreEqual("host:8080", telemetry.Target);
                Assert.AreEqual("http://host:8080/path?query#fragment", telemetry.Data);
                Assert.AreEqual("206", telemetry.ResultCode);
                Assert.AreEqual("Http", telemetry.Type);
                Assert.IsTrue(telemetry.Success.Value);
                Assert.AreEqual("client-request-id", telemetry.Properties["ClientRequestId"]);
                Assert.AreEqual("service-request-id", telemetry.Properties["ServerRequestId"]);

                Assert.IsNull(telemetry.Context.Operation.ParentId);
                Assert.AreEqual(httpActivity.TraceId.ToHexString(), telemetry.Context.Operation.Id);
                Assert.AreEqual(httpActivity.SpanId.ToHexString(), telemetry.Id);
            }
        }

        [TestMethod]
        public void AzureClientSpansAreCollectedForHttpNotSuccessResponse()
        {
            using (var listener = new DiagnosticListener("Azure.SomeClient"))
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.Initialize(this.configuration);

                Activity httpActivity = new Activity("Azure.SomeClient.Http.Request")
                    .AddTag("http.method", "PATCH")
                    .AddTag("http.url", "http://host/path?query#fragment");

                var payload = new HttpRequestMessage();
                listener.StartActivity(httpActivity, payload);
                httpActivity.AddTag("http.status_code", "503");

                listener.StopActivity(httpActivity, payload);

                var telemetry = this.sentItems.Last() as DependencyTelemetry;

                Assert.IsNotNull(telemetry);
                Assert.AreEqual("PATCH /path", telemetry.Name);
                Assert.AreEqual("host", telemetry.Target);
                Assert.AreEqual("http://host/path?query#fragment", telemetry.Data);
                Assert.AreEqual("503", telemetry.ResultCode);
                Assert.AreEqual("Http", telemetry.Type);
                Assert.IsFalse(telemetry.Success.Value);

                Assert.IsNull(telemetry.Context.Operation.ParentId);
                Assert.AreEqual(httpActivity.TraceId.ToHexString(), telemetry.Context.Operation.Id);
                Assert.AreEqual(httpActivity.SpanId.ToHexString(), telemetry.Id);
            }
        }

        [TestMethod]
        public void AzureClientSpansAreCollectedForHttpException()
        {
            using (var listener = new DiagnosticListener("Azure.SomeClient"))
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.Initialize(this.configuration);

                var exception = new InvalidOperationException();
                Activity httpActivity = new Activity("Azure.SomeClient.Http.Request")
                    .AddTag("http.method", "PATCH")
                    .AddTag("http.url", "http://host/path?query#fragment");

                listener.StartActivity(httpActivity, null);
                listener.Write("Azure.SomeClient.Send.Exception", exception);

                listener.StopActivity(httpActivity, null);

                var telemetry = this.sentItems.Last() as DependencyTelemetry;

                Assert.IsNotNull(telemetry);
                Assert.AreEqual("PATCH /path", telemetry.Name);
                Assert.AreEqual("host", telemetry.Target);
                Assert.AreEqual("http://host/path?query#fragment", telemetry.Data);
                Assert.IsNull(telemetry.ResultCode);
                Assert.AreEqual("Http", telemetry.Type);
                Assert.IsFalse(telemetry.Success.Value);
                Assert.AreEqual(exception.ToInvariantString(), telemetry.Properties["Error"]);

                Assert.IsNull(telemetry.Context.Operation.ParentId);
                Assert.AreEqual(httpActivity.TraceId.ToHexString(), telemetry.Context.Operation.Id);
                Assert.AreEqual(httpActivity.SpanId.ToHexString(), telemetry.Id);
            }
        }

        [TestMethod]
        public void AzureClientSpansAreCollectedForEventHubs()
        {
            using (var listener = new DiagnosticListener("Azure.SomeClient"))
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.Initialize(this.configuration);

                var exception = new InvalidOperationException();
                Activity sendActivity = new Activity("Azure.SomeClient.Send")
                    .AddTag("peer.address", "amqps://eventHub.servicebus.windows.net/")
                    .AddTag("message_bus.destination", "queueName")
                    .AddTag("kind", "producer")
                    .AddTag("component", "eventhubs");

                listener.StartActivity(sendActivity, null);
                listener.Write("Azure.SomeClient.Send.Exception", exception);
                listener.StopActivity(sendActivity, null);

                var telemetry = this.sentItems.Last() as DependencyTelemetry;

                Assert.IsNotNull(telemetry);
                Assert.AreEqual("SomeClient.Send", telemetry.Name);
                Assert.AreEqual("amqps://eventHub.servicebus.windows.net/ | queueName", telemetry.Target);
                Assert.AreEqual(string.Empty, telemetry.Data);
                Assert.AreEqual(string.Empty, telemetry.ResultCode);
                Assert.AreEqual("Azure Event Hubs", telemetry.Type);
                Assert.IsFalse(telemetry.Success.Value);
                Assert.AreEqual(exception.ToInvariantString(), telemetry.Properties["Error"]);
                Assert.IsNull(telemetry.Context.Operation.ParentId);
                Assert.AreEqual(sendActivity.TraceId.ToHexString(), telemetry.Context.Operation.Id);
                Assert.AreEqual(sendActivity.SpanId.ToHexString(), telemetry.Id);
            }
        }

        [TestMethod]
        public void AzureClientSpansAreCollectedForEventHubsException()
        {
            using (var listener = new DiagnosticListener("Azure.SomeClient"))
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.Initialize(this.configuration);

                Activity sendActivity = new Activity("Azure.SomeClient.Send")
                    .AddTag("peer.address", "amqps://eventHub.servicebus.windows.net/")
                    .AddTag("message_bus.destination", "queueName")
                    .AddTag("kind", "producer")
                    .AddTag("component", "eventhubs");

                listener.StartActivity(sendActivity, null);
                listener.StopActivity(sendActivity, null);

                var telemetry = this.sentItems.Last() as DependencyTelemetry;

                Assert.IsNotNull(telemetry);
                Assert.AreEqual("SomeClient.Send", telemetry.Name);
                Assert.AreEqual("amqps://eventHub.servicebus.windows.net/ | queueName", telemetry.Target);
                Assert.AreEqual(string.Empty, telemetry.Data);
                Assert.AreEqual(string.Empty, telemetry.ResultCode);
                Assert.AreEqual("Azure Event Hubs", telemetry.Type);
                Assert.IsTrue(telemetry.Success.Value);

                Assert.IsNull(telemetry.Context.Operation.ParentId);
                Assert.AreEqual(sendActivity.TraceId.ToHexString(), telemetry.Context.Operation.Id);
                Assert.AreEqual(sendActivity.SpanId.ToHexString(), telemetry.Id);
            }
        }

        private T TrackOperation<T>(
            DiagnosticListener listener,
            string activityName,
            string parentId = null,
            Action operation = null) where T : OperationTelemetry
        {
            Activity activity = null;
            int itemCountBefore = this.sentItems.Count;

            if (listener.IsEnabled(activityName))
            {
                activity = new Activity(activityName);

                if (Activity.Current == null && parentId != null)
                {
                    activity.SetParentId(parentId);
                }

                listener.StartActivity(activity, null);
            }

            operation?.Invoke();

            if (activity != null)
            {
                listener.StopActivity(activity, null);

                // a single new telemetry item was addedAssert.AreEqual(itemCountBefore + 1, this.sentItems.Count);
                return this.sentItems.Last() as T;
            }

            // no new telemetry items were added
            Assert.AreEqual(itemCountBefore, this.sentItems.Count);
            return null;
        }

        private class PayloadWithLinks
        {
            public IEnumerable<Activity> Links { get; set; }
        }

        private class ApplicationInsightsLink
        {
            [JsonProperty("operation_Id")]
            public string OperationId { get; set; }

            [JsonProperty("id")]
            public string Id { get; set; }
        }
    }
}