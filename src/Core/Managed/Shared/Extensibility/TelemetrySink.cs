namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    /// <summary>
    /// Represents a destination for telemetry, consisting of a set of telemetry processors and a channel.
    /// </summary>
    public sealed class TelemetrySink : IDisposable
    {
        private TelemetryConfiguration telemetryConfiguration;
        private ITelemetryChannel telemetryChannel;
        private bool shouldDisposeChannel;
        private TelemetryProcessorChain telemetryProcessorChain;
        private TelemetryProcessorChainBuilder telemetryProcessorChainBuilder;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetrySink"/> class.
        /// </summary>
        /// <param name="telemetryConfiguration">Telemetry configuration to use for the new <see cref="TelemetrySink"/> instance.</param>
        /// <param name="telemetryChannel">Telemetry channel to use for the new <see cref="TelemetrySink"/> instance.</param>
        public TelemetrySink(TelemetryConfiguration telemetryConfiguration, ITelemetryChannel telemetryChannel = null)
        {
            if (telemetryConfiguration == null)
            {
                throw new ArgumentNullException(nameof(telemetryConfiguration));
            }

            this.telemetryConfiguration = telemetryConfiguration;

            if (telemetryChannel != null)
            {
                this.telemetryChannel = telemetryChannel;
                this.shouldDisposeChannel = false;
            }
            else
            {
                this.telemetryChannel = new InMemoryChannel();
                this.shouldDisposeChannel = true;
            }
        }

        /// <summary>
        /// Gets or sets an instance of the <see cref="TelemetryProcessorChainBuilder"/> that this sink is using.
        /// </summary>
        public TelemetryProcessorChainBuilder TelemetryProcessorChainBuilder
        {
            get
            {
                return LazyInitializer.EnsureInitialized(ref this.telemetryProcessorChainBuilder, () => new TelemetryProcessorChainBuilder(this.telemetryConfiguration, this));
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (!object.ReferenceEquals(value.TelemetrySink, this))
                {
                    throw new ArgumentException("The passed TelemetryProcessorChainBuilder has been configured to use a different TelemetrySink instance", nameof(value));
                }

                if (this.telemetryProcessorChain != null && this.telemetryProcessorChainBuilder != null && !object.ReferenceEquals(this.telemetryProcessorChainBuilder, value))
                {
                    // If a new builder is assigned to the sink, dispose the old chain, giving the new builder a chance to build it.
                    this.telemetryProcessorChain.Dispose();
                    this.telemetryProcessorChain = null;
                }

                this.telemetryProcessorChainBuilder = value;
            }
        }

        /// <summary>
        /// Gets or sets the telemetry channel.
        /// </summary>
        public ITelemetryChannel TelemetryChannel
        {
            get => this.telemetryChannel;
            set
            {
                ITelemetryChannel oldChannel = this.telemetryChannel;
                this.telemetryChannel = value;

                // If we have a previously assigned channel which was created by us and is not the same one as the
                // "new" value passed in then we need to dispose of the old channel to keep from leaking resources.
                if (oldChannel != null && oldChannel != value && this.shouldDisposeChannel)
                {
                    oldChannel.Dispose();
                    this.shouldDisposeChannel = false; // The new one wasn't created by us so it should be managed by whoever created it.
                }
            }
        }

        internal TelemetryProcessorChain TelemetryProcessorChain
        {
            get
            {
                if (this.telemetryProcessorChain == null)
                {
                    this.TelemetryProcessorChainBuilder.Build();
                }

                return this.telemetryProcessorChain;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                this.telemetryProcessorChain = value;
            }
        }

        /// <summary>
        /// Releases resources used by the instance of <see cref="TelemetrySink"/> class.
        /// </summary>
        public void Dispose()
        {
            if (this.shouldDisposeChannel)
            {
                this.telemetryChannel?.Dispose();
            }

            this.telemetryChannel = null;

            this.telemetryProcessorChain?.Dispose();
            this.telemetryProcessorChain = null;
        }

        /// <summary>
        /// Processes a collected telemetry item.
        /// </summary>
        /// <param name="item">Item to process.</param>
        public void Process(ITelemetry item)
        {
            this.TelemetryProcessorChain.Process(item);
        }
    }
}
