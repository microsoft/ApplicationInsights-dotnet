namespace Microsoft.ApplicationInsights.Extensibility
{
    using Implementation;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Collections.Generic;

    [TestClass]
    public class OperationCorrelationTelemetryInitializerTests
    {
        [TestMethod]
        public void InitializerDoesNotFailOnNullContextStore()
        {
            var telemetry = new DependencyTelemetry();
            CallContextHelpers.SaveOperationContext(null);
            (new OperationCorrelationTelemetryInitializer()).Initialize(telemetry);
            Assert.IsNull(telemetry.Context.Operation.ParentId);
        }

        [TestMethod]
        public void TelemetryContextIsUpdatedWithOperationIdForDependencyTelemetry()
        {
            CallContextHelpers.SaveOperationContext(new OperationContextForCallContext { ParentOperationId = "ParentOperationId" });
            var telemetry = new DependencyTelemetry();
            (new OperationCorrelationTelemetryInitializer()).Initialize(telemetry);
            Assert.AreEqual("ParentOperationId", telemetry.Context.Operation.ParentId);
            CallContextHelpers.RestoreOperationContext(null);
        }

        [TestMethod]
        public void InitializeDoesNotUpdateOperationIdIfItExists()
        {
            CallContextHelpers.SaveOperationContext(new OperationContextForCallContext { ParentOperationId = "ParentOperationId" });
            var telemetry = new DependencyTelemetry();
            telemetry.Context.Operation.ParentId = "OldParentOperationId";
            (new OperationCorrelationTelemetryInitializer()).Initialize(telemetry);
            Assert.AreEqual("OldParentOperationId", telemetry.Context.Operation.ParentId);
            CallContextHelpers.RestoreOperationContext(null);
        }

        [TestMethod]
        public void TelemetryContextIsUpdatedWithOperationNameForDependencyTelemetry()
        {
            CallContextHelpers.SaveOperationContext(new OperationContextForCallContext { RootOperationName = "OperationName" });
            var telemetry = new DependencyTelemetry();
            (new OperationCorrelationTelemetryInitializer()).Initialize(telemetry);
            Assert.AreEqual(telemetry.Context.Operation.Name, "OperationName");
            CallContextHelpers.RestoreOperationContext(null);
        }

        [TestMethod]
        public void InitializeDoesNotUpdateOperationNameIfItExists()
        {
            CallContextHelpers.SaveOperationContext(new OperationContextForCallContext { RootOperationName = "OperationName" });
            var telemetry = new DependencyTelemetry();
            telemetry.Context.Operation.Name = "OldOperationName";
            (new OperationCorrelationTelemetryInitializer()).Initialize(telemetry);
            Assert.AreEqual(telemetry.Context.Operation.Name, "OldOperationName");
            CallContextHelpers.RestoreOperationContext(null);
        }

        [TestMethod]
        public void InitializeWithCorrelationContextSetsProperties()
        {
            CallContextHelpers.SaveOperationContext(new OperationContextForCallContext
            {
                CorrelationContext = new Dictionary<string, string> { ["k1"] = "v1", ["k2"] = "v2" }
            });

            var telemetry = new DependencyTelemetry();
            (new OperationCorrelationTelemetryInitializer()).Initialize(telemetry);

            Assert.AreEqual(2, telemetry.Context.Properties.Count);
            Assert.AreEqual("v1", telemetry.Context.Properties["k1"]);
            Assert.AreEqual("v2", telemetry.Context.Properties["k2"]);
            CallContextHelpers.RestoreOperationContext(null);
        }

        [TestMethod]
        public void InitializeWithCorrelationContextSetsPropertiesIfNotDuplicated()
        {
            CallContextHelpers.SaveOperationContext(new OperationContextForCallContext
            {
                CorrelationContext = new Dictionary<string, string> { ["k1"] = "v1", ["k2"] = "v2" }
            });

            var telemetry = new DependencyTelemetry();
            telemetry.Context.Properties["k1"] = "v";
            (new OperationCorrelationTelemetryInitializer()).Initialize(telemetry);

            Assert.AreEqual(2, telemetry.Context.Properties.Count);
            Assert.AreEqual("v", telemetry.Context.Properties["k1"]);
            Assert.AreEqual("v2", telemetry.Context.Properties["k2"]);
            CallContextHelpers.RestoreOperationContext(null);
        }
    }
}