namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    internal static class TaskEx
    {
        /// <summary>
        /// Check and rethrow exception for failed task.
        /// </summary>
        /// <param name="task">Task to check.</param>
        public static void RethrowIfFaulted(this Task task)
        {
            if (!task.IsCompleted)
            {
                throw new ArgumentException("Task is not yet completed");
            }

            if (task.IsFaulted)
            {
                throw new AggregateException(task.Exception).Flatten();
            }
        }

        /// <summary>
        /// Creates a task that completes after a specified time interval.
        /// </summary>
        /// <param name="timeout">The time span to wait before completing the returned task.</param>
        /// <returns>A Task that represents the time delay.</returns>
        public static Task Delay(TimeSpan timeout)
        {
            return Delay(timeout, CancellationToken.None);
        }

        /// <summary>
        /// Creates a task that completes after a specified time interval.
        /// </summary>
        /// <param name="timeout">The time span to wait before completing the returned task.</param>
        /// <param name="token">The cancellation token that will interrupt delay.</param>
        /// <returns>A Task that represents the time delay.</returns>
        public static Task Delay(TimeSpan timeout, CancellationToken token)
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

            if (timeout.Ticks <= 0)
            {
                tcs.SetResult(null);
                return tcs.Task;
            }

            Timer timer = null;
            timer = new Timer(
                state =>
                    {
                        timer.Dispose();
                        tcs.TrySetResult(null);
                    }, 
                null, 
                timeout, 
                TimeSpan.FromMilliseconds(Timeout.Infinite));

            token.Register(
                () =>
                    {
                        timer.Dispose();
                        tcs.TrySetCanceled();
                    });

            return tcs.Task;
        }

        /// <summary>
        /// Creates a Task that's completed successfully with the specified result.
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by the task.</typeparam>
        /// <param name="result">The result to store into the completed task.</param>
        /// <returns>The successfully completed task.</returns>
        public static Task<TResult> FromResult<TResult>(TResult result)
        {
            TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>();
            tcs.SetResult(result);
            return tcs.Task;
        }

        /// <summary>
        /// Creates a task that will complete when any of the supplied tasks have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait on for completion.</param>
        /// <returns>A task that represents the completion of one of the supplied tasks. The return Task's Result is the task that completed.</returns>
        public static Task<Task> WhenAny(params Task[] tasks)
        {
            if (tasks.Length == 0)
            {
                throw new ArgumentException("The tasks argument contains no tasks");
            }

            TaskCompletionSource<Task> taskCompletionSource = new TaskCompletionSource<Task>();

            Task.Factory.ContinueWhenAny(tasks, completedTask => taskCompletionSource.SetResult(completedTask), TaskContinuationOptions.ExecuteSynchronously);

            return taskCompletionSource.Task;
        }
    }
}
