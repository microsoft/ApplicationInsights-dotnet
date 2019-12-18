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
#if NETCOREAPP2_0
    using Microsoft.ApplicationInsights.Extensibility.EventCounterCollector;
#endif
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
    using Microsoft.ApplicationInsights.Extensibility.W3C;
    using Microsoft.ApplicationInsights.WindowsServer;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Hosting.Internal;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Internal;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;

#pragma warning disable CS0618 // TelemetryConfiguration.Active is obsolete. We still test with this for backwards compatibility.
    public static class ApplicationInsightsExtensionsTests
    {
        /// <summary>Constant instrumentation key value for testintg.</summary>
        public const string TestInstrumentationKey = "11111111-2222-3333-4444-555555555555";
        private const string TestConnectionString = "InstrumentationKey=11111111-2222-3333-4444-555555555555;IngestionEndpoint=http://127.0.0.1";
        private const string InstrumentationKeyFromConfig = "ApplicationInsights:InstrumentationKey";
        private const string ConnectionStringEnvironmentVariable = "APPLICATIONINSIGHTS_CONNECTION_STRING";

        public static ServiceCollection GetServiceCollectionWithContextAccessor()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IHostingEnvironment>(new HostingEnvironment() { ContentRootPath = Directory.GetCurrentDirectory()});
            services.AddSingleton<DiagnosticListener>(new DiagnosticListener("TestListener"));
            return services;
        }

        public static class AddApplicationInsightsTelemetry
        {
            [Theory]
            [InlineData(typeof(ITelemetryInitializer), typeof(ApplicationInsights.AspNetCore.TelemetryInitializers.DomainNameRoleInstanceTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(AzureAppServiceRoleNameFromHostNameHeaderInitializer), ServiceLifetime.Singleton)]            
            [InlineData(typeof(ITelemetryInitializer), typeof(ComponentVersionTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(ClientIpHeaderTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(OperationNameTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(SyntheticTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(WebSessionTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(WebUserTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(HttpDependenciesParsingTelemetryInitializer), ServiceLifetime.Singleton)]
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
            [InlineData(typeof(ITelemetryInitializer), typeof(AzureAppServiceRoleNameFromHostNameHeaderInitializer), ServiceLifetime.Singleton)]            
            [InlineData(typeof(ITelemetryInitializer), typeof(ComponentVersionTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(ClientIpHeaderTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(OperationNameTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(SyntheticTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(WebSessionTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(WebUserTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(HttpDependenciesParsingTelemetryInitializer), ServiceLifetime.Singleton)]
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
            /// Tests that the connection string can be read from a JSON file by the configuration factory.            
            /// </summary>
            [Fact]
            [Trait("Trait", "ConnectionString")]
            public static void RegistersTelemetryConfigurationFactoryMethodThatReadsConnectionStringFromConfiguration()
            {
                var services = CreateServicesAndAddApplicationinsightsTelemetry(Path.Combine("content", "config-connection-string.json"), null);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                Assert.Equal(TestConnectionString, telemetryConfiguration.ConnectionString);
                Assert.Equal(TestInstrumentationKey, telemetryConfiguration.InstrumentationKey);
                Assert.Equal("http://127.0.0.1/", telemetryConfiguration.EndpointContainer.Ingestion.AbsoluteUri);
            }

            /// <summary>
            /// Tests that the connection string can be read from a JSON file by the configuration factory.            
            /// This config has both a connection string and an instrumentation key. It is expected to use the ikey from the connection string.
            /// </summary>
            [Fact]
            [Trait("Trait", "ConnectionString")]
            public static void RegistersTelemetryConfigurationFactoryMethodThatReadsConnectionStringAndInstrumentationKeyFromConfiguration()
            {
                var services = CreateServicesAndAddApplicationinsightsTelemetry(Path.Combine("content", "config-connection-string-and-instrumentation-key.json"), null);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                Assert.Equal(TestConnectionString, telemetryConfiguration.ConnectionString);
                Assert.Equal(TestInstrumentationKey, telemetryConfiguration.InstrumentationKey);
                Assert.Equal("http://127.0.0.1/", telemetryConfiguration.EndpointContainer.Ingestion.AbsoluteUri);
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
            /// We determine if Active telemetry needs to be configured based on the assumptions that 'default' configuration
            // created by base SDK has single preset ITelemetryInitializer. If it ever changes, change TelemetryConfigurationOptions.IsActiveConfigured method as well.
            /// </summary>
            [Fact]
            public static void DefaultTelemetryConfigurationHasOneTelemetryInitializer()
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
            [Trait("Trait", "ConnectionString")]
            public static void AddApplicationInsightsTelemetry_ReadsConnectionString_FromEnvironment()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                Environment.SetEnvironmentVariable(ConnectionStringEnvironmentVariable, TestConnectionString);
                try
                {
                    services.AddApplicationInsightsTelemetry();
                    IServiceProvider serviceProvider = services.BuildServiceProvider();
                    var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                    Assert.Equal(TestConnectionString, telemetryConfiguration.ConnectionString);
                    Assert.Equal(TestInstrumentationKey, telemetryConfiguration.InstrumentationKey);
                    Assert.Equal("http://127.0.0.1/", telemetryConfiguration.EndpointContainer.Ingestion.AbsoluteUri);
                }
                finally
                {
                    Environment.SetEnvironmentVariable(ConnectionStringEnvironmentVariable, null);
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

            /// <summary>
            /// Validates that while using services.AddApplicationInsightsTelemetry(ApplicationInsightsServiceOptions), with null ikey
            /// and endpoint, ikey and endpoint from AppSettings.Json is NOT overwritten with the null/empty ones from
            /// ApplicationInsightsServiceOptions
            /// </summary>
            [Fact]
            public static void AddApplicationInsightsTelemetryDoesNotOverrideEmptyInstrumentationKeyFromAiOptions()
            {
                // Create new options, which will be default have null ikey and endpoint.
                var options = new ApplicationInsightsServiceOptions();
                string ikey = Guid.NewGuid().ToString();
                string text = File.ReadAllText("appsettings.json");
                try
                {
                    text = text.Replace("ikeyhere", ikey);
                    text = text.Replace("hosthere", "newhost");
                    File.WriteAllText("appsettings.json", text);

                    var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                    services.AddApplicationInsightsTelemetry(options);
                    IServiceProvider serviceProvider = services.BuildServiceProvider();
                    var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                    Assert.Equal(ikey, telemetryConfiguration.InstrumentationKey);
                    Assert.Equal("http://newhost/v2/track/", telemetryConfiguration.DefaultTelemetrySink.TelemetryChannel.EndpointAddress);
                }
                finally
                {
                    text = text.Replace(ikey, "ikeyhere");
                    text = text.Replace("newhost", "hosthere");
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
                    InstrumentationKey = "test",
                    ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000"
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

#if NETCOREAPP2_0
                Assert.Equal(7, modules.Count());
#else
                Assert.Equal(6, modules.Count());
#endif

                var perfCounterModule = modules.OfType<PerformanceCollectorModule>().Single();
                Assert.NotNull(perfCounterModule);

#if NETCOREAPP2_0
                var eventCounterModule = modules.OfType<EventCounterCollectionModule>().Single();
                Assert.NotNull(eventCounterModule);
#endif


                var dependencyModuleDescriptor = modules.OfType<DependencyTrackingTelemetryModule>().Single();
                Assert.NotNull(dependencyModuleDescriptor);

                var reqModuleDescriptor = modules.OfType<RequestTrackingTelemetryModule>().Single();
                Assert.NotNull(reqModuleDescriptor);

                var appServiceHeartBeatModuleDescriptor = modules.OfType<AppServicesHeartbeatTelemetryModule>().Single();
                Assert.NotNull(appServiceHeartBeatModuleDescriptor);

                var azureMetadataHeartBeatModuleDescriptor = modules.OfType<AzureInstanceMetadataTelemetryModule>().Single();
                Assert.NotNull(azureMetadataHeartBeatModuleDescriptor);

                var quickPulseModuleDescriptor = modules.OfType<QuickPulseTelemetryModule>().Single();
                Assert.NotNull(quickPulseModuleDescriptor);
            }

#if NETCOREAPP2_0
            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatPopulatesEventCounterCollectorWithDefaultListOfCounters()
            {
                //ARRANGE
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, null, null, false);
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var modules = serviceProvider.GetServices<ITelemetryModule>();

                //ACT

                // Requesting TelemetryConfiguration from services trigger constructing the TelemetryConfiguration
                // which in turn trigger configuration of all modules.
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                var eventCounterModule = modules.OfType<EventCounterCollectionModule>().Single();

                //VALIDATE
                Assert.Equal(23, eventCounterModule.Counters.Count);
                
                // sanity check with a sample counter.
                var cpuCounterRequest = eventCounterModule.Counters.FirstOrDefault<EventCounterCollectionRequest>(
                    eventCounterCollectionRequest => eventCounterCollectionRequest.EventSourceName == "System.Runtime"
                    && eventCounterCollectionRequest.EventCounterName == "cpu-usage");
                Assert.NotNull(cpuCounterRequest);

                // sanity check - asp.net counters should be added
                var aspnetCounterRequest = eventCounterModule.Counters.Where<EventCounterCollectionRequest>(
                    eventCounterCollectionRequest => eventCounterCollectionRequest.EventSourceName == "Microsoft.AspNetCore.Hosting");
                Assert.NotNull(aspnetCounterRequest);
                Assert.True(aspnetCounterRequest.Count() == 4);
            }
#endif

            [Fact]
            public static void UserCanDisablePerfCollectorModule()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                var aiOptions = new ApplicationInsightsServiceOptions();
                aiOptions.EnablePerformanceCounterCollectionModule = false;
                services.AddApplicationInsightsTelemetry(aiOptions);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var modules = serviceProvider.GetServices<ITelemetryModule>();
                Assert.NotNull(modules);

                // Even if a module is disabled its still added to DI.
                Assert.NotEmpty(modules.OfType<PerformanceCollectorModule>());

                // TODO add unit test to validate that module.isInitialized is false.
                // similar to being done in UserCanDisableRequestCounterCollectorModule
                // It requires some restructuring as internals are not accessible
                // to this test project
            }

#if NETCOREAPP2_0
            [Fact]
            public static void UserCanDisableEventCounterCollectorModule()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                var aiOptions = new ApplicationInsightsServiceOptions();
                aiOptions.EnableEventCounterCollectionModule = false;
                services.AddApplicationInsightsTelemetry(aiOptions);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var modules = serviceProvider.GetServices<ITelemetryModule>();
                Assert.NotNull(modules);

                // Even if a module is disabled its still added to DI.
                Assert.NotEmpty(modules.OfType<EventCounterCollectionModule>());

                // TODO add unit test to validate that module.isInitialized is false.
                // similar to being done in UserCanDisableRequestCounterCollectorModule
                // It requires some restructuring as internals are not accessible
                // to this test project
            }
#endif

            [Fact]
            public static void UserCanDisableRequestCounterCollectorModule()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                var aiOptions = new ApplicationInsightsServiceOptions();
                aiOptions.EnableRequestTrackingTelemetryModule = false;
                services.AddApplicationInsightsTelemetry(aiOptions);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                // Get telemetry client to trigger TelemetryConfig setup.
                var tc = serviceProvider.GetService<TelemetryClient>();
                var modules = serviceProvider.GetServices<ITelemetryModule>();
                Assert.NotNull(modules);

                // Even if a module is disabled its still added to DI.
                Assert.NotEmpty(modules.OfType<RequestTrackingTelemetryModule>());
                var req = modules.OfType<RequestTrackingTelemetryModule>().First();

                // But the module will not be initialized.
                Assert.False(req.IsInitialized);
            }

            [Fact]
            public static void UserCanDisableDependencyCollectorModule()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                var aiOptions = new ApplicationInsightsServiceOptions();
                aiOptions.EnableDependencyTrackingTelemetryModule = false;
                services.AddApplicationInsightsTelemetry(aiOptions);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var modules = serviceProvider.GetServices<ITelemetryModule>();                
                Assert.NotNull(modules);

                // Even if a module is disabled its still added to DI.
                Assert.NotEmpty(modules.OfType<DependencyTrackingTelemetryModule>());

                // TODO add unit test to validate that module.isInitialized is false.
                // similar to being done in UserCanDisableRequestCounterCollectorModule
                // It requires some restructuring as internals are not accessible
                // to this test project
            }

            [Fact]
            public static void UserCanDisableQuickPulseCollectorModule()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                var aiOptions = new ApplicationInsightsServiceOptions();
                aiOptions.EnableQuickPulseMetricStream = false;
                services.AddApplicationInsightsTelemetry(aiOptions);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var modules = serviceProvider.GetServices<ITelemetryModule>();
                Assert.NotNull(modules);

                // Even if a module is disabled its still added to DI.
                Assert.NotEmpty(modules.OfType<QuickPulseTelemetryModule>());

                // TODO add unit test to validate that module.isInitialized is false.
                // similar to being done in UserCanDisableRequestCounterCollectorModule
                // It requires some restructuring as internals are not accessible
                // to this test project
            }

            [Fact]
            public static void UserCanDisableAppServiceHeartbeatModule()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                var aiOptions = new ApplicationInsightsServiceOptions();
                aiOptions.EnableAppServicesHeartbeatTelemetryModule = false;
                services.AddApplicationInsightsTelemetry(aiOptions);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var modules = serviceProvider.GetServices<ITelemetryModule>();
                Assert.NotNull(modules);

                // Even if a module is disabled its still added to DI.
                Assert.NotEmpty(modules.OfType<AppServicesHeartbeatTelemetryModule>());

                // TODO add unit test to validate that module.isInitialized is false.
                // similar to being done in UserCanDisableRequestCounterCollectorModule
                // It requires some restructuring as internals are not accessible
                // to this test project
            }

            [Fact]
            public static void UserCanDisableAzureInstanceMetadataModule()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                var aiOptions = new ApplicationInsightsServiceOptions();
                aiOptions.EnableAzureInstanceMetadataTelemetryModule = false;
                services.AddApplicationInsightsTelemetry(aiOptions);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var modules = serviceProvider.GetServices<ITelemetryModule>();
                Assert.NotNull(modules);

                // Even if a module is disabled its still added to DI.
                Assert.NotEmpty(modules.OfType<AzureInstanceMetadataTelemetryModule>());

                // TODO add unit test to validate that module.isInitialized is false.
                // similar to being done in UserCanDisableRequestCounterCollectorModule
                // It requires some restructuring as internals are not accessible
                // to this test project
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
                Assert.Equal(4, dependencyModule.ExcludeComponentCorrelationHttpHeadersOnDomains.Count);
                Assert.False(dependencyModule.ExcludeComponentCorrelationHttpHeadersOnDomains.Contains("localhost"));
                Assert.False(dependencyModule.ExcludeComponentCorrelationHttpHeadersOnDomains.Contains("127.0.0.1"));
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatPopulatesDependencyCollectorWithCustomValues()
            {
                //ARRANGE
                var services = CreateServicesAndAddApplicationinsightsTelemetry(
                    null,
                    null,
                    o => { o.DependencyCollectionOptions.EnableLegacyCorrelationHeadersInjection = true; },
                    false);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var modules = serviceProvider.GetServices<ITelemetryModule>();

                // Requesting TelemetryConfiguration from services trigger constructing the TelemetryConfiguration
                // which in turn trigger configuration of all modules.
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();

                //ACT
                var dependencyModule = modules.OfType<DependencyTrackingTelemetryModule>().Single();

                //VALIDATE
                Assert.Equal(6, dependencyModule.ExcludeComponentCorrelationHttpHeadersOnDomains.Count);
                Assert.True(dependencyModule.ExcludeComponentCorrelationHttpHeadersOnDomains.Contains("localhost"));
                Assert.True(dependencyModule.ExcludeComponentCorrelationHttpHeadersOnDomains.Contains("127.0.0.1"));
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatPopulatesItWithTelemetryProcessorFactoriesFromContainer()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                services.AddApplicationInsightsTelemetryProcessor<FakeTelemetryProcessor>();

                services.AddApplicationInsightsTelemetry(new ConfigurationBuilder().Build());

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                // TP added via AddApplicationInsightsTelemetryProcessor is added to the default sink.
                FakeTelemetryProcessor telemetryProcessor = telemetryConfiguration.DefaultTelemetrySink.TelemetryProcessors.OfType<FakeTelemetryProcessor>().FirstOrDefault();
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

                // TP added via AddApplicationInsightsTelemetryProcessor is added to the default sink.
                FakeTelemetryProcessorWithImportingConstructor telemetryProcessor = telemetryConfiguration.DefaultTelemetrySink.TelemetryProcessors.OfType<FakeTelemetryProcessorWithImportingConstructor>().FirstOrDefault();
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
#if NETCOREAPP2_0
                Assert.False(requestTrackingModule.CollectionOptions.TrackExceptions);
#else
                Assert.True(requestTrackingModule.CollectionOptions.TrackExceptions);
#endif
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
                var adaptiveSamplingProcessorCount = GetTelemetryProcessorsCountInConfigurationDefaultSink<AdaptiveSamplingTelemetryProcessor>(telemetryConfiguration);

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
                var qpProcessorCount = GetTelemetryProcessorsCountInConfigurationDefaultSink<AdaptiveSamplingTelemetryProcessor>(telemetryConfiguration);
                Assert.Equal(0, qpProcessorCount);
            }

            [Fact]
            public static void AddsAddaptiveSamplingServiceToTheConfigurationWithServiceOptions()
            {                
                Action<ApplicationInsightsServiceOptions> serviceOptions = options => options.EnableAdaptiveSampling = true;
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, "http://localhost:1234/v2/track/", serviceOptions, false);
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                var adaptiveSamplingProcessorCount = GetTelemetryProcessorsCountInConfigurationDefaultSink<AdaptiveSamplingTelemetryProcessor>(telemetryConfiguration);
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
            [Trait("Trait", "ConnectionString")]
            public static void AddApplicationInsightsSettings_SetsConnectionString()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                services.AddSingleton<ITelemetryChannel>(new InMemoryChannel());
                var config = new ConfigurationBuilder().AddApplicationInsightsSettings(connectionString: TestConnectionString).Build();
                services.AddApplicationInsightsTelemetry(config);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                Assert.Equal(TestConnectionString, telemetryConfiguration.ConnectionString);
                Assert.Equal(TestInstrumentationKey, telemetryConfiguration.InstrumentationKey);
                Assert.Equal("http://127.0.0.1/", telemetryConfiguration.EndpointContainer.Ingestion.AbsoluteUri);
            }

            [Fact]
            [Trait("Trait", "Endpoints")]
            public static void DoesNotOverWriteExistingChannel()
            {
                var testEndpoint = "http://localhost:1234/v2/track/";

                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                services.AddSingleton<ITelemetryChannel, InMemoryChannel>();
                var config = new ConfigurationBuilder().AddApplicationInsightsSettings(endpointAddress: testEndpoint).Build();
                services.AddApplicationInsightsTelemetry(config);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                Assert.Equal(typeof(InMemoryChannel), telemetryConfiguration.TelemetryChannel.GetType());
                Assert.Equal(testEndpoint, telemetryConfiguration.TelemetryChannel.EndpointAddress);
            }

            [Fact]
            public static void FallbacktoDefaultChannelWhenNoChannelFoundInDI()
            {
                var testEndpoint = "http://localhost:1234/v2/track/";

                // ARRANGE
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();                                
                var config = new ConfigurationBuilder().AddApplicationInsightsSettings(endpointAddress: testEndpoint).Build();
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
                Assert.Equal(testEndpoint, telemetryConfiguration.TelemetryChannel.EndpointAddress);
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
            public static void ValidatesThatOnlyPassThroughProcessorIsAddedToCommonPipeline()
            {
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, "http://localhost:1234/v2/track/", null, false);
                services.AddSingleton<ITelemetryChannel, InMemoryChannel>();
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();

                // All default TelemetryProcessors are expected to be on the default sink. There should be
                // none on the main pipeline except the PassThrough.

                var qpProcessorCount = GetTelemetryProcessorsCountInConfiguration<QuickPulseTelemetryProcessor>(telemetryConfiguration);
                Assert.Equal(0, qpProcessorCount);

                var metricExtractorProcessorCount = GetTelemetryProcessorsCountInConfiguration<AutocollectedMetricsExtractor>(telemetryConfiguration);
                Assert.Equal(0, metricExtractorProcessorCount);

                var samplingProcessorCount = GetTelemetryProcessorsCountInConfiguration<AdaptiveSamplingTelemetryProcessor>(telemetryConfiguration);
                Assert.Equal(0, samplingProcessorCount);

                var passThroughProcessorCount = telemetryConfiguration.TelemetryProcessors.Count;
                Assert.Equal(1, passThroughProcessorCount);
                
                Assert.Equal("PassThroughProcessor", telemetryConfiguration.TelemetryProcessors[0].GetType().Name);
            }

            [Fact]
            public static void AddsQuickPulseProcessorToTheConfigurationByDefault()
            {                
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, "http://localhost:1234/v2/track/", null, false);
                services.AddSingleton<ITelemetryChannel, InMemoryChannel>();
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                var qpProcessorCount = GetTelemetryProcessorsCountInConfigurationDefaultSink<QuickPulseTelemetryProcessor>(telemetryConfiguration);
                Assert.Equal(1, qpProcessorCount);
            }

            [Fact]
            public static void AddsAutoCollectedMetricsExtractorProcessorToTheConfigurationByDefault()
            {
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, "http://localhost:1234/v2/track/", null, false);
                services.AddSingleton<ITelemetryChannel, InMemoryChannel>();
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                var metricExtractorProcessorCount = GetTelemetryProcessorsCountInConfigurationDefaultSink<AutocollectedMetricsExtractor>(telemetryConfiguration);
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
                var metricExtractorProcessorCount = GetTelemetryProcessorsCountInConfigurationDefaultSink<AutocollectedMetricsExtractor>(telemetryConfiguration);
                Assert.Equal(0, metricExtractorProcessorCount);
            }

            [Fact]
            public static void DoesNotAddQuickPulseProcessorToConfigurationIfExplicitlyControlledThroughParameter()
            {                
                Action<ApplicationInsightsServiceOptions> serviceOptions = options => options.EnableQuickPulseMetricStream = false;
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, "http://localhost:1234/v2/track/", serviceOptions, false);
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                var qpProcessorCount = GetTelemetryProcessorsCountInConfigurationDefaultSink<QuickPulseTelemetryProcessor>(telemetryConfiguration);
                Assert.Equal(0, qpProcessorCount);
            }

            [Fact]
            public static void AddsQuickPulseProcessorToTheConfigurationWithServiceOptions()
            {                
                Action< ApplicationInsightsServiceOptions> serviceOptions = options => options.EnableQuickPulseMetricStream = true;
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, "http://localhost:1234/v2/track/", serviceOptions, false);
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
                var qpProcessorCount = GetTelemetryProcessorsCountInConfigurationDefaultSink<QuickPulseTelemetryProcessor>(telemetryConfiguration);
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

            [Fact]
            public static void W3CIsEnabledByDefault()
            {
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, "http://localhost:1234/v2/track/");
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();

                Assert.DoesNotContain(telemetryConfiguration.TelemetryInitializers, t => t is W3COperationCorrelationTelemetryInitializer);
                Assert.DoesNotContain(TelemetryConfiguration.Active.TelemetryInitializers, t => t is W3COperationCorrelationTelemetryInitializer);

                var modules = serviceProvider.GetServices<ITelemetryModule>().ToList();

                var requestTracking = modules.OfType<RequestTrackingTelemetryModule>().ToList();
                var dependencyTracking = modules.OfType<DependencyTrackingTelemetryModule>().ToList();
                Assert.Single(requestTracking);
                Assert.Single(dependencyTracking);

                Assert.True(Activity.DefaultIdFormat == ActivityIdFormat.W3C);
                Assert.True(Activity.ForceDefaultIdFormat);
            }

            private static int GetTelemetryProcessorsCountInConfiguration<T>(TelemetryConfiguration telemetryConfiguration)
            {
                return telemetryConfiguration.TelemetryProcessors.Where(processor => processor.GetType() == typeof(T)).Count();
            }

            private static int GetTelemetryProcessorsCountInConfigurationDefaultSink<T>(TelemetryConfiguration telemetryConfiguration)
            {
                return telemetryConfiguration.DefaultTelemetrySink.TelemetryProcessors.Where(processor => processor.GetType() == typeof(T)).Count();
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
#pragma warning restore CS0618 // TelemetryConfiguration.Active is obsolete. We still test with this for backwards compatibility.
}