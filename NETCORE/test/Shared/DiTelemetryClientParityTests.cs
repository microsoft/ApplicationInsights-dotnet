using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
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

    public class DiTelemetryClientParityTests : IDisposable
    {
        private const string ApplicationInsightsActivitySourceName = "Microsoft.ApplicationInsights";
        private const string ServiceNameTag = "service.name";
        private const string OperationNameTag = "microsoft.operation_name";
        private const string TestConnectionString = "InstrumentationKey=11111111-2222-3333-4444-555555555555;IngestionEndpoint=http://127.0.0.1";

        private readonly List<Activity> activityItems = new();
        private readonly CaptureActivityProcessor captureProcessor;

        public DiTelemetryClientParityTests()
        {
            this.captureProcessor = new CaptureActivityProcessor(this.activityItems);
        }

        [Fact]
        public void DiResolvedTelemetryClient_AppliesItsOwnContext_NotDefault()
        {
            using var serviceProvider = this.BuildServiceProvider(defaultRoleName: "default-role");
            _ = new TelemetryClient(serviceProvider.GetRequiredService<TelemetryConfiguration>());
            var client = serviceProvider.GetRequiredService<TelemetryClient>();

            client.Context.Cloud.RoleName = "client-role";
            client.TrackDependency(CreateDependencyTelemetry("dep-client"));

            var capturedActivity = this.GetActivity("dep-client");
            var roleName = capturedActivity.GetTagItem(ServiceNameTag)?.ToString();
            Assert.Equal("client-role", roleName);
            Assert.NotEqual("default-role", roleName);
        }

        [Fact]
        public void DiResolvedTwoClients_DistinctContext_NoCrossContamination()
        {
            using var serviceProvider = this.BuildServiceProvider();
            var configuration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
            var clientA = serviceProvider.GetRequiredService<TelemetryClient>();
            var clientB = new TelemetryClient(configuration);

            clientA.Context.Cloud.RoleName = "client-a";
            clientB.Context.Cloud.RoleName = "client-b";

            clientA.TrackDependency(CreateDependencyTelemetry("dep-a"));
            clientB.TrackDependency(CreateDependencyTelemetry("dep-b"));

            Assert.Equal("client-a", this.GetActivity("dep-a").GetTagItem(ServiceNameTag)?.ToString());
            Assert.Equal("client-b", this.GetActivity("dep-b").GetTagItem(ServiceNameTag)?.ToString());
        }

        [Fact]
        public void DiPipeline_RegistersDefaultContextProcessors()
        {
            using var serviceProvider = this.BuildServiceProvider(defaultOperationName: "default-op");
            _ = new TelemetryClient(serviceProvider.GetRequiredService<TelemetryConfiguration>());
            using var activitySource = new ActivitySource(ApplicationInsightsActivitySourceName);

            using (var activity = activitySource.StartActivity("bare-activity"))
            {
                Assert.NotNull(activity);
                activity.Stop();
            }

            Assert.Equal("default-op", this.GetActivity("bare-activity").GetTagItem(OperationNameTag)?.ToString());
        }

        public void Dispose()
        {
            while (Activity.Current != null)
            {
                Activity.Current.Stop();
            }

            Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_CLOUD_ROLE_NAME", null);
            Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_CLOUD_ROLE_INSTANCE", null);
        }

        private static DependencyTelemetry CreateDependencyTelemetry(string name)
        {
            return new DependencyTelemetry
            {
                Type = "HTTP",
                Name = name,
                Target = "example.test",
                Data = "https://example.test/",
                ResultCode = "200",
                Duration = TimeSpan.FromMilliseconds(1),
                Success = true,
            };
        }

        private ServiceProvider BuildServiceProvider(string defaultRoleName = null, string defaultOperationName = null)
        {
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());

#if AI_ASPNETCORE_WEB
            services.AddApplicationInsightsTelemetry(options =>
#else
            services.AddApplicationInsightsTelemetryWorkerService(options =>
#endif
            {
                options.ConnectionString = TestConnectionString;
                options.EnableQuickPulseMetricStream = false;
                options.AddAutoCollectedMetricExtractor = false;
                options.SamplingRatio = 1.0f;
            });

            services.Configure<TelemetryConfiguration>(configuration =>
            {
                configuration.DisableOfflineStorage = true;
                configuration.SamplingRatio = 1.0f;

                var defaultContext = GetDefaultContext(configuration);
                if (!string.IsNullOrEmpty(defaultRoleName))
                {
                    defaultContext.Cloud.RoleName = defaultRoleName;
                }

                if (!string.IsNullOrEmpty(defaultOperationName))
                {
                    defaultContext.Operation.Name = defaultOperationName;
                }

                EnsureBuilderConfiguration(configuration);
                configuration.ConfigureOpenTelemetryBuilder(builder =>
                {
                    builder.WithTracing(tracing => tracing.AddProcessor(this.captureProcessor));
                });
            });

            return services.BuildServiceProvider();
        }

        private Activity GetActivity(string name)
        {
            return this.activityItems.Single(activity => activity.DisplayName == name);
        }

        private static TelemetryContext GetDefaultContext(TelemetryConfiguration configuration)
        {
            var property = typeof(TelemetryConfiguration).GetProperty("DefaultContext", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(property);
            return Assert.IsType<TelemetryContext>(property.GetValue(configuration));
        }

        private static void EnsureBuilderConfiguration(TelemetryConfiguration configuration)
        {
            var field = typeof(TelemetryConfiguration).GetField("builderConfiguration", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);

            if (field.GetValue(configuration) == null)
            {
                field.SetValue(configuration, (Action<IOpenTelemetryBuilder>)(builder =>
                    builder.WithTracing(tracing => tracing.AddSource(ApplicationInsightsActivitySourceName))));
            }
        }

        private sealed class CaptureActivityProcessor : BaseProcessor<Activity>
        {
            private readonly List<Activity> activities;

            public CaptureActivityProcessor(List<Activity> activities)
            {
                this.activities = activities;
            }

            public override void OnEnd(Activity data)
            {
                this.activities.Add(data);
            }
        }
    }
}
