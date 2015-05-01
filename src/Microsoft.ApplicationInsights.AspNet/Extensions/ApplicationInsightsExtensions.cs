namespace Microsoft.Framework.DependencyInjection
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.AspNet;
    using Microsoft.ApplicationInsights.AspNet.ContextInitializers;
    using Microsoft.ApplicationInsights.AspNet.TelemetryInitializers;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNet.Builder;
    using Microsoft.AspNet.Mvc.Rendering;
    using Microsoft.Framework.ConfigurationModel;
    using Microsoft.ApplicationInsights.AspNet.JavaScript;
    using Microsoft.ApplicationInsights.AspNet.Config;

    public static class ApplicationInsightsExtensions
    {
        public static IApplicationBuilder UseApplicationInsightsRequestTelemetry(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RequestTrackingMiddleware>();
        }

        public static IApplicationBuilder UseApplicationInsightsExceptionTelemetry(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ExceptionTrackingMiddleware>();
        }

        public static void AddApplicationInsightsTelemetry(this IServiceCollection services, IConfiguration config)
        {
            services.AddSingleton<IContextInitializer, DomainNameRoleInstanceContextInitializer>();

            services.AddSingleton<ITelemetryInitializer, ClientIpHeaderTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, OperationIdTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, OperationNameTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, UserAgentTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, WebSessionTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, WebUserTelemetryInitializer>();

            services.AddSingleton<TelemetryConfiguration>(serviceProvider =>
            {
                var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
                telemetryConfiguration.TelemetryChannel = serviceProvider.GetService<ITelemetryChannel>() ?? telemetryConfiguration.TelemetryChannel;
                AddTelemetryConfiguration(config, telemetryConfiguration);
                AddServicesToCollection(serviceProvider, telemetryConfiguration.ContextInitializers);
                AddServicesToCollection(serviceProvider, telemetryConfiguration.TelemetryInitializers);
                return telemetryConfiguration;
            });

            services.AddSingleton<IJavaScriptSnippet, ApplicationInsightsJavaScript>();

            services.AddScoped<TelemetryClient>();

            services.AddScoped<RequestTelemetry>((svcs) => {
                // Default constructor need to be used
                var rt = new RequestTelemetry();
                return rt;
            });
        }

        public static IConfigurationSourceRoot AddApplicaitonInsightsSettings(this IConfigurationSourceRoot configurationSourceRoot, bool? developerMode = null, string endpointAddress = null, string instrumentationKey = null)
        {
            var configSource = new TelemetryConfigurationSource()
            {
                DeveloperMode = developerMode,
                EndpointAddress = endpointAddress,
                InstrumentationKey = instrumentationKey
            };
            configurationSourceRoot.Add(configSource);

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
            string instrumentationKey = config.Get(ConfigurationConstants.InstrumentationKeyForWebSites);
            if (string.IsNullOrWhiteSpace(instrumentationKey))
            {
                instrumentationKey = config.Get(ConfigurationConstants.InstrumentationKeyFromConfig);
            }
            if (!string.IsNullOrWhiteSpace(instrumentationKey))
            {
                telemetryConfiguration.InstrumentationKey = instrumentationKey;
            }

            string developerModeValue = config.Get(ConfigurationConstants.DeveloperModeForWebSites);
            if (string.IsNullOrWhiteSpace(developerModeValue))
            {
                developerModeValue = config.Get(ConfigurationConstants.DeveloperModeFromConfig);
            }
            if (!string.IsNullOrWhiteSpace(developerModeValue))
            {
                bool developerMode = false;
                if (bool.TryParse(developerModeValue, out developerMode))
                {
                    telemetryConfiguration.TelemetryChannel.DeveloperMode = developerMode;
                }
            }

            string endpointAddress = config.Get(ConfigurationConstants.EndpointAddressForWebSites);
            if (string.IsNullOrWhiteSpace(endpointAddress))
            {
                endpointAddress = config.Get(ConfigurationConstants.EndpointAddressFromConfig);
            }
            if (!string.IsNullOrWhiteSpace(endpointAddress))
            {
                var channel = telemetryConfiguration.TelemetryChannel as InProcessTelemetryChannel;
                if (channel != null)
                {
                    channel.EndpointAddress = endpointAddress;
                }
            }
        }

        private static void AddServicesToCollection<T>(IServiceProvider serviceProvider, ICollection<T> collection)
        {
            var services = serviceProvider.GetService<IEnumerable<T>>();
            foreach (T service in services)
            {
                collection.Add(service);
            }
        }
    }
}