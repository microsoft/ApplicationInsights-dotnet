using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace Microsoft.Extensions.DependencyInjection.Test
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Logging;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.AspNetCore;
    using Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners;
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.ApplicationInsights.AspNetCore.Logging;
    using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
    using Microsoft.ApplicationInsights.AspNetCore.Tests;
    using Microsoft.ApplicationInsights.AspNetCore.Tests.Helpers;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.DependencyCollector;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
    using Microsoft.ApplicationInsights.WindowsServer;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Hosting.Internal;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Internal;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;    

    public static class ApplicationInsightsExtensionsTests
    {
        /// <summary>Constant instrumentation key value for testintg.</summary>
        public const string TestInstrumentationKey = "11111111-2222-3333-4444-555555555555";
        private const string InstrumentationKeyFromConfig = "ApplicationInsights:InstrumentationKey";

        public static ServiceCollection GetServiceCollectionWithContextAccessor()
        {
            var services = new ServiceCollection();
            IHttpContextAccessor contextAccessor = new HttpContextAccessor();
            services.AddSingleton<IHttpContextAccessor>(contextAccessor);
            services.AddSingleton<IHostingEnvironment>(new HostingEnvironment() { ContentRootPath = Directory.GetCurrentDirectory()});
            services.AddSingleton<DiagnosticListener>(new DiagnosticListener("TestListener"));
            return services;
        }

        public static class AddApplicationInsightsTelemetry
        {
            [Theory]
            [InlineData(typeof(ITelemetryInitializer), typeof(ApplicationInsights.AspNetCore.TelemetryInitializers.DomainNameRoleInstanceTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(AzureWebAppRoleEnvironmentTelemetryInitializer), ServiceLifetime.Singleton)]            
            [InlineData(typeof(ITelemetryInitializer), typeof(ComponentVersionTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(ClientIpHeaderTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(OperationNameTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(SyntheticTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(WebSessionTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(WebUserTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(TelemetryConfiguration), null, ServiceLifetime.Singleton)]
            [InlineData(typeof(TelemetryClient), typeof(TelemetryClient), ServiceLifetime.Singleton)]
            public static void RegistersExpectedServices(Type serviceType, Type implementationType, ServiceLifetime lifecycle)
            {
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, null);
                ServiceDescriptor service = services.Single(s => s.ServiceType == serviceType && s.ImplementationType == implementationType);
                Assert.Equal(lifecycle, service.Lifetime);
            }

            [Theory]
            [InlineData(typeof(ITelemetryInitializer), typeof(ApplicationInsights.AspNetCore.TelemetryInitializers.DomainNameRoleInstanceTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(AzureWebAppRoleEnvironmentTelemetryInitializer), ServiceLifetime.Singleton)]            
            [InlineData(typeof(ITelemetryInitializer), typeof(ComponentVersionTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(ClientIpHeaderTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(OperationNameTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(SyntheticTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(WebSessionTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(WebUserTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(TelemetryConfiguration), null, ServiceLifetime.Singleton)]
            [InlineData(typeof(TelemetryClient), typeof(TelemetryClient), ServiceLifetime.Singleton)]
            public static void RegistersExpectedServicesOnlyOnce(Type serviceType, Type implementationType, ServiceLifetime lifecycle)
            {
                var services = GetServiceCollectionWithContextAccessor();
                services.AddApplicationInsightsTelemetry();
                services.AddApplicationInsightsTelemetry();
                ServiceDescriptor service = services.Single(s => s.ServiceType == serviceType && s.ImplementationType == implementationType);
                Assert.Equal(lifecycle, service.Lifetime);
            }

            [Fact]
            public static void DoesNotThrowWithoutInstrumentationKey()
            {
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, null);
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatCreatesDefaultInstance()
            {
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, null);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                Assert.Contains(telemetryConfiguration.TelemetryInitializers, t => t is OperationNameTelemetryInitializer);
            }

            /// <summary>
            /// Tests that the instrumentation key configuration can be read from a JSON file by the configuration factory.            
            /// </summary>
            [Fact]
            
            public static void RegistersTelemetryConfigurationFactoryMethodThatReadsInstrumentationKeyFromConfiguration()
            {                
                var services = CreateServicesAndAddApplicationinsightsTelemetry(Path.Combine("content", "config-instrumentation-key.json"), null);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                Assert.Equal(TestInstrumentationKey, telemetryConfiguration.InstrumentationKey);
            }

            /// <summary>
            /// Tests that the Active configuration singleton is updated, but another instance of telemetry configuration is created for dependency injection.
            /// ASP.NET Core developers should always use Dependency Injection instead of static singleton approach. 
            /// See Microsoft/ApplicationInsights-dotnet#613
            /// </summary>
            [Fact]
            public static void ConfigurationFactoryMethodUpdatesTheActiveConfigurationSingletonByDefault()
            {
                // Clear off Active before beginning test to avoid being affected by previous tests.
                TelemetryConfiguration.Active.InstrumentationKey = "";
                TelemetryConfiguration.Active.TelemetryInitializers.Clear();

                var activeConfig = TelemetryConfiguration.Active;
                var services = CreateServicesAndAddApplicationinsightsTelemetry(Path.Combine("content","config-instrumentation-key.json"), null);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                TelemetryConfiguration telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                Assert.Equal(TestInstrumentationKey, telemetryConfiguration.InstrumentationKey);
                Assert.Equal(TestInstrumentationKey, activeConfig.InstrumentationKey);
                Assert.NotEqual(activeConfig, telemetryConfiguration);
            }

            /// <summary>
            /// We determine if Active telemtery needs to be configured based on the assumptions that 'default' configuration
            // created by base SDK has single preset ITelemetryIntitializer. If it ever changes, change TelemetryConfigurationOptions.IsActiveConfigured method as well.
            /// </summary>
            [Fact]
            public static void DefaultTelemetryconfigurationHasOneTelemetryInitializer()
            {
                //
                var defaultConfig = TelemetryConfiguration.CreateDefault();
                Assert.Equal(1, defaultConfig.TelemetryInitializers.Count);
            }

            [Fact]
            
            public static void RegistersTelemetryConfigurationFactoryMethodThatReadsDeveloperModeFromConfiguration()
            {
                var services = CreateServicesAndAddApplicationinsightsTelemetry(Path.Combine("content", "config-developer-mode.json"), null);                

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                Assert.True(telemetryConfiguration.TelemetryChannel.DeveloperMode);
            }

            [Fact]
            
            public static void RegistersTelemetryConfigurationFactoryMethodThatReadsEndpointAddressFromConfiguration()
            {
                var services = CreateServicesAndAddApplicationinsightsTelemetry(Path.Combine("content", "config-endpoint-address.json"), null);                

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                Assert.Equal("http://localhost:1234/v2/track/", telemetryConfiguration.TelemetryChannel.EndpointAddress);
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatReadsInstrumentationKeyFromEnvironment()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                services.AddSingleton<ITelemetryChannel>(new InMemoryChannel());
                Environment.SetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", TestInstrumentationKey);
                var config = new ConfigurationBuilder().AddEnvironmentVariables().Build();
                try
                {
                    services.AddApplicationInsightsTelemetry(config);

                    IServiceProvider serviceProvider = services.BuildServiceProvider();
                    var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                    Assert.Equal(TestInstrumentationKey, telemetryConfiguration.InstrumentationKey);
                }
                finally
                {
                    Environment.SetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", null);
                }
            }

            /// <summary>
            /// Validates that while using services.AddApplicationInsightsTelemetry(); ikey is read from
            /// Environment
            /// </summary>
            [Fact]
            public static void AddApplicationInsightsTelemetryReadsInstrumentationKeyFromEnvironment()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();                
                Environment.SetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", TestInstrumentationKey);                
                try
                {
                    services.AddApplicationInsightsTelemetry();
                    IServiceProvider serviceProvider = services.BuildServiceProvider();
                    var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                    Assert.Equal(TestInstrumentationKey, telemetryConfiguration.InstrumentationKey);
                }
                finally
                {
                    Environment.SetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", null);
                }
            }

            /// <summary>
            /// Validates that while using services.AddApplicationInsightsTelemetry(ikey), supplied ikey is
            /// used instead of one from Environment
            /// </summary>
            [Fact]
            public static void AddApplicationInsightsTelemetryDoesNotReadInstrumentationKeyFromEnvironmentIfSupplied()
            {
                Environment.SetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", TestInstrumentationKey);
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                string ikeyExpected = Guid.NewGuid().ToString();

                try
                {
                    services.AddApplicationInsightsTelemetry(ikeyExpected);
                    IServiceProvider serviceProvider = services.BuildServiceProvider();
                    var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                    Assert.Equal(ikeyExpected, telemetryConfiguration.InstrumentationKey);
                }
                finally
                {
                    Environment.SetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", null);
                }
            }

            /// <summary>
            /// Validates that while using services.AddApplicationInsightsTelemetry(); reads ikey/other settings from
            /// appsettings.json
            /// </summary>
            [Fact]
            public static void AddApplicationInsightsTelemetryReadsInstrumentationKeyFromDefaultAppsettingsFile()
            {
                string ikeyExpected = Guid.NewGuid().ToString();
                string hostExpected = "http://ainewhost/v2/track/";
                string text = File.ReadAllText("appsettings.json");
                string originalText = text;
                try
                {
                    text = text.Replace("ikeyhere", ikeyExpected);
                    text = text.Replace("http://hosthere/v2/track/", hostExpected);
                    File.WriteAllText("appsettings.json", text);

                    var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                    services.AddApplicationInsightsTelemetry();
                    IServiceProvider serviceProvider = services.BuildServiceProvider();
                    var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                    Assert.Equal(ikeyExpected, telemetryConfiguration.InstrumentationKey);
                    Assert.Equal(hostExpected, telemetryConfiguration.TelemetryChannel.EndpointAddress);
                }
                finally
                {                    
                    File.WriteAllText("appsettings.json", originalText);
                }
            }

            /// <summary>
            /// Validates that while using services.AddApplicationInsightsTelemetry(ikey), supplied ikey is
            /// used instead of one from appsettings.json
            /// </summary>
            [Fact]
            public static void AddApplicationInsightsTelemetryDoesNotReadInstrumentationKeyFromDefaultAppsettingsIfSupplied()
            {
                string suppliedIKey = "suppliedikey";
                string ikey = Guid.NewGuid().ToString();
                string text = File.ReadAllText("appsettings.json");
                try
                {
                    text = text.Replace("ikeyhere", ikey);
                    File.WriteAllText("appsettings.json", text);

                    var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                    services.AddApplicationInsightsTelemetry(suppliedIKey);
                    IServiceProvider serviceProvider = services.BuildServiceProvider();
                    var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                    Assert.Equal(suppliedIKey, telemetryConfiguration.InstrumentationKey);
                }
                finally
                {
                    text = text.Replace(ikey, "ikeyhere");
                    File.WriteAllText("appsettings.json", text);
                }
            }

            /// <summary>
            /// Validates that while using services.AddApplicationInsightsTelemetry(ApplicationInsightsServiceOptions), supplied ikey is
            /// used instead of one from appsettings.json
            /// </summary>
            [Fact]
            public static void AddApplicationInsightsTelemetryDoesNotReadInstrumentationKeyFromDefaultAppsettingsIfSuppliedViaOptions()
            {
                string suppliedIKey = "suppliedikey";
                var options = new ApplicationInsightsServiceOptions() { InstrumentationKey = suppliedIKey };
                string ikey = Guid.NewGuid().ToString();
                string text = File.ReadAllText("appsettings.json");
                try
                {
                    text = text.Replace("ikeyhere", ikey);
                    File.WriteAllText("appsettings.json", text);

                    var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                    services.AddApplicationInsightsTelemetry(options);
                    IServiceProvider serviceProvider = services.BuildServiceProvider();
                    var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                    Assert.Equal(suppliedIKey, telemetryConfiguration.InstrumentationKey);
                }
                finally
                {
                    text = text.Replace(ikey, "ikeyhere");
                    File.WriteAllText("appsettings.json", text);
                }
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatReadsDeveloperModeFromEnvironment()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                services.AddSingleton<ITelemetryChannel>(new InMemoryChannel());
                Environment.SetEnvironmentVariable("APPINSIGHTS_DEVELOPER_MODE", "true");
                var config = new ConfigurationBuilder().AddEnvironmentVariables().Build();
                try
                {
                    services.AddApplicationInsightsTelemetry(config);

                    IServiceProvider serviceProvider = services.BuildServiceProvider();
                    var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                    Assert.True(telemetryConfiguration.TelemetryChannel.DeveloperMode);
                }
                finally
                {
                    Environment.SetEnvironmentVariable("APPINSIGHTS_DEVELOPER_MODE", null);
                }
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatReadsEndpointAddressFromEnvironment()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                services.AddSingleton<ITelemetryChannel>(new InMemoryChannel());
                Environment.SetEnvironmentVariable("APPINSIGHTS_ENDPOINTADDRESS", "http://localhost:1234/v2/track/");
                var config = new ConfigurationBuilder().AddEnvironmentVariables().Build();
                try
                {
                    services.AddApplicationInsightsTelemetry(config);

                    IServiceProvider serviceProvider = services.BuildServiceProvider();
                    var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                    Assert.Equal("http://localhost:1234/v2/track/", telemetryConfiguration.TelemetryChannel.EndpointAddress);
                }
                finally
                {
                    Environment.SetEnvironmentVariable("APPINSIGHTS_ENDPOINTADDRESS", null);
                }
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatPopulatesItWithTelemetryInitializersFromContainer()
            {
                var telemetryInitializer = new FakeTelemetryInitializer();
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                services.AddSingleton<ITelemetryInitializer>(telemetryInitializer);
                services.AddSingleton<ITelemetryChannel>(new InMemoryChannel());

                services.AddApplicationInsightsTelemetry(new ConfigurationBuilder().Build());

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                Assert.Contains(telemetryInitializer, telemetryConfiguration.TelemetryInitializers);
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatPopulatesItWithTelemetryChannelFromContainer()
            {
                var telemetryChannel = new FakeTelemetryChannel();
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                services.AddSingleton<ITelemetryChannel>(telemetryChannel);

                services.AddApplicationInsightsTelemetry(new ConfigurationBuilder().Build());

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                Assert.Same(telemetryChannel, telemetryConfiguration.TelemetryChannel);
            }

            [Fact]
            public static void DoesNotOverrideDefaultTelemetryChannelIfTelemetryChannelServiceIsNotRegistered()
            {
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, null);
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                Assert.NotNull(telemetryConfiguration.TelemetryChannel);
            }

            [Fact]
            public static void RegistersTelemetryClientToGetTelemetryConfigurationFromContainerAndNotGlobalInstance()
            {
                ITelemetry sentTelemetry = null;
                var telemetryChannel = new FakeTelemetryChannel { OnSend = telemetry => sentTelemetry = telemetry };
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, null);
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var configuration = serviceProvider.GetTelemetryConfiguration();
                configuration.InstrumentationKey = Guid.NewGuid().ToString();
                ITelemetryChannel oldChannel = configuration.TelemetryChannel;
                try
                {
                    configuration.TelemetryChannel = telemetryChannel;

                    var telemetryClient = serviceProvider.GetRequiredService<TelemetryClient>();
                    telemetryClient.TrackEvent("myevent");

                    // We want to check that configuration from container was used but configuration is a private field so we check instrumentation key instead.
                    Assert.Equal(configuration.InstrumentationKey, sentTelemetry.Context.InstrumentationKey);
                }
                finally
                {
                    configuration.TelemetryChannel = oldChannel;
                }
            }

            [Fact]
            public static void AddApplicationInsightsTelemetryDoesNotThrowOnNullServiceOptions()
            {
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, "http://localhost:1234/v2/track/", null, false);
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
            }

            [Fact]
            public static void AppApplicationInsightsTelemetryFromApplicationInsightsServiceOptionsCopiesAllSettings()
            {
                ServiceCollection services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                ApplicationInsightsServiceOptions options = new ApplicationInsightsServiceOptions()
                {
                    ApplicationVersion = "test",
                    DeveloperMode = true,
                    EnableAdaptiveSampling = false,
                    EnableAuthenticationTrackingJavaScript = false,
                    EnableDebugLogger = true,
                    EnableQuickPulseMetricStream = false,
                    EndpointAddress = "http://test",
                    EnableHeartbeat = false,
                    InstrumentationKey = "test"
                };
                services.AddApplicationInsightsTelemetry(options);
                ApplicationInsightsServiceOptions servicesOptions = null;
                services.Configure((ApplicationInsightsServiceOptions o) =>
                {
                    servicesOptions = o;
                });

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                TelemetryConfiguration telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();

                Type optionsType = typeof(ApplicationInsightsServiceOptions);
                PropertyInfo[] properties = optionsType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                Assert.True(properties.Length > 0);
                foreach (PropertyInfo property in properties)
                {
                    Assert.Equal(property.GetValue(options).ToString(), property.GetValue(servicesOptions).ToString());
                }
            }


            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatPopulatesItWithModulesFromContainer()
            {
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, null, null, false);
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var modules = serviceProvider.GetServices<ITelemetryModule>();
                Assert.NotNull(modules);


                Assert.Equal(6, modules.Count());
                var perfCounterModule = services.FirstOrDefault<ServiceDescriptor>(t => t.ImplementationType == typeof(PerformanceCollectorModule));
                Assert.NotNull(perfCounterModule);

                var dependencyModuleDescriptor = services.FirstOrDefault<ServiceDescriptor>(t => t.ImplementationType == typeof(DependencyTrackingTelemetryModule));
                Assert.NotNull(dependencyModuleDescriptor);

                var reqModuleDescriptor = services.FirstOrDefault<ServiceDescriptor>(t => t.ImplementationType == typeof(RequestTrackingTelemetryModule));
                Assert.NotNull(reqModuleDescriptor);

                var appServiceHeartBeatModuleDescriptor = services.FirstOrDefault<ServiceDescriptor>(t => t.ImplementationType == typeof(AppServicesHeartbeatTelemetryModule));
                Assert.NotNull(appServiceHeartBeatModuleDescriptor);

                var azureMetadataHeartBeatModuleDescriptor = services.FirstOrDefault<ServiceDescriptor>(t => t.ImplementationType == typeof(AzureInstanceMetadataTelemetryModule));
                Assert.NotNull(azureMetadataHeartBeatModuleDescriptor);

                var quickPulseModuleDescriptor = services.FirstOrDefault<ServiceDescriptor>(t => t.ImplementationType == typeof(QuickPulseTelemetryModule));
                Assert.NotNull(quickPulseModuleDescriptor);
            }
            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatPopulatesDependencyCollectorWithDefaultValues()
            {
                //ARRANGE
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, null, null, false);
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var modules = serviceProvider.GetServices<ITelemetryModule>();

                //ACT

                // Requesting TelemetryConfiguration from services trigger constructing the TelemetryConfiguration
                // which in turn trigger configuration of all modules.
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                var dependencyModule = modules.OfType<DependencyTrackingTelemetryModule>().Single();

                //VALIDATE
                Assert.True(dependencyModule.ExcludeComponentCorrelationHttpHeadersOnDomains.Count > 0);

            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatPopulatesItWithTelemetryProcessorFactoriesFromContainer()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                services.AddApplicationInsightsTelemetryProcessor<FakeTelemetryProcessor>();

                services.AddApplicationInsightsTelemetry(new ConfigurationBuilder().Build());

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                FakeTelemetryProcessor telemetryProcessor = telemetryConfiguration.TelemetryProcessors.OfType<FakeTelemetryProcessor>().FirstOrDefault();
                Assert.NotNull(telemetryProcessor);
                Assert.True(telemetryProcessor.IsInitialized);
            }

            [Fact]
            public static void AddApplicationInsightsTelemetryProcessorWithNullTelemetryProcessorTypeThrows()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                Assert.Throws<ArgumentNullException>(() => services.AddApplicationInsightsTelemetryProcessor(null));
            }

            [Fact]
            public static void AddApplicationInsightsTelemetryProcessorWithNonTelemetryProcessorTypeThrows()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                Assert.Throws<ArgumentException>(() => services.AddApplicationInsightsTelemetryProcessor(typeof(string)));
                Assert.Throws<ArgumentException>(() => services.AddApplicationInsightsTelemetryProcessor(typeof(ITelemetryProcessor)));
            }

            [Fact]
            public static void AddApplicationInsightsTelemetryProcessorWithImportingConstructor()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                services.AddApplicationInsightsTelemetryProcessor<FakeTelemetryProcessorWithImportingConstructor>();
                services.AddApplicationInsightsTelemetry(new ConfigurationBuilder().Build());
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                FakeTelemetryProcessorWithImportingConstructor telemetryProcessor = telemetryConfiguration.TelemetryProcessors.OfType<FakeTelemetryProcessorWithImportingConstructor>().FirstOrDefault();
                Assert.NotNull(telemetryProcessor);
                Assert.Same(serviceProvider.GetService<IHostingEnvironment>(), telemetryProcessor.HostingEnvironment);
            }

            [Fact]
            public static void ConfigureApplicationInsightsTelemetryModuleWorks()
            {
                //ARRANGE
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                services.AddSingleton<ITelemetryModule, TestTelemetryModule>();

                //ACT
                services.ConfigureTelemetryModule<TestTelemetryModule>
                ((module, o) => module.CustomProperty = "mycustomvalue");
                services.AddApplicationInsightsTelemetry(new ConfigurationBuilder().Build());
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                
                // Requesting TelemetryConfiguration from services trigger constructing the TelemetryConfiguration
                // which in turn trigger configuration of all modules.
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();

                //VALIDATE
                var modules = serviceProvider.GetServices<ITelemetryModule>();                
                var testTelemetryModule = modules.OfType<TestTelemetryModule>().Single();

                //The module should be initialized and configured as instructed.
                Assert.NotNull(testTelemetryModule);
                Assert.Equal("mycustomvalue", testTelemetryModule.CustomProperty);
                Assert.True(testTelemetryModule.IsInitialized);
            }

            [Fact]
            public static void ConfigureApplicationInsightsTelemetryModuleWorksWithOptions()
            {
                //ARRANGE
                Action<ApplicationInsightsServiceOptions> serviceOptions = options => options.ApplicationVersion = "123";
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                services.AddSingleton<ITelemetryModule, TestTelemetryModule>();

                //ACT
                services.ConfigureTelemetryModule<TestTelemetryModule>
                    ((module, o) => module.CustomProperty = o.ApplicationVersion);
                services.AddApplicationInsightsTelemetry(serviceOptions);
                IServiceProvider serviceProvider = services.BuildServiceProvider();

                // Requesting TelemetryConfiguration from services trigger constructing the TelemetryConfiguration
                // which in turn trigger configuration of all modules.
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();

                //VALIDATE
                var modules = serviceProvider.GetServices<ITelemetryModule>();
                var testTelemetryModule = modules.OfType<TestTelemetryModule>().Single();

                //The module should be initialized and configured as instructed.
                Assert.NotNull(testTelemetryModule);
                Assert.Equal("123", testTelemetryModule.CustomProperty);
                Assert.True(testTelemetryModule.IsInitialized);
            }

            [Fact]
            public static void ConfigureApplicationInsightsTelemetryModuleWorksWithoutOptions()
            {
                //ARRANGE
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                services.AddSingleton<ITelemetryModule, TestTelemetryModule>();

                //ACT
                services.ConfigureTelemetryModule<TestTelemetryModule>
                    (module => module.CustomProperty = "mycustomproperty");
                services.AddApplicationInsightsTelemetry();
                IServiceProvider serviceProvider = services.BuildServiceProvider();

                // Requesting TelemetryConfiguration from services trigger constructing the TelemetryConfiguration
                // which in turn trigger configuration of all modules.
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();

                //VALIDATE
                var modules = serviceProvider.GetServices<ITelemetryModule>();
                var testTelemetryModule = modules.OfType<TestTelemetryModule>().Single();

                //The module should be initialized and configured as instructed.
                Assert.NotNull(testTelemetryModule);
                Assert.Equal("mycustomproperty", testTelemetryModule.CustomProperty);
                Assert.True(testTelemetryModule.IsInitialized);
            }

            [Fact]
            public static void ConfigureRequestTrackingTelemetryDefaultOptions()
            {
                //ARRANGE
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();

                //ACT
                services.AddApplicationInsightsTelemetry();
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();

                //VALIDATE
                var requestTrackingModule = (RequestTrackingTelemetryModule)serviceProvider.GetServices<ITelemetryModule>().FirstOrDefault(x => x.GetType()
                                                                                                                == typeof(RequestTrackingTelemetryModule));

                Assert.True(requestTrackingModule.CollectionOptions.InjectResponseHeaders);
                Assert.True(requestTrackingModule.CollectionOptions.TrackExceptions);
            }

            [Fact]
            public static void ConfigureRequestTrackingTelemetryCustomOptions()
            {
                //ARRANGE
                Action<ApplicationInsightsServiceOptions> serviceOptions = options =>
                {
                    options.RequestCollectionOptions.InjectResponseHeaders = false;
                    options.RequestCollectionOptions.TrackExceptions = false;
                };
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();

                //ACT
                services.AddApplicationInsightsTelemetry(serviceOptions);
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();

                //VALIDATE
                var requestTrackingModule = (RequestTrackingTelemetryModule) serviceProvider
                    .GetServices<ITelemetryModule>().FirstOrDefault(x => x.GetType() == typeof(RequestTrackingTelemetryModule));

                Assert.False(requestTrackingModule.CollectionOptions.InjectResponseHeaders);
                Assert.False(requestTrackingModule.CollectionOptions.TrackExceptions);
            }

            [Fact]
            public static void ConfigureApplicationInsightsTelemetryModuleThrowsIfConfigureIsNull()
            {
                //ARRANGE
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                services.AddSingleton<ITelemetryModule, TestTelemetryModule>();

                //ACT and VALIDATE
                Assert.Throws<ArgumentNullException>(() => services.ConfigureTelemetryModule<TestTelemetryModule>((Action<TestTelemetryModule, ApplicationInsightsServiceOptions>)null));
                Assert.Throws<ArgumentNullException>(() => services.ConfigureTelemetryModule<TestTelemetryModule>((Action<TestTelemetryModule>)null));
            }

            [Fact]
            public static void ConfigureApplicationInsightsTelemetryModuleDoesNotThrowIfModuleNotFound()
            {
                //ARRANGE
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                // Intentionally NOT adding the module
                // services.AddSingleton<ITelemetryModule, TestTelemetryModule>();

                //ACT
                services.ConfigureTelemetryModule<TestTelemetryModule>
                ((module, options) => module.CustomProperty = "mycustomvalue");
                services.AddApplicationInsightsTelemetry(new ConfigurationBuilder().Build());
                IServiceProvider serviceProvider = services.BuildServiceProvider();

                // Requesting TelemetryConfiguration from services trigger constructing the TelemetryConfiguration
                // which in turn trigger configuration of all modules.
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();

                //VALIDATE
                var modules = serviceProvider.GetServices<ITelemetryModule>();
                var testTelemetryModule = modules.OfType<TestTelemetryModule>().FirstOrDefault();

                // No exceptions thrown here.
                Assert.Null(testTelemetryModule);                
            }


            [Fact]
            public static void AddsAddaptiveSamplingServiceToTheConfigurationByDefault()
            {                                
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, "http://localhost:1234/v2/track/", null, false);
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                var adaptiveSamplingProcessorCount = GetTelemetryProcessorsCountInConfiguration<AdaptiveSamplingTelemetryProcessor>(telemetryConfiguration);

                // There will be 2 separate SamplingTelemetryProcessors - one for Events, and other for everything else.
                Assert.Equal(2, adaptiveSamplingProcessorCount);
            }

            [Fact]
            public static void DoesNotAddSamplingToConfigurationIfExplicitlyControlledThroughParameter()
            {                
                Action<ApplicationInsightsServiceOptions> serviceOptions = options => options.EnableAdaptiveSampling = false;
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, "http://localhost:1234/v2/track/", serviceOptions, false);
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                var qpProcessorCount = GetTelemetryProcessorsCountInConfiguration<AdaptiveSamplingTelemetryProcessor>(telemetryConfiguration);
                Assert.Equal(0, qpProcessorCount);
            }

            [Fact]
            public static void AddsAddaptiveSamplingServiceToTheConfigurationWithServiceOptions()
            {                
                Action<ApplicationInsightsServiceOptions> serviceOptions = options => options.EnableAdaptiveSampling = true;
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, "http://localhost:1234/v2/track/", serviceOptions, false);
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                var adaptiveSamplingProcessorCount =  GetTelemetryProcessorsCountInConfiguration<AdaptiveSamplingTelemetryProcessor>(telemetryConfiguration);
                // There will be 2 separate SamplingTelemetryProcessors - one for Events, and other for everything else.
                Assert.Equal(2, adaptiveSamplingProcessorCount);
            }

            [Fact]
            public static void AddsServerTelemetryChannelByDefault()
            {
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, "http://localhost:1234/v2/track/", null, false);
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                Assert.Equal(typeof(ServerTelemetryChannel), telemetryConfiguration.TelemetryChannel.GetType());
            }

            [Fact]
            public static void DoesNotOverWriteExistingChannel()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                services.AddSingleton<ITelemetryChannel, InMemoryChannel>();
                var config = new ConfigurationBuilder().AddApplicationInsightsSettings(endpointAddress: "http://localhost:1234/v2/track/").Build();
                services.AddApplicationInsightsTelemetry(config);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                Assert.Equal(typeof(InMemoryChannel), telemetryConfiguration.TelemetryChannel.GetType());
            }

            [Fact]
            public static void FallbacktoDefaultChannelWhenNoChannelFoundInDI()
            {
                // ARRANGE
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();                                
                var config = new ConfigurationBuilder().AddApplicationInsightsSettings(endpointAddress: "http://localhost:1234/v2/track/").Build();
                services.AddApplicationInsightsTelemetry(config);

                // Remove all ITelemetryChannel to simulate scenario where customer remove all channel from DI but forgot to add new one.
                // This should not crash application startup, and should fall back to default channel supplied by base sdk.
                for (var i = services.Count - 1; i >= 0; i--)
                {
                    var descriptor = services[i];
                    if (descriptor.ServiceType == typeof(ITelemetryChannel))
                    {
                        services.RemoveAt(i);
                    }
                }

                // VERIFY that default channel is configured when nothing is present in DI
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                Assert.Equal(typeof(InMemoryChannel), telemetryConfiguration.TelemetryChannel.GetType());
            }

            [Fact]
            public static void VerifyNoExceptionWhenAppIdProviderNotFoundInDI()
            {
                // ARRANGE
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                var config = new ConfigurationBuilder().AddApplicationInsightsSettings(endpointAddress: "http://localhost:1234/v2/track/").Build();
                services.AddApplicationInsightsTelemetry(config);

                for (var i = services.Count - 1; i >= 0; i--)
                {
                    var descriptor = services[i];
                    if (descriptor.ServiceType == typeof(IApplicationIdProvider))
                    {
                        services.RemoveAt(i);
                    }
                }

                // ACT
                IServiceProvider serviceProvider = services.BuildServiceProvider();


                // VERIFY
                var requestTrackingModule = serviceProvider.GetServices<ITelemetryModule>().FirstOrDefault(x => x.GetType() 
                    == typeof(RequestTrackingTelemetryModule));

                Assert.NotNull(requestTrackingModule); // this verifies the instance was created without exception
            }

            [Fact]
            public static void VerifyUserCanOverrideAppIdProvider()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                services.AddSingleton<IApplicationIdProvider, MockApplicationIdProvider>(); // assume user tries to define own implementation
                var config = new ConfigurationBuilder().AddApplicationInsightsSettings(endpointAddress: "http://localhost:1234/v2/track/").Build();
                services.AddApplicationInsightsTelemetry(config);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var applicationIdProvider = serviceProvider.GetRequiredService<IApplicationIdProvider>();

                Assert.Equal(typeof(MockApplicationIdProvider), applicationIdProvider.GetType());
            }

            [Fact]
            public static void AddsQuickPulseProcessorToTheConfigurationByDefault()
            {                
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, "http://localhost:1234/v2/track/", null, false);
                services.AddSingleton<ITelemetryChannel, InMemoryChannel>();
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                var qpProcessorCount = GetTelemetryProcessorsCountInConfiguration<QuickPulseTelemetryProcessor>(telemetryConfiguration);
                Assert.Equal(1, qpProcessorCount);
            }

            [Fact]
            public static void AddsAutoCollectedMetricsExtractorProcessorToTheConfigurationByDefault()
            {
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, "http://localhost:1234/v2/track/", null, false);
                services.AddSingleton<ITelemetryChannel, InMemoryChannel>();
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                var metricExtractorProcessorCount = GetTelemetryProcessorsCountInConfiguration<AutocollectedMetricsExtractor>(telemetryConfiguration);
                Assert.Equal(1, metricExtractorProcessorCount);
            }

            [Fact]
            public static void DoesNotAddAutoCollectedMetricsExtractorToConfigurationIfExplicitlyControlledThroughParameter()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                ApplicationInsightsServiceOptions serviceOptions = new ApplicationInsightsServiceOptions();
                serviceOptions.AddAutoCollectedMetricExtractor = false;

                services.AddApplicationInsightsTelemetry(serviceOptions);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                var metricExtractorProcessorCount = GetTelemetryProcessorsCountInConfiguration<AutocollectedMetricsExtractor>(telemetryConfiguration);
                Assert.Equal(0, metricExtractorProcessorCount);
            }

            [Fact]
            public static void DoesNotAddQuickPulseProcessorToConfigurationIfExplicitlyControlledThroughParameter()
            {                
                Action<ApplicationInsightsServiceOptions> serviceOptions = options => options.EnableQuickPulseMetricStream = false;
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, "http://localhost:1234/v2/track/", serviceOptions, false);
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                var qpProcessorCount = GetTelemetryProcessorsCountInConfiguration<QuickPulseTelemetryProcessor>(telemetryConfiguration);
                Assert.Equal(0, qpProcessorCount);
            }

            [Fact]
            public static void AddsQuickPulseProcessorToTheConfigurationWithServiceOptions()
            {                
                Action< ApplicationInsightsServiceOptions> serviceOptions = options => options.EnableQuickPulseMetricStream = true;
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, "http://localhost:1234/v2/track/", serviceOptions, false);
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                var qpProcessorCount =  GetTelemetryProcessorsCountInConfiguration<QuickPulseTelemetryProcessor>(telemetryConfiguration);
                Assert.Equal(1, qpProcessorCount);
            }

            [Fact]
            public static void AddsHeartbeatModulesToTheConfigurationByDefault()
            {
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, "http://localhost:1234/v2/track/", null, false);
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                var modules = serviceProvider.GetServices<ITelemetryModule>();
                Assert.NotNull(modules.OfType<AppServicesHeartbeatTelemetryModule>().Single());
                Assert.NotNull(modules.OfType<AzureInstanceMetadataTelemetryModule>().Single());
            }

            [Fact]
            public static void HeartbeatIsDisabledWithServiceOptions()
            {
                var heartbeatModulePRE = TelemetryModules.Instance.Modules.OfType<IHeartbeatPropertyManager>().First();
                Assert.True(heartbeatModulePRE.IsHeartbeatEnabled);

                Action<ApplicationInsightsServiceOptions> serviceOptions = options => options.EnableHeartbeat = false;
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, "http://localhost:1234/v2/track/", serviceOptions, false);
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                var heartbeatModule = TelemetryModules.Instance.Modules.OfType<IHeartbeatPropertyManager>().First();
                Assert.NotNull(heartbeatModule);
                Assert.False(heartbeatModule.IsHeartbeatEnabled);
            }

            private static int GetTelemetryProcessorsCountInConfiguration<T>(TelemetryConfiguration telemetryConfiguration)
            {
                return telemetryConfiguration.TelemetryProcessors.Where(processor => processor.GetType() == typeof(T)).Count();
            }

            [Fact]
            public static void LoggerCallbackIsInvoked()
            {
                var services = new ServiceCollection();
                services.AddSingleton<ApplicationInsightsLoggerEvents>();
                var serviceProvider = services.BuildServiceProvider();

                var loggerProvider = new MockLoggingFactory();

                bool firstLoggerCallback = false;
                bool secondLoggerCallback = false;

                loggerProvider.AddApplicationInsights(serviceProvider, (s, level) => true, () => firstLoggerCallback = true);
                loggerProvider.AddApplicationInsights(serviceProvider, (s, level) => true, () => secondLoggerCallback = true);

                Assert.True(firstLoggerCallback);
                Assert.False(secondLoggerCallback);
            }

            [Fact]
            public static void NullLoggerCallbackAlowed()
            {
                var services = new ServiceCollection();
                services.AddSingleton<ApplicationInsightsLoggerEvents>();
                var serviceProvider = services.BuildServiceProvider();

                var loggerProvider = new MockLoggingFactory();

                loggerProvider.AddApplicationInsights(serviceProvider, (s, level) => true, null);
                loggerProvider.AddApplicationInsights(serviceProvider, (s, level) => true, null);
            }
        }

        public static class AddApplicationInsightsSettings
        {
            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatReadsInstrumentationKeyFromSettings()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                services.AddSingleton<ITelemetryChannel>(new InMemoryChannel());
                var config = new ConfigurationBuilder().AddApplicationInsightsSettings(instrumentationKey: TestInstrumentationKey).Build();
                services.AddApplicationInsightsTelemetry(config);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                Assert.Equal(TestInstrumentationKey, telemetryConfiguration.InstrumentationKey);
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatReadsDeveloperModeFromSettings()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                services.AddSingleton<ITelemetryChannel>(new InMemoryChannel());
                var config = new ConfigurationBuilder().AddApplicationInsightsSettings(developerMode: true).Build();
                services.AddApplicationInsightsTelemetry(config);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                Assert.True(telemetryConfiguration.TelemetryChannel.DeveloperMode);
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatReadsEndpointAddressFromSettings()
            {
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, "http://localhost:1234/v2/track/");
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                Assert.Equal("http://localhost:1234/v2/track/", telemetryConfiguration.TelemetryChannel.EndpointAddress);
            }

            /// <summary>
            /// Sanity check to validate that node name and roleinstance are populated
            /// </summary>
            [Fact]
            public static void SanityCheckRoleInstance()
            {
                // ARRANGE
                string expected = Environment.MachineName;
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();                
                services.AddApplicationInsightsTelemetry();
                IServiceProvider serviceProvider = services.BuildServiceProvider();

                // Request TC from DI which would be made with the default TelemetryConfiguration which should 
                // contain the telemetry initializer capable of populate node name and role instance name.
                var tc = serviceProvider.GetRequiredService<TelemetryClient>();                
                var mockItem = new EventTelemetry();

                // ACT
                // This is expected to run all TI and populate the node name and role instance.
                tc.Initialize(mockItem);

                // VERIFY                
                Assert.Contains(expected,mockItem.Context.Cloud.RoleInstance, StringComparison.CurrentCultureIgnoreCase);                                
            }
        }

        public static TelemetryConfiguration GetTelemetryConfiguration(this IServiceProvider serviceProvider)
        {
            return serviceProvider.GetRequiredService<IOptions<TelemetryConfiguration>>().Value;
        }

        public static ServiceCollection CreateServicesAndAddApplicationinsightsTelemetry(string jsonPath, string channelEndPointAddress, Action<ApplicationInsightsServiceOptions> serviceOptions = null, bool addChannel = true)
        {
            var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
            if (addChannel)
            {
                services.AddSingleton<ITelemetryChannel>(new InMemoryChannel());
            }
            IConfigurationRoot config = null;

            if (jsonPath != null)
            {
                var jsonFullPath = Path.Combine(Directory.GetCurrentDirectory(), jsonPath);
                Console.WriteLine("json:" + jsonFullPath);
                Trace.WriteLine("json:" + jsonFullPath);
                try
                {
                    config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile(jsonFullPath).Build();
                }
                catch(Exception)
                {
                    throw new Exception("Unable to build with json:" + jsonFullPath);
                }
            }
            else  if (channelEndPointAddress != null)
            {
                config = new ConfigurationBuilder().AddApplicationInsightsSettings(endpointAddress: channelEndPointAddress).Build();
            }
            else
            {
                config = new ConfigurationBuilder().Build();
            }

            services.AddApplicationInsightsTelemetry(config);
            if (serviceOptions != null)
            {
                services.Configure(serviceOptions);
            }
            return services;
        }

        private class MockLoggingFactory : ILoggerFactory
        {
            public void Dispose()
            {
            }

            public ILogger CreateLogger(string categoryName)
            {
                return null;
            }

            public void AddProvider(ILoggerProvider provider)
            {
            }
        }
    }
}