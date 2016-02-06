namespace Unit.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class QuickPulseTelemetryInitializerTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            QuickPulseDataHub.ResetInstance();
        }

        [TestMethod]
        public void QuickPulseTelemetryInitializerKeepsAccurateCountRequests()
        {
            // ARRANGE
            var telemetryInitializer = new QuickPulseTelemetryInitializer(QuickPulseDataHub.Instance);
            telemetryInitializer.Enabled = true;

            // ACT
            telemetryInitializer.Initialize(new RequestTelemetry() { Success = true, Duration = TimeSpan.FromSeconds(1) });
            telemetryInitializer.Initialize(new RequestTelemetry() { Success = true, Duration = TimeSpan.FromSeconds(1) });
            telemetryInitializer.Initialize(new RequestTelemetry() { Success = false, Duration = TimeSpan.FromSeconds(2) });
            telemetryInitializer.Initialize(new RequestTelemetry() { Success = null, Duration = TimeSpan.FromSeconds(3) });

            // ASSERT
            Assert.AreEqual(4, QuickPulseDataHub.Instance.CurrentDataSampleReference.AIRequestCount);
            Assert.AreEqual(1 + 1 + 2 + 3, TimeSpan.FromTicks(QuickPulseDataHub.Instance.CurrentDataSampleReference.AIRequestDurationTicks).TotalSeconds);
            Assert.AreEqual(2, QuickPulseDataHub.Instance.CurrentDataSampleReference.AIRequestSuccessCount);
            Assert.AreEqual(1, QuickPulseDataHub.Instance.CurrentDataSampleReference.AIRequestFailureCount);
        }

        [TestMethod]
        public void QuickPulseTelemetryInitializerKeepsAccurateCountDependencies()
        {
            // ARRANGE
            var telemetryInitializer = new QuickPulseTelemetryInitializer(QuickPulseDataHub.Instance);
            telemetryInitializer.Enabled = true;

            // ACT
            telemetryInitializer.Initialize(new DependencyTelemetry() { Success = true, Duration = TimeSpan.FromSeconds(1) });
            telemetryInitializer.Initialize(new DependencyTelemetry() { Success = true, Duration = TimeSpan.FromSeconds(1) });
            telemetryInitializer.Initialize(new DependencyTelemetry() { Success = false, Duration = TimeSpan.FromSeconds(2) });
            telemetryInitializer.Initialize(new DependencyTelemetry() { Success = null, Duration = TimeSpan.FromSeconds(3) });

            // ASSERT
            Assert.AreEqual(4, QuickPulseDataHub.Instance.CurrentDataSampleReference.AIDependencyCallCount);
            Assert.AreEqual(1 + 1 + 2 + 3, TimeSpan.FromTicks(QuickPulseDataHub.Instance.CurrentDataSampleReference.AIDependencyCallDurationTicks).TotalSeconds);
            Assert.AreEqual(2, QuickPulseDataHub.Instance.CurrentDataSampleReference.AIDependencyCallSuccessCount);
            Assert.AreEqual(1, QuickPulseDataHub.Instance.CurrentDataSampleReference.AIDependencyCallFailureCount);
        }

        [TestMethod]
        public void QuickPulseTelemetryInitializerIsDisabledByDefault()
        {
            // ARRANGE
            var telemetryInitializer = new QuickPulseTelemetryInitializer(QuickPulseDataHub.Instance);
            
            // ACT
            telemetryInitializer.Initialize(new RequestTelemetry());

            // ASSERT
            Assert.AreEqual(0, QuickPulseDataHub.Instance.CurrentDataSampleReference.AIRequestCount);
        }

        [TestMethod]
        public void QuickPulseTelemetryInitializerIgnoresUnrelatedTelemetryItems()
        {
            // ARRANGE
            var telemetryInitializer = new QuickPulseTelemetryInitializer(QuickPulseDataHub.Instance);

            // ACT
            telemetryInitializer.Initialize(new EventTelemetry());
            telemetryInitializer.Initialize(new ExceptionTelemetry());
            telemetryInitializer.Initialize(new MetricTelemetry());
            telemetryInitializer.Initialize(new PageViewTelemetry());
            telemetryInitializer.Initialize(new PerformanceCounterTelemetry());
            telemetryInitializer.Initialize(new SessionStateTelemetry());
            telemetryInitializer.Initialize(new TraceTelemetry());

            // ASSERT
            Assert.AreEqual(0, QuickPulseDataHub.Instance.CurrentDataSampleReference.AIRequestCount);
            Assert.AreEqual(0, QuickPulseDataHub.Instance.CurrentDataSampleReference.AIDependencyCallCount);
        }

        [TestMethod]
        public void QuickPulseTelemetryInitializerHandlesMultipleThreadsCorrectly()
        {
            // ARRANGE
            var telemetryInitializer = new QuickPulseTelemetryInitializer(QuickPulseDataHub.Instance);
            telemetryInitializer.Enabled = true;

            // expected data loss if threading is misimplemented is around 10% (established through experiment)
            int taskCount = 10000;
            var tasks = new List<Task>(taskCount);

            for (int i = 0; i < taskCount; i++)
            {
                var requestTelemetry = new RequestTelemetry() { Success = i % 2 == 0, Duration = TimeSpan.FromMilliseconds(i) };

                var task = new Task(() => telemetryInitializer.Initialize(requestTelemetry));
                tasks.Add(task);
            }

            // ACT
            tasks.ForEach(task => task.Start());

            Task.WaitAll(tasks.ToArray());

            // ASSERT
            Assert.AreEqual(taskCount, QuickPulseDataHub.Instance.CurrentDataSampleReference.AIRequestCount);
            Assert.AreEqual(taskCount / 2, QuickPulseDataHub.Instance.CurrentDataSampleReference.AIRequestSuccessCount);
        }
    }
}
