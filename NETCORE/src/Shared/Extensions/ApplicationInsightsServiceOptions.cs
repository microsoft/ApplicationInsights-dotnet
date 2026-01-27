#if AI_ASPNETCORE_WEB
namespace Microsoft.ApplicationInsights.AspNetCore.Extensions
#else
    namespace Microsoft.ApplicationInsights.WorkerService
#endif
{
    using System.Reflection;
    using Azure.Core;

    /// <summary>
    /// Application Insights service options defines the custom behavior of the features to add, as opposed to the default selection of features obtained from Application Insights.
    /// </summary>
    public class ApplicationInsightsServiceOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether QuickPulseTelemetryModule and QuickPulseTelemetryProcessor are registered with the configuration.
        /// Setting EnableQuickPulseMetricStream to <value>false</value>, will disable the default quick pulse metric stream. Defaults to <value>true</value>.
        /// </summary>
        public bool EnableQuickPulseMetricStream { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether PerformanceCollectorModule should be enabled.
        /// Defaults to <value>true</value>.
        /// </summary>
        public bool EnablePerformanceCounterCollectionModule { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether DependencyTrackingTelemetryModule should be enabled.
        /// Defaults to <value>true</value>.
        /// </summary>
        public bool EnableDependencyTrackingTelemetryModule { get; set; } = true;

        /// <summary>
        /// Gets or sets the connection string for the application.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the value of <see cref="TokenCredential" />.
        /// If <see cref="TokenCredential" /> is not set, AAD authentication is disabled
        /// and Instrumentation Key from the Connection String will be used.
        /// </summary>
        public TokenCredential Credential { get; set; }

        /// <summary>
        /// Gets or sets the application version reported with telemetries.
        /// </summary>
        public string ApplicationVersion { get; set; } = Assembly.GetEntryAssembly()?.GetName().Version.ToString();

        /// <summary>
        /// Gets or sets a value indicating whether AutoCollectedMetricExtractors are added or not.
        /// Defaults to <value>true</value>.
        /// </summary>
        public bool AddAutoCollectedMetricExtractor { get; set; } = true;

        /// <summary>
        /// Gets or sets the target number of traces per second to be collected.
        /// </summary>
        public double? TracesPerSecond { get; set; }

        /// <summary>
        /// Gets or sets the sampling ratio for telemetry.
        /// Value must be between 0.0 and 1.0, where 1.0 means all telemetry is collected (no sampling).
        /// </summary>
        public float? SamplingRatio { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether trace-based log sampling is enabled.
        /// When null, the Azure Monitor Exporter default of true is used.
        /// </summary>
        public bool? EnableTraceBasedLogsSampler { get; set; }

#if AI_ASPNETCORE_WEB
        /// <summary>
        /// Gets or sets a value indicating whether RequestTrackingTelemetryModule should be enabled.
        /// Defaults to <value>true</value>.
        /// </summary>
        public bool EnableRequestTrackingTelemetryModule { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether a JavaScript snippet to track the current authenticated user should
        /// be printed along with the main ApplicationInsights tracking script.
        /// </summary>
        public bool EnableAuthenticationTrackingJavaScript { get; set; } = false;
#endif

        /// <summary>
        /// Copy the properties from this <see cref="ApplicationInsightsServiceOptions"/> to a target instance.
        /// </summary>
        /// <param name="target">Target instance to copy properties to.</param>
        internal void CopyPropertiesTo(ApplicationInsightsServiceOptions target)
        {
            if (!string.IsNullOrEmpty(this.ConnectionString))
            {
                target.ConnectionString = this.ConnectionString;
            }

            if (this.Credential != null)
            {
                target.Credential = this.Credential;
            }

            target.ApplicationVersion = this.ApplicationVersion;
            target.EnableQuickPulseMetricStream = this.EnableQuickPulseMetricStream;
            target.AddAutoCollectedMetricExtractor = this.AddAutoCollectedMetricExtractor;
            target.EnablePerformanceCounterCollectionModule = this.EnablePerformanceCounterCollectionModule;
            target.EnableDependencyTrackingTelemetryModule = this.EnableDependencyTrackingTelemetryModule;
            target.TracesPerSecond = this.TracesPerSecond;
            target.SamplingRatio = this.SamplingRatio;
            target.EnableTraceBasedLogsSampler = this.EnableTraceBasedLogsSampler;

#if AI_ASPNETCORE_WEB
            target.EnableAuthenticationTrackingJavaScript = this.EnableAuthenticationTrackingJavaScript;
            target.EnableRequestTrackingTelemetryModule = this.EnableRequestTrackingTelemetryModule;
#endif
        }
    }
}
