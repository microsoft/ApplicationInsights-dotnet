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
    using Microsoft.ApplicationInsights.W3C;
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
            this.configuration.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());
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
            using (var listener = new DiagnosticListener("Microsoft.Azure.ServiceBus"))
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.ServiceBus");
                module.Initialize(this.configuration);

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
            using (var listener = new DiagnosticListener("Microsoft.Azure.ServiceBus"))
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.ServiceBus");
                module.Initialize(this.configuration);

                Activity parentActivity = new Activity("parent").AddBaggage("k1", "v1").Start();
                var telemetry = this.TrackOperation<DependencyTelemetry>(listener,
                    "Microsoft.Azure.ServiceBus.Send", TaskStatus.RanToCompletion);

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
            using (var listener = new DiagnosticListener("Microsoft.Azure.ServiceBus"))
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.ServiceBus");
                module.Initialize(this.configuration);

                var telemetry = this.TrackOperation<DependencyTelemetry>(listener,
                    "Microsoft.Azure.ServiceBus.Send", TaskStatus.RanToCompletion);

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
            using (var listener = new DiagnosticListener("Microsoft.Azure.ServiceBus"))
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.ServiceBus");
                module.Initialize(this.configuration);

                Activity parentActivity = new Activity("parent").AddBaggage("k1", "v1").Start();
                var telemetry = this.TrackOperation<DependencyTelemetry>(listener,
                    "Microsoft.Azure.ServiceBus.Send", TaskStatus.Faulted);

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
            var tc = new TelemetryClient(this.configuration);
            void TrackTraceDuringProcessing() => tc.TrackTrace("trace");

            using (var listener = new DiagnosticListener("Microsoft.Azure.ServiceBus"))
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.ServiceBus");
                module.Initialize(this.configuration);

                Activity parentActivity = new Activity("parent").AddBaggage("k1", "v1").Start();
                var requestTelemetry = this.TrackOperation<RequestTelemetry>(
                    listener,
                    "Microsoft.Azure.ServiceBus.Process",
                    TaskStatus.RanToCompletion,
                    operation: TrackTraceDuringProcessing);

                Assert.IsNotNull(requestTelemetry);
                Assert.AreEqual("Process", requestTelemetry.Name);
                Assert.AreEqual(
                    $"type:{RemoteDependencyConstants.AzureServiceBus} | name:queueName | endpoint:sb://queuename.myservicebus.com/",
                    requestTelemetry.Source);
                Assert.IsTrue(requestTelemetry.Success.Value);

                Assert.AreEqual(parentActivity.Id, requestTelemetry.Context.Operation.ParentId);
                Assert.AreEqual(parentActivity.RootId, requestTelemetry.Context.Operation.Id);
                Assert.AreEqual("v1", requestTelemetry.Properties["k1"]);

                Assert.AreEqual("messageId", requestTelemetry.Properties["MessageId"]);

                var traceTelemetry = this.sentItems.OfType<TraceTelemetry>();
                Assert.AreEqual(1, traceTelemetry.Count());

                Assert.AreEqual(requestTelemetry.Context.Operation.Id, traceTelemetry.Single().Context.Operation.Id);
                Assert.AreEqual(requestTelemetry.Id, traceTelemetry.Single().Context.Operation.ParentId);
            }
        }

        [TestMethod]
        public void ServiceBusProcessHandingExternalParent()
        {
            var tc = new TelemetryClient(this.configuration);
            void TrackTraceDuringProcessing() => tc.TrackTrace("trace");

            using (var listener = new DiagnosticListener("Microsoft.Azure.ServiceBus"))
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.ServiceBus");
                module.Initialize(this.configuration);

                var requestTelemetry = this.TrackOperation<RequestTelemetry>(
                    listener,
                    "Microsoft.Azure.ServiceBus.Process", 
                    TaskStatus.RanToCompletion, 
                    "parent",
                    TrackTraceDuringProcessing);

                Assert.IsNotNull(requestTelemetry);
                Assert.AreEqual("Process", requestTelemetry.Name);
                Assert.AreEqual(
                    $"type:{RemoteDependencyConstants.AzureServiceBus} | name:queueName | endpoint:sb://queuename.myservicebus.com/",
                    requestTelemetry.Source);
                Assert.IsTrue(requestTelemetry.Success.Value);

                Assert.AreEqual("parent", requestTelemetry.Context.Operation.ParentId);
                Assert.AreEqual("parent", requestTelemetry.Context.Operation.Id);
                Assert.AreEqual("messageId", requestTelemetry.Properties["MessageId"]);

                var traceTelemetry = this.sentItems.OfType<TraceTelemetry>();
                Assert.AreEqual(1, traceTelemetry.Count());

                Assert.AreEqual(requestTelemetry.Context.Operation.Id, traceTelemetry.Single().Context.Operation.Id);
                Assert.AreEqual(requestTelemetry.Id, traceTelemetry.Single().Context.Operation.ParentId);
            }
        }

        [TestMethod]
        public void ServiceBusProcessHandingWithoutParent()
        {
            var tc = new TelemetryClient(this.configuration);
            void TrackTraceDuringProcessing() => tc.TrackTrace("trace");

            using (var module = new DependencyTrackingTelemetryModule())
            using (var listener = new DiagnosticListener("Microsoft.Azure.ServiceBus"))
            {
                module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.ServiceBus");
                module.Initialize(this.configuration);

                var requestTelemetry = this.TrackOperation<RequestTelemetry>(
                    listener,
                    "Microsoft.Azure.ServiceBus.Process",
                    TaskStatus.RanToCompletion,
                    operation: TrackTraceDuringProcessing);

                Assert.IsNotNull(requestTelemetry);
                Assert.AreEqual("Process", requestTelemetry.Name);
                Assert.AreEqual(
                    $"type:{RemoteDependencyConstants.AzureServiceBus} | name:queueName | endpoint:sb://queuename.myservicebus.com/",
                    requestTelemetry.Source);
                Assert.IsTrue(requestTelemetry.Success.Value);

                // W3C compatible-Id ( should go away when W3C is implemented in .NET https://github.com/dotnet/corefx/issues/30331 TODO)
                Assert.AreEqual(32, requestTelemetry.Context.Operation.Id.Length);
                Assert.IsTrue(Regex.Match(requestTelemetry.Context.Operation.Id, @"[a-z][0-9]").Success);
                // end of workaround test

                Assert.AreEqual("messageId", requestTelemetry.Properties["MessageId"]);

                var traceTelemetry = this.sentItems.OfType<TraceTelemetry>();
                Assert.AreEqual(1, traceTelemetry.Count());

                Assert.AreEqual(requestTelemetry.Context.Operation.Id, traceTelemetry.Single().Context.Operation.Id);
                Assert.AreEqual(requestTelemetry.Id, traceTelemetry.Single().Context.Operation.ParentId);
            }
        }

