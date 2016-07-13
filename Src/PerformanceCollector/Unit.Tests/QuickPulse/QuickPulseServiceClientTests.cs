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
        private const int Port = 49152 + 11;

        private readonly Uri serviceEndpoint = new Uri(string.Format(CultureInfo.InvariantCulture, "http://localhost:{0}", Port));

        private readonly List<Tuple<DateTimeOffset, MonitoringDataPoint>> samples = new List<Tuple<DateTimeOffset, MonitoringDataPoint>>();

        private readonly List<Tuple<DateTimeOffset, MonitoringDataPoint>> pings = new List<Tuple<DateTimeOffset, MonitoringDataPoint>>();

        private Action<HttpListenerResponse> pingResponse;

        private Action<HttpListenerResponse> submitResponse;

        private HttpListener listener;

        private int pingCount;

        private int submitCount;

        private DateTimeOffset? lastPingTimestamp;

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

            string uriPrefix = string.Format(CultureInfo.InvariantCulture, "http://localhost:{0}/", Port);

            this.listener = new HttpListener();
            this.listener.Prefixes.Add(uriPrefix);
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
            var timestamp = DateTimeOffset.UtcNow;

            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, instance, instance, string.Empty, new Clock());

            // ACT
            serviceClient.Ping(string.Empty, timestamp);
            serviceClient.Ping(string.Empty, timestamp);
            serviceClient.Ping(string.Empty, timestamp);

            // ASSERT
            Assert.AreEqual(3, this.pingCount);
            Assert.AreEqual(timestamp.DateTime.ToLongTimeString(), this.lastPingTimestamp.Value.DateTime.ToLongTimeString());
            Assert.AreEqual(instance, this.lastPingInstance);
        }

        [TestMethod]
        public void QuickPulseServiceClientSubmitsSamplesToService()
        {
            // ARRANGE
            var now = DateTimeOffset.UtcNow;
            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, string.Empty, string.Empty, string.Empty, new Clock());
            var sample1 =
                new QuickPulseDataSample(
                    new QuickPulseDataAccumulator { AIRequestSuccessCount = 5, StartTimestamp = now, EndTimestamp = now.AddSeconds(1) },
                    new Dictionary<string, Tuple<PerformanceCounterData, float>>());

            var sample2 =
                new QuickPulseDataSample(
                    new QuickPulseDataAccumulator { AIDependencyCallSuccessCount = 10, StartTimestamp = now, EndTimestamp = now.AddSeconds(1) },
                    new Dictionary<string, Tuple<PerformanceCounterData, float>>());

            var sample3 =
                new QuickPulseDataSample(
                    new QuickPulseDataAccumulator { AIExceptionCount = 15, StartTimestamp = now, EndTimestamp = now.AddSeconds(1) },
                    new Dictionary<string, Tuple<PerformanceCounterData, float>>());

            // ACT
            bool? sendMore = serviceClient.SubmitSamples(new[] { sample1, sample2, sample3 }, string.Empty);

            // ASSERT
            this.listener.Stop();

            Assert.AreEqual(true, sendMore);
            Assert.AreEqual(3, this.samples.Count);
            Assert.AreEqual(5, this.samples[0].Item2.Metrics.Single(m => m.Name == @"\ApplicationInsights\Requests Succeeded/Sec").Value);
            Assert.AreEqual(10, this.samples[1].Item2.Metrics.Single(m => m.Name == @"\ApplicationInsights\Dependency Calls Succeeded/Sec").Value);
            Assert.AreEqual(15, this.samples[2].Item2.Metrics.Single(m => m.Name == @"\ApplicationInsights\Exceptions/Sec").Value);
        }

        [TestMethod]
        public void QuickPulseServiceClientSetsTransmissionTimeCorrectly()
        {
            // ARRANGE
            var dummy = new Dictionary<string, Tuple<PerformanceCounterData, float>>();
            var timeProvider = new ClockMock();

            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, string.Empty, string.Empty, string.Empty, timeProvider);
            var sample1 =
                new QuickPulseDataSample(
                    new QuickPulseDataAccumulator { StartTimestamp = timeProvider.UtcNow.AddSeconds(-1), EndTimestamp = timeProvider.UtcNow },
                    dummy);

            timeProvider.FastForward(TimeSpan.FromSeconds(1));
            var sample2 =
                new QuickPulseDataSample(
                    new QuickPulseDataAccumulator { StartTimestamp = timeProvider.UtcNow.AddSeconds(-1), EndTimestamp = timeProvider.UtcNow },
                    dummy);

            timeProvider.FastForward(TimeSpan.FromSeconds(1));
            var sample3 =
                new QuickPulseDataSample(
                    new QuickPulseDataAccumulator { StartTimestamp = timeProvider.UtcNow.AddSeconds(-1), EndTimestamp = timeProvider.UtcNow },
                    dummy);

            // ACT
            timeProvider.FastForward(TimeSpan.FromSeconds(5));
            serviceClient.SubmitSamples(new[] { sample1, sample2, sample3 }, string.Empty);

            // ASSERT
            this.listener.Stop();

            Assert.AreEqual(3, this.samples.Count);
            Assert.IsTrue((timeProvider.UtcNow - this.samples[0].Item1).Duration() < TimeSpan.FromMilliseconds(1));
            Assert.IsTrue((timeProvider.UtcNow - this.samples[1].Item1).Duration() < TimeSpan.FromMilliseconds(1));
            Assert.IsTrue((timeProvider.UtcNow - this.samples[2].Item1).Duration() < TimeSpan.FromMilliseconds(1));
            Assert.IsTrue(this.samples.All(s => (s.Item2.Timestamp - timeProvider.UtcNow).Duration() > TimeSpan.FromSeconds(1)));
        }

        [TestMethod]
        public void QuickPulseServiceClientRoundsSampleValuesWhenSubmittingToService()
        {
            // ARRANGE
            var now = DateTimeOffset.UtcNow;
            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, string.Empty, string.Empty, string.Empty, new Clock());
            var sample1 =
                new QuickPulseDataSample(
                    new QuickPulseDataAccumulator { AIRequestSuccessCount = 1, StartTimestamp = now, EndTimestamp = now.AddSeconds(3) },
                    new Dictionary<string, Tuple<PerformanceCounterData, float>>());

            // ACT
            serviceClient.SubmitSamples(new[] { sample1 }, string.Empty);

            // ASSERT
            this.listener.Stop();

            Assert.AreEqual(0.3333, this.samples[0].Item2.Metrics.Single(m => m.Name == @"\ApplicationInsights\Requests Succeeded/Sec").Value);
        }

        [TestMethod]
        public void QuickPulseServiceClientFillsInSampleWeightWhenSubmittingToService()
        {
            // ARRANGE
            var now = DateTimeOffset.UtcNow;
            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, string.Empty, string.Empty, string.Empty, new Clock());
            var sample1 =
                new QuickPulseDataSample(
                    new QuickPulseDataAccumulator
                        {
                            AIRequestCountAndDurationInTicks = QuickPulseDataAccumulator.EncodeCountAndDuration(3, 10000),
                            StartTimestamp = now,
                            EndTimestamp = now.AddSeconds(1)
                        },
                    new Dictionary<string, Tuple<PerformanceCounterData, float>>());

            var sample2 =
                new QuickPulseDataSample(
                    new QuickPulseDataAccumulator
                        {
                            AIDependencyCallCountAndDurationInTicks =
                                QuickPulseDataAccumulator.EncodeCountAndDuration(4, 10000),
                            StartTimestamp = now,
                            EndTimestamp = now.AddSeconds(1)
                        },
                    new Dictionary<string, Tuple<PerformanceCounterData, float>>());

            // ACT
            serviceClient.SubmitSamples(new[] { sample1, sample2 }, string.Empty);

            // ASSERT
            this.listener.Stop();

            Assert.AreEqual(3, this.samples[0].Item2.Metrics.Single(m => m.Name == @"\ApplicationInsights\Request Duration").Weight);
            Assert.AreEqual(4, this.samples[1].Item2.Metrics.Single(m => m.Name == @"\ApplicationInsights\Dependency Call Duration").Weight);
        }

        [TestMethod]
        public void QuickPulseServiceClientInterpretsPingResponseCorrectlyWhenHeaderTrue()
        {
            // ARRANGE
            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, string.Empty, string.Empty, string.Empty, new Clock());

            // ACT
            this.pingResponse = r => { r.AddHeader("x-ms-qps-subscribed", true.ToString()); };
            bool? response = serviceClient.Ping(string.Empty, DateTimeOffset.UtcNow);

            // ASSERT
            this.listener.Stop();

            Assert.IsTrue(response.Value);
        }

        [TestMethod]
        public void QuickPulseServiceClientInterpretsPingResponseCorrectlyWhenHeaderFalse()
        {
            // ARRANGE
            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, string.Empty, string.Empty, string.Empty, new Clock());

            // ACT
            this.pingResponse = r => { r.AddHeader("x-ms-qps-subscribed", false.ToString()); };
            bool? response = serviceClient.Ping(string.Empty, DateTimeOffset.UtcNow);

            // ASSERT
            this.listener.Stop();

            Assert.IsFalse(response.Value);
        }

        [TestMethod]
        public void QuickPulseServiceClientInterpretsPingResponseCorrectlyWhenHeaderInvalid()
        {
            // ARRANGE
            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, string.Empty, string.Empty, string.Empty, new Clock());

            // ACT
            this.pingResponse = r => { r.AddHeader("x-ms-qps-subscribed", "bla"); };
            bool? response = serviceClient.Ping(string.Empty, DateTimeOffset.UtcNow);

            // ASSERT
            this.listener.Stop();

            Assert.IsNull(response);
        }

        [TestMethod]
        public void QuickPulseServiceClientInterpretsPingResponseCorrectlyWhenHeaderMissing()
        {
            // ARRANGE
            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, string.Empty, string.Empty, string.Empty, new Clock());

            // ACT
            this.pingResponse = r => { };
            bool? response = serviceClient.Ping(string.Empty, DateTimeOffset.UtcNow);

            // ASSERT
            this.listener.Stop();

            Assert.IsNull(response);
        }

        [TestMethod]
        public void QuickPulseServiceClientInterpretsSubmitSamplesResponseCorrectlyWhenHeaderTrue()
        {
            // ARRANGE
            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, string.Empty, string.Empty, string.Empty, new Clock());

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
            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, string.Empty, string.Empty, string.Empty, new Clock());

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
            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, string.Empty, string.Empty, string.Empty, new Clock());

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
            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, string.Empty, string.Empty, string.Empty, new Clock());

            // ACT
            this.submitResponse = r => { };
            bool? response = serviceClient.SubmitSamples(new QuickPulseDataSample[] { }, string.Empty);

            // ASSERT
            this.listener.Stop();

            Assert.IsNull(response);
        }

        [TestMethod]
        public void QuickPulseServiceClientDoesNotRetryPing()
        {
            // ARRANGE
            var serviceClient = new QuickPulseServiceClient(
                this.serviceEndpoint,
                string.Empty,
                string.Empty,
                string.Empty,
                new Clock(),
                TimeSpan.FromMilliseconds(50));

            this.emulateTimeout = true;

            // ACT
            serviceClient.Ping(string.Empty, DateTimeOffset.UtcNow);

            // ASSERT
            this.listener.Stop();

            Assert.AreEqual(1, this.pingCount);
        }

        [TestMethod]
        public void QuickPulseServiceClientDoesNotRetrySubmitSamples()
        {
            // ARRANGE
            var serviceClient = new QuickPulseServiceClient(
                this.serviceEndpoint,
                string.Empty,
                string.Empty,
                string.Empty,
                new Clock(),
                TimeSpan.FromMilliseconds(50));

            this.emulateTimeout = true;

            // ACT
            serviceClient.SubmitSamples(new QuickPulseDataSample[] { }, string.Empty);

            // ASSERT
            this.listener.Stop();

            Assert.AreEqual(1, this.submitCount);
        }

        [TestMethod]
        public void QuickPulseServiceClientSubmitsInstanceNameToServiceWithPing()
        {
            // ARRANGE
            var instanceName = "this instance";
            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, instanceName, instanceName, string.Empty, new Clock());

            // ACT
            serviceClient.Ping(Guid.NewGuid().ToString(), DateTimeOffset.UtcNow);

            // ASSERT
            this.listener.Stop();

            Assert.AreEqual(1, this.pings.Count);
            Assert.AreEqual(instanceName, this.pings[0].Item2.Instance);
        }

        [TestMethod]
        public void QuickPulseServiceClientSubmitsInstanceNameToServiceWithSubmitSamples()
        {
            // ARRANGE
            var now = DateTimeOffset.UtcNow;
            var instanceName = "this instance";
            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, instanceName, instanceName, string.Empty, new Clock());
            var sample = new QuickPulseDataSample(
                new QuickPulseDataAccumulator { StartTimestamp = now, EndTimestamp = now.AddSeconds(1) },
                new Dictionary<string, Tuple<PerformanceCounterData, float>>());

            // ACT
            serviceClient.SubmitSamples(new[] { sample }, string.Empty);

            // ASSERT
            this.listener.Stop();

            Assert.AreEqual(1, this.samples.Count);
            Assert.AreEqual(instanceName, this.samples[0].Item2.Instance);
        }

        [TestMethod]
        public void QuickPulseServiceClientSubmitsStreamIdToServiceWithPing()
        {
            // ARRANGE
            var streamId = "this stream";
            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, string.Empty, streamId, string.Empty, new Clock());

            // ACT
            serviceClient.Ping(Guid.NewGuid().ToString(), DateTimeOffset.UtcNow);

            // ASSERT
            this.listener.Stop();

            Assert.AreEqual(1, this.pings.Count);
            Assert.AreEqual(streamId, this.pings[0].Item2.StreamId);
        }

        [TestMethod]
        public void QuickPulseServiceClientSubmitsStreamIdToServiceWithSubmitSamples()
        {
            // ARRANGE
            var now = DateTimeOffset.UtcNow;
            var streamId = "this stream";
            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, string.Empty, streamId, string.Empty, new Clock());
            var sample = new QuickPulseDataSample(
                new QuickPulseDataAccumulator { StartTimestamp = now, EndTimestamp = now.AddSeconds(1) },
                new Dictionary<string, Tuple<PerformanceCounterData, float>>());

            // ACT
            serviceClient.SubmitSamples(new[] { sample }, string.Empty);

            // ASSERT
            this.listener.Stop();

            Assert.AreEqual(1, this.samples.Count);
            Assert.AreEqual(streamId, this.samples[0].Item2.StreamId);
        }

        [TestMethod]
        public void QuickPulseServiceClientSubmitsVersionToServiceWithPing()
        {
            // ARRANGE
            var now = DateTimeOffset.UtcNow;
            var version = "this version";
            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, string.Empty, string.Empty, version, new Clock());

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
            var now = DateTimeOffset.UtcNow;
            var version = "this version";
            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, string.Empty, string.Empty, version, new Clock());
            var sample = new QuickPulseDataSample(
                new QuickPulseDataAccumulator { StartTimestamp = now, EndTimestamp = now.AddSeconds(1) },
                new Dictionary<string, Tuple<PerformanceCounterData, float>>());

            // ACT
            serviceClient.SubmitSamples(new[] { sample }, string.Empty);

            // ASSERT
            this.listener.Stop();

            Assert.AreEqual(1, this.samples.Count);
            Assert.AreEqual(version, this.samples[0].Item2.Version);
        }

        [TestMethod]
        public void QuickPulseServiceClientSubmitsInstrumentationKeyToService()
        {
            // ARRANGE
            var now = DateTimeOffset.UtcNow;
            var ikey = "some ikey";
            var serviceClient = new QuickPulseServiceClient(this.serviceEndpoint, string.Empty, string.Empty, string.Empty, new Clock());
            var sample = new QuickPulseDataSample(
                new QuickPulseDataAccumulator { StartTimestamp = now, EndTimestamp = now.AddSeconds(1) },
                new Dictionary<string, Tuple<PerformanceCounterData, float>>());

            // ACT
            serviceClient.SubmitSamples(new[] { sample }, ikey);

            // ASSERT
            this.listener.Stop();

            Assert.AreEqual(1, this.samples.Count);
            Assert.AreEqual(ikey, this.samples[0].Item2.InstrumentationKey);
        }
        
        public void Dispose()
        {
            ((IDisposable)this.listener).Dispose();
        }

        #region Helpers

        private void ProcessRequest(HttpListener listener)
        {
            var serializerDataPoint = new DataContractJsonSerializer(typeof(MonitoringDataPoint));
            var serializerDataPointArray = new DataContractJsonSerializer(typeof(MonitoringDataPoint[]));

            while (listener.IsListening)
            {
                HttpListenerContext context = listener.GetContext();

                var request = context.Request;
                if (request.Url.LocalPath == "/ping")
                {
                    this.pingCount++;

                    this.pingResponse(context.Response);

                    var dataPoint = (MonitoringDataPoint)serializerDataPoint.ReadObject(context.Request.InputStream);
                    var transmissionTime = long.Parse(context.Request.Headers["x-ms-qps-transmission-time"], CultureInfo.InvariantCulture);

                    this.pings.Add(Tuple.Create(new DateTimeOffset(transmissionTime, TimeSpan.Zero), dataPoint));

                    this.lastPingTimestamp = dataPoint.Timestamp;
                    this.lastPingInstance = dataPoint.Instance;
                    this.lastVersion = dataPoint.Version;
                }
                else if (request.Url.LocalPath == "/post")
                {
                    this.submitCount++;

                    this.submitResponse(context.Response);

                    var dataPoints = serializerDataPointArray.ReadObject(context.Request.InputStream) as MonitoringDataPoint[];
                    var transmissionTime = long.Parse(context.Request.Headers["x-ms-qps-transmission-time"], CultureInfo.InvariantCulture);

                    this.samples.AddRange(dataPoints.Select(dp => Tuple.Create(new DateTimeOffset(transmissionTime, TimeSpan.Zero), dp)));
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