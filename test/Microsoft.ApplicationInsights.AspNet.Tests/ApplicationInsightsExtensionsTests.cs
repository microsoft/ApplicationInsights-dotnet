namespace Microsoft.ApplicationInsights.AspNet.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using Microsoft.ApplicationInsights.AspNet.ContextInitializers;
    using Microsoft.ApplicationInsights.AspNet.TelemetryInitializers;
    using Microsoft.ApplicationInsights.AspNet.Tests.Helpers;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNet.Builder;
    using Microsoft.AspNet.Hosting;
    using Microsoft.Framework.ConfigurationModel;
    using Microsoft.Framework.DependencyInjection;
    using Xunit;

    public static class ApplicationInsightsExtensionsTests
    {
        public static ServiceCollection GetServiceCollectionWithContextAccessor()
        {
            var services = new ServiceCollection();
            IHttpContextAccessor contextAccessor = new HttpContextAccessor();
            services.AddInstance<IHttpContextAccessor>(contextAccessor);
            return services;
        }

        public static class SetApplicationInsightsTelemetryDeveloperMode
        {
            [Fact]
            public static void ChangesDeveloperModeOfTelemetryChannelInTelemetryConfigurationInContainerToTrue()
            {
                var telemetryChannel = new FakeTelemetryChannel();
                var telemetryConfiguration = new TelemetryConfiguration { TelemetryChannel = telemetryChannel };
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                services.AddInstance(telemetryConfiguration);
                var app = new ApplicationBuilder(services.BuildServiceProvider());

                app.SetApplicationInsightsTelemetryDeveloperMode();

                Assert.True(telemetryChannel.DeveloperMode);
            }
        }

        public static class AddApplicationInsightsTelemetry
        {
            [Theory]
            [InlineData(typeof(IContextInitializer), typeof(DomainNameRoleInstanceContextInitializer), ServiceLifetime.Singleton)]
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

                services.AddApplicationInsightsTelemetry(new Configuration());

                ServiceDescriptor service = services.Single(s => s.ServiceType == serviceType && s.ImplementationType == implementationType);
                Assert.Equal(lifecycle, service.Lifetime);
            }

            [Fact]
            public static void DoesNotThrowWithoutInstrumentationKey()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();

                //Empty configuration that doesn't have instrumentation key
                IConfiguration config = new Configuration();

                services.AddApplicationInsightsTelemetry(config);
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatCreatesDefaultInstance()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();

                services.AddApplicationInsightsTelemetry(new Configuration());

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                Assert.Contains(telemetryConfiguration.TelemetryInitializers, t => t is TimestampPropertyInitializer);
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatReadsInstrumentationKeyFromConfiguration()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                var config = new Configuration(".").AddJsonFile("content\\config.json");

                services.AddApplicationInsightsTelemetry(config);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                Assert.Equal("11111111-2222-3333-4444-555555555555", telemetryConfiguration.InstrumentationKey);
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatPopulatesItWithContextInitializersFromContainer()
            {
                var contextInitializer = new FakeContextInitializer();
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                services.AddInstance<IContextInitializer>(contextInitializer);

                services.AddApplicationInsightsTelemetry(new Configuration());

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                Assert.Contains(contextInitializer, telemetryConfiguration.ContextInitializers);
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatPopulatesItWithTelemetryInitializersFromContainer()
            {
                var telemetryInitializer = new FakeTelemetryInitializer();
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                services.AddInstance<ITelemetryInitializer>(telemetryInitializer);

                services.AddApplicationInsightsTelemetry(new Configuration());

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

                services.AddApplicationInsightsTelemetry(new Configuration());

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                Assert.Same(telemetryChannel, telemetryConfiguration.TelemetryChannel);
            }

            [Fact]
            public static void DoesNotOverrideDefaultTelemetryChannelIfTelemetryChannelServiceIsNotRegistered()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();

                services.AddApplicationInsightsTelemetry(new Configuration());

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                Assert.NotNull(telemetryConfiguration.TelemetryChannel);
            }

            [Fact]
            public static void RegistersTelemetryClientToGetTelemetryConfigurationFromContainerAndNotGlobalInstance()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();

                services.AddApplicationInsightsTelemetry(new Configuration());

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var configuration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                configuration.InstrumentationKey = Guid.NewGuid().ToString();
                var telemetryClient = serviceProvider.GetRequiredService<TelemetryClient>();
                Assert.Equal(configuration.InstrumentationKey, telemetryClient.Context.InstrumentationKey);
            }
        }

        public static class ApplicationInsightsJavaScriptSnippet
        {
            [Fact]
            public static void DoesNotThrowWithoutInstrumentationKey()
            {
                var helper = new HtmlHelperMock();
                helper.ApplicationInsightsJavaScriptSnippet(null);
                helper.ApplicationInsightsJavaScriptSnippet("");
            }

            [Fact]
            public static void UsesInstrumentationKey()
            {
                var key = "1236543";
                HtmlHelperMock helper = new HtmlHelperMock();
                var result = helper.ApplicationInsightsJavaScriptSnippet(key);
                using (StringWriter sw = new StringWriter())
                {
                    result.WriteTo(sw);
                    Assert.Contains(key, sw.ToString());
                }
            }
        }
    }
}