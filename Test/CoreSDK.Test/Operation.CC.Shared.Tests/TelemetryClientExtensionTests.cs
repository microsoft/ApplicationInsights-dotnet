namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Remoting.Messaging;
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
            this.telemetryClient = new TelemetryClient(configuration);
            CallContext.FreeNamedDataSlot(CallContextHelpers.OperationContextSlotName);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            CallContext.FreeNamedDataSlot(CallContextHelpers.OperationContextSlotName); 
        }

        [TestMethod]
        public void StartDependencyTrackingReturnsOperationWithSameTelemetryItem()
        {
            var operation = this.telemetryClient.StartOperation<DependencyTelemetry>(null);
            Assert.IsNotNull(operation);
            Assert.IsNotNull(operation.Telemetry);

            CallContext.FreeNamedDataSlot(CallContextHelpers.OperationContextSlotName);
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
            Assert.IsNotNull(operation.Telemetry.Context.Operation.RootId);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void StartDependencyTrackingThrowsExceptionWithNullTelemetryClient()
        {
            TelemetryClient tc = null;
            tc.StartOperation<DependencyTelemetry>(null);
        }

        [TestMethod]
        public void StartDependencyTrackingCreatesADependencyTelemetryItemWithTimeStamp()
        {
            var operation = this.telemetryClient.StartOperation<DependencyTelemetry>(null);
            Assert.AreEqual(operation.Telemetry.StartTime, operation.Telemetry.Timestamp);
            Assert.AreNotEqual(operation.Telemetry.StartTime, DateTimeOffset.MinValue);

            CallContext.FreeNamedDataSlot(CallContextHelpers.OperationContextSlotName);
        }

        [TestMethod]
        public void StartDependencyTrackingAddsOperationContextStoreToCallContext()
        {
            Assert.IsNull(CallContextHelpers.GetCurrentOperationContextFromCallContext());
            var operation = this.telemetryClient.StartOperation<DependencyTelemetry>(null);
            Assert.IsNotNull(CallContextHelpers.GetCurrentOperationContextFromCallContext());

            CallContext.FreeNamedDataSlot(CallContextHelpers.OperationContextSlotName);
        }

        [TestMethod]
        public void UsingSendsTelemetryAndDisposesOperationItem()
        {
            Assert.IsNull(CallContextHelpers.GetCurrentOperationContextFromCallContext());
            using (var operation = this.telemetryClient.StartOperation<DependencyTelemetry>(null))
            {
            }

            Assert.IsNull(CallContextHelpers.GetCurrentOperationContextFromCallContext());
            Assert.AreEqual(1, this.sendItems.Count);
            CallContext.FreeNamedDataSlot(CallContextHelpers.OperationContextSlotName);
        }

        [TestMethod]
        public void UsingWithStopOperationSendsTelemetryAndDisposesOperationItemOnlyOnce()
        {
            Assert.IsNull(CallContextHelpers.GetCurrentOperationContextFromCallContext());
            using (var operation = this.telemetryClient.StartOperation<DependencyTelemetry>(null))
            {
                this.telemetryClient.StopOperation(operation);
            }

            Assert.IsNull(CallContextHelpers.GetCurrentOperationContextFromCallContext());
            Assert.AreEqual(1, this.sendItems.Count);
        }

        [TestMethod]
        public void StartDependencyTrackingHandlesMultipleContextStoresInCallContext()
        {
            var operation = this.telemetryClient.StartOperation<DependencyTelemetry>("OperationName") as CallContextBasedOperationHolder<DependencyTelemetry>;
            var parentContextStore = CallContextHelpers.GetCurrentOperationContextFromCallContext();
            Assert.AreEqual(operation.Telemetry.Context.Operation.Id, parentContextStore.ParentOperationId);
            Assert.AreEqual(operation.Telemetry.Context.Operation.RootName, parentContextStore.OperationName);

            var childOperation = this.telemetryClient.StartOperation<DependencyTelemetry>("OperationName") as CallContextBasedOperationHolder<DependencyTelemetry>;
            var childContextStore = CallContextHelpers.GetCurrentOperationContextFromCallContext();
            Assert.AreEqual(childOperation.Telemetry.Context.Operation.Id, childContextStore.ParentOperationId);
            Assert.AreEqual(childOperation.Telemetry.Context.Operation.RootName, childContextStore.OperationName);

            Assert.IsNull(operation.ParentContext);
            Assert.AreEqual(parentContextStore, childOperation.ParentContext);

            this.telemetryClient.StopOperation(childOperation);
            Assert.AreEqual(parentContextStore, CallContextHelpers.GetCurrentOperationContextFromCallContext());
            this.telemetryClient.StopOperation(operation);
            Assert.IsNull(CallContextHelpers.GetCurrentOperationContextFromCallContext());
        }

        [TestMethod]
        public void StopOperationDoesNotFailOnNullOperation()
        {
            TelemetryClient tc = new TelemetryClient();
            tc.StopOperation<DependencyTelemetry>(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void StopDependencyTrackingThrowsExceptionWithNullTelemetryClient()
        {
            var operationItem = new CallContextBasedOperationHolder<DependencyTelemetry>(this.telemetryClient, new DependencyTelemetry());
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
    }
}
