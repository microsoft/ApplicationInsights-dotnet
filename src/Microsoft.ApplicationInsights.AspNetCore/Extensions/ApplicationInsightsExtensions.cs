namespace Microsoft.Extensions.DependencyInjection
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using AspNetCore.Builder;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.AspNetCore;
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.ApplicationInsights.AspNetCore.Logging;
    using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DependencyCollector;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.ApplicationId;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
    using Microsoft.ApplicationInsights.WindowsServer;    
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Configuration.Memory;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Extension methods for <see cref="IServiceCollection"/> that allow adding Application Insights services to application.
    /// </summary>
    public static class ApplicationInsightsExtensions
    {
        private const string VersionKeyFromConfig = "version";
        private const string InstrumentationKeyFromConfig = "ApplicationInsights:InstrumentationKey";
        private const string DeveloperModeFromConfig = "ApplicationInsights:TelemetryChannel:DeveloperMode";
        private const string EndpointAddressFromConfig = "ApplicationInsights:TelemetryChannel:EndpointAddress";

        private const string InstrumentationKeyForWebSites = "APPINSIGHTS_INSTRUMENTATIONKEY";
        private const string DeveloperModeForWebSites = "APPINSIGHTS_DEVELOPER_MODE";
        private const string EndpointAddressForWebSites = "APPINSIGHTS_ENDPOINTADDRESS";

        [Obsolete]
        public static IApplicationBuilder UseApplicationInsightsRequestTelemetry(this IApplicationBuilder app)
        {
            return app;
        }

        [Obsolete]
        public static IApplicationBuilder UseApplicationInsightsExceptionTelemetry(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ExceptionTrackingMiddleware>();
        }

        /// <summary>
        /// Adds Application Insights services into service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <param name="instrumentationKey">Instrumentation key to use for telemetry.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddApplicationInsightsTelemetry(this IServiceCollection services,
            string instrumentationKey)
        {
            services.AddApplicationInsightsTelemetry(options => options.InstrumentationKey = instrumentationKey);
            return services;
        }

        /// <summary>
        /// Adds Application Insights services into service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <param name="configuration">Configuration to use for sending telemetry.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddApplicationInsightsTelemetry(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddApplicationInsightsTelemetry(options => AddTelemetryConfiguration(configuration, options));
            return services;
        }

        /// <summary>
        /// Adds Application Insights services into service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <param name="options">The action used to configure the options.</param>
        /// <returns>
        /// The <see cref="IServiceCollection"/>.
        /// </returns>
        public static IServiceCollection AddApplicationInsightsTelemetry(this IServiceCollection services,
            Action<ApplicationInsightsServiceOptions> options)
        {
            services.AddApplicationInsightsTelemetry();
            services.Configure(options);
            return services;
        }

        /// <summary>
        /// Adds Application Insights services into service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <param name="options">The options instance used to configure with.</param>
        /// <returns>
        /// The <see cref="IServiceCollection"/>.
        /// </returns>
        public static IServiceCollection AddApplicationInsightsTelemetry(this IServiceCollection services,
            ApplicationInsightsServiceOptions options)
        {
            services.AddApplicationInsightsTelemetry();
            services.Configure((ApplicationInsightsServiceOptions o) =>
            {
                o.ApplicationVersion = options.ApplicationVersion;
                o.DeveloperMode = options.DeveloperMode;
                o.EnableAdaptiveSampling = options.EnableAdaptiveSampling;
                o.EnableAuthenticationTrackingJavaScript = options.EnableAuthenticationTrackingJavaScript;
                o.EnableDebugLogger = options.EnableDebugLogger;
                o.EnableQuickPulseMetricStream = options.EnableQuickPulseMetricStream;
                o.EndpointAddress = options.EndpointAddress;
                o.InstrumentationKey = options.InstrumentationKey;
                o.EnableHeartbeat = options.EnableHeartbeat;
                o.AddAutoCollectedMetricExtractor = options.AddAutoCollectedMetricExtractor;
            });
            return services;
        }

        /// <summary>
        /// Adds Application Insights services into service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <returns>
        /// The <see cref="IServiceCollection"/>.
        /// </returns>
        public static IServiceCollection AddApplicationInsightsTelemetry(this IServiceCollection services)
        {
            if (!IsApplicationInsightsAdded(services))
            {
                services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

                services
                    .AddSingleton<ITelemetryInitializer, ApplicationInsights.AspNetCore.TelemetryInitializers.
                        DomainNameRoleInstanceTelemetryInitializer>();
                services.AddSingleton<ITelemetryInitializer, AzureWebAppRoleEnvironmentTelemetryInitializer>();
                services.AddSingleton<ITelemetryInitializer, ComponentVersionTelemetryInitializer>();
                services.AddSingleton<ITelemetryInitializer, ClientIpHeaderTelemetryInitializer>();
                services.AddSingleton<ITelemetryInitializer, OperationNameTelemetryInitializer>();
                services.AddSingleton<ITelemetryInitializer, SyntheticTelemetryInitializer>();
                services.AddSingleton<ITelemetryInitializer, WebSessionTelemetryInitializer>();
                services.AddSingleton<ITelemetryInitializer, WebUserTelemetryInitializer>();
                services.AddSingleton<ITelemetryInitializer, AspNetCoreEnvironmentTelemetryInitializer>();
                services.AddSingleton<ITelemetryInitializer, HttpDependenciesParsingTelemetryInitializer>();
                services.TryAddSingleton<ITelemetryChannel, ServerTelemetryChannel>();

                services.AddSingleton<ITelemetryModule, DependencyTrackingTelemetryModule>();
                services.ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((module, o) =>
                {
                    module.EnableLegacyCorrelationHeadersInjection =
                        o.DependencyCollectionOptions.EnableLegacyCorrelationHeadersInjection;

                    var excludedDomains = module.ExcludeComponentCorrelationHttpHeadersOnDomains;
                    excludedDomains.Add("core.windows.net");
                    excludedDomains.Add("core.chinacloudapi.cn");
                    excludedDomains.Add("core.cloudapi.de");
                    excludedDomains.Add("core.usgovcloudapi.net");

                    if (module.EnableLegacyCorrelationHeadersInjection)
                    {
                        excludedDomains.Add("localhost");
                        excludedDomains.Add("127.0.0.1");
                    }

                    var includedActivities = module.IncludeDiagnosticSourceActivities;
                    includedActivities.Add("Microsoft.Azure.EventHubs");
                    includedActivities.Add("Microsoft.Azure.ServiceBus");

                    module.EnableW3CHeadersInjection = o.RequestCollectionOptions.EnableW3CDistributedTracing;
                });

                services.ConfigureTelemetryModule<RequestTrackingTelemetryModule>((module, options) =>
                {
                    module.CollectionOptions = options.RequestCollectionOptions;
                });

                services.AddSingleton<ITelemetryModule, PerformanceCollectorModule>();
                services.AddSingleton<ITelemetryModule, AppServicesHeartbeatTelemetryModule>();
                services.AddSingleton<ITelemetryModule, AzureInstanceMetadataTelemetryModule>();
                services.AddSingleton<ITelemetryModule, QuickPulseTelemetryModule>();
                services.AddSingleton<ITelemetryModule, RequestTrackingTelemetryModule>();
                services.AddSingleton<TelemetryConfiguration>(provider =>
                    provider.GetService<IOptions<TelemetryConfiguration>>().Value);

                services.TryAddSingleton<IApplicationIdProvider, ApplicationInsightsApplicationIdProvider>();

                services.AddSingleton<TelemetryClient>();

                services
                    .TryAddSingleton<IConfigureOptions<ApplicationInsightsServiceOptions>,
                        DefaultApplicationInsightsServiceConfigureOptions>();

                // Using startup filter instead of starting DiagnosticListeners directly because
                // AspNetCoreHostingDiagnosticListener injects TelemetryClient that injects TelemetryConfiguration
                // that requires IOptions infrastructure to run and initialize
                services.AddSingleton<IStartupFilter, ApplicationInsightsStartupFilter>();

                services.AddSingleton<JavaScriptSnippet>();                

                services.AddOptions();
                services.AddSingleton<IOptions<TelemetryConfiguration>, TelemetryConfigurationOptions>();
                services.AddSingleton<IConfigureOptions<TelemetryConfiguration>, TelemetryConfigurationOptionsSetup>();

                // NetStandard2.0 has a package reference to Microsoft.Extensions.Logging.ApplicationInsights, and
                // enables ApplicationInsightsLoggerProvider by default.                
#if NETSTANDARD2_0
                services.AddLogging(loggingBuilder =>
                {
                     loggingBuilder.AddApplicationInsights();

                    // The default behavior is to capture only logs above Warning level from all categories.
                    // This can achieved with this code level filter -> loggingBuilder.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>("",LogLevel.Warning);
                    // However, this will make it impossible to override this behavior from Configuration like below using appsettings.json:
                    //"ApplicationInsights": {
                    // "LogLevel": {
                    // "": "Error"
                    // }
                    // },
                    // The reason is as both rules will match the filter, the last one added wins.
                    // To ensure that the default filter is in the beginning of filter rules, so that user override from Configuration will always win, 
                    // we add code filter rule to the 0th position as below.

                    loggingBuilder.Services.Configure<LoggerFilterOptions>
                    (options => options.Rules.Insert(0,
                        new LoggerFilterRule(
                            "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider", null,
                            LogLevel.Warning, null)));
                });                                
#endif
            }

            return services;
        }

        /// <summary>
        /// Adds an Application Insights Telemetry Processor into a service collection via a <see cref="ITelemetryProcessorFactory"/>.
        /// </summary>
        /// <typeparam name="T">Type of the telemetry processor to add.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <returns>
        /// The <see cref="IServiceCollection"/>.
        /// </returns>
        public static IServiceCollection AddApplicationInsightsTelemetryProcessor<T>(this IServiceCollection services)
            where T : ITelemetryProcessor
        {
            return services.AddSingleton<ITelemetryProcessorFactory>(serviceProvider =>
                new TelemetryProcessorFactory(serviceProvider, typeof(T)));
        }

        /// <summary>
        /// Adds an Application Insights Telemetry Processor into a service collection via a <see cref="ITelemetryProcessorFactory"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <param name="telemetryProcessorType">Type of the telemetry processor to add.</param>
        /// <returns>
        /// The <see cref="IServiceCollection"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">The <paramref name="telemetryProcessorType"/> argument is null.</exception>
        /// <exception cref="ArgumentException">The <paramref name="telemetryProcessorType"/> type does not implement <see cref="ITelemetryProcessor"/>.</exception>
        public static IServiceCollection AddApplicationInsightsTelemetryProcessor(this IServiceCollection services,
            Type telemetryProcessorType)
        {
            if (telemetryProcessorType == null)
            {
                throw new ArgumentNullException(nameof(telemetryProcessorType));
            }

            if (!telemetryProcessorType.GetTypeInfo().ImplementedInterfaces.Contains(typeof(ITelemetryProcessor)))
            {
                throw new ArgumentException(nameof(telemetryProcessorType));
            }

            return services.AddSingleton<ITelemetryProcessorFactory>(serviceProvider =>
                new TelemetryProcessorFactory(serviceProvider, telemetryProcessorType));
        }

        /// <summary>
        /// Extension method to provide configuration logic for application insights telemetry module.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <param name="configModule">Action used to configure the module.</param>
        /// <returns>
        /// The <see cref="IServiceCollection"/>.
        /// </returns>
        [Obsolete("Use ConfigureTelemetryModule overload that accepts ApplicationInsightsServiceOptions.")]
        public static IServiceCollection ConfigureTelemetryModule<T>(this IServiceCollection services, Action<T> configModule) where T : ITelemetryModule
        {
            if (configModule == null)
            {
                throw new ArgumentNullException(nameof(configModule));
            }

            return services.AddSingleton(typeof(ITelemetryModuleConfigurator),
                new TelemetryModuleConfigurator((config, options) => configModule((T)config), typeof(T)));
        }

        /// <summary>
        /// Extension method to provide configuration logic for application insights telemetry module.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <param name="configModule">Action used to configure the module.</param>
        /// <returns>
        /// The <see cref="IServiceCollection"/>.
        /// </returns>        
        public static IServiceCollection ConfigureTelemetryModule<T>(this IServiceCollection services,
            Action<T, ApplicationInsightsServiceOptions> configModule) where T : ITelemetryModule
        {
            if (configModule == null)
            {
                throw new ArgumentNullException(nameof(configModule));
            }

            return services.AddSingleton(typeof(ITelemetryModuleConfigurator),
                new TelemetryModuleConfigurator((config, options) => configModule((T) config, options), typeof(T)));
        }

        /// <summary>
        /// Adds Application Insight specific configuration properties to <see cref="IConfigurationBuilder"/>.
        /// </summary>
        /// <param name="configurationSourceRoot">The <see cref="IConfigurationBuilder"/> instance.</param>
        /// <param name="developerMode">Enables or disables developer mode.</param>
        /// <param name="endpointAddress">Sets telemetry endpoint address.</param>
        /// <param name="instrumentationKey">Sets instrumentation key.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
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
                telemetryConfigValues.Add(new KeyValuePair<string, string>(DeveloperModeForWebSites,
                    developerMode.Value.ToString()));
                wasAnythingSet = true;
            }

            if (instrumentationKey != null)
            {
                telemetryConfigValues.Add(new KeyValuePair<string, string>(InstrumentationKeyForWebSites,
                    instrumentationKey));
                wasAnythingSet = true;
            }

            if (endpointAddress != null)
            {
                telemetryConfigValues.Add(new KeyValuePair<string, string>(EndpointAddressForWebSites,
                    endpointAddress));
                wasAnythingSet = true;
            }

            if (wasAnythingSet)
            {
                configurationSourceRoot.Add(new MemoryConfigurationSource() {InitialData = telemetryConfigValues});
            }

            return configurationSourceRoot;
        }

        /// <summary>
        /// Read from configuration
        /// Config.json will look like this:
        /// <para>
        ///      "ApplicationInsights": {
        ///          "InstrumentationKey": "11111111-2222-3333-4444-555555555555"
        ///          "TelemetryChannel": {
        ///              EndpointAddress: "http://dc.services.visualstudio.com/v2/track",
        ///              DeveloperMode: true
        ///          }
        ///      }
        /// </para>
        /// Values can also be read from environment variables to support azure web sites configuration:
        /// </summary>
        /// <param name="config">Configuration to read variables from.</param>
        /// <param name="serviceOptions">Telemetry configuration to populate.</param>
        internal static void AddTelemetryConfiguration(IConfiguration config,
            ApplicationInsightsServiceOptions serviceOptions)
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

            var version = config[VersionKeyFromConfig];
            if (!string.IsNullOrWhiteSpace(version))
            {
                serviceOptions.ApplicationVersion = version;
            }
        }

        private static bool IsApplicationInsightsAdded(IServiceCollection services)
        {
            // We treat TelemetryClient as a marker that AI services were added to service collection
            return services.Any(service => service.ServiceType == typeof(TelemetryClient));
        }
    }
}
