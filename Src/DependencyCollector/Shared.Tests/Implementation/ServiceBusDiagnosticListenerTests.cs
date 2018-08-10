namespace Microsoft.ApplicationInsights.DependencyCollector
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.DependencyCollector.Implementation;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Web.TestFramework;
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
        public void DiagnosticEventWithoutActivityIsIgnored()
        {
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.ServiceBus");
                module.Initialize(this.configuration);

                DiagnosticListener listener = new DiagnosticListener("Microsoft.Azure.ServiceBus");

                listener.Write(
                    "Microsoft.Azure.ServiceBus.Send.Stop",
                    new
                    {
                        Entity = "queueName",
                        Endpoint = new Uri("sb://queuename.myservicebus.com/")
                    });

                Assert.IsFalse(this.sentItems.Any());
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
                var telemetry = this.TrackOperation<DependencyTelemetry>(listener, "Microsoft.Azure.ServiceBus.Send", TaskStatus.RanToCompletion);

                Assert.IsNotNull(telemetry);
                Assert.AreEqual("Send", telemetry.Name);
                Assert.AreEqual(RemoteDependencyConstants.AzureServiceBus, telemetry.Type);
                Assert.AreEqual("sb://queuename.myservicebus.com/ | queueName", telemetry.Target);
                Assert.IsTrue(telemetry.Success.Value);

                Assert.AreEqual(parentActivity.Id, telemetry.Context.Operation.ParentId);
                Assert.AreEqual(parentActivity.RootId, telemetry.Context.Operation.Id);
                Assert.AreEqual("v1", telemetry.Properties["k1"]);
                Assert.AreEqual("messageId", telemetry.Properties["MessageId"]);
            }
        }

        [TestMethod]
        public void ServiceBusSendHandingWithoutParent()
        {
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.ServiceBus");
                module.Initialize(this.configuration);

                DiagnosticListener listener = new DiagnosticListener("Microsoft.Azure.ServiceBus");

                var telemetry = this.TrackOperation<DependencyTelemetry>(listener, "Microsoft.Azure.ServiceBus.Send", TaskStatus.RanToCompletion);

                Assert.IsNotNull(telemetry);
                Assert.AreEqual("Send", telemetry.Name);
                Assert.AreEqual(RemoteDependencyConstants.AzureServiceBus, telemetry.Type);
                Assert.AreEqual("sb://queuename.myservicebus.com/ | queueName", telemetry.Target);
                Assert.IsTrue(telemetry.Success.Value);

                // W3C compatible-Id ( should go away when W3C is implemented in .NET https://github.com/dotnet/corefx/issues/30331)
                Assert.AreEqual(32, telemetry.Context.Operation.Id.Length);
                Assert.IsTrue(Regex.Match(telemetry.Context.Operation.Id, @"[a-z][0-9]").Success);
                // end of workaround test

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
                Assert.AreEqual(RemoteDependencyConstants.AzureServiceBus, telemetry.Type);
                Assert.AreEqual("sb://queuename.myservicebus.com/ | queueName", telemetry.Target);
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
                Assert.AreEqual($"type:{RemoteDependencyConstants.AzureServiceBus} | name:queueName | endpoint:sb://queuename.myservicebus.com/", telemetry.Source);  
                Assert.IsTrue(telemetry.Success.Value);

                Assert.AreEqual(parentActivity.Id, telemetry.Context.Operation.ParentId);
                Assert.AreEqual(parentActivity.RootId, telemetry.Context.Operation.Id);
                Assert.AreEqual("v1", telemetry.Properties["k1"]);
                Assert.AreEqual("messageId", telemetry.Properties["MessageId"]);
            }
        }

        [TestMethod]
        public void ServiceBusProcessHandingExternalParent()
        {
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.ServiceBus");
                module.Initialize(this.configuration);

                DiagnosticListener listener = new DiagnosticListener("Microsoft.Azure.ServiceBus");

                var telemetry = this.TrackOperation<RequestTelemetry>(listener, "Microsoft.Azure.ServiceBus.Process", TaskStatus.RanToCompletion, "parent");

                Assert.IsNotNull(telemetry);
                Assert.AreEqual("Process", telemetry.Name);
                Assert.AreEqual($"type:{RemoteDependencyConstants.AzureServiceBus} | name:queueName | endpoint:sb://queuename.myservicebus.com/", telemetry.Source);
                Assert.IsTrue(telemetry.Success.Value);

                Assert.AreEqual("parent", telemetry.Context.Operation.ParentId);
                Assert.AreEqual("parent", telemetry.Context.Operation.Id);
                Assert.AreEqual("messageId", telemetry.Properties["MessageId"]);
            }
        }

        [TestMethod]
        public void ServiceBusProcessHandingWithoutParent()
        {
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.ServiceBus");
                module.Initialize(this.configuration);

                DiagnosticListener listener = new DiagnosticListener("Microsoft.Azure.ServiceBus");

                var telemetry = this.TrackOperation<RequestTelemetry>(listener, "Microsoft.Azure.ServiceBus.Process", TaskStatus.RanToCompletion);

                Assert.IsNotNull(telemetry);
                Assert.AreEqual("Process", telemetry.Name);
                Assert.AreEqual($"type:{RemoteDependencyConstants.AzureServiceBus} | name:queueName | endpoint:sb://queuename.myservicebus.com/", telemetry.Source);
                Assert.IsTrue(telemetry.Success.Value);

                // W3C compatible-Id ( should go away when W3C is implemented in .NET https://github.com/dotnet/corefx/issues/30331 TODO)
                Assert.AreEqual(32, telemetry.Context.Operation.Id.Length);
                Assert.IsTrue(Regex.Match(telemetry.Context.Operation.Id, @"[a-z][0-9]").Success);
                // end of workaround test

                Assert.AreEqual("messageId", telemetry.Properties["MessageId"]);
            }
        }

        [TestMethod]
        public void ServiceBusExceptionsAreIgnored()
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

                Assert.IsFalse(this.sentItems.Any());
            }
        }

        private T TrackOperation<T>(DiagnosticListener listener, string activityName, TaskStatus status, string parentId = null) where T : OperationTelemetry
        {
            Activity activity = null;

            if (listener.IsEnabled(activityName))
            {
                activity = new Activity(activityName);
                activity.AddTag("MessageId", "messageId");
                if (Activity.Current == null && parentId != null)
                {
                    activity.SetParentId(parentId);
                }

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