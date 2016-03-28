namespace Unit.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
    using Microsoft.ApplicationInsights.Web.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class QuickPulseTelemetryProcessorTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            QuickPulseTestHelper.ClearEnvironment();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void QuickPulseTelemetryProcessorThrowsIfNextIsNull()
        {
            new QuickPulseTelemetryProcessor(null);
        }
        
        [TestMethod]
        public void QuickPulseTelemetryProcessorRegistersWithModule()
        {
            // ARRANGE
            var module = new QuickPulseTelemetryModule(null, null, null, null, null);

            TelemetryModules.Instance.Modules.Add(module);

            // ACT
            var spy = new SimpleTelemetryProcessorSpy();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(spy);
            telemetryProcessor.Initialize(new TelemetryConfiguration());

            // ASSERT
            Assert.AreEqual(telemetryProcessor, QuickPulseTestHelper.GetTelemetryProcessors(module).Single());
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorCallsNext()
        {
            // ARRANGE
            var spy = new SimpleTelemetryProcessorSpy();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(spy);

            // ACT
            telemetryProcessor.Process(new RequestTelemetry() { Context = { InstrumentationKey = "some ikey" } });

            // ASSERT
            Assert.AreEqual(1, spy.ReceivedCalls);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorKeepsAccurateCountOfRequests()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://microsoft.com"),
                new TelemetryConfiguration() { InstrumentationKey = "some ikey" });

            // ACT
            telemetryProcessor.Process(
                new RequestTelemetry()
                    {
                        Success = false,
                        ResponseCode = "200",
                        Duration = TimeSpan.FromSeconds(1),
                        Context = { InstrumentationKey = "some ikey" }
                    });
            telemetryProcessor.Process(
                new RequestTelemetry()
                    {
                        Success = true,
                        ResponseCode = "200",
                        Duration = TimeSpan.FromSeconds(2),
                        Context = { InstrumentationKey = "some ikey" }
                    });
            telemetryProcessor.Process(
                new RequestTelemetry()
                    {
                        Success = false,
                        ResponseCode = string.Empty,
                        Duration = TimeSpan.FromSeconds(3),
                        Context = { InstrumentationKey = "some ikey" }
                    });
            telemetryProcessor.Process(
                new RequestTelemetry()
                    {
                        Success = null,
                        ResponseCode = string.Empty,
                        Duration = TimeSpan.FromSeconds(4),
                        Context = { InstrumentationKey = "some ikey" }
                    });
            telemetryProcessor.Process(
                new RequestTelemetry()
                    {
                        Success = true,
                        ResponseCode = string.Empty,
                        Duration = TimeSpan.FromSeconds(5),
                        Context = { InstrumentationKey = "some ikey" }
                    });
            telemetryProcessor.Process(
                new RequestTelemetry()
                    {
                        Success = null,
                        ResponseCode = "404",
                        Duration = TimeSpan.FromSeconds(6),
                        Context = { InstrumentationKey = "some ikey" }
                    });

            // ASSERT
            Assert.AreEqual(6, accumulatorManager.CurrentDataAccumulator.AIRequestCount);
            Assert.AreEqual(
                1 + 2 + 3 + 4 + 5 + 6,
                TimeSpan.FromTicks(accumulatorManager.CurrentDataAccumulator.AIRequestDurationInTicks).TotalSeconds);
            Assert.AreEqual(4, accumulatorManager.CurrentDataAccumulator.AIRequestSuccessCount);
            Assert.AreEqual(2, accumulatorManager.CurrentDataAccumulator.AIRequestFailureCount);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorKeepsAccurateCountOfDependencies()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://microsoft.com"),
                new TelemetryConfiguration() { InstrumentationKey = "some ikey" });

            // ACT
            telemetryProcessor.Process(
                new DependencyTelemetry() { Success = true, Duration = TimeSpan.FromSeconds(1), Context = { InstrumentationKey = "some ikey" } });
            telemetryProcessor.Process(
                new DependencyTelemetry() { Success = true, Duration = TimeSpan.FromSeconds(1), Context = { InstrumentationKey = "some ikey" } });
            telemetryProcessor.Process(
                new DependencyTelemetry() { Success = false, Duration = TimeSpan.FromSeconds(2), Context = { InstrumentationKey = "some ikey" } });
            telemetryProcessor.Process(
                new DependencyTelemetry() { Success = null, Duration = TimeSpan.FromSeconds(3), Context = { InstrumentationKey = "some ikey" } });

            // ASSERT
            Assert.AreEqual(4, accumulatorManager.CurrentDataAccumulator.AIDependencyCallCount);
            Assert.AreEqual(1 + 1 + 2 + 3, TimeSpan.FromTicks(accumulatorManager.CurrentDataAccumulator.AIDependencyCallDurationInTicks).TotalSeconds);
            Assert.AreEqual(2, accumulatorManager.CurrentDataAccumulator.AIDependencyCallSuccessCount);
            Assert.AreEqual(1, accumulatorManager.CurrentDataAccumulator.AIDependencyCallFailureCount);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorKeepsAccurateCountOfExceptions()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://microsoft.com"),
                new TelemetryConfiguration() { InstrumentationKey = "some ikey" });

            // ACT
            telemetryProcessor.Process(new ExceptionTelemetry() { Context = { InstrumentationKey = "some ikey" } });
            telemetryProcessor.Process(new ExceptionTelemetry() { Context = { InstrumentationKey = "some ikey" } });
            telemetryProcessor.Process(new ExceptionTelemetry() { Context = { InstrumentationKey = "some ikey" } });

            // ASSERT
            Assert.AreEqual(3, accumulatorManager.CurrentDataAccumulator.AIExceptionCount);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorStopsCollection()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            var endpoint = new Uri("http://microsoft.com");
            var config = new TelemetryConfiguration() { InstrumentationKey = "some ikey" };
            
            // ACT
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(accumulatorManager, endpoint, config);
            telemetryProcessor.Process(new RequestTelemetry() { Context = { InstrumentationKey = "some ikey" } });
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StopCollection();
            telemetryProcessor.Process(new DependencyTelemetry() { Context = { InstrumentationKey = "some ikey" } });

            // ASSERT
            Assert.AreEqual(1, accumulatorManager.CurrentDataAccumulator.AIRequestCount);
            Assert.AreEqual(0, accumulatorManager.CurrentDataAccumulator.AIDependencyCallCount);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorIgnoresUnrelatedTelemetryItems()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://microsoft.com"),
                new TelemetryConfiguration() { InstrumentationKey = "some ikey" });

            // ACT
            telemetryProcessor.Process(new EventTelemetry() { Context = { InstrumentationKey = "some ikey" } });
            telemetryProcessor.Process(new ExceptionTelemetry() { Context = { InstrumentationKey = "some ikey" } });
            telemetryProcessor.Process(new MetricTelemetry() { Context = { InstrumentationKey = "some ikey" } });
            telemetryProcessor.Process(new PageViewTelemetry() { Context = { InstrumentationKey = "some ikey" } });
            telemetryProcessor.Process(new PerformanceCounterTelemetry() { Context = { InstrumentationKey = "some ikey" } });
            telemetryProcessor.Process(new SessionStateTelemetry() { Context = { InstrumentationKey = "some ikey" } });
            telemetryProcessor.Process(new TraceTelemetry() { Context = { InstrumentationKey = "some ikey" } });

            // ASSERT
            Assert.AreEqual(0, accumulatorManager.CurrentDataAccumulator.AIRequestCount);
            Assert.AreEqual(0, accumulatorManager.CurrentDataAccumulator.AIDependencyCallCount);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorIgnoresTelemetryItemsToDifferentInstrumentationKeys()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());

            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://microsoft.com"),
                new TelemetryConfiguration() { InstrumentationKey = "some ikey" });

            // ACT
            telemetryProcessor.Process(new RequestTelemetry() { Context = { InstrumentationKey = "some other ikey" } });
            telemetryProcessor.Process(new RequestTelemetry() { Context = { InstrumentationKey = "some ikey" } });

            // ASSERT
            Assert.AreEqual(1, accumulatorManager.CurrentDataAccumulator.AIRequestCount);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorHandlesMultipleThreadsCorrectly()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());

            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://microsoft.com"),
                new TelemetryConfiguration() { InstrumentationKey = "some ikey" });

            // expected data loss if threading is misimplemented is around 10% (established through experiment)
            int taskCount = 10000;
            var tasks = new List<Task>(taskCount);

            for (int i = 0; i < taskCount; i++)
            {
                var requestTelemetry = new RequestTelemetry()
                                           {
                                               ResponseCode = (i % 2 == 0) ? "200" : "500",
                                               Duration = TimeSpan.FromMilliseconds(i),
                                               Context = { InstrumentationKey = "some ikey" }
                                           };

                var task = new Task(() => telemetryProcessor.Process(requestTelemetry));
                tasks.Add(task);
            }

            // ACT
            tasks.ForEach(task => task.Start());

            Task.WaitAll(tasks.ToArray());

            // ASSERT
            Assert.AreEqual(taskCount, accumulatorManager.CurrentDataAccumulator.AIRequestCount);
            Assert.AreEqual(taskCount / 2, accumulatorManager.CurrentDataAccumulator.AIRequestSuccessCount);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorSwitchesBetweenMultipleAccumulatorManagers()
        {
            // ARRANGE
            var accumulatorManager1 = new QuickPulseDataAccumulatorManager();
            var accumulatorManager2 = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());

            // ACT
            var serviceEndpoint = new Uri("http://microsoft.com");
            var config = new TelemetryConfiguration() { InstrumentationKey = "some ikey" };
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager1,
                serviceEndpoint,
                config);
            telemetryProcessor.Process(new RequestTelemetry() { Context = { InstrumentationKey = "some ikey" } });
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StopCollection();

            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager2,
                serviceEndpoint,
                config);
            telemetryProcessor.Process(new DependencyTelemetry() { Context = { InstrumentationKey = "some ikey" } });
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StopCollection();

            // ASSERT
            Assert.AreEqual(1, accumulatorManager1.CurrentDataAccumulator.AIRequestCount);
            Assert.AreEqual(0, accumulatorManager1.CurrentDataAccumulator.AIDependencyCallCount);

            Assert.AreEqual(0, accumulatorManager2.CurrentDataAccumulator.AIRequestCount);
            Assert.AreEqual(1, accumulatorManager2.CurrentDataAccumulator.AIDependencyCallCount);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void QuickPulseTelemetryProcessorMustBeStoppedBeforeReceivingStartCommand()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());

            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://test.com"),
                new TelemetryConfiguration());

            // ACT
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://test.com"),
                new TelemetryConfiguration());

            // ASSERT
            // must throw
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorFiltersOutDependencyCallsToQuickPulseServiceDuringCollection()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var simpleTelemetryProcessorSpy = new SimpleTelemetryProcessorSpy();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(simpleTelemetryProcessorSpy);
            var config = new TelemetryConfiguration() { InstrumentationKey = "some ikey" };

            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("https://qps.cloudapp.net/endpoint.svc"),
                config);

            // ACT
            telemetryProcessor.Process(
                new DependencyTelemetry() { Name = "http://microsoft.ru", Context = { InstrumentationKey = config.InstrumentationKey } });
            telemetryProcessor.Process(
                new DependencyTelemetry() { Name = "http://qps.cloudapp.net/blabla", Context = { InstrumentationKey = config.InstrumentationKey } });
            telemetryProcessor.Process(
                new DependencyTelemetry() { Name = "https://bing.com", Context = { InstrumentationKey = config.InstrumentationKey } });

            // ASSERT
            Assert.AreEqual(2, simpleTelemetryProcessorSpy.ReceivedCalls);
            Assert.AreEqual("http://microsoft.ru", (simpleTelemetryProcessorSpy.ReceivedItems[0] as DependencyTelemetry).Name);
            Assert.AreEqual("https://bing.com", (simpleTelemetryProcessorSpy.ReceivedItems[1] as DependencyTelemetry).Name);
            Assert.AreEqual(2, accumulatorManager.CurrentDataAccumulator.AIDependencyCallCount);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorFiltersOutDependencyCallsToDefaultQuickPulseServiceEndpointInIdleMode()
        {
            // ARRANGE
            var simpleTelemetryProcessorSpy = new SimpleTelemetryProcessorSpy();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(simpleTelemetryProcessorSpy);
            
            // ACT
            telemetryProcessor.Process(new DependencyTelemetry() { Name = "http://microsoft.ru" });
            telemetryProcessor.Process(new DependencyTelemetry() { Name = "http://rt.services.visualstudio.com/blabla" });
            telemetryProcessor.Process(new DependencyTelemetry() { Name = "https://bing.com" });

            // ASSERT
            Assert.AreEqual(2, simpleTelemetryProcessorSpy.ReceivedCalls);
            Assert.AreEqual("http://microsoft.ru", (simpleTelemetryProcessorSpy.ReceivedItems[0] as DependencyTelemetry).Name);
            Assert.AreEqual("https://bing.com", (simpleTelemetryProcessorSpy.ReceivedItems[1] as DependencyTelemetry).Name);
        }
    }
}