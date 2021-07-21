using IntegrationTests.Tests.TestFramework;
using IntegrationTests.WebApp;

using Microsoft.ApplicationInsights.DataContracts;

using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests.Tests
{
    public class RequestCorrelationHeaderTest : IClassFixture<CustomWebApplicationFactory<Startup>>
    {
        private readonly CustomWebApplicationFactory<Startup> _factory;
        private readonly ITestOutputHelper _output;

        public RequestCorrelationHeaderTest(CustomWebApplicationFactory<Startup> factory, ITestOutputHelper output)
        {
            this._output = output;
            _factory = factory;
            _factory.sentItems.Clear();
        }

        [Fact]
        public async Task RequestSuccessWithTraceParent()
        {
            // Arrange
            var path = "Home";
            var expectedName = "GET Home/Get";
            var requestUri = _factory.MakeUri(path);
            var requestHeaders = new Dictionary<string, string>()
            {
                { "traceparent", "00-4e3083444c10254ba40513c7316332eb-e2a5f830c0ee2c46-00"}
            };

            // Act
            var response = await _factory.SendRequestAsync(requestUri, requestHeaders);

            // Assert
            response.EnsureSuccessStatusCode();
            await this._output.PrintResponseContentAsync(response);

            var items = _factory.sentItems;
            _output.PrintTelemetryItems(items);
            Assert.Equal(2, items.Count); // 1 Trace from Ilogger, 1 Request

            var reqs = items.GetTelemetryOfType<RequestTelemetry>();
            Assert.Single(reqs);
            var req = reqs[0];
            var traces = items.GetTelemetryOfType<TraceTelemetry>();
            Assert.Single(traces);
            var trace = traces[0];
            Assert.NotNull(req);
            Assert.NotNull(trace);

            Assert.Equal("4e3083444c10254ba40513c7316332eb", req.Context.Operation.Id);
            Assert.Equal("e2a5f830c0ee2c46", req.Context.Operation.ParentId);
            Assert.Equal("4e3083444c10254ba40513c7316332eb", trace.Context.Operation.Id);
            Assert.Equal(req.Id, trace.Context.Operation.ParentId);

            TelemetryValidation.ValidateRequest(
                 requestTelemetry: req,
                 expectedResponseCode: "200",
                 expectedName: expectedName,
                 expectedUri: requestUri,
                 expectedSuccess: true);
        }

        [Fact]
        public async Task RequestFailedWithTraceParent()
        {
            // Arrange
            var path = "Home/Error";
            var expectedName = "GET Home/Error";
            var requestUri = _factory.MakeUri(path);
            var requestHeaders = new Dictionary<string, string>()
            {
                { "traceparent", "00-4e3083444c10254ba40513c7316332eb-e2a5f830c0ee2c46-00"}
            };

            // Act
            var response = await _factory.SendRequestAsync(requestUri, requestHeaders);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            await this._output.PrintResponseContentAsync(response);

            var items = _factory.sentItems;
            _output.PrintTelemetryItems(items);
            Assert.Equal(2, items.Count); // 1 Trace from Ilogger, 1 Request

            var reqs = items.GetTelemetryOfType<RequestTelemetry>();
            Assert.Single(reqs);
            var req = reqs[0];
            var exceptions = items.GetTelemetryOfType<ExceptionTelemetry>();
            Assert.Single(exceptions);
            var exception = exceptions[0];
            Assert.NotNull(req);
            Assert.NotNull(exception);

            Assert.Equal("4e3083444c10254ba40513c7316332eb", req.Context.Operation.Id);
            Assert.Equal("4e3083444c10254ba40513c7316332eb", exception.Context.Operation.Id);
            Assert.Equal("e2a5f830c0ee2c46", req.Context.Operation.ParentId);
            Assert.Equal(req.Id, exception.Context.Operation.ParentId);

            TelemetryValidation.ValidateRequest(
                 requestTelemetry: req,
                 expectedResponseCode: "500",
                 expectedName: expectedName,
                 expectedUri: requestUri,
                 expectedSuccess: false);
        }

        [Fact]
        public async Task RequestSuccessWithW3CCompatibleRequestId()
        {
            // Arrange
            var path = "Home";
            var expectedName = "GET Home/Get";
            var requestUri = _factory.MakeUri(path);
            var requestHeaders = new Dictionary<string, string>()
            {
                { "Request-Id", "|40d1a5a08a68c0998e4a3b7c91915ca6.b9e41c35_1."}
            };

            // Act
            var response = await _factory.SendRequestAsync(requestUri, requestHeaders);

            // Assert
            response.EnsureSuccessStatusCode();
            await this._output.PrintResponseContentAsync(response);

            var items = _factory.sentItems;
            _output.PrintTelemetryItems(items);
            Assert.Equal(2, items.Count); // 1 Trace from Ilogger, 1 Request

            var reqs = items.GetTelemetryOfType<RequestTelemetry>();
            Assert.Single(reqs);
            var req = reqs[0];
            var traces = items.GetTelemetryOfType<TraceTelemetry>();
            Assert.Single(traces);
            var trace = traces[0];
            Assert.NotNull(req);
            Assert.NotNull(trace);

            Assert.Equal("40d1a5a08a68c0998e4a3b7c91915ca6", req.Context.Operation.Id);
            Assert.Equal("40d1a5a08a68c0998e4a3b7c91915ca6", trace.Context.Operation.Id);

            Assert.Equal("|40d1a5a08a68c0998e4a3b7c91915ca6.b9e41c35_1.", req.Context.Operation.ParentId);
            Assert.Equal(req.Id, trace.Context.Operation.ParentId);

            TelemetryValidation.ValidateRequest(
                 requestTelemetry: req,
                 expectedResponseCode: "200",
                 expectedName: expectedName,
                 expectedUri: requestUri,
                 expectedSuccess: true);
        }

        [Fact]
        public async Task RequestFailedWithW3CCompatibleRequestId()
        {
            // Arrange
            var path = "Home/Error";
            var expectedName = "GET Home/Error";
            var requestUri = _factory.MakeUri(path);
            var requestHeaders = new Dictionary<string, string>()
            {
                { "Request-Id", "|40d1a5a08a68c0998e4a3b7c91915ca6.b9e41c35_1."}
            };

            // Act
            var response = await _factory.SendRequestAsync(requestUri, requestHeaders);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            await this._output.PrintResponseContentAsync(response);

            var items = _factory.sentItems;
            _output.PrintTelemetryItems(items);
            Assert.Equal(2, items.Count); // 1 Trace from Ilogger, 1 Request

            var reqs = items.GetTelemetryOfType<RequestTelemetry>();
            Assert.Single(reqs);
            var req = reqs[0];
            var exceptions = items.GetTelemetryOfType<ExceptionTelemetry>();
            Assert.Single(exceptions);
            var exception = exceptions[0];
            Assert.NotNull(req);
            Assert.NotNull(exception);

            Assert.Equal("40d1a5a08a68c0998e4a3b7c91915ca6", req.Context.Operation.Id);
            Assert.Equal("40d1a5a08a68c0998e4a3b7c91915ca6", exception.Context.Operation.Id);

            Assert.Equal(req.Id, exception.Context.Operation.ParentId);
            Assert.Equal("|40d1a5a08a68c0998e4a3b7c91915ca6.b9e41c35_1.", req.Context.Operation.ParentId);
            TelemetryValidation.ValidateRequest(
                 requestTelemetry: req,
                 expectedResponseCode: "500",
                 expectedName: "GET " + path,
                 expectedUri: requestUri,
                 expectedSuccess: false);
        }

        [Fact]
        public async Task RequestSuccessWithNonW3CCompatibleRequestId()
        {
            // Arrange
            var path = "Home";
            var expectedName = "GET Home/Get";
            var requestUri = _factory.MakeUri(path);
            var requestHeaders = new Dictionary<string, string>()
            {
                { "Request-Id", "|noncompatible.b9e41c35_1."}
            };

            // Act
            var response = await _factory.SendRequestAsync(requestUri, requestHeaders);

            // Assert
            response.EnsureSuccessStatusCode();
            await this._output.PrintResponseContentAsync(response);

            var items = _factory.sentItems;
            _output.PrintTelemetryItems(items);
            Assert.Equal(2, items.Count); // 1 Trace from Ilogger, 1 Request

            var reqs = items.GetTelemetryOfType<RequestTelemetry>();
            Assert.Single(reqs);
            var req = reqs[0];
            var traces = items.GetTelemetryOfType<TraceTelemetry>();
            Assert.Single(traces);
            var trace = traces[0];
            Assert.NotNull(req);
            Assert.NotNull(trace);

            Assert.NotEqual("noncompatible", req.Context.Operation.Id);
            Assert.NotEqual("noncompatible", trace.Context.Operation.Id);

            Assert.Equal("|noncompatible.b9e41c35_1.", req.Context.Operation.ParentId);
            Assert.Equal(req.Id, trace.Context.Operation.ParentId);
            Assert.Equal("noncompatible", req.Properties["ai_legacyRootId"]);

            TelemetryValidation.ValidateRequest(
                 requestTelemetry: req,
                 expectedResponseCode: "200",
                 expectedName: expectedName,
                 expectedUri: requestUri,
                 expectedSuccess: true);
        }
    }
}
