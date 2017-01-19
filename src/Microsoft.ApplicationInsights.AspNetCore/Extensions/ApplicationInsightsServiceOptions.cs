namespace Microsoft.ApplicationInsights.AspNetCore.Extensions
{
    using System.Reflection;

    /// <summary>
    /// Application Insights service options defines the custom behavior of the features to add, as opposed to the default selection of features obtained from Application Insights.
    /// </summary>
    public class ApplicationInsightsServiceOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationInsightsServiceOptions" /> class.
        /// Application Insights service options that controlls the default behavior of application insights features.
        /// </summary>
        public ApplicationInsightsServiceOptions()
        {
            this.EnableQuickPulseMetricStream = true;
            this.EnableAdaptiveSampling = true;
            this.EnableDebugLogger = true;
            this.EnableAuthenticationTrackingJavaScript = false;
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
    }
}
