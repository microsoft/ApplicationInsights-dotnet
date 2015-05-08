namespace SampleWebAppIntegration.FunctionalTest
{
    using FunctionalTestUtils;
    using Microsoft.ApplicationInsights.DataContracts;
    using System.Linq;
    using Xunit;

    public class RequestTelemetryTests : TelemetryTestsBase
    {
        public RequestTelemetryTests() : base("Mvc6Framework45.FunctionalTests")
        { }

        [Fact]
        public void TestBasicRequestPropertiesAfterRequestingHomeController()
        {
            const string RequestPath = "/";

            var expectedRequestTelemetry = new RequestTelemetry();
            expectedRequestTelemetry.HttpMethod = "GET";
            expectedRequestTelemetry.Name = "GET Home/Index";
            expectedRequestTelemetry.ResponseCode = "200";
            expectedRequestTelemetry.Success = true;
            expectedRequestTelemetry.Url = new System.Uri(this.Server.BaseHost + RequestPath);

            this.ValidateBasicRequest(RequestPath, expectedRequestTelemetry);
        }

        [Fact]
        public void TestBasicRequestPropertiesAfterRequestingActionWithParameter()
        {
            const string RequestPath = "/Home/About/5";

            var expectedRequestTelemetry = new RequestTelemetry();
            expectedRequestTelemetry.HttpMethod = "GET";
            expectedRequestTelemetry.Name = "GET Home/About [id]";
            expectedRequestTelemetry.ResponseCode = "200";
            expectedRequestTelemetry.Success = true;
            expectedRequestTelemetry.Url = new System.Uri(this.Server.BaseHost + RequestPath);

            this.ValidateBasicRequest(RequestPath, expectedRequestTelemetry);
        }

        [Fact]
        public void TestBasicRequestPropertiesAfterRequestingNotExistingController()
        {
            const string RequestPath = "/not/existing/controller";

            var expectedRequestTelemetry = new RequestTelemetry();
            expectedRequestTelemetry.HttpMethod = "GET";
            expectedRequestTelemetry.Name = "GET /not/existing/controller";
            expectedRequestTelemetry.ResponseCode = "404";
            expectedRequestTelemetry.Success = false;
            expectedRequestTelemetry.Url = new System.Uri(this.Server.BaseHost + RequestPath);

            this.ValidateBasicRequest(RequestPath, expectedRequestTelemetry);
        }

        [Fact]
        public void TestMixedTelemetryItemsReceived()
        {
            var task = this.HttpClient.GetAsync(this.Server.BaseHost + "/home/contact");
            task.Wait(TestTimeoutMs);

            var request = this.Buffer.OfType<RequestTelemetry>().Single();
            var eventTelemetry = this.Buffer.OfType<EventTelemetry>().Single();
            var metricTelemetry = this.Buffer.OfType<MetricTelemetry>().Single();
            var traceTelemetry = this.Buffer.OfType<TraceTelemetry>().Single();

            Assert.Equal(4, this.Buffer.Count);
            Assert.NotNull(request);
            Assert.NotNull(eventTelemetry);
            Assert.NotNull(metricTelemetry);
            Assert.NotNull(traceTelemetry);
        }
    }
}
