namespace Microsoft.ApplicationInsights.AspNet.Tests
{
    using Microsoft.ApplicationInsights.AspNet.Tests.Helpers;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNet.Hosting;
    using Microsoft.Framework.ConfigurationModel;
    using Microsoft.Framework.DependencyInjection;
    using Microsoft.Framework.DependencyInjection.Fallback;
    using System;
    using System.IO;
    using System.Linq;
    using Xunit;
    using Microsoft.ApplicationInsights.AspNet.ContextInitializers;
    using Microsoft.ApplicationInsights.AspNet.TelemetryInitializers;
    using System.Collections.Generic;

    public class ApplicationInsightsExtensionsTests
    {
        public static class AddApplicationInsightsTelemetry
        {
            [Theory]
            [InlineData(typeof(IContextInitializer), typeof(DomainNameRoleInstanceContextInitializer), LifecycleKind.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(ClientIpHeaderTelemetryInitializer), LifecycleKind.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(OperationNameTelemetryInitializer), LifecycleKind.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(OperationIdTelemetryInitializer), LifecycleKind.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(UserAgentTelemetryInitializer), LifecycleKind.Singleton)]
            [InlineData(typeof(TelemetryConfiguration), null, LifecycleKind.Singleton)]
            [InlineData(typeof(TelemetryClient), typeof(TelemetryClient), LifecycleKind.Scoped)]
            public static void RegistersExpectedServices(Type serviceType, Type implementationType, LifecycleKind lifecycle)
            {
                var services = new ServiceCollection();

                services.AddApplicationInsightsTelemetry(new Configuration());

                IServiceDescriptor service = services.Single(s => s.ServiceType == serviceType && s.ImplementationType == implementationType);
                Assert.Equal(lifecycle, service.Lifecycle);
            }

            [Fact]
            public static void DoesNotThrowWithoutInstrumentationKey()
            {
                var services = new ServiceCollection();

                //Empty configuration that doesn't have instrumentation key
                IConfiguration config = new Configuration();

                services.AddApplicationInsightsTelemetry(config);
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatCreatesDefaultInstance()
            {
                IServiceCollection services = HostingServices.Create();

                services.AddApplicationInsightsTelemetry(new Configuration());

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                Assert.Contains(telemetryConfiguration.TelemetryInitializers, t => t is TimestampPropertyInitializer);
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatReadsInstrumentationKeyFromConfiguration()
            {
                IServiceCollection services = HostingServices.Create();
                var config = new Configuration().AddJsonFile("content\\config.json");

                services.AddApplicationInsightsTelemetry(config);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                Assert.Equal("11111111-2222-3333-4444-555555555555", telemetryConfiguration.InstrumentationKey);
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatPopulatesItWithContextInitializersFromContainer()
            {
                var contextInitializer = new FakeContextInitializer();
                IServiceCollection services = HostingServices.Create();
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
                IServiceCollection services = HostingServices.Create();
                services.AddInstance<ITelemetryInitializer>(telemetryInitializer);

                services.AddApplicationInsightsTelemetry(new Configuration());

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                Assert.Contains(telemetryInitializer, telemetryConfiguration.TelemetryInitializers);
            }

            [Fact]
            public static void RegistersTelemetryClientToGetTelemetryConfigurationFromContainerAndNotGlobalInstance()
            {
                IServiceCollection services = HostingServices.Create();

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