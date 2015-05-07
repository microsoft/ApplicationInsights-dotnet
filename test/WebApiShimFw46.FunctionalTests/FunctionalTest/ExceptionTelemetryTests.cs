namespace SampleWebAppIntegration.FunctionalTest
{
    using FunctionalTestUtils.Tests;
    using Microsoft.ApplicationInsights.DataContracts;
    using System;
    using System.Linq;
    using Xunit;

    public class ExceptionTelemetryTests : RequestTelemetryTestsBase
    {
        public ExceptionTelemetryTests() : base("WebApiShimFw46.FunctionalTests")
        { }

        [Fact]
        public void TestBasicRequestPropertiesAfterRequestingControllerThatThrows()
        {
            var expectedRequestTelemetry = new RequestTelemetry();
            expectedRequestTelemetry.HttpMethod = "GET";
            expectedRequestTelemetry.Name = "GET Values/Get [id]";
            //TODO: default template of Web API applicaiton doesn't have error handling middleware 
            //that will set appropriate status code
            expectedRequestTelemetry.ResponseCode = "200";
            expectedRequestTelemetry.Success = true;
            // expectedRequestTelemetry.Url ???

            this.ValidateBasicRequest("/api/values/42", expectedRequestTelemetry);
        }

        [Fact]
        public void TestBasicExceptionPropertiesAfterRequestingControllerThatThrows()
        {
            var expectedExceptionTelemetry = new ExceptionTelemetry();
            expectedExceptionTelemetry.HandledAt = ExceptionHandledAt.Platform;
            expectedExceptionTelemetry.Exception = new InvalidOperationException();

            this.ValidateBasicException("/api/values/42", expectedExceptionTelemetry);
        }
    }
}
