// <copyright file="InMemoryTransmitter.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// </copyright>

namespace Microsoft.ApplicationInsights.Channel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility.Implementation;
    using Extensibility.Implementation.Tracing;

    /// <summary>
    /// A transmitter that will immediately send telemetry over HTTP. 
    /// Telemetry items are being sent when Flush is called, or when the buffer is full (An OnFull "event" is raised) or every 30 seconds. 
    /// </summary>
    internal class InMemoryTransmitter : IDisposable
    {
        private readonly TelemetryBuffer buffer;

        /// <summary>
        /// A lock object to serialize the sending calls from Flush, OnFull event and the Runner.  
        /// </summary>
        private object sendingLockObj = new object();
        private AutoResetEvent startRunnerEvent;
        private bool enabled = true;
        
        /// <summary>
        /// The number of times this object was disposed.
        /// </summary>
        private int disposeCount = 0;

        private TimeSpan sendingInterval = TimeSpan.FromSeconds(30);
        private Uri endpointAddress = new Uri(Constants.TelemetryServiceEndpoint);
                
        internal InMemoryTransmitter(TelemetryBuffer buffer)
        {
            this.buffer = buffer;
            this.buffer.OnFull = this.OnBufferFull;

            // Starting the Runner
            Task.Factory.StartNew(this.Runner, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)
                .ContinueWith(
                    task => 
                    {
                        string msg = string.Format(CultureInfo.InvariantCulture, "InMemoryTransmitter: Unhandled exception in Runner: {0}", task.Exception);
                        CoreEventSource.Log.LogVerbose(msg);
                    }, 
                    TaskContinuationOptions.OnlyOnFaulted);
        }

        internal Uri EndpointAddress
        {
            get { return this.endpointAddress; }
            set { Property.Set(ref this.endpointAddress, value); }
        }

        internal TimeSpan SendingInterval
        {
            get { return this.sendingInterval; }
            set { this.sendingInterval = value; }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Flushes the in-memory buffer and sends it.
        /// </summary>
        internal void Flush()
        {
            this.DequeueAndSend();
        }

        /// <summary>
        /// Flushes the in-memory buffer and sends the telemetry items in <see cref="sendingInterval"/> intervals or when 
        /// <see cref="startRunnerEvent" /> is set.
        /// </summary>
        private void Runner()
        {
            using (this.startRunnerEvent = new AutoResetEvent(false))
            {
                while (this.enabled)
                {
                    // Pulling all items from the buffer and sending as one transmissiton.
                    this.DequeueAndSend();

                    // Waiting for the flush delay to elapse
                    this.startRunnerEvent.WaitOne(this.sendingInterval);
                }
            }
        }

        /// <summary>
        /// Happens when the in-memory buffer is full. Flushes the in-memory buffer and sends the telemetry items.
        /// </summary>
        private void OnBufferFull()
        {
            this.startRunnerEvent.Set();
        }

        /// <summary>
        /// Flushes the in-memory buffer and send it.
        /// </summary>
        private void DequeueAndSend()
        {
            lock (this.sendingLockObj)
            {
                IEnumerable<ITelemetry> telemetryItems = this.buffer.Dequeue();
                try
                {
                    // send request
                    this.Send(telemetryItems).ConfigureAwait(false).GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    CoreEventSource.Log.LogVerbose("DequeueAndSend: Failed Sending: Exception: " + e.ToString());
                }
            }
        }

        /// <summary>
        /// Serializes a list of telemetry items and sends them.
        /// </summary>
        private async Task Send(IEnumerable<ITelemetry> telemetryItems)
        {
            if (telemetryItems == null || !telemetryItems.Any())
            {
                CoreEventSource.Log.LogVerbose("No Telemetry Items passed to Enqueue");
                return;
            }

            byte[] data = JsonSerializer.Serialize(telemetryItems);
            var transmission = new Transmission(this.endpointAddress, data, "application/x-json-stream", JsonSerializer.CompressionType);

            await transmission.SendAsync().ConfigureAwait(false);
        }

        private void Dispose(bool disposing)
        {
            if (Interlocked.Increment(ref this.disposeCount) == 1)
            {
                // Stops the runner.
                this.enabled = false;

                if (this.startRunnerEvent != null)
                {
                    // call Set to to prevent waiting for the next interval. 
                    this.startRunnerEvent.Set();
                }
            }
        }
    }
}
