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
        public void InitializeSetsParentAndCurrentCorrelationContextProperties()
        {
            CallContextHelpers.SaveOperationContext(new OperationContextForCallContext
            {
                CorrelationContext = new Dictionary<string, string>
                {
                    ["1"] = "parent1",
                    ["2"] = "parent2"
                }
            });
            var telemetry = new DependencyTelemetry();
            telemetry.Context.CorrelationContext["1"] = "child1";
            telemetry.Context.CorrelationContext["3"] = "child3";

            (new OperationCorrelationTelemetryInitializer()).Initialize(telemetry);

            Assert.AreEqual("child1", telemetry.Context.Properties["1"]);
            Assert.AreEqual("parent2", telemetry.Context.Properties["2"]);
            Assert.AreEqual("child3", telemetry.Context.Properties["3"]);
            Assert.AreEqual(3, telemetry.Context.Properties.Count);
            CallContextHelpers.RestoreOperationContext(null);
        }

        [TestMethod]
        public void InitializeSetsParentCorrelationContextProperties()
        {
            CallContextHelpers.SaveOperationContext(new OperationContextForCallContext
            {
                CorrelationContext = new Dictionary<string, string>
                {
                    ["1"] = "parent1",
                    ["2"] = "parent2"
                }
            });
            var telemetry = new DependencyTelemetry();
            (new OperationCorrelationTelemetryInitializer()).Initialize(telemetry);

            Assert.AreEqual("parent1", telemetry.Context.Properties["1"]);
            Assert.AreEqual("parent2", telemetry.Context.Properties["2"]);
            Assert.AreEqual(2, telemetry.Context.Properties.Count);
            CallContextHelpers.RestoreOperationContext(null);
        }

        [TestMethod]
        public void InitializeSetsCurrentCorrelationContextProperties()
        {
            var telemetry = new DependencyTelemetry();
            telemetry.Context.CorrelationContext["1"] = "1";
            telemetry.Context.CorrelationContext["2"] = "2";

            (new OperationCorrelationTelemetryInitializer()).Initialize(telemetry);

            Assert.AreEqual("1", telemetry.Context.Properties["1"]);
            Assert.AreEqual("2", telemetry.Context.Properties["2"]);
            Assert.AreEqual(2, telemetry.Context.Properties.Count);
            CallContextHelpers.RestoreOperationContext(null);
        }

    }
}
