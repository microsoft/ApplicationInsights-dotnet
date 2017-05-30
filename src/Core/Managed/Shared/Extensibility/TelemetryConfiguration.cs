namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
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
        private const int DefaultSinkIndex = 0;

        private static object syncRoot = new object();
        private static TelemetryConfiguration active;

        private readonly SnapshottingList<ITelemetryInitializer> telemetryInitializers = new SnapshottingList<ITelemetryInitializer>();
        private readonly SnapshottingList<TelemetrySink> telemetrySinks = new SnapshottingList<TelemetrySink>();
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

        /// <summary>
        /// Initializes a new instance of the TelemetryConfiguration class.
        /// </summary>
        /// <param name="instrumentationKey">The instrumentation key this configuration instance will provide.</param>
        /// <param name="channel">The telemetry channel to provide with this configuration instance.</param>
        public TelemetryConfiguration(string instrumentationKey, ITelemetryChannel channel)
        {
            if (instrumentationKey == null)
            {
                throw new ArgumentNullException("instrumentationKey");
            }

            this.instrumentationKey = instrumentationKey;
            var defaultSink = new TelemetrySink(this, channel);
            this.telemetrySinks.Add(defaultSink);
            Debug.Assert(object.ReferenceEquals(this.telemetrySinks[DefaultSinkIndex], defaultSink), "Default sink should be the first on the list");
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
                return this.telemetrySinks[DefaultSinkIndex].TelemetryChannel;
            }

            set
            {
                this.EnsureNotDisposed();
                this.telemetrySinks[DefaultSinkIndex].TelemetryChannel = value;
            }
        }

        /// <summary>
        /// Gets a read-only collection of telemetry sinks.
        /// </summary>
        public ReadOnlyCollection<TelemetrySink> TelemetrySinks
        {
            get
            {
                return new ReadOnlyCollection<TelemetrySink>(this.telemetrySinks);
            }
        }

        /// <summary>
        /// Gets the default telemetry sink.
        /// </summary>
        public TelemetrySink DefaultTelemetrySink => this.telemetrySinks[DefaultSinkIndex];

        /// <summary>
        /// Gets the list of <see cref="IMetricProcessor"/> objects used for custom metric data processing        
        /// before client-side metric aggregation process.
        /// </summary>
        internal IList<IMetricProcessor> MetricProcessors
        {
            get { return this.metricProcessors; }
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
        /// Adds a new telemetry sink to the configuration.
        /// </summary>
        /// <param name="sink">Sink to add.</param>
        public void AddSink(TelemetrySink sink)
        {
            if (sink == null)
            {
                throw new ArgumentNullException(nameof(sink));
            }

            this.EnsureNotDisposed();
            this.telemetrySinks.Add(sink);
        }

        /// <summary>
        /// Removes given sink from the configuration.
        /// </summary>
        /// <param name="sink">Sink to remove.</param>
        /// <returns>True, if sink was removed successfully, otherwise false.</returns>
        /// <remarks>Default sink (with index zero in the <see cref="TelemetrySinks"/> collection) cannot be removed.</remarks>
        public bool RemoveSink(TelemetrySink sink)
        {
            if (sink == null)
            {
                throw new ArgumentNullException(nameof(sink));
            }

            if (this.IsDefaultSink(sink))
            {
                throw new InvalidOperationException("Default sink cannot be removed");
            }

            this.EnsureNotDisposed();
            return this.telemetrySinks.Remove(sink);
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

                if (this.telemetryProcessorChain != null)
                {
                    // Not setting this.telemetryProcessorChain to null because calls to the property getter would reinitialize it.
                    this.telemetryProcessorChain.Dispose();
                }

                foreach (TelemetrySink sink in this.telemetrySinks)
                {
                    sink.Dispose();
                    if (!this.IsDefaultSink(sink))
                    {
                        this.telemetrySinks.Remove(sink);
                    }
                }
            }
        }

        private bool IsDefaultSink(TelemetrySink sink)
        {
            Debug.Assert(sink != null, "The 'sink' parameter value should not be null");
            return object.ReferenceEquals(sink, this.telemetrySinks[DefaultSinkIndex]);
        }

        private void EnsureNotDisposed()
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException(nameof(TelemetryConfiguration));
            }
        }
    }
}