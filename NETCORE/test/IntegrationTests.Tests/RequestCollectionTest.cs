using IntegrationTests.Tests.TestFramework;
using IntegrationTests.WebApp;

using Microsoft.ApplicationInsights.DataContracts;

using System.Net;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests.Tests
{
    public class RequestCollectionTest : IClassFixture<CustomWebApplicationFactory<Startup>>
    {
        private readonly CustomWebApplicationFactory<Startup> _factory;
        private readonly ITestOutputHelper _output;

        public RequestCollectionTest(CustomWebApplicationFactory<Startup> factory, ITestOutputHelper output)
        {
            this._output = output;
            _factory = factory;
            _factory.sentItems.Clear();
        }

        [Fact]
        public async Task RequestSuccess()
        {
            // Arrange
            var path = "Home/Empty";
            var requestUri = _factory.MakeUri(path);

            // Act
            var response = await _factory.SendRequestAsync(requestUri);

            // Assert
            response.EnsureSuccessStatusCode();

            var items = _factory.sentItems;
            _output.PrintTelemetryItems(items);
            Assert.Equal(1, items.Count);

            var reqs = items.GetTelemetryOfType<RequestTelemetry>();
            Assert.Single(reqs);
            var req = reqs[0];
            Assert.NotNull(req);

            TelemetryValidation.ValidateRequest(
                 requestTelemetry: req,
                 expectedResponseCode: "200",
                 expectedName: "GET " + path,
                 expectedUri: requestUri,
                 expectedSuccess: true);
        }

        [Fact]
        public async Task RequestSuccessActionWithParameter()
        {
            // Arrange
            var path = "Home/5";
            var expectedName = "GET Home/Get [id]";
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
            Assert.NotNull(trace);

            TelemetryValidation.ValidateRequest(
                 requestTelemetry: req,
                 expectedResponseCode: "200",
                 expectedName: expectedName,
                 expectedUri: requestUri,
                 expectedSuccess: true);
        }

        [Fact]
        public async Task RequestFailed()
        {
            // Arrange
            var path = "Home/Error";
            var requestUri = _factory.MakeUri(path);

            // Act
            var response = await _factory.SendRequestAsync(requestUri);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            var items = _factory.sentItems;
            _output.PrintTelemetryItems(items);
            Assert.Equal(2, items.Count);

            var reqs = items.GetTelemetryOfType<RequestTelemetry>();
            var exceptions = items.GetTelemetryOfType<ExceptionTelemetry>();
            Assert.Single(reqs);
            Assert.Single(exceptions);

            var req = reqs[0];
            var exc = exceptions[0];
            Assert.NotNull(req);
            Assert.NotNull(exc);

            Assert.Equal(exc.Context.Operation.Id, req.Context.Operation.Id);
            TelemetryValidation.ValidateRequest(
                 requestTelemetry: req,
                 expectedResponseCode: "500",
                 expectedName: "GET " + path,
                 expectedUri: requestUri,
                 expectedSuccess: false);
        }

        [Fact]
        public async Task RequestNonExistentPage()
        {
            // Arrange
            var path = "Nonexistent";
            var requestUri = _factory.MakeUri(path);

            // Act
            var response = await _factory.SendRequestAsync(requestUri);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            var items = _factory.sentItems;
            _output.PrintTelemetryItems(items);
            Assert.Equal(1, items.Count);

            var reqs = items.GetTelemetryOfType<RequestTelemetry>();
            Assert.Single(reqs);
            var req = reqs[0];
            Assert.NotNull(req);

            TelemetryValidation.ValidateRequest(
                 requestTelemetry: req,
                 expectedResponseCode: "404",
                 expectedName: "GET /" + path,
                 expectedUri: requestUri,
                 expectedSuccess: false);
        }
    }
}
