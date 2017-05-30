namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
#if NET40
    using Microsoft.Diagnostics.Tracing;
#else
    using System.Diagnostics.Tracing;
#endif

    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Shared.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.TestFramework;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;

    [TestClass]
    public class AsyncCallTests
    {
        [TestMethod]
        public void WillProcessSingleItem()
        {
            var processedItems = new ConcurrentQueue<string>();
            var taskScheduler = new DeterministicTaskScheduler();

            AsyncCall<string> ac = new AsyncCall<string>(item => processedItems.Enqueue(item), new AsyncCallOptions
            {
                MaxDegreeOfParallelism = 2,                
                TaskScheduler = taskScheduler,
                DelayTaskStartAction = (action) => action()
            });

            ac.Post("Record1");

            taskScheduler.RunTasksUntilIdle();

            Assert.True(processedItems.TryDequeue(out string record));
            Assert.Equal("Record1", record);
        }

        [TestMethod]
        public void WillProcessItemsSerially()
        {
            const int ItemCount = 8;

            var taskScheduler = new DeterministicTaskScheduler();
            int processedItemCount = 0;

            AsyncCall<string> ac = new AsyncCall<string>(item => { processedItemCount++; }, new AsyncCallOptions
            {
                MaxDegreeOfParallelism = 1,
                MaxItemsPerTask = 2,
                TaskScheduler = taskScheduler,
                DelayTaskStartAction = (action) => action()
            });

            for (int i = 0; i < ItemCount; i++)
            {
                ac.Post(i.ToString());
            }

            taskScheduler.RunTasksUntilIdle();

            Assert.Equal(ItemCount, processedItemCount);
        }

        [TestMethod]
        public void WillEndAllTasksIfNoDataAvailable()
        {
            const int ItemCount = 8;
            const int Parallelism = 2;
            const int ItemsPerTask = 2;

            var taskScheduler = new DeterministicTaskScheduler();
            int processedItemCount = 0;

            AsyncCall<string> ac = new AsyncCall<string>(item => { processedItemCount++; }, new AsyncCallOptions
            {
                MaxDegreeOfParallelism = Parallelism,
                MaxItemsPerTask = ItemsPerTask,
                TaskScheduler = taskScheduler,
                DelayTaskStartAction = (action) => action()
            });

            for (int i = 0; i < ItemCount; i++)
            {
                ac.Post(i.ToString());
            }

            // Two tasks should be scheduled (max degree of parallelism)
            Assert.Equal(2, taskScheduler.ScheduledTasks.Count(t => IsAsyncCallTask(t)));

            taskScheduler.RunPendingTasks();

            // Should be left with 4 items and two tasks
            Assert.Equal(2, taskScheduler.ScheduledTasks.Count(t => IsAsyncCallTask(t)));

            taskScheduler.RunPendingTasks();

            // Now there should be just one task--the second task should have seen that the item queue was empty and just exit
            Assert.Equal(1, taskScheduler.ScheduledTasks.Count(t => IsAsyncCallTask(t)));
            // All items should be processed at this point
            Assert.Equal(ItemCount, processedItemCount);

            taskScheduler.RunPendingTasks();

            // Now there should be no more tasks left
            Assert.Equal(0, taskScheduler.ScheduledTasks.Count(t => IsAsyncCallTask(t)));

            // There might be leftover, cancelled tasks from parallel execution, clear them.
            taskScheduler.RunTasksUntilIdle();
        }

        [TestMethod]
        public void WillDelayStartingNewTasks()
        {
            const int Parallelism = 4;
            const int ItemsPerTask = 2;

            var taskScheduler = new DeterministicTaskScheduler();
            int processedItemCount = 0;
            Action taskStartAction = null;

            AsyncCall<string> ac = new AsyncCall<string>(item => { processedItemCount++; }, new AsyncCallOptions
            {
                MaxDegreeOfParallelism = Parallelism,
                MaxItemsPerTask = ItemsPerTask,
                TaskScheduler = taskScheduler,
                DelayTaskStartAction = (action) => taskStartAction = action
            });

            // Posting 3 items causes the AsyncCall to attempt to start 3 tasks (below max parallelism).
            // But because delayTaskStartAction has not be executed, only one task will actually be scheduled.
            ac.Post("1");
            ac.Post("2");
            ac.Post("3");
            Assert.Equal(1, taskScheduler.ScheduledTasks.Count(t => IsAsyncCallTask(t)));

            taskScheduler.RunPendingTasks();
            // After on execution cycle 1 item should be left, so there should be still one task scheduled.
            Assert.Equal(1, taskScheduler.ScheduledTasks.Count(t => IsAsyncCallTask(t)));

            taskScheduler.RunPendingTasks();
            // The queue should be drained by now and no tasks should be scheduled.
            Assert.Equal(0, taskScheduler.ScheduledTasks.Count(t => IsAsyncCallTask(t)));
            Assert.Equal(3, processedItemCount);

            ac.Post("4");
            ac.Post("5");
            // Posting new items should schedule 1 task (because running tasks count was zero)
            Assert.Equal(1, taskScheduler.ScheduledTasks.Count(t => IsAsyncCallTask(t)));

            // After executing the task start action and adding some more items 1 additional task should be scheduled.
            taskStartAction();
            ac.Post("6");
            ac.Post("7");
            Assert.Equal(2, taskScheduler.ScheduledTasks.Count(t => IsAsyncCallTask(t)));

            taskScheduler.RunPendingTasks();
            // Two tasks should be enough to process all items the queue (2 items per task).
            Assert.Equal(7, processedItemCount);

            // Clean up leftover tasks.
            taskScheduler.RunTasksUntilIdle();
        }

        [TestMethod]
        public void WillSpawnMaxConcurrentTasks()
        {
            const int Parallelism = 4;
            const int ItemsPerTask = 2;
            const int ItemCount = 32;

            var taskScheduler = new DeterministicTaskScheduler();
            int processedItemCount = 0;

            AsyncCall<string> ac = new AsyncCall<string>(item => { processedItemCount++; }, new AsyncCallOptions
            {
                MaxDegreeOfParallelism =Parallelism,
                MaxItemsPerTask = ItemsPerTask,
                TaskScheduler = taskScheduler,
                DelayTaskStartAction = (action) => action()
            });

            for (int i = 0; i < ItemCount; i++)
            {
                ac.Post(i.ToString());
            }

            // AsyncCall should have spawned 4 tasks (max degree of parallelism)
            Assert.Equal(Parallelism, taskScheduler.ScheduledTasks.Count(t => IsAsyncCallTask(t)));

            taskScheduler.RunTasksUntilIdle();
            Assert.Equal(ItemCount, processedItemCount);
        }

        [TestMethod]
        public void WillWarnAndShedLoadIfInflowTooHigh()
        {
            const int Parallelism = 2;
            const int ItemsPerTask = 2;
            const int ItemCount = 32;
            const int MaxQueueLength = 10;

            var taskScheduler = new DeterministicTaskScheduler();
            var processedItems = new List<int>(ItemCount);

            AsyncCall<int> ac = new AsyncCall<int>(item => { processedItems.Add(item); }, new AsyncCallOptions
            {
                MaxDegreeOfParallelism = Parallelism,
                MaxItemsPerTask = ItemsPerTask,
                TaskScheduler = taskScheduler,
                DelayTaskStartAction = (action) => action(),
                MaxItemQueueLength = MaxQueueLength
            });

            using (var listener = new TestEventListener())
            {
                const long AllKeywords = -1;
                listener.EnableEvents(CoreEventSource.Log, EventLevel.Warning, (EventKeywords)AllKeywords);

                for (int i = 0; i < ItemCount; i++)
                {
                    ac.Post(i);
                }
                taskScheduler.RunTasksUntilIdle();

                // Because of max queue length we should only have processed 10 items, the rest should have been shed.
                Assert.Equal(MaxQueueLength, processedItems.Count);

                var traces = listener.Messages.ToList();
                Assert.Equal(ItemCount - MaxQueueLength, traces.Count);
#if !NET45
                // EventName is not available in .NET 4.5. We'll just skip this check for this framework.
                Assert.True(traces.All(t => t.EventName.Equals(nameof(CoreEventSource.TelemetryDroppedToPreventQueueOverflow))));
#endif
            }
        }

        [TestMethod]
        public void WillProcessItemsAgainIfQueueDropsBelowMax()
        {
            const int Parallelism = 2;
            const int ItemsPerTask = 2;
            const int MaxQueueLength = 10;

            var taskScheduler = new DeterministicTaskScheduler();
            var processedItems = new List<int>();

            AsyncCall<int> ac = new AsyncCall<int>(item => { processedItems.Add(item); }, new AsyncCallOptions
            {
                MaxDegreeOfParallelism = Parallelism,
                MaxItemsPerTask = ItemsPerTask,
                TaskScheduler = taskScheduler,
                DelayTaskStartAction = (action) => action(),
                MaxItemQueueLength = MaxQueueLength
            });

            for (int i = 0; i < MaxQueueLength; i++)
            {
                ac.Post(i);
            }

            // Queue is now full. The following item should be rejected.
            ac.Post(MaxQueueLength);
            taskScheduler.RunPendingTasks();
            Assert.Equal(Parallelism * ItemsPerTask, processedItems.Count);

            // Now there should be space for a few more items and they should be processed eventually.
            ac.Post(MaxQueueLength + 1);
            ac.Post(MaxQueueLength + 2);
            taskScheduler.RunTasksUntilIdle();
            Assert.Equal(MaxQueueLength + 2, processedItems.Count);
            // Verify the last processed item.
            Assert.Equal(MaxQueueLength + 2, processedItems[processedItems.Count - 1]);
        }

        [TestMethod]
        public void WillNotAcceptItemsAfterCompletionStarted()
        {
            const int Parallelism = 2;
            const int ItemsPerTask = 2;
            const int MaxQueueLength = 10;

            var taskScheduler = new DeterministicTaskScheduler();
            var processedItems = new List<int>();

            AsyncCall<int> ac = new AsyncCall<int>(item => { processedItems.Add(item); }, new AsyncCallOptions
            {
                MaxDegreeOfParallelism = Parallelism,
                MaxItemsPerTask = ItemsPerTask,
                TaskScheduler = taskScheduler,
                DelayTaskStartAction = (action) => action(),
                MaxItemQueueLength = MaxQueueLength
            });

            Assert.True(ac.Post(1));
            Task completionTask = ac.CompleteAsync();
            Assert.False(ac.Post(2));
            taskScheduler.RunTasksUntilIdle();
            Assert.Equal(1, processedItems.Count);
            Assert.True(completionTask.Status == TaskStatus.RanToCompletion);
        }

        [TestMethod]
        public void CompletionTaskIsSingleton()
        {
            const int Parallelism = 2;
            const int ItemsPerTask = 2;
            const int MaxQueueLength = 10;

            var taskScheduler = new DeterministicTaskScheduler();

            AsyncCall<int> ac = new AsyncCall<int>((item) => { }, new AsyncCallOptions
            {
                MaxDegreeOfParallelism = Parallelism,
                MaxItemsPerTask = ItemsPerTask,
                TaskScheduler = taskScheduler,
                DelayTaskStartAction = (action) => action(),
                MaxItemQueueLength = MaxQueueLength
            });

            Task first = ac.CompleteAsync();
            Task second = ac.CompleteAsync();
            Assert.Same(first, second);            
            taskScheduler.RunTasksUntilIdle();
            Assert.True(first.Status == TaskStatus.RanToCompletion);
        }

        private bool IsAsyncCallTask(Task t)
        {
            return AsyncCall<string>.AsyncCallTaskTag.Equals(t.AsyncState);
        }
    }
}
