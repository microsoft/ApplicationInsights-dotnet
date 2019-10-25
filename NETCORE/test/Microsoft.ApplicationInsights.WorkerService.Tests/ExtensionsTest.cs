using Microsoft.ApplicationInsights.WorkerService.TelemetryInitializers;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.EventCounterCollector;
using Microsoft.ApplicationInsights.Extensibility.Implementation.ApplicationId;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Microsoft.ApplicationInsights.WindowsServer;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.ApplicationInsights.WorkerService.Tests
{
    public class ExtensionsTests
    {
        private readonly ITestOutputHelper output;
        private const string TestConnectionString = "InstrumentationKey=11111111-2222-3333-4444-555555555555;IngestionEndpoint=http://127.0.0.1";
        public const string TestInstrumentationKey = "11111111-2222-3333-4444-555555555555";
        public const string TestInstrumentationKeyEnv = "AAAAAAAA-BBBB-CCCC-DDDD-DDDDDDDDDD";
        public const string TestEndPoint = "http://testendpoint/v2/track";
        public const string TestEndPointEnv = "http://testendpointend/v2/track";
        public ExtensionsTests(ITestOutputHelper output)
        {
            this.output = output;
            this.output.WriteLine("Initialized");
        }

        private static ServiceCollection CreateServicesAndAddApplicationinsightsWorker(Action<ApplicationInsightsServiceOptions> serviceOptions = null)
        {
            var services = new ServiceCollection();
            services.AddApplicationInsightsTelemetryWorkerService();
            if (serviceOptions != null)
            {
                services.Configure(serviceOptions);
            }
            return services;
        }

        [Theory]
        [InlineData(typeof(ITelemetryInitializer), typeof(ApplicationInsights.WorkerService.TelemetryInitializers.DomainNameRoleInstanceTelemetryInitializer), ServiceLifetime.Singleton)]
        [InlineData(typeof(ITelemetryInitializer), typeof(AzureWebAppRoleEnvironmentTelemetryInitializer), ServiceLifetime.Singleton)]
        [InlineData(typeof(ITelemetryInitializer), typeof(ComponentVersionTelemetryInitializer), ServiceLifetime.Singleton)]
        [InlineData(typeof(ITelemetryInitializer), typeof(HttpDependenciesParsingTelemetryInitializer), ServiceLifetime.Singleton)]
        [InlineData(typeof(TelemetryConfiguration), null, ServiceLifetime.Singleton)]
        [InlineData(typeof(TelemetryClient), typeof(TelemetryClient), ServiceLifetime.Singleton)]
        [InlineData(typeof(ITelemetryChannel), typeof(ServerTelemetryChannel), ServiceLifetime.Singleton)]
        public void RegistersExpectedServices(Type serviceType, Type implementationType, ServiceLifetime lifecycle)
        {
            var services = CreateServicesAndAddApplicationinsightsWorker(null);
            ServiceDescriptor service = services.Single(s => s.ServiceType == serviceType && s.ImplementationType == implementationType);
            Assert.Equal(lifecycle, service.Lifetime);
        }

        [Theory]
        [InlineData(typeof(ITelemetryInitializer), typeof(ApplicationInsights.WorkerService.TelemetryInitializers.DomainNameRoleInstanceTelemetryInitializer), ServiceLifetime.Singleton)]
        [InlineData(typeof(ITelemetryInitializer), typeof(AzureWebAppRoleEnvironmentTelemetryInitializer), ServiceLifetime.Singleton)]
        [InlineData(typeof(ITelemetryInitializer), typeof(ComponentVersionTelemetryInitializer), ServiceLifetime.Singleton)]
        [InlineData(typeof(ITelemetryInitializer), typeof(HttpDependenciesParsingTelemetryInitializer), ServiceLifetime.Singleton)]
        [InlineData(typeof(TelemetryConfiguration), null, ServiceLifetime.Singleton)]
        [InlineData(typeof(TelemetryClient), typeof(TelemetryClient), ServiceLifetime.Singleton)]
        [InlineData(typeof(ITelemetryChannel), typeof(ServerTelemetryChannel), ServiceLifetime.Singleton)]
        public void RegistersExpectedServicesOnlyOnce(Type serviceType, Type implementationType, ServiceLifetime lifecycle)
        {
            var services = CreateServicesAndAddApplicationinsightsWorker(null);
            services.AddApplicationInsightsTelemetryWorkerService();
            ServiceDescriptor service = services.Single(s => s.ServiceType == serviceType && s.ImplementationType == implementationType);
            Assert.Equal(lifecycle, service.Lifetime);
        }

        [Fact]
        public void DoesNotThrowWithoutInstrumentationKey()
        {
            var services = CreateServicesAndAddApplicationinsightsWorker(null);
        }

        [Fact]
        public void ReadsSettingsFromSuppliedConfiguration()
        {
            var jsonFullPath = Path.Combine(Directory.GetCurrentDirectory(), "content", "sample-appsettings.json");

            this.output.WriteLine("json:" + jsonFullPath);
            var config = new ConfigurationBuilder().AddJsonFile(jsonFullPath).Build();
            var services = new ServiceCollection();
            
            services.AddApplicationInsightsTelemetryWorkerService(config);
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
            Assert.Equal(TestInstrumentationKey, telemetryConfiguration.InstrumentationKey);
            Assert.Equal(TestEndPoint, telemetryConfiguration.TelemetryChannel.EndpointAddress);
            Assert.Equal(true, telemetryConfiguration.TelemetryChannel.DeveloperMode);
        }

        [Fact]
        public void ReadsSettingsFromDefaultConfiguration()
        {
            // Host.CreateDefaultBuilder() in .NET Core 3.0  adds appsetting.json and env variable
            // to configuration and is made available for constructor injection.
            // this test validates that SDK reads settings from this configuration by default.
            // ARRANGE
            var jsonFullPath = Path.Combine(Directory.GetCurrentDirectory(), "content", "sample-appsettings.json");
            this.output.WriteLine("json:" + jsonFullPath);
            var config = new ConfigurationBuilder().AddJsonFile(jsonFullPath).Build();
            var services = new ServiceCollection();
            // This line mimics the default behavior by CreateDefaultBuilder
            services.AddSingleton<IConfiguration>(config);

            // ACT             
            services.AddApplicationInsightsTelemetryWorkerService();
            
            // VALIDATE
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
            Assert.Equal(TestInstrumentationKey, telemetryConfiguration.InstrumentationKey);
            Assert.Equal(TestEndPoint, telemetryConfiguration.TelemetryChannel.EndpointAddress);
            Assert.Equal(true, telemetryConfiguration.TelemetryChannel.DeveloperMode);
        }

        /// <summary>
        /// Tests that the connection string can be read from a JSON file by the configuration factory.            
        /// </summary>
        [Fact]
        [Trait("Trait", "ConnectionString")]
        public void ReadsConnectionStringFromConfiguration()
        {
            var jsonFullPath = Path.Combine(Directory.GetCurrentDirectory(), "content", "config-connection-string.json");

            this.output.WriteLine("json:" + jsonFullPath);
            var config = new ConfigurationBuilder().AddJsonFile(jsonFullPath).Build();
            var services = new ServiceCollection();

            services.AddApplicationInsightsTelemetryWorkerService(config);
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
            Assert.Equal(TestConnectionString, telemetryConfiguration.ConnectionString);
            Assert.Equal(TestInstrumentationKey, telemetryConfiguration.InstrumentationKey);
            Assert.Equal("http://127.0.0.1/", telemetryConfiguration.EndpointContainer.Ingestion.AbsoluteUri);
        }

        [Fact]
        public void ReadsSettingsFromDefaultConfigurationWithEnvOverridingConfig()
        {
            // Host.CreateDefaultBuilder() in .NET Core 3.0  adds appsetting.json and env variable
            // to configuration and is made available for constructor injection.
            // this test validates that SDK reads settings from this configuration by default
            // and gives priority to the ENV variables than the one from config.
            
            // ARRANGE
            Environment.SetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", TestInstrumentationKeyEnv);
            Environment.SetEnvironmentVariable("APPINSIGHTS_ENDPOINTADDRESS", TestEndPointEnv);
            try
            {
                var jsonFullPath = Path.Combine(Directory.GetCurrentDirectory(), "content", "sample-appsettings.json");
                this.output.WriteLine("json:" + jsonFullPath);

                // This config will have ikey,endpoint from json and env. ENV one is expected to win.
                var config = new ConfigurationBuilder().AddJsonFile(jsonFullPath).AddEnvironmentVariables().Build();
                var services = new ServiceCollection();

                // This line mimics the default behavior by CreateDefaultBuilder
                services.AddSingleton<IConfiguration>(config);

                // ACT             
                services.AddApplicationInsightsTelemetryWorkerService();

                // VALIDATE
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                Assert.Equal(TestInstrumentationKeyEnv, telemetryConfiguration.InstrumentationKey);
                Assert.Equal(TestEndPointEnv, telemetryConfiguration.TelemetryChannel.EndpointAddress);
            }
            finally
            {
                Environment.SetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", null);
                Environment.SetEnvironmentVariable("APPINSIGHTS_ENDPOINTADDRESS", null);
            }
        }

        [Fact]
        public void VerifiesIkeyProvidedInAddApplicationInsightsAlwaysWinsOverOtherOptions()
        {
            // ARRANGE
            Environment.SetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", TestInstrumentationKeyEnv);
            try
            {
                var jsonFullPath = Path.Combine(Directory.GetCurrentDirectory(), "content", "sample-appsettings.json");
                this.output.WriteLine("json:" + jsonFullPath);

                // This config will have ikey,endpoint from json and env. But the one
                // user explicitly provider is expected to win.
                var config = new ConfigurationBuilder().AddJsonFile(jsonFullPath).AddEnvironmentVariables().Build();
                var services = new ServiceCollection();

                // This line mimics the default behavior by CreateDefaultBuilder
                services.AddSingleton<IConfiguration>(config);

                // ACT             
                services.AddApplicationInsightsTelemetryWorkerService("userkey");

                // VALIDATE
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                Assert.Equal("userkey", telemetryConfiguration.InstrumentationKey);
            }
            finally
            {
                Environment.SetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", null);
            }
        }

        [Fact]
        public void DoesNoThrowIfNoSettingsFound()
        {
            // Host.CreateDefaultBuilder() in .NET Core 3.0  adds appsetting.json and env variable
            // to configuration and is made available for constructor injection.
            // This test validates that SDK does not throw any error if it cannot find 
            // application insights configuration in default IConfiguration.
            // ARRANGE
            var jsonFullPath = Path.Combine(Directory.GetCurrentDirectory(), "content", "sample-appsettings_dontexist.json");
            this.output.WriteLine("json:" + jsonFullPath);
            var config = new ConfigurationBuilder().AddJsonFile(jsonFullPath, true).Build();
            var services = new ServiceCollection();
            // This line mimics the default behavior by CreateDefaultBuilder
            services.AddSingleton<IConfiguration>(config);

            // ACT             
            services.AddApplicationInsightsTelemetryWorkerService();

            // VALIDATE
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
            Assert.True(string.IsNullOrEmpty(telemetryConfiguration.InstrumentationKey));
        }

        [Fact]
        public void VerifyAddAIWorkerServiceSetsUpDefaultConfigurationAndModules()
        {
            var services = new ServiceCollection();

            // ACT                         
            services.AddApplicationInsightsTelemetryWorkerService("ikey");

            // VALIDATE
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
            Assert.Equal("ikey", telemetryConfiguration.InstrumentationKey);

            // AppID
            var appIdProvider = serviceProvider.GetRequiredService<IApplicationIdProvider>();
            Assert.NotNull(appIdProvider);
            Assert.True(appIdProvider is ApplicationInsightsApplicationIdProvider);

            // AppID
            var channel = serviceProvider.GetRequiredService<ITelemetryChannel>();
            Assert.NotNull(channel);            
            Assert.True(channel is ServerTelemetryChannel);

            // TelemetryModules
            var modules = serviceProvider.GetServices<ITelemetryModule>();
            Assert.NotNull(modules);
            Assert.Equal(6, modules.Count());

            var perfCounterModule = modules.FirstOrDefault<ITelemetryModule>(t => t.GetType() == typeof(PerformanceCollectorModule));
            Assert.NotNull(perfCounterModule);

            var qpModule = modules.FirstOrDefault<ITelemetryModule>(t => t.GetType() == typeof(QuickPulseTelemetryModule));
            Assert.NotNull(qpModule);

            var evtCounterModule = modules.FirstOrDefault<ITelemetryModule>(t => t.GetType() == typeof(EventCounterCollectionModule));
            Assert.NotNull(evtCounterModule);

            var depModule = modules.FirstOrDefault<ITelemetryModule>(t => t.GetType() == typeof(DependencyTrackingTelemetryModule));
            Assert.NotNull(depModule);

            var hbModule = modules.FirstOrDefault<ITelemetryModule>(t => t.GetType() == typeof(AppServicesHeartbeatTelemetryModule));
            Assert.NotNull(hbModule);

            var azMetadataModule = modules.FirstOrDefault<ITelemetryModule>(t => t.GetType() == typeof(AzureInstanceMetadataTelemetryModule));
            Assert.NotNull(azMetadataModule);

            // TelemetryProcessors
            Assert.Contains(telemetryConfiguration.DefaultTelemetrySink.TelemetryProcessors, proc => proc.GetType().Name.Contains("AutocollectedMetricsExtractor"));
            Assert.Contains(telemetryConfiguration.DefaultTelemetrySink.TelemetryProcessors, proc => proc.GetType().Name.Contains("AdaptiveSamplingTelemetryProcessor"));
            Assert.Contains(telemetryConfiguration.DefaultTelemetrySink.TelemetryProcessors, proc => proc.GetType().Name.Contains("QuickPulseTelemetryProcessor"));

            // TelemetryInitializers
            // 4 added by WorkerService SDK, 1 from Base Sdk
            Assert.Equal(5, telemetryConfiguration.TelemetryInitializers.Count);
            Assert.Contains(telemetryConfiguration.TelemetryInitializers, initializer => initializer.GetType().Name.Contains("DomainNameRoleInstanceTelemetryInitializer"));
            Assert.Contains(telemetryConfiguration.TelemetryInitializers, initializer => initializer.GetType().Name.Contains("AzureWebAppRoleEnvironmentTelemetryInitializer"));
            Assert.Contains(telemetryConfiguration.TelemetryInitializers, initializer => initializer.GetType().Name.Contains("ComponentVersionTelemetryInitializer"));
            Assert.Contains(telemetryConfiguration.TelemetryInitializers, initializer => initializer.GetType().Name.Contains("HttpDependenciesParsingTelemetryInitializer"));
            Assert.Contains(telemetryConfiguration.TelemetryInitializers, initializer => initializer.GetType().Name.Contains("OperationCorrelationTelemetryInitializer"));

            // TelemetryClient
            var tc = serviceProvider.GetRequiredService<TelemetryClient>();
            Assert.NotNull(tc);
        }

        [Fact]
        public static void RegistersTelemetryConfigurationFactoryMethodThatPopulatesEventCounterCollectorWithDefaultListOfCounters()
        {
            //ARRANGE
            var services = new ServiceCollection();
            services.AddApplicationInsightsTelemetryWorkerService();

            //ACT
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var modules = serviceProvider.GetServices<ITelemetryModule>();            
            var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
            var eventCounterModule = modules.OfType<EventCounterCollectionModule>().Single();

            //VALIDATE
            Assert.Equal(19, eventCounterModule.Counters.Count);

            // sanity check with a sample counter.
            var cpuCounterRequest = eventCounterModule.Counters.FirstOrDefault<EventCounterCollectionRequest>(
                eventCounterCollectionRequest => eventCounterCollectionRequest.EventSourceName == "System.Runtime"
                && eventCounterCollectionRequest.EventCounterName == "cpu-usage");
            Assert.NotNull(cpuCounterRequest);

            // sanity check - no asp.net counters should be added
            var aspnetCounterRequest = eventCounterModule.Counters.FirstOrDefault<EventCounterCollectionRequest>(
                eventCounterCollectionRequest => eventCounterCollectionRequest.EventSourceName == "Microsoft.AspNetCore.Hosting");
            Assert.Null(aspnetCounterRequest);
        }

        [Fact]
        public void VerifyAddAIWorkerServiceUsesTelemetryInitializerAddedToDI()
        {
            var services = new ServiceCollection();
            var telemetryInitializer = new FakeTelemetryInitializer();

            // ACT                         
            services.AddApplicationInsightsTelemetryWorkerService();
            services.AddSingleton<ITelemetryInitializer>(telemetryInitializer);


            // VALIDATE
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
            Assert.Contains(telemetryInitializer, telemetryConfiguration.TelemetryInitializers);
        }

        [Fact]
        public void VerifyAddAIWorkerServiceUsesTelemetryChannelAddedToDI()
        {
            var services = new ServiceCollection();
            var telChannel = new ServerTelemetryChannel() {StorageFolder = "c:\\mycustom" };

            // ACT                         
            services.AddApplicationInsightsTelemetryWorkerService("ikey");
            services.AddSingleton<ITelemetryChannel>(telChannel);

            // VALIDATE
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
            Assert.Equal(telChannel, telemetryConfiguration.TelemetryChannel);
            Assert.Equal("c:\\mycustom", ((ServerTelemetryChannel) telemetryConfiguration.TelemetryChannel).StorageFolder);

        }


        [Fact]
        public void VerifyAddAIWorkerServiceRespectsAIOptions()
        {
            var services = new ServiceCollection();

            // ACT             
            var aiOptions = new ApplicationInsightsServiceOptions();
            aiOptions.AddAutoCollectedMetricExtractor = false;
            aiOptions.EnableAdaptiveSampling = false;
            aiOptions.EnableQuickPulseMetricStream = false;
            aiOptions.InstrumentationKey = "keyfromaioption";
            services.AddApplicationInsightsTelemetryWorkerService(aiOptions);

            // VALIDATE
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
            Assert.Equal("keyfromaioption", telemetryConfiguration.InstrumentationKey);
            Assert.DoesNotContain(telemetryConfiguration.DefaultTelemetrySink.TelemetryProcessors, proc => proc.GetType().Name.Contains("AutocollectedMetricsExtractor"));
            Assert.DoesNotContain(telemetryConfiguration.DefaultTelemetrySink.TelemetryProcessors, proc => proc.GetType().Name.Contains("AdaptiveSamplingTelemetryProcessor"));
            Assert.DoesNotContain(telemetryConfiguration.DefaultTelemetrySink.TelemetryProcessors, proc => proc.GetType().Name.Contains("QuickPulseTelemetryProcessor"));
        }

        /// <summary>
        /// Sanity check to validate that node name and roleinstance are populated
        /// </summary>
        [Fact]
        public static void SanityCheckRoleInstance()
        {
            // ARRANGE
            string expected = Environment.MachineName;
            var services = new ServiceCollection();
            services.AddApplicationInsightsTelemetryWorkerService();
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            // Request TC from DI which would be made with the default TelemetryConfiguration which should 
            // contain the telemetry initializer capable of populate node name and role instance name.
            var tc = serviceProvider.GetRequiredService<TelemetryClient>();
            var mockItem = new EventTelemetry();

            // ACT
            // This is expected to run all TI and populate the node name and role instance.
            tc.Initialize(mockItem);

            // VERIFY                
            Assert.Contains(expected, mockItem.Context.Cloud.RoleInstance, StringComparison.CurrentCultureIgnoreCase);
        }
    }

    internal class FakeTelemetryInitializer : ITelemetryInitializer
    {
        public FakeTelemetryInitializer()
        {
        }

        public void Initialize(ITelemetry telemetry)
        {
            
        }
    }
}
