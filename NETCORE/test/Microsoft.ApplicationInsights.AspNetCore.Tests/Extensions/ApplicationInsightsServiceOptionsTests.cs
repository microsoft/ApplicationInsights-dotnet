using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
#if NETCOREAPP
using Microsoft.ApplicationInsights.Extensibility.EventCounterCollector;
#endif 
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Microsoft.ApplicationInsights.WindowsServer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace Microsoft.ApplicationInsights.AspNetCore.Tests.Extensions
{
    /// <summary>
    /// Test class for <see cref="ApplicationInsightsServiceOptions"/>.
    /// </summary>
    /// <remarks>
    /// This configuration can be read from a JSON file by the configuration factory or through code by passing ApplicationInsightsServiceOptions. 
    /// <param name="configType">
    /// DefaultConfiguration - calls services.AddApplicationInsightsTelemetryWorkerService() which reads IConfiguration from user application automatically.
    /// SuppliedConfiguration - invokes services.AddApplicationInsightsTelemetryWorkerService(configuration) where IConfiguration object is supplied by caller.
    /// Code - Caller creates an instance of ApplicationInsightsServiceOptions and passes it. This option overrides all configuration being used in JSON file. 
    /// There is a special case where NULL values in these properties - InstrumentationKey, ConnectionString, EndpointAddress and DeveloperMode are overwritten. We check IConfiguration object to see if these properties have values, if values are present then we override it. 
    /// </param>
    /// <param name="isEnable">Sets the value for property EnableEventCounterCollectionModule.</param>
    /// </remarks>
    public class ApplicationInsightsServiceOptionsTests
    {
        private static IServiceProvider TestShim(string configType, bool isEnabled, Action<ApplicationInsightsServiceOptions, bool> testConfig)
        {
            // ARRANGE
            Action<ApplicationInsightsServiceOptions> serviceOptions = null;
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "content", "config-all-settings-" + isEnabled.ToString().ToLower() + ".json");

            if (configType == "Code")
            {
                filePath = null;

                // This will set the property defined in the test.
                serviceOptions = o => { testConfig(o, isEnabled); };
            }

            // ACT
            var services = CreateServicesAndAddApplicationinsightsWorker(
                jsonPath: filePath, 
                serviceOptions: serviceOptions, 
                useDefaultConfig: configType == "DefaultConfiguration" ? true : false);

            IServiceProvider serviceProvider = services.BuildServiceProvider();

            // Get telemetry client to trigger TelemetryConfig setup.
            var tc = serviceProvider.GetService<TelemetryClient>();

            // Verify that Modules were added to DI.
            var modules = serviceProvider.GetServices<ITelemetryModule>();
            Assert.NotNull(modules);

            return serviceProvider;
        }

        private static ServiceCollection CreateServicesAndAddApplicationinsightsWorker(string jsonPath, Action<ApplicationInsightsServiceOptions> serviceOptions = null, bool useDefaultConfig = true)
        {
            IConfigurationRoot config;
            var services = new ServiceCollection()
                .AddSingleton<IHostingEnvironment>(new HostingEnvironment() { ContentRootPath = Directory.GetCurrentDirectory() })
                .AddSingleton<DiagnosticListener>(new DiagnosticListener("TestListener"));

            if (jsonPath != null)
            {
                var jsonFullPath = Path.Combine(Directory.GetCurrentDirectory(), jsonPath);
                Console.WriteLine("json:" + jsonFullPath);
                try
                {
                    config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile(jsonFullPath).Build();
                }
                catch (Exception)
                {
                    throw new Exception("Unable to build with json:" + jsonFullPath);
                }
            }
            else
            {
                var configBuilder = new ConfigurationBuilder()
                    .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"), true)
                    .AddEnvironmentVariables();
                config = configBuilder.Build();
            }

#if NET46
            // In NET46, we don't read from default configuration or bind configuration. 
            services.AddApplicationInsightsTelemetry(config);
#else
            if (useDefaultConfig)
            {
                services.AddSingleton<IConfiguration>(config);
                services.AddApplicationInsightsTelemetry();
            }
            else
            {
                services.AddApplicationInsightsTelemetry(config);
            }
#endif

            if (serviceOptions != null)
            {
                services.Configure(serviceOptions);
            }

            return (ServiceCollection)services;
        }

        /// <summary>
        /// User could enable or disable PerformanceCounterCollectionModule by setting EnablePerformanceCounterCollectionModule.
        /// </summary>
        [Theory]
#if !NET46
        [InlineData("DefaultConfiguration", true)]
        [InlineData("DefaultConfiguration", false)]
        [InlineData("SuppliedConfiguration", true)]
        [InlineData("SuppliedConfiguration", false)]
#endif
        [InlineData("Code", true)]
        [InlineData("Code", false)]
        public static void UserCanEnableAndDisablePerfCollectorModule(string configType, bool isEnable)
        {
            IServiceProvider serviceProvider = TestShim(configType: configType, isEnabled: isEnable, testConfig: (o, b) => o.EnablePerformanceCounterCollectionModule = b );

            var modules = serviceProvider.GetServices<ITelemetryModule>();
            var module = modules.OfType<PerformanceCollectorModule>().Single();
            Assert.Equal(isEnable, module.IsInitialized);
        }

#if NETCOREAPP
        /// <summary>
        /// User could enable or disable EventCounterCollectionModule by setting EnableEventCounterCollectionModule.
        /// </summary>
        [Theory]

        [InlineData("DefaultConfiguration", true)]
        [InlineData("DefaultConfiguration", false)]
        [InlineData("SuppliedConfiguration", true)]
        [InlineData("SuppliedConfiguration", false)]

        [InlineData("Code", true)]
        [InlineData("Code", false)]
        public static void UserCanEnableAndDisableEventCounterCollectorModule(string configType, bool isEnable)
        {
            IServiceProvider serviceProvider = TestShim(configType: configType, isEnabled: isEnable, testConfig: (o, b) => o.EnableEventCounterCollectionModule = b);

            var modules = serviceProvider.GetServices<ITelemetryModule>();
            var module = modules.OfType<EventCounterCollectionModule>().Single();
            Assert.Equal(isEnable, module.IsInitialized);
        }
#endif

        /// <summary>
        /// User could enable or disable DependencyTrackingTelemetryModule by setting EnableDependencyTrackingTelemetryModule.
        /// </summary>
        [Theory]
#if !NET46
        [InlineData("DefaultConfiguration", true)]
        [InlineData("DefaultConfiguration", false)]
        [InlineData("SuppliedConfiguration", true)]
        [InlineData("SuppliedConfiguration", false)]
#endif
        [InlineData("Code", true)]
        [InlineData("Code", false)]
        public static void UserCanEnableAndDisableDependencyCollectorModule(string configType, bool isEnable)
        {
            IServiceProvider serviceProvider = TestShim(configType: configType, isEnabled: isEnable, testConfig: (o, b) => o.EnableDependencyTrackingTelemetryModule = b);

            var modules = serviceProvider.GetServices<ITelemetryModule>();
            var module = modules.OfType<DependencyTrackingTelemetryModule>().Single();
            Assert.Equal(isEnable, module.IsInitialized);
        }

        /// <summary>
        /// User could enable or disable QuickPulseCollectorModule by setting EnableQuickPulseMetricStream.
        /// </summary>
        [Theory]
#if !NET46
        [InlineData("DefaultConfiguration", true)]
        [InlineData("DefaultConfiguration", false)]
        [InlineData("SuppliedConfiguration", true)]
        [InlineData("SuppliedConfiguration", false)]
#endif
        [InlineData("Code", true)]
        [InlineData("Code", false)]
        public static void UserCanEnableAndDisableQuickPulseCollectorModule(string configType, bool isEnable)
        {
            IServiceProvider serviceProvider = TestShim(configType: configType, isEnabled: isEnable, testConfig: (o, b) => o.EnableQuickPulseMetricStream = b);

            var modules = serviceProvider.GetServices<ITelemetryModule>();
            var module = modules.OfType<QuickPulseTelemetryModule>().Single();
            Assert.Equal(isEnable, module.IsInitialized);
        }

        /// <summary>
        /// User could enable or disable AzureInstanceMetadataModule by setting EnableAzureInstanceMetadataTelemetryModule.
        /// </summary>
        [Theory]
#if !NET46
        [InlineData("DefaultConfiguration", true)]
        [InlineData("DefaultConfiguration", false)]
        [InlineData("SuppliedConfiguration", true)]
        [InlineData("SuppliedConfiguration", false)]
#endif
        [InlineData("Code", true)]
        [InlineData("Code", false)]
        public static void UserCanEnableAndDisableAzureInstanceMetadataModule(string configType, bool isEnable)
        {
            IServiceProvider serviceProvider = TestShim(configType: configType, isEnabled: isEnable, testConfig: (o, b) => o.EnableAzureInstanceMetadataTelemetryModule = b);

            var modules = serviceProvider.GetServices<ITelemetryModule>();
            var module = modules.OfType<AzureInstanceMetadataTelemetryModule>().Single();
            Assert.Equal(isEnable, module.IsInitialized);
        }

        /// <summary>
        /// User could enable or disable RequestTrackingTelemetryModule by setting EnableRequestTrackingTelemetryModule.
        /// </summary>
        [Theory]
#if !NET46
        [InlineData("DefaultConfiguration", true)]
        [InlineData("DefaultConfiguration", false)]
        [InlineData("SuppliedConfiguration", true)]
        [InlineData("SuppliedConfiguration", false)]
#endif
        [InlineData("Code", true)]
        [InlineData("Code", false)]
        public static void UserCanEnableAndDisableRequestCounterCollectorModule(string configType, bool isEnable)
        {
            IServiceProvider serviceProvider = TestShim(configType: configType, isEnabled: isEnable, testConfig: (o, b) => o.EnableRequestTrackingTelemetryModule = b);

            var modules = serviceProvider.GetServices<ITelemetryModule>();
            var module = modules.OfType<RequestTrackingTelemetryModule>().Single();
            Assert.Equal(isEnable, module.IsInitialized);
        }

        /// <summary>
        /// User could enable or disable AppServiceHeartbeatModule by setting EnableAppServicesHeartbeatTelemetryModule.
        /// </summary>
        [Theory]
#if !NET46
        [InlineData("DefaultConfiguration", true)]
        [InlineData("DefaultConfiguration", false)]
        [InlineData("SuppliedConfiguration", true)]
        [InlineData("SuppliedConfiguration", false)]
#endif
        [InlineData("Code", true)]
        [InlineData("Code", false)]
        public static void UserCanEnableAndDisableAppServiceHeartbeatModule(string configType, bool isEnable)
        {
            IServiceProvider serviceProvider = TestShim(configType: configType, isEnabled: isEnable, testConfig: (o, b) => o.EnableAppServicesHeartbeatTelemetryModule = b);

            var modules = serviceProvider.GetServices<ITelemetryModule>();
            var module = modules.OfType<AppServicesHeartbeatTelemetryModule>().Single();
            Assert.Equal(isEnable, module.IsInitialized);
        }
    }
}