namespace Unit.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
    using Microsoft.ApplicationInsights.Web.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class QuickPulseTelemetryProcessorTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void QuickPulseTelemetryProcessorThrowsIfNextIsNull()
        {
            new QuickPulseTelemetryProcessor(null);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorCallsNext()
        {
            // ARRANGE
            var spy = new SimpleTelemetryProcessorSpy();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(spy);
            
            // ACT
            telemetryProcessor.Process(new RequestTelemetry());
            
            // ASSERT
            Assert.AreEqual(1, spy.ReceivedCalls);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorKeepsAccurateCountOfRequests()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            telemetryProcessor.StartCollection(accumulatorManager);

            // ACT
            telemetryProcessor.Process(new RequestTelemetry() { Success = true, ResponseCode = "200", Duration = TimeSpan.FromSeconds(1) });
            telemetryProcessor.Process(new RequestTelemetry() { Success = true, ResponseCode = "200", Duration = TimeSpan.FromSeconds(2) });
            telemetryProcessor.Process(new RequestTelemetry() { Success = false, ResponseCode = string.Empty, Duration = TimeSpan.FromSeconds(3) });
            telemetryProcessor.Process(new RequestTelemetry() { Success = null, ResponseCode = string.Empty, Duration = TimeSpan.FromSeconds(4) });
            telemetryProcessor.Process(new RequestTelemetry() { Success = true, ResponseCode = string.Empty, Duration = TimeSpan.FromSeconds(5) });
            telemetryProcessor.Process(new RequestTelemetry() { Success = null, ResponseCode = "404", Duration = TimeSpan.FromSeconds(6) });

            // ASSERT
            Assert.AreEqual(6, accumulatorManager.CurrentDataAccumulator.AIRequestCount);
            Assert.AreEqual(1 + 2 + 3 + 4 + 5 + 6, TimeSpan.FromTicks(accumulatorManager.CurrentDataAccumulator.AIRequestDurationInTicks).TotalSeconds);
            Assert.AreEqual(5, accumulatorManager.CurrentDataAccumulator.AIRequestSuccessCount);
            Assert.AreEqual(1, accumulatorManager.CurrentDataAccumulator.AIRequestFailureCount);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorKeepsAccurateCountOfDependencies()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            telemetryProcessor.StartCollection(accumulatorManager);

            // ACT
            telemetryProcessor.Process(new DependencyTelemetry() { Success = true, Duration = TimeSpan.FromSeconds(1) });
            telemetryProcessor.Process(new DependencyTelemetry() { Success = true, Duration = TimeSpan.FromSeconds(1) });
            telemetryProcessor.Process(new DependencyTelemetry() { Success = false, Duration = TimeSpan.FromSeconds(2) });
            telemetryProcessor.Process(new DependencyTelemetry() { Success = null, Duration = TimeSpan.FromSeconds(3) });

            // ASSERT
            Assert.AreEqual(4, accumulatorManager.CurrentDataAccumulator.AIDependencyCallCount);
            Assert.AreEqual(
                1 + 1 + 2 + 3,
                TimeSpan.FromTicks(accumulatorManager.CurrentDataAccumulator.AIDependencyCallDurationInTicks).TotalSeconds);
            Assert.AreEqual(2, accumulatorManager.CurrentDataAccumulator.AIDependencyCallSuccessCount);
            Assert.AreEqual(1, accumulatorManager.CurrentDataAccumulator.AIDependencyCallFailureCount);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorKeepsAccurateCountOfExceptions()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            telemetryProcessor.StartCollection(accumulatorManager);

            // ACT
            telemetryProcessor.Process(new ExceptionTelemetry());
            telemetryProcessor.Process(new ExceptionTelemetry());
            telemetryProcessor.Process(new ExceptionTelemetry());

            // ASSERT
            Assert.AreEqual(3, accumulatorManager.CurrentDataAccumulator.AIExceptionCount);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorStopsCollection()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());

            // ACT
            telemetryProcessor.StartCollection(accumulatorManager);
            telemetryProcessor.Process(new RequestTelemetry());
            telemetryProcessor.StopCollection();
            telemetryProcessor.Process(new DependencyTelemetry());

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
            telemetryProcessor.StartCollection(accumulatorManager);

            // ACT
            telemetryProcessor.Process(new EventTelemetry());
            telemetryProcessor.Process(new ExceptionTelemetry());
            telemetryProcessor.Process(new MetricTelemetry());
            telemetryProcessor.Process(new PageViewTelemetry());
            telemetryProcessor.Process(new PerformanceCounterTelemetry());
            telemetryProcessor.Process(new SessionStateTelemetry());
            telemetryProcessor.Process(new TraceTelemetry());

            // ASSERT
            Assert.AreEqual(0, accumulatorManager.CurrentDataAccumulator.AIRequestCount);
            Assert.AreEqual(0, accumulatorManager.CurrentDataAccumulator.AIDependencyCallCount);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorHandlesMultipleThreadsCorrectly()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            telemetryProcessor.StartCollection(accumulatorManager);

            // expected data loss if threading is misimplemented is around 10% (established through experiment)
            int taskCount = 10000;
            var tasks = new List<Task>(taskCount);

            for (int i = 0; i < taskCount; i++)
            {
                var requestTelemetry = new RequestTelemetry() { ResponseCode = (i % 2 == 0) ? "200" : "500", Duration = TimeSpan.FromMilliseconds(i) };

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
            telemetryProcessor.StartCollection(accumulatorManager1);
            telemetryProcessor.Process(new RequestTelemetry());
            telemetryProcessor.StopCollection();

            telemetryProcessor.StartCollection(accumulatorManager2);
            telemetryProcessor.Process(new DependencyTelemetry());
            telemetryProcessor.StopCollection();

            // ASSERT
            Assert.AreEqual(1, accumulatorManager1.CurrentDataAccumulator.AIRequestCount);
            Assert.AreEqual(0, accumulatorManager1.CurrentDataAccumulator.AIDependencyCallCount);

            Assert.AreEqual(0, accumulatorManager2.CurrentDataAccumulator.AIRequestCount);
            Assert.AreEqual(1, accumulatorManager2.CurrentDataAccumulator.AIDependencyCallCount);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void QuickPulseTelemetryProcessorDoesHasToBeStoppedBeforeReceingStartCommand()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());

            telemetryProcessor.StartCollection(accumulatorManager);

            // ACT
            telemetryProcessor.StartCollection(accumulatorManager);

            // ASSERT
            // must throw
        }
    }
}