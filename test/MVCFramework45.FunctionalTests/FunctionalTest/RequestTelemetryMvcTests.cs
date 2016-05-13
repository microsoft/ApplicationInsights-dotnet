namespace SampleWebAppIntegration.FunctionalTest
{
    using System.Linq;
    using System.Net.Http;
    using FunctionalTestUtils;
    using Microsoft.ApplicationInsights.DataContracts;
    using Xunit;

    public class RequestTelemetryMvcTests : TelemetryTestsBase
    {
        private const string assemblyName = "MVCFramework45.FunctionalTests";

        [Fact]
        public void TestBasicRequestPropertiesAfterRequestingHomeController()
        {
            using (var server = new InProcessServer(assemblyName))
            {
                const string RequestPath = "/";

                var expectedRequestTelemetry = new RequestTelemetry();
                expectedRequestTelemetry.HttpMethod = "GET";
                expectedRequestTelemetry.Name = "GET Home/Index";
                expectedRequestTelemetry.ResponseCode = "200";
                expectedRequestTelemetry.Success = true;
                expectedRequestTelemetry.Url = new System.Uri(server.BaseHost + RequestPath);

                this.ValidateBasicRequest(server, RequestPath, expectedRequestTelemetry);
            }
        }

        [Fact]
        public void TestBasicRequestPropertiesAfterRequestingActionWithParameter()
        {
            using (var server = new InProcessServer(assemblyName))
            {
                const string RequestPath = "/Home/About/5";

                var expectedRequestTelemetry = new RequestTelemetry();
                expectedRequestTelemetry.HttpMethod = "GET";
                expectedRequestTelemetry.Name = "GET Home/About [id]";
                expectedRequestTelemetry.ResponseCode = "200";
                expectedRequestTelemetry.Success = true;
                expectedRequestTelemetry.Url = new System.Uri(server.BaseHost + RequestPath);

                this.ValidateBasicRequest(server, RequestPath, expectedRequestTelemetry);
            }
        }

        [Fact]
        public void TestBasicRequestPropertiesAfterRequestingNotExistingController()
        {
            using (var server = new InProcessServer(assemblyName))
            {
                const string RequestPath = "/not/existing/controller";

                var expectedRequestTelemetry = new RequestTelemetry();
                expectedRequestTelemetry.HttpMethod = "GET";
                expectedRequestTelemetry.Name = "GET /not/existing/controller";
                expectedRequestTelemetry.ResponseCode = "404";
                expectedRequestTelemetry.Success = false;
                expectedRequestTelemetry.Url = new System.Uri(server.BaseHost + RequestPath);

                this.ValidateBasicRequest(server, RequestPath, expectedRequestTelemetry);
            }
        }

        [Fact]
        public void TestMixedTelemetryItemsReceived()
        {
            using (var server = new InProcessServer(assemblyName))
            {
                var httpClient = new HttpClient();
                var task = httpClient.GetAsync(server.BaseHost + "/Home/Contact");
                task.Wait(TestTimeoutMs);

                var request = server.BackChannel.Buffer.OfType<RequestTelemetry>().Single();
                var eventTelemetry = server.BackChannel.Buffer.OfType<EventTelemetry>().Single();
                var metricTelemetry = server.BackChannel.Buffer.OfType<MetricTelemetry>().Single();
                var traceTelemetry = server.BackChannel.Buffer.OfType<TraceTelemetry>().Single();

#if NET451
                var dependencyTelemetry = server.BackChannel.Buffer.OfType<DependencyTelemetry>().Single();
                Assert.NotNull(dependencyTelemetry);               
#endif

                Assert.True(server.BackChannel.Buffer.Count >= 4);
                Assert.NotNull(request);
                Assert.NotNull(eventTelemetry);
                Assert.NotNull(metricTelemetry);
                Assert.NotNull(traceTelemetry);
            }
        }
    }
}
