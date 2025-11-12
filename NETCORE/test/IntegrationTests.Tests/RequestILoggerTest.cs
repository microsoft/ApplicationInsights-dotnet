using IntegrationTests.WebApp;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Xunit;

namespace IntegrationTests.Tests
{
    public partial class RequestCollectionTest : IClassFixture<CustomWebApplicationFactory<Startup>>
    {
        [Fact]
        public async Task RequestILoggerUserConfigOverRidesDefaultLevel()
        {
            // Arrange
            var loggerCategory = "IntegrationTests.WebApp.Controllers.HomeController";

            var client = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices((services) =>
            services.AddLogging(logBuilder => logBuilder.AddFilter(loggerCategory, LogLevel.Information))
            )).CreateClient();

            var path = "Home";
            var url = client.BaseAddress + path;

            // Act
            var request = CreateRequestMessage();
            request.RequestUri = new Uri(url);
            var response = await client.SendAsync(request);

            // Assert
            response.EnsureSuccessStatusCode();

            await WaitForTelemetryToArrive(expectedItemCount: 3);

            var items = _factory.Telemetry.Items;
            PrintItems(items);
            Assert.Equal(3, items.Count);

            var reqs = _factory.Telemetry.GetTelemetryOfType<RequestTelemetryEnvelope>();
            Assert.Single(reqs);
            var req = reqs[0];
            Assert.NotNull(req);

            var traces = _factory.Telemetry.GetTelemetryOfType<TraceTelemetryEnvelope>();
            Assert.Equal(2, traces.Count);
            var trace1 = traces[0];
            var trace2 = traces[1];
            Assert.Equal(req.Id, trace1.OperationParentId);

            // TODO: Validate traces

            ValidateRequest(
                 requestTelemetry: req,
                 expectedResponseCode: "200",
                  expectedName: "GET " + path,
                 expectedUrl: url,
                 expectedSuccess: true);

            client.Dispose();
        }

        [Fact]
        public async Task IloggerWarningOrAboveCapturedByDefault()
        {
            var client = _factory.CreateClient();

            var path = "Home";
            var url = client.BaseAddress + path;

            // Act
            var request = CreateRequestMessage();
            request.RequestUri = new Uri(url);
            var response = await client.SendAsync(request);

            // Assert
            response.EnsureSuccessStatusCode();

            await WaitForTelemetryToArrive(expectedItemCount: 2);

            var items = _factory.Telemetry.Items;
            PrintItems(items);
            Assert.Equal(2, items.Count);

            var reqs = _factory.Telemetry.GetTelemetryOfType<RequestTelemetryEnvelope>();
            Assert.Single(reqs);
            var req = reqs[0];
            Assert.NotNull(req);

            var traces = _factory.Telemetry.GetTelemetryOfType<TraceTelemetryEnvelope>();
            Assert.Single(traces);
            var trace = traces[0];
            Assert.Equal(req.Id, trace.OperationParentId);

            // TODO: Validate traces

            ValidateRequest(
                 requestTelemetry: req,
                 expectedResponseCode: "200",
                    expectedName: "GET " + path,
                 expectedUrl: url,
                 expectedSuccess: true);

        }
    }
}
