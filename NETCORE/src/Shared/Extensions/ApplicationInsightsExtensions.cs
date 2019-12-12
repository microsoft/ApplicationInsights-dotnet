namespace Microsoft.Extensions.DependencyInjection
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Reflection;

    using Microsoft.ApplicationInsights;
#if AI_ASPNETCORE_WEB
    using Microsoft.ApplicationInsights.AspNetCore;
    using Microsoft.ApplicationInsights.AspNetCore.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
#endif

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DependencyCollector;
    using Microsoft.ApplicationInsights.Extensibility;

#if NETSTANDARD2_0
    using Microsoft.ApplicationInsights.Extensibility.EventCounterCollector;
#endif

    using Microsoft.ApplicationInsights.Extensibility.Implementation.ApplicationId;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
    using Microsoft.ApplicationInsights.WindowsServer;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;

#if AI_ASPNETCORE_WORKER
    using Microsoft.ApplicationInsights.WorkerService;
    using Microsoft.ApplicationInsights.WorkerService.Implementation.Tracing;
    using Microsoft.ApplicationInsights.WorkerService.TelemetryInitializers;
#endif

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Configuration.Memory;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    using Shared.Implementation;

    /// <summary>
    /// Extension methods for <see cref="IServiceCollection"/> that allow adding Application Insights services to application.
    /// </summary>
    public static partial class ApplicationInsightsExtensions
    {
        private const string VersionKeyFromConfig = "version";
        private const string InstrumentationKeyFromConfig = "ApplicationInsights:InstrumentationKey";
        private const string ConnectionStringFromConfig = "ApplicationInsights:ConnectionString";
        private const string DeveloperModeFromConfig = "ApplicationInsights:TelemetryChannel:DeveloperMode";
        private const string EndpointAddressFromConfig = "ApplicationInsights:TelemetryChannel:EndpointAddress";

        private const string InstrumentationKeyForWebSites = "APPINSIGHTS_INSTRUMENTATIONKEY";
        private const string ConnectionStringEnvironmentVariable = "APPLICATIONINSIGHTS_CONNECTION_STRING";
        private const string DeveloperModeForWebSites = "APPINSIGHTS_DEVELOPER_MODE";
        private const string EndpointAddressForWebSites = "APPINSIGHTS_ENDPOINTADDRESS";

        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification = "Used in NetStandard2.0 build.")]
        private const string EventSourceNameForSystemRuntime = "System.Runtime";

        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification = "Used in NetStandard2.0 build.")]
        private const string EventSourceNameForAspNetCoreHosting = "Microsoft.AspNetCore.Hosting";

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
        public static IServiceCollection AddApplicationInsightsTelemetryProcessor(this IServiceCollection services, Type telemetryProcessorType)
        {
            if (telemetryProcessorType == null)
            {
                throw new ArgumentNullException(nameof(telemetryProcessorType));
            }

            if (!telemetryProcessorType.GetTypeInfo().ImplementedInterfaces.Contains(typeof(ITelemetryProcessor)))
            {
                throw new ArgumentException(nameof(telemetryProcessorType) + "does not implement ITelemetryProcessor.");
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
        public static IServiceCollection ConfigureTelemetryModule<T>(this IServiceCollection services, Action<T> configModule)
            where T : ITelemetryModule
        {
            if (configModule == null)
            {
                throw new ArgumentNullException(nameof(configModule));
            }

            return services.AddSingleton(
                typeof(ITelemetryModuleConfigurator),
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
        public static IServiceCollection ConfigureTelemetryModule<T>(
            this IServiceCollection services,
            Action<T, ApplicationInsightsServiceOptions> configModule)
            where T : ITelemetryModule
        {
            if (configModule == null)
            {
                throw new ArgumentNullException(nameof(configModule));
            }

            return services.AddSingleton(
                typeof(ITelemetryModuleConfigurator),
                new TelemetryModuleConfigurator((config, options) => configModule((T)config, options), typeof(T)));
        }

        /// <summary>
        /// Adds Application Insight specific configuration properties to <see cref="IConfigurationBuilder"/>.
        /// </summary>
        /// <param name="configurationSourceRoot">The <see cref="IConfigurationBuilder"/> instance.</param>
        /// <param name="developerMode">Enables or disables developer mode.</param>
        /// <param name="endpointAddress">Sets telemetry endpoint address.</param>
        /// <param name="instrumentationKey">Sets instrumentation key.</param>
        /// <param name="connectionString">Sets connection string.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddApplicationInsightsSettings(
            this IConfigurationBuilder configurationSourceRoot,
            bool? developerMode = null,
            string endpointAddress = null,
            string instrumentationKey = null,
            string connectionString = null)
        {
            if (configurationSourceRoot == null)
            {
                throw new ArgumentNullException(nameof(configurationSourceRoot));
            }

            var telemetryConfigValues = new List<KeyValuePair<string, string>>();

            bool wasAnythingSet = false;

            if (developerMode != null)
            {
                telemetryConfigValues.Add(new KeyValuePair<string, string>(
                    DeveloperModeForWebSites,
#if !NETSTANDARD1_6
                    developerMode.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
#else
                    developerMode.Value.ToString()));
#endif
                wasAnythingSet = true;
            }

            if (connectionString != null)
            {
                telemetryConfigValues.Add(new KeyValuePair<string, string>(ConnectionStringEnvironmentVariable, connectionString));
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
        /// <para>
        ///      "ApplicationInsights": {
        ///          "InstrumentationKey": "11111111-2222-3333-4444-555555555555",
        ///          "TelemetryChannel": {
        ///              "EndpointAddress": "http://dc.services.visualstudio.com/v2/track",
        ///              "DeveloperMode": true
        ///          }
        ///      }.
        /// </para>
        /// Or.
        /// <para>
        ///      "ApplicationInsights": {
        ///          "ConnectionString" : "InstrumentationKey=11111111-2222-3333-4444-555555555555;IngestionEndpoint=http://dc.services.visualstudio.com"
        ///          "TelemetryChannel": {
        ///              "DeveloperMode": true
        ///          }
        ///      }.
        /// </para>
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
                if (config.TryGetValue(primaryKey: ConnectionStringEnvironmentVariable, backupKey: ConnectionStringFromConfig, value: out string connectionStringValue))
                {
                    serviceOptions.ConnectionString = connectionStringValue;
                }

                if (config.TryGetValue(primaryKey: InstrumentationKeyForWebSites, backupKey: InstrumentationKeyFromConfig, value: out string instrumentationKey))
                {
                    serviceOptions.InstrumentationKey = instrumentationKey;
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

        private static void AddCommonInitializers(IServiceCollection services)
        {
#if AI_ASPNETCORE_WEB
            services.AddSingleton<ITelemetryInitializer, Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers.DomainNameRoleInstanceTelemetryInitializer>();
#else
            services.AddSingleton<ITelemetryInitializer, Microsoft.ApplicationInsights.WorkerService.TelemetryInitializers.DomainNameRoleInstanceTelemetryInitializer>();
#endif
            services.AddSingleton<ITelemetryInitializer, HttpDependenciesParsingTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, ComponentVersionTelemetryInitializer>();
        }

        private static void AddCommonTelemetryModules(IServiceCollection services)
        {
            services.AddSingleton<ITelemetryModule, PerformanceCollectorModule>();
            services.AddSingleton<ITelemetryModule, AppServicesHeartbeatTelemetryModule>();
            services.AddSingleton<ITelemetryModule, AzureInstanceMetadataTelemetryModule>();
            services.AddSingleton<ITelemetryModule, QuickPulseTelemetryModule>();

            AddAndConfigureDependencyTracking(services);
#if NETSTANDARD2_0
            services.AddSingleton<ITelemetryModule, EventCounterCollectionModule>();
#endif
        }

        private static void AddTelemetryChannel(IServiceCollection services)
        {
            services.TryAddSingleton<ITelemetryChannel, ServerTelemetryChannel>();
        }

        private static void AddDefaultApplicationIdProvider(IServiceCollection services)
        {
            services.TryAddSingleton<IApplicationIdProvider, ApplicationInsightsApplicationIdProvider>();
        }

        private static void AddTelemetryConfigAndClient(IServiceCollection services)
        {
            services.AddOptions();
            services.AddSingleton<IOptions<TelemetryConfiguration>, TelemetryConfigurationOptions>();
            services.AddSingleton<IConfigureOptions<TelemetryConfiguration>, TelemetryConfigurationOptionsSetup>();
            services.AddSingleton<TelemetryConfiguration>(provider =>
                provider.GetService<IOptions<TelemetryConfiguration>>().Value);
            services.AddSingleton<TelemetryClient>();
        }

        private static void AddAndConfigureDependencyTracking(IServiceCollection services)
        {
            services.AddSingleton<ITelemetryModule, DependencyTrackingTelemetryModule>();

            services.ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((module, o) =>
            {
                if (o.EnableDependencyTrackingTelemetryModule)
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
                }
            });
        }

#if NETSTANDARD2_0
        private static void AddEventCounterIfNotExist(EventCounterCollectionModule eventCounterModule, string eventSource, string eventCounterName)
        {
            if (!eventCounterModule.Counters.Any(req => req.EventSourceName.Equals(eventSource, StringComparison.Ordinal) && req.EventCounterName.Equals(eventCounterName, StringComparison.Ordinal)))
            {
                eventCounterModule.Counters.Add(new EventCounterCollectionRequest(eventSource, eventCounterName));
            }
        }

        private static void ConfigureEventCounterModuleWithSystemCounters(IServiceCollection services)
        {
            services.ConfigureTelemetryModule<EventCounterCollectionModule>((eventCounterModule, options) =>
            {
                if (options.EnableEventCounterCollectionModule)
                {
                    // Ref this code for actual names. https://github.com/dotnet/coreclr/blob/dbc5b56c48ce30635ee8192c9814c7de998043d5/src/System.Private.CoreLib/src/System/Diagnostics/Eventing/RuntimeEventSource.cs
                    AddEventCounterIfNotExist(eventCounterModule, EventSourceNameForSystemRuntime, "cpu-usage");
                    AddEventCounterIfNotExist(eventCounterModule, EventSourceNameForSystemRuntime, "working-set");
                    AddEventCounterIfNotExist(eventCounterModule, EventSourceNameForSystemRuntime, "gc-heap-size");
                    AddEventCounterIfNotExist(eventCounterModule, EventSourceNameForSystemRuntime, "gen-0-gc-count");
                    AddEventCounterIfNotExist(eventCounterModule, EventSourceNameForSystemRuntime, "gen-1-gc-count");
                    AddEventCounterIfNotExist(eventCounterModule, EventSourceNameForSystemRuntime, "gen-2-gc-count");
                    AddEventCounterIfNotExist(eventCounterModule, EventSourceNameForSystemRuntime, "time-in-gc");
                    AddEventCounterIfNotExist(eventCounterModule, EventSourceNameForSystemRuntime, "gen-0-size");
                    AddEventCounterIfNotExist(eventCounterModule, EventSourceNameForSystemRuntime, "gen-1-size");
                    AddEventCounterIfNotExist(eventCounterModule, EventSourceNameForSystemRuntime, "gen-2-size");
                    AddEventCounterIfNotExist(eventCounterModule, EventSourceNameForSystemRuntime, "loh-size");
                    AddEventCounterIfNotExist(eventCounterModule, EventSourceNameForSystemRuntime, "alloc-rate");
                    AddEventCounterIfNotExist(eventCounterModule, EventSourceNameForSystemRuntime, "assembly-count");
                    AddEventCounterIfNotExist(eventCounterModule, EventSourceNameForSystemRuntime, "exception-count");
                    AddEventCounterIfNotExist(eventCounterModule, EventSourceNameForSystemRuntime, "threadpool-thread-count");
                    AddEventCounterIfNotExist(eventCounterModule, EventSourceNameForSystemRuntime, "monitor-lock-contention-count");
                    AddEventCounterIfNotExist(eventCounterModule, EventSourceNameForSystemRuntime, "threadpool-queue-length");
                    AddEventCounterIfNotExist(eventCounterModule, EventSourceNameForSystemRuntime, "threadpool-completed-items-count");
                    AddEventCounterIfNotExist(eventCounterModule, EventSourceNameForSystemRuntime, "active-timer-count");
                }
            });
        }

        private static void ConfigureEventCounterModuleWithAspNetCounters(IServiceCollection services)
        {
            services.ConfigureTelemetryModule<EventCounterCollectionModule>((eventCounterModule, options) =>
            {
                if (options.EnableEventCounterCollectionModule)
                {
                    // Ref this code for actual names. https://github.com/aspnet/AspNetCore/blob/f3f9a1cdbcd06b298035b523732b9f45b1408461/src/Hosting/Hosting/src/Internal/HostingEventSource.cs
                    AddEventCounterIfNotExist(eventCounterModule, EventSourceNameForAspNetCoreHosting, "requests-per-second");
                    AddEventCounterIfNotExist(eventCounterModule, EventSourceNameForAspNetCoreHosting, "total-requests");
                    AddEventCounterIfNotExist(eventCounterModule, EventSourceNameForAspNetCoreHosting, "current-requests");
                    AddEventCounterIfNotExist(eventCounterModule, EventSourceNameForAspNetCoreHosting, "failed-requests");
                }
            });
        }
#endif

        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", Justification = "services parameter is used in only NetStandard 2.0 build.")]
        private static void AddApplicationInsightsLoggerProvider(IServiceCollection services)
        {
#if NETSTANDARD2_0
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddApplicationInsights();

                // The default behavior is to capture only logs above Warning level from all categories.
                // This can achieved with this code level filter -> loggingBuilder.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>("",LogLevel.Warning);
                // However, this will make it impossible to override this behavior from Configuration like below using appsettings.json:
                // {
                //   "Logging": {
                //     "ApplicationInsights": {
                //       "LogLevel": {
                //         "": "Error"
                //       }
                //     }
                //   },
                //   ...
                // }
                // The reason is as both rules will match the filter, the last one added wins.
                // To ensure that the default filter is in the beginning of filter rules, so that user override from Configuration will always win,
                // we add code filter rule to the 0th position as below.
                loggingBuilder.Services.Configure<LoggerFilterOptions>(
                    options => options.Rules.Insert(
                        0,
                        new LoggerFilterRule(
                            "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider",
                            null,
                            LogLevel.Warning,
                            null)));
            });
#endif
        }
    }
}
