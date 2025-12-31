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
        /// Sets the sampling ratio for traces.
        /// </summary>
        /// <remarks>
        /// The sampling ratio controls what percentage of trace telemetry is sent to Application Insights.
        /// A value of 1.0 means all telemetry is sent, 0.5 means 50% is sent, etc.
        /// </remarks>
        /// <param name="samplingRatio">The sampling ratio between 0.0 and 1.0 inclusive.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if samplingRatio is not between 0.0 and 1.0.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the configuration has already been built.</exception>
        public void SetSamplingRatio(float samplingRatio)
        {
            this.ThrowIfBuilt();

            if (samplingRatio < 0.0F || samplingRatio > 1.0F)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(samplingRatio),
                    samplingRatio,
                    "Sampling ratio must be between 0.0 and 1.0 inclusive.");
            }

            this.ConfigureOpenTelemetryBuilder(builder =>
            {
                builder.Services.Configure<AzureMonitorExporterOptions>(exporterOptions =>
                {
                    exporterOptions.SamplingRatio = samplingRatio;
                });
            });
        }

        /// <summary>
        /// Sets the number of traces per second to be sampled when using rate-limited sampling.
        /// </summary>
        /// <remarks>
        /// When set, this takes precedence over <see cref="SetSamplingRatio"/>.
        /// For example, specifying 0.5 means one request every two seconds.
        /// </remarks>
        /// <param name="tracesPerSecond">The number of traces per second. Must be greater than or equal to zero.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if tracesPerSecond is negative.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the configuration has already been built.</exception>
        public void SetTracesPerSecond(double tracesPerSecond)
        {
            this.ThrowIfBuilt();

            if (tracesPerSecond < 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(tracesPerSecond),
                    tracesPerSecond,
                    "Traces per second must be greater than or equal to zero.");
            }

            this.ConfigureOpenTelemetryBuilder(builder =>
            {
                builder.Services.Configure<AzureMonitorExporterOptions>(exporterOptions =>
                {
                    exporterOptions.TracesPerSecond = tracesPerSecond;
                });
            });
        }

        /// <summary>
        /// Sets the directory where telemetry will be stored when offline.
        /// </summary>
        /// <remarks>
        /// By default, telemetry is cached locally when the application loses connection to Application Insights
        /// and automatically retried for up to 48 hours.
        /// </remarks>
        /// <param name="storageDirectory">The path to the storage directory.</param>
        /// <exception cref="ArgumentException">Thrown if storageDirectory is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the configuration has already been built.</exception>
        public void SetStorageDirectory(string storageDirectory)
        {
            this.ThrowIfBuilt();

            if (string.IsNullOrWhiteSpace(storageDirectory))
            {
                throw new ArgumentException(
                    "Storage directory cannot be null or empty.",
                    nameof(storageDirectory));
            }

            this.ConfigureOpenTelemetryBuilder(builder =>
            {
                builder.Services.Configure<AzureMonitorExporterOptions>(exporterOptions =>
                {
                    exporterOptions.StorageDirectory = storageDirectory;
                });
            });
        }

        /// <summary>
        /// Disables offline storage for telemetry. By default, offline storage is enabled.
        /// </summary>
        /// <remarks>
        /// When disabled, telemetry that cannot be sent immediately will be dropped instead of being cached locally.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown if the configuration has already been built.</exception>
        public void DisableOfflineStorage()
        {
            this.ThrowIfBuilt();

            this.ConfigureOpenTelemetryBuilder(builder =>
            {
                builder.Services.Configure<AzureMonitorExporterOptions>(exporterOptions =>
                {
                    exporterOptions.DisableOfflineStorage = true;
                });
            });
        }

        /// <summary>
        /// Disables the Live Metrics feature - it is enabled by default.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the configuration has already been built.</exception>
        public void DisableLiveMetrics()
        {
            this.ThrowIfBuilt();

            this.ConfigureOpenTelemetryBuilder(builder =>
            {
                builder.Services.Configure<AzureMonitorExporterOptions>(exporterOptions =>
                {
                    exporterOptions.EnableLiveMetrics = false;
                });
            });
        }

        /// <summary>
        /// Disables trace-based log sampling.
        /// </summary>
        /// <remarks>
        /// When trace-based log sampling is enabled (the default), logs belonging to unsampled traces are dropped.
        /// Use this method to export all logs regardless of trace sampling decisions.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown if the configuration has already been built.</exception>
        public void DisableTraceBasedLogsSampling()
        {
            this.ThrowIfBuilt();

            this.ConfigureOpenTelemetryBuilder(builder =>
            {
                builder.Services.Configure<AzureMonitorExporterOptions>(exporterOptions =>
                {
                    exporterOptions.EnableTraceBasedLogsSampler = false;
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
