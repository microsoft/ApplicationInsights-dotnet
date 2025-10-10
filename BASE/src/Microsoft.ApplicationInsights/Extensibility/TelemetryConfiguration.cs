namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Threading;
    using Microsoft.ApplicationInsights.DataContracts;
    using OpenTelemetry;

    /// <summary>
    /// Encapsulates the global telemetry configuration typically loaded from the ApplicationInsights.config file.
    /// </summary>
    /// <remarks>
    /// All <see cref="TelemetryContext"/> objects are initialized using the <see cref="Active"/> 
    /// telemetry configuration provided by this class.
    /// </remarks>
    public sealed class TelemetryConfiguration : IDisposable
    {
        // internal readonly SamplingRateStore LastKnownSampleRateStore = new SamplingRateStore();

        internal const string ApplicationInsightsActivitySourceName = "Microsoft.ApplicationInsights";
        
        private static object syncRoot = new object();
        private static TelemetryConfiguration active;

        private readonly object lockObject = new object();

        private string instrumentationKey = string.Empty;
        private string connectionString;
        private bool disableTelemetry = false;
        private bool isBuilt = false;
        private bool isDisposed = false;

        private Action<IOpenTelemetryBuilder> builderConfiguration;
        private OpenTelemetrySdk openTelemetrySdk;
        private ActivitySource defaultActivitySource;

        /// <summary>
        /// Static Constructor which sets ActivityID Format to W3C if Format not enforced.
        /// This ensures SDK operates in W3C mode, unless turned off explicitily with the following 2 lines
        /// in user code in application startup.
        /// Activity.DefaultIdFormat = ActivityIdFormat.Hierarchical
        /// Activity.ForceDefaultIdFormat = true.
        /// </summary>
        static TelemetryConfiguration()
        {
            /*ActivityExtensions.TryRun(() =>
            {
                if (!Activity.ForceDefaultIdFormat)
                {
                    Activity.DefaultIdFormat = ActivityIdFormat.W3C;
                    Activity.ForceDefaultIdFormat = true;
                }
            });
            SelfDiagnosticsInitializer.EnsureInitialized();*/
        }

        /// <summary>
        /// Initializes a new instance of the TelemetryConfiguration class.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public TelemetryConfiguration()
        {
            // Create the default ActivitySource
            this.defaultActivitySource = new ActivitySource(ApplicationInsightsActivitySourceName);

            // Start with default Application Insights configuration
            this.builderConfiguration = builder => builder.WithApplicationInsights();
        }

        /// <summary>
        /// Gets the active <see cref="TelemetryConfiguration"/> instance loaded from the ApplicationInsights.config file. 
        /// If the configuration file does not exist, the active configuration instance is initialized with minimum defaults 
        /// needed to send telemetry to Application Insights.
        /// </summary>
#if NETSTANDARD // This constant is defined for all versions of NetStandard https://docs.microsoft.com/en-us/dotnet/core/tutorials/libraries#how-to-multitarget
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
            get => this.instrumentationKey;

            [Obsolete("InstrumentationKey based global ingestion is being deprecated. Use TelemetryConfiguration.ConnectionString. See https://github.com/microsoft/ApplicationInsights-dotnet/issues/2560 for more details.")]
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
                this.ThrowIfBuilt();
                this.disableTelemetry = value;
            }
        }

        /// <summary>
        /// Gets or sets the connection string. Setting this value will also set (and overwrite) the <see cref="InstrumentationKey"/>. The endpoints are validated and will be set (and overwritten) for InMemoryChannel and ServerTelemetryChannel as well as the ApplicationIdProvider"/>.
        /// </summary>
        public string ConnectionString
        {
            get => this.connectionString;

            set
            {
                this.ThrowIfBuilt();
                this.connectionString = value ?? throw new ArgumentNullException(nameof(value));
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
        /// Gets the default ActivitySource used by TelemetryClient.
        /// </summary>
        internal ActivitySource ApplicationInsightsActivitySource => this.defaultActivitySource;

        /// <summary>
        /// Gets a value indicating whether this configuration has been built.
        /// Once built, the configuration becomes read-only.
        /// </summary>
        internal bool IsBuilt => this.isBuilt;

        /// <summary>
        /// Creates a new <see cref="TelemetryConfiguration"/> instance loaded from the ApplicationInsights.config file.
        /// If the configuration file does not exist, the new configuration instance is initialized with minimum defaults 
        /// needed to send telemetry to Application Insights.
        /// </summary>
        public static TelemetryConfiguration CreateDefault()
        {
            return new TelemetryConfiguration();
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
            return configuration;
        }

        /// <summary>
        /// Allows extending the OpenTelemetry builder configuration.
        /// </summary>
        /// <remarks>
        /// Use this to extend the telemetry pipeline with custom sources, processors, or exporters.
        /// This can only be called before the configuration is built.
        /// </remarks>
        /// <param name="configure">Action to configure the OpenTelemetry builder.</param>
        /// <exception cref="InvalidOperationException">Thrown if the configuration has already been built.</exception>
        public void ConfigureOpenTelemetryBuilder(Action<IOpenTelemetryBuilder> configure)
        {
            this.ThrowIfBuilt();

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            // Chain the configurations
            var previousConfiguration = this.builderConfiguration;
            this.builderConfiguration = builder =>
            {
                previousConfiguration(builder);
                configure(builder);
            };
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
        /// Set a TokenCredential for this configuration.
        /// </summary>
        /// <remarks>
        /// For more information on expected types, review the documentation for the Azure.Identity library.
        /// (https://github.com/Azure/azure-sdk-for-net/tree/master/sdk/identity/Azure.Identity).
        /// </remarks>
        /// <param name="tokenCredential">An instance of Azure.Core.TokenCredential.</param>
        /// <exception cref="ArgumentException">An ArgumentException is thrown if the provided object does not inherit Azure.Core.TokenCredential.</exception>
#pragma warning disable CA1822 // Mark members as static
#pragma warning disable CA1801 // Review unused parameters
        public void SetAzureTokenCredential(object tokenCredential)
#pragma warning restore CA1801 // Review unused parameters
#pragma warning restore CA1822 // Mark members as static
        {
        }

        /// <summary>
        /// Builds the OpenTelemetry SDK with the current configuration.
        /// This is called internally by TelemetryClient and should not be called directly.
        /// </summary>
        internal OpenTelemetrySdk Build()
        {
            if (this.isBuilt)
            {
                return this.openTelemetrySdk;
            }

            lock (this.lockObject)
            {
                if (this.isBuilt)
                {
                    return this.openTelemetrySdk;
                }

                // Build the final configuration action
                var finalConfiguration = this.builderConfiguration;

                // Add connection string configuration if provided
                if (!string.IsNullOrEmpty(this.connectionString))
                {
                    var connectionStringCopy = this.connectionString;
                    finalConfiguration = builder =>
                    {
                        this.builderConfiguration(builder);
                        builder.SetAzureMonitorExporter(options =>
                        {
                            options.ConnectionString = connectionStringCopy;
                        });
                    };
                }

                // Create the OpenTelemetry SDK using the actual API
                this.openTelemetrySdk = OpenTelemetrySdk.Create(finalConfiguration);
                this.isBuilt = true;

                return this.openTelemetrySdk;
            }
        }

        private void ThrowIfBuilt()
        {
            if (this.isBuilt)
            {
                throw new InvalidOperationException(
                    "Configuration cannot be modified after it has been built. " +
                    "Create a new TelemetryConfiguration instance if you need different settings.");
            }
        }

        /// <summary>
        /// Disposes of resources.
        /// </summary>
        /// <param name="disposing">Indicates if managed code is being disposed.</param>
        private void Dispose(bool disposing)
        {
            if (!this.isDisposed && disposing)
            {
                // Dispose OpenTelemetry SDK - this will dispose:
                // - ServiceProvider
                // - LoggerProvider
                // - MeterProvider  
                // - TracerProvider
                // - All registered processors, exporters, etc.
                this.openTelemetrySdk?.Dispose();

                // Dispose the ActivitySource
                this.defaultActivitySource?.Dispose();

                this.isDisposed = true;
                Interlocked.CompareExchange(ref active, null, this);

                // I think we should be flushing this.telemetrySinks.DefaultSink.TelemetryChannel at this point.
                // Filed https://github.com/Microsoft/ApplicationInsights-dotnet/issues/823 to track.
                // For now just flushing the metrics:
                // this.metricManager?.Flush();
            }
        }
    }
}
