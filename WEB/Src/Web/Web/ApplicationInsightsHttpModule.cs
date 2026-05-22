namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Web;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// Platform agnostic module for web application instrumentation.
    /// </summary>
    /// <remarks>
    /// The shared <see cref="TelemetryConfiguration"/> is populated from
    /// <c>ApplicationInsights.config</c> by <see cref="WebApplicationInsightsInitializer"/>,
    /// which ASP.NET invokes before <c>Application_Start</c> via
    /// <see cref="System.Web.PreApplicationStartMethodAttribute"/>.
    /// </remarks>
    public sealed class ApplicationInsightsHttpModule : IHttpModule
    {
        private readonly object lockObject = new object();
        private TelemetryConfiguration telemetryConfiguration;
        private TelemetryClient telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationInsightsHttpModule"/> class.
        /// </summary>
        public ApplicationInsightsHttpModule()
        {
        }

        /// <summary>
        /// Initializes module for a given application.
        /// </summary>
        /// <param name="context">HttpApplication instance.</param>
        public void Init(HttpApplication context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // Defensive: PreApplicationStartMethod normally runs before this, but call again
            // in case the module is loaded without our assembly being scanned early.
            // Initialize() is idempotent.
            WebApplicationInsightsInitializer.Initialize();

            // CreateDefault returns the singleton already populated by the initializer above.
            this.telemetryConfiguration = TelemetryConfiguration.CreateDefault();

            context.BeginRequest += this.OnBeginRequest;
        }

        /// <summary>
        /// Required IDisposable implementation.
        /// </summary>
        public void Dispose()
        {
            // The shared TelemetryConfiguration singleton is owned by TelemetryConfiguration
            // itself and is cleaned up on AppDomain unload. Nothing to dispose here.
        }

        private void OnBeginRequest(object sender, EventArgs eventArgs)
        {
            // Ensure TelemetryClient is created only once per module instance using double-check locking.
            if (this.telemetryClient == null)
            {
                lock (this.lockObject)
                {
                    if (this.telemetryClient == null)
                    {
                        this.telemetryClient = new TelemetryClient(this.telemetryConfiguration);
                    }
                }
            }
        }
    }
}
