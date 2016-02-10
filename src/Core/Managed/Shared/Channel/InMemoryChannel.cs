namespace Microsoft.ApplicationInsights.Channel
{
    using System;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    /// <summary>
    /// Represents a communication channel for sending telemetry to Application Insights via HTTPS. There will be a buffer that will not be persisted, to enforce the 
    /// queued telemetry items to be sent, <see cref="ITelemetryChannel.Flush"/> should be called.    
    /// </summary>
    public class InMemoryChannel : ITelemetryChannel
    {
        private readonly TelemetryBuffer buffer;
        private readonly InMemoryTransmitter transmitter;
        private bool? developerMode = false;
        private int bufferSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryChannel" /> class.
        /// </summary>
        public InMemoryChannel()
        {
            this.buffer = new TelemetryBuffer();
            this.transmitter = new InMemoryTransmitter(this.buffer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryChannel" /> class. Used in unit tests for constructor injection.  
        /// </summary>
        /// <param name="telemetryBuffer">The telemetry buffer that will be used to enqueue new events.</param>
        /// <param name="transmitter">The in memory transmitter that will send the events queued in the buffer.</param>
        internal InMemoryChannel(TelemetryBuffer telemetryBuffer, InMemoryTransmitter transmitter)
        {
            this.buffer = telemetryBuffer;
            this.transmitter = transmitter;
        }

        /// <summary>
        /// Gets or sets a value indicating whether developer mode of telemetry transmission is enabled.
        /// </summary>
        public bool? DeveloperMode
        {
            get
            {
                return this.developerMode;
            }

            set
            {
                if (value != this.developerMode)
                {
                    if (value.HasValue && value.Value)
                    {
                        this.bufferSize = this.buffer.Capacity;
                        this.buffer.Capacity = 1;
                    }
                    else
                    {
                        this.buffer.Capacity = this.bufferSize;
                    }

                    this.developerMode = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the sending interval. Once the interval expires, <see cref="InMemoryChannel"/> 
        /// serializes the accumulated telemetry items for transmission and sends it over the wire.
        /// </summary>    
        public TimeSpan SendingInterval
        {
            get
            {
                return this.transmitter.SendingInterval;
            }

            set
            {
                this.transmitter.SendingInterval = value;
            }
        }

        /// <summary>
        /// Gets or sets the HTTP address where the telemetry is sent.
        /// </summary>
        public string EndpointAddress
        {
            get { return this.transmitter.EndpointAddress.ToString(); }
            set { this.transmitter.EndpointAddress = new Uri(value); }
        }

        /// <summary>
        /// Gets or sets the maximum number of telemetry items will accumulate in a memory before 
        /// the <see cref="InMemoryChannel"/> serializing them for transmission to Application Insights.
        /// </summary>
        public int MaxTelemetryBufferCapacity
        {
            get { return this.buffer.Capacity; }
            set { this.buffer.Capacity = value; }
        }

        /// <summary>
        /// Sends an instance of ITelemetry through the channel.
        /// </summary>
        public void Send(ITelemetry item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            try
            {
                this.buffer.Enqueue(item);
            }
            catch (Exception e)
            {
                CoreEventSource.Log.LogVerbose("TelemetryBuffer.Enqueue failed: ", e.ToString());
            }
        }

        /// <summary>
        /// Will send all the telemetry items stored in the memory.
        /// </summary>
        public void Flush()
        {
            this.transmitter.Flush();
        }

        /// <summary>
        /// Disposing the channel.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.transmitter != null)
                {
                    this.transmitter.Dispose();
                }
            }
        }
    }
}
