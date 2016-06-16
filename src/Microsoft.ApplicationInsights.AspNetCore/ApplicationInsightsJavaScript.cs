namespace Microsoft.ApplicationInsights.AspNetCore
{
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// This class helps to inject Application Insights JavaScript snippet into application code.
    /// </summary>
    public class JavaScriptSnippet
    {
        /// <summary>JavaScript snippet.</summary>
        private static readonly string Snippet = Resources.JavaScriptSnippet;

        /// <summary>Configuration instance.</summary>
        private TelemetryConfiguration telemetryConfiguration;

        /// <summary>
        /// Initializes a new instance of the JavaScriptSnippet class.
        /// </summary>
        /// <param name="telemetryConfiguration">The configuration instance to use.</param>
        public JavaScriptSnippet(TelemetryConfiguration telemetryConfiguration)
        {
            this.telemetryConfiguration = telemetryConfiguration;
        }

        /// <summary>
        /// Gets a code snippet with instrumentation key initialized from TelemetryConfiguration.
        /// </summary>
        /// <returns>JavaScript code snippet with instrumentation key or empty if instrumentation key was not set for the applicaiton.</returns>
        public string FullScript
        {
            get
            {
                if (!string.IsNullOrEmpty(this.telemetryConfiguration.InstrumentationKey))
                {
                    return string.Format(Snippet, this.telemetryConfiguration.InstrumentationKey);
                }
                else
                {
                    return string.Empty;
                }
            }
        }
    }
}
