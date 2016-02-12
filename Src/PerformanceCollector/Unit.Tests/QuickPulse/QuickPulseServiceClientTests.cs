namespace Unit.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;
    using Microsoft.ManagementServices.RealTimeDataProcessing.QuickPulseService;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public sealed class QuickPulseServiceClientTests : IDisposable
    {
        private const int Port = 49152 + 10;
        private readonly Uri serviceEndpoint = new Uri(string.Format(CultureInfo.InvariantCulture, "http://localhost:{0}", Port));

        private Action<HttpListenerResponse> pingResponse;
        private Action<HttpListenerResponse> submitResponse;

        private HttpListener listener;

        private int pingCount;
        private int submitCount;

        private List<MonitoringDataPoint> samples = new List<MonitoringDataPoint>();
        
        [TestInitialize]
        public void TestInitialize()
        {
            this.pingCount = 0;
            this.submitCount = 0;
            this.samples.Clear();

            this.pingResponse = response =>
            {
                response.AddHeader("x-ms-qps-subscribed", true.ToString());
            };

            this.submitResponse = response =>
            {
                response.AddHeader("x-ms-qps-subscribed", true.ToString());
            };

            this.listener = new HttpListener();
            this.listener.Prefixes.Add(string.Format(CultureInfo.InvariantCulture, "http://*:{0}/", Port));
            this.listener.Start();

            Task.Factory.StartNew(() => this.ProcessRequest(this.listener));
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.listener.Stop();
            this.listener.Close();
        }

        [TestMethod]
        public void QuickPulseServicePingsTheService()
        {
            // ARRANGE
            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint);

            // ACT
            serviceClient.Ping(string.Empty);
            serviceClient.Ping(string.Empty);
            serviceClient.Ping(string.Empty);

            // ASSERT
            this.listener.Stop();

            Assert.AreEqual(3, this.pingCount);
        }

        [TestMethod]
        public void QuickPulseServiceSubmitsSamplesToService()
        {
            // ARRANGE
            var now = DateTime.UtcNow;
            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint);
            var sample1 =
                new QuickPulseDataSample(
                    new QuickPulseDataAccumulator { AIRequestSuccessCount = 5, StartTimestamp = now, EndTimestamp = now.AddSeconds(1) },
                    new Dictionary<string, float>());

            var sample2 =
                new QuickPulseDataSample(
                    new QuickPulseDataAccumulator { AIDependencyCallSuccessCount = 10, StartTimestamp = now, EndTimestamp = now.AddSeconds(1) },
                    new Dictionary<string, float>());

            // ACT
            bool? sendMore = serviceClient.SubmitSamples(new[] { sample1, sample2 }, string.Empty);

            // ASSERT
            this.listener.Stop();

            Assert.AreEqual(true, sendMore);
            Assert.AreEqual(2, this.samples.Count);
            Assert.AreEqual(5, this.samples[0].Metrics.Single(m => m.Name == @"\ApplicationInsights\Requests Succeeded/Sec").Value);
            Assert.AreEqual(10, this.samples[1].Metrics.Single(m => m.Name == @"\ApplicationInsights\Dependency Calls Succeeded/Sec").Value);
        }

        public void Dispose()
        {
            ((IDisposable)this.listener).Dispose();
        }

        #region Helpers
        private void ProcessRequest(HttpListener listener)
        {
            while (listener.IsListening)
            {
                HttpListenerContext context = listener.GetContext();

                var request = context.Request;
                if (request.Url.LocalPath == "/ping")
                {
                    this.pingCount++;
                    
                    this.pingResponse(context.Response);
                }
                else if (request.Url.LocalPath == "/post")
                {
                    this.submitCount++;

                    this.submitResponse(context.Response);

                    var serializer = new DataContractJsonSerializer(typeof(MonitoringDataPoint[]));
                    var dataPoints = serializer.ReadObject(context.Request.InputStream) as MonitoringDataPoint[];

                    this.samples.AddRange(dataPoints);
                }

                context.Response.Close();
            }
        }

        #endregion
    }
}