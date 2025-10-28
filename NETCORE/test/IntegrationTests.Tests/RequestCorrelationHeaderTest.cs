using IntegrationTests.WebApp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace IntegrationTests.Tests
{
    public partial class RequestCollectionTest : IClassFixture<CustomWebApplicationFactory<Startup>>
    {
        [Fact]
        public async Task RequestSuccessWithTraceParent()
        {
            // Arrange
            var client = _factory.CreateClient();
            var path = "Home";
            var url = client.BaseAddress + path;

            // Act
            Dictionary<string, string> requestHeaders = new Dictionary<string, string>()
            {
                { "traceparent", "00-4e3083444c10254ba40513c7316332eb-e2a5f830c0ee2c46-00"}
            };
            var request = CreateRequestMessage(requestHeaders);
            request.RequestUri = new Uri(url);
            var response = await client.SendAsync(request);

            // Assert
            response.EnsureSuccessStatusCode();

            this.output.WriteLine(await response.Content.ReadAsStringAsync());

            await WaitForTelemetryToArrive(expectedItemCount: 2);

            var items = _factory.Telemetry.Items;
            PrintItems(items);
            // 1 Trace from Ilogger, 1 Request
            Assert.Equal(2, items.Count);

            var reqs = _factory.Telemetry.GetTelemetryOfType<RequestTelemetryEnvelope>();
            Assert.Single(reqs);
            var req = reqs[0];
            var traces = _factory.Telemetry.GetTelemetryOfType<TraceTelemetryEnvelope>();
            Assert.Single(traces);
            var trace = traces[0];
            Assert.NotNull(req);
            Assert.NotNull(trace);

            Assert.False(string.IsNullOrEmpty(req.OperationId));
            Assert.Equal(req.OperationId, trace.OperationId);
            Assert.Equal(req.Id, trace.OperationParentId);

            Assert.Equal("sample warning", trace.Message);
            Assert.True(trace.Properties.TryGetValue("CategoryName", out var traceCategory));
            Assert.Equal("IntegrationTests.WebApp.Controllers.HomeController", traceCategory);

            ValidateRequest(
                    requestTelemetry: req,
                    expectedResponseCode: "200",
                expectedName: "GET " + path,
                    expectedUrl: url,
                    expectedSuccess: true);
        }

        [Fact]
        public async Task DependencyTelemetryCapturedForHttpClientRequest()
        {
            var client = _factory.CreateClient();
            var path = "Home/Dependency";
            var url = client.BaseAddress + path;

            var response = await client.GetAsync(path);
            response.EnsureSuccessStatusCode();

            await WaitForTelemetryToArrive(expectedItemCount: 2);

            var items = _factory.Telemetry.Items;
            PrintItems(items);
            Assert.Equal(2, items.Count);

            var requests = _factory.Telemetry.GetTelemetryOfType<RequestTelemetryEnvelope>();
            Assert.Single(requests);
            var request = requests[0];

            var dependencies = _factory.Telemetry.GetTelemetryOfType<DependencyTelemetryEnvelope>();
            Assert.Single(dependencies);
            var dependency = dependencies[0];

            Assert.Equal(request.OperationId, dependency.OperationId);
            Assert.Equal(request.Id, dependency.OperationParentId);
            Assert.False(string.IsNullOrEmpty(dependency.Id));
            Assert.Equal("Http", dependency.Type);
            Assert.Contains("www.bing.com", dependency.Target, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("www.bing.com", dependency.Data, StringComparison.OrdinalIgnoreCase);
            Assert.False(string.IsNullOrEmpty(dependency.ResultCode));

            ValidateRequest(
                 requestTelemetry: request,
                 expectedResponseCode: "200",
                 expectedName: "GET " + path,
                 expectedUrl: url,
                 expectedSuccess: true);
        }

        [Fact]
        public async Task RequestFailedWithTraceParent()
        {
            // Arrange
            var client = _factory.CreateClient();
            var path = "Home/Error";
            var url = client.BaseAddress + path;

            // Act
            Dictionary<string, string> requestHeaders = new Dictionary<string, string>()
            {
                { "traceparent", "00-4e3083444c10254ba40513c7316332eb-e2a5f830c0ee2c46-00"}
            };
            var request = CreateRequestMessage(requestHeaders);
            request.RequestUri = new Uri(url);

            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            await WaitForTelemetryToArrive(expectedItemCount: 2);
            var items = _factory.Telemetry.Items;
            PrintItems(items);
            Assert.Equal(2, items.Count);

            var reqs = _factory.Telemetry.GetTelemetryOfType<RequestTelemetryEnvelope>();
            Assert.Single(reqs);
            var req = reqs[0];
            var exceptions = _factory.Telemetry.GetTelemetryOfType<ExceptionTelemetryEnvelope>();
            Assert.Single(exceptions);
            var exception = exceptions[0];
            Assert.NotNull(req);
            Assert.NotNull(exception);

            Assert.False(string.IsNullOrEmpty(req.OperationId));
            Assert.Equal(req.OperationId, exception.OperationId);
            Assert.Equal(req.Id, exception.OperationParentId);

            Assert.Equal("sample exception", exception.Message);
            Assert.Equal("System.Exception", exception.TypeName);

            ValidateRequest(
                 requestTelemetry: req,
                 expectedResponseCode: "500",
                 expectedName: "GET " + path,
                 expectedUrl: url,
                 expectedSuccess: false);
        }

        [Fact]
        public async Task RequestSuccessIgnoresLegacyRequestIdHeader()
        {
            // Arrange
            var client = _factory.CreateClient();
            var path = "Home";
            var url = client.BaseAddress + path;

            // Act
            Dictionary<string, string> requestHeaders = new Dictionary<string, string>();
            var request = CreateRequestMessage(requestHeaders);
            request.RequestUri = new Uri(url);

            var response = await client.SendAsync(request);

            // Assert
            response.EnsureSuccessStatusCode();

            await WaitForTelemetryToArrive(expectedItemCount: 2);
            var items = _factory.Telemetry.Items;
            PrintItems(items);
            // 1 Trace from Ilogger, 1 Request
            Assert.Equal(2, items.Count);

            var reqs = _factory.Telemetry.GetTelemetryOfType<RequestTelemetryEnvelope>();
            Assert.Single(reqs);
            var req = reqs[0];
            var traces = _factory.Telemetry.GetTelemetryOfType<TraceTelemetryEnvelope>();
            Assert.Single(traces);
            var trace = traces[0];
            Assert.NotNull(req);
            Assert.NotNull(trace);

            Assert.False(string.IsNullOrEmpty(req.OperationId));
            Assert.True(string.IsNullOrEmpty(req.OperationParentId));
            Assert.Equal(req.OperationId, trace.OperationId);
            Assert.Equal(req.Id, trace.OperationParentId);

            ValidateRequest(
                 requestTelemetry: req,
                 expectedResponseCode: "200",
                 expectedName: "GET " + path,
                 expectedUrl: url,
                 expectedSuccess: true);
        }

        [Fact]
        public async Task RequestFailedIgnoresLegacyRequestIdHeader()
        {
            // Arrange
            var client = _factory.CreateClient();
            var path = "Home/Error";
            var url = client.BaseAddress + path;

            // Act
            Dictionary<string, string> requestHeaders = new Dictionary<string, string>();
            var request = CreateRequestMessage(requestHeaders);
            request.RequestUri = new Uri(url);

            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            await WaitForTelemetryToArrive(expectedItemCount: 2);
            var items = _factory.Telemetry.Items;
            PrintItems(items);
            Assert.Equal(2, items.Count);

            var reqs = _factory.Telemetry.GetTelemetryOfType<RequestTelemetryEnvelope>();
            Assert.Single(reqs);
            var req = reqs[0];
            var exceptions = _factory.Telemetry.GetTelemetryOfType<ExceptionTelemetryEnvelope>();
            Assert.Single(exceptions);
            var exception = exceptions[0];
            Assert.NotNull(req);
            Assert.NotNull(exception);

            Assert.False(string.IsNullOrEmpty(req.OperationId));
            Assert.True(string.IsNullOrEmpty(req.OperationParentId));
            Assert.Equal(req.OperationId, exception.OperationId);
            Assert.Equal(req.Id, exception.OperationParentId);
            Assert.False(req.Properties.ContainsKey("ai_legacyRootId"));
            ValidateRequest(
                 requestTelemetry: req,
                 expectedResponseCode: "500",
                 expectedName: "GET " + path,
                 expectedUrl: url,
                 expectedSuccess: false);
        }

        [Fact]
        public async Task RequestSuccessIgnoresNonConformantRequestIdHeader()
        {
            // Arrange
            var client = _factory.CreateClient();
            var path = "Home";
            var url = client.BaseAddress + path;

            // Act
            Dictionary<string, string> requestHeaders = new Dictionary<string, string>()
                {
                    { "Request-Id", "|noncompatible.b9e41c35_1."}
                };
            var request = CreateRequestMessage(requestHeaders);
            request.RequestUri = new Uri(url);

            var response = await client.SendAsync(request);

            // Assert
            response.EnsureSuccessStatusCode();

            await WaitForTelemetryToArrive(expectedItemCount: 2);
            var items = _factory.Telemetry.Items;
            PrintItems(items);
            // 1 Trace from Ilogger, 1 Request
            Assert.Equal(2, items.Count);

            var reqs = _factory.Telemetry.GetTelemetryOfType<RequestTelemetryEnvelope>();
            Assert.Single(reqs);
            var req = reqs[0];
            var traces = _factory.Telemetry.GetTelemetryOfType<TraceTelemetryEnvelope>();
            Assert.Single(traces);
            var trace = traces[0];
            Assert.NotNull(req);
            Assert.NotNull(trace);

            Assert.False(string.IsNullOrEmpty(req.OperationId));
            Assert.Equal(req.OperationId, trace.OperationId);
            Assert.Equal(req.Id, trace.OperationParentId);

            ValidateRequest(
                 requestTelemetry: req,
                 expectedResponseCode: "200",
              expectedName: "GET " + path,
                 expectedUrl: url,
                 expectedSuccess: true);
        }
    }
}
