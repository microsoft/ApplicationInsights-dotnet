namespace Microsoft.ApplicationInsights.AspNetCore
{
    using System.Globalization;
    using System.Security.Principal;
    using System.Text.Encodings.Web;
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// This class helps to inject Application Insights JavaScript snippet into application code.
    /// </summary>
    public class JavaScriptSnippet
    {
        /// <summary>JavaScript snippet.</summary>
        private static readonly string Snippet = Resources.JavaScriptSnippet;

        /// <summary>JavaScript authenticated user tracking snippet.</summary>
        private static readonly string AuthSnippet = Resources.JavaScriptAuthSnippet;

        /// <summary>Configuration instance.</summary>
        private TelemetryConfiguration telemetryConfiguration;

        /// <summary> Http context accessor.</summary>
        private readonly IHttpContextAccessor httpContextAccessor;

        /// <summary> Weather to print authenticated user tracking snippet.</summary>
        private bool enableAuthSnippet;

        private JavaScriptEncoder encoder;

        /// <summary>
        /// Initializes a new instance of the JavaScriptSnippet class.
        /// </summary>
        /// <param name="telemetryConfiguration">The configuration instance to use.</param>
        /// <param name="serviceOptions">Service options instance to use.</param>
        /// <param name="httpContextAccessor">Http context accessor to use.</param>
        public JavaScriptSnippet(TelemetryConfiguration telemetryConfiguration,
            IOptions<ApplicationInsightsServiceOptions> serviceOptions,
            IHttpContextAccessor httpContextAccessor,
            JavaScriptEncoder encoder)
        {
            this.telemetryConfiguration = telemetryConfiguration;
            this.httpContextAccessor = httpContextAccessor;
            this.enableAuthSnippet = serviceOptions.Value.EnableAuthenticationTrackingJavaScript;
            this.encoder = encoder;
        }

        /// <summary>
        /// Gets a code snippet with instrumentation key initialized from TelemetryConfiguration.
        /// </summary>
        /// <returns>JavaScript code snippet with instrumentation key or empty if instrumentation key was not set for the application.</returns>
        public string FullScript
        {
            get
            {
                if (!this.telemetryConfiguration.DisableTelemetry &&
                    !string.IsNullOrEmpty(this.telemetryConfiguration.InstrumentationKey))
                {
                    string additionalJS = string.Empty;
                    IIdentity identity = httpContextAccessor?.HttpContext?.User?.Identity;
                    if (enableAuthSnippet &&
                        identity != null &&
                        identity.IsAuthenticated)
                    {
                        string escapedUserName = encoder.Encode(identity.Name);
                        additionalJS = string.Format(CultureInfo.InvariantCulture, AuthSnippet, escapedUserName);
                    }
                    return string.Format(CultureInfo.InvariantCulture, Snippet, this.telemetryConfiguration.InstrumentationKey, additionalJS);
                }
                else
                {
                    return string.Empty;
                }
            }
        }
    }
}
