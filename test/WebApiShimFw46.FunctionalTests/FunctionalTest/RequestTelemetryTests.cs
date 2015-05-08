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
            const string RequestPath = "/api/values";

            var expectedRequestTelemetry = new RequestTelemetry();
            expectedRequestTelemetry.HttpMethod = "GET";
            expectedRequestTelemetry.Name = "GET Values/Get";
            expectedRequestTelemetry.ResponseCode = "200";
            expectedRequestTelemetry.Success = true;
            expectedRequestTelemetry.Url = new System.Uri(this.Server.BaseHost + RequestPath);

            this.ValidateBasicRequest(RequestPath, expectedRequestTelemetry);
        }

        [Fact]
        public void TestBasicRequestPropertiesAfterRequestingNotExistingController()
        {
            const string RequestPath = "/api/notexistingcontroller";

            var expectedRequestTelemetry = new RequestTelemetry();
            expectedRequestTelemetry.HttpMethod = "GET";
            expectedRequestTelemetry.Name = "GET /api/notexistingcontroller";
            expectedRequestTelemetry.ResponseCode = "404";
            expectedRequestTelemetry.Success = false;
            expectedRequestTelemetry.Url = new System.Uri(this.Server.BaseHost + RequestPath);

            this.ValidateBasicRequest(RequestPath, expectedRequestTelemetry);
        }

        [Fact]
        public void TestBasicRequestPropertiesAfterRequestingWebApiShimRoute()
        {
            const string RequestPath = "/api/values/1";

            var expectedRequestTelemetry = new RequestTelemetry();
            expectedRequestTelemetry.HttpMethod = "GET";
            expectedRequestTelemetry.Name = "GET Values/Get [id]";
            expectedRequestTelemetry.ResponseCode = "200";
            expectedRequestTelemetry.Success = true;
            expectedRequestTelemetry.Url = new System.Uri(this.Server.BaseHost + RequestPath);

            this.ValidateBasicRequest(RequestPath, expectedRequestTelemetry);
        }

    }
}
