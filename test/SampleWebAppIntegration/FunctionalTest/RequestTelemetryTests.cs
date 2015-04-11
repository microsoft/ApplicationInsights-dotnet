namespace SampleWebAppIntegration.FunctionalTest
{
    using FunctionalTestUtils.Tests;
    using Microsoft.ApplicationInsights.DataContracts;
    using Xunit;

    public class RequestTelemetryTests : RequestTelemetryTestsBase
    {
        public RequestTelemetryTests() : base("SampleWebAppIntegration")
        { }

        [Fact]
        public void TestBasicRequestPropertiesAfterRequestingHomeController()
        {
            var expectedRequestTelemetry = new RequestTelemetry();
            expectedRequestTelemetry.HttpMethod = "GET";
            expectedRequestTelemetry.Name = "GET Home/Index";
            expectedRequestTelemetry.ResponseCode = "200";
            expectedRequestTelemetry.Success = true;
            // expectedRequestTelemetry.Url ???

            this.ValidateBasicRequest("/", expectedRequestTelemetry);
        }

        [Fact]
        public void TestBasicRequestPropertiesAfterRequestingActionWithParameter()
        {
            var expectedRequestTelemetry = new RequestTelemetry();
            expectedRequestTelemetry.HttpMethod = "GET";
            expectedRequestTelemetry.Name = "GET Home/Contact [id]";
            expectedRequestTelemetry.ResponseCode = "200";
            expectedRequestTelemetry.Success = true;
            // expectedRequestTelemetry.Url ???

            this.ValidateBasicRequest("/Home/Contact/5", expectedRequestTelemetry);
        }

        [Fact]
        public void TestBasicRequestPropertiesAfterRequestingNotExistingController()
        {
            var expectedRequestTelemetry = new RequestTelemetry();
            expectedRequestTelemetry.HttpMethod = "GET";
            expectedRequestTelemetry.Name = "GET /not/existing/controller";
            expectedRequestTelemetry.ResponseCode = "404";
            expectedRequestTelemetry.Success = false;

            this.ValidateBasicRequest("/not/existing/controller", expectedRequestTelemetry);
        }
    }
}
