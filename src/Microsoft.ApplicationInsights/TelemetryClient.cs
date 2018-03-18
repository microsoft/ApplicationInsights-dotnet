namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Platform;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Metrics;
    using Microsoft.ApplicationInsights.Metrics.Extensibility;

    /// <summary>
    /// Send events, metrics and other telemetry to the Application Insights service.
    /// <a href="https://go.microsoft.com/fwlink/?linkid=525722">Learn more</a>
    /// </summary>
    public sealed class TelemetryClient
    {
        private const string VersionPrefix = "dotnet:";
        private readonly TelemetryConfiguration configuration;
        private TelemetryContext context;
        private string sdkVersion;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryClient" /> class. Send telemetry with the active configuration, usually loaded from ApplicationInsights.config.
        /// </summary>
        public TelemetryClient() : this(TelemetryConfiguration.Active)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryClient" /> class. Send telemetry with the specified <paramref name="configuration"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">The <paramref name="configuration"/> is null.</exception>
        /// <exception cref="ArgumentException">The <paramref name="configuration"/> does not contain a telemetry channel.</exception>
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
                throw new ArgumentException("The specified configuration does not have a telemetry channel.", "configuration");
            }
        }

        /// <summary>
        /// Gets the current context that will be used to augment telemetry you send.
        /// </summary>
        public TelemetryContext Context
        {
            get { return LazyInitializer.EnsureInitialized(ref this.context, () => new TelemetryContext()); }
            internal set { this.context = value; }
        }

        /// <summary>
        /// Gets or sets the default instrumentation key for all <see cref="ITelemetry"/> objects logged in this <see cref="TelemetryClient"/>.
        /// </summary>
        public string InstrumentationKey
        {
            get { return this.Context.InstrumentationKey; }
            set { this.Context.InstrumentationKey = value; }
        }

        /// <summary>
        /// Gets the <see cref="TelemetryConfiguration"/> object associated with this telemetry client instance.
        /// </summary>
        internal TelemetryConfiguration TelemetryConfiguration
        {
            get { return this.configuration; }
        }

        /// <summary>
        /// Check to determine if the tracking is enabled.
        /// </summary>
        public bool IsEnabled()
        {
            return !this.configuration.DisableTelemetry;
        }

        /// <summary>
        /// Send an <see cref="EventTelemetry"/> for display in Diagnostic Search and in the Analytics Portal.
        /// </summary>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#trackevent">Learn more</a>
        /// </remarks>
        /// <param name="eventName">A name for the event.</param>
        /// <param name="properties">Named string values you can use to search and classify events.</param>
        /// <param name="metrics">Measurements associated with this event.</param>
        public void TrackEvent(string eventName, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            var telemetry = new EventTelemetry(eventName);

            if (properties != null && properties.Count > 0)
            {
                Utils.CopyDictionary(properties, telemetry.Context.Properties);
            }

            if (metrics != null && metrics.Count > 0)
            {
                Utils.CopyDictionary(metrics, telemetry.Metrics);
            }

            this.TrackEvent(telemetry);
        }

        /// <summary>
        /// Send an <see cref="EventTelemetry"/> for display in Diagnostic Search and in the Analytics Portal.
        /// Create a separate <see cref="EventTelemetry"/> instance for each call to <see cref="TrackEvent(EventTelemetry)"/>.
        /// </summary>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#trackevent">Learn more</a>
        /// </remarks>
        /// <param name="telemetry">An event log item.</param>
        public void TrackEvent(EventTelemetry telemetry)
        {
            if (telemetry == null)
            {
                telemetry = new EventTelemetry();
            }

            this.Track(telemetry);
        }

        /// <summary>
        /// Send a trace message for display in Diagnostic Search.
        /// </summary>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#tracktrace">Learn more</a>
        /// </remarks>
        /// <param name="message">Message to display.</param>
        public void TrackTrace(string message)
        {
            this.TrackTrace(new TraceTelemetry(message));
        }

        /// <summary>
        /// Send a trace message for display in Diagnostic Search.
        /// </summary>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#tracktrace">Learn more</a>
        /// </remarks>
        /// <param name="message">Message to display.</param>
        /// <param name="severityLevel">Trace severity level.</param>
        public void TrackTrace(string message, SeverityLevel severityLevel)
        {
            this.TrackTrace(new TraceTelemetry(message, severityLevel));
        }

        /// <summary>
        /// Send a trace message for display in Diagnostic Search.
        /// </summary>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#tracktrace">Learn more</a>
        /// </remarks>
        /// <param name="message">Message to display.</param>
        /// <param name="properties">Named string values you can use to search and classify events.</param>
        public void TrackTrace(string message, IDictionary<string, string> properties)
        {
            TraceTelemetry telemetry = new TraceTelemetry(message);

            if (properties != null && properties.Count > 0)
            {
                Utils.CopyDictionary(properties, telemetry.Context.Properties);
            }

            this.TrackTrace(telemetry);
        }

        /// <summary>
        /// Send a trace message for display in Diagnostic Search.
        /// </summary>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#tracktrace">Learn more</a>
        /// </remarks>
        /// <param name="message">Message to display.</param>
        /// <param name="severityLevel">Trace severity level.</param>
        /// <param name="properties">Named string values you can use to search and classify events.</param>
        public void TrackTrace(string message, SeverityLevel severityLevel, IDictionary<string, string> properties)
        {
            TraceTelemetry telemetry = new TraceTelemetry(message, severityLevel);

            if (properties != null && properties.Count > 0)
            {
                Utils.CopyDictionary(properties, telemetry.Context.Properties);
            }

            this.TrackTrace(telemetry);
        }

        /// <summary>
        /// Send a trace message for display in Diagnostic Search.
        /// Create a separate <see cref="TraceTelemetry"/> instance for each call to <see cref="TrackTrace(TraceTelemetry)"/>.
        /// </summary>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#tracktrace">Learn more</a>
        /// </remarks>
        /// <param name="telemetry">Message with optional properties.</param>
        public void TrackTrace(TraceTelemetry telemetry)
        {
            telemetry = telemetry ?? new TraceTelemetry();
            this.Track(telemetry);
        }

        /// <summary>
        /// This method is deprecated. Metrics should always be pre-aggregated across a time period before being sent.<br />
        /// Use one of the <c>GetMetric(..)</c> overloads to get a metric object for accessing SDK pre-aggregation capabilities.<br />
        /// If you are implementing your own pre-aggregation logic, you can use the <c>Track(ITelemetry metricTelemetry)</c> method to
        /// send the resulting aggregates.<br />
        /// If your application requires sending a separate telemetry item at every occasion without aggregation across time,
        /// you likely have a use case for event telemetry; see <see cref="TrackEvent(EventTelemetry)"/>.
        /// </summary>
        /// <param name="name">Metric name.</param>
        /// <param name="value">Metric value.</param>
        /// <param name="properties">Named string values you can use to classify and filter metrics.</param>
        [Obsolete("Use GetMetric(..) to use SDK pre-aggregation capabilities or Track(ITelemetry metricTelemetry) if you performed your own local aggregation.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void TrackMetric(string name, double value, IDictionary<string, string> properties = null)
        {
            var telemetry = new MetricTelemetry(name, value);
            if (properties != null && properties.Count > 0)
            {
                Utils.CopyDictionary(properties, telemetry.Properties);
            }

            this.TrackMetric(telemetry);
        }

        /// <summary>
        /// This method is deprecated. Metrics should always be pre-aggregated across a time period before being sent.<br />
        /// Use one of the <c>GetMetric(..)</c> overloads to get a metric object for accessing SDK pre-aggregation capabilities.<br />
        /// If you are implementing your own pre-aggregation logic, you can use the <c>Track(ITelemetry metricTelemetry)</c> method to
        /// send the resulting aggregates.<br />
        /// If your application requires sending a separate telemetry item at every occasion without aggregation across time,
        /// you likely have a use case for event telemetry; see <see cref="TrackEvent(EventTelemetry)"/>.
        /// </summary>
        /// <param name="telemetry">The metric telemetry item.</param>
        [Obsolete("Use GetMetric(..) to use SDK pre-aggregation capabilities or Track(ITelemetry metricTelemetry) if you performed your own local aggregation.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void TrackMetric(MetricTelemetry telemetry)
        {
            if (telemetry == null)
            {
                telemetry = new MetricTelemetry();
            }

            this.Track(telemetry);
        }

        /// <summary>
        /// Send an <see cref="ExceptionTelemetry"/> for display in Diagnostic Search.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="properties">Named string values you can use to classify and search for this exception.</param>
        /// <param name="metrics">Additional values associated with this exception.</param>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#trackexception">Learn more</a>
        /// </remarks>
        public void TrackException(Exception exception, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            if (exception == null)
            {
                exception = new Exception(Utils.PopulateRequiredStringValue(null, "message", typeof(ExceptionTelemetry).FullName));
            }

            var telemetry = new ExceptionTelemetry(exception);

            if (properties != null && properties.Count > 0)
            {
                Utils.CopyDictionary(properties, telemetry.Context.Properties);
            }

            if (metrics != null && metrics.Count > 0)
            {
                Utils.CopyDictionary(metrics, telemetry.Metrics);
            }

            this.TrackException(telemetry);
        }

        /// <summary>
        /// Send an <see cref="ExceptionTelemetry"/> for display in Diagnostic Search.
        /// Create a separate <see cref="ExceptionTelemetry"/> instance for each call to <see cref="TrackException(ExceptionTelemetry)"/>
        /// </summary>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#trackexception">Learn more</a>
        /// </remarks>
        public void TrackException(ExceptionTelemetry telemetry)
        {
            if (telemetry == null)
            {
                var exception = new Exception(Utils.PopulateRequiredStringValue(null, "message", typeof(ExceptionTelemetry).FullName));
                telemetry = new ExceptionTelemetry(exception);
            }

            this.Track(telemetry);
        }

        /// <summary>
        /// Send information about an external dependency (outgoing call) in the application.
        /// </summary>
        /// <param name="dependencyName">Name of the command initiated with this dependency call. Low cardinality value. Examples are stored procedure name and URL path template.</param>
        /// <param name="data">Command initiated by this dependency call. Examples are SQL statement and HTTP URL's with all query parameters.</param>
        /// <param name="startTime">The time when the dependency was called.</param>
        /// <param name="duration">The time taken by the external dependency to handle the call.</param>
        /// <param name="success">True if the dependency call was handled successfully.</param>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#trackdependency">Learn more</a>
        /// </remarks>
        [Obsolete]
        public void TrackDependency(string dependencyName, string data, DateTimeOffset startTime, TimeSpan duration, bool success)
        {
#pragma warning disable 618
            this.TrackDependency(new DependencyTelemetry(dependencyName, data, startTime, duration, success));
#pragma warning restore 618
        }

        /// <summary>
        /// Send information about an external dependency (outgoing call) in the application.
        /// </summary>
        /// <param name="dependencyTypeName">External dependency type. Very low cardinality value for logical grouping and interpretation of fields. Examples are SQL, Azure table, and HTTP.</param>
        /// <param name="dependencyName">Name of the command initiated with this dependency call. Low cardinality value. Examples are stored procedure name and URL path template.</param>
        /// <param name="data">Command initiated by this dependency call. Examples are SQL statement and HTTP URL's with all query parameters.</param>
        /// <param name="startTime">The time when the dependency was called.</param>
        /// <param name="duration">The time taken by the external dependency to handle the call.</param>
        /// <param name="success">True if the dependency call was handled successfully.</param>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#trackdependency">Learn more</a>
        /// </remarks>
        public void TrackDependency(string dependencyTypeName, string dependencyName, string data, DateTimeOffset startTime, TimeSpan duration, bool success)
        {
            this.TrackDependency(new DependencyTelemetry(dependencyTypeName, null, dependencyName, data, startTime, duration, null, success));
        }

        /// <summary>
        /// Send information about an external dependency (outgoing call) in the application.
        /// </summary>
        /// <param name="dependencyTypeName">External dependency type. Very low cardinality value for logical grouping and interpretation of fields. Examples are SQL, Azure table, and HTTP.</param>
        /// <param name="target">External dependency target.</param>
        /// <param name="dependencyName">Name of the command initiated with this dependency call. Low cardinality value. Examples are stored procedure name and URL path template.</param>
        /// <param name="data">Command initiated by this dependency call. Examples are SQL statement and HTTP URL's with all query parameters.</param>
        /// <param name="startTime">The time when the dependency was called.</param>
        /// <param name="duration">The time taken by the external dependency to handle the call.</param>
        /// <param name="resultCode">Result code of dependency call execution.</param>
        /// <param name="success">True if the dependency call was handled successfully.</param>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#trackdependency">Learn more</a>
        /// </remarks>
        public void TrackDependency(string dependencyTypeName, string target, string dependencyName, string data, DateTimeOffset startTime, TimeSpan duration, string resultCode, bool success)
        {
            this.TrackDependency(new DependencyTelemetry(dependencyTypeName, target, dependencyName, data, startTime, duration, resultCode, success));
        }

        /// <summary>
        /// Send information about external dependency call in the application.
        /// Create a separate <see cref="DependencyTelemetry"/> instance for each call to <see cref="TrackDependency(DependencyTelemetry)"/>
        /// </summary>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#trackdependency">Learn more</a>
        /// </remarks>
        public void TrackDependency(DependencyTelemetry telemetry)
        {
            if (telemetry == null)
            {
                telemetry = new DependencyTelemetry();
            }

            this.Track(telemetry);
        }

        /// <summary>
        /// Send information about availability of an application.
        /// </summary>
        /// <param name="name">Availability test name.</param>
        /// <param name="timeStamp">The time when the availability was captured.</param>
        /// <param name="duration">The time taken for the availability test to run.</param>
        /// <param name="runLocation">Name of the location the availability test was run from.</param>
        /// <param name="success">True if the availability test ran successfully.</param>
        /// <param name="message">Error message on availability test run failure.</param>
        /// <param name="properties">Named string values you can use to classify and search for this availability telemetry.</param>
        /// <param name="metrics">Additional values associated with this availability telemetry.</param>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=517889">Learn more</a>
        /// </remarks>
        public void TrackAvailability(string name, DateTimeOffset timeStamp, TimeSpan duration, string runLocation, bool success, string message = null, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            var availabilityTelemetry = new AvailabilityTelemetry(name, timeStamp, duration, runLocation, success, message);

            if (properties != null && properties.Count > 0)
            {
                Utils.CopyDictionary(properties, availabilityTelemetry.Context.Properties);
            }

            if (metrics != null && metrics.Count > 0)
            {
                Utils.CopyDictionary(metrics, availabilityTelemetry.Metrics);
            }

            this.TrackAvailability(availabilityTelemetry);
        }

        /// <summary>
        /// Send information about availability of an application.
        /// Create a separate <see cref="AvailabilityTelemetry"/> instance for each call to <see cref="TrackAvailability(AvailabilityTelemetry)"/>
        /// </summary>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=517889">Learn more</a>
        /// </remarks>
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
            // TALK TO YOUR TEAM MATES BEFORE CHANGING THIS.
            // This method needs to be public so that we can build and ship new telemetry types without having to ship core.
            // It is hidden from intellisense to prevent customer confusion.
            if (this.IsEnabled())
            {
                this.Initialize(telemetry);

                // invokes the Process in the first processor in the chain
                this.configuration.TelemetryProcessorChain.Process(telemetry);

                // logs rich payload ETW event for any partners to process it
                RichPayloadEventSource.Log.Process(telemetry);
            }
        }

        /// <summary>
        /// This method is an internal part of Application Insights infrastructure. Do not call.
        /// </summary>
        /// <param name="telemetry">Telemetry item to initialize.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Initialize(ITelemetry telemetry)
        {
            string instrumentationKey = this.Context.InstrumentationKey;

            if (string.IsNullOrEmpty(instrumentationKey))
            {
                instrumentationKey = this.configuration.InstrumentationKey;
            }

            var telemetryWithProperties = telemetry as ISupportProperties;
            if (telemetryWithProperties != null)
            {
                if ((this.configuration.TelemetryChannel != null) && (this.configuration.TelemetryChannel.DeveloperMode.HasValue && this.configuration.TelemetryChannel.DeveloperMode.Value))
                {
                    if (!telemetryWithProperties.Properties.ContainsKey("DeveloperMode"))
                    {
                        telemetryWithProperties.Properties.Add("DeveloperMode", "true");
                    }
                }

                Utils.CopyDictionary(this.Context.Properties, telemetryWithProperties.Properties);
            }

            telemetry.Context.Initialize(this.Context, instrumentationKey);
            foreach (ITelemetryInitializer initializer in this.configuration.TelemetryInitializers)
            {
                try
                {
                    initializer.Initialize(telemetry);
                }
                catch (Exception exception)
                {
                    CoreEventSource.Log.LogError(string.Format(
                                                    CultureInfo.InvariantCulture,
                                                    "Exception while initializing {0}, exception message - {1}",
                                                    initializer.GetType().FullName,
                                                    exception));
                }
            }

            if (telemetry.Timestamp == default(DateTimeOffset))
            {
                telemetry.Timestamp = DateTimeOffset.UtcNow;
            }

            // Currently backend requires SDK version to comply "name: version"
            if (string.IsNullOrEmpty(telemetry.Context.Internal.SdkVersion))
            {
                var version = LazyInitializer.EnsureInitialized(ref this.sdkVersion, () => SdkVersionUtils.GetSdkVersion(VersionPrefix));
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

        /// <summary>
        /// Send information about the page viewed in the application.
        /// </summary>
        /// <param name="name">Name of the page.</param>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#page-views">Learn more</a>
        /// </remarks>
        public void TrackPageView(string name)
        {
            this.Track(new PageViewTelemetry(name));
        }

        /// <summary>
        /// Send information about the page viewed in the application.
        /// Create a separate <see cref="PageViewTelemetry"/> instance for each call to <see cref="TrackPageView(PageViewTelemetry)"/>.
        /// </summary>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#page-views">Learn more</a>
        /// </remarks>
        public void TrackPageView(PageViewTelemetry telemetry)
        {
            if (telemetry == null)
            {
                telemetry = new PageViewTelemetry();
            }

            this.Track(telemetry);
        }

        /// <summary>
        /// Send information about a request handled by the application.
        /// </summary>
        /// <param name="name">The request name.</param>
        /// <param name="startTime">The time when the page was requested.</param>
        /// <param name="duration">The time taken by the application to handle the request.</param>
        /// <param name="responseCode">The response status code.</param>
        /// <param name="success">True if the request was handled successfully by the application.</param>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#trackrequest">Learn more</a>
        /// </remarks>
        public void TrackRequest(string name, DateTimeOffset startTime, TimeSpan duration, string responseCode, bool success)
        {
            this.Track(new RequestTelemetry(name, startTime, duration, responseCode, success));
        }

        /// <summary>
        /// Send information about a request handled by the application.
        /// Create a separate <see cref="RequestTelemetry"/> instance for each call to <see cref="TrackRequest(RequestTelemetry)"/>.
        /// </summary>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#trackrequest">Learn more</a>
        /// </remarks>
        public void TrackRequest(RequestTelemetry request)
        {
            if (request == null)
            {
                request = new RequestTelemetry();
            }

            this.Track(request);
        }

        /// <summary>
        /// Flushes the in-memory buffer.
        /// </summary>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#flushing-data">Learn more</a>
        /// </remarks>
        public void Flush()
        {
            this.configuration.TelemetryChannel.Flush();
        }

        /// <summary>
        /// Gets or creates a metric container that you can use to track, aggregate and send metric values.<br />
        /// Optionally specify a metric configuration to control how the tracked values are aggregated.
        /// </summary>
        /// <remarks>The aggregated values will be sent to the <c>TelemetryConfiguration</c>
        /// associated with this client.<br />
        /// The aggregation scope of the fetched<c>Metric</c> is <c>TelemetryConfiguration</c>; this
        /// means that all values tracked for a given metric ID and dimensions will be aggregated together
        /// across all clients that share the same <c>TelemetryConfiguration</c>.</remarks>
        /// <param name="metricId">The ID (name) of the metric.
        ///   (The namespace specified in <see cref="MetricIdentifier.DefaultMetricNamespace"/> will be used.
        ///   To specify another namespace, user an overload that takes a <c>MetricIdentifier</c> paramater instead.)</param>
        /// <returns>A <c>Metric</c> with the specified ID and dimensions. If you call this method several times
        /// with the same metric ID and dimensions for a given aggregation scope, you will receive the same
        /// instance of <c>Metric</c>.</returns>
        /// <exception cref="ArgumentException">If you previously created a metric with the same namespace, ID, dimensions
        /// and aggregation scope, but with a different configuration. When calling this method to get a previously
        /// created metric, you can simply avoid specifying any configuration (or specify null) to imply the
        /// configuration used earlier.</exception>
        public Metric GetMetric(
                            string metricId)
        {
            return GetOrCreateMetric(
                        MetricAggregationScope.TelemetryConfiguration,
                        new MetricIdentifier(metricId),
                        metricConfiguration: null);
        }

        /// <summary>
        /// Gets or creates a metric container that you can use to track, aggregate and send metric values.<br />
        /// Optionally specify a metric configuration to control how the tracked values are aggregated.
        /// </summary>
        /// <remarks>The aggregated values will be sent to the <c>TelemetryConfiguration</c>
        /// associated with this client.<br />
        /// The aggregation scope of the fetched<c>Metric</c> is <c>TelemetryConfiguration</c>; this
        /// means that all values tracked for a given metric ID and dimensions will be aggregated together
        /// across all clients that share the same <c>TelemetryConfiguration</c>.</remarks>
        /// <param name="metricId">The ID (name) of the metric.
        ///   (The namespace specified in <see cref="MetricIdentifier.DefaultMetricNamespace"/> will be used.
        ///   To specify another namespace, user an overload that takes a <c>MetricIdentifier</c> paramater instead.)</param>
        /// <param name="metricConfiguration">Determines how tracked values will be aggregated. <br />
        /// Use presets in <see cref="MetricConfigurations.Common"/> or specify your own settings. </param>
        /// <returns>A <c>Metric</c> with the specified ID and dimensions. If you call this method several times
        /// with the same metric ID and dimensions for a given aggregation scope, you will receive the same
        /// instance of <c>Metric</c>.</returns>
        /// <exception cref="ArgumentException">If you previously created a metric with the same namespace, ID, dimensions
        /// and aggregation scope, but with a different configuration. When calling this method to get a previously
        /// created metric, you can simply avoid specifying any configuration (or specify null) to imply the
        /// configuration used earlier.</exception>
        public Metric GetMetric(
                            string metricId,
                            MetricConfiguration metricConfiguration)
        {
            return GetOrCreateMetric(
                        MetricAggregationScope.TelemetryConfiguration,
                        new MetricIdentifier(metricId),
                        metricConfiguration: metricConfiguration);
        }

        /// <summary>
        /// Gets or creates a metric container that you can use to track, aggregate and send metric values.<br />
        /// Optionally specify a metric configuration to control how the tracked values are aggregated.
        /// </summary>
        /// <param name="metricId">The ID (name) of the metric.
        ///   (The namespace specified in <see cref="MetricIdentifier.DefaultMetricNamespace"/> will be used.
        ///   To specify another namespace, user an overload that takes a <c>MetricIdentifier</c> paramater instead.)</param>
        /// <param name="metricConfiguration">Determines how tracked values will be aggregated. <br />
        /// Use presets in <see cref="MetricConfigurations.Common"/> or specify your own settings. </param>
        /// <returns>A <c>Metric</c> with the specified ID and dimensions. If you call this method several times
        /// with the same metric ID and dimensions for a given aggregation scope, you will receive the same
        /// instance of <c>Metric</c>.</returns>
        /// <exception cref="ArgumentException">If you previously created a metric with the same namespace, ID, dimensions
        /// and aggregation scope, but with a different configuration. When calling this method to get a previously
        /// created metric, you can simply avoid specifying any configuration (or specify null) to imply the
        /// configuration used earlier.</exception>
        /// <param name="aggregationScope">The scope across which the values for the metric are to be aggregated in memory.
        /// See <see cref="MetricAggregationScope" /> for more info.</param>
        /// <returns></returns>
        public Metric GetMetric(
                            string metricId,
                            MetricConfiguration metricConfiguration,
                            MetricAggregationScope aggregationScope)
        {
            return GetOrCreateMetric(
                        aggregationScope,
                        new MetricIdentifier(metricId),
                        metricConfiguration: metricConfiguration);
        }

        /// <summary>
        /// Gets or creates a metric container that you can use to track, aggregate and send metric values.<br />
        /// Optionally specify a metric configuration to control how the tracked values are aggregated.
        /// </summary>
        /// <remarks>The aggregated values will be sent to the <c>TelemetryConfiguration</c>
        /// associated with this client.<br />
        /// The aggregation scope of the fetched<c>Metric</c> is <c>TelemetryConfiguration</c>; this
        /// means that all values tracked for a given metric ID and dimensions will be aggregated together
        /// across all clients that share the same <c>TelemetryConfiguration</c>.</remarks>
        /// <param name="metricId">The ID (name) of the metric.
        ///   (The namespace specified in <see cref="MetricIdentifier.DefaultMetricNamespace"/> will be used.
        ///   To specify another namespace, user an overload that takes a <c>MetricIdentifier</c> paramater instead.)</param>
        /// <param name="dimension1Name">The name of the first dimension.</param>
        /// <exception cref="ArgumentException">If you previously created a metric with the same namespace, ID, dimensions
        /// and aggregation scope, but with a different configuration. When calling this method to get a previously
        /// created metric, you can simply avoid specifying any configuration (or specify null) to imply the
        /// configuration used earlier.</exception>
        /// <returns></returns>
        public Metric GetMetric(
                            string metricId,
                            string dimension1Name)
        {
            return GetOrCreateMetric(
                        MetricAggregationScope.TelemetryConfiguration,
                        new MetricIdentifier(MetricIdentifier.DefaultMetricNamespace, metricId, dimension1Name),
                        metricConfiguration: null);
        }

        /// <summary>
        /// Gets or creates a metric container that you can use to track, aggregate and send metric values.<br />
        /// Optionally specify a metric configuration to control how the tracked values are aggregated.
        /// </summary>
        /// <remarks>The aggregated values will be sent to the <c>TelemetryConfiguration</c>
        /// associated with this client.<br />
        /// The aggregation scope of the fetched<c>Metric</c> is <c>TelemetryConfiguration</c>; this
        /// means that all values tracked for a given metric ID and dimensions will be aggregated together
        /// across all clients that share the same <c>TelemetryConfiguration</c>.</remarks>
        /// <param name="metricId">The ID (name) of the metric.
        ///   (The namespace specified in <see cref="MetricIdentifier.DefaultMetricNamespace"/> will be used.
        ///   To specify another namespace, user an overload that takes a <c>MetricIdentifier</c> paramater instead.)</param>
        /// <param name="dimension1Name">The name of the first dimension.</param>
        /// <param name="metricConfiguration">Determines how tracked values will be aggregated. <br />
        /// Use presets in <see cref="MetricConfigurations.Common"/> or specify your own settings. </param>
        /// <returns>A <c>Metric</c> with the specified ID and dimensions. If you call this method several times
        /// with the same metric ID and dimensions for a given aggregation scope, you will receive the same
        /// instance of <c>Metric</c>.</returns>
        /// <exception cref="ArgumentException">If you previously created a metric with the same namespace, ID, dimensions
        /// and aggregation scope, but with a different configuration. When calling this method to get a previously
        /// created metric, you can simply avoid specifying any configuration (or specify null) to imply the
        /// configuration used earlier.</exception>
        public Metric GetMetric(
                            string metricId,
                            string dimension1Name,
                            MetricConfiguration metricConfiguration)
        {
            return GetOrCreateMetric(
                        MetricAggregationScope.TelemetryConfiguration,
                        new MetricIdentifier(MetricIdentifier.DefaultMetricNamespace, metricId, dimension1Name),
                        metricConfiguration: metricConfiguration);
        }

        /// <summary>
        /// Gets or creates a metric container that you can use to track, aggregate and send metric values.<br />
        /// Optionally specify a metric configuration to control how the tracked values are aggregated.
        /// </summary>
        /// <param name="metricId">The ID (name) of the metric.
        ///   (The namespace specified in <see cref="MetricIdentifier.DefaultMetricNamespace"/> will be used.
        ///   To specify another namespace, user an overload that takes a <c>MetricIdentifier</c> paramater instead.)</param>
        /// <param name="dimension1Name">The name of the first dimension.</param>
        /// <param name="metricConfiguration">Determines how tracked values will be aggregated. <br />
        /// Use presets in <see cref="MetricConfigurations.Common"/> or specify your own settings. </param>
        /// <returns>A <c>Metric</c> with the specified ID and dimensions. If you call this method several times
        /// with the same metric ID and dimensions for a given aggregation scope, you will receive the same
        /// instance of <c>Metric</c>.</returns>
        /// <exception cref="ArgumentException">If you previously created a metric with the same namespace, ID, dimensions
        /// and aggregation scope, but with a different configuration. When calling this method to get a previously
        /// created metric, you can simply avoid specifying any configuration (or specify null) to imply the
        /// configuration used earlier.</exception>
        /// <param name="aggregationScope">The scope across which the values for the metric are to be aggregated in memory.
        /// See <see cref="MetricAggregationScope" /> for more info.</param>
        public Metric GetMetric(
                            string metricId,
                            string dimension1Name,
                            MetricConfiguration metricConfiguration,
                            MetricAggregationScope aggregationScope)
        {
            return GetOrCreateMetric(
                        aggregationScope,
                        new MetricIdentifier(MetricIdentifier.DefaultMetricNamespace, metricId, dimension1Name),
                        metricConfiguration: metricConfiguration);
        }

        /// <summary>
        /// Gets or creates a metric container that you can use to track, aggregate and send metric values.<br />
        /// Optionally specify a metric configuration to control how the tracked values are aggregated.
        /// </summary>
        /// <remarks>The aggregated values will be sent to the <c>TelemetryConfiguration</c>
        /// associated with this client.<br />
        /// The aggregation scope of the fetched<c>Metric</c> is <c>TelemetryConfiguration</c>; this
        /// means that all values tracked for a given metric ID and dimensions will be aggregated together
        /// across all clients that share the same <c>TelemetryConfiguration</c>.</remarks>
        /// <param name="metricId">The ID (name) of the metric.
        ///   (The namespace specified in <see cref="MetricIdentifier.DefaultMetricNamespace"/> will be used.
        ///   To specify another namespace, user an overload that takes a <c>MetricIdentifier</c> paramater instead.)</param>
        /// <param name="dimension1Name">The name of the first dimension.</param>
        /// <param name="dimension2Name">The name of the second dimension.</param>
        /// <exception cref="ArgumentException">If you previously created a metric with the same namespace, ID, dimensions
        /// and aggregation scope, but with a different configuration. When calling this method to get a previously
        /// created metric, you can simply avoid specifying any configuration (or specify null) to imply the
        /// configuration used earlier.</exception>
        /// <returns></returns>
        public Metric GetMetric(
                            string metricId,
                            string dimension1Name,
                            string dimension2Name)
        {
            return GetOrCreateMetric(
                        MetricAggregationScope.TelemetryConfiguration,
                        new MetricIdentifier(MetricIdentifier.DefaultMetricNamespace, metricId, dimension1Name, dimension2Name),
                        metricConfiguration: null);
        }

        /// <summary>
        /// Gets or creates a metric container that you can use to track, aggregate and send metric values.<br />
        /// Optionally specify a metric configuration to control how the tracked values are aggregated.
        /// </summary>
        /// <remarks>The aggregated values will be sent to the <c>TelemetryConfiguration</c>
        /// associated with this client.<br />
        /// The aggregation scope of the fetched<c>Metric</c> is <c>TelemetryConfiguration</c>; this
        /// means that all values tracked for a given metric ID and dimensions will be aggregated together
        /// across all clients that share the same <c>TelemetryConfiguration</c>.</remarks>
        /// <param name="metricId">The ID (name) of the metric.
        ///   (The namespace specified in <see cref="MetricIdentifier.DefaultMetricNamespace"/> will be used.
        ///   To specify another namespace, user an overload that takes a <c>MetricIdentifier</c> paramater instead.)</param>
        /// <param name="dimension1Name">The name of the first dimension.</param>
        /// <param name="dimension2Name">The name of the second dimension.</param>
        /// <param name="metricConfiguration">Determines how tracked values will be aggregated. <br />
        /// Use presets in <see cref="MetricConfigurations.Common"/> or specify your own settings. </param>
        /// <returns>A <c>Metric</c> with the specified ID and dimensions. If you call this method several times
        /// with the same metric ID and dimensions for a given aggregation scope, you will receive the same
        /// instance of <c>Metric</c>.</returns>
        /// <exception cref="ArgumentException">If you previously created a metric with the same namespace, ID, dimensions
        /// and aggregation scope, but with a different configuration. When calling this method to get a previously
        /// created metric, you can simply avoid specifying any configuration (or specify null) to imply the
        /// configuration used earlier.</exception>
        public Metric GetMetric(
                            string metricId,
                            string dimension1Name,
                            string dimension2Name,
                            MetricConfiguration metricConfiguration)
        {
            return GetOrCreateMetric(
                        MetricAggregationScope.TelemetryConfiguration,
                        new MetricIdentifier(MetricIdentifier.DefaultMetricNamespace, metricId, dimension1Name, dimension2Name),
                        metricConfiguration);
        }

        /// <summary>
        /// Gets or creates a metric container that you can use to track, aggregate and send metric values.<br />
        /// Optionally specify a metric configuration to control how the tracked values are aggregated.
        /// </summary>
        /// <param name="metricId">The ID (name) of the metric.
        ///   (The namespace specified in <see cref="MetricIdentifier.DefaultMetricNamespace"/> will be used.
        ///   To specify another namespace, user an overload that takes a <c>MetricIdentifier</c> paramater instead.)</param>
        /// <param name="dimension1Name">The name of the first dimension.</param>
        /// <param name="dimension2Name">The name of the second dimension.</param>
        /// <param name="metricConfiguration">Determines how tracked values will be aggregated. <br />
        /// Use presets in <see cref="MetricConfigurations.Common"/> or specify your own settings. </param>
        /// <returns>A <c>Metric</c> with the specified ID and dimensions. If you call this method several times
        /// with the same metric ID and dimensions for a given aggregation scope, you will receive the same
        /// instance of <c>Metric</c>.</returns>
        /// <exception cref="ArgumentException">If you previously created a metric with the same namespace, ID, dimensions
        /// and aggregation scope, but with a different configuration. When calling this method to get a previously
        /// created metric, you can simply avoid specifying any configuration (or specify null) to imply the
        /// configuration used earlier.</exception>
        /// <param name="aggregationScope">The scope across which the values for the metric are to be aggregated in memory.
        /// See <see cref="MetricAggregationScope" /> for more info.</param>
        public Metric GetMetric(
                            string metricId,
                            string dimension1Name,
                            string dimension2Name,
                            MetricConfiguration metricConfiguration,
                            MetricAggregationScope aggregationScope)
        {
            return GetOrCreateMetric(
                        aggregationScope,
                        new MetricIdentifier(MetricIdentifier.DefaultMetricNamespace, metricId, dimension1Name, dimension2Name),
                        metricConfiguration);
        }

        /// <summary>
        /// Gets or creates a metric container that you can use to track, aggregate and send metric values.<br />
        /// Optionally specify a metric configuration to control how the tracked values are aggregated.
        /// </summary>
        /// <remarks>The aggregated values will be sent to the <c>TelemetryConfiguration</c>
        /// associated with this client.<br />
        /// The aggregation scope of the fetched<c>Metric</c> is <c>TelemetryConfiguration</c>; this
        /// means that all values tracked for a given metric ID and dimensions will be aggregated together
        /// across all clients that share the same <c>TelemetryConfiguration</c>.</remarks>
        /// <param name="metricId">The ID (name) of the metric.
        ///   (The namespace specified in <see cref="MetricIdentifier.DefaultMetricNamespace"/> will be used.
        ///   To specify another namespace, user an overload that takes a <c>MetricIdentifier</c> paramater instead.)</param>
        /// <param name="dimension1Name">The name of the first dimension.</param>
        /// <param name="dimension2Name">The name of the second dimension.</param>
        /// <param name="dimension3Name">The name of the third dimension.</param>
        /// <exception cref="ArgumentException">If you previously created a metric with the same namespace, ID, dimensions
        /// and aggregation scope, but with a different configuration. When calling this method to get a previously
        /// created metric, you can simply avoid specifying any configuration (or specify null) to imply the
        /// configuration used earlier.</exception>
        /// <returns></returns>
        public Metric GetMetric(
                            string metricId,
                            string dimension1Name,
                            string dimension2Name,
                            string dimension3Name)
        {
            return GetOrCreateMetric(
                        MetricAggregationScope.TelemetryConfiguration,
                        new MetricIdentifier(MetricIdentifier.DefaultMetricNamespace, metricId, dimension1Name, dimension2Name, dimension3Name),
                        metricConfiguration: null);
        }

        /// <summary>
        /// Gets or creates a metric container that you can use to track, aggregate and send metric values.<br />
        /// Optionally specify a metric configuration to control how the tracked values are aggregated.
        /// </summary>
        /// <remarks>The aggregated values will be sent to the <c>TelemetryConfiguration</c>
        /// associated with this client.<br />
        /// The aggregation scope of the fetched<c>Metric</c> is <c>TelemetryConfiguration</c>; this
        /// means that all values tracked for a given metric ID and dimensions will be aggregated together
        /// across all clients that share the same <c>TelemetryConfiguration</c>.</remarks>
        /// <param name="metricId">The ID (name) of the metric.
        ///   (The namespace specified in <see cref="MetricIdentifier.DefaultMetricNamespace"/> will be used.
        ///   To specify another namespace, user an overload that takes a <c>MetricIdentifier</c> paramater instead.)</param>
        /// <param name="dimension1Name">The name of the first dimension.</param>
        /// <param name="dimension2Name">The name of the second dimension.</param>
        /// <param name="dimension3Name">The name of the third dimension.</param>
        /// <param name="metricConfiguration">Determines how tracked values will be aggregated. <br />
        /// Use presets in <see cref="MetricConfigurations.Common"/> or specify your own settings. </param>
        /// <returns>A <c>Metric</c> with the specified ID and dimensions. If you call this method several times
        /// with the same metric ID and dimensions for a given aggregation scope, you will receive the same
        /// instance of <c>Metric</c>.</returns>
        /// <exception cref="ArgumentException">If you previously created a metric with the same namespace, ID, dimensions
        /// and aggregation scope, but with a different configuration. When calling this method to get a previously
        /// created metric, you can simply avoid specifying any configuration (or specify null) to imply the
        /// configuration used earlier.</exception>
        public Metric GetMetric(
                            string metricId,
                            string dimension1Name,
                            string dimension2Name,
                            string dimension3Name,
                            MetricConfiguration metricConfiguration)
        {
            return GetOrCreateMetric(
                        MetricAggregationScope.TelemetryConfiguration,
                        new MetricIdentifier(MetricIdentifier.DefaultMetricNamespace, metricId, dimension1Name, dimension2Name, dimension3Name),
                        metricConfiguration);
        }

        /// <summary>
        /// Gets or creates a metric container that you can use to track, aggregate and send metric values.<br />
        /// Optionally specify a metric configuration to control how the tracked values are aggregated.
        /// </summary>
        /// <param name="metricId">The ID (name) of the metric.
        ///   (The namespace specified in <see cref="MetricIdentifier.DefaultMetricNamespace"/> will be used.
        ///   To specify another namespace, user an overload that takes a <c>MetricIdentifier</c> paramater instead.)</param>
        /// <param name="dimension1Name">The name of the first dimension.</param>
        /// <param name="dimension2Name">The name of the second dimension.</param>
        /// <param name="dimension3Name">The name of the third dimension.</param>
        /// <param name="metricConfiguration">Determines how tracked values will be aggregated. <br />
        /// Use presets in <see cref="MetricConfigurations.Common"/> or specify your own settings. </param>
        /// <returns>A <c>Metric</c> with the specified ID and dimensions. If you call this method several times
        /// with the same metric ID and dimensions for a given aggregation scope, you will receive the same
        /// instance of <c>Metric</c>.</returns>
        /// <exception cref="ArgumentException">If you previously created a metric with the same namespace, ID, dimensions
        /// and aggregation scope, but with a different configuration. When calling this method to get a previously
        /// created metric, you can simply avoid specifying any configuration (or specify null) to imply the
        /// configuration used earlier.</exception>
        /// <param name="aggregationScope">The scope across which the values for the metric are to be aggregated in memory.
        /// See <see cref="MetricAggregationScope" /> for more info.</param>
        public Metric GetMetric(
                            string metricId,
                            string dimension1Name,
                            string dimension2Name,
                            string dimension3Name,
                            MetricConfiguration metricConfiguration,
                            MetricAggregationScope aggregationScope)
        {
            return GetOrCreateMetric(
                        aggregationScope,
                        new MetricIdentifier(MetricIdentifier.DefaultMetricNamespace, metricId, dimension1Name, dimension2Name, dimension3Name),
                        metricConfiguration);
        }

        /// <summary>
        /// Gets or creates a metric container that you can use to track, aggregate and send metric values.<br />
        /// Optionally specify a metric configuration to control how the tracked values are aggregated.
        /// </summary>
        /// <remarks>The aggregated values will be sent to the <c>TelemetryConfiguration</c>
        /// associated with this client.<br />
        /// The aggregation scope of the fetched<c>Metric</c> is <c>TelemetryConfiguration</c>; this
        /// means that all values tracked for a given metric ID and dimensions will be aggregated together
        /// across all clients that share the same <c>TelemetryConfiguration</c>.</remarks>
        /// <param name="metricId">The ID (name) of the metric.
        ///   (The namespace specified in <see cref="MetricIdentifier.DefaultMetricNamespace"/> will be used.
        ///   To specify another namespace, user an overload that takes a <c>MetricIdentifier</c> paramater instead.)</param>
        /// <param name="dimension1Name">The name of the first dimension.</param>
        /// <param name="dimension2Name">The name of the second dimension.</param>
        /// <param name="dimension3Name">The name of the third dimension.</param>
        /// <param name="dimension4Name">The name of the fourth dimension.</param>
        /// <exception cref="ArgumentException">If you previously created a metric with the same namespace, ID, dimensions
        /// and aggregation scope, but with a different configuration. When calling this method to get a previously
        /// created metric, you can simply avoid specifying any configuration (or specify null) to imply the
        /// configuration used earlier.</exception>
        /// <returns></returns>
        public Metric GetMetric(
                            string metricId,
                            string dimension1Name,
                            string dimension2Name,
                            string dimension3Name,
                            string dimension4Name)
        {
            return GetOrCreateMetric(
                        MetricAggregationScope.TelemetryConfiguration,
                        new MetricIdentifier(MetricIdentifier.DefaultMetricNamespace, metricId, dimension1Name, dimension2Name, dimension3Name, dimension4Name),
                        metricConfiguration: null);
        }

        /// <summary>
        /// Gets or creates a metric container that you can use to track, aggregate and send metric values.<br />
        /// Optionally specify a metric configuration to control how the tracked values are aggregated.
        /// </summary>
        /// <remarks>The aggregated values will be sent to the <c>TelemetryConfiguration</c>
        /// associated with this client.<br />
        /// The aggregation scope of the fetched<c>Metric</c> is <c>TelemetryConfiguration</c>; this
        /// means that all values tracked for a given metric ID and dimensions will be aggregated together
        /// across all clients that share the same <c>TelemetryConfiguration</c>.</remarks>
        /// <param name="metricId">The ID (name) of the metric.
        ///   (The namespace specified in <see cref="MetricIdentifier.DefaultMetricNamespace"/> will be used.
        ///   To specify another namespace, user an overload that takes a <c>MetricIdentifier</c> paramater instead.)</param>
        /// <param name="dimension1Name">The name of the first dimension.</param>
        /// <param name="dimension2Name">The name of the second dimension.</param>
        /// <param name="dimension3Name">The name of the third dimension.</param>
        /// <param name="dimension4Name">The name of the fourth dimension.</param>
        /// <param name="metricConfiguration">Determines how tracked values will be aggregated. <br />
        /// Use presets in <see cref="MetricConfigurations.Common"/> or specify your own settings. </param>
        /// <returns>A <c>Metric</c> with the specified ID and dimensions. If you call this method several times
        /// with the same metric ID and dimensions for a given aggregation scope, you will receive the same
        /// instance of <c>Metric</c>.</returns>
        /// <exception cref="ArgumentException">If you previously created a metric with the same namespace, ID, dimensions
        /// and aggregation scope, but with a different configuration. When calling this method to get a previously
        /// created metric, you can simply avoid specifying any configuration (or specify null) to imply the
        /// configuration used earlier.</exception>
        public Metric GetMetric(
                            string metricId,
                            string dimension1Name,
                            string dimension2Name,
                            string dimension3Name,
                            string dimension4Name,
                            MetricConfiguration metricConfiguration)
        {
            return GetOrCreateMetric(
                        MetricAggregationScope.TelemetryConfiguration,
                        new MetricIdentifier(MetricIdentifier.DefaultMetricNamespace, metricId, dimension1Name, dimension2Name, dimension3Name, dimension4Name),
                        metricConfiguration);
        }

        /// <summary>
        /// Gets or creates a metric container that you can use to track, aggregate and send metric values.<br />
        /// Optionally specify a metric configuration to control how the tracked values are aggregated.
        /// </summary>
        /// <param name="metricId">The ID (name) of the metric.
        ///   (The namespace specified in <see cref="MetricIdentifier.DefaultMetricNamespace"/> will be used.
        ///   To specify another namespace, user an overload that takes a <c>MetricIdentifier</c> paramater instead.)</param>
        /// <param name="dimension1Name">The name of the first dimension.</param>
        /// <param name="dimension2Name">The name of the second dimension.</param>
        /// <param name="dimension3Name">The name of the third dimension.</param>
        /// <param name="dimension4Name">The name of the fourth dimension.</param>
        /// <param name="metricConfiguration">Determines how tracked values will be aggregated. <br />
        /// Use presets in <see cref="MetricConfigurations.Common"/> or specify your own settings. </param>
        /// <returns>A <c>Metric</c> with the specified ID and dimensions. If you call this method several times
        /// with the same metric ID and dimensions for a given aggregation scope, you will receive the same
        /// instance of <c>Metric</c>.</returns>
        /// <exception cref="ArgumentException">If you previously created a metric with the same namespace, ID, dimensions
        /// and aggregation scope, but with a different configuration. When calling this method to get a previously
        /// created metric, you can simply avoid specifying any configuration (or specify null) to imply the
        /// configuration used earlier.</exception>
        /// <param name="aggregationScope">The scope across which the values for the metric are to be aggregated in memory.
        /// See <see cref="MetricAggregationScope" /> for more info.</param>
        public Metric GetMetric(
                            string metricId,
                            string dimension1Name,
                            string dimension2Name,
                            string dimension3Name,
                            string dimension4Name,
                            MetricConfiguration metricConfiguration,
                            MetricAggregationScope aggregationScope)
        {
            return GetOrCreateMetric(
                        aggregationScope,
                        new MetricIdentifier(MetricIdentifier.DefaultMetricNamespace, metricId, dimension1Name, dimension2Name, dimension3Name, dimension4Name),
                        metricConfiguration);
        }

        /// <summary>
        /// Gets or creates a metric container that you can use to track, aggregate and send metric values.<br />
        /// Optionally specify a metric configuration to control how the tracked values are aggregated.
        /// </summary>
        /// <remarks>The aggregated values will be sent to the <c>TelemetryConfiguration</c>
        /// associated with this client.<br />
        /// The aggregation scope of the fetched<c>Metric</c> is <c>TelemetryConfiguration</c>; this
        /// means that all values tracked for a given metric ID and dimensions will be aggregated together
        /// across all clients that share the same <c>TelemetryConfiguration</c>.</remarks>
        /// <param name="metricIdentifier">A grouping containing the Namespace, the ID (name) and the dimension names of the metric.</param>
        /// <exception cref="ArgumentException">If you previously created a metric with the same namespace, ID, dimensions
        /// and aggregation scope, but with a different configuration. When calling this method to get a previously
        /// created metric, you can simply avoid specifying any configuration (or specify null) to imply the
        /// configuration used earlier.</exception>
        /// <returns></returns>
        public Metric GetMetric(
                            MetricIdentifier metricIdentifier)
        {
            return GetOrCreateMetric(
                        MetricAggregationScope.TelemetryConfiguration,
                        metricIdentifier,
                        metricConfiguration: null);
        }

        /// <summary>
        /// Gets or creates a metric container that you can use to track, aggregate and send metric values.<br />
        /// Optionally specify a metric configuration to control how the tracked values are aggregated.
        /// </summary>
        /// <remarks>The aggregated values will be sent to the <c>TelemetryConfiguration</c>
        /// associated with this client.<br />
        /// The aggregation scope of the fetched<c>Metric</c> is <c>TelemetryConfiguration</c>; this
        /// means that all values tracked for a given metric ID and dimensions will be aggregated together
        /// across all clients that share the same <c>TelemetryConfiguration</c>.</remarks>
        /// <param name="metricIdentifier">A grouping containing the Namespace, the ID (name) and the dimension names of the metric.</param>
        /// <param name="metricConfiguration">Determines how tracked values will be aggregated. <br />
        /// Use presets in <see cref="MetricConfigurations.Common"/> or specify your own settings. </param>
        /// <returns>A <c>Metric</c> with the specified ID and dimensions. If you call this method several times
        /// with the same metric ID and dimensions for a given aggregation scope, you will receive the same
        /// instance of <c>Metric</c>.</returns>
        /// <exception cref="ArgumentException">If you previously created a metric with the same namespace, ID, dimensions
        /// and aggregation scope, but with a different configuration. When calling this method to get a previously
        /// created metric, you can simply avoid specifying any configuration (or specify null) to imply the
        /// configuration used earlier.</exception>
        public Metric GetMetric(
                            MetricIdentifier metricIdentifier,
                            MetricConfiguration metricConfiguration)
        {
            return GetOrCreateMetric(
                        MetricAggregationScope.TelemetryConfiguration,
                        metricIdentifier,
                        metricConfiguration);
        }

        /// <summary>
        /// Gets or creates a metric container that you can use to track, aggregate and send metric values.<br />
        /// Optionally specify a metric configuration to control how the tracked values are aggregated.
        /// </summary>
        /// <param name="metricIdentifier">A grouping containing the Namespace, the ID (name) and the dimension names of the metric.</param>
        /// <param name="metricConfiguration">Determines how tracked values will be aggregated. <br />
        /// Use presets in <see cref="MetricConfigurations.Common"/> or specify your own settings. </param>
        /// <returns>A <c>Metric</c> with the specified ID and dimensions. If you call this method several times
        /// with the same metric ID and dimensions for a given aggregation scope, you will receive the same
        /// instance of <c>Metric</c>.</returns>
        /// <exception cref="ArgumentException">If you previously created a metric with the same namespace, ID, dimensions
        /// and aggregation scope, but with a different configuration. When calling this method to get a previously
        /// created metric, you can simply avoid specifying any configuration (or specify null) to imply the
        /// configuration used earlier.</exception>
        /// <param name="aggregationScope">The scope across which the values for the metric are to be aggregated in memory.
        /// See <see cref="MetricAggregationScope" /> for more info.</param>
        public Metric GetMetric(
                            MetricIdentifier metricIdentifier,
                            MetricConfiguration metricConfiguration,
                            MetricAggregationScope aggregationScope)
        {
            return GetOrCreateMetric(
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
