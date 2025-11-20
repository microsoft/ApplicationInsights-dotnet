using System.Diagnostics;

namespace Microsoft.ApplicationInsights
{

    using Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Tests;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using OpenTelemetry;
    using OpenTelemetry.Logs;
    using OpenTelemetry.Trace;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;


    /// <summary>
    /// Tests corresponding to TelemetryClientExtension methods.
    /// </summary>
    [TestClass]
    public class TelemetryClientExtensionAsyncTests
    {
        private TelemetryClient telemetryClient;
        private List<ITelemetry> sendItems;
        private List<Activity> traceItems;
        private List<LogRecord> logItems;
        private object sendItemsLock;

        [TestInitialize]
        public void TestInitialize()
        {
            var configuration = new TelemetryConfiguration();
            this.sendItems = new List<ITelemetry>();
            this.traceItems = new List<Activity>();
            this.logItems = new List<LogRecord>();
            this.sendItemsLock = new object();
            var instrumentationKey = Guid.NewGuid().ToString();
            configuration.ConnectionString = "InstrumentationKey=" + instrumentationKey;
            configuration.ConfigureOpenTelemetryBuilder(b => b.WithTracing(t => t.AddInMemoryExporter(traceItems)).WithLogging(l => l.AddInMemoryExporter(logItems)));
            this.telemetryClient = new TelemetryClient(configuration);
        }

        /// <summary>
        /// Ensure that context being propagated via async/await.
        /// </summary>
        [TestMethod]
        public async Task ContextPropagatesThroughAsyncAwait()
        {
            await this.TestAsync();
        }

        /// <summary>
        /// Actual async test method.
        /// </summary>
        /// <returns>Task to await.</returns>
        public async Task TestAsync()
        {
            Activity activity;
            using (var op = this.telemetryClient.StartOperation<RequestTelemetry>("request"))
            {
                activity = Activity.Current;

                var id1 = Thread.CurrentThread.ManagedThreadId;
                this.telemetryClient.TrackTrace("trace1");

                //HttpClient client = new HttpClient();
                await Task.Delay(TimeSpan.FromMilliseconds(100));//client.GetStringAsync("http://bing.com");

                var id2 = Thread.CurrentThread.ManagedThreadId;
                this.telemetryClient.TrackTrace("trace2");

                Assert.AreNotEqual(id1, id2);
            }

            Assert.AreEqual(1, traceItems.Count);
            Assert.AreEqual(2, logItems.Count);

            var requestTelemetry = traceItems[0].ToRequestTelemetry();
            var requestId = requestTelemetry.Id;
            var requestOperationId = requestTelemetry.Context.Operation.Id;
            Assert.IsFalse(string.IsNullOrEmpty(requestTelemetry.Id));
            Assert.IsFalse(string.IsNullOrEmpty(requestOperationId));

            foreach (var item in this.logItems)
            {
                var tracetelemetry = item.ToTraceTelemetry();
                Assert.AreEqual(requestId, tracetelemetry.Context.Operation.ParentId);
                Assert.AreEqual(requestOperationId, tracetelemetry.Context.Operation.Id);
            }
        }

        /// <summary>
        /// Ensure that context being propagated via Begin/End.
        /// </summary>
        [TestMethod, Timeout(2000)]
        public void ContextPropagatesThroughBeginEnd()
        {

            int id1 = Thread.CurrentThread.ManagedThreadId;
            int id2 = 0;

            // Start the request operation
            var op = this.telemetryClient.StartOperation<RequestTelemetry>("request");
            var activity = Activity.Current;

            this.telemetryClient.TrackTrace("trace1");

            // Simulate async continuation (Begin/End pattern)
            var task = Task.Delay(50).ContinueWith(t =>
            {
                id2 = Thread.CurrentThread.ManagedThreadId;
                this.telemetryClient.TrackTrace("trace2");

                // Explicitly stop the operation (like End)
                this.telemetryClient.StopOperation(op);
            });

            // Wait for completion
            task.Wait();

            // Assert propagation
            Assert.AreNotEqual(id1, id2, "Thread IDs should differ to confirm async context flow.");

            // Expect one request Activity and two log records
            Assert.AreEqual(1, traceItems.Count, "Should capture one request Activity.");
            Assert.AreEqual(2, logItems.Count, "Should capture two logs within that Activity.");

            // Convert captured activity to RequestTelemetry for easier correlation check
            var requestTelemetry = traceItems[0].ToRequestTelemetry();
            var requestId = requestTelemetry.Id;
            var requestOperationId = requestTelemetry.Context.Operation.Id;

            Assert.IsFalse(string.IsNullOrEmpty(requestTelemetry.Id), "RequestTelemetry.Id must not be null.");
            Assert.IsFalse(string.IsNullOrEmpty(requestOperationId), "RequestTelemetry.Context.Operation.Id must not be null.");

            // Verify log correlation
            foreach (var record in logItems)
            {
                var traceTelemetry = record.ToTraceTelemetry();
                Assert.AreEqual(requestId, traceTelemetry.Context.Operation.ParentId, "Log ParentId should match request SpanId.");
                Assert.AreEqual(requestOperationId, traceTelemetry.Context.Operation.Id, "Log Operation.Id should match request TraceId.");
            }

            // Verify Activity correlation correctness
            Assert.AreEqual(activity.TraceId.ToHexString(), requestOperationId);
            Assert.AreEqual(activity.SpanId.ToHexString(), requestId);
        }
    }
}
