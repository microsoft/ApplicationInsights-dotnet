namespace Microsoft.ApplicationInsights.Web.Implementation
{
    /// <summary>
    /// Holds configuration options read from ApplicationInsights.config file.
    /// </summary>
    internal class ApplicationInsightsConfigOptions
    {
        /// <summary>
        /// Gets or sets the connection string for Application Insights.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether telemetry collection is disabled.
        /// </summary>
        public bool? DisableTelemetry { get; set; }

        /// <summary>
        /// Gets or sets the sampling ratio (0.0 to 1.0). 1.0 means 100% of telemetry is sent.
        /// </summary>
        public float? SamplingRatio { get; set; }

        /// <summary>
        /// Gets or sets the rate limit for traces per second.
        /// </summary>
        public double? TracesPerSecond { get; set; }

        /// <summary>
        /// Gets or sets the directory path for offline storage.
        /// </summary>
        public string StorageDirectory { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether offline storage is disabled.
        /// </summary>
        public bool? DisableOfflineStorage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether trace-based logs sampler is enabled.
        /// </summary>
        public bool? EnableTraceBasedLogsSampler { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether quick pulse metric stream (live metrics) is enabled.
        /// </summary>
        public bool? EnableQuickPulseMetricStream { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether performance counter collection is enabled.
        /// </summary>
        public bool? EnablePerformanceCounterCollectionModule { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether auto-collected metric extractor is enabled.
        /// </summary>
        public bool? AddAutoCollectedMetricExtractor { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether dependency tracking is enabled.
        /// </summary>
        public bool? EnableDependencyTrackingTelemetryModule { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether request tracking is enabled.
        /// </summary>
        public bool? EnableRequestTrackingTelemetryModule { get; set; }

        /// <summary>
        /// Gets or sets the application version.
        /// </summary>
        public string ApplicationVersion { get; set; }
    }
}
