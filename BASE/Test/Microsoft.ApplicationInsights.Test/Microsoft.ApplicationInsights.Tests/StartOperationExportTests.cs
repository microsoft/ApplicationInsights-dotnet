namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Core;
    using Azure.Core.Pipeline;
    using Azure.Monitor.OpenTelemetry.Exporter;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Extensions.DependencyInjection;
    using OpenTelemetry;
    using OpenTelemetry.Trace;
    using Xunit;

    /// <summary>
    /// Runs the export tests without other test classes in parallel: a concurrently active TracerProvider listening to the
    /// same ActivitySource makes both Azure Monitor samplers add "microsoft.sample_rate" to the same activity, which throws.
    /// </summary>
    [CollectionDefinition(nameof(StartOperationExportTests), DisableParallelization = true)]
    public class StartOperationExportTestsCollection
    {
    }

    /// <summary>
    /// Runs StartOperation/StopOperation through the Azure Monitor exporter and asserts on the exported envelopes,
    /// so fields set on the operation telemetry that the exporter does not pick up (or that land in customDimensions
    /// under unmapped keys) are caught.
    /// </summary>
    [Collection(nameof(StartOperationExportTests))]
    public class StartOperationExportTests : IDisposable
    {
        private static readonly string[] UnmappedKeys = { "OperationName", "messaging.destination", "http.method", "url.full" };

        private const string CallerActivitySourceName = "Microsoft.ApplicationInsights.Tests.StartOperationExport";
        private static readonly ActivitySource CallerActivitySource = new ActivitySource(CallerActivitySourceName);

        private readonly CapturingTransport transport = new CapturingTransport();
        private readonly TelemetryConfiguration configuration;
        private readonly TelemetryClient telemetryClient;

        public StartOperationExportTests()
        {
            this.configuration = new TelemetryConfiguration();
            this.configuration.SamplingRatio = 1.0f;
            this.configuration.TracesPerSecond = null;
            this.configuration.DisableOfflineStorage = true;

            // The exporter caches its transmitter (and transport) per connection string, so use a unique one per test.
            this.configuration.ConnectionString = "InstrumentationKey=" + Guid.NewGuid().ToString();
            this.configuration.ConfigureOpenTelemetryBuilder(b =>
            {
                b.Services.Configure<AzureMonitorExporterOptions>(o => o.Transport = this.transport);
                b.WithTracing(t => t.AddSource(CallerActivitySourceName));
            });
            this.telemetryClient = new TelemetryClient(this.configuration);
        }

        public void Dispose()
        {
            this.configuration.Dispose();
        }

        [Theory]
        [InlineData("Http", "https://example.com/api/items?id=1", "example.com", "200")]
        [InlineData("SQL", "SELECT 1", "myserver | mydb", "0")]
        [InlineData("Queue Message", "myqueue", "https://acct.queue.core.windows.net", "201")]
        [InlineData("Custom", "custom-data", "custom-target", "custom-rc")]
        public void StartOperationDependencyExportsTelemetryFields(string type, string data, string target, string resultCode)
        {
            using (var operation = this.telemetryClient.StartOperation<DependencyTelemetry>("StartOpExportDependency"))
            {
                operation.Telemetry.Type = type;
                operation.Telemetry.Data = data;
                operation.Telemetry.Target = target;
                operation.Telemetry.ResultCode = resultCode;
                operation.Telemetry.Properties["depProp"] = "depValue";
            }

            var envelope = this.ExportSingle("RemoteDependency");
            var baseData = GetBaseData(envelope);

            Assert.Equal("StartOpExportDependency", GetString(baseData, "name"));
            Assert.Equal(type, GetString(baseData, "type"));
            Assert.Equal(data, GetString(baseData, "data"));
            Assert.Equal(target, GetString(baseData, "target"));
            Assert.Equal(resultCode, GetString(baseData, "resultCode"));
            Assert.True(baseData.GetProperty("success").GetBoolean());
            Assert.Equal("depValue", GetString(baseData.GetProperty("properties"), "depProp"));
            AssertNoUnmappedKeys(baseData);
        }

        [Fact]
        public void StartOperationDependencyWithTelemetryObjectExportsTelemetryFields()
        {
            var dependency = new DependencyTelemetry("Http", "example.com", "GET /startop-export/dependency", "https://example.com/api") { ResultCode = "500", Success = false };

            using (this.telemetryClient.StartOperation(dependency))
            {
            }

            var baseData = GetBaseData(this.ExportSingle("RemoteDependency"));
            Assert.Equal("GET /startop-export/dependency", GetString(baseData, "name"));
            Assert.Equal("Http", GetString(baseData, "type"));
            Assert.Equal("https://example.com/api", GetString(baseData, "data"));
            Assert.Equal("example.com", GetString(baseData, "target"));
            Assert.Equal("500", GetString(baseData, "resultCode"));
            Assert.False(baseData.GetProperty("success").GetBoolean());
            AssertNoUnmappedKeys(baseData);
        }

        [Fact]
        public void StartOperationDependencyExportsOnlyExplicitlyAssignedOperationName()
        {
            using (this.telemetryClient.StartOperation<DependencyTelemetry>("DefaultOperationName"))
            {
            }

            using (var operation = this.telemetryClient.StartOperation<DependencyTelemetry>("AssignedOperationName"))
            {
                operation.Telemetry.Context.Operation.Name = "GET /parent";
            }

            this.telemetryClient.Flush();
            var envelopes = this.transport.Envelopes.Where(e => e.GetProperty("name").GetString() == "RemoteDependency").ToList();

            var defaultDependency = envelopes.Single(e => GetString(GetBaseData(e), "name") == "DefaultOperationName");
            Assert.Null(GetString(defaultDependency.GetProperty("tags"), "ai.operation.name"));

            var assignedDependency = envelopes.Single(e => GetString(GetBaseData(e), "name") == "AssignedOperationName");
            Assert.Equal("GET /parent", GetString(assignedDependency.GetProperty("tags"), "ai.operation.name"));
        }

        [Fact]
        public void StartOperationRequestExportsTelemetryFields()
        {
            using (var operation = this.telemetryClient.StartOperation<RequestTelemetry>("GET /startop-export/orders"))
            {
                operation.Telemetry.ResponseCode = "404";
                operation.Telemetry.Success = false;
                operation.Telemetry.Url = new Uri("https://myapp/orders?id=1");
                operation.Telemetry.Properties["reqProp"] = "reqValue";
                operation.Telemetry.Context.User.Id = "user1";
            }

            var envelope = this.ExportSingle("Request");
            var baseData = GetBaseData(envelope);

            Assert.Equal("GET /startop-export/orders", GetString(baseData, "name"));
            Assert.Equal("404", GetString(baseData, "responseCode"));
            Assert.False(baseData.GetProperty("success").GetBoolean());
            Assert.Equal("https://myapp/orders?id=1", GetString(baseData, "url"));
            Assert.Equal("reqValue", GetString(baseData.GetProperty("properties"), "reqProp"));

            var tags = envelope.GetProperty("tags");
            Assert.Equal("GET /startop-export/orders", GetString(tags, "ai.operation.name"));
            Assert.Equal("user1", GetString(tags, "ai.user.id"));
            AssertNoUnmappedKeys(baseData);
        }

        [Fact]
        public void StartOperationRequestWithoutSuccessIsExportedAsSuccessful()
        {
            using (this.telemetryClient.StartOperation<RequestTelemetry>("GET /startop-export/no-success"))
            {
            }

            var baseData = GetBaseData(this.ExportSingle("Request"));
            Assert.True(baseData.GetProperty("success").GetBoolean());
            AssertNoUnmappedKeys(baseData);
        }

        [Fact]
        public void StartOperationWithCallerActivityAppliesExplicitDependencyFields()
        {
            using (var activity = CallerActivitySource.StartActivity("CallerDependency", ActivityKind.Client))
            {
                Assert.NotNull(activity);

                using (var operation = this.telemetryClient.StartOperation<DependencyTelemetry>(activity))
                {
                    operation.Telemetry.Type = "Custom";
                    operation.Telemetry.Data = "custom-data";
                    operation.Telemetry.Target = "custom-target";
                    operation.Telemetry.ResultCode = "500";
                    operation.Telemetry.Success = false;
                }
            }

            var baseData = GetBaseData(this.ExportSingle("RemoteDependency"));
            Assert.Equal("CallerDependency", GetString(baseData, "name"));
            Assert.Equal("Custom", GetString(baseData, "type"));
            Assert.Equal("custom-data", GetString(baseData, "data"));
            Assert.Equal("custom-target", GetString(baseData, "target"));
            Assert.Equal("500", GetString(baseData, "resultCode"));
            Assert.False(baseData.GetProperty("success").GetBoolean());
        }

        [Fact]
        public void StartOperationWithCallerActivityKeepsInstrumentationWhenTelemetryIsUntouched()
        {
            using (var activity = CallerActivitySource.StartActivity("GET", ActivityKind.Client))
            {
                Assert.NotNull(activity);
                activity.SetTag("http.request.method", "GET");
                activity.SetTag("url.full", "https://example.com/api/items");
                activity.SetTag("server.address", "example.com");
                activity.SetTag("http.response.status_code", 200);

                using (this.telemetryClient.StartOperation<DependencyTelemetry>(activity))
                {
                }
            }

            var baseData = GetBaseData(this.ExportSingle("RemoteDependency"));
            Assert.Equal("Http", GetString(baseData, "type"));
            Assert.Equal("https://example.com/api/items", GetString(baseData, "data"));
            Assert.Equal("example.com", GetString(baseData, "target"));
            Assert.Equal("200", GetString(baseData, "resultCode"));
        }

        private static JsonElement GetBaseData(JsonElement envelope)
        {
            return envelope.GetProperty("data").GetProperty("baseData");
        }

        private static string GetString(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var value) ? value.ToString() : null;
        }

        private static void AssertNoUnmappedKeys(JsonElement baseData)
        {
            if (!baseData.TryGetProperty("properties", out var properties))
            {
                return;
            }

            foreach (var key in UnmappedKeys)
            {
                Assert.False(properties.TryGetProperty(key, out _), $"'{key}' was exported as a custom property.");
            }
        }

        private JsonElement ExportSingle(string envelopeName)
        {
            this.telemetryClient.Flush();
            return this.transport.Envelopes.Single(e => e.GetProperty("name").GetString() == envelopeName);
        }

        /// <summary>
        /// Captures the Azure Monitor exporter payloads instead of sending them to the ingestion endpoint.
        /// </summary>
        private sealed class CapturingTransport : HttpPipelineTransport
        {
            private readonly object gate = new object();
            private readonly List<JsonElement> envelopes = new List<JsonElement>();

            public IReadOnlyList<JsonElement> Envelopes
            {
                get
                {
                    lock (this.gate)
                    {
                        return this.envelopes.ToList();
                    }
                }
            }

            public override Request CreateRequest()
            {
                return HttpClientTransport.Shared.CreateRequest();
            }

            public override void Process(HttpMessage message)
            {
                this.Capture(message.Request);
                message.Response = new OkResponse();
            }

            public override ValueTask ProcessAsync(HttpMessage message)
            {
                this.Process(message);
                return default;
            }

            private void Capture(Request request)
            {
                if (request.Content == null)
                {
                    return;
                }

                using (var buffer = new MemoryStream())
                {
                    request.Content.WriteTo(buffer, CancellationToken.None);
                    buffer.Position = 0;

                    Stream payload = buffer;
                    if (request.Headers.TryGetValue("Content-Encoding", out var encoding) &&
                        encoding.IndexOf("gzip", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        payload = new GZipStream(buffer, CompressionMode.Decompress, leaveOpen: true);
                    }

                    string text;
                    using (var reader = new StreamReader(payload, Encoding.UTF8))
                    {
                        text = reader.ReadToEnd();
                    }

                    lock (this.gate)
                    {
                        foreach (var line in text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            using (var document = JsonDocument.Parse(line))
                            {
                                this.envelopes.Add(document.RootElement.Clone());
                            }
                        }
                    }
                }
            }
        }

        private sealed class OkResponse : Response
        {
            public override int Status => 200;

            public override string ReasonPhrase => "OK";

            public override Stream ContentStream { get; set; }

            public override string ClientRequestId { get; set; } = Guid.NewGuid().ToString();

            public override void Dispose()
            {
            }

            protected override bool ContainsHeader(string name) => false;

            protected override IEnumerable<HttpHeader> EnumerateHeaders() => Enumerable.Empty<HttpHeader>();

            protected override bool TryGetHeader(string name, out string value)
            {
                value = null;
                return false;
            }

            protected override bool TryGetHeaderValues(string name, out IEnumerable<string> values)
            {
                values = null;
                return false;
            }
        }
    }
}
