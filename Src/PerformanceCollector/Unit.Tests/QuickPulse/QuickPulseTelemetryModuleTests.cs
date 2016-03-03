namespace Unit.Tests
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Threading;

    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
    using Microsoft.ApplicationInsights.Web.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class QuickPulseTelemetryModuleTests
    {
        [TestInitialize]
        public void Initialize()
        {
            TelemetryConfiguration.Active.InstrumentationKey = string.Empty;
        }

        [TestMethod]
        public void QuickPulseTelemetryModuleIsInitializedBySdk()
        {
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            var configuration = new TelemetryConfiguration();
            var builder = configuration.TelemetryProcessorChainBuilder;
            builder = builder.Use(current => telemetryProcessor);
            builder.Build();

            new QuickPulseTelemetryModule().Initialize(configuration);
        }

        [TestMethod]
        public void QuickPulseTelemetryModuleInitializesServiceClientFromConfiguration()
        {
            // ARRANGE
            var module = new QuickPulseTelemetryModule(
                null,
                null,
                new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy()),
                null,
                null,
                null);

            module.QuickPulseServiceEndpoint = "https://test.com/api";

            // ACT
            module.Initialize(new TelemetryConfiguration());

            // ASSERT
            Assert.IsInstanceOfType(GetPrivateField(module, "serviceClient"), typeof(QuickPulseServiceClient));
        }

        [TestMethod]
        public void QuickPulseTelemetryModuleInitializesServiceClientFromDefault()
        {
            // ARRANGE
            var module = new QuickPulseTelemetryModule(
                null,
                null,
                new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy()),
                null,
                null,
                null);

            // ACT
            // do not provide module configuration, force default service client
            module.Initialize(new TelemetryConfiguration());

            // ASSERT
            IQuickPulseServiceClient serviceClient = (IQuickPulseServiceClient)GetPrivateField(module, "serviceClient");

            Assert.IsInstanceOfType(serviceClient, typeof(QuickPulseServiceClient));
            Assert.AreEqual(GetPrivateField(module, "serviceUriDefault"), serviceClient.ServiceUri);
        }

        [TestMethod]
        public void QuickPulseTelemetryModuleDoesNothingWithoutInstrumentationKey()
        {
            // ARRANGE
            var interval = TimeSpan.FromMilliseconds(1);
            var timings = new QuickPulseTimings(interval, interval);
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = true, ReturnValueFromSubmitSample = true };
            var performanceCollector = new PerformanceCollectorMock();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());

            var module = new QuickPulseTelemetryModule(null, null, telemetryProcessor, serviceClient, performanceCollector, timings);

            module.Initialize(new TelemetryConfiguration());

            // ACT
            Thread.Sleep(TimeSpan.FromMilliseconds(100));

            // ASSERT
            Assert.AreEqual(0, serviceClient.PingCount);
            Assert.AreEqual(0, serviceClient.SnappedSamples.Count);
        }

        [TestMethod]
        public void QuickPulseTelemetryModulePicksUpInstrumentationKeyAsItGoes()
        {
            // ARRANGE
            var interval = TimeSpan.FromMilliseconds(1);
            var timings = new QuickPulseTimings(interval, interval);
            var collectionTimeSlotManager = new QuickPulseCollectionTimeSlotManagerMock(timings);
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = true, ReturnValueFromSubmitSample = true };
            var performanceCollector = new PerformanceCollectorMock();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());

            var module = new QuickPulseTelemetryModule(
                collectionTimeSlotManager,
                null,
                telemetryProcessor,
                serviceClient,
                performanceCollector,
                timings);

            var config = new TelemetryConfiguration();
            module.Initialize(config);

            // ACT
            Thread.Sleep(TimeSpan.FromMilliseconds(100));
            config.InstrumentationKey = "some ikey";
            Thread.Sleep(TimeSpan.FromMilliseconds(100));

            // ASSERT
            Assert.IsTrue(serviceClient.PingCount > 0);
            Assert.IsTrue(serviceClient.SnappedSamples.Count > 0);
        }

        [TestMethod]
        public void QuickPulseTelemetryModulePingsService()
        {
            // ARRANGE
            var interval = TimeSpan.FromMilliseconds(1);
            var timings = new QuickPulseTimings(interval, interval);
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = false, ReturnValueFromSubmitSample = false };
            
            var module = new QuickPulseTelemetryModule(
                null,
                null,
                new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy()),
                serviceClient,
                new PerformanceCollectorMock(),
                timings);

            // ACT
            module.Initialize(new TelemetryConfiguration() { InstrumentationKey = "some ikey" });

            // ASSERT
            Thread.Sleep((int)(interval.TotalMilliseconds * 100));

            Assert.IsTrue(serviceClient.PingCount > 0);
            Assert.AreEqual(0, serviceClient.SnappedSamples.Count);
        }

        [TestMethod]
        public void QuickPulseTelemetryModuleCollectsData()
        {
            // ARRANGE
            var interval = TimeSpan.FromMilliseconds(1);
            var timings = new QuickPulseTimings(interval, interval);
            var collectionTimeSlotManager = new QuickPulseCollectionTimeSlotManagerMock(timings);
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = true, ReturnValueFromSubmitSample = true };
            var performanceCollector = new PerformanceCollectorMock();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            
            var module = new QuickPulseTelemetryModule(collectionTimeSlotManager, null, telemetryProcessor, serviceClient, performanceCollector, timings);

            // ACT
            module.Initialize(new TelemetryConfiguration() { InstrumentationKey = "some ikey" });

            Thread.Sleep((int)(interval.TotalMilliseconds * 100));

            telemetryProcessor.Process(new RequestTelemetry() { Context = { InstrumentationKey = "some ikey" } });
            telemetryProcessor.Process(new DependencyTelemetry() { Context = { InstrumentationKey = "some ikey" } });

            Thread.Sleep((int)(interval.TotalMilliseconds * 100));

            // ASSERT
            Assert.AreEqual(1, serviceClient.PingCount);
            Assert.IsTrue(serviceClient.SnappedSamples.Count > 0);

            Assert.IsTrue(serviceClient.SnappedSamples.Any(s => s.AIRequestsPerSecond > 0));
            Assert.IsTrue(serviceClient.SnappedSamples.Any(s => s.AIDependencyCallsPerSecond > 0));
            Assert.IsTrue(serviceClient.SnappedSamples.Any(s => Math.Abs(s.PerfIisRequestsPerSecond) > double.Epsilon));
        }

        [TestMethod]
        public void QuickPulseTelemetryModuleTimestampsDataSamples()
        {
            // ARRANGE
            var interval = TimeSpan.FromMilliseconds(1);
            var timings = new QuickPulseTimings(interval, interval);
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = true, ReturnValueFromSubmitSample = true };
            var performanceCollector = new PerformanceCollectorMock();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());

            var module = new QuickPulseTelemetryModule(null, null, telemetryProcessor, serviceClient, performanceCollector, timings);

            var timestampStart = DateTimeOffset.UtcNow;

            // ACT
            module.Initialize(new TelemetryConfiguration());

            Thread.Sleep((int)(interval.TotalMilliseconds * 100));

            // ASSERT
            var timestampEnd = DateTimeOffset.UtcNow;
            Assert.IsTrue(serviceClient.SnappedSamples.All(s => s.StartTimestamp > timestampStart));
            Assert.IsTrue(serviceClient.SnappedSamples.All(s => s.StartTimestamp < timestampEnd));
            Assert.IsTrue(serviceClient.SnappedSamples.All(s => s.StartTimestamp <= s.EndTimestamp));
        }

        [TestMethod]
        public void QuickPulseTelemetryModuleFetchesTelemetryProcessorFromConfiguration()
        {
            // ARRANGE
            var module = new QuickPulseTelemetryModule(null, null, null, null, null, null);

            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            var configuration = new TelemetryConfiguration();
            var builder = configuration.TelemetryProcessorChainBuilder;
            builder = builder.Use(current => telemetryProcessor);
            builder.Build();

            // ACT
            module.Initialize(configuration);

            // ASSERT
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void QuickPulseTelemetryModuleThrowsWhenNoTelemetryProcessorPresentInConfiguration()
        {
            // ARRANGE
            var module = new QuickPulseTelemetryModule(null, null, null, null, null, null);

            // ACT
            module.Initialize(new TelemetryConfiguration());

            // ASSERT
        }

        [TestMethod]
        public void QuickPulseTelemetryModuleManagesTimersCorrectly()
        {
            // ARRANGE
            var timings = new QuickPulseTimings(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(10));
            var collectionTimeSlotManager = new QuickPulseCollectionTimeSlotManagerMock(timings);
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = false, ReturnValueFromSubmitSample = true };
            var performanceCollector = new PerformanceCollectorMock();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
           
            var module = new QuickPulseTelemetryModule(
                collectionTimeSlotManager,
                null,
                telemetryProcessor,
                serviceClient,
                performanceCollector,
                timings);

            // ACT & ASSERT
            module.Initialize(new TelemetryConfiguration() { InstrumentationKey = "some ikey" });

            // initially, the module is in the polling state
            Thread.Sleep((int)(2.5 * TimeSpan.FromMilliseconds(100).TotalMilliseconds));

            // 2.5 polling intervals have elapsed, we must have pinged the service 3 times (the first time immediately upon initialization), but no samples yet
            Assert.AreEqual(3, serviceClient.PingCount);
            Assert.AreEqual(0, serviceClient.SnappedSamples.Count);

            serviceClient.Reset();

            // now the service wants the data
            serviceClient.ReturnValueFromPing = true;
            serviceClient.ReturnValueFromSubmitSample = true;

            Thread.Sleep((int)(30 * TimeSpan.FromMilliseconds(10).TotalMilliseconds));

            // 30  collection intervals have elapsed, we must have pinged the service once, and then started sending samples
            Assert.AreEqual(1, serviceClient.PingCount);
            Assert.IsTrue(serviceClient.SnappedSamples.Count > 0);

            serviceClient.Reset();

            // the service doesn't want the data anymore
            serviceClient.ReturnValueFromPing = false;
            serviceClient.ReturnValueFromSubmitSample = false;

            Thread.Sleep((int)(2.5 * TimeSpan.FromMilliseconds(100).TotalMilliseconds));

            // 2.5 polling intervals have elapsed, we must have submitted one sample, stopped collecting and pinged the service twice afterwards
            Assert.AreEqual(1, serviceClient.SnappedSamples.Count);
            Assert.AreEqual(2, serviceClient.PingCount);
        }

        [TestMethod]
        public void QuickPulseTelemetryModuleResendsFailedSamples()
        {
            // ARRANGE
            var interval = TimeSpan.FromMilliseconds(1);
            var timings = new QuickPulseTimings(interval, interval);
            var collectionTimeSlotManager = new QuickPulseCollectionTimeSlotManagerMock(timings);
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = true, ReturnValueFromSubmitSample = null };
            var performanceCollector = new PerformanceCollectorMock();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            
            var module = new QuickPulseTelemetryModule(
                collectionTimeSlotManager,
                null,
                telemetryProcessor,
                serviceClient,
                performanceCollector,
                timings);

            module.Initialize(new TelemetryConfiguration() { InstrumentationKey = "some ikey" });

            // ACT
            // below timeout should be sufficient for the module to get to maximum storage capacity
            Thread.Sleep(TimeSpan.FromMilliseconds(200));

            // ASSERT
            Assert.AreEqual(10, serviceClient.LastSampleBatchSize);
        }

        [TestMethod]
        public void QuickPulseTelemetryModuleHandlesUnexpectedExceptions()
        {
            // ARRANGE
            var interval = TimeSpan.FromMilliseconds(1);
            var timings = new QuickPulseTimings(interval, interval, interval, interval, interval, interval);
            var collectionTimeSlotManager = new QuickPulseCollectionTimeSlotManagerMock(timings);
            var serviceClient = new QuickPulseServiceClientMock { AlwaysThrow = true, ReturnValueFromPing = false, ReturnValueFromSubmitSample = null };
            var performanceCollector = new PerformanceCollectorMock();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());

            var module = new QuickPulseTelemetryModule(
                collectionTimeSlotManager,
                null,
                telemetryProcessor,
                serviceClient,
                performanceCollector,
                timings);

            module.Initialize(new TelemetryConfiguration() { InstrumentationKey = "some ikey" });

            // ACT
            Thread.Sleep(TimeSpan.FromMilliseconds(100));

            // ASSERT
            // it shouldn't throw and must keep pinging
            Assert.IsTrue(serviceClient.PingCount > 5);
        }

        [TestMethod]
        public void QuickPulseTelemetryModuleDisposesCorrectly()
        {
            // ARRANGE
            var interval = TimeSpan.FromMilliseconds(1);
            var timings = new QuickPulseTimings(interval, interval, interval, interval, interval, interval);
            var collectionTimeSlotManager = new QuickPulseCollectionTimeSlotManagerMock(timings);
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = true, ReturnValueFromSubmitSample = true };
            var performanceCollector = new PerformanceCollectorMock();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());

            var module = new QuickPulseTelemetryModule(
                collectionTimeSlotManager,
                null,
                telemetryProcessor,
                serviceClient,
                performanceCollector,
                timings);

            module.Initialize(new TelemetryConfiguration() { InstrumentationKey = "some ikey" });

            // ACT
            Thread.Sleep(TimeSpan.FromMilliseconds(100));

            // ASSERT
            module.Dispose();
        }

        [TestMethod]
        public void QuickPulseTelemetryModuleEndsInternalThreads()
        {
            // ARRANGE
            var interval = TimeSpan.FromMilliseconds(1);
            var timings = new QuickPulseTimings(interval, interval, interval, interval, interval, interval);
            var collectionTimeSlotManager = new QuickPulseCollectionTimeSlotManagerMock(timings);
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = false, ReturnValueFromSubmitSample = false };
            var performanceCollector = new PerformanceCollectorMock();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());

            var module = new QuickPulseTelemetryModule(
                collectionTimeSlotManager,
                null,
                telemetryProcessor,
                serviceClient,
                performanceCollector,
                timings);

            var initialThreadCount = Process.GetCurrentProcess().Threads.Count;

            module.Initialize(new TelemetryConfiguration() { InstrumentationKey = "some ikey" });

            Thread.Sleep(TimeSpan.FromMilliseconds(100));

            // ACT & ASSERT
            // state thread expected
            Assert.AreEqual(initialThreadCount + 1, Process.GetCurrentProcess().Threads.Count);

            // this will flip-flop between collection and no collection, creating and ending a collection thread each time
            serviceClient.ReturnValueFromPing = true;
            serviceClient.ReturnValueFromSubmitSample = false;
            Thread.Sleep(TimeSpan.FromMilliseconds(100));

            serviceClient.ReturnValueFromPing = true;
            serviceClient.ReturnValueFromSubmitSample = true;
            Thread.Sleep(TimeSpan.FromMilliseconds(100));

            // state and collection threads expected
            Assert.AreEqual(initialThreadCount + 2, Process.GetCurrentProcess().Threads.Count);

            serviceClient.ReturnValueFromPing = false;
            serviceClient.ReturnValueFromSubmitSample = false;
            Thread.Sleep(TimeSpan.FromMilliseconds(100));

            // only state thread expected
            Assert.AreEqual(initialThreadCount + 1, Process.GetCurrentProcess().Threads.Count);

            module.Dispose();

            Thread.Sleep(TimeSpan.FromMilliseconds(100));

            // no threads expected
            Assert.AreEqual(initialThreadCount, Process.GetCurrentProcess().Threads.Count);
        }

        #region Helpers
        private static void SetPrivateProperty(object obj, string propertyName, string propertyValue)
        {
            PropertyInfo propertyInfo = obj.GetType().GetProperty(propertyName);
            propertyInfo.GetSetMethod(true).Invoke(obj, new object[] { propertyValue });
        }

        private static object GetPrivateField(object obj, string fieldName)
        {
            FieldInfo fieldInfo = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            return fieldInfo.GetValue(obj);
        }

        #endregion
    }
}