namespace Microsoft.ApplicationInsights.AspNetCore.Extensions
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
            this.EnableQuickPulseMetricStream = true;
            this.EnableAdaptiveSampling = true;
            this.EnableDebugLogger = true;
            this.EnableAuthenticationTrackingJavaScript = false;
            this.EnableHeartbeat = true;
            this.AddAutoCollectedMetricExtractor = true;
            this.RequestCollectionOptions = new RequestCollectionOptions();
            this.DependencyCollectionOptions = new DependencyCollectionOptions();
            this.ApplicationVersion = Assembly.GetEntryAssembly()?.GetName().Version.ToString();
        }

        /// <summary>
        /// Gets or sets a value indicating whether QuickPulseTelemetryModule and QuickPulseTelemetryProcessor are registered with the configuration.
        /// Setting EnableQuickPulseMetricStream to <c>false</c>, will disable the default quick pulse metric stream. Defaults to <code>true</code>.
        /// </summary>
        public bool EnableQuickPulseMetricStream { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether telemetry processor that controls sampling is added to the service.
        /// Setting EnableAdaptiveSampling to <c>false</c>, will disable the default adaptive sampling feature. Defaults to <code>true</code>.
        /// </summary>
        public bool EnableAdaptiveSampling { get; set; }

        /// <summary>
        /// Gets or sets the default instrumentation key for the application.
        /// </summary>
        public string InstrumentationKey { get; set; }

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
        /// Gets or sets a value indicating whether a JavaScript snippet to track the current authenticated user should
        /// be printed along with the main ApplicationInsights tracking script.
        /// </summary>
        public bool EnableAuthenticationTrackingJavaScript { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether heartbeats are enabled.
        /// </summary>
        public bool EnableHeartbeat { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether AutoCollectedMetricExtractor are added or not.
        /// </summary>
        public bool AddAutoCollectedMetricExtractor { get; set; }

        /// <summary>
        /// Gets <see cref="RequestCollectionOptions"/> that allow to manage <see cref="RequestTrackingTelemetryModule"/>
        /// </summary>
        public RequestCollectionOptions RequestCollectionOptions { get; }

        /// <summary>
        /// Gets <see cref="DependencyCollectionOptions"/> that allow to manage <see cref="DependencyTrackingTelemetryModule"/>
        /// </summary>
        public DependencyCollectionOptions DependencyCollectionOptions { get; }
      
    }
}
