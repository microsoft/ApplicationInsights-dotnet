namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
#if !NET40
    using TaskEx = System.Threading.Tasks.Task;
#endif

    /// <summary>
    /// Accumulates <see cref="ITelemetry"/> items for efficient transmission.
    /// </summary>
    internal class TelemetryBuffer : IEnumerable<ITelemetry>, ITelemetryProcessor, IDisposable
    {
        private static readonly TimeSpan DefaultFlushDelay = TimeSpan.FromSeconds(30);

        private readonly TaskTimerInternal flushTimer;
        private readonly TelemetrySerializer serializer;

        private int capacity = 500;
        private int backlogSize = 1000000;
        private int minimumBacklogSize = 1001;
        private bool itemDroppedMessageLogged = false;
        private List<ITelemetry> transmissionBuffer;

        public TelemetryBuffer(TelemetrySerializer serializer, IApplicationLifecycle applicationLifecycle)
            : this()
        {
            if (serializer == null)
            {
                throw new ArgumentNullException("serializer");
            }

            if (applicationLifecycle == null)
            {
                throw new ArgumentNullException("applicationLifecycle");
            }

            this.serializer = serializer;

            applicationLifecycle.Stopping += this.HandleApplicationStoppingEvent;
        }

        protected TelemetryBuffer()
        {
            this.flushTimer = new TaskTimerInternal { Delay = DefaultFlushDelay };
            this.transmissionBuffer = new List<ITelemetry>(this.Capacity);
        }

        /// <summary>
        /// Gets or sets the maximum number of telemetry items that can be buffered before transmission.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The value is zero or less.</exception>
        /// <exception cref="ArgumentException">The value is greater than the MaximumBacklogSize.</exception>
        public int Capacity
        {
            get
            {
                return this.capacity;
            }

            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                if (value > this.backlogSize)
                {
                    throw new ArgumentException("Capacity cannot be greater than MaximumBacklogSize", "Capacity");
                }

                this.capacity = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of telemetry items that can be in the backlog to send. Items will be dropped
        /// once this limit is hit.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The value is zero or less.</exception>
        /// <exception cref="ArgumentException">The value is less than the Capacity.</exception>
        public int BacklogSize
        {
            get
            {
                return this.backlogSize;
            }

            set
            {
                if (value < this.minimumBacklogSize)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                if (value < this.capacity)
                {
                    throw new ArgumentException("MaximumBacklogSize cannot be lower than capacity", "MaximumBacklogSize");
                }

                this.backlogSize = value;
            }
        }

        public TimeSpan MaxTransmissionDelay 
        {
            get { return this.flushTimer.Delay; }
            set { this.flushTimer.Delay = value; }
        }

        /// <summary>
        /// Releases resources used by this <see cref="TelemetryBuffer"/> instance.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Processes the specified <paramref name="item"/> item.
        /// </summary>
        /// <exception cref="ArgumentNullException">The <paramref name="item"/> is null.</exception>
        public virtual void Process(ITelemetry item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (!this.flushTimer.IsStarted)
            {
                this.flushTimer.Start(this.FlushAsync);
            }

            lock (this)
            {
                if (this.transmissionBuffer.Count >= this.BacklogSize)
                {
                    if (!this.itemDroppedMessageLogged)
                    {
                        TelemetryChannelEventSource.Log.ItemDroppedAsMaximumUnsentBacklogSizeReached(this.BacklogSize);
                        this.itemDroppedMessageLogged = true;
                    }

                    return;
                }

                this.transmissionBuffer.Add(item);
                if (this.transmissionBuffer.Count >= this.Capacity)
                {
                    ExceptionHandler.Start(this.FlushAsync);
                }
            }
        }

        /// <summary>
        /// Passes all <see cref="ITelemetry"/> items to the <see cref="TelemetrySerializer"/> and empties the queue.
        /// </summary>
        public virtual async Task FlushAsync()
        {
            List<ITelemetry> telemetryToFlush = null;
            if (this.transmissionBuffer.Count > 0)
            {
                lock (this)
                {
                    if (this.transmissionBuffer.Count > 0)
                    {
                        this.flushTimer.Cancel();
                        telemetryToFlush = this.transmissionBuffer;
                        this.transmissionBuffer = new List<ITelemetry>(this.Capacity);
                        this.itemDroppedMessageLogged = false;
                    }
                }
            }

            if (telemetryToFlush != null)
            {
                TelemetryChannelEventSource.Log.SerializationStarted(telemetryToFlush.Count);

                // Flush on thread pull to offload the rest of the channel logic from the customer's thread.
                // This also works around the problem in ASP.NET 4.0, does not support await and SynchronizationContext correctly.
                // See also: http://www.bing.com/search?q=UseTaskFriendlySynchronizationContext
                await TaskEx.Run(() => this.serializer.Serialize(telemetryToFlush)).ConfigureAwait(false);
            }
        }

        public IEnumerator<ITelemetry> GetEnumerator()
        {
            return this.transmissionBuffer.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private void HandleApplicationStoppingEvent(object sender, ApplicationStoppingEventArgs e)
        {
            e.Run(this.FlushAsync);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.flushTimer.Dispose();
            }
        }
    }
}
