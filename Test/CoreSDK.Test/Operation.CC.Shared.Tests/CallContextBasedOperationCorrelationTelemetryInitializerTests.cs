namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Runtime.Remoting.Messaging;
    using Extensibility.Implementation;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CallContextBasedOperationCorrelationTelemetryInitializerTests
    {
        [TestMethod]
        public void InitializerDoesNotFailOnNullContextStore()
        {
            var telemetry = new DependencyTelemetry();
            this.SetOperationContextToCallContext(null);
            (new CallContextBasedOperationCorrelationTelemetryInitializer()).Initialize(telemetry);
            Assert.IsNull(telemetry.Context.Operation.ParentId);
        }

        [TestMethod]
        public void TelemetryContextIsUpdatedWithOperationIdForDependencyTelemetry()
        {
            this.SetOperationContextToCallContext(new OperationContextForCallContext { ParentOperationId = "ParentOperationId" });
            var telemetry = new DependencyTelemetry();
            (new CallContextBasedOperationCorrelationTelemetryInitializer()).Initialize(telemetry);
            Assert.AreEqual("ParentOperationId", telemetry.Context.Operation.ParentId);
            CallContext.FreeNamedDataSlot(CallContextHelpers.OperationContextSlotName);
        }

        [TestMethod]
        public void InitializeDoesNotUpdateOperationIdIfItExists()
        {
            this.SetOperationContextToCallContext(new OperationContextForCallContext { ParentOperationId = "ParentOperationId" });
            var telemetry = new DependencyTelemetry();
            telemetry.Context.Operation.ParentId = "OldParentOperationId";
            (new CallContextBasedOperationCorrelationTelemetryInitializer()).Initialize(telemetry);
            Assert.AreEqual("OldParentOperationId", telemetry.Context.Operation.ParentId);
            CallContext.FreeNamedDataSlot(CallContextHelpers.OperationContextSlotName);
        }

        [TestMethod]
        public void TelemetryContextIsUpdatedWithRootOperationIdForDependencyTelemetry()
        {
            this.SetOperationContextToCallContext(new OperationContextForCallContext { RootOperationId = "RootOperationId" });
            var telemetry = new DependencyTelemetry();
            (new CallContextBasedOperationCorrelationTelemetryInitializer()).Initialize(telemetry);
            Assert.AreEqual("RootOperationId", telemetry.Context.Operation.RootId);
            CallContext.FreeNamedDataSlot(CallContextHelpers.OperationContextSlotName);
        }

        [TestMethod]
        public void InitializeDoesNotUpdateRootOperationIdIfItExists()
        {
            this.SetOperationContextToCallContext(new OperationContextForCallContext { RootOperationId = "RootOperationId" });
            var telemetry = new DependencyTelemetry();
            telemetry.Context.Operation.RootId = "OldRootOperationId";
            (new CallContextBasedOperationCorrelationTelemetryInitializer()).Initialize(telemetry);
            Assert.AreEqual("OldRootOperationId", telemetry.Context.Operation.RootId);
            CallContext.FreeNamedDataSlot(CallContextHelpers.OperationContextSlotName);
        }

        [TestMethod]
        public void TelemetryContextIsUpdatedWithOperationNameForDependencyTelemetry()
        {
            this.SetOperationContextToCallContext(new OperationContextForCallContext { OperationName = "OperationName" });
            var telemetry = new DependencyTelemetry();
            (new CallContextBasedOperationCorrelationTelemetryInitializer()).Initialize(telemetry);
            Assert.AreEqual(telemetry.Context.Operation.RootName, "OperationName");
            CallContext.FreeNamedDataSlot(CallContextHelpers.OperationContextSlotName);
        }

        [TestMethod]
        public void InitializeDoesNotUpdateOperationNameIfItExists()
        {
            this.SetOperationContextToCallContext(new OperationContextForCallContext { OperationName = "OperationName" });
            var telemetry = new DependencyTelemetry();
            telemetry.Context.Operation.RootName = "OldOperationName";
            (new CallContextBasedOperationCorrelationTelemetryInitializer()).Initialize(telemetry);
            Assert.AreEqual(telemetry.Context.Operation.RootName, "OldOperationName");
            CallContext.FreeNamedDataSlot(CallContextHelpers.OperationContextSlotName);
        }

        private void SetOperationContextToCallContext(OperationContextForCallContext operationContext)
        {
            CallContext.LogicalSetData(CallContextHelpers.OperationContextSlotName, operationContext);
        }
    }
}
