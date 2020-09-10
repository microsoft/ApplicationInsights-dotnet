using System;
using System.Linq;

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
#if NETCOREAPP
using Microsoft.ApplicationInsights.Extensibility.EventCounterCollector;
#endif
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Test;
using Microsoft.Extensions.Options;

using Xunit;

namespace Microsoft.ApplicationInsights.AspNetCore.Tests.Extensions.ApplicationInsightsExtensionsTests
{
    public class AddApplicationInsightsTelemetry_TelemetryProcessors : BaseTestClass
    {
        [Fact]
        public static void VerifyCanAddInstanceOfTelemetryProcessor_UsingFactory()
        {
            var testConfig = new MyTelemetryProcessorConfiguration
            {
                IntValue = 123,
                BoolValue = true
            };

            // SETUP
            var services = GetServiceCollectionWithContextAccessor();
            services.AddSingleton<ITelemetryProcessorFactory>(new MyTelemetryProcessorFactory(testConfig));
            services.AddApplicationInsightsTelemetry(new ConfigurationBuilder().Build());

            // ACT
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();

            // ASSERT
            var telemetryProcessor = telemetryConfiguration.DefaultTelemetrySink.TelemetryProcessors.OfType<MyTelemetryProcessor>().FirstOrDefault();
            Assert.NotNull(telemetryProcessor);
            Assert.Same(testConfig, telemetryProcessor.Configuration);
        }

        [Fact]
        public static void VerifyCanAddInstanceOfTelemetryProcessor_UsingTelemetryConfiguration()
        {
            var testConfig = new MyTelemetryProcessorConfiguration
            {
                IntValue = 123,
                BoolValue = true
            };

            // SETUP
            var services = GetServiceCollectionWithContextAccessor();
            services.AddApplicationInsightsTelemetry(new ConfigurationBuilder().Build());
            services.Configure<TelemetryConfiguration>(config => {
                config.DefaultTelemetrySink.TelemetryProcessorChainBuilder.Use(next => new MyTelemetryProcessor(next, testConfig));
                config.DefaultTelemetrySink.TelemetryProcessorChainBuilder.Build();
            });

            // ACT
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();

            // ASSERT
            var telemetryProcessor = telemetryConfiguration.DefaultTelemetrySink.TelemetryProcessors.OfType<MyTelemetryProcessor>().FirstOrDefault();
            Assert.NotNull(telemetryProcessor);
            Assert.Same(testConfig, telemetryProcessor.Configuration);
        }

        private class MyTelemetryProcessorConfiguration
        {
            public int IntValue { get; set; }
            public bool BoolValue { get; set; }
        }

        private class MyTelemetryProcessor : ITelemetryProcessor
        {
            public MyTelemetryProcessorConfiguration Configuration { get; private set; }

            private ITelemetryProcessor Next { get; }

            public int Counter { get; private set; }

            public MyTelemetryProcessor(ITelemetryProcessor next, MyTelemetryProcessorConfiguration configuration)
            {
                this.Configuration = configuration;
            }

            public void Process(ITelemetry item)
            {
                this.Counter++;

                this.Next.Process(item);
            }
        }

        private class MyTelemetryProcessorFactory : ITelemetryProcessorFactory
        {
            private readonly MyTelemetryProcessorConfiguration configuration;

            public MyTelemetryProcessorFactory(MyTelemetryProcessorConfiguration configuration)
            {
                this.configuration = configuration;
            }

            public ITelemetryProcessor Create(ITelemetryProcessor nextProcessor)
            {
                return new MyTelemetryProcessor(nextProcessor, configuration: configuration);
            }
        }
    }
}
