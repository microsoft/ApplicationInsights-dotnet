using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace Microsoft.Extensions.DependencyInjection.Test
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using Logging;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.AspNetCore;
    using Microsoft.ApplicationInsights.AspNetCore.Logging;
    using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
    using Microsoft.ApplicationInsights.AspNetCore.Tests;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DependencyCollector;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Hosting.Internal;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Internal;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;

#if NET451 || NET46
    using ApplicationInsights.Extensibility.PerfCounterCollector;
    using ApplicationInsights.WindowsServer.TelemetryChannel;
    using ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
#endif

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
            services.AddSingleton<IHostingEnvironment>(new HostingEnvironment());
            services.AddSingleton<DiagnosticListener>(new DiagnosticListener("TestListener"));
            return services;
        }

        public static class AddApplicationInsightsTelemetry
        {
            [Theory]
            [InlineData(typeof(ITelemetryInitializer), typeof(AzureWebAppRoleEnvironmentTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(DomainNameRoleInstanceTelemetryInitializer), ServiceLifetime.Singleton)]
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
            [InlineData(typeof(ITelemetryInitializer), typeof(AzureWebAppRoleEnvironmentTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(DomainNameRoleInstanceTelemetryInitializer), ServiceLifetime.Singleton)]
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
                var services = CreateServicesAndAddApplicationinsightsTelemetry("content\\config-instrumentation-key.json", null);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                Assert.Equal(TestInstrumentationKey, telemetryConfiguration.InstrumentationKey);
            }

            /// <summary>
            /// Tests that the Active configuration singleton is used as the telemetry configuration instance by the configuration factory.
            /// This demonstrates that existing documentation for how to create a telemetry client and track custom events etc. works in ASP.NET 5
            /// when no ApplicationInsights.config file exists but a project.json file does exist which contains the instrumentation key.
            /// </summary>
            [Fact]
            public static void ConfigurationFactoryMethodUpdatesTheActiveConfigurationSingletonByDefault()
            {
                var services = CreateServicesAndAddApplicationinsightsTelemetry("content\\config-instrumentation-key.json", null);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                TelemetryConfiguration telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                Assert.Equal(TestInstrumentationKey, TelemetryConfiguration.Active.InstrumentationKey);
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatReadsDeveloperModeFromConfiguration()
            {
                var services = CreateServicesAndAddApplicationinsightsTelemetry("content\\config-developer-mode.json", null);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                Assert.Equal(true, telemetryConfiguration.TelemetryChannel.DeveloperMode);
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatReadsEndpointAddressFromConfiguration()
            {
                var services = CreateServicesAndAddApplicationinsightsTelemetry("content\\config-endpoint-address.json", null);

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
                    Assert.Equal(true, telemetryConfiguration.TelemetryChannel.DeveloperMode);
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

#if NET451 || NET46
                Assert.Equal(2, modules.Count());
                var perfCounterModule = services.FirstOrDefault<ServiceDescriptor>(t => t.ImplementationType == typeof(PerformanceCollectorModule));
                Assert.NotNull(perfCounterModule);
#else
                Assert.Equal(1, modules.Count());
#endif

                var dependencyModuleDescriptor = services.FirstOrDefault<ServiceDescriptor>(t => t.ImplementationFactory?.GetMethodInfo().ReturnType == typeof(DependencyTrackingTelemetryModule));
                Assert.NotNull(dependencyModuleDescriptor);

                var dependencyModule = modules.OfType<DependencyTrackingTelemetryModule>().Single();
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

#if NET451 || NET46
            [Fact]
            public static void AddsAddaptiveSamplingServiceToTheConfigurationInFullFrameworkByDefault()
            {
                var exisitingProcessorCount = GetTelemetryProcessorsCountInConfiguration<AdaptiveSamplingTelemetryProcessor>(TelemetryConfiguration.Active);
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, "http://localhost:1234/v2/track/", null, false);
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                var updatedCount = GetTelemetryProcessorsCountInConfiguration<AdaptiveSamplingTelemetryProcessor>(telemetryConfiguration);
                Assert.Equal(updatedCount, exisitingProcessorCount + 1);
            }

            [Fact]
            public static void DoesNotAddSamplingToConfigurationIfExplicitlyControlledThroughParameter()
            {
                var exisitingProcessorCount = GetTelemetryProcessorsCountInConfiguration<AdaptiveSamplingTelemetryProcessor>(TelemetryConfiguration.Active);
                Action<ApplicationInsightsServiceOptions> serviceOptions = options => options.EnableAdaptiveSampling = false;
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, "http://localhost:1234/v2/track/", serviceOptions, false);
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                var updatedCount = GetTelemetryProcessorsCountInConfiguration<AdaptiveSamplingTelemetryProcessor>(telemetryConfiguration);
                Assert.Equal(updatedCount, exisitingProcessorCount);
            }

            [Fact]
            public static void AddsAddaptiveSamplingServiceToTheConfigurationInFullFrameworkWithServiceOptions()
            {
                var exisitingProcessorCount = GetTelemetryProcessorsCountInConfiguration<AdaptiveSamplingTelemetryProcessor>(TelemetryConfiguration.Active);
                Action<ApplicationInsightsServiceOptions> serviceOptions = options => options.EnableAdaptiveSampling = true;
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, "http://localhost:1234/v2/track/", serviceOptions, false);
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                var updatedCount =  GetTelemetryProcessorsCountInConfiguration<AdaptiveSamplingTelemetryProcessor>(telemetryConfiguration);
                Assert.Equal(updatedCount, exisitingProcessorCount + 1);
            }

            [Fact]
            public static void AddsServerTelemetryChannelInFullFramework()
            {
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, "http://localhost:1234/v2/track/", null, false);
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                Assert.Equal(telemetryConfiguration.TelemetryChannel.GetType(), typeof(ServerTelemetryChannel));
            }

            [Fact]
            public static void DoesNotOverWriteExistingChannelInFullFramework()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                services.AddSingleton<ITelemetryChannel, InMemoryChannel>();
                var config = new ConfigurationBuilder().AddApplicationInsightsSettings(endpointAddress: "http://localhost:1234/v2/track/").Build();
                services.AddApplicationInsightsTelemetry(config);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                Assert.Equal(telemetryConfiguration.TelemetryChannel.GetType(), typeof(InMemoryChannel));
            }

            [Fact]
            public static void AddsQuickPulseProcessorToTheConfigurationInFullFrameworkByDefault()
            {
                var exisitingProcessorCount = GetTelemetryProcessorsCountInConfiguration<QuickPulseTelemetryProcessor>(TelemetryConfiguration.Active);
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, "http://localhost:1234/v2/track/", null, false);
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                var updatedCount = GetTelemetryProcessorsCountInConfiguration<QuickPulseTelemetryProcessor>(telemetryConfiguration);
                Assert.Equal(updatedCount, exisitingProcessorCount + 1);
            }

            [Fact]
            public static void DoesNotAddQuickPulseProcessorToConfigurationIfExplicitlyControlledThroughParameter()
            {
                var exisitingProcessorCount = GetTelemetryProcessorsCountInConfiguration<QuickPulseTelemetryProcessor>(TelemetryConfiguration.Active);
                Action<ApplicationInsightsServiceOptions> serviceOptions = options => options.EnableQuickPulseMetricStream = false;
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, "http://localhost:1234/v2/track/", serviceOptions, false);
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                var updatedCount = GetTelemetryProcessorsCountInConfiguration<QuickPulseTelemetryProcessor>(telemetryConfiguration);
                Assert.Equal(updatedCount, exisitingProcessorCount);
            }

            [Fact]
            public static void AddsQuickPulseProcessorToTheConfigurationInFullFrameworkWithServiceOptions()
            {
                var exisitingProcessorCount = GetTelemetryProcessorsCountInConfiguration<QuickPulseTelemetryProcessor>(TelemetryConfiguration.Active);
                Action< ApplicationInsightsServiceOptions> serviceOptions = options => options.EnableQuickPulseMetricStream = true;
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, "http://localhost:1234/v2/track/", serviceOptions, false);
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                var updatedCount =  GetTelemetryProcessorsCountInConfiguration<QuickPulseTelemetryProcessor>(telemetryConfiguration);
                Assert.Equal(updatedCount, exisitingProcessorCount + 1);
            }

            [Fact]
            public static void ProcessorsAreNotAddedToTheConfigurationWithExistingNonServerChannel()
            {
                int exisitingProcessorCount = GetTelemetryProcessorsCountInConfiguration<QuickPulseTelemetryProcessor>(TelemetryConfiguration.Active);
                ServiceCollection services = CreateServicesAndAddApplicationinsightsTelemetry(null, "http://localhost:1234/v2/track/", null, true);
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                TelemetryConfiguration telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                int updatedCount =  GetTelemetryProcessorsCountInConfiguration<QuickPulseTelemetryProcessor>(telemetryConfiguration);
                Assert.Equal(updatedCount, exisitingProcessorCount);
            }
#endif
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
                Assert.Equal(true, telemetryConfiguration.TelemetryChannel.DeveloperMode);
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatReadsEndpointAddressFromSettings()
            {
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, "http://localhost:1234/v2/track/");
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                Assert.Equal("http://localhost:1234/v2/track/", telemetryConfiguration.TelemetryChannel.EndpointAddress);
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
                config = new ConfigurationBuilder().AddJsonFile(jsonPath).Build();
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