namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Threading;
    using System.Threading.Tasks;
    using Channel;
    using DataContracts;
    using Extensibility;
    using Metrics;

    /// <summary>
    /// Send events, metrics and other telemetry to the Application Insights service.
    /// <a href="https://go.microsoft.com/fwlink/?linkid=525722">Learn more</a>
    /// </summary>
    public interface ITelemetryClient
    {
        /// <summary>
        /// Gets the current context that will be used to augment telemetry you send.
        /// </summary>
        TelemetryContext Context { get; }

        /// <summary>
        /// Gets or sets the default instrumentation key for all <see cref="ITelemetry"/> objects logged in this <see cref="TelemetryClient"/>.
        /// </summary>
        string InstrumentationKey
        {
            get;

            [Obsolete("InstrumentationKey based global ingestion is being deprecated. Recommended to set TelemetryConfiguration.ConnectionString. See https://github.com/microsoft/ApplicationInsights-dotnet/issues/2560 for more details.")]
            set;
        }

        /// <summary>
        /// Gets the <see cref="TelemetryConfiguration"/> object associated with this telemetry client instance.
        /// Changes made to the configuration can affect other clients.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        TelemetryConfiguration TelemetryConfiguration { get; }

        /// <summary>
        /// Check to determine if the tracking is enabled.
        /// </summary>
        bool IsEnabled();

        /// <summary>
        /// Send an <see cref="EventTelemetry"/> for display in Diagnostic Search and in the Analytics Portal.
        /// </summary>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#trackevent">Learn more</a>
        /// </remarks>
        /// <param name="eventName">A name for the event.</param>
        /// <param name="properties">Named string values you can use to search and classify events.</param>
        /// <param name="metrics">Measurements associated with this event.</param>
        void TrackEvent(string eventName, IDictionary<string, string> properties = null,
            IDictionary<string, double> metrics = null);

        /// <summary>
        /// Send an <see cref="EventTelemetry"/> for display in Diagnostic Search and in the Analytics Portal.
        /// Create a separate <see cref="EventTelemetry"/> instance for each call to <see cref="TrackEvent(EventTelemetry)"/>.
        /// </summary>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#trackevent">Learn more</a>
        /// </remarks>
        /// <param name="telemetry">An event log item.</param>
        void TrackEvent(EventTelemetry telemetry);

        /// <summary>
        /// Send a trace message for display in Diagnostic Search.
        /// </summary>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#tracktrace">Learn more</a>
        /// </remarks>
        /// <param name="message">Message to display.</param>
        void TrackTrace(string message);

        /// <summary>
        /// Send a trace message for display in Diagnostic Search.
        /// </summary>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#tracktrace">Learn more</a>
        /// </remarks>
        /// <param name="message">Message to display.</param>
        /// <param name="severityLevel">Trace severity level.</param>
        void TrackTrace(string message, SeverityLevel severityLevel);

        /// <summary>
        /// Send a trace message for display in Diagnostic Search.
        /// </summary>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#tracktrace">Learn more</a>
        /// </remarks>
        /// <param name="message">Message to display.</param>
        /// <param name="properties">Named string values you can use to search and classify events.</param>
        void TrackTrace(string message, IDictionary<string, string> properties);

        /// <summary>
        /// Send a trace message for display in Diagnostic Search.
        /// </summary>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#tracktrace">Learn more</a>
        /// </remarks>
        /// <param name="message">Message to display.</param>
        /// <param name="severityLevel">Trace severity level.</param>
        /// <param name="properties">Named string values you can use to search and classify events.</param>
        void TrackTrace(string message, SeverityLevel severityLevel, IDictionary<string, string> properties);

        /// <summary>
        /// Send a trace message for display in Diagnostic Search.
        /// Create a separate <see cref="TraceTelemetry"/> instance for each call to <see cref="TrackTrace(TraceTelemetry)"/>.
        /// </summary>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#tracktrace">Learn more</a>
        /// </remarks>
        /// <param name="telemetry">Message with optional properties.</param>
        void TrackTrace(TraceTelemetry telemetry);

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
        void TrackMetric(string name, double value, IDictionary<string, string> properties = null);

        /// <summary>
        /// This method is not the preferred method for sending metrics.
        /// Metrics should always be pre-aggregated across a time period before being sent.<br />
        /// Use one of the <c>GetMetric(..)</c> overloads to get a metric object for accessing SDK pre-aggregation capabilities.<br />
        /// If you are implementing your own pre-aggregation logic, then you can use this method.
        /// If your application requires sending a separate telemetry item at every occasion without aggregation across time,
        /// you likely have a use case for event telemetry; see <see cref="TrackEvent(EventTelemetry)"/>.
        /// </summary>
        /// <param name="telemetry">The metric telemetry item.</param>        
        void TrackMetric(MetricTelemetry telemetry);

        /// <summary>
        /// Send an <see cref="ExceptionTelemetry"/> for display in Diagnostic Search.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="properties">Named string values you can use to classify and search for this exception.</param>
        /// <param name="metrics">Additional values associated with this exception.</param>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#trackexception">Learn more</a>
        /// </remarks>
        void TrackException(Exception exception, IDictionary<string, string> properties = null,
            IDictionary<string, double> metrics = null);

        /// <summary>
        /// Send an <see cref="ExceptionTelemetry"/> for display in Diagnostic Search.
        /// Create a separate <see cref="ExceptionTelemetry"/> instance for each call to <see cref="TrackException(ExceptionTelemetry)"/>.
        /// </summary>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#trackexception">Learn more</a>
        /// </remarks>
        void TrackException(ExceptionTelemetry telemetry);

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
        void TrackDependency(string dependencyName, string data, DateTimeOffset startTime, TimeSpan duration,
            bool success);

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
        void TrackDependency(string dependencyTypeName, string dependencyName, string data, DateTimeOffset startTime,
            TimeSpan duration, bool success);

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
        void TrackDependency(string dependencyTypeName, string target, string dependencyName, string data,
            DateTimeOffset startTime, TimeSpan duration, string resultCode, bool success);

        /// <summary>
        /// Send information about external dependency call in the application.
        /// Create a separate <see cref="DependencyTelemetry"/> instance for each call to <see cref="TrackDependency(DependencyTelemetry)"/>.
        /// </summary>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#trackdependency">Learn more</a>
        /// </remarks>
        void TrackDependency(DependencyTelemetry telemetry);

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
        void TrackAvailability(string name, DateTimeOffset timeStamp, TimeSpan duration, string runLocation,
            bool success, string message = null, IDictionary<string, string> properties = null,
            IDictionary<string, double> metrics = null);

        /// <summary>
        /// Send information about availability of an application.
        /// Create a separate <see cref="AvailabilityTelemetry"/> instance for each call to <see cref="TrackAvailability(AvailabilityTelemetry)"/>.
        /// </summary>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=517889">Learn more</a>
        /// </remarks>
        void TrackAvailability(AvailabilityTelemetry telemetry);

        /// <summary>
        /// Send information about the page viewed in the application.
        /// </summary>
        /// <param name="name">Name of the page.</param>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#page-views">Learn more</a>
        /// </remarks>
        void TrackPageView(string name);

        /// <summary>
        /// Send information about the page viewed in the application.
        /// Create a separate <see cref="PageViewTelemetry"/> instance for each call to <see cref="TrackPageView(PageViewTelemetry)"/>.
        /// </summary>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#page-views">Learn more</a>
        /// </remarks>
        void TrackPageView(PageViewTelemetry telemetry);

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
        void TrackRequest(string name, DateTimeOffset startTime, TimeSpan duration, string responseCode, bool success);

        /// <summary>
        /// Send information about a request handled by the application.
        /// Create a separate <see cref="RequestTelemetry"/> instance for each call to <see cref="TrackRequest(RequestTelemetry)"/>.
        /// </summary>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#trackrequest">Learn more</a>
        /// </remarks>
        void TrackRequest(RequestTelemetry request);

        /// <summary>
        /// Flushes the in-memory buffer and any metrics being pre-aggregated.
        /// </summary>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#flushing-data">Learn more</a>
        /// </remarks>
        void Flush();

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
        Task<bool> FlushAsync(CancellationToken cancellationToken);

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
        ///   To specify another namespace, use an overload that takes a <c>MetricIdentifier</c> parameter instead.)</param>
        /// <returns>A <c>Metric</c> with the specified ID and dimensions. If you call this method several times
        /// with the same metric ID and dimensions for a given aggregation scope, you will receive the same
        /// instance of <c>Metric</c>.</returns>
        /// <exception cref="ArgumentException">If you previously created a metric with the same namespace, ID, dimensions
        /// and aggregation scope, but with a different configuration. When calling this method to get a previously
        /// created metric, you can simply avoid specifying any configuration (or specify null) to imply the
        /// configuration used earlier.</exception>
        Metric GetMetric(string metricId);

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
        ///   To specify another namespace, use an overload that takes a <c>MetricIdentifier</c> parameter instead.)</param>
        /// <param name="metricConfiguration">Determines how tracked values will be aggregated. <br />
        /// Use presets in <see cref="MetricConfigurations.Common"/> or specify your own settings. </param>
        /// <returns>A <c>Metric</c> with the specified ID and dimensions. If you call this method several times
        /// with the same metric ID and dimensions for a given aggregation scope, you will receive the same
        /// instance of <c>Metric</c>.</returns>
        /// <exception cref="ArgumentException">If you previously created a metric with the same namespace, ID, dimensions
        /// and aggregation scope, but with a different configuration. When calling this method to get a previously
        /// created metric, you can simply avoid specifying any configuration (or specify null) to imply the
        /// configuration used earlier.</exception>
        Metric GetMetric(string metricId, MetricConfiguration metricConfiguration);

        /// <summary>
        /// Gets or creates a metric container that you can use to track, aggregate and send metric values.<br />
        /// Optionally specify a metric configuration to control how the tracked values are aggregated.
        /// </summary>
        /// <param name="metricId">The ID (name) of the metric.
        ///   (The namespace specified in <see cref="MetricIdentifier.DefaultMetricNamespace"/> will be used.
        ///   To specify another namespace, use an overload that takes a <c>MetricIdentifier</c> parameter instead.)</param>
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
        /// <returns>A <see cref="Metric"/> instance that you can use to automatically aggregate and then sent metric data value.</returns>
        Metric GetMetric(
            string metricId,
            MetricConfiguration metricConfiguration,
            MetricAggregationScope aggregationScope);

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
        ///   To specify another namespace, use an overload that takes a <c>MetricIdentifier</c> parameter instead.)</param>
        /// <param name="dimension1Name">The name of the first dimension.</param>
        /// <exception cref="ArgumentException">If you previously created a metric with the same namespace, ID, dimensions
        /// and aggregation scope, but with a different configuration. When calling this method to get a previously
        /// created metric, you can simply avoid specifying any configuration (or specify null) to imply the
        /// configuration used earlier.</exception>
        /// <returns>A <see cref="Metric"/> instance that you can use to automatically aggregate and then sent metric data value.</returns>
        Metric GetMetric(
            string metricId,
            string dimension1Name);

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
        ///   To specify another namespace, use an overload that takes a <c>MetricIdentifier</c> parameter instead.)</param>
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
        Metric GetMetric(
            string metricId,
            string dimension1Name,
            MetricConfiguration metricConfiguration);

        /// <summary>
        /// Gets or creates a metric container that you can use to track, aggregate and send metric values.<br />
        /// Optionally specify a metric configuration to control how the tracked values are aggregated.
        /// </summary>
        /// <param name="metricId">The ID (name) of the metric.
        ///   (The namespace specified in <see cref="MetricIdentifier.DefaultMetricNamespace"/> will be used.
        ///   To specify another namespace, use an overload that takes a <c>MetricIdentifier</c> parameter instead.)</param>
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
        Metric GetMetric(
            string metricId,
            string dimension1Name,
            MetricConfiguration metricConfiguration,
            MetricAggregationScope aggregationScope);

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
        ///   To specify another namespace, use an overload that takes a <c>MetricIdentifier</c> parameter instead.)</param>
        /// <param name="dimension1Name">The name of the first dimension.</param>
        /// <param name="dimension2Name">The name of the second dimension.</param>
        /// <exception cref="ArgumentException">If you previously created a metric with the same namespace, ID, dimensions
        /// and aggregation scope, but with a different configuration. When calling this method to get a previously
        /// created metric, you can simply avoid specifying any configuration (or specify null) to imply the
        /// configuration used earlier.</exception>
        /// <returns>A <see cref="Metric"/> instance that you can use to automatically aggregate and then sent metric data value.</returns>
        Metric GetMetric(
            string metricId,
            string dimension1Name,
            string dimension2Name);

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
        ///   To specify another namespace, use an overload that takes a <c>MetricIdentifier</c> parameter instead.)</param>
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
        Metric GetMetric(
            string metricId,
            string dimension1Name,
            string dimension2Name,
            MetricConfiguration metricConfiguration);

        /// <summary>
        /// Gets or creates a metric container that you can use to track, aggregate and send metric values.<br />
        /// Optionally specify a metric configuration to control how the tracked values are aggregated.
        /// </summary>
        /// <param name="metricId">The ID (name) of the metric.
        ///   (The namespace specified in <see cref="MetricIdentifier.DefaultMetricNamespace"/> will be used.
        ///   To specify another namespace, use an overload that takes a <c>MetricIdentifier</c> parameter instead.)</param>
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
        Metric GetMetric(
            string metricId,
            string dimension1Name,
            string dimension2Name,
            MetricConfiguration metricConfiguration,
            MetricAggregationScope aggregationScope);

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
        ///   To specify another namespace, use an overload that takes a <c>MetricIdentifier</c> parameter instead.)</param>
        /// <param name="dimension1Name">The name of the first dimension.</param>
        /// <param name="dimension2Name">The name of the second dimension.</param>
        /// <param name="dimension3Name">The name of the third dimension.</param>
        /// <exception cref="ArgumentException">If you previously created a metric with the same namespace, ID, dimensions
        /// and aggregation scope, but with a different configuration. When calling this method to get a previously
        /// created metric, you can simply avoid specifying any configuration (or specify null) to imply the
        /// configuration used earlier.</exception>
        /// <returns>A <see cref="Metric"/> instance that you can use to automatically aggregate and then sent metric data value.</returns>
        Metric GetMetric(
            string metricId,
            string dimension1Name,
            string dimension2Name,
            string dimension3Name);

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
        ///   To specify another namespace, use an overload that takes a <c>MetricIdentifier</c> parameter instead.)</param>
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
        Metric GetMetric(
            string metricId,
            string dimension1Name,
            string dimension2Name,
            string dimension3Name,
            MetricConfiguration metricConfiguration);

        /// <summary>
        /// Gets or creates a metric container that you can use to track, aggregate and send metric values.<br />
        /// Optionally specify a metric configuration to control how the tracked values are aggregated.
        /// </summary>
        /// <param name="metricId">The ID (name) of the metric.
        ///   (The namespace specified in <see cref="MetricIdentifier.DefaultMetricNamespace"/> will be used.
        ///   To specify another namespace, use an overload that takes a <c>MetricIdentifier</c> parameter instead.)</param>
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
        Metric GetMetric(
            string metricId,
            string dimension1Name,
            string dimension2Name,
            string dimension3Name,
            MetricConfiguration metricConfiguration,
            MetricAggregationScope aggregationScope);

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
        ///   To specify another namespace, use an overload that takes a <c>MetricIdentifier</c> parameter instead.)</param>
        /// <param name="dimension1Name">The name of the first dimension.</param>
        /// <param name="dimension2Name">The name of the second dimension.</param>
        /// <param name="dimension3Name">The name of the third dimension.</param>
        /// <param name="dimension4Name">The name of the fourth dimension.</param>
        /// <exception cref="ArgumentException">If you previously created a metric with the same namespace, ID, dimensions
        /// and aggregation scope, but with a different configuration. When calling this method to get a previously
        /// created metric, you can simply avoid specifying any configuration (or specify null) to imply the
        /// configuration used earlier.</exception>
        /// <returns>A <see cref="Metric"/> instance that you can use to automatically aggregate and then sent metric data value.</returns>
        Metric GetMetric(
            string metricId,
            string dimension1Name,
            string dimension2Name,
            string dimension3Name,
            string dimension4Name);

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
        ///   To specify another namespace, use an overload that takes a <c>MetricIdentifier</c> parameter instead.)</param>
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
        Metric GetMetric(
            string metricId,
            string dimension1Name,
            string dimension2Name,
            string dimension3Name,
            string dimension4Name,
            MetricConfiguration metricConfiguration);

        /// <summary>
        /// Gets or creates a metric container that you can use to track, aggregate and send metric values.<br />
        /// Optionally specify a metric configuration to control how the tracked values are aggregated.
        /// </summary>
        /// <param name="metricId">The ID (name) of the metric.
        ///   (The namespace specified in <see cref="MetricIdentifier.DefaultMetricNamespace"/> will be used.
        ///   To specify another namespace, use an overload that takes a <c>MetricIdentifier</c> parameter instead.)</param>
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
        Metric GetMetric(
            string metricId,
            string dimension1Name,
            string dimension2Name,
            string dimension3Name,
            string dimension4Name,
            MetricConfiguration metricConfiguration,
            MetricAggregationScope aggregationScope);

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
        /// <returns>A <see cref="Metric"/> instance that you can use to automatically aggregate and then sent metric data value.</returns>
        Metric GetMetric(
            MetricIdentifier metricIdentifier);

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
        Metric GetMetric(
            MetricIdentifier metricIdentifier,
            MetricConfiguration metricConfiguration);

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
        Metric GetMetric(
            MetricIdentifier metricIdentifier,
            MetricConfiguration metricConfiguration,
            MetricAggregationScope aggregationScope);
    }
}
