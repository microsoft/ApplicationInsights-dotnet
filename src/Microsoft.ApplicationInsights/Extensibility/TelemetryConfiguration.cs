namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Endpoints;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Sampling;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Metrics;
    using Microsoft.ApplicationInsights.Metrics.Extensibility;

    /// <summary>
    /// Encapsulates the global telemetry configuration typically loaded from the ApplicationInsights.config file.
    /// </summary>
    /// <remarks>
    /// All <see cref="TelemetryContext"/> objects are initialized using the <see cref="Active"/> 
    /// telemetry configuration provided by this class.
    /// </remarks>
    public sealed class TelemetryConfiguration : IDisposable
    {
        internal readonly SamplingRateStore LastKnownSampleRateStore = new SamplingRateStore();

        private static object syncRoot = new object();
        private static TelemetryConfiguration active;

        private readonly SnapshottingList<ITelemetryInitializer> telemetryInitializers = new SnapshottingList<ITelemetryInitializer>();
        private readonly TelemetrySinkCollection telemetrySinks = new TelemetrySinkCollection();
        
        private TelemetryProcessorChain telemetryProcessorChain;
        private string instrumentationKey = string.Empty;
        private string connectionString;
        private bool disableTelemetry = false;
        private TelemetryProcessorChainBuilder builder;
        private MetricManager metricManager = null;

        /// <summary>
        /// Indicates if this instance has been disposed of.
        /// </summary>
        private bool isDisposed = false;

        /// <summary>
        /// Initializes a new instance of the TelemetryConfiguration class.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public TelemetryConfiguration() : this(string.Empty, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the TelemetryConfiguration class.
        /// </summary>
        /// <param name="instrumentationKey">The instrumentation key this configuration instance will provide.</param>
        public TelemetryConfiguration(string instrumentationKey) : this(instrumentationKey, null)
        {
        }

        public TelemetryConfiguration(ITelemetryChannel channel, string connectionString)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Initializes a new instance of the TelemetryConfiguration class.
        /// </summary>
        /// <param name="instrumentationKey">The instrumentation key this configuration instance will provide.</param>
        /// <param name="channel">The telemetry channel to provide with this configuration instance.</param>
        public TelemetryConfiguration(string instrumentationKey, ITelemetryChannel channel)
        {
            if (instrumentationKey == null)
            {
                throw new ArgumentNullException(nameof(instrumentationKey));
            }

            this.instrumentationKey = instrumentationKey;
            var defaultSink = new TelemetrySink(this, channel);
            defaultSink.Name = "default";
            this.telemetrySinks.Add(defaultSink);
        }

        /// <summary>
        /// Gets the active <see cref="TelemetryConfiguration"/> instance loaded from the ApplicationInsights.config file. 
        /// If the configuration file does not exist, the active configuration instance is initialized with minimum defaults 
        /// needed to send telemetry to Application Insights.
        /// </summary>
#if NETSTANDARD1_3 || NETSTANDARD2_0
        [Obsolete("We do not recommend using TelemetryConfiguration.Active on .NET Core. See https://github.com/microsoft/ApplicationInsights-dotnet/issues/1152 for more details")]
#endif 
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
            get { return this.instrumentationKey; }

            set { this.instrumentationKey = value ?? throw new ArgumentNullException(nameof(this.InstrumentationKey)); }
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
        /// Gets or sets the telemetry channel for the default sink.
        /// </summary>
        public ITelemetryChannel TelemetryChannel
        {
            get
            {
                // We do not ensure not disposed here because TelemetryChannel is accessed during configuration disposal.
                return this.telemetrySinks.DefaultSink.TelemetryChannel;
            }

            set
            {
                if (!this.isDisposed)
                {
                    this.telemetrySinks.DefaultSink.TelemetryChannel = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the Application Id Provider.
        /// </summary>
        /// <remarks>
        /// This feature is opt-in and must be configured to be enabled.
        /// </remarks>
        public IApplicationIdProvider ApplicationIdProvider { get; set; }

        /// <summary>
        /// Gets the Endpoint Controller responsible for making service endpoints available.
        /// </summary>
        public EndpointContainer Endpoint { get; private set; } = new EndpointContainer(new EndpointProvider());

        /// <summary>
        /// Gets or sets the connection string. Setting this value will also set the Instrumentation Key, validate the endpoints, and set the TelemetryChannel.Endpoint.
        /// </summary>
        public string ConnectionString
        {
            get
            {
                return this.connectionString;
            }

            set
            {
                try
                {
                    this.connectionString = value ?? throw new ArgumentNullException(nameof(this.ConnectionString));

                    var endpointProvider = new EndpointProvider
                    {
                        ConnectionString = value,
                    };

                    this.InstrumentationKey = endpointProvider.GetInstrumentationKey();

                    this.Endpoint = new EndpointContainer(endpointProvider);

                    foreach (var tSink in this.TelemetrySinks)
                    {
                        if (tSink.TelemetryChannel is InMemoryChannel || tSink.TelemetryChannel.GetType().FullName == "Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.ServerTelemetryChannel")
                        {
                            tSink.TelemetryChannel.EndpointAddress = new Uri(this.Endpoint.Ingestion, "v2/track").AbsoluteUri;
                        }
                    }

                    // TODO: NEED TO UPDATE ApplicationIdProvider's Endpoint
                }
                catch (Exception ex)
                {
                    // TODO: LOG TO ETW ERROR: Could not set Connection String. Log Inner Exception.
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets a collection of strings indicating if an experimental feature should be enabled.
        /// The presence of a string in this collection will be evaluated as 'true'.
        /// </summary>
        /// <remarks>
        /// This property allows the dev team to ship and evaluate features before adding these to the public API.
        /// We are not committing to support any features enabled through this property.
        /// Use this at your own risk.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public IList<string> ExperimentalFeatures { get; } = new List<string>(0);

        /// <summary>
        /// Gets a list of telemetry sinks associated with the configuration.
        /// </summary>
        public IList<TelemetrySink> TelemetrySinks => this.telemetrySinks;

        /// <summary>
        /// Gets the default telemetry sink.
        /// </summary>
        public TelemetrySink DefaultTelemetrySink => this.telemetrySinks.DefaultSink;

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
                    throw new ArgumentNullException(nameof(value));
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
                throw new ArgumentNullException(nameof(config));
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

        internal MetricManager GetMetricManager(bool createIfNotExists)
        {
            MetricManager manager = this.metricManager;
            if (manager == null && createIfNotExists)
            {
                var pipelineAdapter = new ApplicationInsightsTelemetryPipeline(this);
                MetricManager newManager = new MetricManager(pipelineAdapter);
                MetricManager prevManager = Interlocked.CompareExchange(ref this.metricManager, newManager, null);

                if (prevManager == null)
                {
                    manager = newManager;
                }
                else
                {
                    // We just created a new manager that we are not using. Stop is before discarding.
                    Task fireAndForget = newManager.StopDefaultAggregationCycleAsync();
                    manager = prevManager;
                }
            }

            return manager;
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

                // I think we should be flushing this.telemetrySinks.DefaultSink.TelemetryChannel at this point.
                // Filed https://github.com/Microsoft/ApplicationInsights-dotnet/issues/823 to track.
                // For now just flushing the metrics:
                this.metricManager?.Flush();

                if (this.telemetryProcessorChain != null)
                {
                    // Not setting this.telemetryProcessorChain to null because calls to the property getter would reinitialize it.
                    this.telemetryProcessorChain.Dispose();
                }

                foreach (TelemetrySink sink in this.telemetrySinks)
                {
                    sink.Dispose();
                    if (!object.ReferenceEquals(sink, this.telemetrySinks.DefaultSink))
                    {
                        this.telemetrySinks.Remove(sink);
                    }
                }
            }
        }
    }
}
