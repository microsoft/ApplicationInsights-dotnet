namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation;
    
#if NET45
    using TaskEx = System.Threading.Tasks.Task;
#endif

    /// <summary>
    /// Represents a communication channel for sending telemetry to Application Insights via HTTP/S.
    /// </summary>
    public sealed class ServerTelemetryChannel : ITelemetryChannel, ITelemetryModule
    {
        internal readonly TelemetrySerializer TelemetrySerializer;
        internal Implementation.TelemetryBuffer TelemetryBuffer;
        internal Transmitter Transmitter;

        private bool? developerMode;
        private int telemetryBufferCapacity;
        private ITelemetryProcessor telemetryProcessor;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ServerTelemetryChannel"/> class.
        /// </summary>
        public ServerTelemetryChannel() : this(new Network(), new WebApplicationLifecycle())
        {
        }
        
        internal ServerTelemetryChannel(INetwork network, IApplicationLifecycle applicationLifecycle)
        {
            var policies = new TransmissionPolicy[] 
            { 
                new ApplicationLifecycleTransmissionPolicy(applicationLifecycle),
                new ErrorHandlingTransmissionPolicy(),
                new NetworkAvailabilityTransmissionPolicy(network),
                new ThrottlingTransmissionPolicy()
            };

            this.Transmitter = new Transmitter(policies: policies);

            this.TelemetrySerializer = new TelemetrySerializer(this.Transmitter);
            this.TelemetryBuffer = new Implementation.TelemetryBuffer(this.TelemetrySerializer, applicationLifecycle);
            this.telemetryBufferCapacity = this.TelemetryBuffer.Capacity;

            this.TelemetryProcessor = this.TelemetryBuffer;
        }

        /// <summary>
        /// Gets or sets a value indicating whether developer mode of telemetry transmission is enabled.
        /// When developer mode is True, <see cref="TelemetryChannel"/> sends telemetry to Application Insights immediately 
        /// during the entire lifetime of the application. When developer mode is False, <see cref="TelemetryChannel"/>
        /// respects production sending policies defined by other properties.
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
                        this.telemetryBufferCapacity = this.TelemetryBuffer.Capacity;
                        this.TelemetryBuffer.Capacity = 1;
                    }
                    else
                    {
                        this.TelemetryBuffer.Capacity = this.telemetryBufferCapacity;
                    }

                    this.developerMode = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the HTTP address where the telemetry is sent.
        /// </summary>
        public string EndpointAddress
        {
            get { return this.TelemetrySerializer.EndpointAddress.ToString(); }
            set { this.TelemetrySerializer.EndpointAddress = new Uri(value); }
        }

        /// <summary>
        /// Gets or sets the maximum telemetry batching interval. Once the interval expires, <see cref="TelemetryChannel"/> 
        /// serializes the accumulated telemetry items for transmission.
        /// </summary>
        public TimeSpan MaxTelemetryBufferDelay 
        {
            get { return this.TelemetryBuffer.MaxTransmissionDelay; }
            set { this.TelemetryBuffer.MaxTransmissionDelay = value; }
        }

        /// <summary>
        /// Gets or sets the maximum number of telemetry items will accumulate in a memory before 
        /// the <see cref="TelemetryChannel"/> serializing them for transmission to Application Insights.
        /// </summary>
        public int MaxTelemetryBufferCapacity 
        {
            get { return this.TelemetryBuffer.Capacity; }
            set { this.TelemetryBuffer.Capacity = value; } 
        }

        /// <summary>
        /// Gets or sets the maximum amount of memory, in bytes, that <see cref="TelemetryChannel"/> will use 
        /// to buffer transmissions before sending them to Application Insights.
        /// </summary>
        public int MaxTransmissionBufferCapacity
        {
            get { return this.Transmitter.MaxBufferCapacity; }
            set { this.Transmitter.MaxBufferCapacity = value; }
        }

        /// <summary>
        /// Gets or sets the maximum number of telemetry transmissions that <see cref="TelemetryChannel"/> will 
        /// send to Application Insights at the same time.
        /// </summary>
        public int MaxTransmissionSenderCapacity
        {
            get { return this.Transmitter.MaxSenderCapacity; }
            set { this.Transmitter.MaxSenderCapacity = value; }
        }

        /// <summary>
        /// Gets or sets the maximum amount of disk space, in bytes, that <see cref="TelemetryChannel"/> will 
        /// use to store unsent telemetry transmissions.
        /// </summary>
        public long MaxTransmissionStorageCapacity
        {
            get { return this.Transmitter.MaxStorageCapacity; }
            set { this.Transmitter.MaxStorageCapacity = value; }
        }

        /// <summary>
        /// Gets or sets first TelemetryProcessor in processor call chain.
        /// </summary>
        internal ITelemetryProcessor TelemetryProcessor
        {
            get
            {
                return this.telemetryProcessor;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                this.telemetryProcessor = value;
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public void Dispose()
        {   
            // Tested by FxCop rule CA2213
            this.TelemetryBuffer.Dispose();
            this.Transmitter.Dispose();
        }

        /// <summary>
        /// Sends an instance of ITelemetry through the channel.
        /// </summary>
        public void Send(ITelemetry item)
        {
            if (item != null)
            {
                if (TelemetryChannelEventSource.Log.IsVerboseEnabled)
                {
                    TelemetryChannelEventSource.Log.TelemetryChannelSend(
                        item.ToString(),
                        item.Context.InstrumentationKey.Substring(0, Math.Min(item.Context.InstrumentationKey.Length, 8)));
                }

                item.Sanitize();
                this.TelemetryProcessor.Process(item);
            }
        }

        /// <summary>
        /// Synchronously flushes the telemetry buffer. 
        /// </summary>
        public void Flush()
        {
            TelemetryChannelEventSource.Log.TelemetryChannelFlush();
            this.TelemetryBuffer.FlushAsync().ConfigureAwait(false).GetAwaiter().GetResult(); // Don't use Task.Wait() because it wraps the original exception in an AggregateException.
        }

        /// <summary>
        /// Initialize method is called after all configuration properties have been loaded from the configuration.
        /// </summary>
        public void Initialize(TelemetryConfiguration configuration)
        {
            // ApplyPolicies will syncronously get list of file names from disk and calculate size
            // Creating task to improve application startup time
            ExceptionHandler.Start(() => { return TaskEx.Run(() => this.Transmitter.ApplyPolicies()); });
        }
    }
}
