namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Extensibility.Implementation;
    using TestFramework;

    [TestClass]
    public class TelemetryClientExtensionTests
    {
        private TelemetryClient telemetryClient;
        private List<ITelemetry> sendItems;

        [TestInitialize]
        public void TestInitialize()
        {
            var configuration = new TelemetryConfiguration();
            this.sendItems = new List<ITelemetry>();
            configuration.TelemetryChannel = new StubTelemetryChannel { OnSend = item => this.sendItems.Add(item) };
            configuration.InstrumentationKey = Guid.NewGuid().ToString();
            configuration.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());
            this.telemetryClient = new TelemetryClient(configuration);
            CallContextHelpers.RestoreOperationContext(null);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            CallContextHelpers.RestoreOperationContext(null);
            while (Activity.Current != null)
            {
                Activity.Current.Stop();
            }
        }

        [TestMethod]
        public void StartDependencyTrackingReturnsOperationWithSameTelemetryItem()
        {
            var operation = this.telemetryClient.StartOperation<DependencyTelemetry>(operationName: null);
            Assert.IsNotNull(operation);
            Assert.IsNotNull(operation.Telemetry);
        }

        [TestMethod]
        public void StartDependencyTrackingReturnsOperationWithInitializedOperationName()
        {
            var operation = this.telemetryClient.StartOperation<DependencyTelemetry>("TestOperationName");
            Assert.AreEqual("TestOperationName", operation.Telemetry.Name);
        }

        [TestMethod]
        public void StartDependencyTrackingReturnsOperationWithInitializedOperationId()
        {
            var operation = this.telemetryClient.StartOperation<DependencyTelemetry>("TestOperationName");
            Assert.IsNotNull(operation.Telemetry.Context.Operation.Id);
        }

        [TestMethod]
        public void StartDependencyTrackingReturnsOperationWithInitializedOperationRootId()
        {
            var operation = this.telemetryClient.StartOperation<DependencyTelemetry>("TestOperationName");
            Assert.IsNotNull(operation.Telemetry.Context.Operation.Id);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void StartDependencyTrackingThrowsExceptionWithNullTelemetryClient()
        {
            TelemetryClient tc = null;
            tc.StartOperation<DependencyTelemetry>(operationName: null);
        }

        [TestMethod]
        public void StartDependencyTrackingCreatesADependencyTelemetryItemWithTimeStamp()
        {
            var operation = this.telemetryClient.StartOperation<DependencyTelemetry>(operationName: null);
            Assert.AreNotEqual(operation.Telemetry.Timestamp, DateTimeOffset.MinValue);
        }

        [TestMethod]
        public void StartDependencyTrackingAddsOperationContextStoreToCurrentActivity()
        {
            Assert.IsNull(Activity.Current);
            var operation = this.telemetryClient.StartOperation<DependencyTelemetry>(operationName: null);
            Assert.IsNotNull(Activity.Current);
        }

        [TestMethod]
        public void UsingSendsTelemetryAndDisposesOperationItem()
        {
            Assert.IsNull(Activity.Current);
            using (var operation = this.telemetryClient.StartOperation<DependencyTelemetry>(operationName: null))
            {
            }

            Assert.IsNull(Activity.Current);
            Assert.AreEqual(1, this.sendItems.Count);
        }

        [TestMethod]
        public void UsingWithStopOperationSendsTelemetryAndDisposesOperationItemOnlyOnce()
        {
            Assert.IsNull(Activity.Current);
            using (var operation = this.telemetryClient.StartOperation<DependencyTelemetry>(operationName: null))
            {
                this.telemetryClient.StopOperation(operation);
            }

            Assert.IsNull(Activity.Current);
            Assert.AreEqual(1, this.sendItems.Count);
        }

        [TestMethod]
        public void StartDependencyTrackingHandlesMultipleContextStoresInCurrentActivity()
        {
            var operation = this.telemetryClient.StartOperation<DependencyTelemetry>("OperationName") as OperationHolder<DependencyTelemetry>;
            var currentActivity = Activity.Current;
            Assert.AreEqual(operation.Telemetry.Id, currentActivity.Id);
            Assert.AreEqual(operation.Telemetry.Context.Operation.Name, this.GetOperationName(currentActivity));

            var childOperation = this.telemetryClient.StartOperation<DependencyTelemetry>("OperationName") as OperationHolder<DependencyTelemetry>;
            var childActivity = Activity.Current;
            Assert.AreEqual(childOperation.Telemetry.Id, childActivity.Id);
            Assert.AreEqual(childOperation.Telemetry.Context.Operation.Name, this.GetOperationName(currentActivity));

            Assert.IsNull(currentActivity.Parent);
            Assert.AreEqual(currentActivity, childActivity.Parent);

            this.telemetryClient.StopOperation(childOperation);
            Assert.AreEqual(currentActivity, Activity.Current);
            this.telemetryClient.StopOperation(operation);
            Assert.IsNull(Activity.Current);
        }

        [TestMethod]
        public void StopOperationDoesNotFailOnNullOperation()
        {
            this.telemetryClient.StopOperation<DependencyTelemetry>(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void StopDependencyTrackingThrowsExceptionWithNullTelemetryClient()
        {
            var operationItem = new OperationHolder<DependencyTelemetry>(this.telemetryClient, new DependencyTelemetry());
            TelemetryClient tc = null;
            tc.StopOperation(operationItem);
        }

        [TestMethod]
        public void StopOperationDoesNotThrowExceptionIfParentOpertionIsStoppedBeforeChildOperation()
        {
            using (var parentOperation = this.telemetryClient.StartOperation<DependencyTelemetry>("operationName"))
            {
                using (var childOperation = this.telemetryClient.StartOperation<DependencyTelemetry>("operationName"))
                {
                    this.telemetryClient.StopOperation(parentOperation);
                }
            }
        }

        [TestMethod]
        public void StopOperationWorksFineWithNestedOperations()
        {
            using (var parentOperation = this.telemetryClient.StartOperation<DependencyTelemetry>("operationName"))
            {
                using (var childOperation = this.telemetryClient.StartOperation<DependencyTelemetry>("operationName"))
                {
                    this.telemetryClient.StopOperation(childOperation);
                }

                this.telemetryClient.StopOperation(parentOperation);
            }

            Assert.AreEqual(2, this.sendItems.Count);
        }

        [TestMethod]
        public void StartDependencyTrackingStoresTheArgumentOperationNameInCurrentActivity()
        {
            var operation = this.telemetryClient.StartOperation<DependencyTelemetry>("TestOperationName");
            Assert.AreEqual("TestOperationName", this.GetOperationName(Activity.Current));
        }

        [TestMethod]
        public void DisposeOperationAppliesChangesOnActivityDoneAfterStart()
        {
            this.telemetryClient.TelemetryConfiguration.TelemetryInitializers.Add(new ActivityTagsTelemetryIntitializer());

            DependencyTelemetry telemetry = null;
            using (var operation = this.telemetryClient.StartOperation<DependencyTelemetry>("TestOperationName"))
            {
                Activity.Current.AddTag("my custom tag", "value");
                telemetry = operation.Telemetry;
            }

            Assert.IsTrue(telemetry.Properties.ContainsKey("my custom tag"));
            Assert.AreEqual("value", telemetry.Properties["my custom tag"]);
        }

        [TestMethod]
        public void ContextPropagatesThroughNestedOperations()
        {
            using (this.telemetryClient.StartOperation<RequestTelemetry>("OuterRequest"))
            {
                using (this.telemetryClient.StartOperation<DependencyTelemetry>("DependentCall"))
                {
                }
            }

            Assert.AreEqual(2, this.sendItems.Count);

            var requestTelmetry = (RequestTelemetry)this.sendItems[1];
            var dependentTelmetry = (DependencyTelemetry)this.sendItems[0];
            Assert.IsNull(requestTelmetry.Context.Operation.ParentId);
            Assert.AreEqual(requestTelmetry.Id, dependentTelmetry.Context.Operation.ParentId);
            Assert.AreEqual(requestTelmetry.Context.Operation.Id, dependentTelmetry.Context.Operation.Id);
            Assert.AreEqual(requestTelmetry.Context.Operation.Name, dependentTelmetry.Context.Operation.Name);
        }

        [TestMethod]
        public void StartOperationCanOverrideOperationId()
        {
            using (this.telemetryClient.StartOperation<RequestTelemetry>("Request", "HOME"))
            {
            }

            Assert.AreEqual(1, this.sendItems.Count);

            var requestTelmetry = (RequestTelemetry)this.sendItems[0];
            Assert.IsNull(requestTelmetry.Context.Operation.ParentId);
            Assert.AreEqual("HOME", requestTelmetry.Context.Operation.Id);
        }

        [TestMethod]
        public void StartOperationCanOverrideRootAndParentOperationId()
        {
            using (this.telemetryClient.StartOperation<RequestTelemetry>("Request", operationId: "ROOT", parentOperationId: "PARENT"))
            {
                this.telemetryClient.TrackTrace("child trace");
            }

            Assert.AreEqual(2, this.sendItems.Count);

            var requestTelmetry = (RequestTelemetry)this.sendItems.Single(t => t is RequestTelemetry);
            Assert.AreEqual("PARENT", requestTelmetry.Context.Operation.ParentId);
            Assert.AreEqual("ROOT", requestTelmetry.Context.Operation.Id);

            var traceTelemetry = (TraceTelemetry)this.sendItems.Single(t => t is TraceTelemetry);
            Assert.AreEqual(requestTelmetry.Id, traceTelemetry.Context.Operation.ParentId);
            Assert.AreEqual("ROOT", traceTelemetry.Context.Operation.Id);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void StartOperationThrowsOnNullOperationTelemetry()
        {
            this.telemetryClient.StartOperation<RequestTelemetry>(operationTelemetry: null);
        }

        [TestMethod]
        public void StartOperationWithOperationTelemetrySetsOperationHolderTelemetry()
        {
            var requestTelemetry = new RequestTelemetry
            {
                Name = "REQUEST",
                Id = "1"
            };

            using (var operationHolder = this.telemetryClient.StartOperation<RequestTelemetry>(requestTelemetry))
            {
                Assert.AreEqual(requestTelemetry, operationHolder.Telemetry);
            }

            Assert.AreEqual(1, this.sendItems.Count);
            Assert.AreEqual(requestTelemetry, this.sendItems[0]);
        }

        private string GetOperationName(Activity activity)
        {
            return activity.Tags.FirstOrDefault(tag => tag.Key == "OperationName").Value;
        }

        private class ActivityTagsTelemetryIntitializer : ITelemetryInitializer
        {
            public void Initialize(ITelemetry telemetry)
            {
                if (Activity.Current == null)
                {
                    return;
                }

                foreach (var tag in Activity.Current.Tags)
                {
                    telemetry.Context.Properties[tag.Key] = tag.Value;
                }
            }
        }
    }
}
