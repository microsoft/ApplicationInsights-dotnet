namespace SampleWebAppIntegration.FunctionalTest
{
    using System;
    using System.Linq;
    using FunctionalTestUtils;
    using Microsoft.ApplicationInsights.DataContracts;
    using Xunit;

    public class ExceptionTelemetryTests : TelemetryTestsBase
    {
        public ExceptionTelemetryTests() : base("WebApiShimFw46.FunctionalTests")
        { }

        [Fact]
        public void TestBasicRequestPropertiesAfterRequestingControllerThatThrows()
        {
            var expectedRequestTelemetry = new RequestTelemetry();
            expectedRequestTelemetry.HttpMethod = "GET";
            expectedRequestTelemetry.Name = "GET Exception/Get";
            //TODO: default template of Web API applicaiton doesn't have error handling middleware 
            //that will set appropriate status code
            expectedRequestTelemetry.ResponseCode = "200";
            expectedRequestTelemetry.Success = true;
            // expectedRequestTelemetry.Url ???

            this.ValidateBasicRequest("/api/exception", expectedRequestTelemetry);
        }

        [Fact]
        public void TestBasicExceptionPropertiesAfterRequestingControllerThatThrows()
        {
            var expectedExceptionTelemetry = new ExceptionTelemetry();
            expectedExceptionTelemetry.HandledAt = ExceptionHandledAt.Platform;
            expectedExceptionTelemetry.Exception = new InvalidOperationException();

            this.ValidateBasicException("/api/exception", expectedExceptionTelemetry);
        }
    }
}
