namespace EmptyApp20.FunctionalTests.FunctionalTest
{
    using System.Linq;
    using System.Net.Http;
    using AI;
    using FunctionalTestUtils;
    using Microsoft.ApplicationInsights.DataContracts;
    using Xunit;
    using Xunit.Abstractions;    

    public class RequestTelemetryEmptyAppTests : TelemetryTestsBase
    {
        private const string assemblyName = "EmptyApp20.FunctionalTests";

        public RequestTelemetryEmptyAppTests(ITestOutputHelper output) : base (output)
        {
        }

        [Fact]
        public void TestBasicRequestPropertiesAfterRequestingBasicPage()
        {
            using (var server = new InProcessServer(assemblyName, this.output))
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
            using (var server = new InProcessServer(assemblyName, this.output))
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
            using (var server = new InProcessServer(assemblyName, this.output))
            {
                this.ExecuteRequest(server.BaseHost + "/Mixed");

                var telemetries = server.Listener.ReceiveItems(5, TestListenerTimeoutInMs);
                this.DebugTelemetryItems(telemetries);

                Assert.True(telemetries.Length >= 5);

                Assert.Contains(telemetries.OfType<TelemetryItem<RemoteDependencyData>>(), 
                    t => ((TelemetryItem<RemoteDependencyData>)t).data.baseData.name == "GET /Mixed");

                Assert.Contains(telemetries.OfType<TelemetryItem<RequestData>>(),
                    t => ((TelemetryItem<RequestData>)t).data.baseData.name == "GET /Mixed");

                Assert.Contains(telemetries.OfType<TelemetryItem<EventData>>(),
                    t => ((TelemetryItem<EventData>)t).data.baseData.name == "GetContact");

                Assert.Contains(telemetries.OfType<TelemetryItem<MetricData>>(),
                    t => ((TelemetryItem<MetricData>)t).data.baseData.metrics[0].name == "ContactFile" && ((TelemetryItem<MetricData>)t).data.baseData.metrics[0].value == 1);

                Assert.Contains(telemetries.OfType<TelemetryItem<MessageData>>(),
                    t => ((TelemetryItem<MessageData>)t).data.baseData.message == "Fetched contact details." && ((TelemetryItem<MessageData>)t).data.baseData.severityLevel == AI.SeverityLevel.Information);
            }
        }
    }
}
