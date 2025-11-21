using System.Diagnostics;

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
    using System.Threading;
    using System.Threading.Tasks;


    /// <summary>
    /// Tests corresponding to TelemetryClientExtension methods.
    /// </summary>
    [Collection("TelemetryClientTests")]
    public class TelemetryClientExtensionAsyncTests : IDisposable
    {
        private TelemetryClient telemetryClient;
        private List<ITelemetry> sendItems;
        private List<Activity> traceItems;
        private List<LogRecord> logItems;
        private object sendItemsLock;

        public TelemetryClientExtensionAsyncTests()
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

        public void Dispose()
        {
            this.telemetryClient?.TelemetryConfiguration?.Dispose();
        }

        /// <summary>
        /// Ensure that context being propagated via async/await.
        /// </summary>
        [Fact]
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

                Assert.NotEqual(id1, id2);
            }

            Assert.Equal(1, traceItems.Count);
            Assert.Equal(2, logItems.Count);

            var requestTelemetry = traceItems[0].ToRequestTelemetry();
            var requestId = requestTelemetry.Id;
            var requestOperationId = requestTelemetry.Context.Operation.Id;
            Assert.False(string.IsNullOrEmpty(requestTelemetry.Id));
            Assert.False(string.IsNullOrEmpty(requestOperationId));

            foreach (var item in this.logItems)
            {
                var tracetelemetry = item.ToTraceTelemetry();
                Assert.Equal(requestId, tracetelemetry.Context.Operation.ParentId);
                Assert.Equal(requestOperationId, tracetelemetry.Context.Operation.Id);
            }
        }

        /// <summary>
        /// Ensure that context being propagated via Begin/End.
        /// </summary>
        [Fact(Timeout = 2000)]
        public void ContextPropagatesThroughBeginEnd()
        {

            int id1 = Thread.CurrentThread.ManagedThreadId;
            int id2 = 0;

            // Start the request operation
            using (var op = this.telemetryClient.StartOperation<RequestTelemetry>("request"))
            {
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
                Assert.NotEqual(id1, id2);

                // Expect one request Activity and two log records
                Assert.Equal(1, traceItems.Count);
                Assert.Equal(2, logItems.Count);

                // Convert captured activity to RequestTelemetry for easier correlation check
                var requestTelemetry = traceItems[0].ToRequestTelemetry();
                var requestId = requestTelemetry.Id;
                var requestOperationId = requestTelemetry.Context.Operation.Id;

                Assert.False(string.IsNullOrEmpty(requestTelemetry.Id));
                Assert.False(string.IsNullOrEmpty(requestOperationId));

                // Verify log correlation
                foreach (var record in logItems)
                {
                    var traceTelemetry = record.ToTraceTelemetry();
                    Assert.Equal(requestId, traceTelemetry.Context.Operation.ParentId);
                    Assert.Equal(requestOperationId, traceTelemetry.Context.Operation.Id);
                }

                // Verify Activity correlation correctness
                Assert.Equal(activity.TraceId.ToHexString(), requestOperationId);
                Assert.Equal(activity.SpanId.ToHexString(), requestId);
            }
        }
    }
}
