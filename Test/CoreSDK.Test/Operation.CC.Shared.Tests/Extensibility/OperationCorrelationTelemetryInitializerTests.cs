namespace Microsoft.ApplicationInsights.Extensibility
{
    using System.Runtime.Remoting.Messaging;
    using Implementation;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class OperationCorrelationTelemetryInitializerTests
    {
        [TestMethod]
        public void InitializerDoesNotFailOnNullContextStore()
        {
            var telemetry = new DependencyTelemetry();
            this.SetOperationContextToCallContext(null);
            (new OperationCorrelationTelemetryInitializer()).Initialize(telemetry);
            Assert.IsNull(telemetry.Context.Operation.ParentId);
        }

        [TestMethod]
        public void TelemetryContextIsUpdatedWithOperationIdForDependencyTelemetry()
        {
            this.SetOperationContextToCallContext(new OperationContextForCallContext { ParentOperationId = "ParentOperationId" });
            var telemetry = new DependencyTelemetry();
            (new OperationCorrelationTelemetryInitializer()).Initialize(telemetry);
            Assert.AreEqual("ParentOperationId", telemetry.Context.Operation.ParentId);
            CallContext.FreeNamedDataSlot(CallContextHelpers.OperationContextSlotName);
        }

        [TestMethod]
        public void InitializeDoesNotUpdateOperationIdIfItExists()
        {
            this.SetOperationContextToCallContext(new OperationContextForCallContext { ParentOperationId = "ParentOperationId" });
            var telemetry = new DependencyTelemetry();
            telemetry.Context.Operation.ParentId = "OldParentOperationId";
            (new OperationCorrelationTelemetryInitializer()).Initialize(telemetry);
            Assert.AreEqual("OldParentOperationId", telemetry.Context.Operation.ParentId);
            CallContext.FreeNamedDataSlot(CallContextHelpers.OperationContextSlotName);
        }

        [TestMethod]
        public void TelemetryContextIsUpdatedWithOperationNameForDependencyTelemetry()
        {
            this.SetOperationContextToCallContext(new OperationContextForCallContext { RootOperationName = "OperationName" });
            var telemetry = new DependencyTelemetry();
            (new OperationCorrelationTelemetryInitializer()).Initialize(telemetry);
            Assert.AreEqual(telemetry.Context.Operation.Name, "OperationName");
            CallContext.FreeNamedDataSlot(CallContextHelpers.OperationContextSlotName);
        }

        [TestMethod]
        public void InitializeDoesNotUpdateOperationNameIfItExists()
        {
            this.SetOperationContextToCallContext(new OperationContextForCallContext { RootOperationName = "OperationName" });
            var telemetry = new DependencyTelemetry();
            telemetry.Context.Operation.Name = "OldOperationName";
            (new OperationCorrelationTelemetryInitializer()).Initialize(telemetry);
            Assert.AreEqual(telemetry.Context.Operation.Name, "OldOperationName");
            CallContext.FreeNamedDataSlot(CallContextHelpers.OperationContextSlotName);
        }

        private void SetOperationContextToCallContext(OperationContextForCallContext operationContext)
        {
            CallContext.LogicalSetData(CallContextHelpers.OperationContextSlotName, operationContext);
        }
    }
}
