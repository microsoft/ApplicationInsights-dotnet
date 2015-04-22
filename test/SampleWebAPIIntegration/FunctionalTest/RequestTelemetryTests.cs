namespace SampleWebAppIntegration.FunctionalTest
{
    using FunctionalTestUtils.Tests;
    using Microsoft.ApplicationInsights.DataContracts;
    using System.Linq;
    using Xunit;

    public class RequestTelemetryTests : RequestTelemetryTestsBase
    {
        public RequestTelemetryTests() : base("SampleWebAPIIntegration")
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

    }
}
