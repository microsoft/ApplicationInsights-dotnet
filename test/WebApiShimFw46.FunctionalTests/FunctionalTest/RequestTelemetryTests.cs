namespace SampleWebAppIntegration.FunctionalTest
{
    using FunctionalTestUtils;
    using Microsoft.ApplicationInsights.DataContracts;
    using System.Linq;
    using Xunit;

    public class RequestTelemetryTests : TelemetryTestsBase
    {
        public RequestTelemetryTests() : base("WebApiShimFw46.FunctionalTests")
        { }

        [Fact]
        public void TestBasicRequestPropertiesAfterRequestingValuesController()
        {
            var expectedRequestTelemetry = new RequestTelemetry();
            expectedRequestTelemetry.HttpMethod = "GET";
            expectedRequestTelemetry.Name = "GET Values/Get";
            expectedRequestTelemetry.ResponseCode = "200";
            expectedRequestTelemetry.Success = true;

            this.ValidateBasicRequest("/api/values", expectedRequestTelemetry);
        }

        [Fact]
        public void TestBasicRequestPropertiesAfterRequestingNotExistingController()
        {
            var expectedRequestTelemetry = new RequestTelemetry();
            expectedRequestTelemetry.HttpMethod = "GET";
            expectedRequestTelemetry.Name = "GET /api/notexistingcontroller";
            expectedRequestTelemetry.ResponseCode = "404";
            expectedRequestTelemetry.Success = false;

            this.ValidateBasicRequest("/api/notexistingcontroller", expectedRequestTelemetry);
        }

        [Fact]
        public void TestBasicRequestPropertiesAfterRequestingWebApiShimRoute()
        {
            var expectedRequestTelemetry = new RequestTelemetry();
            expectedRequestTelemetry.HttpMethod = "GET";
            expectedRequestTelemetry.Name = "GET Values/Get [id]";
            expectedRequestTelemetry.ResponseCode = "200";
            expectedRequestTelemetry.Success = true;

            this.ValidateBasicRequest("/api/values/1", expectedRequestTelemetry);
        }

    }
}
