namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Platform;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Metrics;
    using Microsoft.ApplicationInsights.Metrics.Extensibility;

    /// <inheritdoc />
    public sealed class TelemetryClient : ITelemetryClient
    {
#if NETSTANDARD // This constant is defined for all versions of NetStandard https://docs.microsoft.com/en-us/dotnet/core/tutorials/libraries#how-to-multitarget
        private const string VersionPrefix = "dotnetc:";
#else
        private const string VersionPrefix = "dotnet:";
#endif  
        private readonly TelemetryConfiguration configuration;

        private string sdkVersion;

#pragma warning disable 612, 618 // TelemetryConfiguration.Active
        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryClient" /> class. Send telemetry with the active configuration, usually loaded from ApplicationInsights.config.
        /// </summary>
#if NETSTANDARD // This constant is defined for all versions of NetStandard https://docs.microsoft.com/en-us/dotnet/core/tutorials/libraries#how-to-multitarget
        [Obsolete("We do not recommend using TelemetryConfiguration.Active on .NET Core. See https://github.com/microsoft/ApplicationInsights-dotnet/issues/1152 for more details")]
#endif
        public TelemetryClient() : this(TelemetryConfiguration.Active)
        {
        }

        /// <inheritdoc />
        public TelemetryClient(TelemetryConfiguration configuration)
        {
            if (configuration == null)
            {
                CoreEventSource.Log.TelemetryClientConstructorWithNoTelemetryConfiguration();
                configuration = TelemetryConfiguration.Active;
            }

            this.configuration = configuration;

            if (this.configuration.TelemetryChannel == null)
            {
                throw new ArgumentException("The specified configuration does not have a telemetry channel.", nameof(configuration));
            }
        }
#pragma warning restore 612, 618 // TelemetryConfiguration.Active

        /// <inheritdoc />
        public TelemetryContext Context
        {
            get;
            internal set;
        }

        = new TelemetryContext();

        /// <inheritdoc />
        public string InstrumentationKey
        {
            get => this.Context.InstrumentationKey;

            [Obsolete("InstrumentationKey based global ingestion is being deprecated. Recommended to set TelemetryConfiguration.ConnectionString. See https://github.com/microsoft/ApplicationInsights-dotnet/issues/2560 for more details.")]
            set { this.Context.InstrumentationKey = value; }
        }

        /// <inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public TelemetryConfiguration TelemetryConfiguration
        {
            get { return this.configuration; }
        }

        /// <inheritdoc />
        public bool IsEnabled()
        {
            return !this.configuration.DisableTelemetry;
        }

        /// <inheritdoc />
        public void TrackEvent(string eventName, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            var telemetry = new EventTelemetry(eventName);

            if (properties != null && properties.Count > 0)
            {
                Utils.CopyDictionary(properties, telemetry.Properties);
            }

            if (metrics != null && metrics.Count > 0)
            {
                Utils.CopyDictionary(metrics, telemetry.Metrics);
            }

            this.TrackEvent(telemetry);
        }

        /// <inheritdoc />
        public void TrackEvent(EventTelemetry telemetry)
        {
            if (telemetry == null)
            {
                telemetry = new EventTelemetry();
            }

            this.Track(telemetry);
        }

        /// <inheritdoc />
        public void TrackTrace(string message)
        {
            this.TrackTrace(new TraceTelemetry(message));
        }

        /// <inheritdoc />
        public void TrackTrace(string message, SeverityLevel severityLevel)
        {
            this.TrackTrace(new TraceTelemetry(message, severityLevel));
        }

        /// <inheritdoc />
        public void TrackTrace(string message, IDictionary<string, string> properties)
        {
            TraceTelemetry telemetry = new TraceTelemetry(message);

            if (properties != null && properties.Count > 0)
            {
                Utils.CopyDictionary(properties, telemetry.Properties);
            }

            this.TrackTrace(telemetry);
        }

        /// <inheritdoc />
        public void TrackTrace(string message, SeverityLevel severityLevel, IDictionary<string, string> properties)
        {
            TraceTelemetry telemetry = new TraceTelemetry(message, severityLevel);

            if (properties != null && properties.Count > 0)
            {
                Utils.CopyDictionary(properties, telemetry.Properties);
            }

            this.TrackTrace(telemetry);
        }

        /// <inheritdoc />
        public void TrackTrace(TraceTelemetry telemetry)
        {
            telemetry = telemetry ?? new TraceTelemetry();
            this.Track(telemetry);
        }

        /// <inheritdoc />        
        public void TrackMetric(string name, double value, IDictionary<string, string> properties = null)
        {
            var telemetry = new MetricTelemetry(name, value);
            if (properties != null && properties.Count > 0)
            {
                Utils.CopyDictionary(properties, telemetry.Properties);
            }

            this.TrackMetric(telemetry);
        }

        /// <inheritdoc />    
        public void TrackMetric(MetricTelemetry telemetry)
        {
            if (telemetry == null)
            {
                telemetry = new MetricTelemetry();
            }

            this.Track(telemetry);
        }

        /// <inheritdoc />
        public void TrackException(Exception exception, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            if (exception == null)
            {
                exception = new Exception(Utils.PopulateRequiredStringValue(null, "message", typeof(ExceptionTelemetry).FullName));
            }

            var telemetry = new ExceptionTelemetry(exception);

            if (properties != null && properties.Count > 0)
            {
                Utils.CopyDictionary(properties, telemetry.Properties);
            }

            if (metrics != null && metrics.Count > 0)
            {
                Utils.CopyDictionary(metrics, telemetry.Metrics);
            }

            this.TrackException(telemetry);
        }

        /// <inheritdoc />
        public void TrackException(ExceptionTelemetry telemetry)
        {
            if (telemetry == null)
            {
                var exception = new Exception(Utils.PopulateRequiredStringValue(null, "message", typeof(ExceptionTelemetry).FullName));
                telemetry = new ExceptionTelemetry(exception);
            }

            this.Track(telemetry);
        }

        /// <inheritdoc />
        [Obsolete("Please use a different overload of TrackDependency")]
        public void TrackDependency(string dependencyName, string data, DateTimeOffset startTime, TimeSpan duration, bool success)
        {
#pragma warning disable 618
            this.TrackDependency(new DependencyTelemetry(dependencyName, data, startTime, duration, success));
#pragma warning restore 618
        }

        /// <inheritdoc />
        public void TrackDependency(string dependencyTypeName, string dependencyName, string data, DateTimeOffset startTime, TimeSpan duration, bool success)
        {
            this.TrackDependency(new DependencyTelemetry(dependencyTypeName, null, dependencyName, data, startTime, duration, null, success));
        }

        /// <inheritdoc />
        public void TrackDependency(string dependencyTypeName, string target, string dependencyName, string data, DateTimeOffset startTime, TimeSpan duration, string resultCode, bool success)
        {
            this.TrackDependency(new DependencyTelemetry(dependencyTypeName, target, dependencyName, data, startTime, duration, resultCode, success));
        }

        /// <inheritdoc />
        public void TrackDependency(DependencyTelemetry telemetry)
        {
            if (telemetry == null)
            {
                telemetry = new DependencyTelemetry();
            }

            this.Track(telemetry);
        }

        /// <inheritdoc />
        public void TrackAvailability(string name, DateTimeOffset timeStamp, TimeSpan duration, string runLocation, bool success, string message = null, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            var availabilityTelemetry = new AvailabilityTelemetry(name, timeStamp, duration, runLocation, success, message);

            if (properties != null && properties.Count > 0)
            {
                Utils.CopyDictionary(properties, availabilityTelemetry.Properties);
            }

            if (metrics != null && metrics.Count > 0)
            {
                Utils.CopyDictionary(metrics, availabilityTelemetry.Metrics);
            }

            this.TrackAvailability(availabilityTelemetry);
        }

        /// <inheritdoc />
        public void TrackAvailability(AvailabilityTelemetry telemetry)
        {
            if (telemetry == null)
            {
                telemetry = new AvailabilityTelemetry();
            }

            this.Track(telemetry);
        }

        /// <summary>
        /// This method is an internal part of Application Insights infrastructure. Do not call.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Track(ITelemetry telemetry)
        {
            if (telemetry == null)
            {
                throw new ArgumentNullException(nameof(telemetry));
            }

            // TALK TO YOUR TEAM MATES BEFORE CHANGING THIS.
            // This method needs to be public so that we can build and ship new telemetry types without having to ship core.
            // It is hidden from intellisense to prevent customer confusion.
            if (this.IsEnabled())
            {
                this.Initialize(telemetry);

                telemetry.Context.ClearTempRawObjects();

                // invokes the Process in the first processor in the chain
                this.configuration.TelemetryProcessorChain.Process(telemetry);

                // logs rich payload ETW event for any partners to process it
                RichPayloadEventSource.Log.Process(telemetry);
            }
        }

        /// <summary>
        /// This method is an internal part of Application Insights infrastructure. Do not call.
        /// </summary>
        /// <param name="telemetry">Telemetry item to initialize instrumentation key.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void InitializeInstrumentationKey(ITelemetry telemetry)
        {
            if (telemetry == null)
            {
                throw new ArgumentNullException(nameof(telemetry));
            }

            string instrumentationKey = this.Context.InstrumentationKey;

            if (string.IsNullOrEmpty(instrumentationKey))
            {
                instrumentationKey = this.configuration.InstrumentationKey;
            }

            telemetry.Context.InitializeInstrumentationkey(instrumentationKey);
        }

        /// <summary>
        /// This method is an internal part of Application Insights infrastructure. Do not call.
        /// </summary>
        /// <param name="telemetry">Telemetry item to initialize.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry == null)
            {
                throw new ArgumentNullException(nameof(telemetry));
            }

            ISupportAdvancedSampling telemetryWithSampling = telemetry as ISupportAdvancedSampling;

            // Telemetry can be already sampled out if that decision was made before calling Track()
            bool sampledOut = false;
            if (telemetryWithSampling != null)
            {
                sampledOut = telemetryWithSampling.ProactiveSamplingDecision == SamplingDecision.SampledOut;
            }

            if (!sampledOut)
            {
                if (telemetry is ISupportProperties telemetryWithProperties)
                {
                    if (this.configuration.TelemetryChannel?.DeveloperMode != null && this.configuration.TelemetryChannel.DeveloperMode.Value)
                    {
                        if (!telemetryWithProperties.Properties.ContainsKey("DeveloperMode"))
                        {
                            telemetryWithProperties.Properties.Add("DeveloperMode", "true");
                        }
                    }
                }

                // Properties set of TelemetryClient's Context are copied over to that of ITelemetry's Context
#pragma warning disable CS0618 // Type or member is obsolete
                if (this.Context.PropertiesValue != null)
                {
                    Utils.CopyDictionary(this.Context.Properties, telemetry.Context.Properties);
                }

#pragma warning restore CS0618 // Type or member is obsolete

                // This check avoids accessing the public accessor GlobalProperties
                // unless needed, to avoid the penalty of ConcurrentDictionary instantiation.
                if (this.Context.GlobalPropertiesValue != null)
                {
                    Utils.CopyDictionary(this.Context.GlobalProperties, telemetry.Context.GlobalProperties);
                }

                string instrumentationKey = this.Context.InstrumentationKey;

                if (string.IsNullOrEmpty(instrumentationKey))
                {
                    instrumentationKey = this.configuration.InstrumentationKey;
                }

                telemetry.Context.Initialize(this.Context, instrumentationKey);

                for (int index = 0; index < this.configuration.TelemetryInitializers.Count; index++)
                {
                    try
                    {
                        this.configuration.TelemetryInitializers[index].Initialize(telemetry);
                    }
                    catch (Exception exception)
                    {
                        CoreEventSource.Log.LogError(string.Format(
                                                        CultureInfo.InvariantCulture,
                                                        "Exception while initializing {0}, exception message - {1}",
                                                        this.configuration.TelemetryInitializers[index].GetType().FullName,
                                                        exception));
                    }
                }

                if (telemetry.Timestamp == default(DateTimeOffset))
                {
                    telemetry.Timestamp = PreciseTimestamp.GetUtcNow();
                }

                // Currently backend requires SDK version to comply "name: version"
                if (string.IsNullOrEmpty(telemetry.Context.Internal.SdkVersion))
                {
                    var version = this.sdkVersion ?? (this.sdkVersion = SdkVersionUtils.GetSdkVersion(VersionPrefix));
                    telemetry.Context.Internal.SdkVersion = version;
                }

                // set NodeName to the machine name if it's not initialized yet, if RoleInstance is also not set then we send only RoleInstance
                if (string.IsNullOrEmpty(telemetry.Context.Internal.NodeName) && !string.IsNullOrEmpty(telemetry.Context.Cloud.RoleInstance))
                {
                    telemetry.Context.Internal.NodeName = PlatformSingleton.Current.GetMachineName();
                }

                // set RoleInstance to the machine name if it's not initialized yet
                if (string.IsNullOrEmpty(telemetry.Context.Cloud.RoleInstance))
                {
                    telemetry.Context.Cloud.RoleInstance = PlatformSingleton.Current.GetMachineName();
                }
            }
            else
            {
                CoreEventSource.Log.InitializationIsSkippedForSampledItem();
            }
        }

        /// <inheritdoc />
        public void TrackPageView(string name)
        {
            this.Track(new PageViewTelemetry(name));
        }

        /// <inheritdoc />
        public void TrackPageView(PageViewTelemetry telemetry)
        {
            if (telemetry == null)
            {
                telemetry = new PageViewTelemetry();
            }

            this.Track(telemetry);
        }

        /// <inheritdoc />
        public void TrackRequest(string name, DateTimeOffset startTime, TimeSpan duration, string responseCode, bool success)
        {
            this.Track(new RequestTelemetry(name, startTime, duration, responseCode, success));
        }

        /// <inheritdoc />
        public void TrackRequest(RequestTelemetry request)
        {
            if (request == null)
            {
                request = new RequestTelemetry();
            }

            this.Track(request);
        }

        /// <inheritdoc />
        public void Flush()
        {
            CoreEventSource.Log.TelemetlyClientFlush();

            if (this.TryGetMetricManager(out MetricManager privateMetricManager))
            {
                privateMetricManager.Flush(flushDownstreamPipeline: false);
            }

            TelemetryConfiguration pipeline = this.configuration;
            if (pipeline != null)
            {
                MetricManager sharedMetricManager = pipeline.GetMetricManager(createIfNotExists: false);
                sharedMetricManager?.Flush(flushDownstreamPipeline: false);

                ITelemetryChannel channel = pipeline.TelemetryChannel;
                channel?.Flush();
            }
        }

        /// <inheritdoc />
        /// TODO: Metrics flush to respect CancellationToken.
        public Task<bool> FlushAsync(CancellationToken cancellationToken)
        {
            if (this.TryGetMetricManager(out MetricManager privateMetricManager))
            {
                privateMetricManager.Flush(flushDownstreamPipeline: false);
            }

            TelemetryConfiguration pipeline = this.configuration;
            if (pipeline != null)
            {
                MetricManager sharedMetricManager = pipeline.GetMetricManager(createIfNotExists: false);
                sharedMetricManager?.Flush(flushDownstreamPipeline: false);

                ITelemetryChannel channel = pipeline.TelemetryChannel;

                if (channel is IAsyncFlushable asyncFlushableChannel && !cancellationToken.IsCancellationRequested)
                {
                    return asyncFlushableChannel.FlushAsync(cancellationToken);
                }
            }

            return cancellationToken.IsCancellationRequested ? TaskEx.FromCanceled<bool>(cancellationToken) : Task.FromResult(false);
        }

        /// <inheritdoc />
        public Metric GetMetric(
                            string metricId)
        {
            return this.GetOrCreateMetric(
                        MetricAggregationScope.TelemetryConfiguration,
                        new MetricIdentifier(metricId),
                        metricConfiguration: null);
        }

        /// <inheritdoc />
        public Metric GetMetric(
                            string metricId,
                            MetricConfiguration metricConfiguration)
        {
            return this.GetOrCreateMetric(
                        MetricAggregationScope.TelemetryConfiguration,
                        new MetricIdentifier(metricId),
                        metricConfiguration: metricConfiguration);
        }

        /// <inheritdoc />
        public Metric GetMetric(
                            string metricId,
                            MetricConfiguration metricConfiguration,
                            MetricAggregationScope aggregationScope)
        {
            return this.GetOrCreateMetric(
                        aggregationScope,
                        new MetricIdentifier(metricId),
                        metricConfiguration: metricConfiguration);
        }

        /// <inheritdoc />
        public Metric GetMetric(
                            string metricId,
                            string dimension1Name)
        {
            return this.GetOrCreateMetric(
                        MetricAggregationScope.TelemetryConfiguration,
                        new MetricIdentifier(MetricIdentifier.DefaultMetricNamespace, metricId, dimension1Name),
                        metricConfiguration: null);
        }

        /// <inheritdoc />
        public Metric GetMetric(
                            string metricId,
                            string dimension1Name,
                            MetricConfiguration metricConfiguration)
        {
            return this.GetOrCreateMetric(
                        MetricAggregationScope.TelemetryConfiguration,
                        new MetricIdentifier(MetricIdentifier.DefaultMetricNamespace, metricId, dimension1Name),
                        metricConfiguration: metricConfiguration);
        }

        /// <inheritdoc />
        public Metric GetMetric(
                            string metricId,
                            string dimension1Name,
                            MetricConfiguration metricConfiguration,
                            MetricAggregationScope aggregationScope)
        {
            return this.GetOrCreateMetric(
                        aggregationScope,
                        new MetricIdentifier(MetricIdentifier.DefaultMetricNamespace, metricId, dimension1Name),
                        metricConfiguration: metricConfiguration);
        }

        /// <inheritdoc />
        public Metric GetMetric(
                            string metricId,
                            string dimension1Name,
                            string dimension2Name)
        {
            return this.GetOrCreateMetric(
                        MetricAggregationScope.TelemetryConfiguration,
                        new MetricIdentifier(MetricIdentifier.DefaultMetricNamespace, metricId, dimension1Name, dimension2Name),
                        metricConfiguration: null);
        }

        /// <inheritdoc />
        public Metric GetMetric(
                            string metricId,
                            string dimension1Name,
                            string dimension2Name,
                            MetricConfiguration metricConfiguration)
        {
            return this.GetOrCreateMetric(
                        MetricAggregationScope.TelemetryConfiguration,
                        new MetricIdentifier(MetricIdentifier.DefaultMetricNamespace, metricId, dimension1Name, dimension2Name),
                        metricConfiguration);
        }

        /// <inheritdoc />
        public Metric GetMetric(
                            string metricId,
                            string dimension1Name,
                            string dimension2Name,
                            MetricConfiguration metricConfiguration,
                            MetricAggregationScope aggregationScope)
        {
            return this.GetOrCreateMetric(
                        aggregationScope,
                        new MetricIdentifier(MetricIdentifier.DefaultMetricNamespace, metricId, dimension1Name, dimension2Name),
                        metricConfiguration);
        }

        /// <inheritdoc />
        public Metric GetMetric(
                            string metricId,
                            string dimension1Name,
                            string dimension2Name,
                            string dimension3Name)
        {
            return this.GetOrCreateMetric(
                        MetricAggregationScope.TelemetryConfiguration,
                        new MetricIdentifier(MetricIdentifier.DefaultMetricNamespace, metricId, dimension1Name, dimension2Name, dimension3Name),
                        metricConfiguration: null);
        }

        /// <inheritdoc />
        public Metric GetMetric(
                            string metricId,
                            string dimension1Name,
                            string dimension2Name,
                            string dimension3Name,
                            MetricConfiguration metricConfiguration)
        {
            return this.GetOrCreateMetric(
                        MetricAggregationScope.TelemetryConfiguration,
                        new MetricIdentifier(MetricIdentifier.DefaultMetricNamespace, metricId, dimension1Name, dimension2Name, dimension3Name),
                        metricConfiguration);
        }

        /// <inheritdoc />
        public Metric GetMetric(
                            string metricId,
                            string dimension1Name,
                            string dimension2Name,
                            string dimension3Name,
                            MetricConfiguration metricConfiguration,
                            MetricAggregationScope aggregationScope)
        {
            return this.GetOrCreateMetric(
                        aggregationScope,
                        new MetricIdentifier(MetricIdentifier.DefaultMetricNamespace, metricId, dimension1Name, dimension2Name, dimension3Name),
                        metricConfiguration);
        }

        /// <inheritdoc />
        public Metric GetMetric(
                            string metricId,
                            string dimension1Name,
                            string dimension2Name,
                            string dimension3Name,
                            string dimension4Name)
        {
            return this.GetOrCreateMetric(
                        MetricAggregationScope.TelemetryConfiguration,
                        new MetricIdentifier(MetricIdentifier.DefaultMetricNamespace, metricId, dimension1Name, dimension2Name, dimension3Name, dimension4Name),
                        metricConfiguration: null);
        }

        /// <inheritdoc />
        public Metric GetMetric(
                            string metricId,
                            string dimension1Name,
                            string dimension2Name,
                            string dimension3Name,
                            string dimension4Name,
                            MetricConfiguration metricConfiguration)
        {
            return this.GetOrCreateMetric(
                        MetricAggregationScope.TelemetryConfiguration,
                        new MetricIdentifier(MetricIdentifier.DefaultMetricNamespace, metricId, dimension1Name, dimension2Name, dimension3Name, dimension4Name),
                        metricConfiguration);
        }

        /// <inheritdoc />
        public Metric GetMetric(
                            string metricId,
                            string dimension1Name,
                            string dimension2Name,
                            string dimension3Name,
                            string dimension4Name,
                            MetricConfiguration metricConfiguration,
                            MetricAggregationScope aggregationScope)
        {
            return this.GetOrCreateMetric(
                        aggregationScope,
                        new MetricIdentifier(MetricIdentifier.DefaultMetricNamespace, metricId, dimension1Name, dimension2Name, dimension3Name, dimension4Name),
                        metricConfiguration);
        }

        /// <inheritdoc />
        public Metric GetMetric(
                            MetricIdentifier metricIdentifier)
        {
            return this.GetOrCreateMetric(
                        MetricAggregationScope.TelemetryConfiguration,
                        metricIdentifier,
                        metricConfiguration: null);
        }

        /// <inheritdoc />
        public Metric GetMetric(
                            MetricIdentifier metricIdentifier,
                            MetricConfiguration metricConfiguration)
        {
            return this.GetOrCreateMetric(
                        MetricAggregationScope.TelemetryConfiguration,
                        metricIdentifier,
                        metricConfiguration);
        }

        /// <inheritdoc />
        public Metric GetMetric(
                            MetricIdentifier metricIdentifier,
                            MetricConfiguration metricConfiguration,
                            MetricAggregationScope aggregationScope)
        {
            return this.GetOrCreateMetric(
                        aggregationScope,
                        metricIdentifier,
                        metricConfiguration);
        }

        private Metric GetOrCreateMetric(
                                    MetricAggregationScope aggregationScope,
                                    MetricIdentifier metricIdentifier,
                                    MetricConfiguration metricConfiguration)
        {
            MetricManager metricManager = this.GetMetricManager(aggregationScope);
            Metric metric = metricManager.Metrics.GetOrCreate(metricIdentifier, metricConfiguration);
            return metric;
        }
    }
}
