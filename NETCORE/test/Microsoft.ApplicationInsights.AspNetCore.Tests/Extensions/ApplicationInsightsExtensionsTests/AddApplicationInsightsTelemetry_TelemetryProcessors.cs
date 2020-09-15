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
        public static void VerifyCanAddInstanceOfTelemetryProcessor_AddProcessor()
        {
            // SETUP
            var services = GetServiceCollectionWithContextAccessor();

            services.AddApplicationInsightsTelemetryProcessor<MyTelemetryProcessor1>();

            // We inject some of our own TelemetryProcessors here and then call TelemetryProcessorChainBuilder.Build().
            services.AddApplicationInsightsTelemetry();

            // ACT
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();

            // ASSERT
            var telemetryProcessor1 = telemetryConfiguration.DefaultTelemetrySink.TelemetryProcessors.OfType<MyTelemetryProcessor1>().FirstOrDefault();
            Assert.NotNull(telemetryProcessor1);
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

            services.AddSingleton<ITelemetryChannel, MyTelemetryChannel>();

            // Before calling AddApplicationInsightsTelemetry(), this is all that's needed.
            services.Configure<TelemetryConfiguration>(config => {
                config.DefaultTelemetrySink.TelemetryProcessorChainBuilder.Use(next => new MyTelemetryProcessor2(next, testConfig));
            });

            // We inject some of our own TelemetryProcessors here and then call TelemetryProcessorChainBuilder.Build().
            services.AddApplicationInsightsTelemetry();

            // After calling AddApplicationInsightsTelemetry(), all new processors must to call Build()
            services.Configure<TelemetryConfiguration>(config => {
                config.DefaultTelemetrySink.TelemetryProcessorChainBuilder.Use(next => new MyTelemetryProcessor3(next, testConfig));
                config.DefaultTelemetrySink.TelemetryProcessorChainBuilder.Build();
            });

            // ACT
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();

            // ASSERT
            var telemetryProcessor2 = telemetryConfiguration.DefaultTelemetrySink.TelemetryProcessors.OfType<MyTelemetryProcessor2>().FirstOrDefault();
            Assert.NotNull(telemetryProcessor2);
            Assert.Same(testConfig, telemetryProcessor2.Configuration);

            var telemetryProcessor3 = telemetryConfiguration.DefaultTelemetrySink.TelemetryProcessors.OfType<MyTelemetryProcessor3>().FirstOrDefault();
            Assert.NotNull(telemetryProcessor3);
        }

        [Fact]
        public static void VerifyCanAddInstanceOfTelemetryProcessor_UsingAddProcessor_PlusSingleton()
        {
            var testConfig = new MyTelemetryProcessorConfiguration
            {
                IntValue = 123,
                BoolValue = true
            };

            // SETUP
            var services = GetServiceCollectionWithContextAccessor();

            // If a customer's TelemetryProcessor is customizable via the constructor, this is safe.
            services.AddSingleton<MyTelemetryProcessorConfiguration>(testConfig);
            services.AddApplicationInsightsTelemetryProcessor<MyTelemetryProcessor2>();

            services.AddApplicationInsightsTelemetry();

            // ACT
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();

            // ASSERT
            var telemetryProcessor2 = telemetryConfiguration.DefaultTelemetrySink.TelemetryProcessors.OfType<MyTelemetryProcessor2>().FirstOrDefault();
            Assert.NotNull(telemetryProcessor2);
            Assert.Same(testConfig, telemetryProcessor2.Configuration);
        }

        private class MyTelemetryProcessorConfiguration
        {
            public int IntValue { get; set; }
            public bool BoolValue { get; set; }
        }

        private class MyTelemetryProcessor1 : ITelemetryProcessor
        {
            private ITelemetryProcessor Next { get; }

            public MyTelemetryProcessor1(ITelemetryProcessor next) => this.Next = next;

            public void Process(ITelemetry item)
            {
                this.Next.Process(item);
            }
        }

        private class MyTelemetryProcessor2 : ITelemetryProcessor
        {
            public MyTelemetryProcessorConfiguration Configuration { get; private set; }

            private ITelemetryProcessor Next { get; }

            public int Counter { get; private set; }

            public MyTelemetryProcessor2(ITelemetryProcessor next, MyTelemetryProcessorConfiguration configuration)
            {
                this.Configuration = configuration;
            }

            public void Process(ITelemetry item)
            {
                this.Counter++;

                this.Next.Process(item);
            }
        }

        private class MyTelemetryProcessor3 : ITelemetryProcessor
        {
            public MyTelemetryProcessorConfiguration Configuration { get; private set; }

            private ITelemetryProcessor Next { get; }

            public int Counter { get; private set; }

            public MyTelemetryProcessor3(ITelemetryProcessor next, MyTelemetryProcessorConfiguration configuration)
            {
                this.Configuration = configuration;
            }

            public void Process(ITelemetry item)
            {
                this.Counter++;

                this.Next.Process(item);
            }
        }

        private class MyTelemetryChannel : ITelemetryChannel
        {
            public bool? DeveloperMode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public string EndpointAddress { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public void Flush()
            {
                throw new NotImplementedException();
            }

            public void Send(ITelemetry item)
            {
                throw new NotImplementedException();
            }
        }
    }
}
