namespace Microsoft.ApplicationInsights.DependencyCollector
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Web.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

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
                Assert.IsTrue(telemetry.Success.Value);

                Assert.AreEqual($"|{sendActivity.TraceId.ToHexString()}.{sendActivity.ParentSpanId.ToHexString()}.", telemetry.Context.Operation.ParentId);
                Assert.AreEqual(sendActivity.TraceId.ToHexString(), telemetry.Context.Operation.Id);
                Assert.AreEqual($"|{sendActivity.TraceId.ToHexString()}.{sendActivity.SpanId.ToHexString()}.", telemetry.Id);

                Assert.AreEqual("v1", telemetry.Properties["k1"]);
                Assert.AreEqual("eventhubname.servicebus.windows.net", telemetry.Properties["property"]);

                Assert.IsTrue(telemetry.Properties.TryGetValue("tracestate", out var tracestate));
                Assert.AreEqual("state=some", tracestate);
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
                Assert.AreEqual(telemetry.Data, exception.ToInvariantString());

                Assert.IsNull(telemetry.Context.Operation.ParentId);
                Assert.AreEqual(sendActivity.TraceId.ToHexString(), telemetry.Context.Operation.Id);
                Assert.AreEqual($"|{sendActivity.TraceId.ToHexString()}.{sendActivity.SpanId.ToHexString()}.", telemetry.Id);

                Assert.AreEqual("eventhubname.servicebus.windows.net", telemetry.Properties["property"]);
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
                activity.AddTag("property", "eventhubname.servicebus.windows.net");

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
    }
}