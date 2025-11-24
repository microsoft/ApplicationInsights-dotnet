namespace Microsoft.ApplicationInsights
{
    using Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Tests;
    using Xunit;
    using OpenTelemetry;
    using OpenTelemetry.Logs;
    using OpenTelemetry.Trace;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    [Collection("TelemetryClientTests")]
    public class TelemetryClientExtensionTests : IDisposable
    {
        const string W3CCompatibleOperationId = "8ee8641cbdd8dd280d239fa2121c7e4e";
        const string AnyRootId = "ANYID";
        const string AnyParentId = "ANYParentID";

        private TelemetryClient telemetryClient;
        private TelemetryConfiguration telemetryConfiguration;
        private List<ITelemetry> sendItems;
        private List<Activity> traceItems;
        private List<LogRecord> logItems;

        public TelemetryClientExtensionTests()
        {
            this.telemetryConfiguration = new TelemetryConfiguration();
            this.sendItems = new List<ITelemetry>();
            this.traceItems = new List<Activity>();
            this.logItems = new List<LogRecord>();
            var instrumentationKey = Guid.NewGuid().ToString();
            telemetryConfiguration.ConnectionString = "InstrumentationKey=" + instrumentationKey;
            telemetryConfiguration.ConfigureOpenTelemetryBuilder(b => b.WithTracing(t => t.AddInMemoryExporter(traceItems)).WithLogging(l => l.AddInMemoryExporter(logItems)));
            this.telemetryClient = new TelemetryClient(telemetryConfiguration);
        }

        public void Dispose()
        {
            while (Activity.Current != null)
            {
                Activity.Current.Stop();
            }
            this.telemetryClient?.TelemetryConfiguration?.Dispose();
        }

        [Fact]
        public void StartDependencyTrackingReturnsOperationWithSameTelemetryItem()
        {
            using (var operation = this.telemetryClient.StartOperation<DependencyTelemetry>(operationName: null))
            {
                Assert.NotNull(operation);
                Assert.NotNull(operation.Telemetry);
            }
        }

        [Fact]
        public void StartDependencyTrackingReturnsOperationWithInitializedOperationName()
        {
            using (var operation = this.telemetryClient.StartOperation<DependencyTelemetry>("TestOperationName"))
            {
                Assert.Equal("TestOperationName", operation.Telemetry.Name);
            }
        }

        [Fact]
        public void StartDependencyTrackingReturnsOperationWithInitializedOperationTelemetryId()
        {
            using (var operation = this.telemetryClient.StartOperation<DependencyTelemetry>("TestOperationName"))
            {
                Assert.NotNull(operation.Telemetry.Id);
            }
        }

        [Fact]
        public void StartDependencyTrackingReturnsOperationWithInitializedOperationId()
        {
            using (var operation = this.telemetryClient.StartOperation<DependencyTelemetry>("TestOperationName"))
            {
                Assert.NotNull(operation.Telemetry.Context.Operation.Id);
            }
        }

        [Fact]
        public void StartDependencyTrackingThrowsExceptionWithNullTelemetryClient()
        {
            TelemetryClient tc = null;
            Assert.Throws<ArgumentNullException>(() => tc.StartOperation<DependencyTelemetry>(operationName: null));
        }

        [Fact]
        public void StartDependencyTrackingCreatesADependencyTelemetryItemWithTimeStamp()
        {
            using (var operation = this.telemetryClient.StartOperation<DependencyTelemetry>(operationName: null))
            {
                Assert.NotEqual(operation.Telemetry.Timestamp, DateTimeOffset.MinValue);
            }
        }

        [Fact]
        public void StartDependencyTrackingAddsOperationContextStoreToCurrentActivity()
        {
            Assert.Null(Activity.Current);
            using (var operation = this.telemetryClient.StartOperation<DependencyTelemetry>(operationName: null))
            {
                Assert.NotNull(Activity.Current);
                Assert.Equal(ActivityIdFormat.W3C, Activity.Current.IdFormat);
            }
        }

        [Fact]
        public void UsingSendsTelemetryAndDisposesOperationItem()
        {
            Assert.Null(Activity.Current);
            using (var operation = this.telemetryClient.StartOperation<DependencyTelemetry>(operationName: null))
            {
            }

            Assert.Null(Activity.Current);
            Assert.Equal(1, this.traceItems.Count);
        }

        [Fact]
        public void UsingWithStopOperationSendsTelemetryAndDisposesOperationItemOnlyOnce()
        {
            Assert.Null(Activity.Current);
            using (var operation = this.telemetryClient.StartOperation<DependencyTelemetry>(operationName: null))
            {
                this.telemetryClient.StopOperation(operation);
            }

            Assert.Null(Activity.Current);
            Assert.Equal(1, this.traceItems.Count);
        }

        [Fact]
        public void StartDependencyTrackingHandlesMultipleContextStoresInCurrentActivityW3C()
        {
            using (var operation = this.telemetryClient.StartOperation<DependencyTelemetry>("OperationName"))
            {
                var currentActivity = Activity.Current;
                Assert.Equal(operation.Telemetry.Id, currentActivity.SpanId.ToHexString());
                Assert.Equal(operation.Telemetry.Context.Operation.Name, this.GetOperationName(currentActivity));

                using (var childOperation = this.telemetryClient.StartOperation<DependencyTelemetry>("OperationName"))
                {
                    var childActivity = Activity.Current;
                    Assert.Equal(childOperation.Telemetry.Id, childActivity.SpanId.ToHexString());
                    Assert.Equal(childOperation.Telemetry.Context.Operation.Name, this.GetOperationName(currentActivity));

                    Assert.Null(currentActivity.Parent);
                    Assert.Equal(currentActivity, childActivity.Parent);

                    this.telemetryClient.StopOperation(childOperation);
                    Assert.Equal(currentActivity, Activity.Current);
                }
                this.telemetryClient.StopOperation(operation);
            }
            Assert.Null(Activity.Current);
        }

        [Fact]
        public void StopOperationDoesNotFailOnNullOperation()
        {
            this.telemetryClient.StopOperation<DependencyTelemetry>(null);
        }

        [Fact]
        
        public void StopDependencyTrackingThrowsExceptionWithNullTelemetryClient()
        {
            var operationItem = new OperationHolder<DependencyTelemetry>(this.telemetryClient, new DependencyTelemetry(), null);
            TelemetryClient tc = null;
            Assert.Throws<ArgumentNullException>(() => tc.StopOperation(operationItem));
        }

        [Fact]
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

            Assert.Equal(2, this.traceItems.Count);
        }

        [Fact]
        public void StartDependencyTrackingStoresTheArgumentOperationNameInCurrentActivity()
        {
            using (var operation = this.telemetryClient.StartOperation<DependencyTelemetry>("TestOperationName"))
            {
                Assert.Equal("TestOperationName", this.GetOperationName(Activity.Current));
            }
        }

        [Fact]
        public void DisposeOperationAppliesChangesOnActivityDoneAfterStart()
        {
            using (var operation = this.telemetryClient.StartOperation<DependencyTelemetry>("TestOperationName"))
            {
                Activity.Current.SetTag("my custom tag", "value");
            }

            Assert.Equal(1, this.traceItems.Count);
            var telemetry = this.traceItems[0].ToDependencyTelemetry();
            Assert.True(telemetry.Properties.ContainsKey("my custom tag"));
            Assert.Equal("value", telemetry.Properties["my custom tag"]);
        }

        [Fact]
        public void ContextPropagatesThroughNestedOperationsW3C()
        {
            using (this.telemetryClient.StartOperation<RequestTelemetry>("OuterRequest"))
            {
                using (this.telemetryClient.StartOperation<DependencyTelemetry>("DependentCall"))
                {
                }
            }

            Assert.Equal(2, this.traceItems.Count);

            var requestTelemetry = this.traceItems[1].ToRequestTelemetry();
            var dependentTelemetry = this.traceItems[0].ToDependencyTelemetry();
            Assert.Null(requestTelemetry.Context.Operation.ParentId);
            Assert.Equal(requestTelemetry.Id, dependentTelemetry.Context.Operation.ParentId);
            Assert.Equal(requestTelemetry.Context.Operation.Id, dependentTelemetry.Context.Operation.Id);
            Assert.Equal(requestTelemetry.Context.Operation.Name, dependentTelemetry.Context.Operation.Name);
        }

        [Fact]
        public void StartStopRespectsUserProvidedIdsInScopeOfAnotherActivityExplicitIds()
        {
            var activity = new Activity("foo").Start();

            var customOperationId = ActivityTraceId.CreateRandom().ToHexString();
            var customParentId = ActivitySpanId.CreateRandom().ToHexString();

            using (var operation = this.telemetryClient.StartOperation<DependencyTelemetry>("name", customOperationId, customParentId))
            {
                Assert.NotNull(Activity.Current);
                Assert.NotEqual(activity, Activity.Current.Parent);
                Assert.Equal(customOperationId, Activity.Current.TraceId.ToHexString());
                Assert.Equal(customOperationId, operation.Telemetry.Context.Operation.Id);
                Assert.Equal(customParentId, operation.Telemetry.Context.Operation.ParentId);
            }

            Assert.Equal(activity, Activity.Current);
            Assert.Equal(1, this.traceItems.Count);
            var dependency = this.traceItems[0].ToDependencyTelemetry();

            Assert.Equal(customOperationId, dependency.Context.Operation.Id);
            Assert.Equal(customParentId, dependency.Context.Operation.ParentId);
        }

        [Fact]
        public void StartStopRespectsUserProvidedIdsInScopeOfAnotherActivityExplicitOperationIdOnly()
        {
            var activity = this.telemetryClient.TelemetryConfiguration.ApplicationInsightsActivitySource.StartActivity("foo");

            var customOperationId = ActivityTraceId.CreateRandom().ToHexString();

            using (var operation = this.telemetryClient.StartOperation<DependencyTelemetry>("name", customOperationId))
            {
                Assert.NotNull(Activity.Current);
                Assert.NotEqual(activity, Activity.Current.Parent);
                Assert.Equal(customOperationId, Activity.Current.TraceId.ToHexString());
                Assert.Equal(customOperationId, operation.Telemetry.Context.Operation.Id);
                Assert.Null(operation.Telemetry.Context.Operation.ParentId);
            }

            Assert.Equal(activity, Activity.Current);
            Assert.Equal(1, this.traceItems.Count);
            var dependency = this.traceItems[0].ToDependencyTelemetry();

            activity.Dispose();

            Assert.Equal(customOperationId, dependency.Context.Operation.Id);
            Assert.Null(dependency.Context.Operation.ParentId);
        }

        [Fact]
        public void StartStopRespectsUserProvidedIdsInScopeOfAnotherActivityExplicitIdsW3COff()
        {
            var activity = new Activity("foo").Start();

            var customOperationId = ActivityTraceId.CreateRandom().ToHexString();
            var customParentId = ActivitySpanId.CreateRandom().ToHexString();

            using (var operation = this.telemetryClient.StartOperation<DependencyTelemetry>("name", customOperationId, customParentId))
            {
                Assert.NotNull(Activity.Current);
                Assert.NotEqual(activity, Activity.Current.Parent);
                Assert.Equal(customOperationId, Activity.Current.RootId);
                Assert.Equal(customOperationId, operation.Telemetry.Context.Operation.Id);
                Assert.Equal(customParentId, operation.Telemetry.Context.Operation.ParentId);
            }

            Assert.Equal(activity, Activity.Current);
            Assert.Equal(1, this.traceItems.Count);
            var dependency = this.traceItems[0].ToDependencyTelemetry();

            Assert.Equal(customOperationId, dependency.Context.Operation.Id);
            Assert.Equal(customParentId, dependency.Context.Operation.ParentId);
        }

        [Fact]
        public void StartStopRespectsUserProvidedIdsInScopeOfAnotherActivityTelemetry()
        {
            var activity = new Activity("foo").Start();

            var customOperationId = ActivityTraceId.CreateRandom().ToHexString();
            var customParentId = ActivitySpanId.CreateRandom().ToHexString();
            var dependency = new DependencyTelemetry();
            dependency.Context.Operation.Id = customOperationId;
            dependency.Context.Operation.ParentId = customParentId;

            using (var operation = this.telemetryClient.StartOperation<DependencyTelemetry>(dependency))
            {
                Assert.NotNull(Activity.Current);
                Assert.NotEqual(activity, Activity.Current.Parent);
                Assert.Equal(customOperationId, Activity.Current.TraceId.ToHexString());
                Assert.Equal(customOperationId, operation.Telemetry.Context.Operation.Id);
                Assert.Equal(customParentId, operation.Telemetry.Context.Operation.ParentId);
            }

            Assert.Equal(activity, Activity.Current);
            Assert.Equal(1, this.traceItems.Count);

            Assert.Equal(customOperationId, dependency.Context.Operation.Id);
            Assert.Equal(customParentId, dependency.Context.Operation.ParentId);
        }

        [Fact]
        public void StartOperationOperationIdIsIgnoredIfNotW3cCompatible()
        {
            using (this.telemetryClient.StartOperation<RequestTelemetry>("Request"))
            {
            }

            Assert.Equal(1, this.traceItems.Count);

            var requestTelemetry = this.traceItems[0].ToRequestTelemetry();
            Assert.Null(requestTelemetry.Context.Operation.ParentId);
            // Assert.Equal("HOME", requestTelemetry.Properties[W3CConstants.LegacyRootIdProperty]);
        }

        [Fact]
        public void StartOperationOperationIdIsUsedIfW3cCompatible()
        {
            using (this.telemetryClient.StartOperation<RequestTelemetry>("Request", operationId: W3CCompatibleOperationId))
            {
            }

            Assert.Equal(1, this.traceItems.Count);

            var requestTelemetry = this.traceItems[0].ToRequestTelemetry();
            Assert.Null(requestTelemetry.Context.Operation.ParentId);
            Assert.Equal("8ee8641cbdd8dd280d239fa2121c7e4e", requestTelemetry.Context.Operation.Id);
        }

        [Fact]
        public void StartOperationCannotOverrideRootAndParentOperationIdNotW3CCompatible()
        {
            using (this.telemetryClient.StartOperation<RequestTelemetry>("Request", operationId: "ROOT", parentOperationId: "PARENT"))
            {
                this.telemetryClient.TrackTrace("child trace");
            }

            Assert.Equal(1, this.traceItems.Count);
            Assert.Equal(1, this.logItems.Count);

            var requestTelemetry = this.traceItems[0].ToRequestTelemetry();
            Assert.Null(requestTelemetry.Context.Operation.ParentId);
            Assert.NotEqual("ROOT", requestTelemetry.Context.Operation.Id);

            var traceTelemetry = this.logItems[0].ToTraceTelemetry();
            Assert.Equal(requestTelemetry.Id, traceTelemetry.Context.Operation.ParentId);
            Assert.NotEqual("ROOT", traceTelemetry.Context.Operation.Id);
        }

        [Fact]
        public void StartOperationPopulatesContextCorrectlyW3C()
        {
            string spanId;
            string traceId;
            // Act - start an operation, and generate telemetry inside it.
            using (this.telemetryClient.StartOperation<RequestTelemetry>("Request"))
            {
                traceId = Activity.Current.TraceId.ToHexString();
                spanId = Activity.Current.SpanId.ToHexString();
                this.telemetryClient.TrackTrace("child trace");
                this.telemetryClient.TrackEvent("child event");
            }

            Assert.Equal(1, this.traceItems.Count);
            Assert.Equal(2, this.logItems.Count);

            // The RequestTelemetry is the root operation here.
            var requestTelemetry = this.traceItems[0].ToRequestTelemetry();
            ValidateRootTelemetry(requestTelemetry, traceId, spanId, null, true);

            // The generated TraceTelemetry should become the child of the root RequestTelemetry
            var traceTelemetry = this.logItems[0].ToTraceTelemetry();
            ValidateChildTelemetry(requestTelemetry, traceTelemetry);

            // The generated EventTelemetry should become the child of the root RequestTelemetry
            var eventTelemetry = this.logItems[1].ToTraceTelemetry();
            ValidateChildTelemetry(requestTelemetry, eventTelemetry);
        }

        [Fact]
        public void StartOperationPopulatesContextCorrectlyWithOverridingW3CCompatibleRootIdW3C()
        {
            string spanId;

            // Act - start an operation, supply operation ID which is NOT W3C compatible, and generate a telemetry inside it.
            using (this.telemetryClient.StartOperation<RequestTelemetry>("Request", operationId: W3CCompatibleOperationId))
            {
                spanId = Activity.Current.SpanId.ToHexString();
                this.telemetryClient.TrackTrace("child trace");
                this.telemetryClient.TrackEvent("child event");
            }

            Assert.Equal(1, this.traceItems.Count);
            Assert.Equal(2, this.logItems.Count);

            // The RequestTelemetry is the root operation here.
            // The user provided operationid will be used as it is W3C compatible.
            var requestTelemetry = this.traceItems[0].ToRequestTelemetry();
            ValidateRootTelemetry(requestTelemetry, W3CCompatibleOperationId, spanId, null, true);

            // The generated TraceTelemetry should become the child of the root RequestTelemetry
            var traceTelemetry = this.logItems[0].ToTraceTelemetry();
            ValidateChildTelemetry(requestTelemetry, traceTelemetry);


            // The generated EventTelemetry should become the child of the root RequestTelemetry
            var eventTelemetry = this.logItems[1].ToTraceTelemetry();
            ValidateChildTelemetry(requestTelemetry, eventTelemetry);
        }

        [Fact]
        public void StartOperationPopulatesContextCorrectlyWithOverridingParentIdW3C()
        {
            string spanId;
            // Act - start an operation, supply ANY parent operation ID, and generate a telemetry inside it.
            using (this.telemetryClient.StartOperation<RequestTelemetry>("Request", operationId: W3CCompatibleOperationId, parentOperationId: AnyParentId))
            {
                spanId = Activity.Current.SpanId.ToHexString();
                this.telemetryClient.TrackTrace("child trace");
                this.telemetryClient.TrackEvent("child event");
            }

            Assert.Equal(1, this.traceItems.Count);
            Assert.Equal(2, this.logItems.Count);

            // The RequestTelemetry is the root operation here.
            // The user provided parent operationid will be used as is.
            var requestTelemetry = this.traceItems[0].ToRequestTelemetry();
            ValidateRootTelemetry(requestTelemetry, W3CCompatibleOperationId, spanId, null, true);

            // The generated TraceTelemetry should become the child of the root RequestTelemetry
            var traceTelemetry = this.logItems[0].ToTraceTelemetry();
            ValidateChildTelemetry(requestTelemetry, traceTelemetry);


            // The generated EventTelemetry should become the child of the root RequestTelemetry
            var eventTelemetry = this.logItems[1].ToTraceTelemetry();
            ValidateChildTelemetry(requestTelemetry, eventTelemetry);
        }

        [Fact]
        public void StartOperationThrowsOnNullOperationTelemetry()
        {
            Assert.Throws<ArgumentNullException>(() => this.telemetryClient.StartOperation<RequestTelemetry>(operationTelemetry: null));
        }

        [Fact]
        public void StartOperationWithOperationTelemetrySetsOperationHolderTelemetry()
        {
            var requestTelemetry = new RequestTelemetry
            {
                Name = "REQUEST",
                Id = "1"
            };

            using (var operationHolder = this.telemetryClient.StartOperation<RequestTelemetry>(requestTelemetry))
            {
                Assert.Equal(requestTelemetry, operationHolder.Telemetry);
            }

            Assert.Equal(1, this.traceItems.Count);
        }

        [Fact]
        public void StopOperationWhenTelemetryIdDoesNotMatchActivityId()
        {
            Assert.Null(Activity.Current);
            using (var operation = this.telemetryClient.StartOperation<DependencyTelemetry>(operationName: null))
            {
                operation.Telemetry.Id = "123";
            }

            // In OTel model, the Activity is still tracked even if telemetry.Id is manually changed
            // because the Activity itself drives the tracking, not the telemetry object
            Assert.Equal(1, this.traceItems.Count);
        }

        [Fact]
        public void StopOperationWhenTelemetryIdDoesNotMatchActivityIdButMatchesLegacyId()
        {
            Assert.Null(Activity.Current);
            using (var operation = this.telemetryClient.StartOperation<DependencyTelemetry>(operationName: null))
            {
                operation.Telemetry.Properties["ai_legacyRequestId"] = Activity.Current.Id;
                operation.Telemetry.Id = "123";
            }

            Assert.Equal(1, this.traceItems.Count);
        }

        private void ValidateRootTelemetry(OperationTelemetry operationTelemetry, string expectedOperationId, string expectedId, string expectedOperationParentId, bool isW3C)
        {
            Assert.Equal(expectedOperationParentId, operationTelemetry.Context.Operation.ParentId);
            Assert.NotNull(operationTelemetry.Context.Operation.Id);

            Assert.Equal(expectedOperationId, operationTelemetry.Context.Operation.Id);

            if (isW3C)
            {
                // Assert.True(W3CUtilities.IsCompatibleW3CTraceId(operationTelemetry.Context.Operation.Id));
            }

            Assert.NotNull(operationTelemetry.Id);
            Assert.Equal(expectedId, operationTelemetry.Id);
        }

        private void ValidateChildTelemetry(OperationTelemetry rootOperationTelemetry, ITelemetry childTelemetry)
        {
            Assert.Equal(rootOperationTelemetry.Id, childTelemetry.Context.Operation.ParentId);
            Assert.Equal(rootOperationTelemetry.Context.Operation.Id, childTelemetry.Context.Operation.Id);
        }
        private string GetOperationName(Activity activity)
        {
            return activity.Tags.FirstOrDefault(tag => tag.Key == "OperationName").Value;
        }
    }
}

