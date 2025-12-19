#if AI_ASPNETCORE_WEB
    namespace Microsoft.ApplicationInsights.AspNetCore.Extensions
#else
    namespace Microsoft.ApplicationInsights.WorkerService
#endif
{
    using System.Reflection;

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
        /// Gets or sets a value indicating whether telemetry processor that controls sampling is added to the service.
        /// Setting EnableAdaptiveSampling to <value>false</value>, will disable sampling entirely. 
        /// When <value>true</value> (default), enables rate-limit based sampling in Azure Monitor Exporter.
        /// Defaults to <value>true</value>.
        /// </summary>
        public bool EnableAdaptiveSampling { get; set; } = true;

        /// <summary>
        /// Gets or sets the connection string for the application.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the value of <see cref="Azure.Core.TokenCredential" />.
        /// If <see cref="Azure.Core.TokenCredential" /> is not set, AAD authentication is disabled
        /// and Instrumentation Key from the Connection String will be used.
        /// </summary>
        public Azure.Core.TokenCredential Credential { get; set; }

        /// <summary>
        /// Gets or sets the application version reported with telemetries.
        /// </summary>
        public string ApplicationVersion { get; set; } = Assembly.GetEntryAssembly()?.GetName().Version.ToString();

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
        public bool EnableDebugLogger { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether AutoCollectedMetricExtractors are added or not.
        /// Defaults to <value>true</value>.
        /// </summary>
        public bool AddAutoCollectedMetricExtractor { get; set; } = true;

#if AI_ASPNETCORE_WEB
        /// <summary>
        /// Gets <see cref="RequestCollectionOptions"/> that allow to manage RequestTrackingTelemetryModule" />.
        /// </summary>
        public RequestCollectionOptions RequestCollectionOptions { get; } = new RequestCollectionOptions();

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
        /// Gets <see cref="DependencyCollectionOptions"/> that allow to manage DependencyTrackingTelemetryModule/>.
        /// </summary>
        public DependencyCollectionOptions DependencyCollectionOptions { get; } = new DependencyCollectionOptions();

#if AI_ASPNETCORE_WEB
        /// <summary>
        /// Gets or sets a value indicating whether TelemetryConfiguration.Active should be initialized.
        /// Former versions of this library had a dependency on this static instance. 
        /// This dependency has been removed and we no longer initialize this by default.
        /// If users depended on this behavior you should enable this.
        /// However, we recommend migrating away from using TelemetryConfiguration.Active in your projects.
        /// </summary>
        public bool EnableActiveTelemetryConfigurationSetup { get; set; } = false;
#endif

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

            if (!string.IsNullOrEmpty(this.ConnectionString))
            {
                target.ConnectionString = this.ConnectionString;
            }

            if (this.Credential != null)
            {
                target.Credential = this.Credential;
            }

            target.ApplicationVersion = this.ApplicationVersion;
            target.EnableAdaptiveSampling = this.EnableAdaptiveSampling;
            target.EnableDebugLogger = this.EnableDebugLogger;
            target.EnableQuickPulseMetricStream = this.EnableQuickPulseMetricStream;
            target.AddAutoCollectedMetricExtractor = this.AddAutoCollectedMetricExtractor;
            target.EnablePerformanceCounterCollectionModule = this.EnablePerformanceCounterCollectionModule;
            target.EnableDependencyTrackingTelemetryModule = this.EnableDependencyTrackingTelemetryModule;

#if AI_ASPNETCORE_WEB
            target.EnableAuthenticationTrackingJavaScript = this.EnableAuthenticationTrackingJavaScript;
            target.EnableRequestTrackingTelemetryModule = this.EnableRequestTrackingTelemetryModule;
            target.EnableActiveTelemetryConfigurationSetup = this.EnableActiveTelemetryConfigurationSetup;
#endif
        }
    }
}
