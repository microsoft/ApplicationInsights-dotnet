namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Web;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Web.Extensions;

    /// <summary>
    /// Platform agnostic module for web application instrumentation.
    /// </summary>
    public sealed class ApplicationInsightsHttpModule : IHttpModule
    {
        // Static fields to track initialization across all module instances
        private static readonly object StaticLockObject = new object();
        private static int initializationCount = 0;
        private static TelemetryConfiguration sharedTelemetryConfiguration;
        private static bool isInitialized = false;

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

            lock (StaticLockObject)
            {
                initializationCount++;
                System.Diagnostics.Debug.WriteLine($"Module Init called #{initializationCount} at {DateTime.Now:HH:mm:ss.fff}");
                System.Diagnostics.Debug.WriteLine($"AppDomain: {AppDomain.CurrentDomain.Id}");

                // Only initialize the shared configuration once per AppDomain
                if (!isInitialized)
                {
                    System.Diagnostics.Debug.WriteLine("Performing first-time initialization");

                    sharedTelemetryConfiguration = TelemetryConfiguration.CreateDefault();
                    
                    // Read connection string from applicationinsights.config
                    string connectionString = Implementation.ApplicationInsightsConfigurationReader.GetConnectionString();
                    if (!string.IsNullOrEmpty(connectionString))
                    {
                        sharedTelemetryConfiguration.ConnectionString = connectionString;
                        System.Diagnostics.Debug.WriteLine($"ConnectionString loaded from config: {connectionString}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("No ConnectionString found in applicationinsights.config");
                    }

                    sharedTelemetryConfiguration.ConfigureOpenTelemetryBuilder(
                        builder => builder.UseApplicationInsightsAspNetTelemetry());

                    isInitialized = true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Skipping duplicate initialization - using shared configuration");
                }

                // Use the shared configuration for this instance
                this.telemetryConfiguration = sharedTelemetryConfiguration;
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
