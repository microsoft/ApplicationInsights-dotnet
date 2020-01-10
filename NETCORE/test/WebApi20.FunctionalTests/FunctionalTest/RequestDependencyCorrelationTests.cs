namespace WebApi20.FuncTests
{
    using FunctionalTestUtils;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.ApplicationInsights.AspNetCore;
    using Microsoft.ApplicationInsights.DependencyCollector;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;
    using Xunit.Abstractions;
    using System.Reflection;

    public class RequestDependencyCorrelationTests : TelemetryTestsBase, IDisposable
    {
        private readonly string assemblyName;

        public RequestDependencyCorrelationTests(ITestOutputHelper output) : base (output)
        {
            this.assemblyName = this.GetType().GetTypeInfo().Assembly.GetName().Name;
        }

        // The NET451 conditional check is wrapped inside the test to make the tests visible in the test explorer. We can move them to the class level once if the issue is resolved.

        public void TestBasicDependencyPropertiesAfterRequestingBasicPage()
        {
            const string RequestPath = "/api/values";

            using (var server = new InProcessServer(assemblyName, this.output))
            {
                DependencyTelemetry expected = new DependencyTelemetry();
                expected.ResultCode = "200";
                expected.Success = true;
                expected.Name = "GET " + RequestPath;
                expected.Data = server.BaseHost + RequestPath;

                this.ValidateBasicDependency(server, RequestPath, expected);
            }
        }

        // We may need to add more tests to cover Request + Dependency Tracking
        public void TestDependencyAndRequestWithW3CStandard()
        {
            const string RequestPath = "/api/values";

            using (var server = new InProcessServer(assemblyName, this.output, builder =>
            {
                return builder.ConfigureServices(
                    services =>
                    {
                        services.AddApplicationInsightsTelemetry(
                            o => o.RequestCollectionOptions.EnableW3CDistributedTracing = true);

                        // enable headers injection on localhost
                        var dependencyModuleConfigFactoryDescriptor = services.Where(sd => sd.ServiceType == typeof(ITelemetryModuleConfigurator));
                        services.Remove(dependencyModuleConfigFactoryDescriptor.First());

                        services.ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((module, o) =>
                        {
                            module.EnableW3CHeadersInjection = true;
                        });
                    });
            }))
            {
                DependencyTelemetry expected = new DependencyTelemetry
                {
                    ResultCode = "200",
                    Success = true,
                    Name = "GET " + RequestPath,
                    Data = server.BaseHost + RequestPath
                };

                var activity = new Activity("dummy")
                    .Start();

                var (request, dependency) = this.ValidateBasicDependency(server, RequestPath, expected);
                string expectedTraceId = activity.TraceId.ToHexString();
                string expectedParentSpanId = activity.SpanId.ToHexString();

                Assert.Equal(expectedTraceId, request.tags["ai.operation.id"]);
                Assert.Equal(expectedTraceId, dependency.tags["ai.operation.id"]);
                Assert.Equal(expectedParentSpanId, dependency.tags["ai.operation.parentId"]);
            }
        }

        public void Dispose()
        {
            while (Activity.Current != null)
            {
                Activity.Current.Stop();
            }
        }
    }
}
