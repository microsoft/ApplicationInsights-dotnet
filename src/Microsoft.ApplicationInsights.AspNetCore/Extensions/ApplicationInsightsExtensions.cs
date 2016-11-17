using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.AspNetCore;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners;
using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
using Microsoft.ApplicationInsights.Extensibility;
#if NET451
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector;
#endif
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ApplicationInsightsExtensions
    {
        private const string VersionKeyFromConfig = "version";
        private const string InstrumentationKeyFromConfig = "ApplicationInsights:InstrumentationKey";
        private const string DeveloperModeFromConfig = "ApplicationInsights:TelemetryChannel:DeveloperMode";
        private const string EndpointAddressFromConfig = "ApplicationInsights:TelemetryChannel:EndpointAddress";

        private const string InstrumentationKeyForWebSites = "APPINSIGHTS_INSTRUMENTATIONKEY";
        private const string DeveloperModeForWebSites = "APPINSIGHTS_DEVELOPER_MODE";
        private const string EndpointAddressForWebSites = "APPINSIGHTS_ENDPOINTADDRESS";

        public static IServiceCollection AddApplicationInsightsTelemetry(this IServiceCollection services, string instrumentationKey)
        {
            services.AddApplicationInsightsTelemetry(options => options.InstrumentationKey = instrumentationKey);
            return services;
        }
        public static IServiceCollection AddApplicationInsightsTelemetry(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddApplicationInsightsTelemetry(options => AddTelemetryConfiguration(configuration, options));
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
            services.AddSingleton<IOptions<TelemetryConfiguration>, TelemetryConfigurationOptions>();
            services.AddSingleton<IConfigureOptions<TelemetryConfiguration>, TelemetryConfigurationOptionsSetup>();
            if (options != null)
            {
                services.Configure<ApplicationInsightsServiceOptions>(options);
            }
            services.AddSingleton<TelemetryConfiguration>(provider => provider.GetService<IOptions<TelemetryConfiguration>>().Value);

            services.AddSingleton<TelemetryClient>();

            services.AddSingleton<ApplicationInsightsInitializer, ApplicationInsightsInitializer>();
            services.AddSingleton<IApplicationInsightDiagnosticListener, AspNetCoreHostingDiagnosticListener>();
            services.AddSingleton<IApplicationInsightDiagnosticListener, AspNetCoreMvcDiagnosticsListener>();
            services.AddSingleton<IStartupFilter, ApplicationInsightsStartupFilter>();

            services.AddSingleton<JavaScriptSnippet>();

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
        /// <param name="serviceOptions">Telemetry configuration to populate</param>
        internal static void AddTelemetryConfiguration(IConfiguration config, ApplicationInsightsServiceOptions serviceOptions)
        {
            string instrumentationKey = config[InstrumentationKeyForWebSites];
            if (string.IsNullOrWhiteSpace(instrumentationKey))
            {
                instrumentationKey = config[InstrumentationKeyFromConfig];
            }

            if (!string.IsNullOrWhiteSpace(instrumentationKey))
            {
                serviceOptions.InstrumentationKey = instrumentationKey;
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
                    serviceOptions.DeveloperMode = developerMode;
                }
            }

            string endpointAddress = config[EndpointAddressForWebSites];
            if (string.IsNullOrWhiteSpace(endpointAddress))
            {
                endpointAddress = config[EndpointAddressFromConfig];
            }

            if (!string.IsNullOrWhiteSpace(endpointAddress))
            {
                serviceOptions.EndpointAddress = endpointAddress;
            }

            serviceOptions.Version = config[VersionKeyFromConfig];
        }
    }
}
