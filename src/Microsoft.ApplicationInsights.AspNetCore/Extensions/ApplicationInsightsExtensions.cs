using Microsoft.ApplicationInsights.AspNetCore;
using Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Configuration.Memory;
    using AspNetCore.Http;

#if NET451
    using ApplicationInsights.DependencyCollector;
    using ApplicationInsights.Extensibility.PerfCounterCollector;

#endif

    public static class ApplicationInsightsExtensions
    {
        private const string InstrumentationKeyFromConfig = "ApplicationInsights:InstrumentationKey";
        private const string DeveloperModeFromConfig = "ApplicationInsights:TelemetryChannel:DeveloperMode";
        private const string EndpointAddressFromConfig = "ApplicationInsights:TelemetryChannel:EndpointAddress";

        private const string InstrumentationKeyForWebSites = "APPINSIGHTS_INSTRUMENTATIONKEY";
        private const string DeveloperModeForWebSites = "APPINSIGHTS_DEVELOPER_MODE";
        private const string EndpointAddressForWebSites = "APPINSIGHTS_ENDPOINTADDRESS";

        public static IServiceCollection AddApplicationInsightsTelemetry(this IServiceCollection services, string instrumentationKey)
        {
            services.AddApplicationInsightsTelemetry(options => options.TelemetryConfiguration.InstrumentationKey = instrumentationKey);
            return services;
        }
        public static IServiceCollection AddApplicationInsightsTelemetry(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddApplicationInsightsTelemetry(options => AddTelemetryConfiguration(configuration, options.TelemetryConfiguration));
            return services;
        }

        public static IServiceCollection AddApplicationInsightsTelemetry(this IServiceCollection services, Action<ApplicationInsightsServiceOptions> options)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddSingleton<ITelemetryInitializer, AzureWebAppRoleEnvironmentTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, DomainNameRoleInstanceTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, ComponentVersionTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, ClientIpHeaderTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, OperationIdTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, OperationNameTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, SyntheticTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, UserAgentTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, WebSessionTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, WebUserTelemetryInitializer>();

#if NET451
            services.AddSingleton<ITelemetryModule, PerformanceCollectorModule>();
            services.AddSingleton<ITelemetryModule, DependencyTrackingTelemetryModule>();
#endif

            services.AddOptions();
            services.AddSingleton<IConfigureOptions<ApplicationInsightsServiceOptions>, AppInsightsTelemetryConfigurationOptionsSetup>();
            services.Configure<ApplicationInsightsServiceOptions>(options);

            services.AddSingleton<TelemetryClient>();

            services.AddSingleton<ApplicationInsightsInitializer, ApplicationInsightsInitializer>();
            services.AddSingleton<IApplicationInsightDiagnosticListener, AspNetCoreHostingDiagnosticListener>();
            services.AddSingleton<IStartupFilter, ApplicationInsightsStartupFilter>();

            return services;
        }

        public static IConfigurationBuilder AddApplicationInsightsSettings(
            this IConfigurationBuilder configurationSourceRoot,
            bool? developerMode = null,
            string endpointAddress = null,
            string instrumentationKey = null)
        {
            var telemetryConfigValues = new List<KeyValuePair<string, string>>();

            bool wasAnythingSet = false;

            if (developerMode != null)
            {
                telemetryConfigValues.Add(new KeyValuePair<string, string>(DeveloperModeForWebSites, developerMode.Value.ToString()));
                wasAnythingSet = true;
            }

            if (instrumentationKey != null)
            {
                telemetryConfigValues.Add(new KeyValuePair<string, string>(InstrumentationKeyForWebSites, instrumentationKey));
                wasAnythingSet = true;
            }

            if (endpointAddress != null)
            {
                telemetryConfigValues.Add(new KeyValuePair<string, string>(EndpointAddressForWebSites, endpointAddress));
                wasAnythingSet = true;
            }

            if (wasAnythingSet)
            {
                configurationSourceRoot.Add(new MemoryConfigurationSource() { InitialData = telemetryConfigValues });
            }

            return configurationSourceRoot;
        }

        /// <summary>
        /// Read from configuration
        /// Config.json will look like this:
        ///
        ///      "ApplicationInsights": {
        ///          "InstrumentationKey": "11111111-2222-3333-4444-555555555555"
        ///          "TelemetryChannel": {
        ///              EndpointAddress: "http://dc.services.visualstudio.com/v2/track",
        ///              DeveloperMode: true
        ///          }
        ///      }
        /// Values can also be read from environment variables to support azure web sites configuration:
        /// </summary>
        /// <param name="config">Configuration to read variables from.</param>
        /// <param name="telemetryConfiguration">Telemetry configuration to populate</param>
        private static void AddTelemetryConfiguration(IConfiguration config, TelemetryConfiguration telemetryConfiguration)
        {
            string instrumentationKey = config[InstrumentationKeyForWebSites];
            if (string.IsNullOrWhiteSpace(instrumentationKey))
            {
                instrumentationKey = config[InstrumentationKeyFromConfig];
            }

            if (!string.IsNullOrWhiteSpace(instrumentationKey))
            {
                telemetryConfiguration.InstrumentationKey = instrumentationKey;
            }

            string developerModeValue = config[DeveloperModeForWebSites];
            if (string.IsNullOrWhiteSpace(developerModeValue))
            {
                developerModeValue = config[DeveloperModeFromConfig];
            }

            if (!string.IsNullOrWhiteSpace(developerModeValue))
            {
                bool developerMode = false;
                if (bool.TryParse(developerModeValue, out developerMode))
                {
                    telemetryConfiguration.TelemetryChannel.DeveloperMode = developerMode;
                }
            }

            string endpointAddress = config[EndpointAddressForWebSites];
            if (string.IsNullOrWhiteSpace(endpointAddress))
            {
                endpointAddress = config[EndpointAddressFromConfig];
            }

            if (!string.IsNullOrWhiteSpace(endpointAddress))
            {
                telemetryConfiguration.TelemetryChannel.EndpointAddress = endpointAddress;
            }
        }
    }
}
