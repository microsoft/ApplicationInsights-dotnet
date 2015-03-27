namespace SampleWebAppIntegration.FunctionalTest
{
    using Xunit;
    using System.Net.Http;
    using System.Net;
    using FunctionalTestUtils;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;

    public class SuccessScenario
    {
        [Fact]
        public async void ServerReturns200WithMonitoring()
        {
            InProcessServer server = new InProcessServer("SampleWebAppIntegration");

            List<ITelemetry> buffer = new List<ITelemetry>();
            BackTelemetryChannelExtensions.InitializeFunctionalTestTelemetryChannel(buffer);

            using (server.Start())
            {
                HttpClient client = new HttpClient();
                var result = await client.GetAsync(server.BaseHost);
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);

                Assert.Equal(1, buffer.Count);
                ITelemetry telemetry = buffer[0];
                Assert.IsAssignableFrom(typeof(RequestTelemetry), telemetry);

                RequestTelemetry request = (RequestTelemetry)telemetry;
                Assert.Equal("200", request.ResponseCode);
                Assert.Equal("GET /", request.Name);
            }
        }

        [Fact]
        public async void ServerReturns404ForNotExistingController()
        {
            InProcessServer server = new InProcessServer("SampleWebAppIntegration");

            List<ITelemetry> buffer = new List<ITelemetry>();
            BackTelemetryChannelExtensions.InitializeFunctionalTestTelemetryChannel(buffer);

            using (server.Start())
            {
                HttpClient client = new HttpClient();
                var result = await client.GetAsync(server.BaseHost + "/not/existing/controller");
                Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);

                Assert.Equal(1, buffer.Count);
                ITelemetry telemetry = buffer[0];
                Assert.IsAssignableFrom(typeof(RequestTelemetry), telemetry);

                RequestTelemetry request = (RequestTelemetry)telemetry;
                Assert.Equal("404", request.ResponseCode);
            }
        }

    }
}
