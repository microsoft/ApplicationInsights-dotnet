namespace Microsoft.ApplicationInsights.Channel
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    /// <summary>
    /// Accumulates <see cref="ITelemetry"/> items for efficient transmission.
    /// </summary>
    internal class TelemetryBuffer
    {
        /// <summary>
        /// Delegate that is raised when the buffer is full.
        /// </summary>
        public Action OnFull;

        private const int DefaultCapacity = 500;
        private readonly object lockObj = new object();
        private int capacity = DefaultCapacity;
        private List<ITelemetry> items;
        private bool bufferFullMessageLogged = false;

        internal TelemetryBuffer()
        {
            this.items = new List<ITelemetry>();
        }

        /// <summary>
        /// Gets or sets the maximum number of telemetry items that can be buffered before transmission.
        /// </summary>        
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
                    this.capacity = DefaultCapacity;
                    return;
                }

                this.capacity = value;
            }
        }
        
        public void Enqueue(ITelemetry item)
        {
            if (item == null)
            {
                CoreEventSource.Log.LogVerbose("item is null in TelemetryBuffer.Enqueue");
                return;
            }

            lock (this.lockObj)
            {
                if (this.items.Count >= this.Capacity)
                {
                    if(!bufferFullMessageLogged)
                    {
                        CoreEventSource.Log.LogError("InMemory buffer has reached max capacity and items will be dropped until already buffered items are sent.");
                        this.bufferFullMessageLogged = true;
                    }
                    return;
                }
                this.items.Add(item);
                if (this.items.Count >= this.Capacity)
                {
                    var onFull = this.OnFull;
                    if (onFull != null)
                    {
                        onFull();
                    }
                }
            }
        }

        public IEnumerable<ITelemetry> Dequeue()
        {
            List<ITelemetry> telemetryToFlush = null;

            if (this.items.Count > 0)
            {
                lock (this.lockObj)
                {
                    if (this.items.Count > 0)
                    {
                        telemetryToFlush = this.items;
                        this.items = new List<ITelemetry>();
                        this.bufferFullMessageLogged = false;
                    }
                }
            }

            return telemetryToFlush;
        }
    }
}
