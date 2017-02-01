namespace Unit.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse.Helpers;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
    using Microsoft.ApplicationInsights.Web.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class QuickPulseTelemetryModuleTests
    {
        private readonly string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ApplicationInsights.config");

        [TestInitialize]
        public void TestInitialize()
        {
            QuickPulseTestHelper.ClearEnvironment();
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
        [ExpectedException(typeof(ArgumentNullException))]
        public void QuickPulseTelemetryModuleDoesNotRegisterNullProcessor()
        {
            // ARRANGE
            var module = new QuickPulseTelemetryModule(null, null, null, null, null, null);

            // ACT
            module.RegisterTelemetryProcessor(null);

            // ASSERT
        }

        [TestMethod]
        public void QuickPulseTelemetryModuleDoesNotRegisterSameProcessorMoreThanOnce()
        {
            // ARRANGE
            var module = new QuickPulseTelemetryModule(null, null, null, null, null, null);
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());

            // ACT
            module.RegisterTelemetryProcessor(telemetryProcessor);
            module.RegisterTelemetryProcessor(telemetryProcessor);
            module.RegisterTelemetryProcessor(telemetryProcessor);

            // ASSERT
            Assert.AreEqual(telemetryProcessor, QuickPulseTestHelper.GetTelemetryProcessors(module).Single());
        }

        [TestMethod]
        public void QuickPulseTelemetryModuleInitializesServiceClientFromConfiguration()
        {
            // ARRANGE
            var module = new QuickPulseTelemetryModule(null, null, null, null, null, null);

            module.QuickPulseServiceEndpoint = "https://test.com/api";

            // ACT
            module.Initialize(new TelemetryConfiguration());
            
            // ASSERT
            Assert.IsInstanceOfType(QuickPulseTestHelper.GetPrivateField(module, "serviceClient"), typeof(QuickPulseServiceClient));
        }

        [TestMethod]
        public void QuickPulseTelemetryModuleInitializesServiceClientFromDefault()
        {
            // ARRANGE
            var module = new QuickPulseTelemetryModule(null, null, null, null, null, null);

            // ACT
            // do not provide module configuration, force default service client
            module.Initialize(new TelemetryConfiguration());
            
            // ASSERT
            IQuickPulseServiceClient serviceClient = (IQuickPulseServiceClient)QuickPulseTestHelper.GetPrivateField(module, "serviceClient");

            Assert.IsInstanceOfType(serviceClient, typeof(QuickPulseServiceClient));
            Assert.AreEqual(QuickPulseDefaults.ServiceEndpoint, serviceClient.ServiceUri);
        }

        [TestMethod]
        public void QuickPulseTelemetryModuleDoesNothingWithoutInstrumentationKey()
        {
            // ARRANGE
            var interval = TimeSpan.FromMilliseconds(1);
            var timings = new QuickPulseTimings(interval, interval);
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = true, ReturnValueFromSubmitSample = true };
            var performanceCollector = new PerformanceCollectorMock();
            var topCpuCollector = new QuickPulseTopCpuCollectorMock();

            var module = new QuickPulseTelemetryModule(null, null, serviceClient, performanceCollector, topCpuCollector, timings);

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
            var topCpuCollector = new QuickPulseTopCpuCollectorMock();

            var module = new QuickPulseTelemetryModule(
                collectionTimeSlotManager,
                null,
                serviceClient,
                performanceCollector,
                topCpuCollector,
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
            var performanceCollector = new PerformanceCollectorMock();
            var topCpuCollector = new QuickPulseTopCpuCollectorMock();

            var module = new QuickPulseTelemetryModule(
                null,
                null,
                serviceClient,
                performanceCollector,
                topCpuCollector,
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
            var pause = TimeSpan.FromMilliseconds(100);
            var interval = TimeSpan.FromMilliseconds(1);
            var timings = new QuickPulseTimings(interval, interval);
            var collectionTimeSlotManager = new QuickPulseCollectionTimeSlotManagerMock(timings);
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = true, ReturnValueFromSubmitSample = true };
            var performanceCollector = new PerformanceCollectorMock();
            var topCpuCollector = new QuickPulseTopCpuCollectorMock()
                                      {
                                          TopProcesses = new List<Tuple<string, int>>() { Tuple.Create("Process1", 25) }
                                      };

            var module = new QuickPulseTelemetryModule(collectionTimeSlotManager, null, serviceClient, performanceCollector, topCpuCollector, timings);
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            telemetryProcessor.Initialize(new TelemetryConfiguration());
            module.RegisterTelemetryProcessor(telemetryProcessor);

            // ACT
            module.Initialize(new TelemetryConfiguration() { InstrumentationKey = "some ikey" });

            Thread.Sleep(pause);

            telemetryProcessor.Process(new RequestTelemetry() { Context = { InstrumentationKey = "some ikey" } });
            telemetryProcessor.Process(new DependencyTelemetry() { Context = { InstrumentationKey = "some ikey" } });

            Thread.Sleep(pause);

            Assert.AreEqual(1, serviceClient.PingCount);

            // ASSERT
            serviceClient.ReturnValueFromPing = false;
            serviceClient.ReturnValueFromSubmitSample = false;

            Thread.Sleep(pause);
            Assert.IsTrue(serviceClient.SnappedSamples.Count > 0);

            Assert.IsTrue(serviceClient.SnappedSamples.Any(s => s.AIRequestsPerSecond > 0));
            Assert.IsTrue(serviceClient.SnappedSamples.Any(s => s.AIDependencyCallsPerSecond > 0));
            Assert.IsTrue(
                serviceClient.SnappedSamples.Any(s => Math.Abs(s.PerfCountersLookup[@"\Processor(_Total)\% Processor Time"]) > double.Epsilon));

            Assert.IsTrue(
                serviceClient.SnappedSamples.TrueForAll(s => s.TopCpuData.Single().Item1 == "Process1" && s.TopCpuData.Single().Item2 == 25));
        }

        [TestMethod]
        public void QuickPulseTelemetryModuleDoesNotCollectTopCpuDataWhenSwitchedOff()
        {
            // ARRANGE
            var pause = TimeSpan.FromMilliseconds(100);
            var interval = TimeSpan.FromMilliseconds(1);
            var timings = new QuickPulseTimings(interval, interval);
            var collectionTimeSlotManager = new QuickPulseCollectionTimeSlotManagerMock(timings);
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = true, ReturnValueFromSubmitSample = true };
            var performanceCollector = new PerformanceCollectorMock();
            var topCpuCollector = new QuickPulseTopCpuCollectorMock()
            {
                TopProcesses = new List<Tuple<string, int>>() { Tuple.Create("Process1", 25) }
            };

            var module = new QuickPulseTelemetryModule(collectionTimeSlotManager, null, serviceClient, performanceCollector, topCpuCollector, timings);
            module.DisableTopCpuProcesses = true;
            
            // ACT
            module.Initialize(new TelemetryConfiguration() { InstrumentationKey = "some ikey" });

            Thread.Sleep(pause);

            Assert.AreEqual(1, serviceClient.PingCount);

            // ASSERT
            serviceClient.ReturnValueFromPing = false;
            serviceClient.ReturnValueFromSubmitSample = false;

            Thread.Sleep(pause);
            Assert.IsTrue(serviceClient.SnappedSamples.Count > 0);

            Assert.IsTrue(serviceClient.SnappedSamples.TrueForAll(s => !s.TopCpuData.Any()));
        }

        [TestMethod]
        public void QuickPulseTelemetryModuleInteractsWithTelemetryProcessorCorrectlyWhenLoadedBySdkModuleFirst()
        {
            // ARRANGE
            var configXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ApplicationInsights xmlns=""http://schemas.microsoft.com/ApplicationInsights/2013/Settings"">
  <InstrumentationKey>some ikey</InstrumentationKey>
  <TelemetryModules>
    <Add Type=""Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse.QuickPulseTelemetryModule, Microsoft.AI.PerfCounterCollector""/>
  </TelemetryModules>
  <TelemetryProcessors>
    <Add Type=""Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse.QuickPulseTelemetryProcessor, Microsoft.AI.PerfCounterCollector""/>
  </TelemetryProcessors>
</ApplicationInsights>";

            File.WriteAllText(this.configFilePath, configXml);

            // ACT
            var config = TelemetryConfiguration.Active;
            
            // ASSERT
            var module = TelemetryModules.Instance.Modules.OfType<QuickPulseTelemetryModule>().SingleOrDefault();
            Assert.IsNotNull(module);

            var telemetryProcessor = config.TelemetryProcessors.OfType<QuickPulseTelemetryProcessor>().SingleOrDefault();
            Assert.IsNotNull(telemetryProcessor);

            Assert.AreEqual(telemetryProcessor, QuickPulseTestHelper.GetTelemetryProcessors(module).SingleOrDefault());
        }

        [TestMethod]
        public void QuickPulseTelemetryModuleInteractsWithTelemetryProcessorCorrectlyWhenLoadedBySdkProcessorFirst()
        {
            // ARRANGE
            var configXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ApplicationInsights xmlns=""http://schemas.microsoft.com/ApplicationInsights/2013/Settings"">
  <InstrumentationKey>some ikey</InstrumentationKey>
  <TelemetryProcessors>
    <Add Type=""Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse.QuickPulseTelemetryProcessor, Microsoft.AI.PerfCounterCollector""/>
  </TelemetryProcessors>
  <TelemetryModules>
    <Add Type=""Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse.QuickPulseTelemetryModule, Microsoft.AI.PerfCounterCollector""/>
  </TelemetryModules>
</ApplicationInsights>";

            File.WriteAllText(this.configFilePath, configXml);

            // ACT
            var config = TelemetryConfiguration.Active;

            // ASSERT
            var module = TelemetryModules.Instance.Modules.OfType<QuickPulseTelemetryModule>().SingleOrDefault();
            Assert.IsNotNull(module);

            var telemetryProcessor = config.TelemetryProcessors.OfType<QuickPulseTelemetryProcessor>().SingleOrDefault();
            Assert.IsNotNull(telemetryProcessor);

            Assert.AreEqual(telemetryProcessor, QuickPulseTestHelper.GetTelemetryProcessors(module).SingleOrDefault());
        }

        [TestMethod]
        public void QuickPulseTelemetryModuleOnlyInitializesPerformanceCollectorAfterCollectionStarts()
        {
            // ARRANGE
            var interval = TimeSpan.FromMilliseconds(1);
            var timings = new QuickPulseTimings(interval, interval);
            var collectionTimeSlotManager = new QuickPulseCollectionTimeSlotManagerMock(timings);
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = false, ReturnValueFromSubmitSample = false };
            var performanceCollector = new PerformanceCollectorMock();
            var topCpuCollector = new QuickPulseTopCpuCollectorMock();

            var module = new QuickPulseTelemetryModule(collectionTimeSlotManager, null, serviceClient, performanceCollector, topCpuCollector, timings);

            // ACT & ASSERT
            module.Initialize(new TelemetryConfiguration() { InstrumentationKey = "some ikey" });
            
            Thread.Sleep((int)(interval.TotalMilliseconds * 100));
            
            Assert.IsFalse(performanceCollector.PerformanceCounters.Any());

            serviceClient.ReturnValueFromPing = true;
            
            Thread.Sleep((int)(interval.TotalMilliseconds * 100));

            Assert.IsTrue(performanceCollector.PerformanceCounters.Any());
            Assert.IsTrue(serviceClient.SnappedSamples.All(s => Math.Abs(s.PerfCountersLookup[@"\Processor(_Total)\% Processor Time"]) > double.Epsilon));
        }

        [TestMethod]
        public void QuickPulseTelemetryModuleTimestampsDataSamples()
        {
            // ARRANGE
            var interval = TimeSpan.FromMilliseconds(1);
            var timings = new QuickPulseTimings(interval, interval);
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = true, ReturnValueFromSubmitSample = true };
            var performanceCollector = new PerformanceCollectorMock();
            var topCpuCollector = new QuickPulseTopCpuCollectorMock();

            var module = new QuickPulseTelemetryModule(null, null, serviceClient, performanceCollector, topCpuCollector, timings);

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
        public void QuickPulseTelemetryModuleSupportsMultipleTelemetryProcessorsForMultipleConfigurations()
        {
            // ARRANGE
            var pollingInterval = TimeSpan.FromMilliseconds(1);
            var collectionInterval = TimeSpan.FromMilliseconds(1);
            var timings = new QuickPulseTimings(pollingInterval, collectionInterval);
            var collectionTimeSlotManager = new QuickPulseCollectionTimeSlotManagerMock(timings);
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = true, ReturnValueFromSubmitSample = true };
            var performanceCollector = new PerformanceCollectorMock();
            var topCpuCollector = new QuickPulseTopCpuCollectorMock();

            var module = new QuickPulseTelemetryModule(collectionTimeSlotManager, accumulatorManager, serviceClient, performanceCollector, topCpuCollector, timings);

            const int TelemetryProcessorCount = 4;

            var ikey = "some ikey";
            var config = new TelemetryConfiguration { InstrumentationKey = ikey };

            // spawn a bunch of configurations, each one having an individual instance of QuickPulseTelemetryProcessor
            var telemetryProcessors = new List<QuickPulseTelemetryProcessor>();
            for (int i = 0; i < TelemetryProcessorCount; i++)
            {
                var configuration = new TelemetryConfiguration();
                var builder = configuration.TelemetryProcessorChainBuilder;
                builder = builder.Use(current => new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy()));
                builder.Build();

                telemetryProcessors.Add(configuration.TelemetryProcessors.OfType<QuickPulseTelemetryProcessor>().Single());
            }

            // ACT
            foreach (var telemetryProcessor in telemetryProcessors)
            {
                module.RegisterTelemetryProcessor(telemetryProcessor);
            }

            module.Initialize(config);

            Thread.Sleep(TimeSpan.FromMilliseconds(100));

            // send data to each instance of QuickPulseTelemetryProcessor
            var request = new RequestTelemetry() { ResponseCode = "200", Success = true, Context = { InstrumentationKey = ikey } };
            telemetryProcessors[0].Process(request);

            request = new RequestTelemetry() { ResponseCode = "500", Success = false, Context = { InstrumentationKey = ikey } };
            telemetryProcessors[1].Process(request);

            var dependency = new DependencyTelemetry() { Success = true, Context = { InstrumentationKey = ikey } };
            telemetryProcessors[2].Process(dependency);

            dependency = new DependencyTelemetry() { Success = false, Context = { InstrumentationKey = ikey } };
            telemetryProcessors[3].Process(dependency);

            Thread.Sleep(TimeSpan.FromMilliseconds(100));

            // ASSERT
            // verify that every telemetry processor has contributed to the accumulator
            int samplesWithSomeRequests = serviceClient.SnappedSamples.Count(s => s.AIRequestsPerSecond > 0);
            int samplesWithSomeDependencies = serviceClient.SnappedSamples.Count(s => s.AIDependencyCallsPerSecond > 0);

            Assert.AreEqual(TelemetryProcessorCount, QuickPulseTestHelper.GetTelemetryProcessors(module).Count);
            Assert.IsTrue(samplesWithSomeRequests > 0 && samplesWithSomeRequests <= 2);
            Assert.AreEqual(1, serviceClient.SnappedSamples.Count(s => s.AIRequestsFailedPerSecond > 0));
            Assert.IsTrue(samplesWithSomeDependencies > 0 && samplesWithSomeDependencies < 2);
            Assert.AreEqual(1, serviceClient.SnappedSamples.Count(s => s.AIDependencyCallsFailedPerSecond > 0));
        }

        [TestMethod]
        public void QuickPulseTelemetryModuleSupportsMultipleTelemetryProcessorsForSingleConfiguration()
        {
            // ARRANGE
            var configXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ApplicationInsights xmlns=""http://schemas.microsoft.com/ApplicationInsights/2013/Settings"">
  <InstrumentationKey>some ikey</InstrumentationKey>
  <TelemetryModules>
    <Add Type=""Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse.QuickPulseTelemetryModule, Microsoft.AI.PerfCounterCollector""/>
  </TelemetryModules>
  <TelemetryProcessors>
    <Add Type=""Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse.QuickPulseTelemetryProcessor, Microsoft.AI.PerfCounterCollector""/>
  </TelemetryProcessors>
</ApplicationInsights>";

            File.WriteAllText(this.configFilePath, configXml);

            var config = TelemetryConfiguration.Active;
            
            // ACT
            var telemetryProcessors = new List<IQuickPulseTelemetryProcessor>();
            const int TelemetryProcessorCount = 4;
            for (int i = 0; i < TelemetryProcessorCount; i++)
            {
                // this recreates config.TelemetryProcessors collection, and all its members are reinstantiated
                var builder = config.TelemetryProcessorChainBuilder;
                builder = builder.Use(current => new SimpleTelemetryProcessorSpy());
                builder.Build();

                telemetryProcessors.Add(config.TelemetryProcessors.OfType<QuickPulseTelemetryProcessor>().Single());
            }

            // ASSERT
            var module = TelemetryModules.Instance.Modules.OfType<QuickPulseTelemetryModule>().SingleOrDefault();
            var registeredProcessors = QuickPulseTestHelper.GetTelemetryProcessors(module);

            Assert.AreEqual(TelemetryProcessorCount + 1, registeredProcessors.Count);  // one was there after the initial configuration loading
            Assert.IsTrue(telemetryProcessors.TrueForAll(registeredProcessors.Contains));
        }

        [TestMethod]
        public void QuickPulseTelemetryModuleManagesTimersCorrectly()
        {
            // ARRANGE
            var pollingInterval = TimeSpan.FromMilliseconds(200);
            var collectionInterval = TimeSpan.FromMilliseconds(80);
            var timings = new QuickPulseTimings(pollingInterval, collectionInterval);
            var collectionTimeSlotManager = new QuickPulseCollectionTimeSlotManagerMock(timings);
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = false, ReturnValueFromSubmitSample = true };
            var performanceCollector = new PerformanceCollectorMock();
            var topCpuCollector = new QuickPulseTopCpuCollectorMock();

            var module = new QuickPulseTelemetryModule(
                collectionTimeSlotManager,
                null,
                serviceClient,
                performanceCollector,
                topCpuCollector,
                timings);

            // ACT & ASSERT
            module.Initialize(new TelemetryConfiguration() { InstrumentationKey = "some ikey" });
            
            // initially, the module is in the polling state
            Thread.Sleep((int)(2.5 * pollingInterval.TotalMilliseconds));
            serviceClient.CountersEnabled = false;
            
            // 2.5 polling intervals have elapsed, we must have pinged the service 3 times (the first time immediately upon initialization), but no samples yet
            Assert.AreEqual(3, serviceClient.PingCount, "Ping count 1");
            Assert.AreEqual(0, serviceClient.SnappedSamples.Count, "Sample count 1");

            // now the service wants the data
            serviceClient.Reset();
            serviceClient.ReturnValueFromPing = true;
            serviceClient.ReturnValueFromSubmitSample = true;

            serviceClient.CountersEnabled = true;
            Thread.Sleep((int)(5 * collectionInterval.TotalMilliseconds));
            serviceClient.CountersEnabled = false;
            
            // a number of  collection intervals have elapsed, we must have pinged the service once, and then started sending samples
            Assert.AreEqual(1, serviceClient.PingCount, "Ping count 2");
            Assert.IsTrue(serviceClient.SnappedSamples.Count > 0, "Sample count 2");

            lock (serviceClient.ResponseLock)
            {
                // the service doesn't want the data anymore
                serviceClient.ReturnValueFromPing = false;
                serviceClient.ReturnValueFromSubmitSample = false;

                serviceClient.Reset();
                serviceClient.CountersEnabled = true;
            }
            
            Thread.Sleep((int)(2.9 * pollingInterval.TotalMilliseconds));
            serviceClient.CountersEnabled = false;
            
            // 2 polling intervals have elapsed, we must have submitted one batch of samples, stopped collecting and pinged the service twice afterwards
            Assert.AreEqual(1, serviceClient.SnappedSamples.Count / serviceClient.LastSampleBatchSize, "Sample count 3");
            Assert.AreEqual(2, serviceClient.PingCount, "Ping count 3");
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
            var topCpuCollector = new QuickPulseTopCpuCollectorMock();

            var module = new QuickPulseTelemetryModule(
                collectionTimeSlotManager,
                null,
                serviceClient,
                performanceCollector,
                topCpuCollector,
                timings);

            module.Initialize(new TelemetryConfiguration() { InstrumentationKey = "some ikey" });
            
            // ACT
            // below timeout should be sufficient for the module to get to the maximum storage capacity
            Thread.Sleep(TimeSpan.FromMilliseconds(300));

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
            var topCpuCollector = new QuickPulseTopCpuCollectorMock();

            var module = new QuickPulseTelemetryModule(
                collectionTimeSlotManager,
                null,
                serviceClient,
                performanceCollector,
                topCpuCollector,
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
            var topCpuCollector = new QuickPulseTopCpuCollectorMock();

            var module = new QuickPulseTelemetryModule(
                collectionTimeSlotManager,
                null,
                serviceClient,
                performanceCollector,
                topCpuCollector,
                timings);

            module.Initialize(new TelemetryConfiguration() { InstrumentationKey = "some ikey" });
            
            // ACT
            Thread.Sleep(TimeSpan.FromMilliseconds(100));

            // ASSERT
            module.Dispose();
        }

        [TestMethod]
        public void QuickPulseTelemetryModuleDoesNotLeakThreads()
        {
            // ARRANGE
            var interval = TimeSpan.FromMilliseconds(1);
            var timings = new QuickPulseTimings(interval, interval, interval, interval, interval, interval);
            var collectionTimeSlotManager = new QuickPulseCollectionTimeSlotManagerMock(timings);
            
            // this will flip-flop between collection and no collection, creating and ending a collection thread each time
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = true, ReturnValueFromSubmitSample = false };
            var performanceCollector = new PerformanceCollectorMock();
            var topCpuCollector = new QuickPulseTopCpuCollectorMock();

            var module = new QuickPulseTelemetryModule(
                collectionTimeSlotManager,
                null,
                serviceClient,
                performanceCollector,
                topCpuCollector,
                timings);

            module.Initialize(new TelemetryConfiguration() { InstrumentationKey = "some ikey" });

            int initialThreadCount = Process.GetCurrentProcess().Threads.Count;

            // ACT
            Thread.Sleep(TimeSpan.FromMilliseconds(300));
            
            // ASSERT
            // we don't expect to find many more threads, even though other components might be spinning new ones up and down
            var threadDelta = Process.GetCurrentProcess().Threads.Count - initialThreadCount;
            Assert.IsTrue(Math.Abs(threadDelta) < 5, threadDelta.ToString(CultureInfo.InvariantCulture));
        }
     }
}