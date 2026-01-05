namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Reflection;
    using System.Threading;
    using Azure.Core;
    using Azure.Monitor.OpenTelemetry.Exporter;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Metrics;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using OpenTelemetry;
    using OpenTelemetry.Resources;

    /// <summary>
    /// Encapsulates the global telemetry configuration typically loaded from the ApplicationInsights.config file.
    /// </summary>
    /// <remarks>
    /// All <see cref="TelemetryContext"/> objects are initialized using the Active/> 
    /// telemetry configuration provided by this class.
    /// </remarks>
    public sealed class TelemetryConfiguration : IDisposable
    {
        // internal readonly SamplingRateStore LastKnownSampleRateStore = new SamplingRateStore();

        internal const string ApplicationInsightsActivitySourceName = "Microsoft.ApplicationInsights";
        internal const string ApplicationInsightsMeterName = "Microsoft.ApplicationInsights";
        private static readonly Lazy<TelemetryConfiguration> DefaultInstance =
                                                        new Lazy<TelemetryConfiguration>(() => new TelemetryConfiguration(), LazyThreadSafetyMode.ExecutionAndPublication);

        private readonly object lockObject = new object();
        private readonly bool skipDefaultBuilderConfiguration;

        private string connectionString;
        private bool disableTelemetry = false;
        private bool isBuilt = false;
        private bool isDisposed = false;

        // Exporter options
        private float samplingRatio = 1.0f;
        private double tracesPerSecond = 5.0;
        private string storageDirectory;
        private bool disableOfflineStorage = false;
        private bool enableLiveMetrics = true;
        private bool enableTraceBasedLogsSampler = true;

        private Action<IOpenTelemetryBuilder> builderConfiguration;
        private OpenTelemetrySdk openTelemetrySdk;
        private ActivitySource defaultActivitySource;
        private MetricsManager metricsManager;

        /// <summary>
        /// Initializes a new instance of the TelemetryConfiguration class.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public TelemetryConfiguration() : this(skipDefaultBuilderConfiguration: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the TelemetryConfiguration class.
        /// </summary>
        /// <param name="skipDefaultBuilderConfiguration">If true, skips setting default builder configuration (used in DI scenarios).</param>
        internal TelemetryConfiguration(bool skipDefaultBuilderConfiguration)
        {
            this.skipDefaultBuilderConfiguration = skipDefaultBuilderConfiguration;

            // Create the default ActivitySource
            this.defaultActivitySource = new ActivitySource(ApplicationInsightsActivitySourceName);

            // Create the MetricsManager
            this.metricsManager = new MetricsManager(ApplicationInsightsMeterName);

            // Only set default configuration for non-DI scenarios
            if (!skipDefaultBuilderConfiguration)
            {
                this.builderConfiguration = builder => builder.WithApplicationInsights();
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
                this.ThrowIfBuilt();
                this.disableTelemetry = value;
            }
        }

        /// <summary>
        /// Gets or sets the connection string.
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
        /// Gets or sets the sampling ratio for traces (0.0 to 1.0).
        /// A value of 1.0 means all telemetry is sent, 0.5 means 50% is sent.
        /// Default is 1.0 (all telemetry sent).
        /// </summary>
        public float SamplingRatio
        {
            get => this.samplingRatio;
            set
            {
                this.ThrowIfBuilt();
                this.samplingRatio = value;
            }
        }

        /// <summary>
        /// Gets or sets the number of traces per second for rate-limited sampling.
        /// Default is 5.0.
        /// </summary>
        public double TracesPerSecond
        {
            get => this.tracesPerSecond;
            set
            {
                this.ThrowIfBuilt();
                this.tracesPerSecond = value;
            }
        }

        /// <summary>
        /// Gets or sets the directory for offline telemetry storage.
        /// Default is null (uses system default location).
        /// </summary>
        public string StorageDirectory
        {
            get => this.storageDirectory;
            set
            {
                this.ThrowIfBuilt();
                this.storageDirectory = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether offline storage is disabled.
        /// Default is false (offline storage is enabled).
        /// </summary>
        public bool DisableOfflineStorage
        {
            get => this.disableOfflineStorage;
            set
            {
                this.ThrowIfBuilt();
                this.disableOfflineStorage = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether Live Metrics is enabled.
        /// Default is true.
        /// </summary>
        public bool EnableLiveMetrics
        {
            get => this.enableLiveMetrics;
            set
            {
                this.ThrowIfBuilt();
                this.enableLiveMetrics = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether trace-based log sampling is enabled.
        /// Default is true.
        /// </summary>
        public bool EnableTraceBasedLogsSampler
        {
            get => this.enableTraceBasedLogsSampler;
            set
            {
                this.ThrowIfBuilt();
                this.enableTraceBasedLogsSampler = value;
            }
        }

        /// <summary>
        /// Gets the default ActivitySource used by TelemetryClient.
        /// </summary>
        internal ActivitySource ApplicationInsightsActivitySource => this.defaultActivitySource;

        /// <summary>
        /// Gets the MetricsManager used by TelemetryClient for metrics tracking.
        /// </summary>
        internal MetricsManager MetricsManager => this.metricsManager;

        /// <summary>
        /// Creates a new <see cref="TelemetryConfiguration"/> instance loaded from the ApplicationInsights.config file.
        /// If the configuration file does not exist, the new configuration instance is initialized with minimum defaults 
        /// needed to send telemetry to Application Insights.
        /// </summary>
        public static TelemetryConfiguration CreateDefault()
        {
            return DefaultInstance.Value;
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
        /// <param name="tokenCredential">An instance of TokenCredential.</param>
        public void SetAzureTokenCredential(TokenCredential tokenCredential)
        {
            this.ThrowIfBuilt();

            if (tokenCredential == null)
            {
                throw new ArgumentNullException(nameof(tokenCredential));
            }

            // Configure the OpenTelemetry builder to pass the credential to Azure Monitor Exporter
            this.ConfigureOpenTelemetryBuilder(builder =>
            {
                builder.Services.Configure<AzureMonitorExporterOptions>(exporterOptions =>
                {
                    exporterOptions.Credential = tokenCredential;
                });
            });
        }

        /// <summary>
        /// Sets the cloud role name and role instance for telemetry.
        /// This configures the OpenTelemetry Resource with service.name, service.namespace, service.instance.id, and service.version attributes
        /// which map to Cloud.RoleName, Cloud.RoleInstance, and Application.Ver in Application Insights.
        /// </summary>
        /// <param name="serviceName">The service name (maps to Cloud.RoleName).</param>
        /// <param name="serviceInstanceId">Optional. The service instance ID (maps to Cloud.RoleInstance). If not provided, defaults to hostname.</param>
        /// <param name="serviceVersion">Optional. The service version (maps to Application.Ver).</param>
        /// <exception cref="InvalidOperationException">Thrown if the configuration has already been built.</exception>
        internal void SetCloudRole(string serviceName, string serviceInstanceId = null, string serviceVersion = null)
        {
            this.ThrowIfBuilt();

            this.ConfigureOpenTelemetryBuilder(builder =>
            {
                builder.ConfigureResource(resourceBuilder =>
                {
                    var attributes = new List<KeyValuePair<string, object>>();

                    if (serviceName != null)
                    {
                        attributes.Add(new KeyValuePair<string, object>("service.name", serviceName));
                    }

                    if (serviceInstanceId != null)
                    {
                        attributes.Add(new KeyValuePair<string, object>("service.instance.id", serviceInstanceId));
                    }

                    if (serviceVersion != null)
                    {
                        attributes.Add(new KeyValuePair<string, object>("service.version", serviceVersion));
                    }

                    if (attributes.Count > 0)
                    {
                        resourceBuilder.AddAttributes(attributes);
                    }
                });
            });
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

                this.openTelemetrySdk = OpenTelemetrySdk.Create(builder =>
                {
                    this.builderConfiguration(builder);
                    builder.SetAzureMonitorExporter(options =>
                    {
                        options.ConnectionString = this.connectionString;
                        options.SamplingRatio = this.samplingRatio;
                        options.TracesPerSecond = this.tracesPerSecond;
                        options.DisableOfflineStorage = this.disableOfflineStorage;
                        options.EnableLiveMetrics = this.enableLiveMetrics;
                        options.EnableTraceBasedLogsSampler = this.enableTraceBasedLogsSampler;

                        if (!string.IsNullOrEmpty(this.storageDirectory))
                        {
                            options.StorageDirectory = this.storageDirectory;
                        }
                    });
                });

                this.isBuilt = true;

                this.StartHostedServices();

                return this.openTelemetrySdk;
            }
        }

        private void StartHostedServices()
        {
            try
            {
                // Use reflection to access the internal Services property
                var servicesProperty = typeof(OpenTelemetrySdk).GetProperty(
                    "Services",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (servicesProperty != null)
                {
                    var serviceProvider = servicesProperty.GetValue(this.openTelemetrySdk) as IServiceProvider;

                    if (serviceProvider != null)
                    {
                        var hostedServices = serviceProvider.GetServices<IHostedService>();
                        foreach (var hostedService in hostedServices)
                        {
                            hostedService.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CoreEventSource.Log.FailedToStartHostedServices(ex.ToInvariantString());
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

                // Dispose the MetricsManager
                this.metricsManager?.Dispose();

                this.isDisposed = true;

                // I think we should be flushing this.telemetrySinks.DefaultSink.TelemetryChannel at this point.
                // Filed https://github.com/Microsoft/ApplicationInsights-dotnet/issues/823 to track.
                // For now just flushing the metrics:
                // this.metricManager?.Flush();
            }
        }
    }
}
