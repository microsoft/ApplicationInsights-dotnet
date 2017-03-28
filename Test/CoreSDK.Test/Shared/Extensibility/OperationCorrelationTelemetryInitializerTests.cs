namespace Microsoft.ApplicationInsights.Extensibility
{
#if !NET40
    using System.Diagnostics;
#endif
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

#if !NET40
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
            Assert.AreEqual("parent", telemetry.Context.Operation.ParentId);
            Assert.AreEqual("operation", telemetry.Context.Operation.Name);
            Assert.AreEqual(currentActivity.Id, telemetry.Id);
            Assert.IsTrue(telemetry.Id.StartsWith("|parent."));

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
            Assert.AreEqual("activityRoot", telemetry.Context.Operation.ParentId);
            Assert.AreEqual("operation", telemetry.Context.Operation.Name);
            Assert.AreEqual(currentActivity.Id, telemetry.Id);
            Assert.IsTrue(telemetry.Id.StartsWith("|activityRoot."));
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
            var telemetry = new RequestTelemetry();

            telemetry.Context.Operation.Id = "rootId";
            telemetry.Context.Operation.ParentId = null;
            telemetry.Context.Operation.Name = "operation";
            telemetry.Id = "12345";
            (new OperationCorrelationTelemetryInitializer()).Initialize(telemetry);

            Assert.AreEqual("rootId", telemetry.Context.Operation.Id);
            Assert.IsNull(telemetry.Context.Operation.ParentId);
            Assert.AreEqual("operation", telemetry.Context.Operation.Name);
            Assert.AreEqual("12345", telemetry.Id);
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
            var telemetry = new RequestTelemetry();

            telemetry.Context.Operation.ParentId = "parentId";
            telemetry.Context.Operation.Name = "operation";
            telemetry.Id = "12345";
            (new OperationCorrelationTelemetryInitializer()).Initialize(telemetry);

            Assert.AreEqual("activityRoot", telemetry.Context.Operation.Id);
            Assert.AreEqual("parentId", telemetry.Context.Operation.ParentId);
            Assert.AreEqual("operation", telemetry.Context.Operation.Name);
            Assert.AreEqual(currentActivity.Id, telemetry.Id);
            Assert.AreEqual(1, telemetry.Context.Properties.Count);
            Assert.AreEqual("v1", telemetry.Context.Properties["k1"]);
            currentActivity.Stop();
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
#endif
    }
}