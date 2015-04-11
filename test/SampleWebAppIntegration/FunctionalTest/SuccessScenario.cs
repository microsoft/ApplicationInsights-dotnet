namespace SampleWebAppIntegration.FunctionalTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using FunctionalTestUtils;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Xunit;

    public class SuccessScenario
    {
        [Fact]
        public async void ServerReturns200WithMonitoring()
        {
            Action<IList<ITelemetry>> assertions = (telemetries) =>
            {
                Assert.Equal(1, telemetries.Count);
                ITelemetry telemetry = telemetries[0];
                Assert.IsAssignableFrom(typeof(RequestTelemetry), telemetry);

                RequestTelemetry request = (RequestTelemetry)telemetry;
                Assert.Equal("200", request.ResponseCode);
                Assert.Equal("GET /", request.Name);
            };

            await this.ExecuteRequest("/", HttpStatusCode.OK, assertions);
        }

        [Fact]
        public async void ServerReturns404ForNotExistingController()
        {
            Action<IList<ITelemetry>> assertions = (telemetries) =>
            {
                Assert.Equal(1, telemetries.Count);
                ITelemetry telemetry = telemetries[0];
                Assert.IsAssignableFrom(typeof(RequestTelemetry), telemetry);

                RequestTelemetry request = (RequestTelemetry)telemetry;
                Assert.Equal("404", request.ResponseCode);
            };

            await this.ExecuteRequest("/not/existing/controller", HttpStatusCode.NotFound, assertions);
        }

        [Fact]
        public async void ServerReturns200FromAsyncController()
        {
            Action<IList<ITelemetry>> assertions = (telemetries) =>
            {
                Assert.Equal(4, telemetries.Count);
                RequestTelemetry request = telemetries.OfType<RequestTelemetry>().Single();
                Assert.Equal("200", request.ResponseCode);

                EventTelemetry eventTelemetry = telemetries.OfType<EventTelemetry>().Single();
                Assert.Equal("GetContact", eventTelemetry.Name);

                MetricTelemetry metricTelemetry = telemetries.OfType<MetricTelemetry>().Single();
                Assert.Equal(1, metricTelemetry.Count);
                Assert.Equal("ContactFile", metricTelemetry.Name);
                Assert.Equal(1, metricTelemetry.Value);

                TraceTelemetry traceTelemetry = telemetries.OfType<TraceTelemetry>().Single();
                Assert.Equal("GET /home/contact", traceTelemetry.Context.Operation.Name);
            };

            await this.ExecuteRequest("/home/contact", HttpStatusCode.OK, assertions);
        }

        private async Task ExecuteRequest(string relativeRequestAddress, HttpStatusCode expectedStatus, Action<List<ITelemetry>> assertions)
        {
            InProcessServer server = new InProcessServer("SampleWebAppIntegration");

            List<ITelemetry> buffer = new List<ITelemetry>();
            BackTelemetryChannelExtensions.InitializeFunctionalTestTelemetryChannel(buffer);

            using (server.Start())
            {
                HttpClient client = new HttpClient();
                var result = await client.GetAsync(server.BaseHost + relativeRequestAddress);
                Assert.Equal(expectedStatus, result.StatusCode);

                assertions.Invoke(buffer);
            }
        }
    }
}
