namespace WebApi20.FunctionalTests.FunctionalTest
{
    using System.Linq;
    using System.Text.RegularExpressions;

    using FunctionalTestUtils;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.DependencyCollector;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;
    using Xunit.Abstractions;

    public class RequestTelemetryWebApiTests : TelemetryTestsBase
    {
        private const string assemblyName = "WebApi20.FunctionalTests20";
        public RequestTelemetryWebApiTests(ITestOutputHelper output) : base (output)
        {
        }

        [Fact]
        public void TestBasicRequestPropertiesAfterRequestingValuesController()
        {
            using (var server = new InProcessServer(assemblyName, this.output))
            {
                const string RequestPath = "/api/values";

                var expectedRequestTelemetry = new RequestTelemetry();
                expectedRequestTelemetry.Name = "GET Values/Get";
                expectedRequestTelemetry.ResponseCode = "200";
                expectedRequestTelemetry.Success = true;
                expectedRequestTelemetry.Url = new System.Uri(server.BaseHost + RequestPath);

                this.ValidateBasicRequest(server, RequestPath, expectedRequestTelemetry);
            }
        }

        [Fact]
        public void TestBasicRequestPropertiesAfterRequestingNotExistingController()
        {
            using (var server = new InProcessServer(assemblyName, this.output))
            {
                const string RequestPath = "/api/notexistingcontroller";

                var expectedRequestTelemetry = new RequestTelemetry();
                expectedRequestTelemetry.Name = "GET /api/notexistingcontroller";
                expectedRequestTelemetry.ResponseCode = "404";
                expectedRequestTelemetry.Success = false;
                expectedRequestTelemetry.Url = new System.Uri(server.BaseHost + RequestPath);

                this.ValidateBasicRequest(server, RequestPath, expectedRequestTelemetry);
            }
        }

        [Fact]
        public void TestBasicRequestPropertiesAfterRequestingWebApiShimRoute()
        {
            using (var server = new InProcessServer(assemblyName, this.output))
            {
                const string RequestPath = "/api/values/1";

                var expectedRequestTelemetry = new RequestTelemetry();
                expectedRequestTelemetry.Name = "GET Values/Get [id]";
                expectedRequestTelemetry.ResponseCode = "200";
                expectedRequestTelemetry.Success = true;
                expectedRequestTelemetry.Url = new System.Uri(server.BaseHost + RequestPath);

                this.ValidateBasicRequest(server, RequestPath, expectedRequestTelemetry);
            }
        }

        [Fact]
        public void TestNoHeaderInjectionRequestTrackingOptions()
        {
            IWebHostBuilder Config(IWebHostBuilder builder)
            {
                return builder.ConfigureServices(services =>
                {
                    services.AddApplicationInsightsTelemetry(options => { options.RequestCollectionOptions.InjectResponseHeaders = false; });
                });
            }

            using (var server = new InProcessServer(assemblyName, this.output, Config))
            {
                const string RequestPath = "/api/values/1";

                var expectedRequestTelemetry = new RequestTelemetry();
                expectedRequestTelemetry.Name = "GET Values/Get [id]";
                expectedRequestTelemetry.ResponseCode = "200";
                expectedRequestTelemetry.Success = true;
                expectedRequestTelemetry.Url = new System.Uri(server.BaseHost + RequestPath);

                this.ValidateBasicRequest(server, RequestPath, expectedRequestTelemetry, false);
            }
        }

        [Fact]
        public void TestW3COperationIdFormatGeneration()
        {
            IWebHostBuilder Config(IWebHostBuilder builder)
            {
                // disable Dependency tracking (i.e. header injection)
                return builder.ConfigureServices(services =>
                {
                    services.AddApplicationInsightsTelemetry();
                    services.Remove(services.Single(sd =>
                        sd.ImplementationType == typeof(DependencyTrackingTelemetryModule)));
                });
            }

            using (var server = new InProcessServer(assemblyName, this.output, Config))
            {
                const string RequestPath = "/api/values/1";

                var expectedRequestTelemetry = new RequestTelemetry();
                expectedRequestTelemetry.Name = "GET Values/Get [id]";
                expectedRequestTelemetry.ResponseCode = "200";
                expectedRequestTelemetry.Success = true;
                expectedRequestTelemetry.Url = new System.Uri(server.BaseHost + RequestPath);

                var item = this.ValidateBasicRequest(server, RequestPath, expectedRequestTelemetry, true);

                // W3C compatible-Id ( should go away when W3C is implemented in .NET https://github.com/dotnet/corefx/issues/30331)
                Assert.Equal(32, item.tags["ai.operation.id"].Length);
                Assert.True(Regex.Match(item.tags["ai.operation.id"], @"[a-z][0-9]").Success);
                // end of workaround test
            }
        }
    }
}

