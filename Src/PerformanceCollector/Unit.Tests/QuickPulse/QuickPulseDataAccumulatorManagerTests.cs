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
        [TestMethod]
        public void QuickPulseDataAccumulatorManagerLocksInSampleCorrectly()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            accumulatorManager.CurrentDataAccumulatorReference.AIRequestSuccessCount = 5;

            // ACT
            var completedSample = accumulatorManager.CompleteCurrentDataAccumulator();

            // ASSERT
            Assert.AreEqual(5, completedSample.AIRequestSuccessCount);
            Assert.AreEqual(0, accumulatorManager.CurrentDataAccumulatorReference.AIRequestSuccessCount);
            Assert.AreNotSame(completedSample, accumulatorManager.CurrentDataAccumulatorReference);
        }

        [TestMethod]
        public void QuickPulseDataAccumulatorManagerLocksInSampleCorrectlyMultithreaded()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            int taskCount = 1000;
            var writeTasks = new List<Task>(taskCount);

            for (int i = 0; i < taskCount; i++)
            {
                var task = new Task(
                    () =>
                        {
                            var encodedRequestCountAndDurationInTicks = QuickPulseDataAccumulator.EncodeCountAndDuration(1, 100);
                            Interlocked.Add(
                                ref accumulatorManager.CurrentDataAccumulatorReference.AIRequestCountAndDurationInTicks,
                                encodedRequestCountAndDurationInTicks);
                        });

                writeTasks.Add(task);
            }

            var completionTask = new Task(
                () =>
                    {
                        accumulatorManager.CompleteCurrentDataAccumulator();
                    });

            // shuffle the completion task into the middle of the pile to have it fire roughly halfway through
            writeTasks.Insert(writeTasks.Count / 2, completionTask);

            // ACT
            var sample1 = accumulatorManager.CurrentDataAccumulatorReference;

            var result = Parallel.For(
                0,
                writeTasks.Count,
                new ParallelOptions() { MaxDegreeOfParallelism = taskCount },
                i => writeTasks[i].RunSynchronously());

            while (!result.IsCompleted)
            {
            }

            var sample2 = accumulatorManager.CurrentDataAccumulatorReference;

            // ASSERT
            Assert.AreEqual(taskCount, sample1.AIRequestCount + sample2.AIRequestCount);
        }
    }
}