#pragma warning disable 612, 618
        [TestMethod]
        public void ServiceBusProcessHandingW3C()
        {
            this.configuration.TelemetryInitializers.Add(new W3COperationCorrelationTelemetryInitializer());
            var tc = new TelemetryClient(this.configuration);
            void TrackTraceDuringProcessing() => tc.TrackTrace("trace");

            using (var listener = new DiagnosticListener("Microsoft.Azure.ServiceBus"))
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.EnableW3CHeadersInjection = true;
                module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.ServiceBus");
                module.Initialize(this.configuration);

                Activity parentActivity = new Activity("parent").AddBaggage("k1", "v1").Start();
                var requestTelemetry = this.TrackOperation<RequestTelemetry>(
                    listener,
                    "Microsoft.Azure.ServiceBus.Process",
                    TaskStatus.RanToCompletion,
                    operation: TrackTraceDuringProcessing);

                Assert.IsNotNull(requestTelemetry);
                Assert.AreEqual("Process", requestTelemetry.Name);
                Assert.AreEqual(
                    $"type:{RemoteDependencyConstants.AzureServiceBus} | name:queueName | endpoint:sb://queuename.myservicebus.com/",
                    requestTelemetry.Source);
                Assert.IsTrue(requestTelemetry.Success.Value);

                Assert.AreEqual($"|{parentActivity.GetTraceId()}.{parentActivity.GetSpanId()}.", requestTelemetry.Context.Operation.ParentId);
                Assert.AreEqual(parentActivity.GetTraceId(), requestTelemetry.Context.Operation.Id);
                Assert.AreEqual("v1", requestTelemetry.Properties["k1"]);

                Assert.AreEqual("messageId", requestTelemetry.Properties["MessageId"]);

                var traceTelemetry = this.sentItems.OfType<TraceTelemetry>();
                Assert.AreEqual(1, traceTelemetry.Count());

                Assert.AreEqual(requestTelemetry.Context.Operation.Id, traceTelemetry.Single().Context.Operation.Id);
                Assert.AreEqual(requestTelemetry.Id, traceTelemetry.Single().Context.Operation.ParentId);
            }
        }

        [TestMethod]
        public void ServiceBusProcessHandingExternalParentW3C()
        {
            this.configuration.TelemetryInitializers.Add(new W3COperationCorrelationTelemetryInitializer());
            var tc = new TelemetryClient(this.configuration);
            void TrackTraceDuringProcessing() => tc.TrackTrace("trace");

            using (var listener = new DiagnosticListener("Microsoft.Azure.ServiceBus"))
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.EnableW3CHeadersInjection = true;
                module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.ServiceBus");
                module.Initialize(this.configuration);

                var requestTelemetry = this.TrackOperation<RequestTelemetry>(
                    listener,
                    "Microsoft.Azure.ServiceBus.Process",
                    TaskStatus.RanToCompletion,
                    "parent",
                    TrackTraceDuringProcessing);

                Assert.IsNotNull(requestTelemetry);
                Assert.AreEqual("Process", requestTelemetry.Name);
                Assert.AreEqual(
                    $"type:{RemoteDependencyConstants.AzureServiceBus} | name:queueName | endpoint:sb://queuename.myservicebus.com/",
                    requestTelemetry.Source);
                Assert.IsTrue(requestTelemetry.Success.Value);

                Assert.AreEqual("parent", requestTelemetry.Context.Operation.ParentId);
                Assert.AreEqual("parent", requestTelemetry.Properties[W3CConstants.LegacyRootIdProperty]);
                Assert.AreEqual("messageId", requestTelemetry.Properties["MessageId"]);

                var traceTelemetry = this.sentItems.OfType<TraceTelemetry>();
                Assert.AreEqual(1, traceTelemetry.Count());

                Assert.AreEqual(requestTelemetry.Context.Operation.Id, traceTelemetry.Single().Context.Operation.Id);
                Assert.AreEqual(requestTelemetry.Id, traceTelemetry.Single().Context.Operation.ParentId);
            }
        }

        [TestMethod]
        public void ServiceBusProcessHandingWithoutParentW3C()
        {
            this.configuration.TelemetryInitializers.Add(new W3COperationCorrelationTelemetryInitializer());
            var tc = new TelemetryClient(this.configuration);
            void TrackTraceDuringProcessing() => tc.TrackTrace("trace");

            using (var module = new DependencyTrackingTelemetryModule())
            using (var listener = new DiagnosticListener("Microsoft.Azure.ServiceBus"))
            {
                module.EnableW3CHeadersInjection = true;
                module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.ServiceBus");
                module.Initialize(this.configuration);

                var requestTelemetry = this.TrackOperation<RequestTelemetry>(
                    listener,
                    "Microsoft.Azure.ServiceBus.Process",
                    TaskStatus.RanToCompletion,
                    operation: TrackTraceDuringProcessing);

                Assert.IsNotNull(requestTelemetry);
                Assert.AreEqual("Process", requestTelemetry.Name);
                Assert.AreEqual(
                    $"type:{RemoteDependencyConstants.AzureServiceBus} | name:queueName | endpoint:sb://queuename.myservicebus.com/",
                    requestTelemetry.Source);
                Assert.IsTrue(requestTelemetry.Success.Value);

                // W3C compatible-Id ( should go away when W3C is implemented in .NET https://github.com/dotnet/corefx/issues/30331 TODO)
                Assert.AreEqual(32, requestTelemetry.Context.Operation.Id.Length);
                Assert.IsTrue(Regex.Match(requestTelemetry.Context.Operation.Id, @"[a-z][0-9]").Success);
                // end of workaround test

                Assert.AreEqual("messageId", requestTelemetry.Properties["MessageId"]);

                var traceTelemetry = this.sentItems.OfType<TraceTelemetry>();
                Assert.AreEqual(1, traceTelemetry.Count());

                Assert.AreEqual(requestTelemetry.Context.Operation.Id, traceTelemetry.Single().Context.Operation.Id);
                Assert.AreEqual(requestTelemetry.Id, traceTelemetry.Single().Context.Operation.ParentId);
            }
        }

        public void ServiceBusProcessHandingExternalParentW3CCompatibleRequestId()
        {
            this.configuration.TelemetryInitializers.Add(new W3COperationCorrelationTelemetryInitializer());
            using (var listener = new DiagnosticListener("Microsoft.Azure.ServiceBus"))
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.EnableW3CHeadersInjection = true;
                module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.ServiceBus");
                module.Initialize(this.configuration);

                var telemetry = this.TrackOperation<RequestTelemetry>(
                    listener,
                    "Microsoft.Azure.ServiceBus.Process", 
                    TaskStatus.RanToCompletion,
                    "|4bf92f3577b34da6a3ce929d0e0e4736.");

                Assert.IsNotNull(telemetry);
                Assert.AreEqual("Process", telemetry.Name);
                Assert.AreEqual(
                    $"type:{RemoteDependencyConstants.AzureServiceBus} | name:queueName | endpoint:sb://queuename.myservicebus.com/",
                    telemetry.Source);
                Assert.IsTrue(telemetry.Success.Value);

                Assert.AreEqual("|4bf92f3577b34da6a3ce929d0e0e4736.", telemetry.Context.Operation.ParentId);
                Assert.AreEqual("4bf92f3577b34da6a3ce929d0e0e4736", telemetry.Context.Operation.Id);
                Assert.AreEqual("messageId", telemetry.Properties["MessageId"]);
            }
        }
#pragma warning restore 612, 618

        [TestMethod]
        public void ServiceBusExceptionsAreIgnored()
        {
            using (var listener = new DiagnosticListener("Microsoft.Azure.ServiceBus"))
            using (var module = new DependencyTrackingTelemetryModule())
            {
                this.configuration.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());
                module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.ServiceBus");
                module.Initialize(this.configuration);

                Activity parentActivity = new Activity("parent").AddBaggage("k1", "v1").Start();
                if (listener.IsEnabled("Microsoft.Azure.ServiceBus.Exception"))
                {
                    listener.Write("Microsoft.Azure.ServiceBus.Exception", new
                    {
                        Exception = new Exception("123")
                    });
                }

                Assert.IsFalse(this.sentItems.Any());
            }
        }

        private T TrackOperation<T>(
            DiagnosticListener listener,
            string activityName, 
            TaskStatus status, 
            string parentId = null,
            Action operation = null) where T : OperationTelemetry
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

            operation?.Invoke();

            if (activity != null)
            {
                listener.StopActivity(activity, new { Entity = "queueName", Endpoint = new Uri("sb://queuename.myservicebus.com/"), Status = status });
                return this.sentItems.OfType<T>().Last();
            }

            return default(T);
        }
    }
}