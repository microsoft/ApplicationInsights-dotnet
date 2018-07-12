using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation.ApplicationId;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace WebApi20.FunctionalTests.FunctionalTest
{
    using FunctionalTestUtils;
    using Microsoft.ApplicationInsights.DataContracts;
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
    }
}

