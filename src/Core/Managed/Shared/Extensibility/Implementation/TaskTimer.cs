// <copyright file="TaskTimer.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// </copyright>

namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

#if CORE_PCL || NET45 || NET46
    using TaskEx = System.Threading.Tasks.Task;
#endif

    /// <summary>
    /// Runs a task after a certain delay and log any error.
    /// </summary>
    public class TaskTimer
    {
        private class DelayedWork
        {
            public DelayedWork(Func<Task> runFunc) { this.RunFunc = runFunc; this.IsCancellationRequested = false; }
            public Func<Task> RunFunc { get; private set; }
            public bool IsCancellationRequested { get; set; }
        }

        /// <summary>
        /// Represents an infinite time span.
        /// </summary>
        public static readonly TimeSpan InfiniteTimeSpan = new TimeSpan(0, 0, 0, 0, Timeout.Infinite);

        private TimeSpan delay = TimeSpan.FromMinutes(1);
        private DelayedWork latestWork = null;

        /// <summary>
        /// Gets or sets the delay before the task starts. 
        /// </summary>
        public TimeSpan Delay 
        { 
            get
            {
                return this.delay;
            }

            set
            {
                if ((value <= TimeSpan.Zero || value.TotalMilliseconds > int.MaxValue) && value != InfiniteTimeSpan)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                this.delay = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether value that indicates if a task has already started.
        /// </summary>
        public bool IsStarted
        {
            get
            {
                return (this.latestWork != null);
            }
        }

        /// <summary>
        /// Start the task.
        /// </summary>
        /// <param name="elapsed">The task to run.</param>
        public void Start(Func<Task> elapsed)
        {
            DelayedWork delayedWork = new DelayedWork(elapsed);
            DelayedWork previousWork = Interlocked.Exchange(ref this.latestWork, delayedWork);

            if (previousWork != null)
            {
                previousWork.IsCancellationRequested = true;
            }

#if !NET40
            Task delayTask = Task.Delay(this.Delay);
#else
            Task delayTask = TaskEx.Delay(this.Delay);
#endif
            delayTask.ContinueWith(
                    (dlyTask) =>
                    {
                        Interlocked.CompareExchange(ref this.latestWork, null, delayedWork);
                        if (delayedWork.IsCancellationRequested)
                        {
                            return;
                        }

                        try
                        {
                            // Task may (or may not) be executed syncronously
                            Task task = (delayedWork.RunFunc == null)
                                                ? null
                                                : delayedWork.RunFunc();

                            // Just in case we check for null if someone returned null
                            if (task == null)
                            {
                                return;
                            }

                            task.ContinueWith(
                                    (userTask) =>
                                    {
                                        try
                                        {
                                            if (task.IsFaulted)
                                            {
                                                throw new AggregateException(task.Exception).Flatten();
                                            }
                                        }
                                        catch (Exception exception)
                                        {
                                            LogException(exception);
                                        }
                                    },
                                    CancellationToken.None,
                                    TaskContinuationOptions.ExecuteSynchronously,
                                    TaskScheduler.Default
                                );
                        }
                        catch (Exception exception)
                        {
                            LogException(exception);
                        }
                    },
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
        }

        /// <summary>
        /// Cancels the current task.
        /// </summary>
        public void Cancel()
        {
            DelayedWork currentWork = this.latestWork;
            if (currentWork != null)
            {
                currentWork.IsCancellationRequested = true;
            }
        }


        /// <summary>
        /// Log exception thrown by outer code.
        /// </summary>
        /// <param name="exception">Exception to log.</param>
        private static void LogException(Exception exception)
        {
            var aggregateException = exception as AggregateException;
            if (aggregateException != null)
            {
                aggregateException = aggregateException.Flatten();
                foreach (Exception e in aggregateException.InnerExceptions)
                {
                    CoreEventSource.Log.LogError(e.ToInvariantString());
                }
            }

            CoreEventSource.Log.LogError(exception.ToInvariantString());
        }
    }
}
