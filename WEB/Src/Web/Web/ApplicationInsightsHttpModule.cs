namespace Microsoft.ApplicationInsights.Web
{
    using Azure.Monitor.OpenTelemetry.Exporter;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Web.Extensions;
    using Microsoft.ApplicationInsights.Web.Implementation;
    using OpenTelemetry;
    using OpenTelemetry.Exporter;
    using OpenTelemetry.Trace;
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Threading;
    using System.Web;

    /// <summary>
    /// Platform agnostic module for web application instrumentation.
    /// </summary>
    public sealed class ApplicationInsightsHttpModule : IHttpModule
    {
        // Static fields to track initialization across all module instances
        private static int _initializationCount = 0;
        private static readonly object _staticLockObject = new object();
        private static TelemetryConfiguration _sharedTelemetryConfiguration;
        private static bool _isInitialized = false;

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

            lock (_staticLockObject)
            {
                _initializationCount++;
                System.Diagnostics.Debug.WriteLine($"Module Init called #{_initializationCount} at {DateTime.Now:HH:mm:ss.fff}");
                System.Diagnostics.Debug.WriteLine($"AppDomain: {AppDomain.CurrentDomain.Id}");

                // Only initialize the shared configuration once per AppDomain
                if (!_isInitialized)
                {
                    System.Diagnostics.Debug.WriteLine("Performing first-time initialization");

                    _sharedTelemetryConfiguration = TelemetryConfiguration.CreateDefault();
                    _sharedTelemetryConfiguration.ConfigureOpenTelemetryBuilder(
                        builder => builder.UseApplicationInsightsAspNetTelemetry());

                    _isInitialized = true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Skipping duplicate initialization - using shared configuration");
                }

                // Use the shared configuration for this instance
                this.telemetryConfiguration = _sharedTelemetryConfiguration;
            }

            // Subscribe to events (this is safe to do multiple times as ASP.NET handles duplicate subscriptions)
            context.BeginRequest += this.OnBeginRequest;
        }

        /// <summary>
        /// Required IDisposable implementation.
        /// </summary>
        public void Dispose()
        {
            // Don't dispose the shared configuration, as other instances might still be using it
            // It will be cleaned up when the AppDomain unloads

            // Note: If you need to dispose, you'd need a reference counting mechanism
            System.Diagnostics.Debug.WriteLine("Dispose called");
        }

        private void OnBeginRequest(object sender, EventArgs eventArgs)
        {
            // Ensure TelemetryClient is created only once per module instance using double-check locking pattern
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
