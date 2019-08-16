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
    using Microsoft.ApplicationInsights.Extensibility.W3C;

    [TestClass]
    public class TelemetryClientExtensionTests
    {
        const string NonW3CCompatileOperationId = "NonCompliantRootId";
        const string W3CCompatileOperationId = "8ee8641cbdd8dd280d239fa2121c7e4e";
        const string AnyRootId = "ANYID";
        const string AnyParentId = "ANYParentID";

        private TelemetryClient telemetryClient;
        private TelemetryConfiguration telemetryConfiguration;
        private List<ITelemetry> sendItems;

        [TestInitialize]
        public void TestInitialize()
        {
            this.telemetryConfiguration = new TelemetryConfiguration();
            this.sendItems = new List<ITelemetry>();
            telemetryConfiguration.TelemetryChannel = new StubTelemetryChannel { OnSend = item => this.sendItems.Add(item) };
            telemetryConfiguration.InstrumentationKey = Guid.NewGuid().ToString();
            telemetryConfiguration.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());
            this.telemetryClient = new TelemetryClient(telemetryConfiguration);
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
        public void StartDependencyTrackingHandlesMultipleContextStoresInCurrentActivityW3C()
        {
            var operation = this.telemetryClient.StartOperation<DependencyTelemetry>("OperationName") as OperationHolder<DependencyTelemetry>;
            var currentActivity = Activity.Current;
            Assert.AreEqual(operation.Telemetry.Id, W3CActivityExtensions.FormatTelemetryId(currentActivity.TraceId.ToHexString(), currentActivity.SpanId.ToHexString()));
            Assert.AreEqual(operation.Telemetry.Context.Operation.Name, this.GetOperationName(currentActivity));

            var childOperation = this.telemetryClient.StartOperation<DependencyTelemetry>("OperationName") as OperationHolder<DependencyTelemetry>;
            var childActivity = Activity.Current;
            Assert.AreEqual(childOperation.Telemetry.Id, W3CActivityExtensions.FormatTelemetryId(childActivity.TraceId.ToHexString(), childActivity.SpanId.ToHexString()));
            Assert.AreEqual(childOperation.Telemetry.Context.Operation.Name, this.GetOperationName(currentActivity));

            Assert.IsNull(currentActivity.Parent);
            Assert.AreEqual(currentActivity, childActivity.Parent);

            this.telemetryClient.StopOperation(childOperation);
            Assert.AreEqual(currentActivity, Activity.Current);
            this.telemetryClient.StopOperation(operation);
            Assert.IsNull(Activity.Current);
        }

        [TestMethod]
        public void StartDependencyTrackingHandlesMultipleContextStoresInCurrentActivityNonW3C()
        {
            this.telemetryConfiguration.EnableW3CCorrelation = false;
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
        public void StopOperationDoesNotThrowExceptionIfParentOperationIsStoppedBeforeChildOperation()
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
            this.telemetryClient.TelemetryConfiguration.TelemetryInitializers.Add(new ActivityTagsTelemetryInitializer());

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
        public void ContextPropagatesThroughNestedOperationsW3C()
        {
            using (this.telemetryClient.StartOperation<RequestTelemetry>("OuterRequest"))
            {
                using (this.telemetryClient.StartOperation<DependencyTelemetry>("DependentCall"))
                {
                }
            }

            Assert.AreEqual(2, this.sendItems.Count);

            var requestTelemetry = (RequestTelemetry)this.sendItems[1];
            var dependentTelemetry = (DependencyTelemetry)this.sendItems[0];
            Assert.IsNull(requestTelemetry.Context.Operation.ParentId);
            Assert.AreEqual(requestTelemetry.Id, dependentTelemetry.Context.Operation.ParentId);
            Assert.AreEqual(requestTelemetry.Context.Operation.Id, dependentTelemetry.Context.Operation.Id);
            Assert.AreEqual(requestTelemetry.Context.Operation.Name, dependentTelemetry.Context.Operation.Name);
        }

        [TestMethod]
        public void ContextPropagatesThroughNestedOperationsNonW3C()
        {
            this.telemetryConfiguration.EnableW3CCorrelation = false;

            using (this.telemetryClient.StartOperation<RequestTelemetry>("OuterRequest"))
            {
                using (this.telemetryClient.StartOperation<DependencyTelemetry>("DependentCall"))
                {
                }
            }

            Assert.AreEqual(2, this.sendItems.Count);

            var requestTelemetry = (RequestTelemetry)this.sendItems[1];
            var dependentTelemetry = (DependencyTelemetry)this.sendItems[0];
            Assert.IsNull(requestTelemetry.Context.Operation.ParentId);
            Assert.AreEqual(requestTelemetry.Id, dependentTelemetry.Context.Operation.ParentId);
            Assert.AreEqual(requestTelemetry.Context.Operation.Id, dependentTelemetry.Context.Operation.Id);
            Assert.AreEqual(requestTelemetry.Context.Operation.Name, dependentTelemetry.Context.Operation.Name);
        }

        [TestMethod]
        public void StartOperationCanOverrideOperationIdNonW3C()
        {
            this.telemetryConfiguration.EnableW3CCorrelation = false;

            using (this.telemetryClient.StartOperation<RequestTelemetry>("Request", "HOME"))
            {
            }

            Assert.AreEqual(1, this.sendItems.Count);

            var requestTelemetry = (RequestTelemetry)this.sendItems[0];
            Assert.IsNull(requestTelemetry.Context.Operation.ParentId);
            Assert.AreEqual("HOME", requestTelemetry.Context.Operation.Id);
        }

        [TestMethod]
        public void StartOperationOperationIdIsIgnoredIfNotW3cCompatible()
        {
            using (this.telemetryClient.StartOperation<RequestTelemetry>("Request", "HOME"))
            {
            }

            Assert.AreEqual(1, this.sendItems.Count);

            var requestTelemetry = (RequestTelemetry)this.sendItems[0];
            Assert.IsNull(requestTelemetry.Context.Operation.ParentId);
            Assert.AreEqual("HOME", requestTelemetry.Properties[W3CConstants.LegacyRootIdProperty]);
        }

        [TestMethod]
        public void StartOperationOperationIdIsUsedIfW3cCompatible()
        {
            using (this.telemetryClient.StartOperation<RequestTelemetry>("Request", "8ee8641cbdd8dd280d239fa2121c7e4e"))
            {
            }

            Assert.AreEqual(1, this.sendItems.Count);

            var requestTelemetry = (RequestTelemetry)this.sendItems[0];
            Assert.IsNull(requestTelemetry.Context.Operation.ParentId);
            Assert.AreEqual("8ee8641cbdd8dd280d239fa2121c7e4e", requestTelemetry.Context.Operation.Id);
        }

        [TestMethod]
        public void StartOperationCanOverrideRootAndParentOperationIdNonW3C()
        {
            this.telemetryConfiguration.EnableW3CCorrelation = false;

            using (this.telemetryClient.StartOperation<RequestTelemetry>("Request", operationId: "ROOT", parentOperationId: "PARENT"))
            {
                this.telemetryClient.TrackTrace("child trace");
            }

            Assert.AreEqual(2, this.sendItems.Count);

            var requestTelemetry = (RequestTelemetry)this.sendItems.Single(t => t is RequestTelemetry);
            Assert.AreEqual("PARENT", requestTelemetry.Context.Operation.ParentId);
            Assert.AreEqual("ROOT", requestTelemetry.Context.Operation.Id);

            var traceTelemetry = (TraceTelemetry)this.sendItems.Single(t => t is TraceTelemetry);
            Assert.AreEqual(requestTelemetry.Id, traceTelemetry.Context.Operation.ParentId);
            Assert.AreEqual("ROOT", traceTelemetry.Context.Operation.Id);
        }

        [TestMethod]
        public void StartOperationCanOverrideRootAndParentOperationIdNotW3CCompatible()
        {
            this.telemetryConfiguration.EnableW3CCorrelation = false;

            using (this.telemetryClient.StartOperation<RequestTelemetry>("Request", operationId: "ROOT", parentOperationId: "PARENT"))
            {
                this.telemetryClient.TrackTrace("child trace");
            }

            Assert.AreEqual(2, this.sendItems.Count);

            var requestTelemetry = (RequestTelemetry)this.sendItems.Single(t => t is RequestTelemetry);
            Assert.AreEqual("PARENT", requestTelemetry.Context.Operation.ParentId);
            Assert.AreEqual("ROOT", requestTelemetry.Context.Operation.Id);

            var traceTelemetry = (TraceTelemetry)this.sendItems.Single(t => t is TraceTelemetry);
            Assert.AreEqual(requestTelemetry.Id, traceTelemetry.Context.Operation.ParentId);
            Assert.AreEqual("ROOT", traceTelemetry.Context.Operation.Id);
        }

        //
        [TestMethod]
        public void StartOperationPopulatesContextCorrectlyW3C()
        {            
            // Act - start an operation, and generate telemetry inside it.
            using (this.telemetryClient.StartOperation<RequestTelemetry>("Request"))
            {
                this.telemetryClient.TrackTrace("child trace");
                this.telemetryClient.TrackEvent("child event");
            }

            Assert.AreEqual(3, this.sendItems.Count);

            // The RequestTelemetry is the root operation here.
            var requestTelemetry = (RequestTelemetry)this.sendItems.Single(t => t is RequestTelemetry);
            ValidateRootTelemetry(requestTelemetry);

            // The generated TraceTelemetry should become the child of the root RequestTelemetry
            var traceTelemetry = (TraceTelemetry)this.sendItems.Single(t => t is TraceTelemetry);
            ValidateChildTelemetry(requestTelemetry, traceTelemetry);            

            // The generated EventTelemetry should become the child of the root RequestTelemetry
            var eventTelemetry = (TraceTelemetry)this.sendItems.Single(t => t is TraceTelemetry);
            ValidateChildTelemetry(requestTelemetry, eventTelemetry);
        }

        private void ValidateRootTelemetry(OperationTelemetry operationTelemetry, string expectedOperationId = "", string expectedOperationParentId = null, bool isW3C = true)
        {
            Assert.AreEqual(expectedOperationParentId, operationTelemetry.Context.Operation.ParentId);
            Assert.IsNotNull(operationTelemetry.Context.Operation.Id);

            if (!string.IsNullOrEmpty(expectedOperationId))
            {
                Assert.AreEqual(expectedOperationId, operationTelemetry.Context.Operation.Id);
            }

            if (isW3C)
            {
                Assert.IsTrue(W3CActivityExtensions.IsCompatibleW3CTraceID(operationTelemetry.Context.Operation.Id));
            }
            Assert.IsNotNull(operationTelemetry.Id);
            // ID is shaped like |TraceID.SpanID.
            Assert.IsTrue(operationTelemetry.Id.Contains(operationTelemetry.Context.Operation.Id));
        }

        private void ValidateChildTelemetry(OperationTelemetry rootOperationTelemetry, ITelemetry childTelemetry)
        {
            Assert.AreEqual(rootOperationTelemetry.Id, childTelemetry.Context.Operation.ParentId);
            Assert.AreEqual(rootOperationTelemetry.Context.Operation.Id, childTelemetry.Context.Operation.Id, "OperationID should be same for all operations in same context");
        }

        [TestMethod]
        public void StartOperationPopulatesContextCorrectlyNonW3C()
        {
            this.telemetryConfiguration.EnableW3CCorrelation = false;
            // Act - start an operation, and generate telemetry inside it.
            using (this.telemetryClient.StartOperation<RequestTelemetry>("Request"))
            {
                this.telemetryClient.TrackTrace("child trace");
                this.telemetryClient.TrackEvent("child event");
            }

            Assert.AreEqual(3, this.sendItems.Count);

            // The RequestTelemetry is the root operation here.
            var requestTelemetry = (RequestTelemetry)this.sendItems.Single(t => t is RequestTelemetry);
            ValidateRootTelemetry(requestTelemetry, isW3C: false);

            // The generated TraceTelemetry should become the child of the root RequestTelemetry
            var traceTelemetry = (TraceTelemetry)this.sendItems.Single(t => t is TraceTelemetry);
            ValidateChildTelemetry(requestTelemetry, traceTelemetry);

            // The generated EventTelemetry should become the child of the root RequestTelemetry
            var eventTelemetry = (TraceTelemetry)this.sendItems.Single(t => t is TraceTelemetry);
            ValidateChildTelemetry(requestTelemetry, eventTelemetry);
        }

        [TestMethod]
        public void StartOperationPopulatesContextCorrectlyWithOverridingNonW3CCompatibleRootIdW3C()
        {            
            // Act - start an operation, supply operation ID which is NOT W3C compatible, and generate a telemetry inside it.
            using (this.telemetryClient.StartOperation<RequestTelemetry>("Request", operationId: NonW3CCompatileOperationId))
            {
                this.telemetryClient.TrackTrace("child trace");
                this.telemetryClient.TrackEvent("child event");
            }

            Assert.AreEqual(3, this.sendItems.Count);

            // The RequestTelemetry is the root operation here.
            // The user provided operationid will be ignore as it is not W3C compatible, and it will
            // be stored inside custom property.
            var requestTelemetry = (RequestTelemetry)this.sendItems.Single(t => t is RequestTelemetry);
            ValidateRootTelemetry(requestTelemetry);

            // Additional Validations.            
            Assert.AreNotEqual(NonW3CCompatileOperationId, requestTelemetry.Context.Operation.Id, "Non compatible operation id supplied by user should be ignored in W3C mode.");
            Assert.AreEqual(NonW3CCompatileOperationId, requestTelemetry.Properties[W3CConstants.LegacyRootIdProperty], "Non compatible operation id supplied by user should be stored in custom property");

            // The generated TraceTelemetry should become the child of the root RequestTelemetry
            var traceTelemetry = (TraceTelemetry)this.sendItems.Single(t => t is TraceTelemetry);
            ValidateChildTelemetry(requestTelemetry, traceTelemetry);

            // The generated EventTelemetry should become the child of the root RequestTelemetry
            var eventTelemetry = (TraceTelemetry)this.sendItems.Single(t => t is TraceTelemetry);
            ValidateChildTelemetry(requestTelemetry, eventTelemetry);
        }

        [TestMethod]
        public void StartOperationPopulatesContextCorrectlyWithOverridingW3CCompatibleRootIdW3C()
        {
            // Act - start an operation, supply operation ID which is NOT W3C compatible, and generate a telemetry inside it.
            using (this.telemetryClient.StartOperation<RequestTelemetry>("Request", operationId: W3CCompatileOperationId))
            {
                this.telemetryClient.TrackTrace("child trace");
                this.telemetryClient.TrackEvent("child event");
            }

            Assert.AreEqual(3, this.sendItems.Count);

            // The RequestTelemetry is the root operation here.
            // The user provided operationid will be used as it is W3C compatible.
            var requestTelemetry = (RequestTelemetry)this.sendItems.Single(t => t is RequestTelemetry);
            ValidateRootTelemetry(requestTelemetry, expectedOperationId:W3CCompatileOperationId);
            Assert.AreEqual(W3CCompatileOperationId, requestTelemetry.Context.Operation.Id, "W3C compatible operation id supplied by user should be used.");

            // The generated TraceTelemetry should become the child of the root RequestTelemetry
            var traceTelemetry = (TraceTelemetry)this.sendItems.Single(t => t is TraceTelemetry);
            ValidateChildTelemetry(requestTelemetry, traceTelemetry);


            // The generated EventTelemetry should become the child of the root RequestTelemetry
            var eventTelemetry = (TraceTelemetry)this.sendItems.Single(t => t is TraceTelemetry);
            ValidateChildTelemetry(requestTelemetry, eventTelemetry);
        }

        [TestMethod]
        [Description("For NonW3C, Validate that any root id supplied by user will be respected.")]
        public void StartOperationPopulatesContextCorrectlyWithAnyOverridingRootIdNonW3C()
        {
            this.telemetryConfiguration.EnableW3CCorrelation = false;

            // Act - start an operation, supply ANY operation ID, and generate a telemetry inside it.
            using (this.telemetryClient.StartOperation<RequestTelemetry>("Request", operationId: AnyRootId))
            {
                this.telemetryClient.TrackTrace("child trace");
                this.telemetryClient.TrackEvent("child event");
            }

            Assert.AreEqual(3, this.sendItems.Count);

            // The RequestTelemetry is the root operation here.
            // The user provided operationid will be used as is.
            var requestTelemetry = (RequestTelemetry)this.sendItems.Single(t => t is RequestTelemetry);
            ValidateRootTelemetry(requestTelemetry, expectedOperationId: AnyRootId, isW3C:false);

            // The generated TraceTelemetry should become the child of the root RequestTelemetry
            var traceTelemetry = (TraceTelemetry)this.sendItems.Single(t => t is TraceTelemetry);
            ValidateChildTelemetry(requestTelemetry, traceTelemetry);


            // The generated EventTelemetry should become the child of the root RequestTelemetry
            var eventTelemetry = (TraceTelemetry)this.sendItems.Single(t => t is TraceTelemetry);
            ValidateChildTelemetry(requestTelemetry, eventTelemetry);
        }

        [TestMethod]
        [Description("For W3C, Validate that any parentid id supplied by user will be respected.")]
        public void StartOperationPopulatesContextCorrectlyWithAnyOverridingParentIdW3C()
        {
            // Act - start an operation, supply ANY parent operation ID, and generate a telemetry inside it.
            using (this.telemetryClient.StartOperation<RequestTelemetry>("Request", operationId: W3CCompatileOperationId, parentOperationId: AnyParentId))
            {
                this.telemetryClient.TrackTrace("child trace");
                this.telemetryClient.TrackEvent("child event");
            }

            Assert.AreEqual(3, this.sendItems.Count);

            // The RequestTelemetry is the root operation here.
            // The user provided parent operationid will be used as is.
            var requestTelemetry = (RequestTelemetry)this.sendItems.Single(t => t is RequestTelemetry);
            ValidateRootTelemetry(requestTelemetry, expectedOperationParentId: AnyParentId);

            // The generated TraceTelemetry should become the child of the root RequestTelemetry
            var traceTelemetry = (TraceTelemetry)this.sendItems.Single(t => t is TraceTelemetry);
            ValidateChildTelemetry(requestTelemetry, traceTelemetry);


            // The generated EventTelemetry should become the child of the root RequestTelemetry
            var eventTelemetry = (TraceTelemetry)this.sendItems.Single(t => t is TraceTelemetry);
            ValidateChildTelemetry(requestTelemetry, eventTelemetry);
        }

        [TestMethod]
        [Description("For Non W3C, Validate that any parentid id supplied by user will be respected.")]
        public void StartOperationPopulatesContextCorrectlyWithAnyOverridingParentIdNonW3C()
        {
            this.telemetryConfiguration.EnableW3CCorrelation = false;

            // Act - start an operation, supply ANY parent operation ID, and generate a telemetry inside it.
            using (this.telemetryClient.StartOperation<RequestTelemetry>("Request", operationId: AnyRootId, parentOperationId: AnyParentId))
            {
                this.telemetryClient.TrackTrace("child trace");
                this.telemetryClient.TrackEvent("child event");
            }

            Assert.AreEqual(3, this.sendItems.Count);

            // The RequestTelemetry is the root operation here.
            // The user provided parent operationid will be used as is.
            var requestTelemetry = (RequestTelemetry)this.sendItems.Single(t => t is RequestTelemetry);
            ValidateRootTelemetry(requestTelemetry, expectedOperationParentId: AnyParentId, isW3C:false);

            // The generated TraceTelemetry should become the child of the root RequestTelemetry
            var traceTelemetry = (TraceTelemetry)this.sendItems.Single(t => t is TraceTelemetry);
            ValidateChildTelemetry(requestTelemetry, traceTelemetry);


            // The generated EventTelemetry should become the child of the root RequestTelemetry
            var eventTelemetry = (TraceTelemetry)this.sendItems.Single(t => t is TraceTelemetry);
            ValidateChildTelemetry(requestTelemetry, eventTelemetry);
        }
        //

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

        [TestMethod]
        public void StopOperationWhenTelemetryIdDoesNotMatchActivityId()
        {
            Assert.IsNull(Activity.Current);
            using (var operation = this.telemetryClient.StartOperation<DependencyTelemetry>(operationName: null))
            {
                operation.Telemetry.Id = "123";
            }

            Assert.AreEqual(0, this.sendItems.Count);
        }

        [TestMethod]
        public void StopOperationWhenTelemetryIdDoesNotMatchActivityIdButMatchesLegacyId()
        {
            Assert.IsNull(Activity.Current);
            using (var operation = this.telemetryClient.StartOperation<DependencyTelemetry>(operationName: null))
            {
                operation.Telemetry.Properties["ai_legacyRequestId"] = Activity.Current.Id;
                operation.Telemetry.Id = "123";
            }

            Assert.AreEqual(1, this.sendItems.Count);
        }

        private string GetOperationName(Activity activity)
        {
            return activity.Tags.FirstOrDefault(tag => tag.Key == "OperationName").Value;
        }

        private class ActivityTagsTelemetryInitializer : ITelemetryInitializer
        {
            public void Initialize(ITelemetry telemetry)
            {
                if (Activity.Current == null)
                {
                    return;
                }

                foreach (var tag in Activity.Current.Tags)
                {
                    if(telemetry is ISupportProperties telProp)
                    {
                        telProp.Properties[tag.Key] = tag.Value;
                    }                        
                }
            }
        }
    }
}
