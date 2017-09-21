namespace Microsoft.ApplicationInsights.Extensibility
{
    using System.Diagnostics;
    using Implementation;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class OperationCorrelationTelemetryInitializerTests
    {
        [TestMethod]
        public void InitializerDoesNotFailOnNullCurrentActivity()
        {
            var telemetry = new DependencyTelemetry();
            (new OperationCorrelationTelemetryInitializer()).Initialize(telemetry);
            Assert.IsNull(telemetry.Context.Operation.ParentId);
        }

        [TestMethod]
        public void TelemetryContextIsUpdatedWithOperationIdForDependencyTelemetry()
        {
            Activity parent = new Activity("parent").SetParentId("ParentOperationId").Start();

            var telemetry = new DependencyTelemetry();
            (new OperationCorrelationTelemetryInitializer()).Initialize(telemetry);

            Assert.AreEqual("ParentOperationId", telemetry.Context.Operation.Id);
            Assert.AreEqual(parent.Id, telemetry.Context.Operation.ParentId);
            parent.Stop();
        }

        [TestMethod]
        public void InitializeDoesNotUpdateOperationIdIfItExists()
        {
            Activity parent = new Activity("parent").SetParentId("ParentOperationId").Start();

            var telemetry = new DependencyTelemetry();
            telemetry.Context.Operation.ParentId = "OldParentOperationId";
            (new OperationCorrelationTelemetryInitializer()).Initialize(telemetry);
            Assert.AreEqual("OldParentOperationId", telemetry.Context.Operation.ParentId);
            parent.Stop();
        }

        [TestMethod]
        public void TelemetryContextIsUpdatedWithOperationNameForDependencyTelemetry()
        {
            Activity parent = new Activity("parent");
            parent.AddTag("OperationName", "OperationName");
            parent.Start();

            var telemetry = new DependencyTelemetry();
            (new OperationCorrelationTelemetryInitializer()).Initialize(telemetry);
            Assert.AreEqual(telemetry.Context.Operation.Name, "OperationName");
            parent.Stop();
        }

        [TestMethod]
        public void InitilaizeWithActivityWithoutOperationName()
        {
            var currentActivity = new Activity("test");
            currentActivity.Start();
            var telemetry = new RequestTelemetry();

            (new OperationCorrelationTelemetryInitializer()).Initialize(telemetry);

            Assert.IsNull(telemetry.Context.Operation.Name);
            currentActivity.Stop();
        }

        [TestMethod]
        public void InitializeDoesNotUpdateOperationNameIfItExists()
        {
            Activity parent = new Activity("parent");
            parent.AddTag("OperationName", "OperationName");
            parent.Start();

            var telemetry = new DependencyTelemetry();
            telemetry.Context.Operation.Name = "OldOperationName";
            (new OperationCorrelationTelemetryInitializer()).Initialize(telemetry);

            Assert.AreEqual(telemetry.Context.Operation.Name, "OldOperationName");
            parent.Stop();
        }

        [TestMethod]
        public void InitilaizeWithActivitySetsOperationContext()
        {
            var currentActivity = new Activity("test");
            currentActivity.SetParentId("parent");
            currentActivity.AddTag("OperationName", "operation");
            currentActivity.AddBaggage("k1", "v1");
            currentActivity.AddBaggage("k2", "v2");
            currentActivity.Start();
            var telemetry = new RequestTelemetry();
            (new OperationCorrelationTelemetryInitializer()).Initialize(telemetry);

            Assert.AreEqual("parent", telemetry.Context.Operation.Id);
            Assert.AreEqual(currentActivity.Id, telemetry.Context.Operation.ParentId);
            Assert.IsTrue(telemetry.Context.Operation.ParentId.StartsWith("|parent."));
            Assert.AreEqual("operation", telemetry.Context.Operation.Name);

            Assert.AreEqual(2, telemetry.Context.Properties.Count);
            Assert.AreEqual("v1", telemetry.Context.Properties["k1"]);
            Assert.AreEqual("v2", telemetry.Context.Properties["k2"]);
            currentActivity.Stop();
        }

        [TestMethod]
        public void InitilaizeWithActivityWinsOverCallContext()
        {
            CallContextHelpers.SaveOperationContext(new OperationContextForCallContext { RootOperationId = "callContextRoot" });
            var currentActivity = new Activity("test");
            currentActivity.SetParentId("activityRoot");
            currentActivity.AddTag("OperationName", "operation");
            currentActivity.AddBaggage("k1", "v1");
            currentActivity.Start();
            var telemetry = new RequestTelemetry();
            (new OperationCorrelationTelemetryInitializer()).Initialize(telemetry);

            Assert.AreEqual("activityRoot", telemetry.Context.Operation.Id);
            Assert.AreEqual(currentActivity.Id, telemetry.Context.Operation.ParentId);
            Assert.IsTrue(telemetry.Context.Operation.ParentId.StartsWith("|activityRoot."));

            Assert.AreEqual("operation", telemetry.Context.Operation.Name);
            Assert.AreEqual(1, telemetry.Context.Properties.Count);
            Assert.AreEqual("v1", telemetry.Context.Properties["k1"]);
            currentActivity.Stop();
        }

        [TestMethod]
        public void InitilaizeWithActivityDoesntOverrideContextIfRootIsSet()
        {
            var currentActivity = new Activity("test");
            currentActivity.SetParentId("activityRoot");
            currentActivity.AddTag("OperationName", "test");
            currentActivity.AddBaggage("k1", "v1");
            currentActivity.Start();
            var telemetry = new TraceTelemetry();

            telemetry.Context.Operation.Id = "rootId";
            telemetry.Context.Operation.ParentId = null;
            telemetry.Context.Operation.Name = "operation";
            (new OperationCorrelationTelemetryInitializer()).Initialize(telemetry);

            Assert.AreEqual("rootId", telemetry.Context.Operation.Id);
            Assert.IsNull(telemetry.Context.Operation.ParentId);
            Assert.AreEqual("operation", telemetry.Context.Operation.Name);
            Assert.AreEqual(0, telemetry.Context.Properties.Count);
            currentActivity.Stop();
        }

        [TestMethod]
        public void InitilaizeWithActivityOverridesContextIfRootIsNotSet()
        {
            var currentActivity = new Activity("test");
            currentActivity.SetParentId("activityRoot");
            currentActivity.AddTag("OperationName", "test");
            currentActivity.AddBaggage("k1", "v1");
            currentActivity.Start();
            var telemetry = new TraceTelemetry();

            telemetry.Context.Operation.ParentId = "parentId";
            telemetry.Context.Operation.Name = "operation";
            (new OperationCorrelationTelemetryInitializer()).Initialize(telemetry);

            Assert.AreEqual("activityRoot", telemetry.Context.Operation.Id);
            Assert.AreEqual("parentId", telemetry.Context.Operation.ParentId);
            Assert.AreEqual("operation", telemetry.Context.Operation.Name);
            Assert.AreEqual(1, telemetry.Context.Properties.Count);
            Assert.AreEqual("v1", telemetry.Context.Properties["k1"]);
            currentActivity.Stop();
        }
    }
}