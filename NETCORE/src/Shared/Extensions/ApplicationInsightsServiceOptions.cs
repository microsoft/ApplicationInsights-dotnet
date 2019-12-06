#if AI_ASPNETCORE_WEB
    namespace Microsoft.ApplicationInsights.AspNetCore.Extensions
#else
    namespace Microsoft.ApplicationInsights.WorkerService
#endif
{
    using System.Reflection;
    using Microsoft.ApplicationInsights.DependencyCollector;

    /// <summary>
    /// Application Insights service options defines the custom behavior of the features to add, as opposed to the default selection of features obtained from Application Insights.
    /// </summary>
    public class ApplicationInsightsServiceOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationInsightsServiceOptions" /> class.
        /// Application Insights service options that controls the default behavior of application insights features.
        /// </summary>
        public ApplicationInsightsServiceOptions()
        {
            this.EnablePerformanceCounterCollectionModule = true;
            this.EnableQuickPulseMetricStream = true;
            this.EnableAdaptiveSampling = true;
            this.EnableDebugLogger = true;
            this.EnableHeartbeat = true;
            this.AddAutoCollectedMetricExtractor = true;
#if AI_ASPNETCORE_WEB
            this.EnableRequestTrackingTelemetryModule = true;
            this.EnableAuthenticationTrackingJavaScript = false;
            this.RequestCollectionOptions = new RequestCollectionOptions();
#endif

#if NETSTANDARD2_0
            this.EnableEventCounterCollectionModule = true;
#endif
            this.EnableDependencyTrackingTelemetryModule = true;
            this.EnableAzureInstanceMetadataTelemetryModule = true;
            this.EnableAppServicesHeartbeatTelemetryModule = true;
            this.DependencyCollectionOptions = new DependencyCollectionOptions();
            this.ApplicationVersion = Assembly.GetEntryAssembly()?.GetName().Version.ToString();
        }

        /// <summary>
        /// Gets or sets a value indicating whether QuickPulseTelemetryModule and QuickPulseTelemetryProcessor are registered with the configuration.
        /// Setting EnableQuickPulseMetricStream to <value>false</value>, will disable the default quick pulse metric stream. Defaults to <value>true</value>.
        /// </summary>
        public bool EnableQuickPulseMetricStream { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether PerformanceCollectorModule should be enabled.
        /// Defaults to <value>true</value>.
        /// </summary>
        public bool EnablePerformanceCounterCollectionModule { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether AppServicesHeartbeatTelemetryModule should be enabled.
        /// Defaults to <value>true</value>.
        /// </summary>
        public bool EnableAppServicesHeartbeatTelemetryModule { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether AzureInstanceMetadataTelemetryModule should be enabled.
        /// Defaults to <value>true</value>.
        /// </summary>
        public bool EnableAzureInstanceMetadataTelemetryModule { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether DependencyTrackingTelemetryModule should be enabled.
        /// Defaults to <value>true</value>.
        /// </summary>
        public bool EnableDependencyTrackingTelemetryModule { get; set; }

#if NETSTANDARD2_0
        /// <summary>
        /// Gets or sets a value indicating whether EventCounterCollectionModule should be enabled.
        /// Defaults to <value>true</value>.
        /// </summary>
        public bool EnableEventCounterCollectionModule { get; set; }
#endif

        /// <summary>
        /// Gets or sets a value indicating whether telemetry processor that controls sampling is added to the service.
        /// Setting EnableAdaptiveSampling to <value>false</value>, will disable the default adaptive sampling feature. Defaults to <value>true</value>.
        /// </summary>
        public bool EnableAdaptiveSampling { get; set; }

        /// <summary>
        /// Gets or sets the default instrumentation key for the application.
        /// </summary>
        public string InstrumentationKey { get; set; }

        /// <summary>
        /// Gets or sets the connection string for the application.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the application version reported with telemetries.
        /// </summary>
        public string ApplicationVersion { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether telemetry channel should be set to developer mode.
        /// </summary>
        public bool? DeveloperMode { get; set; }

        /// <summary>
        /// Gets or sets the endpoint address of the channel.
        /// </summary>
        public string EndpointAddress { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a logger would be registered automatically in debug mode.
        /// </summary>
        public bool EnableDebugLogger { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether heartbeats are enabled.
        /// </summary>
        public bool EnableHeartbeat { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether AutoCollectedMetricExtractors are added or not.
        /// Defaults to <value>true</value>.
        /// </summary>
        public bool AddAutoCollectedMetricExtractor { get; set; }

#if AI_ASPNETCORE_WEB
        /// <summary>
        /// Gets <see cref="RequestCollectionOptions"/> that allow to manage <see cref="RequestTrackingTelemetryModule"/>.
        /// </summary>
        public RequestCollectionOptions RequestCollectionOptions { get; }

        /// <summary>
        /// Gets or sets a value indicating whether RequestTrackingTelemetryModule should be enabled.
        /// Defaults to <value>true</value>.
        /// </summary>
        public bool EnableRequestTrackingTelemetryModule { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a JavaScript snippet to track the current authenticated user should
        /// be printed along with the main ApplicationInsights tracking script.
        /// </summary>
        public bool EnableAuthenticationTrackingJavaScript { get; set; }
#endif

        /// <summary>
        /// Gets <see cref="DependencyCollectionOptions"/> that allow to manage <see cref="DependencyTrackingTelemetryModule"/>.
        /// </summary>
        public DependencyCollectionOptions DependencyCollectionOptions { get; }

        /// <summary>
        /// Copy the properties from this <see cref="ApplicationInsightsServiceOptions"/> to a target instance.
        /// </summary>
        /// <param name="target">Target instance to copy properties to.</param>
        internal void CopyPropertiesTo(ApplicationInsightsServiceOptions target)
        {
            if (this.DeveloperMode != null)
            {
                target.DeveloperMode = this.DeveloperMode;
            }

            if (!string.IsNullOrEmpty(this.EndpointAddress))
            {
                target.EndpointAddress = this.EndpointAddress;
            }

            if (!string.IsNullOrEmpty(this.InstrumentationKey))
            {
                target.InstrumentationKey = this.InstrumentationKey;
            }

            target.ConnectionString = this.ConnectionString;
            target.ApplicationVersion = this.ApplicationVersion;
            target.EnableAdaptiveSampling = this.EnableAdaptiveSampling;
            target.EnableDebugLogger = this.EnableDebugLogger;
            target.EnableQuickPulseMetricStream = this.EnableQuickPulseMetricStream;
            target.EnableHeartbeat = this.EnableHeartbeat;
            target.AddAutoCollectedMetricExtractor = this.AddAutoCollectedMetricExtractor;
            target.EnablePerformanceCounterCollectionModule = this.EnablePerformanceCounterCollectionModule;
            target.EnableDependencyTrackingTelemetryModule = this.EnableDependencyTrackingTelemetryModule;
            target.EnableAppServicesHeartbeatTelemetryModule = this.EnableAppServicesHeartbeatTelemetryModule;
            target.EnableAzureInstanceMetadataTelemetryModule = this.EnableAzureInstanceMetadataTelemetryModule;
#if NETSTANDARD2_0
            target.EnableEventCounterCollectionModule = this.EnableEventCounterCollectionModule;
#endif
#if AI_ASPNETCORE_WEB
            target.EnableAuthenticationTrackingJavaScript = this.EnableAuthenticationTrackingJavaScript;
            target.EnableRequestTrackingTelemetryModule = this.EnableRequestTrackingTelemetryModule;
#endif
        }
    }
}
