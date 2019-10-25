namespace WebApi.FunctionalTests.FunctionalTest
{
    using System;
    using FunctionalTestUtils;
    using Microsoft.ApplicationInsights.DataContracts;
    using Xunit;
    using Microsoft.ApplicationInsights.Extensibility;
    using Xunit.Abstractions;

    public class ExceptionTelemetryWebApiTests : TelemetryTestsBase
    {
        public ExceptionTelemetryWebApiTests(ITestOutputHelper output) : base(output)
        {
        }

        private const string assemblyName = "WebApi.FunctionalTests";

        [Fact]
        public void TestBasicRequestPropertiesAfterRequestingControllerThatThrows()
        {
            using (var server = new InProcessServer(assemblyName))
            {
                const string RequestPath = "/api/exception";

                var expectedRequestTelemetry = new RequestTelemetry();
                expectedRequestTelemetry.Name = "GET Exception/Get";
                //TODO: default template of Web API applicaiton doesn't have error handling middleware 
                //that will set appropriate status code
                expectedRequestTelemetry.ResponseCode = "200";
                expectedRequestTelemetry.Success = false;
                expectedRequestTelemetry.Url = new System.Uri(server.BaseHost + RequestPath);

                this.ValidateBasicRequest(server, RequestPath, expectedRequestTelemetry);
            }
        }

        [Fact]
        public void TestBasicExceptionPropertiesAfterRequestingControllerThatThrows()
        {
            using (var server = new InProcessServer(assemblyName))
            {
                var expectedExceptionTelemetry = new ExceptionTelemetry();
                expectedExceptionTelemetry.Exception = new InvalidOperationException();

                this.ValidateBasicException(server, "/api/exception", expectedExceptionTelemetry);
            }
        }
    }
}
