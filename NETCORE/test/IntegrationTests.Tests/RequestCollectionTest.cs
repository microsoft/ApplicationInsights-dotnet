using IntegrationTests.WebApp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests.Tests
{
    public partial class RequestCollectionTest : IClassFixture<CustomWebApplicationFactory<Startup>>
    {
        private readonly CustomWebApplicationFactory<Startup> _factory;

        protected readonly ITestOutputHelper output;

        public RequestCollectionTest(CustomWebApplicationFactory<Startup> factory, ITestOutputHelper output)
        {
            this.output = output;
            _factory = factory;
            _factory.Telemetry.Clear();
        }

        [Fact]
        public async Task RequestSuccess()
        {
            // Arrange
            var client = _factory.CreateClient();
            var path = "Home/GetEmpty";
            var url = client.BaseAddress + path;

            // Act
            var request = CreateRequestMessage();
            request.RequestUri = new Uri(url);
            var response = await client.SendAsync(request);

            // Assert
            response.EnsureSuccessStatusCode();

            await WaitForTelemetryToArrive(expectedItemCount: 1);

          var items = _factory.Telemetry.Items;
            PrintItems(items);
            Assert.Equal(1, items.Count);

          var reqs = _factory.Telemetry.GetTelemetryOfType<RequestTelemetryEnvelope>();
            Assert.Single(reqs);
            var req = reqs[0];
            Assert.NotNull(req);
            ValidateRequest(
                 requestTelemetry: req,
                 expectedResponseCode: "200",
                 expectedName: "GET " + path,
                 expectedUrl: url,
                 expectedSuccess: true);
        }

        [Fact]
        public async Task RequestSuccessActionWithParameter()
        {
            // Arrange
            var client = _factory.CreateClient();
            var path = "Home/5";
            var expectedName = "GET Home/{id}";
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
            Assert.NotNull(trace);

            ValidateRequest(
                 requestTelemetry: req,
                 expectedResponseCode: "200",
                 expectedName: expectedName,
                 expectedUrl: url,
                 expectedSuccess: true);
        }

        [Fact]
        public async Task RequestFailed()
        {
            // Arrange
            var client = _factory.CreateClient();
            var path = "Home/Error";
            var url = client.BaseAddress + path;

            // Act
            var request = CreateRequestMessage();
            request.RequestUri = new Uri(url);
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            await WaitForTelemetryToArrive(expectedItemCount: 2);

          var items = _factory.Telemetry.Items;
            PrintItems(items);
            Assert.Equal(2, items.Count);

          var reqs = _factory.Telemetry.GetTelemetryOfType<RequestTelemetryEnvelope>();
          var exceptions = _factory.Telemetry.GetTelemetryOfType<ExceptionTelemetryEnvelope>();
            Assert.Single(reqs);
            Assert.Single(exceptions);

            var req = reqs[0];
            var exc = exceptions[0];
            Assert.NotNull(req);
            Assert.NotNull(exc);

        // TODO: Validate Exception.

          Assert.Equal(exc.OperationId, req.OperationId);
            ValidateRequest(
                 requestTelemetry: req,
                 expectedResponseCode: "500",
                 expectedName: "GET " + path,
                 expectedUrl: url,
                 expectedSuccess: false);
        }

        [Fact]
        public async Task RequestNonExistentPage()
        {
            // Arrange
            var client = _factory.CreateClient();
            var path = "Nonexistent";
            var url = client.BaseAddress + path;

            // Act
            var request = CreateRequestMessage();
            request.RequestUri = new Uri(url);
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            await WaitForTelemetryToArrive(expectedItemCount: 1);

            var items = _factory.Telemetry.Items;
            PrintItems(items);
            Assert.Equal(1, items.Count);

            var reqs = _factory.Telemetry.GetTelemetryOfType<RequestTelemetryEnvelope>();
            Assert.Single(reqs);
            var req = reqs[0];
            Assert.NotNull(req);

            ValidateRequest(
                 requestTelemetry: req,
                 expectedResponseCode: "404",
                 expectedName: "GET /" + path,
                 expectedUrl: url,
                 expectedSuccess: false);
        }

        private async Task WaitForTelemetryToArrive(int expectedItemCount)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
            while (DateTime.UtcNow < deadline)
            {
                await Task.Delay(100);

                var currentCount = _factory.Telemetry.Items.Count;
                if (currentCount >= expectedItemCount)
                {
                    return;
                }
            }

            // Allow any final exporter flush.
            await Task.Delay(200);
        }

        private void ValidateRequest(RequestTelemetryEnvelope requestTelemetry,
            string expectedResponseCode,
            string expectedName,
            string expectedUrl,
            bool expectedSuccess)
        {
            Assert.Equal(expectedResponseCode, requestTelemetry.ResponseCode);
            Assert.Equal(expectedName, requestTelemetry.Name);
            Assert.Equal(expectedSuccess, requestTelemetry.Success);
            Assert.Equal(expectedUrl, requestTelemetry.Url?.ToString());
            Assert.True(requestTelemetry.Duration.TotalMilliseconds > 0);
            // requestTelemetry.Timestamp
        }

        private HttpRequestMessage CreateRequestMessage(Dictionary<string, string> requestHeaders = null)
        {
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage();
            httpRequestMessage.Method = HttpMethod.Get;
            if (requestHeaders != null)
            {
                foreach (var h in requestHeaders)
                {
                    httpRequestMessage.Headers.Add(h.Key, h.Value);
                }
            }

            return httpRequestMessage;
        }

        private void PrintItems(IReadOnlyList<AzureMonitorTelemetryEnvelope> items)
        {
            int i = 1;
            foreach (var item in items)
            {
                this.output.WriteLine("Item " + (i++) + ".");

                this.output.WriteLine("OperationId:" + item.OperationId);
                if (!string.IsNullOrEmpty(item.OperationParentId))
                {
                    this.output.WriteLine("OperationParentId:" + item.OperationParentId);
                }

                if (item is RequestTelemetryEnvelope req)
                {
                    this.output.WriteLine("RequestTelemetry");
                    this.output.WriteLine(req.Name);
                    this.output.WriteLine(req.Duration.ToString());
                    this.output.WriteLine("RequestId:" + req.Id);
                }
                else if (item is TraceTelemetryEnvelope trace)
                {
                    this.output.WriteLine("TraceTelemetry");
                    this.output.WriteLine(trace.Message);
                }
                else if (item is ExceptionTelemetryEnvelope exc)
                {
                    this.output.WriteLine("ExceptionTelemetry");
                    this.output.WriteLine(exc.Message);
                }
                PrintProperties(item.Properties);
                this.output.WriteLine("----------------------------");
            }
        }

        private void PrintProperties(IReadOnlyDictionary<string, string> itemProps)
        {
            foreach (var prop in itemProps)
            {
                this.output.WriteLine(prop.Key + ":" + prop.Value);
            }
        }

    }
}
