namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Internals;
    using Microsoft.Extensions.Logging;
    using OpenTelemetry;
    using OpenTelemetry.Logs;
    using OpenTelemetry.Metrics;
    using OpenTelemetry.Trace;

    /// <summary>
    /// Send events, metrics and other telemetry to the Application Insights service.
    /// <a href="https://go.microsoft.com/fwlink/?linkid=525722">Learn more</a>
    /// </summary>
    public sealed class TelemetryClient
    {
        private readonly TelemetryConfiguration configuration;
        private readonly ActivitySource activitySource;
        private OpenTelemetrySdk sdk;
        private ILogger<TelemetryClient> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryClient" /> class. Send telemetry with the specified <paramref name="configuration"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">The <paramref name="configuration"/> is null.</exception>
        public TelemetryClient(TelemetryConfiguration configuration)
            : this(configuration, isFromDependencyInjection: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryClient" /> class.
        /// </summary>
        /// <param name="configuration">The telemetry configuration.</param>
        /// <param name="isFromDependencyInjection">Indicates whether this instance is being created by a DI container.</param>
        internal TelemetryClient(TelemetryConfiguration configuration, bool isFromDependencyInjection)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            // Use the shared ActivitySource from configuration
            this.activitySource = configuration.ApplicationInsightsActivitySource;

            // For non-DI scenarios: Build SDK eagerly to ensure TracerProvider is ready
            // For DI scenarios: SDK will be built by configuration when accessed
            if (!isFromDependencyInjection)
            {
                this.sdk = configuration.Build();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryClient" /> class for DI scenarios with logger injection.
        /// </summary>
        /// <param name="configuration">The telemetry configuration.</param>
        /// <param name="logger">The logger instance from DI container.</param>
        internal TelemetryClient(TelemetryConfiguration configuration, ILogger<TelemetryClient> logger)
            : this(configuration, isFromDependencyInjection: true)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the current context that will be used to augment telemetry you send.
        /// </summary>
        public TelemetryContext Context
        {
            get;
            internal set;
        }

        = new TelemetryContext();

        /// <summary>
        /// Gets or sets the default instrumentation key for all <see cref="ITelemetry"/> objects logged in this <see cref="TelemetryClient"/>.
        /// </summary>
        public string InstrumentationKey
        {
            get => this.Context.InstrumentationKey;

            [Obsolete("InstrumentationKey based global ingestion is being deprecated. Recommended to set TelemetryConfiguration.ConnectionString. See https://github.com/microsoft/ApplicationInsights-dotnet/issues/2560 for more details.")]
            set { this.Context.InstrumentationKey = value; }
        }

        /// <summary>
        /// Gets the <see cref="TelemetryConfiguration"/> object associated with this telemetry client instance.
        /// Changes made to the configuration can affect other clients.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public TelemetryConfiguration TelemetryConfiguration
        {
            get { return this.configuration; }
        }

        /// <summary>
        /// Gets the logger instance, creating it lazily if needed (non-DI scenario).
        /// </summary>
        internal ILogger<TelemetryClient> Logger
        {
            get
            {
                if (this.logger == null)
                {
                    this.logger = this.sdk.GetLoggerFactory().CreateLogger<TelemetryClient>();
                }

                return this.logger;
            }
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
            Dictionary<string, string> allProperties = new Dictionary<string, string>();
            allProperties.Add("microsoft.custom.event_name", eventName);
            if (properties != null)
            {
                Utils.CopyDictionary(properties, allProperties);
            }

            if (metrics != null)
            {
                Utils.ConvertDoubleDictionaryToString(metrics, allProperties);
            }
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
            this.Logger.Log(LogLevel.Information, message);
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
            LogLevel logLevel = GetLogLevel(severityLevel);
            this.Logger.Log(logLevel, message);
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
            var state = new DictionaryLogState(properties, message);
            this.Logger.Log(LogLevel.Information, 0, state, null, (s, ex) => s.Message);
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
            LogLevel logLevel = GetLogLevel(severityLevel);
            var state = new DictionaryLogState(properties, message);
            this.Logger.Log(logLevel, 0, state, null, (s, ex) => s.Message);
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
            if (telemetry == null)
            {
                telemetry = new TraceTelemetry();
            }

            if (telemetry.Message == null)
            {
                telemetry.Message = String.Empty;
            }

            if (telemetry.SeverityLevel == null)
            {
                telemetry.SeverityLevel = SeverityLevel.Information;
            }

            // TODO: LocationContext & UserContext are currently internal, so customer can't set them.
            // Need to determine if its ok to set these to public again, just for properties below.

            /*String clientIP = telemetry.Context?.Location?.Ip;
            if (clientIP != null)
            {
                telemetry.Properties["microsoft.client.ip"] = clientIP;
            }

            String userId = telemetry.Context?.User?.Id;
            if (userId != null)
            {
                telemetry.Properties["enduser.pseudo.id"] = userId;
            }*/

            this.TrackTrace(telemetry.Message, telemetry.SeverityLevel.Value, telemetry.Properties);
        }

        /// <summary>
        /// This method is not the preferred method for sending metrics.
        /// Metrics should always be pre-aggregated across a time period before being sent.<br />
        /// Use one of the <c>GetMetric(..)</c> overloads to get a metric object for accessing SDK pre-aggregation capabilities.<br />
        /// If you are implementing your own pre-aggregation logic, then you can use this method.
        /// If your application requires sending a separate telemetry item at every occasion without aggregation across time,
        /// you likely have a use case for event telemetry; see <see cref="TrackEvent(EventTelemetry)"/>.
        /// </summary>
        /// <param name="name">Metric name.</param>
        /// <param name="value">Metric value.</param>
        /// <param name="properties">Named string values you can use to classify and filter metrics.</param>        
#pragma warning disable CA1822 // Mark members as static
#pragma warning disable CA1801 // Review unused parameters
        public void TrackMetric(string name, double value, IDictionary<string, string> properties = null)
#pragma warning restore CA1801 // Review unused parameters
#pragma warning restore CA1822 // Mark members as static
        {
        }

        /// <summary>
        /// This method is not the preferred method for sending metrics.
        /// Metrics should always be pre-aggregated across a time period before being sent.<br />
        /// Use one of the <c>GetMetric(..)</c> overloads to get a metric object for accessing SDK pre-aggregation capabilities.<br />
        /// If you are implementing your own pre-aggregation logic, then you can use this method.
        /// If your application requires sending a separate telemetry item at every occasion without aggregation across time,
        /// you likely have a use case for event telemetry; see <see cref="TrackEvent(EventTelemetry)"/>.
        /// </summary>
        /// <param name="telemetry">The metric telemetry item.</param>        
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
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#trackexception">Learn more</a>
        /// </remarks>
        public void TrackException(Exception exception, IDictionary<string, string> properties = null)
        {
            if (exception == null)
            {
                exception = new Exception(Utils.PopulateRequiredStringValue(null, "message", typeof(ExceptionTelemetry).FullName));
            }

            var state = new DictionaryLogState(properties, exception.Message);
            this.Logger.Log(LogLevel.Error, 0, state, exception, (s, ex) => s.Message);
        }

        /// <summary>
        /// Send an <see cref="ExceptionTelemetry"/> for display in Diagnostic Search.
        /// Create a separate <see cref="ExceptionTelemetry"/> instance for each call to <see cref="TrackException(ExceptionTelemetry)"/>.
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

            var state = new DictionaryLogState(telemetry.Properties, telemetry.Exception.Message);
            var logLevel = GetLogLevel(telemetry.SeverityLevel ?? SeverityLevel.Error);
            this.Logger.Log(logLevel, 0, state, telemetry.Exception, (s, ex) => s.Message);
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
        [Obsolete("Please use a different overload of TrackDependency")]
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
        /// Create a separate <see cref="DependencyTelemetry"/> instance for each call to <see cref="TrackDependency(DependencyTelemetry)"/>.
        /// </summary>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#trackdependency">Learn more</a>
        /// </remarks>
        public void TrackDependency(DependencyTelemetry telemetry)
        {
            if (telemetry == null)
            {
                return;
            }

            // this.Track(telemetry);
            /*
             * fields below are to note which props from dependency are not accounted for yet
             * this.context
            this.Sequence
            this.samplingPercentage
            this.successFieldSet 
            this.Id  -->  exporter gets it from Activity.Context.SpanId but no override exists for this
             */

            using (var dependencyTelemetryActivity = this.activitySource.StartActivity(telemetry.Name, ActivityKind.Client))
            {
                if (dependencyTelemetryActivity != null)
                {
                    dependencyTelemetryActivity.SetStartTime(telemetry.Timestamp.UtcDateTime);
                    dependencyTelemetryActivity.SetEndTime(telemetry.Timestamp.Add(telemetry.Duration).UtcDateTime);
                    dependencyTelemetryActivity.SetStatus(telemetry.Success == true ? ActivityStatusCode.Ok : ActivityStatusCode.Error);

                    if (telemetry.ResultCode != null)
                    {
                        dependencyTelemetryActivity.SetTag(SemanticConventions.AttributeHttpResponseStatusCode, telemetry.ResultCode);
                    }

                    if (telemetry.Type != null)
                    {
                        // dependencyTelemetryActivity.SetTag("microsoft.dependency.type", telemetry.Type);
                        if (String.Equals("Http", telemetry.Type, StringComparison.OrdinalIgnoreCase) && telemetry.Data != null)
                        {
                            if (Uri.TryCreate(telemetry.Data, UriKind.Absolute, out Uri uri))
                            {
                                dependencyTelemetryActivity.SetTag(SemanticConventions.AttributeUrlFull, uri.ToString());
                                dependencyTelemetryActivity.SetTag(SemanticConventions.AttributeHttpMethod, "_OTHER");
                                dependencyTelemetryActivity.SetTag(SemanticConventions.AttributeServerAddress, uri.Host);
                                dependencyTelemetryActivity.SetTag(SemanticConventions.AttributeServerPort, uri.Port);
                            }
                        }
                        else if (String.Equals("SQL", telemetry.Type, StringComparison.OrdinalIgnoreCase) && telemetry.Data != null)
                        {
                            dependencyTelemetryActivity.SetTag(SemanticConventions.AttributeDbStatement, telemetry.Data);
                            // not sure how to populate attrs like db.name, or db.system, or server related attrs that could be used to autopopulate target
                        }
                        else if (String.Equals("Queue Message", telemetry.Type, StringComparison.OrdinalIgnoreCase) && telemetry.Data != null)
                        {
                            dependencyTelemetryActivity.SetTag(SemanticConventions.AttributeMessagingDestination, telemetry.Data);
                            // not sure how to set messaging.destination_name, this would form part of target
                            if (Uri.TryCreate(telemetry.Data, UriKind.Absolute, out Uri uri))
                            {
                                // for the other part of target
                                dependencyTelemetryActivity.SetTag(SemanticConventions.AttributeServerAddress, uri.Host);
                            }
                        }
                    }

                    if (telemetry.Target != null)
                    {
                        dependencyTelemetryActivity.SetTag("microsoft.dependency.target", telemetry.Target);
                    }
                }
            }
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
                Utils.CopyDictionary(properties, availabilityTelemetry.Properties);
            }

            if (metrics != null && metrics.Count > 0)
            {
                Utils.CopyDictionary(metrics, availabilityTelemetry.Metrics);
            }

            this.TrackAvailability(availabilityTelemetry);
        }

        /// <summary>
        /// Send information about availability of an application.
        /// Create a separate <see cref="AvailabilityTelemetry"/> instance for each call to <see cref="TrackAvailability(AvailabilityTelemetry)"/>.
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

            // ISupportAdvancedSampling telemetryWithSampling = telemetry as ISupportAdvancedSampling;

            // Telemetry can be already sampled out if that decision was made before calling Track()
            bool sampledOut = false;
            // if (telemetryWithSampling != null)
            // {
                // sampledOut = telemetryWithSampling.ProactiveSamplingDecision == SamplingDecision.SampledOut;
            // }

            if (!sampledOut)
            {
                if (telemetry is ISupportProperties telemetryWithProperties)
                {
                    bool isDeveloperMode = Environment.GetEnvironmentVariable("APPINSIGHTS_DEVELOPER_MODE") == "true";
                    if (isDeveloperMode)
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

                if (telemetry.Timestamp == default(DateTimeOffset))
                {
                    telemetry.Timestamp = PreciseTimestamp.GetUtcNow();
                }

                // set RoleInstance to the machine name if it's not initialized yet
                if (string.IsNullOrEmpty(telemetry.Context.Cloud.RoleInstance))
                {
                }
            }
            else
            {
                CoreEventSource.Log.InitializationIsSkippedForSampledItem();
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
            // this.Track(new RequestTelemetry(name, startTime, duration, responseCode, success));
            using (var requestTelemetryActivity = this.activitySource.StartActivity(name, ActivityKind.Server))
            {
                if (requestTelemetryActivity != null)
                {
                    requestTelemetryActivity.SetStartTime(startTime.UtcDateTime);
                    requestTelemetryActivity.SetEndTime(startTime.Add(duration).UtcDateTime);
                    requestTelemetryActivity.SetTag(SemanticConventions.AttributeHttpResponseStatusCode, responseCode);
                    requestTelemetryActivity.SetStatus(success ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
                }
            }
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
            /*if (request == null)
            {
                request = new RequestTelemetry();
            }

            this.Track(request);*/
            if (request == null)
            {
                return;
               // request = new RequestTelemetry();
                // Log message
            }

            using (var requestTelemetryActivity = this.activitySource.StartActivity(request.Name, ActivityKind.Server))
            {
                if (requestTelemetryActivity != null)
                {
                    requestTelemetryActivity.SetStartTime(request.Timestamp.UtcDateTime);
                    requestTelemetryActivity.SetEndTime(request.Timestamp.Add(request.Duration).UtcDateTime);

                    // HTTP semantic conventions
                    requestTelemetryActivity.SetTag(SemanticConventions.AttributeHttpResponseStatusCode, request.ResponseCode);
                    requestTelemetryActivity.SetStatus(request.Success == true ? ActivityStatusCode.Ok : ActivityStatusCode.Error);

                    if (request.Url != null)
                    {
                        requestTelemetryActivity.SetTag(SemanticConventions.AttributeUrlScheme, request.Url.Scheme);
                        requestTelemetryActivity.SetTag(SemanticConventions.AttributeServerAddress, request.Url.Host);

                        if (!request.Url.IsDefaultPort)
                        {
                            requestTelemetryActivity.SetTag(SemanticConventions.AttributeServerPort, request.Url.Port);
                        }

                        if (!string.IsNullOrEmpty(request.Url.AbsolutePath))
                        {
                            requestTelemetryActivity.SetTag(SemanticConventions.AttributeUrlPath, request.Url.AbsolutePath);
                        }

                        if (!string.IsNullOrEmpty(request.Url.Query))
                        {
                            requestTelemetryActivity.SetTag(SemanticConventions.AttributeUrlQuery, request.Url.Query);
                        }

                        requestTelemetryActivity.SetTag(SemanticConventions.AttributeUrlFull, request.Url.ToString());
                    }

                    if (!string.IsNullOrEmpty(request.Source))
                    {
                        requestTelemetryActivity.SetTag("request.source", request.Source);
                    }

                    string clientIp = request.Context.Location.Ip;
                    if (!string.IsNullOrEmpty(clientIp))
                    {
                        requestTelemetryActivity.SetTag(SemanticConventions.AttributeClientAddress, clientIp);
                    }

                    if (request.Properties != null)
                    {
                        foreach (var property in request.Properties)
                        {
                            requestTelemetryActivity.SetTag($"custom.{property.Key}", property.Value);
                        }
                    }
                }
            }

            RichPayloadEventSource.Log.Process(request);
        }

        /// <summary>
        /// Flushes the in-memory buffer and any metrics being pre-aggregated.
        /// </summary>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#flushing-data">Learn more</a>
        /// </remarks>
        public void Flush()
        {
            CoreEventSource.Log.TelemetlyClientFlush();

            // Force flush all providers
            this.sdk.TracerProvider?.ForceFlush();
            this.sdk.MeterProvider?.ForceFlush();
            this.sdk.LoggerProvider?.ForceFlush();
        }

        /// <summary>
        /// Asynchronously Flushes the in-memory buffer and any metrics being pre-aggregated.
        /// </summary>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#flushing-data">Learn more</a>
        /// </remarks>
        /// <returns>
        /// Returns true when telemetry data is transferred out of process (application insights server or local storage) and are emitted before the flush invocation.
        /// Returns false when transfer of telemetry data to server has failed with non-retriable http status.
        /// FlushAsync on InMemoryChannel always returns true, as the channel offers minimal reliability guarantees and doesn't retry sending telemetry after a failure.
        /// </returns>
        /// TODO: Metrics flush to respect CancellationToken.
        public Task<bool> FlushAsync(CancellationToken cancellationToken)
        {
            /*if (this.TryGetMetricManager(out MetricManager privateMetricManager))
            {
                privateMetricManager.Flush(flushDownstreamPipeline: false);
            }*/

            TelemetryConfiguration pipeline = this.configuration;
            if (pipeline != null)
            {
                // MetricManager sharedMetricManager = pipeline.GetMetricManager(createIfNotExists: false);
                // sharedMetricManager?.Flush(flushDownstreamPipeline: false);

                // ITelemetryChannel channel = pipeline.TelemetryChannel;

                // if (channel is IAsyncFlushable asyncFlushableChannel && !cancellationToken.IsCancellationRequested)
                // {
                    // return asyncFlushableChannel.FlushAsync(cancellationToken);
                // }
            }

            return cancellationToken.IsCancellationRequested ? TaskEx.FromCanceled<bool>(cancellationToken) : Task.FromResult(false);
        }

        // <summary>
        // Gets or creates a metric container that you can use to track, aggregate and send metric values.<br />
        // Optionally specify a metric configuration to control how the tracked values are aggregated.
        // </summary>
        // <remarks>The aggregated values will be sent to the <c>TelemetryConfiguration</c>
        // associated with this client.<br />
        // The aggregation scope of the fetched<c>Metric</c> is <c>TelemetryConfiguration</c>; this
        // means that all values tracked for a given metric ID and dimensions will be aggregated together
        // across all clients that share the same <c>TelemetryConfiguration</c>.</remarks>
        // <param name="metricId">The ID (name) of the metric.
        //   (The namespace specified in MetricIdentifier.DefaultMetricNamespace will be used.
        //   To specify another namespace, use an overload that takes a <c>MetricIdentifier</c> parameter instead.)</param>
        // <returns>A <c>Metric</c> with the specified ID and dimensions. If you call this method several times
        // with the same metric ID and dimensions for a given aggregation scope, you will receive the same
        // instance of <c>Metric</c>.</returns>
        // <exception cref="ArgumentException">If you previously created a metric with the same namespace, ID, dimensions
        // and aggregation scope, but with a different configuration. When calling this method to get a previously
        // created metric, you can simply avoid specifying any configuration (or specify null) to imply the
        // configuration used earlier.</exception>
                /*internal Metric GetMetric(
                                    string metricId)
                {
                    return this.GetOrCreateMetric(
                                MetricAggregationScope.TelemetryConfiguration,
                                new MetricIdentifier(metricId),
                                metricConfiguration: null);
                }*/

                /// <summary>
                /// Send information about the page viewed in the application.
                /// Create a separate <see cref="PageViewTelemetry"/> instance for each call to <see cref="TrackPageView(PageViewTelemetry)"/>.
                /// </summary>
                /// <remarks>
                /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#page-views">Learn more</a>
                /// </remarks>
        internal void TrackPageView(PageViewTelemetry telemetry)
        {
            if (telemetry == null)
            {
                telemetry = new PageViewTelemetry();
            }

            this.Track(telemetry);
        }

        private static LogLevel GetLogLevel(SeverityLevel severityLevel)
        {
            return severityLevel switch
            {
                SeverityLevel.Verbose => LogLevel.Debug,
                SeverityLevel.Information => LogLevel.Information,
                SeverityLevel.Warning => LogLevel.Warning,
                SeverityLevel.Error => LogLevel.Error,
                SeverityLevel.Critical => LogLevel.Critical,
                _ => LogLevel.None
            };
        }

        private readonly struct DictionaryLogState : IReadOnlyList<KeyValuePair<string, object>>
        {
            public readonly string Message;
            private readonly IReadOnlyList<KeyValuePair<string, object>> items;

            public DictionaryLogState(IDictionary<string, string> properties, string message)
            {
                this.Message = message ?? string.Empty;

                if (properties == null || properties.Count == 0)
                {
                    this.items = new[] { new KeyValuePair<string, object>("{OriginalFormat}", message ?? string.Empty) };
                }
                else
                {
                    var list = new List<KeyValuePair<string, object>>(properties.Count + 1);
                    foreach (var kvp in properties)
                    {
                        list.Add(new KeyValuePair<string, object>(kvp.Key, kvp.Value));
                    }

                    list.Add(new KeyValuePair<string, object>("{OriginalFormat}", message ?? string.Empty));
                    this.items = list;
                }
            }

            public int Count => this.items.Count;

            public KeyValuePair<string, object> this[int index] => this.items[index];

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => this.items.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
        }
    }
}
