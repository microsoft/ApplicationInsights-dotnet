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
        private const string DeveloperModeFromConfig = "ApplicationInsights:TelemetryChannel:DeveloperMode";
        private const string EndpointAddressFromConfig = "ApplicationInsights:TelemetryChannel:EndpointAddress";

        private const string ConnectionStringEnvironmentVariable = "APPLICATIONINSIGHTS_CONNECTION_STRING";
        private const string DeveloperModeForWebSites = "APPINSIGHTS_DEVELOPER_MODE";
        private const string EndpointAddressForWebSites = "APPINSIGHTS_ENDPOINTADDRESS";

        private const string ApplicationInsightsSectionFromConfig = "ApplicationInsights";
        private const string TelemetryChannelSectionFromConfig = "ApplicationInsights:TelemetryChannel";

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
                config.GetSection(TelemetryChannelSectionFromConfig).Bind(serviceOptions);

                if (config.TryGetValue(primaryKey: ConnectionStringEnvironmentVariable, backupKey: ConnectionStringFromConfig, value: out string connectionStringValue))
                {
                    serviceOptions.ConnectionString = connectionStringValue;
                }

                if (config.TryGetValue(primaryKey: DeveloperModeForWebSites, backupKey: DeveloperModeFromConfig, value: out string developerModeValue))
                {
                    if (bool.TryParse(developerModeValue, out bool developerMode))
                    {
                        serviceOptions.DeveloperMode = developerMode;
                    }
                }

                if (config.TryGetValue(primaryKey: EndpointAddressForWebSites, backupKey: EndpointAddressFromConfig, value: out string endpointAddress))
                {
                    serviceOptions.EndpointAddress = endpointAddress;
                }

                if (config.TryGetValue(primaryKey: VersionKeyFromConfig, value: out string version))
                {
                    serviceOptions.ApplicationVersion = version;
                }
            }
            catch (Exception ex)
            {
#if AI_ASPNETCORE_WEB
                AspNetCoreEventSource.Instance.LogError(ex.ToInvariantString());
#else
                WorkerServiceEventSource.Instance.LogError(ex.ToInvariantString());
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

        private static void AddTelemetryConfigAndClient(IServiceCollection services)
        {
            services.AddOptions();
            
            // Register TelemetryConfiguration as singleton with factory that creates it for DI scenarios
            services.AddSingleton<TelemetryConfiguration>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<ApplicationInsightsServiceOptions>>().Value;
                
                // Create configuration for DI scenario (skip default builder configuration)
                var configuration = new TelemetryConfiguration(skipDefaultBuilderConfiguration: true);
                
                // Apply connection string from options if available
                if (!string.IsNullOrEmpty(options.ConnectionString))
                {
                    configuration.ConnectionString = options.ConnectionString;
                }
                
                return configuration;
            });
            
            // Register TelemetryClient with factory that injects the logger from DI
            services.AddSingleton<TelemetryClient>(provider =>
            {
                var configuration = provider.GetRequiredService<TelemetryConfiguration>();
                var logger = provider.GetRequiredService<ILogger<TelemetryClient>>();
                
                // Use the internal constructor that accepts logger
                return new TelemetryClient(configuration, logger);
            });
        }
    }
}
