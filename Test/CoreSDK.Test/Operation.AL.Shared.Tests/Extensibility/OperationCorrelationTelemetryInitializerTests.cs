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
            AsyncLocalHelpers.SaveOperationContext(null);
            (new OperationCorrelationTelemetryInitializer()).Initialize(telemetry);
            Assert.IsNull(telemetry.Context.Operation.ParentId);
        }

        [TestMethod]
        public void TelemetryContextIsUpdatedWithOperationIdForDependencyTelemetry()
        {
            AsyncLocalHelpers.SaveOperationContext(new OperationContextForAsyncLocal { ParentOperationId = "ParentOperationId" });
            var telemetry = new DependencyTelemetry();
            (new OperationCorrelationTelemetryInitializer()).Initialize(telemetry);
            Assert.AreEqual("ParentOperationId", telemetry.Context.Operation.ParentId);
            AsyncLocalHelpers.SaveOperationContext(null);
        }

        [TestMethod]
        public void InitializeDoesNotUpdateOperationIdIfItExists()
        {
            AsyncLocalHelpers.SaveOperationContext(new OperationContextForAsyncLocal { ParentOperationId = "ParentOperationId" });
            var telemetry = new DependencyTelemetry();
            telemetry.Context.Operation.ParentId = "OldParentOperationId";
            (new OperationCorrelationTelemetryInitializer()).Initialize(telemetry);
            Assert.AreEqual("OldParentOperationId", telemetry.Context.Operation.ParentId);
            AsyncLocalHelpers.SaveOperationContext(null);
        }

        [TestMethod]
        public void TelemetryContextIsUpdatedWithRootOperationIdForDependencyTelemetry()
        {
            AsyncLocalHelpers.SaveOperationContext(new OperationContextForAsyncLocal { RootOperationId = "RootOperationId" });
            var telemetry = new DependencyTelemetry();
            (new OperationCorrelationTelemetryInitializer()).Initialize(telemetry);
            Assert.AreEqual("RootOperationId", telemetry.Context.Operation.RootId);
            AsyncLocalHelpers.SaveOperationContext(null);
        }

        [TestMethod]
        public void InitializeDoesNotUpdateRootOperationIdIfItExists()
        {
            AsyncLocalHelpers.SaveOperationContext(new OperationContextForAsyncLocal { RootOperationId = "RootOperationId" });
            var telemetry = new DependencyTelemetry();
            telemetry.Context.Operation.RootId = "OldRootOperationId";
            (new OperationCorrelationTelemetryInitializer()).Initialize(telemetry);
            Assert.AreEqual("OldRootOperationId", telemetry.Context.Operation.RootId);
            AsyncLocalHelpers.SaveOperationContext(null);
        }

        [TestMethod]
        public void TelemetryContextIsUpdatedWithOperationNameForDependencyTelemetry()
        {
            AsyncLocalHelpers.SaveOperationContext(new OperationContextForAsyncLocal { OperationName = "OperationName" });
            var telemetry = new DependencyTelemetry();
            (new OperationCorrelationTelemetryInitializer()).Initialize(telemetry);
            Assert.AreEqual(telemetry.Context.Operation.RootName, "OperationName");
            AsyncLocalHelpers.SaveOperationContext(null);
        }

        [TestMethod]
        public void InitializeDoesNotUpdateOperationNameIfItExists()
        {
            AsyncLocalHelpers.SaveOperationContext(new OperationContextForAsyncLocal { OperationName = "OperationName" });
            var telemetry = new DependencyTelemetry();
            telemetry.Context.Operation.RootName = "OldOperationName";
            (new OperationCorrelationTelemetryInitializer()).Initialize(telemetry);
            Assert.AreEqual(telemetry.Context.Operation.RootName, "OldOperationName");
            AsyncLocalHelpers.SaveOperationContext(null);
        }
    }
}
