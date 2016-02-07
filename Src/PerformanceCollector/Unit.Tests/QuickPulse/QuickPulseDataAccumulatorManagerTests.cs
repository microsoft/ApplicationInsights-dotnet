namespace Unit.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    
    [TestClass]
    public class QuickPulseDataAccumulatorManagerTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            QuickPulseDataAccumulatorManager.ResetInstance();
        }

        [TestMethod]
        public void QuickPulseDataHubLocksInSampleCorrectly()
        {
            // ARRANGE
            QuickPulseDataAccumulatorManager.Instance.CurrentDataAccumulatorReference.AIRequestCount = 5;

            // ACT
            var completedSample = QuickPulseDataAccumulatorManager.Instance.CompleteCurrentDataAccumulator();

            // ASSERT
            Assert.AreEqual(5, completedSample.AIRequestCount);
            Assert.AreEqual(0, QuickPulseDataAccumulatorManager.Instance.CurrentDataAccumulatorReference.AIRequestCount);

            Assert.AreSame(completedSample, QuickPulseDataAccumulatorManager.Instance.CompletedDataAccumulator);
            Assert.AreNotSame(completedSample, QuickPulseDataAccumulatorManager.Instance.CurrentDataAccumulatorReference);

            Assert.AreNotSame(QuickPulseDataAccumulatorManager.Instance.CompletedDataAccumulator, QuickPulseDataAccumulatorManager.Instance.CurrentDataAccumulatorReference);
        }

        [TestMethod]
        public void QuickPulseDataHubLocksInSampleCorrectlyMultithreaded()
        {
            // ARRANGE
            int taskCount = 1000;
            var writeTasks = new List<Task>(taskCount);
            var pause = TimeSpan.FromMilliseconds(10);

            for (int i = 0; i < taskCount; i++)
            {
                var task = new Task(() =>
                {
                    Interlocked.Increment(ref QuickPulseDataAccumulatorManager.Instance.CurrentDataAccumulatorReference.AIRequestCount);

                    // sleep to increase the probability of sample completion happening right now
                    Thread.Sleep(pause);

                    Interlocked.Increment(ref QuickPulseDataAccumulatorManager.Instance.CurrentDataAccumulatorReference.AIDependencyCallCount);
                });

                writeTasks.Add(task);
            }

            var completionTask = new Task(() =>
            {
                // sleep to increase the probability of more write tasks being between the two writes
                Thread.Sleep(TimeSpan.FromTicks(pause.Ticks / 2));

                QuickPulseDataAccumulatorManager.Instance.CompleteCurrentDataAccumulator();
            });

            // shuffle the completion task into the middle of the pile to have it fire roughly halfway through
            writeTasks.Insert(writeTasks.Count / 2, completionTask);

            // ACT
            var sample1 = QuickPulseDataAccumulatorManager.Instance.CurrentDataAccumulatorReference;

            var result = Parallel.For(0, writeTasks.Count, new ParallelOptions() { MaxDegreeOfParallelism = taskCount }, i => writeTasks[i].RunSynchronously());

            while (!result.IsCompleted)
            {
            }

            var sample2 = QuickPulseDataAccumulatorManager.Instance.CurrentDataAccumulatorReference;

            // ASSERT
            // we expect some "telemetry items" to get "sprayed" over the two neighboring samples
            Assert.IsTrue(sample1.AIRequestCount > sample1.AIDependencyCallCount);
            Assert.IsTrue(sample2.AIRequestCount < sample2.AIDependencyCallCount);

            // overall numbers should match exactly
            Assert.AreEqual(taskCount, sample1.AIRequestCount + sample2.AIRequestCount);
            Assert.AreEqual(taskCount, sample1.AIDependencyCallCount + sample2.AIDependencyCallCount);
        }
    }
}
