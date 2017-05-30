namespace Microsoft.ApplicationInsights.Shared.Extensibility.Implementation
{
    using System;
    using System.Threading.Tasks;

    internal class AsyncCallOptions
    {
        private const int DefaultMaximumItemQueueLength = 5000;
        private const int DefaultTaskStartDelayMsec = 100;

        private int maxDegreeOfParallelism;
        private int maxItemsPerTask;
        private TaskScheduler taskScheduler;
        private int maxItemQueueLength;

        public AsyncCallOptions()
        {
            this.maxDegreeOfParallelism = Environment.ProcessorCount;
            this.maxItemsPerTask = int.MaxValue;
            this.DelayTaskStartAction = this.DefaultDelayAction;
            this.TaskScheduler = TaskScheduler.Default;
            this.maxItemQueueLength = DefaultMaximumItemQueueLength;
        }

        /// <summary>
        /// Gets or sets the maximum number of concurrent item processing tasks.
        /// </summary>
        public int MaxDegreeOfParallelism
        {
            get => this.maxDegreeOfParallelism;
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                this.maxDegreeOfParallelism = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of items that should be processed by an individual task.
        /// </summary>
        public int MaxItemsPerTask
        {
            get => this.maxItemsPerTask;
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                this.maxItemsPerTask = value;
            }
        }

        /// <summary>
        /// Gets or sets the TaskScheduler to use. If null, the default task scheduler is used.
        /// </summary>
        public TaskScheduler TaskScheduler
        {
            get => this.taskScheduler;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                this.taskScheduler = value;
            }
        }

        /// <summary>
        /// Gets or sets a delay action delegate, used to prevent starting new item processing tasks too quickly.
        /// </summary>
        /// <remarks>Takes an action delegate that gets executed when the delay is over. Defaults to 100 milliseconds delay.</remarks>
        public Action<Action> DelayTaskStartAction { get; set; }

        /// <summary>
        /// Gets or sets maximum item queue length.
        /// </summary>
        /// <remarks>To avoid locking we allow the maximum queue length to be exceeded slightly.</remarks>
        public int MaxItemQueueLength
        {
            get => this.maxItemQueueLength;
            set
            {
                if (value < 1)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                this.maxItemQueueLength = value;
            }
        }

        private void DefaultDelayAction(Action action)
        {
#if NET40
            TaskEx.Delay(TimeSpan.FromMilliseconds(DefaultTaskStartDelayMsec)).ContinueWith((t) => action());
#else
            Task.Delay(DefaultTaskStartDelayMsec).ContinueWith((t) => action());
#endif
        }
    }
}
