using System.Threading.Tasks;

using IntegrationTests.Tests.TestFramework;
using IntegrationTests.WebApp;

using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;

using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests.Tests
{
    public class RequestILoggerTest : IClassFixture<CustomWebApplicationFactory<Startup>>
    {
        private readonly CustomWebApplicationFactory<Startup> _factory;
        private readonly ITestOutputHelper _output;

        public RequestILoggerTest(CustomWebApplicationFactory<Startup> factory, ITestOutputHelper output)
        {
            this._output = output;
            _factory = factory;
            _factory.sentItems.Clear();
        }

        [Fact]
        public async Task RequestILoggerUserConfigOverRidesDefaultLevel()
        {
            // Arrange
            var loggerCategory = "IntegrationTests.WebApp.Controllers.HomeController";
            var path = "Home";
            var expectedName = "GET Home/Get";
            var requestUri = _factory.MakeUri(path);

            // Act
            var response = await _factory
                .WithWebHostBuilder(builder =>
                    builder.ConfigureTestServices(services =>
                        services.AddLogging(logBuilder =>
                            logBuilder.AddFilter<ApplicationInsightsLoggerProvider>(loggerCategory, LogLevel.Information))))
                .SendRequestAsync(requestUri);

            // Assert
            response.EnsureSuccessStatusCode();

            var items = _factory.sentItems;
            this._output.PrintTelemetryItems(items);
            Assert.Equal(3, items.Count);

            var reqs = items.GetTelemetryOfType<RequestTelemetry>();
            Assert.Single(reqs);
            var req = reqs[0];
            Assert.NotNull(req);

            var traces = items.GetTelemetryOfType<TraceTelemetry>();
            Assert.Equal(2, traces.Count);
            var trace1 = traces[0];
            var trace2 = traces[1];
            Assert.Equal(trace1.Context.Operation.ParentId, req.Id);

            TelemetryValidation.ValidateRequest(
                 requestTelemetry: req,
                 expectedResponseCode: "200",
                 expectedName: expectedName,
                 expectedUri: requestUri,
                 expectedSuccess: true);
        }

        [Fact]
        public async Task IloggerWarningOrAboveCapturedByDefault()
        {
            // Arrange
            var path = "Home";
            var expectedName = "GET Home/Get";
            var requestUri = _factory.MakeUri(path);

            // Act
            var response = await _factory.SendRequestAsync(requestUri);

            // Assert
            response.EnsureSuccessStatusCode();

            var items = _factory.sentItems;
            _output.PrintTelemetryItems(items);
            Assert.Equal(2, items.Count);

            var reqs = items.GetTelemetryOfType<RequestTelemetry>();
            Assert.Single(reqs);
            var req = reqs[0];
            Assert.NotNull(req);

            var traces = items.GetTelemetryOfType<TraceTelemetry>();
            Assert.Single(traces);
            var trace = traces[0];
            Assert.Equal(trace.Context.Operation.ParentId, req.Id);

            TelemetryValidation.ValidateRequest(
                 requestTelemetry: req,
                 expectedResponseCode: "200",
                 expectedName: expectedName,
                 expectedUri: requestUri,
                 expectedSuccess: true);
        }
    }
}
