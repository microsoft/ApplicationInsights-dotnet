namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Runtime.Remoting.Messaging;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Operation;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using TestFramework;

    /// <summary>
    /// Tests corresponding to TelemetryClientExtension methods.
    /// </summary>
    [TestClass]
    public class TelemetryClientExtensionAsyncTests
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
            configuration.TelemetryInitializers.Add(new CallContextBasedOperationCorrelationTelemetryInitializer());
            this.telemetryClient = new TelemetryClient(configuration);
            CallContext.FreeNamedDataSlot(CallContextHelpers.OperationContextSlotName);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            CallContext.FreeNamedDataSlot(CallContextHelpers.OperationContextSlotName); 
        }

        /// <summary>
        /// Ensure that context being propagated via async/await.
        /// </summary>
        [TestMethod]
        public void ContextPropogatesThruAsyncAwait()
        {
            var task = this.TestAsync();
            task.Wait();
        }

        /// <summary>
        /// Actual async test method.
        /// </summary>
        /// <returns>Task to await.</returns>
        public async Task TestAsync()
        {
            using (var op = this.telemetryClient.StartOperation<RequestTelemetry>("request"))
            {
                var id1 = Thread.CurrentThread.ManagedThreadId;
                this.telemetryClient.TrackTrace("trace1");

                HttpClient client = new HttpClient();
                await client.GetStringAsync("http://bing.com");

                var id2 = Thread.CurrentThread.ManagedThreadId;
                this.telemetryClient.TrackTrace("trace2");

                Assert.AreNotEqual(id1, id2);
            }

            Assert.AreEqual(3, this.sendItems.Count);
            var id = this.sendItems[this.sendItems.Count - 1].Context.Operation.Id;
            Assert.IsFalse(string.IsNullOrEmpty(id));

            foreach (var item in this.sendItems)
            {
                if (item is TraceTelemetry)
                {
                    Assert.AreEqual(id, item.Context.Operation.ParentId);
                }
            }
        }

        /// <summary>
        /// Ensure that context being propagated via Begin/End.
        /// </summary>
        [TestMethod]
        public void ContextPropogatesThruBeginEnd()
        {
            var op = this.telemetryClient.StartOperation<RequestTelemetry>("request");
            var id1 = Thread.CurrentThread.ManagedThreadId;
            int id2 = 0;
            this.telemetryClient.TrackTrace("trace1");

            HttpWebRequest request = WebRequest.Create(new Uri("http://bing.com")) as HttpWebRequest;
            var result = request.BeginGetResponse(
                (r) => 
                    {
                        id2 = Thread.CurrentThread.ManagedThreadId;
                        this.telemetryClient.TrackTrace("trace2");

                        this.telemetryClient.StopOperation(op);

                        (r.AsyncState as HttpWebRequest).EndGetResponse(r);
                    }, 
                null);

            while (!result.IsCompleted)
            {
                Thread.Sleep(10);
            }

            Thread.Sleep(100);

            Assert.AreNotEqual(id1, id2);

            Assert.AreEqual(3, this.sendItems.Count);
            var id = this.sendItems[this.sendItems.Count - 1].Context.Operation.Id;
            Assert.IsFalse(string.IsNullOrEmpty(id));

            foreach (var item in this.sendItems)
            {
                if (item is TraceTelemetry)
                {
                    Assert.AreEqual(id, item.Context.Operation.ParentId);
                }
            }
        }
    }
}
