using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Xunit;

#if AI_ASPNETCORE_WEB
namespace Microsoft.ApplicationInsights.AspNetCore.Tests
{
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
#else
namespace Microsoft.ApplicationInsights.WorkerService.Tests
{
    using Microsoft.ApplicationInsights.WorkerService;
#endif

    public class ActivityFilterProcessorTests : IDisposable
    {
        private const string TestActivitySourceName = "Microsoft.ApplicationInsights.FilterProcessor.Tests";
        private static readonly ActivitySource TestActivitySource = new ActivitySource(TestActivitySourceName);
        
        private TracerProvider tracerProvider;

        [Fact]
        public void ActivityFilterProcessor_IsRegisteredInDI()
        {
            // ARRANGE
            var services = new ServiceCollection();

#if AI_ASPNETCORE_WEB
            services.AddApplicationInsightsTelemetry();
#else
            services.AddApplicationInsightsTelemetryWorkerService();
#endif

            // ACT
            var serviceProvider = services.BuildServiceProvider();
            var processor = serviceProvider.GetService<ActivityFilterProcessor>();

            // ASSERT
            Assert.NotNull(processor);
        }

        [Fact]
        public void WhenDependencyTrackingDisabled_FiltersClientActivities()
        {
            // ARRANGE
            var options = new ApplicationInsightsServiceOptions
            {
                EnableDependencyTrackingTelemetryModule = false
            };
            var processor = new ActivityFilterProcessor(Options.Create(options));
            this.SetupTracerProvider(processor);

            // ACT
            Activity activity;
            using (activity = TestActivitySource.StartActivity("TestActivity", ActivityKind.Client))
            {
                Assert.NotNull(activity);
            }

            // ASSERT
            Assert.False(activity.IsAllDataRequested, "Client activity should be filtered when EnableDependencyTrackingTelemetryModule is false");
        }

        [Fact]
        public void WhenDependencyTrackingDisabled_FiltersInternalActivities()
        {
            // ARRANGE
            var options = new ApplicationInsightsServiceOptions
            {
                EnableDependencyTrackingTelemetryModule = false
            };
            var processor = new ActivityFilterProcessor(Options.Create(options));
            this.SetupTracerProvider(processor);

            // ACT
            Activity activity;
            using (activity = TestActivitySource.StartActivity("TestActivity", ActivityKind.Internal))
            {
                Assert.NotNull(activity);
            }

            // ASSERT
            Assert.False(activity.IsAllDataRequested, "Internal activity should be filtered when EnableDependencyTrackingTelemetryModule is false");
        }

        [Fact]
        public void WhenDependencyTrackingDisabled_FiltersProducerActivities()
        {
            // ARRANGE
            var options = new ApplicationInsightsServiceOptions
            {
                EnableDependencyTrackingTelemetryModule = false
            };
            var processor = new ActivityFilterProcessor(Options.Create(options));
            this.SetupTracerProvider(processor);

            // ACT
            Activity activity;
            using (activity = TestActivitySource.StartActivity("TestActivity", ActivityKind.Producer))
            {
                Assert.NotNull(activity);
            }

            // ASSERT
            Assert.False(activity.IsAllDataRequested, "Producer activity should be filtered when EnableDependencyTrackingTelemetryModule is false");
        }

#if AI_ASPNETCORE_WEB
        [Fact]
        public void WhenRequestTrackingDisabled_FiltersServerActivities()
        {
            // ARRANGE
            var options = new ApplicationInsightsServiceOptions
            {
                EnableRequestTrackingTelemetryModule = false
            };
            var processor = new ActivityFilterProcessor(Options.Create(options));
            this.SetupTracerProvider(processor);

            // ACT
            Activity activity;
            using (activity = TestActivitySource.StartActivity("TestActivity", ActivityKind.Server))
            {
                Assert.NotNull(activity);
            }

            // ASSERT
            Assert.False(activity.IsAllDataRequested, "Server activity should be filtered when EnableRequestTrackingTelemetryModule is false");
        }

