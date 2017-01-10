namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Threading;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    /// <summary>
    /// Encapsulates the global telemetry configuration typically loaded from the ApplicationInsights.config file.
    /// </summary>
    /// <remarks>
    /// All <see cref="TelemetryContext"/> objects are initialized using the <see cref="Active"/> 
    /// telemetry configuration provided by this class.
    /// </remarks>
    public sealed class TelemetryConfiguration : IDisposable
    {
        private static object syncRoot = new object();
        private static TelemetryConfiguration active;

        private readonly SnapshottingList<ITelemetryInitializer> telemetryInitializers = new SnapshottingList<ITelemetryInitializer>();
        private ITelemetryChannel telemetryChannel = null;
        private TelemetryProcessorChain telemetryProcessorChain;
        private string instrumentationKey = string.Empty;
        private bool disableTelemetry = false;
        private TelemetryProcessorChainBuilder builder;
        private SnapshottingList<IMetricProcessor> metricProcessors = new SnapshottingList<IMetricProcessor>();

        /// <summary>
        /// Indicates if this instance has been disposed of.
        /// </summary>
        private bool isDisposed = false;

        /// <summary>
        /// Initializes a new instance of the TelemetryConfiguration class.
        /// </summary>
        public TelemetryConfiguration() : this(new InMemoryChannel())
        {
        }

        /// <summary>
        /// Initializes a new instance of the TelemetryConfiguration class.
        /// </summary>
        /// <param name="instrumentationKey">The instrumentation key this configuration instance will provide.</param>
        public TelemetryConfiguration(string instrumentationKey) : this(instrumentationKey, new InMemoryChannel())
        {
        }

        /// <summary>
        /// Initializes a new instance of the TelemetryConfiguration class.
        /// </summary>
        /// <param name="channel">The telemetry channel to provide with this configuration instance.</param>
        public TelemetryConfiguration(ITelemetryChannel channel)
        {
            if (channel == null)
            {
                throw new ArgumentNullException("channel");
            }

            this.telemetryChannel = channel;
        }

        /// <summary>
        /// Initializes a new instance of the TelemetryConfiguration class.
        /// </summary>
        /// <param name="instrumentationKey">The instrumentation key this configuration instance will provide.</param>
        /// <param name="channel">The telemetry channel to provide with this configuration instance.</param>
        public TelemetryConfiguration(string instrumentationKey, ITelemetryChannel channel)
        {
            if (channel == null)
            {
                throw new ArgumentNullException("channel");
            }

            if (instrumentationKey == null)
            {
                throw new ArgumentNullException("instrumentationKey");
            }

            this.instrumentationKey = instrumentationKey;
            this.telemetryChannel = channel;
        }

        /// <summary>
        /// Finalizes an instance of the TelemetryConfiguration class.
        /// Frees resources when a TelemetryConfiguration instance is garbage collected if it was not properly disposed of.
        /// </summary>
        ~TelemetryConfiguration()
        {
            // Cleanup managed resources if the instance was not properly disposed of.
            this.Dispose(true);
        }

        /// <summary>
        /// Gets the active <see cref="TelemetryConfiguration"/> instance loaded from the ApplicationInsights.config file. 
        /// If the configuration file does not exist, the active configuration instance is initialized with minimum defaults 
        /// needed to send telemetry to Application Insights.
        /// </summary>
        public static TelemetryConfiguration Active
        {
            get
            {
                if (active == null)
                {
                    lock (syncRoot)
                    {
                        if (active == null)
                        {
                            active = new TelemetryConfiguration();
                            TelemetryConfigurationFactory.Instance.Initialize(active, TelemetryModules.Instance);
                        }
                    }
                }

                return active;
            }

            internal set
            {
                lock (syncRoot)
                {
                    active = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the default instrumentation key for the application.
        /// </summary>
        /// <exception cref="ArgumentNullException">The new value is null.</exception>
        /// <remarks>
        /// This instrumentation key value is used by default by all <see cref="TelemetryClient"/> instances
        /// created in the application. This value can be overwritten by setting the <see cref="TelemetryContext.InstrumentationKey"/>
        /// property of the <see cref="TelemetryClient.Context"/>.
        /// </remarks>
        public string InstrumentationKey
        {
            get
            {
                return this.instrumentationKey;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                this.instrumentationKey = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether sending of telemetry to Application Insights is disabled.
        /// </summary>
        /// <remarks>
        /// This disable tracking setting value is used by default by all <see cref="TelemetryClient"/> instances
        /// created in the application. 
        /// </remarks>
        public bool DisableTelemetry
        {
            get
            {
                return this.disableTelemetry;
            }

            set
            {
                // Log the state of tracking 
                if (value)
                {
                    CoreEventSource.Log.TrackingWasDisabled();
                }
                else
                {
                    CoreEventSource.Log.TrackingWasEnabled();
                }

                this.disableTelemetry = value;
            }
        }

        /// <summary>
        /// Gets the list of <see cref="ITelemetryInitializer"/> objects that supply additional information about telemetry.
        /// </summary>
        /// <remarks>
        /// Telemetry initializers extend Application Insights telemetry collection by supplying additional information 
        /// about individual <see cref="ITelemetry"/> items, such as <see cref="ITelemetry.Timestamp"/>. A <see cref="TelemetryClient"/>
        /// invokes telemetry initializers each time <see cref="TelemetryClient.Track"/> method is called.
        /// The default list of telemetry initializers is provided by the Application Insights NuGet packages and loaded from 
        /// the ApplicationInsights.config file located in the application directory. 
        /// </remarks>
        public IList<ITelemetryInitializer> TelemetryInitializers
        {
            get { return this.telemetryInitializers; }
        }

        /// <summary>
        /// Gets the list of <see cref="IMetricProcessor"/> objects used for custom metric data processing
        /// before client-side metric aggregation process.
        /// </summary>
        public IList<IMetricProcessor> MetricProcessors
        { 
            get { return this.metricProcessors; }
        }

        /// <summary>
        /// Gets a readonly collection of TelemetryProcessors.
        /// </summary>
        public ReadOnlyCollection<ITelemetryProcessor> TelemetryProcessors
        {
            get
            {
                return new ReadOnlyCollection<ITelemetryProcessor>(this.TelemetryProcessorChain.TelemetryProcessors);
            }
        }

        /// <summary>
        /// Gets the TelemetryProcessorChainBuilder which can build and populate TelemetryProcessors in the TelemetryConfiguration.
        /// </summary>
        public TelemetryProcessorChainBuilder TelemetryProcessorChainBuilder
        {
            get
            {
                LazyInitializer.EnsureInitialized(ref this.builder, () => new TelemetryProcessorChainBuilder(this));
                return this.builder;
            }

            internal set
            {
                this.builder = value;
            }
        }

        /// <summary>
        /// Gets or sets the telemetry channel.
        /// </summary>
        public ITelemetryChannel TelemetryChannel
        {
            get
            {
                return this.telemetryChannel;
            }

            set
            {
                ITelemetryChannel oldChannel = this.telemetryChannel;
                this.telemetryChannel = value;

                // If we have a previously assigned channel which is not the same one as the "new" value
                // passed in then we need to dispose of the old channel to keep from leaking resources.
                if (oldChannel != null && oldChannel != value)
                {
                    oldChannel.Dispose();
                }
            }
        }

        /// <summary>
        /// Gets or sets the chain of processors.
        /// </summary>
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
                    throw new ArgumentNullException("value");
                }

                this.telemetryProcessorChain = value;
            }
        }

        /// <summary>
        /// Creates a new <see cref="TelemetryConfiguration"/> instance loaded from the ApplicationInsights.config file.
        /// If the configuration file does not exist, the new configuration instance is initialized with minimum defaults 
        /// needed to send telemetry to Application Insights.
        /// </summary>
        public static TelemetryConfiguration CreateDefault()
        {
            var configuration = new TelemetryConfiguration();
            TelemetryConfigurationFactory.Instance.Initialize(configuration, null);

            return configuration;
        }

        /// <summary>
        /// Creates a new <see cref="TelemetryConfiguration"/> instance loaded from the specified configuration.
        /// </summary>
        /// <param name="config">An xml serialized configuration.</param>
        /// <exception cref="ArgumentNullException">Throws if the config value is null or empty.</exception>
        public static TelemetryConfiguration CreateFromConfiguration(string config)
        {
            if (string.IsNullOrWhiteSpace(config))
            {
                throw new ArgumentNullException("config");
            }

            var configuration = new TelemetryConfiguration();
            TelemetryConfigurationFactory.Instance.Initialize(configuration, null, config);
            return configuration;
        }

        /// <summary>
        /// Releases resources used by the current instance of the <see cref="TelemetryConfiguration"/> class.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of resources.
        /// </summary>
        /// <param name="disposing">Indicates if managed code is being disposed.</param>
        private void Dispose(bool disposing)
        {
            if (!this.isDisposed && disposing)
            {
                this.isDisposed = true;
                Interlocked.CompareExchange(ref active, null, this);

                ITelemetryChannel telemetryChannel = this.TelemetryChannel;
                if (telemetryChannel != null)
                {
                    telemetryChannel.Dispose();
                    telemetryChannel = null;
                }

                TelemetryProcessorChain processorChain = this.telemetryProcessorChain;
                if (processorChain != null)
                {
                    processorChain.Dispose();
                }
            }
        }
    }
}