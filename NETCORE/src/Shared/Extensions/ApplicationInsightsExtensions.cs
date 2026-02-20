namespace Microsoft.Extensions.DependencyInjection
{
    using System;
    using System.Linq;

    using Microsoft.ApplicationInsights;
#if AI_ASPNETCORE_WEB
    using Microsoft.ApplicationInsights.AspNetCore.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
#endif

    using Microsoft.ApplicationInsights.Extensibility;

    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

#if AI_ASPNETCORE_WORKER
    using Microsoft.ApplicationInsights.WorkerService;
    using Microsoft.ApplicationInsights.WorkerService.Implementation.Tracing;
#endif

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Extension methods for <see cref="IServiceCollection"/> that allow adding Application Insights services to application.
    /// </summary>
    public static partial class ApplicationInsightsExtensions
    {
        private const string VersionKeyFromConfig = "version";
        private const string ConnectionStringFromConfig = "ApplicationInsights:ConnectionString";

        private const string ConnectionStringEnvironmentVariable = "APPLICATIONINSIGHTS_CONNECTION_STRING";

        private const string ApplicationInsightsSectionFromConfig = "ApplicationInsights";

        /// <summary>
        /// Read configuration from appSettings.json, appsettings.{env.EnvironmentName}.json,
        /// IConfiguation used in an application and EnvironmentVariables.
        /// Bind configuration to ApplicationInsightsServiceOptions.
        /// Values can also be read from environment variables to support azure web sites configuration.
        /// </summary>
        /// <param name="config">Configuration to read variables from.</param>
        /// <param name="serviceOptions">Telemetry configuration to populate.</param>
        internal static void AddTelemetryConfiguration(
            IConfiguration config,
            ApplicationInsightsServiceOptions serviceOptions)
        {
            try
            {
                config.GetSection(ApplicationInsightsSectionFromConfig).Bind(serviceOptions);

                if (config.TryGetValue(primaryKey: ConnectionStringEnvironmentVariable, backupKey: ConnectionStringFromConfig, value: out string connectionStringValue))
                {
                    serviceOptions.ConnectionString = connectionStringValue;
                }

                if (config.TryGetValue(primaryKey: VersionKeyFromConfig, value: out string version))
                {
                    serviceOptions.ApplicationVersion = version;
                }
            }
            catch (Exception ex)
            {
#if AI_ASPNETCORE_WEB
                AspNetCoreEventSource.Instance.TelemetryConfigurationFailure(ex.ToInvariantString());
#else
                WorkerServiceEventSource.Instance.TelemetryConfigurationFailure(ex.ToInvariantString());
#endif
            }
        }

        private static bool TryGetValue(this IConfiguration config, string primaryKey, out string value, string backupKey = null)
        {
            value = config[primaryKey];

            if (backupKey != null && string.IsNullOrWhiteSpace(value))
            {
                value = config[backupKey];
            }

            return !string.IsNullOrWhiteSpace(value);
        }

        private static bool IsApplicationInsightsAdded(IServiceCollection services)
        {
            // We treat TelemetryClient as a marker that AI services were added to service collection
            return services.Any(service => service.ServiceType == typeof(TelemetryClient));
        }

        private static void AddTelemetryConfigAndClient(IServiceCollection services, string extensionVersion)
        {
            services.AddOptions();
            
            // Register TelemetryConfiguration singleton with factory that creates it for DI scenarios
            // We use a factory to ensure skipDefaultBuilderConfiguration: true is passed
            services.AddSingleton<TelemetryConfiguration>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<ApplicationInsightsServiceOptions>>().Value;
                
                // Create configuration for DI scenario (skip default builder configuration)
                var configuration = new TelemetryConfiguration(skipDefaultBuilderConfiguration: true);

                configuration.ExtensionVersion = extensionVersion;
                
                // Apply connection string from options if available
                if (!string.IsNullOrEmpty(options.ConnectionString))
                {
                    configuration.ConnectionString = options.ConnectionString;
                }

                // Apply any Configure<TelemetryConfiguration> callbacks
                var configureOptions = provider.GetServices<IConfigureOptions<TelemetryConfiguration>>();
                foreach (var configure in configureOptions)
                {
                    configure.Configure(configuration);
                }

                var postConfigureOptions = provider.GetServices<IPostConfigureOptions<TelemetryConfiguration>>();
                foreach (var postConfigure in postConfigureOptions)
                {
                    postConfigure.PostConfigure(Options.DefaultName, configuration);
                }
                
                return configuration;
            });
            
            // Register TelemetryClient with factory that injects the logger and service provider from DI
            services.AddSingleton<TelemetryClient>(provider =>
            {
                var configuration = provider.GetRequiredService<TelemetryConfiguration>();
                var logger = provider.GetRequiredService<ILogger<TelemetryClient>>();
                
                // Use the internal constructor that accepts logger and service provider
                // The service provider is needed so Flush/FlushAsync can resolve DI-managed OTel providers
                return new TelemetryClient(configuration, logger, provider);
            });
        }
    }
}