        [Fact]
        public void WhenRequestTrackingDisabled_FiltersConsumerActivities()
        {
            // ARRANGE
            var options = new ApplicationInsightsServiceOptions
            {
                EnableRequestTrackingTelemetryModule = false
            };
            var processor = new ActivityFilterProcessor(Options.Create(options));
            this.SetupTracerProvider(processor);

            // ACT
            Activity activity;
            using (activity = TestActivitySource.StartActivity("TestActivity", ActivityKind.Consumer))
            {
                Assert.NotNull(activity);
            }

            // ASSERT
            Assert.False(activity.IsAllDataRequested, "Consumer activity should be filtered when EnableRequestTrackingTelemetryModule is false");
        }
#endif

        [Fact]
        public void WhenDependencyTrackingEnabled_DoesNotFilterClientActivities()
        {
            // ARRANGE
            var options = new ApplicationInsightsServiceOptions
            {
                EnableDependencyTrackingTelemetryModule = true
            };
            var processor = new ActivityFilterProcessor(Options.Create(options));
            this.SetupTracerProvider(processor);

            // ACT
            Activity activity;
            using (activity = TestActivitySource.StartActivity("TestActivity", ActivityKind.Client))
            {
                Assert.NotNull(activity);
            }

            // ASSERT
            Assert.True(activity.IsAllDataRequested, "Client activity should NOT be filtered when EnableDependencyTrackingTelemetryModule is true");
        }

#if AI_ASPNETCORE_WEB
        [Fact]
        public void WhenRequestTrackingEnabled_DoesNotFilterServerActivities()
        {
            // ARRANGE
            var options = new ApplicationInsightsServiceOptions
            {
                EnableRequestTrackingTelemetryModule = true
            };
            var processor = new ActivityFilterProcessor(Options.Create(options));
            this.SetupTracerProvider(processor);

            // ACT
            Activity activity;
            using (activity = TestActivitySource.StartActivity("TestActivity", ActivityKind.Server))
            {
                Assert.NotNull(activity);
            }

            // ASSERT
            Assert.True(activity.IsAllDataRequested, "Server activity should NOT be filtered when EnableRequestTrackingTelemetryModule is true");
        }
#endif

        [Fact]
        public void OnStart_HandlesNullActivity()
        {
            // ARRANGE
            var options = new ApplicationInsightsServiceOptions
            {
                EnableDependencyTrackingTelemetryModule = false
            };
            var processor = new ActivityFilterProcessor(Options.Create(options));

            // ACT & ASSERT - Should not throw
            processor.OnStart(null);
        }

        [Fact]
        public void WhenDependencyTrackingDisabled_DoesNotFilterServerActivities()
        {
            // ARRANGE
            var options = new ApplicationInsightsServiceOptions
            {
                EnableDependencyTrackingTelemetryModule = false
            };
            var processor = new ActivityFilterProcessor(Options.Create(options));
            this.SetupTracerProvider(processor);

            // ACT
            Activity activity;
            using (activity = TestActivitySource.StartActivity("TestActivity", ActivityKind.Server))
            {
                Assert.NotNull(activity);
            }

            // ASSERT
            Assert.True(activity.IsAllDataRequested, "Server activity should NOT be filtered when only dependency tracking is disabled");
        }

#if AI_ASPNETCORE_WEB
        [Fact]
        public void WhenRequestTrackingDisabled_DoesNotFilterClientActivities()
        {
            // ARRANGE
            var options = new ApplicationInsightsServiceOptions
            {
                EnableRequestTrackingTelemetryModule = false
            };
            var processor = new ActivityFilterProcessor(Options.Create(options));
            this.SetupTracerProvider(processor);

            // ACT
            Activity activity;
            using (activity = TestActivitySource.StartActivity("TestActivity", ActivityKind.Client))
            {
                Assert.NotNull(activity);
            }

            // ASSERT
            Assert.True(activity.IsAllDataRequested, "Client activity should NOT be filtered when only request tracking is disabled");
        }
#endif

        private void SetupTracerProvider(BaseProcessor<Activity> processor)
        {
            // Dispose existing provider if any
            this.tracerProvider?.Dispose();
            
            this.tracerProvider = Sdk.CreateTracerProviderBuilder()
                .AddSource(TestActivitySourceName)
                .AddProcessor(processor)
                .Build();
        }

        public void Dispose()
        {
            this.tracerProvider?.Dispose();
        }
    }
}
