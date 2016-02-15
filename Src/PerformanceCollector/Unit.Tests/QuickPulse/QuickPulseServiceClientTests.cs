namespace Unit.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Runtime.Serialization.Json;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;
    using Microsoft.ManagementServices.RealTimeDataProcessing.QuickPulseService;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public sealed class QuickPulseServiceClientTests : IDisposable
    {
        private const int Port = 49152 + 10;
        private readonly Uri serviceEndpoint = new Uri(string.Format(CultureInfo.InvariantCulture, "http://localhost:{0}", Port));

        private readonly List<MonitoringDataPoint> samples = new List<MonitoringDataPoint>();

        private Action<HttpListenerResponse> pingResponse;
        private Action<HttpListenerResponse> submitResponse;

        private HttpListener listener;

        private int pingCount;
        private int submitCount;

        private DateTime? lastPingTimestamp;

        private string lastPingInstance;

        private string lastVersion;

        private bool emulateTimeout;
        
        [TestInitialize]
        public void TestInitialize()
        {
            this.pingCount = 0;
            this.submitCount = 0;
            this.lastPingTimestamp = null;
            this.lastPingInstance = string.Empty;
            this.lastVersion = string.Empty;
            this.samples.Clear();
            this.emulateTimeout = false;

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
        public void QuickPulseServiceClientPingsTheService()
        {
            // ARRANGE
            string instance = Guid.NewGuid().ToString();
            var timestamp = DateTime.UtcNow;

            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, instance, string.Empty);

            // ACT
            serviceClient.Ping(string.Empty, timestamp);
            serviceClient.Ping(string.Empty, timestamp);
            serviceClient.Ping(string.Empty, timestamp);

            // ASSERT
            this.listener.Stop();

            Assert.AreEqual(3, this.pingCount);
            Assert.AreEqual(timestamp.ToLongTimeString(), this.lastPingTimestamp.Value.ToLongTimeString());
            Assert.AreEqual(instance, this.lastPingInstance);
        }

        [TestMethod]
        public void QuickPulseServiceClientSubmitsSamplesToService()
        {
            // ARRANGE
            var now = DateTime.UtcNow;
            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, string.Empty, string.Empty);
            var sample1 =
                new QuickPulseDataSample(
                    new QuickPulseDataAccumulator { AIRequestSuccessCount = 5, StartTimestamp = now, EndTimestamp = now.AddSeconds(1) },
                    new Dictionary<string, Tuple<PerformanceCounterData, float>>());

            var sample2 =
                new QuickPulseDataSample(
                    new QuickPulseDataAccumulator { AIDependencyCallSuccessCount = 10, StartTimestamp = now, EndTimestamp = now.AddSeconds(1) },
                    new Dictionary<string, Tuple<PerformanceCounterData, float>>());

            // ACT
            bool? sendMore = serviceClient.SubmitSamples(new[] { sample1, sample2 }, string.Empty);

            // ASSERT
            this.listener.Stop();

            Assert.AreEqual(true, sendMore);
            Assert.AreEqual(2, this.samples.Count);
            Assert.AreEqual(5, this.samples[0].Metrics.Single(m => m.Name == @"\ApplicationInsights\Requests Succeeded/Sec").Value);
            Assert.AreEqual(10, this.samples[1].Metrics.Single(m => m.Name == @"\ApplicationInsights\Dependency Calls Succeeded/Sec").Value);
        }

        [TestMethod]
        public void QuickPulseServiceClientInterpretsPingResponseCorrectlyWhenHeaderTrue()
        {
            // ARRANGE
            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, string.Empty, string.Empty);

            // ACT
            this.pingResponse = r => { r.AddHeader("x-ms-qps-subscribed", true.ToString()); };
            bool? response = serviceClient.Ping(string.Empty, DateTime.UtcNow);

            // ASSERT
            this.listener.Stop();

            Assert.IsTrue(response.Value);
        }

        [TestMethod]
        public void QuickPulseServiceClientInterpretsPingResponseCorrectlyWhenHeaderFalse()
        {
            // ARRANGE
            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, string.Empty, string.Empty);

            // ACT
            this.pingResponse = r => { r.AddHeader("x-ms-qps-subscribed", false.ToString()); };
            bool? response = serviceClient.Ping(string.Empty, DateTime.UtcNow);

            // ASSERT
            this.listener.Stop();

            Assert.IsFalse(response.Value);
        }

        [TestMethod]
        public void QuickPulseServiceClientInterpretsPingResponseCorrectlyWhenHeaderInvalid()
        {
            // ARRANGE
            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, string.Empty, string.Empty);

            // ACT
            this.pingResponse = r => { r.AddHeader("x-ms-qps-subscribed", "bla"); };
            bool? response = serviceClient.Ping(string.Empty, DateTime.UtcNow);

            // ASSERT
            this.listener.Stop();

            Assert.IsNull(response);
        }

        [TestMethod]
        public void QuickPulseServiceClientInterpretsPingResponseCorrectlyWhenHeaderMissing()
        {
            // ARRANGE
            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, string.Empty, string.Empty);

            // ACT
            this.pingResponse = r => { };
            bool? response = serviceClient.Ping(string.Empty, DateTime.UtcNow);

            // ASSERT
            this.listener.Stop();

            Assert.IsNull(response);
        }

        [TestMethod]
        public void QuickPulseServiceClientInterpretsSubmitSamplesResponseCorrectlyWhenHeaderTrue()
        {
            // ARRANGE
            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, string.Empty, string.Empty);

            // ACT
            this.submitResponse = r => { r.AddHeader("x-ms-qps-subscribed", true.ToString()); };
            bool? response = serviceClient.SubmitSamples(new QuickPulseDataSample[] { }, string.Empty);

            // ASSERT
            this.listener.Stop();

            Assert.IsTrue(response.Value);
        }

        [TestMethod]
        public void QuickPulseServiceClientInterpretsSubmitSamplesResponseCorrectlyWhenHeaderFalse()
        {
            // ARRANGE
            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, string.Empty, string.Empty);

            // ACT
            this.submitResponse = r => { r.AddHeader("x-ms-qps-subscribed", false.ToString()); };
            bool? response = serviceClient.SubmitSamples(new QuickPulseDataSample[] { }, string.Empty);

            // ASSERT
            this.listener.Stop();

            Assert.IsFalse(response.Value);
        }

        [TestMethod]
        public void QuickPulseServiceClientInterpretsSubmitSamplesResponseCorrectlyWhenHeaderInvalid()
        {
            // ARRANGE
            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, string.Empty, string.Empty);

            // ACT
            this.submitResponse = r => { r.AddHeader("x-ms-qps-subscribed", "bla"); };
            bool? response = serviceClient.SubmitSamples(new QuickPulseDataSample[] { }, string.Empty);

            // ASSERT
            this.listener.Stop();

            Assert.IsNull(response);
        }

        [TestMethod]
        public void QuickPulseServiceClientInterpretsSubmitSamplesResponseCorrectlyWhenHeaderMissing()
        {
            // ARRANGE
            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, string.Empty, string.Empty);

            // ACT
            this.submitResponse = r => { };
            bool? response = serviceClient.SubmitSamples(new QuickPulseDataSample[] { }, string.Empty);

            // ASSERT
            this.listener.Stop();

            Assert.IsNull(response);
        }

        [TestMethod]
        public void QuickPulseServiceClientRetriesPing()
        {
            // ARRANGE
            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, string.Empty, string.Empty, TimeSpan.FromMilliseconds(50));
            this.emulateTimeout = true;

            // ACT
            serviceClient.Ping(string.Empty, DateTime.UtcNow);

            // ASSERT
            this.listener.Stop();
            
            Assert.AreEqual(3, this.pingCount);
        }

        [TestMethod]
        public void QuickPulseServiceClientDoesNotRetrySubmitSamples()
        {
            // ARRANGE
            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, string.Empty, string.Empty, TimeSpan.FromMilliseconds(50));
            this.emulateTimeout = true;

            // ACT
            serviceClient.SubmitSamples(new QuickPulseDataSample[] { }, string.Empty);

            // ASSERT
            this.listener.Stop();

            Assert.AreEqual(1, this.submitCount);
        }

        [TestMethod]
        public void QuickPulseServiceClientSubmitsInstanceNameToService()
        {
            // ARRANGE
            var now = DateTime.UtcNow;
            var instanceName = "this instance";
            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, instanceName, string.Empty);
            var sample = new QuickPulseDataSample(
                new QuickPulseDataAccumulator { StartTimestamp = now, EndTimestamp = now.AddSeconds(1) },
                new Dictionary<string, Tuple<PerformanceCounterData, float>>());

            // ACT
            serviceClient.SubmitSamples(new[] { sample }, string.Empty);

            // ASSERT
            this.listener.Stop();

            Assert.AreEqual(1, this.samples.Count);
            Assert.AreEqual(instanceName, this.samples[0].Instance);
        }

        [TestMethod]
        public void QuickPulseServiceClientSubmitsVersionToServiceWithPing()
        {
            // ARRANGE
            var now = DateTime.UtcNow;
            var version = "this version";
            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, string.Empty, version);
           
            // ACT
            serviceClient.Ping("some ikey", now);

            // ASSERT
            this.listener.Stop();

            Assert.AreEqual(1, this.pingCount);
            Assert.AreEqual(version, this.lastVersion);
        }
        
        [TestMethod]
        public void QuickPulseServiceClientSubmitsVersionToServiceWithSubmitSamples()
        {
            // ARRANGE
            var now = DateTime.UtcNow;
            var version = "this version";
            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, string.Empty, version);
            var sample = new QuickPulseDataSample(
                new QuickPulseDataAccumulator { StartTimestamp = now, EndTimestamp = now.AddSeconds(1) },
                new Dictionary<string, Tuple<PerformanceCounterData, float>>());

            // ACT
            serviceClient.SubmitSamples(new[] { sample }, string.Empty);

            // ASSERT
            this.listener.Stop();

            Assert.AreEqual(1, this.samples.Count);
            Assert.AreEqual(version, this.samples[0].Version);
        }

        [TestMethod]
        public void QuickPulseServiceClientSubmitsInstrumentationKeyToService()
        {
            // ARRANGE
            var now = DateTime.UtcNow;
            var ikey = "some ikey";
            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, string.Empty, string.Empty);
            var sample = new QuickPulseDataSample(
                new QuickPulseDataAccumulator { StartTimestamp = now, EndTimestamp = now.AddSeconds(1) },
                new Dictionary<string, Tuple<PerformanceCounterData, float>>());

            // ACT
            serviceClient.SubmitSamples(new[] { sample }, ikey);

            // ASSERT
            this.listener.Stop();

            Assert.AreEqual(1, this.samples.Count);
            Assert.AreEqual(ikey, this.samples[0].InstrumentationKey);
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

                    var serializer = new DataContractJsonSerializer(typeof(MonitoringDataPoint));
                    var dataPoint = (MonitoringDataPoint)serializer.ReadObject(context.Request.InputStream);

                    this.lastPingTimestamp = dataPoint.Timestamp;
                    this.lastPingInstance = dataPoint.Instance;
                    this.lastVersion = dataPoint.Version;
                }
                else if (request.Url.LocalPath == "/post")
                {
                    this.submitCount++;

                    this.submitResponse(context.Response);

                    var serializer = new DataContractJsonSerializer(typeof(MonitoringDataPoint[]));
                    var dataPoints = serializer.ReadObject(context.Request.InputStream) as MonitoringDataPoint[];

                    this.samples.AddRange(dataPoints);
                }

                if (!this.emulateTimeout)
                {
                    context.Response.Close();
                }
            }
        }

        #endregion
    }
}