namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Monitor.OpenTelemetry.Exporter;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.Extensions.Logging;
    using OpenTelemetry;
    using OpenTelemetry.Trace;

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
        private static object syncRoot = new object();
        private static TelemetryConfiguration active;

        private readonly object initLock = new object();
        private TracerProvider tracerProvider;
        private ILoggerFactory loggerFactory;
        private ActivitySource activitySource;
        private bool isInitialized = false;

        private string instrumentationKey = string.Empty;
        private string connectionString;
        private bool disableTelemetry = false;

        /// <summary>
        /// Indicates if this instance has been disposed of.
        /// </summary>
        private bool isDisposed = false;

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
        /// Gets or sets the connection string. Setting this value will also set (and overwrite) the <see cref="InstrumentationKey"/>. The endpoints are validated and will be set (and overwritten) for InMemoryChannel and ServerTelemetryChannel as well as the ApplicationIdProvider"/>.
        /// </summary>
        public string ConnectionString
        {
            get
            {
                return this.connectionString;
            }

            set
            {
                this.connectionString = value ?? throw new ArgumentNullException(nameof(this.ConnectionString));
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

        internal TracerProvider TracerProvider
        {
            get
            {
                this.EnsureInitialized();
                return this.tracerProvider;
            }
        }

        internal ActivitySource ActivitySource
        {
            get
            {
                this.EnsureInitialized();
                return this.activitySource;
            }
        }

        internal ILoggerFactory LoggerFactory
        {
            get
            {
                this.EnsureInitialized();
                return this.loggerFactory;
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

        /*internal MetricManager GetMetricManager(bool createIfNotExists)
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
        }*/

        // <summary>
        // This will check the ApplicationIdProvider and attempt to set the endpoint.
        // This only supports our first party providers <see cref="ApplicationInsightsApplicationIdProvider"/> and <see cref="DictionaryApplicationIdProvider"/>.
        // </summary>
        /* private static void SetApplicationIdEndpoint(IApplicationIdProvider applicationIdProvider, string endpoint, bool force = false)
        {
            if (applicationIdProvider != null)
            {
                if (applicationIdProvider is ApplicationInsightsApplicationIdProvider applicationInsightsApplicationIdProvider)
                {
                    if (force || applicationInsightsApplicationIdProvider.ProfileQueryEndpoint == null)
                    {
                        applicationInsightsApplicationIdProvider.ProfileQueryEndpoint = endpoint;
                    }
                }
                else if (applicationIdProvider is DictionaryApplicationIdProvider dictionaryApplicationIdProvider)
                {
                    if (dictionaryApplicationIdProvider.Next is ApplicationInsightsApplicationIdProvider innerApplicationIdProvider)
                    {
                        if (force || innerApplicationIdProvider.ProfileQueryEndpoint == null)
                        {
                            innerApplicationIdProvider.ProfileQueryEndpoint = endpoint;
                        }
                    }
                }
            }
        }*/

        // <summary>
        // This will check the TelemetryChannel and attempt to set the endpoint.
        // This only supports our first party providers InMemoryChannel and ServerTelemetryChannel.
        // </summary>
        /* private static void SetTelemetryChannelEndpoint(ITelemetryChannel channel, string endpoint, bool force = false)
        {
            if (channel != null)
            {
                if (channel is InMemoryChannel || channel.GetType().FullName == "Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.ServerTelemetryChannel")
                {
                    if (force || channel.EndpointAddress == null)
                    {
                        channel.EndpointAddress = endpoint;
                    }
                }
            }
        }*/

        /*private static void SetTelemetryChannelCredentialEnvelope(ITelemetryChannel telemetryChannel, CredentialEnvelope credentialEnvelope)
        {
            if (telemetryChannel is ISupportCredentialEnvelope tc)
            {
                tc.CredentialEnvelope = credentialEnvelope;
            }
        }

        private void SetTelemetryChannelCredentialEnvelope()
        {
            foreach (var tSink in this.TelemetrySinks)
            {
                SetTelemetryChannelCredentialEnvelope(tSink.TelemetryChannel, this.CredentialEnvelope);
            }
        }*/

        /*private void SetTelemetryChannelEndpoint(string ingestionEndpoint)
        {
            foreach (var tSink in this.TelemetrySinks)
            {
                SetTelemetryChannelEndpoint(tSink.TelemetryChannel, ingestionEndpoint, force: true);
            }
        }*/

        private void EnsureInitialized()
        {
            if (this.isInitialized)
            {
                return;
            }

            lock (this.initLock)
            {
                if (this.isInitialized)
                {
                    return;
                }

                this.InitializeOpenTelemetry();
                this.isInitialized = true;
            }
        }

        private void InitializeOpenTelemetry()
        {
            // Create ActivitySource and Meter for this configuration
            this.activitySource = new ActivitySource("Microsoft.ApplicationInsights", "3.0.0");
            this.tracerProvider = Sdk.CreateTracerProviderBuilder()
                                 .AddSource(this.activitySource.Name)
                                 .AddAzureMonitorTraceExporter(o => o.ConnectionString = this.connectionString)
                                 .Build();
            this.loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddOpenTelemetry(options =>
                {
                    options.AddAzureMonitorLogExporter(o => o.ConnectionString = this.connectionString);
                });
            });
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
                // this.metricManager?.Flush();
            }
        }
    }
}
