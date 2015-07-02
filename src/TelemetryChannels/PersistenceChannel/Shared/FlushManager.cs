// <copyright file="FlushManager.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// </copyright>

namespace Microsoft.ApplicationInsights.Channel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    /// <summary>
    /// This class handles all the logic for flushing the In Memory buffer to the persistent storage. 
    /// </summary>
    internal class FlushManager : IDisposable
    {
        /// <summary>
        /// The memory buffer. 
        /// </summary>
        private readonly TelemetryBuffer telemetryBuffer;

        /// <summary>
        /// A wait handle that signals when a flush is required. 
        /// </summary>
        private AutoResetEvent flushWaitHandle;

        /// <summary>
        /// The storage that is used to persist all the transmissions. 
        /// </summary>
        private StorageBase storage;

        /// <summary>
        /// The number of times this object was disposed.
        /// </summary>
        private int disposeCount = 0;

        /// <summary>
        /// A boolean value that determines if the long running thread that runs flush in a loop will stay alive. 
        /// </summary>
        private bool flushLoopEnabled = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="FlushManager"/> class.
        /// </summary>        
        /// <param name="storage">The storage that persists the telemetries.</param>
        /// <param name="telemetryBuffer">In memory buffer that holds telemetries.</param>
        /// <param name="supportAutoFlush">A boolean value that determines if flush will happen automatically. Used by unit tests.</param>
        internal FlushManager(StorageBase storage, TelemetryBuffer telemetryBuffer, bool supportAutoFlush = true)
        {
            this.storage = storage;
            this.telemetryBuffer = telemetryBuffer;
            this.telemetryBuffer.OnFull = this.OnTelemetryBufferFull;
            this.FlushDelay = TimeSpan.FromSeconds(30);

            if (supportAutoFlush)
            {
                Task.Factory.StartNew(this.FlushLoop, TaskCreationOptions.LongRunning)
                    .ContinueWith(t => CoreEventSource.Log.LogVerbose("FlushManager: Failure in FlushLoop: Exception: " + t.Exception.ToString()), TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        /// <summary>
        /// Gets or sets the maximum telemetry batching interval. Once the interval expires, <see cref="PersistenceTransmitter"/> 
        /// persists the accumulated telemetry items.
        /// </summary>
        internal TimeSpan FlushDelay { get; set; }

        /// <summary>
        /// Gets or sets the service endpoint. 
        /// </summary>
        /// <remarks>
        /// Q: Why flushManager knows about the endpoint? 
        /// A: Storage stores <see cref="Transmission"/> objects and Transmission objects contain the endpoint address.
        /// </remarks>
        internal Uri EndpointAddress { get; set; }

        /// <summary>
        /// Disposes the object.
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Increment(ref this.disposeCount) == 1)
            {
                if (this.flushWaitHandle != null)
                {
                    this.flushLoopEnabled = false;
                    this.flushWaitHandle.Set();
                }
            }
        }

        /// <summary>
        /// Persist the in-memory telemetry items.
        /// </summary>
        internal void Flush()
        {
            IEnumerable<ITelemetry> telemetryItems = this.telemetryBuffer.Dequeue();

            if (telemetryItems != null && telemetryItems.Any())
            {
                byte[] data = JsonSerializer.Serialize(telemetryItems);
                var transmission = new Transmission(
                    this.EndpointAddress,
                    data,
                    "application/x-json-stream",
                    JsonSerializer.CompressionType);

                this.storage.EnqueueAsync(transmission).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// Flushes in intervals set by <see cref="FlushDelay"/>.
        /// </summary>
        private void FlushLoop()
        {
            using (this.flushWaitHandle = new AutoResetEvent(false))
            {
                while (this.flushLoopEnabled)
                {
                    // Pulling all items from the buffer and sending as one transmissiton
                    this.Flush();

                    // Waiting for the flush delay to elapse
                    this.flushWaitHandle.WaitOne(this.FlushDelay);
                }
            }
        }

        /// <summary>
        /// Handles the full event coming from the TelemetryBuffer.
        /// </summary>
        private void OnTelemetryBufferFull()
        {
            if (this.flushWaitHandle != null && this.flushLoopEnabled)
            {
                this.flushWaitHandle.Set();
            }
        }
    }
}
