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
#if !NETCORE
    using Microsoft.ApplicationInsights.Web.TestFramework;
#else
    using Microsoft.ApplicationInsights.Tests;
#endif
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ServiceBusDiagnosticListenerTests
    {
        private TelemetryConfiguration configuration;
        private List<ITelemetry> sentItems;

        [TestInitialize]
        public void TestInitialize()
        {
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
        public void ServiceBusSendHanding()
        {
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.ServiceBus");
                module.Initialize(this.configuration);

                DiagnosticListener listener = new DiagnosticListener("Microsoft.Azure.ServiceBus");

                Activity parentActivity = new Activity("parent").AddBaggage("k1", "v1").Start();
                var telemetry  = this.TrackOperation<DependencyTelemetry>(listener, "Microsoft.Azure.ServiceBus.Send", TaskStatus.RanToCompletion);

                Assert.IsNotNull(telemetry);
                Assert.AreEqual("Send", telemetry.Name);
                Assert.AreEqual("Queue.ServiceBus", telemetry.Type);
                Assert.AreEqual("queueName", telemetry.Target);
                Assert.AreEqual("sb://queuename.myservicebus.com/", telemetry.Data);
                Assert.IsTrue(telemetry.Success.Value);

                Assert.AreEqual(parentActivity.Id, telemetry.Context.Operation.ParentId);
                Assert.AreEqual(parentActivity.RootId, telemetry.Context.Operation.Id);
                Assert.AreEqual("v1", telemetry.Properties["k1"]);
                Assert.AreEqual("messageId", telemetry.Properties["MessageId"]);
            }
        }

        [TestMethod]
        public void ServiceBusBadStatusHanding()
        {
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.ServiceBus");
                module.Initialize(this.configuration);

                DiagnosticListener listener = new DiagnosticListener("Microsoft.Azure.ServiceBus");

                Activity parentActivity = new Activity("parent").AddBaggage("k1", "v1").Start();
                var telemetry = this.TrackOperation<DependencyTelemetry>(listener, "Microsoft.Azure.ServiceBus.Send", TaskStatus.Faulted);

                Assert.IsNotNull(telemetry);
                Assert.AreEqual("Send", telemetry.Name);
                Assert.AreEqual("Queue.ServiceBus", telemetry.Type);
                Assert.AreEqual("queueName", telemetry.Target);
                Assert.AreEqual("sb://queuename.myservicebus.com/", telemetry.Data);
                Assert.IsFalse(telemetry.Success.Value);

                Assert.AreEqual(parentActivity.Id, telemetry.Context.Operation.ParentId);
                Assert.AreEqual(parentActivity.RootId, telemetry.Context.Operation.Id);
                Assert.AreEqual("v1", telemetry.Properties["k1"]);
                Assert.AreEqual("messageId", telemetry.Properties["MessageId"]);
            }
        }

        [TestMethod]
        public void ServiceBusProcessHanding()
        {
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.ServiceBus");
                module.Initialize(this.configuration);

                DiagnosticListener listener = new DiagnosticListener("Microsoft.Azure.ServiceBus");

                Activity parentActivity = new Activity("parent").AddBaggage("k1", "v1").Start();
                var telemetry = this.TrackOperation<RequestTelemetry>(listener, "Microsoft.Azure.ServiceBus.Process", TaskStatus.RanToCompletion);

                Assert.IsNotNull(telemetry);
                Assert.AreEqual("Process", telemetry.Name);
                Assert.AreEqual("roleName:queueName", telemetry.Source);
                Assert.AreEqual("sb://queuename.myservicebus.com/", telemetry.Url.ToString());
                Assert.IsTrue(telemetry.Success.Value);

                Assert.AreEqual(parentActivity.Id, telemetry.Context.Operation.ParentId);
                Assert.AreEqual(parentActivity.RootId, telemetry.Context.Operation.Id);
                Assert.AreEqual("v1", telemetry.Properties["k1"]);
                Assert.AreEqual("messageId", telemetry.Properties["MessageId"]);
            }
        }

        [TestMethod]
        public void ServiceBusExceptionHanding()
        {
            using (var module = new DependencyTrackingTelemetryModule())
            {
                this.configuration.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());
                module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.ServiceBus");
                module.Initialize(this.configuration);

                DiagnosticListener listener = new DiagnosticListener("Microsoft.Azure.ServiceBus");

                Activity parentActivity = new Activity("parent").AddBaggage("k1", "v1").Start();
                if (listener.IsEnabled("Microsoft.Azure.ServiceBus.Exception"))
                {
                    listener.Write("Microsoft.Azure.ServiceBus.Exception", new { Exception = new Exception("123") });
                }

                ExceptionTelemetry exceptionTelemetry = this.sentItems.Last() as ExceptionTelemetry;
                Assert.IsNotNull(exceptionTelemetry);

                Assert.AreEqual(parentActivity.Id, exceptionTelemetry.Context.Operation.ParentId);
                Assert.AreEqual(parentActivity.RootId, exceptionTelemetry.Context.Operation.Id);
                Assert.AreEqual("v1", exceptionTelemetry.Properties["k1"]);
            }
        }

        private T TrackOperation<T>(DiagnosticListener listener, string activityName, TaskStatus status) where T : OperationTelemetry
        {
            Activity activity = null;

            if (listener.IsEnabled(activityName))
            {
                activity = new Activity(activityName);
                activity.AddTag("MessageId", "messageId");
                if (listener.IsEnabled(activityName + ".Start"))
                {
                    listener.StartActivity(activity, new { Entity = "queueName", Endpoint = new Uri("sb://queuename.myservicebus.com/") });
                }
                else
                {
                    activity.Start();
                }
            }

            if (activity != null)
            {
                listener.StopActivity(activity, new { Entity = "queueName", Endpoint = new Uri("sb://queuename.myservicebus.com/"), Status = status });
                return this.sentItems.Last() as T;
            }

            return null;
        }
    }
}