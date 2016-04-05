namespace Microsoft.Extensions.DependencyInjection
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.AspNet.ContextInitializers;
    using Microsoft.ApplicationInsights.AspNet.TelemetryInitializers;
    using Microsoft.ApplicationInsights.AspNet.Tests;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNet.Http;
    using Microsoft.AspNet.Http.Internal;
    using Microsoft.Extensions.Configuration;
    using Xunit;

#if dnx451
    using ApplicationInsights.DependencyCollector;
    using ApplicationInsights.Extensibility.PerfCounterCollector;
#endif

    public static class ApplicationInsightsExtensionsTests
    {
        /// <summary>Constant instrumentation key value for testintg.</summary>
        public const string TestInstrumentationKey = "11111111-2222-3333-4444-555555555555";

        public static ServiceCollection GetServiceCollectionWithContextAccessor()
        {
            var services = new ServiceCollection();
            IHttpContextAccessor contextAccessor = new HttpContextAccessor();
            services.AddInstance<IHttpContextAccessor>(contextAccessor);
            services.AddInstance<DiagnosticListener>(new DiagnosticListener("TestListener"));
            return services;
        }

        public static class AddApplicationInsightsTelemetry
        {
            [Theory]
            [InlineData(typeof(ITelemetryInitializer), typeof(DomainNameRoleInstanceTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(ClientIpHeaderTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(OperationNameTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(OperationIdTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(UserAgentTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(WebSessionTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(WebUserTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(TelemetryConfiguration), null, ServiceLifetime.Singleton)]
            [InlineData(typeof(TelemetryClient), typeof(TelemetryClient), ServiceLifetime.Scoped)]
            public static void RegistersExpectedServices(Type serviceType, Type implementationType, ServiceLifetime lifecycle)
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();

                services.AddApplicationInsightsTelemetry(new ConfigurationBuilder().Build(), false);

                ServiceDescriptor service = services.Single(s => s.ServiceType == serviceType && s.ImplementationType == implementationType);
                Assert.Equal(lifecycle, service.Lifetime);
            }

            [Fact]
            public static void DoesNotThrowWithoutInstrumentationKey()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();

                //Empty configuration that doesn't have instrumentation key
                IConfiguration config = new ConfigurationBuilder().Build();

                services.AddApplicationInsightsTelemetry(config, false);
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatCreatesDefaultInstance()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();

                services.AddApplicationInsightsTelemetry(new ConfigurationBuilder().Build(), false);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                Assert.Contains(telemetryConfiguration.TelemetryInitializers, t => t is OperationIdTelemetryInitializer);
            }

            /// <summary>
            /// Tests that the instrumentation key configuration can be read from a JSON file by the configuration factory.
            /// </summary>
            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatReadsInstrumentationKeyFromConfiguration()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                var config = new ConfigurationBuilder().AddJsonFile("content\\config-instrumentation-key.json").Build();

                services.AddApplicationInsightsTelemetry(config, false);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                Assert.Equal(TestInstrumentationKey, telemetryConfiguration.InstrumentationKey);
            }

            /// <summary>
            /// Tests that the Active configuration singleton is not updated by the configuration factory when specifying false for re-using
            /// the Active config singleton when registering Application Insights services for dependency injection.
            /// </summary>
            [Fact]
            public static void ConfigurationFactoryMethodDoesNotUpdateTheActiveConfigurationSingletonWhenSetToFalse()
            {
                string existingActiveIkey = TelemetryConfiguration.Active.InstrumentationKey;

                ServiceCollection services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                IConfiguration config = new ConfigurationBuilder().AddJsonFile("content\\config-instrumentation-key-new.json").Build();

                Assert.NotEqual(existingActiveIkey, config.GetSection("ApplicationInsights:InstrumentationKey").Value);
                services.AddApplicationInsightsTelemetry(config, false);
            
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                TelemetryConfiguration telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                Assert.Equal(existingActiveIkey, TelemetryConfiguration.Active.InstrumentationKey);
            }

            /// <summary>
            /// Tests that the Active configuration singleton is used as the telemetry configuration instance by the configuration factory.
            /// This demonstrates that existing documentation for how to create a telemetry client and track custom events etc. works in ASP.NET 5
            /// when no ApplicationInsights.config file exists but a project.json file does exist which contains the instrumentation key.
            /// </summary>
            [Fact]
            public static void ConfigurationFactoryMethodUpdatesTheActiveConfigurationSingletonByDefault()
            {
                ServiceCollection services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                IConfiguration config = new ConfigurationBuilder().AddJsonFile("content\\config-instrumentation-key.json").Build();

                services.AddApplicationInsightsTelemetry(config); // The default should be the same as explicitly passing true: (config, true).

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                TelemetryConfiguration telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                Assert.Equal(TestInstrumentationKey, TelemetryConfiguration.Active.InstrumentationKey);
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatReadsDeveloperModeFromConfiguration()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                var config = new ConfigurationBuilder().AddJsonFile("content\\config-developer-mode.json").Build();

                services.AddApplicationInsightsTelemetry(config, false);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                Assert.Equal(true, telemetryConfiguration.TelemetryChannel.DeveloperMode);
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatReadsEndpointAddressFromConfiguration()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                var config = new ConfigurationBuilder().AddJsonFile("content\\config-endpoint-address.json").Build();

                services.AddApplicationInsightsTelemetry(config, false);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                Assert.Equal("http://localhost:1234/v2/track/", telemetryConfiguration.TelemetryChannel.EndpointAddress);
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatReadsInstrumentationKeyFromEnvironment()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();

                Environment.SetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", TestInstrumentationKey);
                var config = new ConfigurationBuilder().AddEnvironmentVariables().Build();
                try
                {
                    services.AddApplicationInsightsTelemetry(config, false);

                    IServiceProvider serviceProvider = services.BuildServiceProvider();
                    var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
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
                Environment.SetEnvironmentVariable("APPINSIGHTS_DEVELOPER_MODE", "true");
                var config = new ConfigurationBuilder().AddEnvironmentVariables().Build();
                try
                {
                    services.AddApplicationInsightsTelemetry(config, false);

                    IServiceProvider serviceProvider = services.BuildServiceProvider();
                    var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
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
                Environment.SetEnvironmentVariable("APPINSIGHTS_ENDPOINTADDRESS", "http://localhost:1234/v2/track/");
                var config = new ConfigurationBuilder().AddEnvironmentVariables().Build();
                try
                {
                    services.AddApplicationInsightsTelemetry(config, false);

                    IServiceProvider serviceProvider = services.BuildServiceProvider();
                    var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
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
                services.AddInstance<ITelemetryInitializer>(telemetryInitializer);

                services.AddApplicationInsightsTelemetry(new ConfigurationBuilder().Build(), false);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                Assert.Contains(telemetryInitializer, telemetryConfiguration.TelemetryInitializers);
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatPopulatesItWithTelemetryChannelFromContainer()
            {
                var telemetryChannel = new FakeTelemetryChannel();
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                services.AddInstance<ITelemetryChannel>(telemetryChannel);

                services.AddApplicationInsightsTelemetry(new ConfigurationBuilder().Build(), false);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                Assert.Same(telemetryChannel, telemetryConfiguration.TelemetryChannel);
            }

            [Fact]
            public static void DoesNotOverrideDefaultTelemetryChannelIfTelemetryChannelServiceIsNotRegistered()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();

                services.AddApplicationInsightsTelemetry(new ConfigurationBuilder().Build(), false);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                Assert.NotNull(telemetryConfiguration.TelemetryChannel);
            }

            [Fact]
            public static void RegistersTelemetryClientToGetTelemetryConfigurationFromContainerAndNotGlobalInstance()
            {
                ITelemetry sentTelemetry = null;
                var telemetryChannel = new FakeTelemetryChannel { OnSend = telemetry => sentTelemetry = telemetry };

                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();

                services.AddApplicationInsightsTelemetry(new ConfigurationBuilder().Build(), false);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var configuration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                configuration.InstrumentationKey = Guid.NewGuid().ToString();
                configuration.TelemetryChannel = telemetryChannel;

                var telemetryClient = serviceProvider.GetRequiredService<TelemetryClient>();
                telemetryClient.TrackEvent("myevent");

                // We want to check that configuration from contaier was used but configuration is a private field so we check
                // instrumentation key instead
                Assert.Equal(configuration.InstrumentationKey, sentTelemetry.Context.InstrumentationKey);
            }

#if dnx451
            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatPopulatesItWithModulesFromContainer()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();

                services.AddApplicationInsightsTelemetry(new ConfigurationBuilder().Build(), false);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var modules = serviceProvider.GetServices<ITelemetryModule>();
                Assert.NotNull(modules);
                Assert.Equal(2, modules.Count());

                var dependencyModule = services.FirstOrDefault<ServiceDescriptor>(t => t.ImplementationType == typeof(DependencyTrackingTelemetryModule));
                Assert.NotNull(dependencyModule);

                var perfCounterModule = services.FirstOrDefault<ServiceDescriptor>(t => t.ImplementationType == typeof(PerformanceCollectorModule));
                Assert.NotNull(perfCounterModule);
            }
#endif
        }

        public static class AddApplicationInsightsSettings
        {
            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatReadsInstrumentationKeyFromSettings()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();

                var config = new ConfigurationBuilder().AddApplicationInsightsSettings(instrumentationKey: TestInstrumentationKey).Build();
                services.AddApplicationInsightsTelemetry(config, false);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                Assert.Equal(TestInstrumentationKey, telemetryConfiguration.InstrumentationKey);
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatReadsDeveloperModeFromSettings()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                var config = new ConfigurationBuilder().AddApplicationInsightsSettings(developerMode: true).Build();
                services.AddApplicationInsightsTelemetry(config, false);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                Assert.Equal(true, telemetryConfiguration.TelemetryChannel.DeveloperMode);
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatReadsEndpointAddressFromSettings()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                var config = new ConfigurationBuilder().AddApplicationInsightsSettings(endpointAddress: "http://localhost:1234/v2/track/").Build();
                services.AddApplicationInsightsTelemetry(config, false);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                Assert.Equal("http://localhost:1234/v2/track/", telemetryConfiguration.TelemetryChannel.EndpointAddress);
            }
        }
    }
}