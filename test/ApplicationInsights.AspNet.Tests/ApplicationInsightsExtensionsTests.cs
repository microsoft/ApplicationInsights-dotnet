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
        [Fact]
        public void AddTelemetryRegistersTelemetryConfigurationServiceWithSingletonLifecycleToBeSharedByAllTelemetryClientInstances()
        {
            var services = new ServiceCollection();

            services.AddApplicationInsightsTelemetry(new Configuration());

            IServiceDescriptor service = services.Single(s => s.ServiceType == typeof(TelemetryConfiguration));
            Assert.Equal(LifecycleKind.Singleton, service.Lifecycle);
        }

        [Fact]
        public void AddTelemetryWillNotThrowWithoutInstrumentationKey()
        {
            var services = new ServiceCollection();

            //Empty configuration that doesn't have instrumentation key
            IConfiguration config = new Configuration();

            services.AddApplicationInsightsTelemetry(config);
        }

        [Fact]
        public void AddTelemetryRegistersTelemetryConfigurationFactoryMethodThatReadsInstrumentationKeyFromConfiguration()
        {
            var services = new ServiceCollection();
            var config = new Configuration().AddJsonFile("content\\config.json");

            services.AddApplicationInsightsTelemetry(config);

            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
            Assert.Equal("11111111-2222-3333-4444-555555555555", telemetryConfiguration.InstrumentationKey);
        }

        [Fact]
        public void AddTelemetryRegistersTelemetryConfigurationFactoryMethodThatPopulatesItWithContextInitializersFromContainer()
        {
            var contextInitializer = new FakeContextInitializer();
            var services = new ServiceCollection();
            services.AddInstance<IContextInitializer>(contextInitializer);

            services.AddApplicationInsightsTelemetry(new Configuration());

            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
            Assert.Contains(contextInitializer, telemetryConfiguration.ContextInitializers);
        }

        [Fact]
        public void AddTelemetryRegistersTelemetryConfigurationFactoryMethodThatPopulatesItWithTelemetryInitializersFromContainer()
        {
            var telemetryInitializer = new FakeTelemetryInitializer();
            var services = new ServiceCollection();
            services.AddInstance<ITelemetryInitializer>(telemetryInitializer);

            services.AddApplicationInsightsTelemetry(new Configuration());

            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
            Assert.Contains(telemetryInitializer, telemetryConfiguration.TelemetryInitializers);
        }

        [Fact]
        public void AddTelemetryRegistersTelemetryClientServiceWithScopedLifecycleToPreventSharingOfRequestSpecificProperties()
        {
            var services = new ServiceCollection();

            services.AddApplicationInsightsTelemetry(new Configuration());

            var service = services.Single(s => s.ServiceType == typeof(TelemetryClient));
            Assert.Equal(LifecycleKind.Scoped, service.Lifecycle);
        }

        [Fact]
        public void AddTelemetryRegistersTelemetryClientToGetTelemetryConfigurationFromContainerAndNotGlobalInstance()
        {
            var services = new ServiceCollection();

            services.AddApplicationInsightsTelemetry(new Configuration());

            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var configuration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
            configuration.InstrumentationKey = Guid.NewGuid().ToString();
            var telemetryClient = serviceProvider.GetRequiredService<TelemetryClient>();
            Assert.Equal(configuration.InstrumentationKey, telemetryClient.Context.InstrumentationKey);
        }

        [Fact]
        public void JSSnippetWillNotThrowWithoutInstrumentationKey()
        {
            var helper = new HtmlHelperMock();
            helper.ApplicationInsightsJavaScriptSnippet(null);
            helper.ApplicationInsightsJavaScriptSnippet("");
        }

        [Fact]
        public void JSSnippetUsesInstrumentationKey()
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