// <copyright file="TaskTimer.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// </copyright>

namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

#if WINRT || CORE_PCL || NET45 || NET46 || UWP
    using TaskEx = System.Threading.Tasks.Task;
#endif

    /// <summary>
    /// Runs a task after a certain delay and log any error.
    /// </summary>
    public class TaskTimer : IDisposable
    {
        /// <summary>
        /// Represents an infinite time span.
        /// </summary>
        public static readonly TimeSpan InfiniteTimeSpan = new TimeSpan(0, 0, 0, 0, Timeout.Infinite);

        private TimeSpan delay = TimeSpan.FromMinutes(1);
        private CancellationTokenSource tokenSource;

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
            get { return this.tokenSource != null; }
        }

        /// <summary>
        /// Start the task.
        /// </summary>
        /// <param name="elapsed">The task to run.</param>
        public void Start(Func<Task> elapsed)
        {
            var newTokenSource = new CancellationTokenSource();

            TaskEx.Delay(this.Delay, newTokenSource.Token)
                .ContinueWith(
                    async previousTask => 
                    {
                        CancelAndDispose(Interlocked.CompareExchange(ref this.tokenSource, null, newTokenSource));
                        try
                        {
                            await elapsed(); 
                        }
                        catch (Exception exception)
                        {
                            if (exception is AggregateException)
                            {
                                foreach (Exception e in ((AggregateException)exception).InnerExceptions)
                                {
                                    CoreEventSource.Log.LogError(e.ToString());
                                }
                            }

                            CoreEventSource.Log.LogError(exception.ToString());
                        }
                    },
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);

            CancelAndDispose(Interlocked.Exchange(ref this.tokenSource, newTokenSource));
        }

        /// <summary>
        /// Cancels the current task.
        /// </summary>
        public void Cancel()
        {
            CancelAndDispose(Interlocked.Exchange(ref this.tokenSource, null));
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private static void CancelAndDispose(CancellationTokenSource tokenSource)
        {
            if (tokenSource != null)
            {
                tokenSource.Cancel();
                tokenSource.Dispose();
            }
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.Cancel();
            }
        }
    }
}
