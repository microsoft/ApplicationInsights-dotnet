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

    /// <summary>
    /// Tests corresponding to TelemetryClientExtension methods.
    /// </summary>
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

        /// <summary>
        /// Tests the scenario if StartOperation returns operation with telemetry item of same type.
        /// </summary>
        [TestMethod]
        public void StartDependencyTrackingReturnsOperationWithSameTelemetryItem()
        {
            var operation = this.telemetryClient.StartOperation<DependencyTelemetry>(null);
            Assert.IsNotNull(operation);
            Assert.IsNotNull(operation.Telemetry);

            CallContext.FreeNamedDataSlot(CallContextHelpers.OperationContextSlotName);
        }

        /// <summary>
        /// Tests the scenario if StartOperation assigns operation name to the telemetry item.
        /// </summary>
        [TestMethod]
        public void StartDependencyTrackingReturnsOperationWithInitializedOperationName()
        {
            var operation = this.telemetryClient.StartOperation<DependencyTelemetry>("TestOperationName");
            Assert.AreEqual("TestOperationName", operation.Telemetry.Context.Operation.RootName);
        }

        /// <summary>
        /// Tests the scenario if StartOperation throws exception when telemetry client is null.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void StartDependencyTrackingThrowsExceptionWithNullTelemetryClient()
        {
            TelemetryClient tc = null;
            tc.StartOperation<DependencyTelemetry>(null);
        }

        /// <summary>
        /// Tests the scenario if StartOperation returns operation with timestamp.
        /// </summary>
        [TestMethod]
        public void StartDependencyTrackingCreatesADependencyTelemetryItemWithTimeStamp()
        {
            var operation = this.telemetryClient.StartOperation<DependencyTelemetry>(null);
            Assert.AreEqual(operation.Telemetry.StartTime, operation.Telemetry.Timestamp);
            Assert.AreNotEqual(operation.Telemetry.StartTime, DateTimeOffset.MinValue);

            CallContext.FreeNamedDataSlot(CallContextHelpers.OperationContextSlotName);
        }

        /// <summary>
        /// Tests the scenario if CallContext is updated with the telemetry item with StartOperation.
        /// </summary>
        [TestMethod]
        public void StartDependencyTrackingAddsOperationContextStoreToCallContext()
        {
            Assert.IsNull(CallContextHelpers.GetCurrentOperationContextFromCallContext());
            var operation = this.telemetryClient.StartOperation<DependencyTelemetry>(null);
            Assert.IsNotNull(CallContextHelpers.GetCurrentOperationContextFromCallContext());

            CallContext.FreeNamedDataSlot(CallContextHelpers.OperationContextSlotName);
        }

        /// <summary>
        /// Tests the scenario if operation item is disposed and telemetry is sent with using.
        /// </summary>
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

        /// <summary>
        /// Tests the scenario if operation item is disposed and telemetry is sent with using.
        /// </summary>
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

        /// <summary>
        /// Tests the scenario if CallContext is updated with multiple operations.
        /// </summary>
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

        /// <summary>
        /// Tests the scenario if StopOperation does not fail when call context is not initialized.
        /// </summary>
        [TestMethod]
        public void StopOperationDoesNotFailOnNullOperation()
        {
            TelemetryClient tc = new TelemetryClient();
            tc.StopOperation<DependencyTelemetry>(null);
        }

        /// <summary>
        /// Tests the scenario if StopOperation throws exception when telemetry client is null.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void StopDependencyTrackingThrowsExceptionWithNullTelemetryClient()
        {
            var operationItem = new CallContextBasedOperationHolder<DependencyTelemetry>(this.telemetryClient, new DependencyTelemetry());
            TelemetryClient tc = null;
            tc.StopOperation(operationItem);
        }

        /// <summary>
        /// Tests the scenario if stop operation does not throw exception when a parent operation is being stopped before the child operation.
        /// </summary>
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

        /// <summary>
        /// Tests the scenario if stop operation works fine with nested operations.
        /// </summary>
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
