namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Extensibility.Implementation;
    using TestFramework;

    /// <summary>
    /// This class tests TelemetryClientEzxtensions.StartOperation<T>(TelemetryClient c, Activity a) overload
    /// </summary>
    [TestClass]
    public class StartOperationActivityTests
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
            configuration.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());
            this.telemetryClient = new TelemetryClient(configuration);
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
        public void BasicStartOperationWithActivity()
        {
            var activity = new Activity("name").SetParentId("parentId").AddBaggage("b1", "v1").AddTag("t1", "v1");

            RequestTelemetry telemetry;
            using (var operation = this.telemetryClient.StartOperation<RequestTelemetry>(activity))
            {
                telemetry = operation.Telemetry;
                Assert.AreEqual(activity, Activity.Current);
                Assert.IsNotNull(activity.Id);
            }

            this.ValidateTelemetry(telemetry, activity);

            Assert.AreEqual(telemetry, this.sendItems.Single());
        }


        /// <summary>
        /// Invalid Usage! Tests that if Activity is started, StartOperation still works and does not crash.
        /// </summary>
        [TestMethod]
        public void InvalidStartOperationWithStartedActivity()
        {
            var activity = new Activity("name").SetParentId("parentId").AddBaggage("b1", "v1").AddTag("t1", "v1").Start();

            DependencyTelemetry telemetry;
            using (var operation = this.telemetryClient.StartOperation<DependencyTelemetry>(activity))
            {
                telemetry = operation.Telemetry;
                Assert.AreEqual(activity, Activity.Current);
                Assert.IsNotNull(activity.Id);
            }

            this.ValidateTelemetry(telemetry, activity);

            Assert.AreEqual(telemetry, this.sendItems.Single());
        }

        [TestMethod]
        public void StartOperationSynchronousInScopeOfOtherUnrelatedActivity()
        {
            // There may be a case when operations are tracked in scope of other background operation, that is not related to the 
            // nested operation processing.
            // E.g. Background Activity is tracking high-level operation "get 5 messages from the queue and process them all"
            // In this case, each message processing has it's own correlation scope, passed in the message (i.e. Parent Activity is external)
            // The requirement is that backgorund Activity must survive each message processing.

            var backgroundActivity = new Activity("background").Start();

            //since ParentId is set on the activity, it won't be child of the parentActivity
            var activity = new Activity("name").SetParentId("parentId");

            // in order to keep parentActivity, StartOperation and StopOperation(or dispose)
            // must be called 
            RequestTelemetry telemetry = Task.Run(() =>
            {
                using (var operation = this.telemetryClient.StartOperation<RequestTelemetry>(activity))
                {
                    return operation.Telemetry;
                }
            }).Result;

            this.ValidateTelemetry(telemetry, activity);
            Assert.AreEqual(telemetry, this.sendItems.Single());

            // after processing is done and chile activity is finished, 
            // parentActivity should still be current
            Assert.AreEqual(backgroundActivity, Activity.Current);
        }

        [TestMethod]
        public async Task StartOperationAsyncInScopeOfOtherUnrelatedActivity()
        {
            var parentActivity = new Activity("background").Start();

            //since ParentId is set on the activity, it won't be child of the parentActivity
            var activity = new Activity("name").SetParentId("parentId");
            RequestTelemetry telemetry = await ProcessWithStartOperationAsync<RequestTelemetry>(activity, null);

            this.ValidateTelemetry(telemetry, activity);
            Assert.AreEqual(telemetry, this.sendItems.Single());

            // after processing is done and chile activity is finished, 
            // parentActivity should still be current
            Assert.AreEqual(parentActivity, Activity.Current);
        }

        /// <summary>
        /// Demonstrates scenario when operation is asyncronous - implemented in async method
        /// and StartOperation is called within this method.
        /// </summary>
        [TestMethod]
        public async Task ParallelStartOperationsAsyncProcessing()
        {
            Activity activity1 = new Activity("name1");
            Task<RequestTelemetry> request1 = ProcessWithStartOperationAsync<RequestTelemetry>(activity1, null);

            Activity activity2 = new Activity("name2");
            Task<RequestTelemetry> request2 = ProcessWithStartOperationAsync<RequestTelemetry>(activity2, null);

            await Task.WhenAll(request1, request2);

            this.ValidateTelemetry(request1.Result, activity1);
            this.ValidateTelemetry(request2.Result, activity2);

            Assert.AreEqual(2, this.sendItems.Count);
        }

        /// <summary>
        /// Demonstrates scenario when operation is asyncronous,
        /// but StartOperation is called outside of process method and in parallel with other StartOerations.
        /// </summary>
        /// <remarks>To ensure proper scoping, each processing still have to be wrapped into Task.Run, see <see cref="ParallelStartOperationsSyncronousProcessing"/></remarks>

        [TestMethod]
        public async Task ParallelStartOperationsInPlaceAsyncProcessing()
        {
            Activity activity1 = new Activity("name1");
            Task<RequestTelemetry> request1 = Task.Run(async () =>
            {
                using (var operation1 = this.telemetryClient.StartOperation<RequestTelemetry>(activity1))
                {
                    await this.ProcessAsync(activity1, null);
                    return operation1.Telemetry;
                }
            });


            Activity activity2 = new Activity("name2");
            Task<RequestTelemetry> request2 = Task.Run(async () =>
            {
                using (var operation2 = this.telemetryClient.StartOperation<RequestTelemetry>(activity2))
                {
                    await this.ProcessAsync(activity2, null);
                    return operation2.Telemetry;
                }
            });

            await Task.WhenAll(request1, request2);

            this.ValidateTelemetry(request1.Result, activity1);
            this.ValidateTelemetry(request2.Result, activity2);

            Assert.AreEqual(2, this.sendItems.Count);
        }

        /// <summary>
        /// Invalid Usage! Demonstrates scenario when operation is syncronous or StartOperation is called from non-async method or 
        /// method where multiple operations are started in parallel.
        /// </summary>
        /// <remarks>In this case, second operation may become child of first operation as shown below.
        /// to ensure proper scoping, each processing still have to be wrapped into Task.Run, see <see cref="ParallelStartOperationsSyncronousProcessing"/></remarks>
        [TestMethod]
        public void InvalidParallelStartOperationsSyncronousProcessing()
        {
            Activity activity1 = new Activity("name1");
            var operation1 = this.telemetryClient.StartOperation<RequestTelemetry>(activity1);
            Assert.AreEqual(activity1, Activity.Current);
            Assert.AreEqual(null, Activity.Current.Parent);

            Activity activity2 = new Activity("name2");
            var operation2 = this.telemetryClient.StartOperation<RequestTelemetry>(activity2);
            Assert.AreEqual(activity2, Activity.Current);
            Assert.AreEqual(activity1, Activity.Current.Parent);

            this.ValidateTelemetry(operation1.Telemetry, activity1);
            this.ValidateTelemetry(operation2.Telemetry, activity2);
        }

        /// <summary>
        /// Invalid! Demonstrates scenario when operation is syncronous or StartOperation is called from non-async method or 
        /// method where multiple operations are started in parallel.
        /// </summary>
        /// <remarks>To ensure proper scoping, each processing still have to be wrapped into Task.Run</remarks>
        [TestMethod]
        public void ParallelStartOperationsSyncronousProcessing()
        {
            Activity activity1 = new Activity("name1");
            Task<RequestTelemetry> request1 = Task.Run(() => ProcessWithStartOperation<RequestTelemetry>(activity1, null));

            Activity activity2 = new Activity("name2");
            Task<RequestTelemetry> request2 = Task.Run(() => ProcessWithStartOperation<RequestTelemetry>(activity2, null));

            Task.WaitAll(request1, request2);

            this.ValidateTelemetry(request1.Result, activity1);
            this.ValidateTelemetry(request2.Result, activity2);

            Assert.AreEqual(2, this.sendItems.Count);
        }

        private async Task<T> ProcessWithStartOperationAsync<T>(Activity activity, Activity parentActivity) where T : OperationTelemetry, new()
        {
            T telemetry;

            using (var operation = this.telemetryClient.StartOperation<T>(activity))
            {
                await Task.Delay(20);

                telemetry = operation.Telemetry;
                Assert.AreEqual(activity, Activity.Current);
                Assert.AreEqual(parentActivity, Activity.Current.Parent);
            }
            return telemetry;
        }

        private async Task ProcessAsync(Activity activity, Activity parentActivity)
        {
            await Task.Delay(20);
            Assert.AreEqual(activity, Activity.Current);
            Assert.AreEqual(parentActivity, Activity.Current.Parent);
        }

        private T ProcessWithStartOperation<T>(Activity activity, Activity parentActivity) where T : OperationTelemetry, new()
        {
            T telemetry;

            using (var operation = this.telemetryClient.StartOperation<T>(activity))
            {
                Task.Delay(20).Wait();
                telemetry = operation.Telemetry;
                Assert.AreEqual(activity, Activity.Current);
                Assert.AreEqual(parentActivity, Activity.Current.Parent);
            }
            return telemetry;
        }

        private void ValidateTelemetry<T>(T telemetry, Activity activity) where T : OperationTelemetry
        {
            Assert.AreEqual(activity.OperationName, telemetry.Name);
            Assert.AreEqual(activity.Id, telemetry.Id);
            Assert.AreEqual(activity.ParentId, telemetry.Context.Operation.ParentId);
            Assert.AreEqual(activity.RootId, telemetry.Context.Operation.Id);

            foreach (var baggage in activity.Baggage)
            {
                Assert.IsTrue(telemetry.Properties.ContainsKey(baggage.Key));
                Assert.AreEqual(baggage.Value, telemetry.Properties[baggage.Key]);
            }

            foreach (var tag in activity.Tags)
            {
                Assert.IsTrue(telemetry.Properties.ContainsKey(tag.Key));
                Assert.AreEqual(tag.Value, telemetry.Properties[tag.Key]);
            }
        }
    }
}
