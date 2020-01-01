namespace EmptyApp.FunctionalTests.FunctionalTest
{
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;
    using FunctionalTestUtils;
    using Microsoft.ApplicationInsights.DataContracts;
    using Xunit;
    using Xunit.Abstractions;

    public class RequestTelemetryEmptyAppTests : TelemetryTestsBase
    {
        private readonly string assemblyName;

        public RequestTelemetryEmptyAppTests(ITestOutputHelper output) : base(output)
        {
            this.assemblyName = this.GetType().GetTypeInfo().Assembly.GetName().Name;
        }

        [Fact]
        public void TestBasicRequestPropertiesAfterRequestingBasicPage()
        {
            using (var server = new InProcessServer(assemblyName))
            {
                const string RequestPath = "/";

                var expectedRequestTelemetry = new RequestTelemetry();
                expectedRequestTelemetry.Name = "GET /";
                expectedRequestTelemetry.ResponseCode = "200";
                expectedRequestTelemetry.Success = true;
                expectedRequestTelemetry.Url = new System.Uri(server.BaseHost + RequestPath);

                this.ValidateBasicRequest(server, RequestPath, expectedRequestTelemetry);
            }
        }

        [Fact]
        public void TestBasicRequestPropertiesAfterRequestingNotExistingPage()
        {
            using (var server = new InProcessServer(assemblyName))
            {
                const string RequestPath = "/not/existing/controller";

                var expectedRequestTelemetry = new RequestTelemetry();
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
            InProcessServer server;
            using (server = new InProcessServer(assemblyName))
            {
                using (var httpClient = new HttpClient())
                {
                    var task = httpClient.GetAsync(server.BaseHost + "/Mixed");
                    task.Wait(TestTimeoutMs);
                }
            }

            var telemetries = server.BackChannel.Buffer;
            Assert.Contains(telemetries.OfType<DependencyTelemetry>(), t => t.Name == "GET /Mixed");
            Assert.True(telemetries.Count >= 4);
            Assert.Contains(telemetries.OfType<RequestTelemetry>(), t => t.Name == "GET /Mixed");
            Assert.Contains(telemetries.OfType<EventTelemetry>(), t => t.Name == "GetContact");
            Assert.Contains(telemetries.OfType<MetricTelemetry>(),
                t => t.Name == "ContactFile" && t.Value == 1);

            Assert.Contains(telemetries.OfType<TraceTelemetry>(),
                t => t.Message == "Fetched contact details." && t.SeverityLevel == SeverityLevel.Information);
        }
    }
}
