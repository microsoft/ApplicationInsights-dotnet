namespace Microsoft.ApplicationInsights.Shared.Extensibility.Implementation
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    /// <summary>
    /// Asynchronously invokes a handler for every posted item.
    /// </summary>
    /// <typeparam name="T">Specifies the type of data processed by the instance.</typeparam>
    internal class AsyncCall<T>
    {
        public static readonly string AsyncCallTaskTag = nameof(AsyncCall<T>);

        /// <summary>
        /// A queue that stores the posted data.  Also serves as the synchronization object for protected instance state.
        /// </summary>
        private readonly ConcurrentQueue<T> itemQueue;

        /// <summary>The delegate to invoke for every element.</summary>
        private readonly Action<T> handler;

        /// <summary>The TaskFactory to use to launch new tasks.</summary>
        private readonly TaskFactory taskFactory;

        /// <summary>The number of item processing tasks.</summary>
        private int activeTaskCount;

        /// <summary>
        /// The item processing task that was just launched.
        /// </summary>
        /// <remarks>
        /// We hold a reference to a recently launched item processing task for a period determined by delayedTaskStartAction.
        /// This serves as a flag preventing us from spinning up new tasks too quickly.
        /// </remarks>
        private Task recentlyLaunchedTask;

        /// <summary>
        /// Current item queue length.
        /// </summary>
        private int queueLength;

        /// <summary>
        /// The task that represents completion of the <see cref="AsyncCall{T}" /> instance.
        /// </summary>
        private Task completionTask;

        /// <summary>
        /// Execution options for the <see cref="AsyncCall{T}" /> instance.
        /// </summary>
        private AsyncCallOptions options;

        /// <summary>Initializes a new instance of the <see cref="AsyncCall{T}" /> class.</summary>
        /// <param name="handler">The action to run for every posted item.</param>
        /// <param name="options">Execution options for the new <see cref="AsyncCall{T}" /> instance.</param>        
        public AsyncCall(
            Action<T> handler,
            AsyncCallOptions options = null)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            this.handler = handler;
            this.options = options ?? new AsyncCallOptions();
            this.itemQueue = new ConcurrentQueue<T>();
            this.taskFactory = new TaskFactory(this.options.TaskScheduler);
            this.queueLength = 0;
            this.completionTask = null;
        }

        /// <summary>
        /// Returns true if a new item processing task should be scheduled.
        /// </summary>
        /// <remarks>
        /// This property is only checked when new items are posted to the call (which might require new item processing task).
        /// New task should be scheduled if:
        /// 1. Completion has not started AND
        ///     2a. There are no active tasks OR
        ///     2b. There are some tasks running but fewer than <see cref="AsyncCallOptions.MaxDegreeOfParallelism"/> 
        ///     and no tasks have been recently launched (see <see cref="AsyncCallOptions.DelayTaskStartAction"/> for details).
        /// </remarks>
        private bool ShouldScheduleNewTask => this.completionTask == null 
            && (this.activeTaskCount == 0 || (this.recentlyLaunchedTask == null && this.activeTaskCount < this.options.MaxDegreeOfParallelism));

        /// <summary>Post an item for processing.</summary>
        /// <param name="item">The item to be processed.</param>
        /// <returns>True if the item was successfully posted, otherwise false.</returns>
        public bool Post(T item)
        {
            if (this.completionTask != null)
            {
                // Completion was requested and the AsyncCall does not accept any new items
                return false;
            }

            if (this.queueLength < this.options.MaxItemQueueLength)
            {
                this.itemQueue.Enqueue(item);
                Interlocked.Increment(ref this.queueLength);
            }
            else
            {
                CoreEventSource.Log.TelemetryDroppedToPreventQueueOverflow();
                return false;
            }

            // Check to see whether we have any item processing tasks scheduled.
            if (this.ShouldScheduleNewTask)
            {
                lock (this.itemQueue)
                {
                    if (this.ShouldScheduleNewTask)
                    {
                        this.activeTaskCount++;
                        this.recentlyLaunchedTask = this.taskFactory.StartNew(this.ProcessItemsActionTaskBody, AsyncCallTaskTag, TaskCreationOptions.PreferFairness);
                        this.options.DelayTaskStartAction(() => this.OnTaskStartDelayPassed());
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Instructs the <see cref="AsyncCall{T}"/> instance to accept no more items and drain the item queue.
        /// </summary>
        /// <returns>A task that is completed when the item queue is drained.</returns>
        public Task CompleteAsync()
        {
            lock (this.itemQueue)
            {
                if (this.completionTask == null)
                {
                    this.completionTask = this.taskFactory.StartNew(() =>
                    {
                        do
                        {
                            lock (this.itemQueue)
                            {
                                if (this.queueLength > 0)
                                {
                                    Monitor.Wait(this.itemQueue);
                                }
                            }
                        }
                        while (this.queueLength > 0);
                    });
                }
            }

            return this.completionTask;
        }

        private void OnTaskStartDelayPassed()
        {
            lock (this.itemQueue)
            {
                this.recentlyLaunchedTask = null;
            }
        }

        /// <summary>Gets an enumerable that yields the items to be processed at this time.</summary>
        /// <returns>An enumerable of items to process.</returns>
        private IEnumerable<T> GetItemsToProcess()
        {
            // Yield the next elements to be processed until either there are no more elements
            // or we've reached the maximum number of elements that an individual task should process.
            int processedCount = 0;
            T nextItem;
            while (processedCount < this.options.MaxItemsPerTask && this.itemQueue.TryDequeue(out nextItem))
            {
                Interlocked.Decrement(ref this.queueLength);
                yield return nextItem;
                processedCount++;
            }
        }

        /// <summary>
        /// Used as the body of an action task to process items in the queue.
        /// </summary>
        /// <param name="ignoredState">The state for the task (ignored in our implementation).</param>
        private void ProcessItemsActionTaskBody(object ignoredState)
        {
            try
            {
                // Process up to maxItemsPerTask items. 
                foreach (var item in this.GetItemsToProcess())
                {
                    this.handler(item);
                }
            }
            finally
            {
                lock (this.itemQueue)
                {
                    // If there are still items in the queue, schedule another task to continue processing.
                    // Otherwise, note that we're no longer processing.
                    // This periodic recycling of tasks ensures that other tasks in the process are not starved.
                    if (!this.itemQueue.IsEmpty)
                    {
                        this.taskFactory.StartNew(this.ProcessItemsActionTaskBody, AsyncCallTaskTag, TaskCreationOptions.PreferFairness);
                    }
                    else
                    {
                        this.activeTaskCount--;
                    }

                    if (this.completionTask != null)
                    {
                        // Notify the completion operation that the queue length has changed.
                        Monitor.Pulse(this.itemQueue);
                    }
                }
            }
        }
    }
}
