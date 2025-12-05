using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using Azure;
using Azure.Core;
using Azure.Core.Pipeline;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace IntegrationTests.Tests
{
    /// <summary>
    /// Collects telemetry items exported by the Azure Monitor exporter so tests can assert on the payload.
    /// </summary>
    internal sealed class TelemetryCollector
    {
        private readonly Dictionary<string, List<AzureMonitorTelemetryEnvelope>> itemsByOperation = new(StringComparer.Ordinal);
        private readonly object gate = new();
        private string? currentOperationId;

        public IReadOnlyList<AzureMonitorTelemetryEnvelope> Items
        {
            get
            {
                lock (this.gate)
                {
                    return this.GetCurrentOperationItemsUnsafe().ToList();
                }
            }
        }

        public void Clear()
        {
            lock (this.gate)
            {
                this.itemsByOperation.Clear();
                this.currentOperationId = null;
            }
        }

        public IReadOnlyList<TEnvelope> GetTelemetryOfType<TEnvelope>()
            where TEnvelope : AzureMonitorTelemetryEnvelope
        {
            lock (this.gate)
            {
                return this.GetCurrentOperationItemsUnsafe()
                    .OfType<TEnvelope>()
                    .ToList();
            }
        }

        /// <summary>
        /// Gets all telemetry items of the specified type across ALL operations (not just current operation).
        /// Used by WorkerService tests that track multiple background operations.
        /// </summary>
        public IReadOnlyList<TEnvelope> GetAllTelemetryOfType<TEnvelope>()
            where TEnvelope : AzureMonitorTelemetryEnvelope
        {
            lock (this.gate)
            {
                return this.itemsByOperation.Values
                    .SelectMany(list => list)
                    .OfType<TEnvelope>()
                    .ToList();
            }
        }

        /// <summary>
        /// Gets the total count of all telemetry items across ALL operations.
        /// Used by WorkerService tests.
        /// </summary>
        public int GetTotalItemCount()
        {
            lock (this.gate)
            {
                return this.itemsByOperation.Values.Sum(list => list.Count);
            }
        }

        internal void AddRange(IEnumerable<AzureMonitorTelemetryEnvelope> telemetryItems)
        {
            lock (this.gate)
            {
                foreach (var item in telemetryItems)
                {
                    if (!this.ShouldStore(item))
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(item.OperationId))
                    {
                        continue;
                    }

                    if (!this.itemsByOperation.TryGetValue(item.OperationId, out var bucket))
                    {
                        bucket = new List<AzureMonitorTelemetryEnvelope>();
                        this.itemsByOperation[item.OperationId] = bucket;
                    }

                    if (item is RequestTelemetryEnvelope request)
                    {
                        if (bucket.OfType<RequestTelemetryEnvelope>().Any(existing => string.Equals(existing.Id, request.Id, StringComparison.Ordinal)))
                        {
                            continue;
                        }

                        bucket.Add(request);
                        this.SetCurrentOperation(item.OperationId);
                    }
                    else
                    {
                        bucket.Add(item);
                    }
                }
            }
        }

        public void Record(Request request)
        {
            var envelopes = AzureMonitorPayloadParser.Parse(request);
            if (envelopes.Count == 0)
            {
                return;
            }

            this.AddRange(envelopes);
        }

        private bool ShouldStore(AzureMonitorTelemetryEnvelope envelope)
        {
            if (envelope is TraceTelemetryEnvelope trace)
            {
                if (!trace.Properties.TryGetValue("CategoryName", out var category) ||
                    category.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        private IEnumerable<AzureMonitorTelemetryEnvelope> GetCurrentOperationItemsUnsafe()
        {
            if (this.currentOperationId == null)
            {
                return Array.Empty<AzureMonitorTelemetryEnvelope>();
            }

            return this.itemsByOperation.TryGetValue(this.currentOperationId, out var bucket)
                ? bucket
                : Array.Empty<AzureMonitorTelemetryEnvelope>();
        }

        private void SetCurrentOperation(string operationId)
        {
            if (string.Equals(this.currentOperationId, operationId, StringComparison.Ordinal))
            {
                return;
            }

            this.currentOperationId = operationId;

            var keysToRemove = new List<string>();
            foreach (var key in this.itemsByOperation.Keys)
            {
                if (!string.Equals(key, operationId, StringComparison.Ordinal))
                {
                    keysToRemove.Add(key);
                }
            }

            foreach (var key in keysToRemove)
            {
                this.itemsByOperation.Remove(key);
            }
        }
    }

    internal abstract record AzureMonitorTelemetryEnvelope(
        string BaseType,
        string Name,
        string OperationId,
        string? OperationParentId,
        IReadOnlyDictionary<string, string> Properties);

    internal sealed record RequestTelemetryEnvelope(
        string Name,
        string OperationId,
        string? OperationParentId,
        string Id,
        string ResponseCode,
        bool Success,
        Uri? Url,
        TimeSpan Duration,
        IReadOnlyDictionary<string, string> Properties)
        : AzureMonitorTelemetryEnvelope("RequestData", Name, OperationId, OperationParentId, Properties);

    internal sealed record ExceptionTelemetryEnvelope(
        string Name,
        string OperationId,
        string? OperationParentId,
        string Message,
        string? TypeName,
        IReadOnlyDictionary<string, string> Properties)
        : AzureMonitorTelemetryEnvelope("ExceptionData", Name, OperationId, OperationParentId, Properties);

    internal sealed record TraceTelemetryEnvelope(
        string Name,
        string OperationId,
        string? OperationParentId,
        string Message,
        int? SeverityLevel,
        IReadOnlyDictionary<string, string> Properties)
        : AzureMonitorTelemetryEnvelope("MessageData", Name, OperationId, OperationParentId, Properties);

    internal sealed record DependencyTelemetryEnvelope(
        string Name,
        string OperationId,
        string? OperationParentId,
        string Id,
        string Type,
        string Target,
        string Data,
        string ResultCode,
        bool Success,
        IReadOnlyDictionary<string, string> Properties)
        : AzureMonitorTelemetryEnvelope("RemoteDependencyData", Name, OperationId, OperationParentId, Properties);

    internal static class AzureMonitorPayloadParser
    {
        public static IReadOnlyList<AzureMonitorTelemetryEnvelope> Parse(Request request)
        {
            if (request.Content == null)
            {
                return Array.Empty<AzureMonitorTelemetryEnvelope>();
            }

            using var buffer = new MemoryStream();
            request.Content.WriteTo(buffer, CancellationToken.None);
            buffer.Position = 0;

            if (request.Headers.TryGetValue("Content-Encoding", out var encoding) &&
                encoding.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Any(value => value.Equals("gzip", StringComparison.OrdinalIgnoreCase)))
            {
                using var gzip = new GZipStream(buffer, CompressionMode.Decompress, leaveOpen: true);
                using var decompressed = new MemoryStream();
                gzip.CopyTo(decompressed);
                decompressed.Position = 0;
                return ParseTelemetryPayload(decompressed);
            }

            return ParseTelemetryPayload(buffer);
        }

        private static IReadOnlyList<AzureMonitorTelemetryEnvelope> ParseTelemetryPayload(Stream payloadStream)
        {
            MemoryStream memoryStream;
            if (payloadStream is MemoryStream ms)
            {
                memoryStream = ms;
            }
            else
            {
                memoryStream = new MemoryStream();
                payloadStream.CopyTo(memoryStream);
            }

            var payloadText = Encoding.UTF8.GetString(memoryStream.ToArray());
            if (string.IsNullOrWhiteSpace(payloadText))
            {
                return Array.Empty<AzureMonitorTelemetryEnvelope>();
            }

            var items = new List<AzureMonitorTelemetryEnvelope>();
            using var reader = new StringReader(payloadText);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                if (TryParseTelemetry(line, out var envelope))
                {
                    items.Add(envelope);
                }
            }

            return items;
        }

        private static bool TryParseTelemetry(string rawJson, [NotNullWhen(true)] out AzureMonitorTelemetryEnvelope? envelope)
        {
            envelope = null;

            using var document = JsonDocument.Parse(rawJson);
            var root = document.RootElement;

            if (!root.TryGetProperty("data", out var dataElement))
            {
                return false;
            }

            var baseType = dataElement.GetProperty("baseType").GetString();
            var baseData = dataElement.GetProperty("baseData");

            var tags = root.TryGetProperty("tags", out var tagsElement)
                ? tagsElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString() ?? string.Empty)
                : new Dictionary<string, string>();

            tags.TryGetValue("ai.operation.id", out var operationId);
            tags.TryGetValue("ai.operation.parentId", out var operationParentId);

            var properties = baseData.TryGetProperty("properties", out var propsElement)
                ? propsElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString() ?? string.Empty)
                : new Dictionary<string, string>();

            var name = root.TryGetProperty("name", out var nameElement)
                ? nameElement.GetString() ?? string.Empty
                : string.Empty;

            switch (baseType)
            {
                case "RequestData":
                    envelope = ParseRequestTelemetry(baseData, name, operationId, operationParentId, properties);
                    return envelope != null;

                case "ExceptionData":
                    envelope = ParseExceptionTelemetry(baseData, name, operationId, operationParentId, properties);
                    return envelope != null;

                case "MessageData":
                    envelope = ParseTraceTelemetry(baseData, name, operationId, operationParentId, properties);
                    return envelope != null;

                case "RemoteDependencyData":
                    envelope = ParseDependencyTelemetry(baseData, name, operationId, operationParentId, properties);
                    return envelope != null;

                default:
                    envelope = null;
                    return false;
            }
        }

        private static RequestTelemetryEnvelope? ParseRequestTelemetry(
            JsonElement baseData,
            string name,
            string? operationId,
            string? operationParentId,
            IReadOnlyDictionary<string, string> properties)
        {
            if (!baseData.TryGetProperty("id", out var idElement) ||
                !baseData.TryGetProperty("responseCode", out var responseCodeElement) ||
                !baseData.TryGetProperty("duration", out var durationElement) ||
                !baseData.TryGetProperty("success", out var successElement))
            {
                return null;
            }

            var id = idElement.GetString() ?? string.Empty;
            var responseCode = responseCodeElement.GetString() ?? string.Empty;
            var duration = ParseDuration(durationElement.GetString());
            var success = successElement.GetBoolean();
            var url = baseData.TryGetProperty("url", out var urlElement) ? ParseUri(urlElement.GetString()) : null;
            var displayName = baseData.TryGetProperty("name", out var friendlyNameElement) ? friendlyNameElement.GetString() ?? name : name;

            return new RequestTelemetryEnvelope(
                Name: displayName,
                OperationId: operationId ?? string.Empty,
                OperationParentId: operationParentId,
                Id: id,
                ResponseCode: responseCode,
                Success: success,
                Url: url,
                Duration: duration,
                Properties: properties);
        }

        private static ExceptionTelemetryEnvelope? ParseExceptionTelemetry(
            JsonElement baseData,
            string name,
            string? operationId,
            string? operationParentId,
            IReadOnlyDictionary<string, string> properties)
        {
            if (!baseData.TryGetProperty("exceptions", out var exceptionsElement) || exceptionsElement.GetArrayLength() == 0)
            {
                return null;
            }

            var first = exceptionsElement[0];
            var message = first.TryGetProperty("message", out var messageElement) ? messageElement.GetString() ?? string.Empty : string.Empty;
            var typeName = first.TryGetProperty("typeName", out var typeNameElement) ? typeNameElement.GetString() : null;

            return new ExceptionTelemetryEnvelope(
                Name: name,
                OperationId: operationId ?? string.Empty,
                OperationParentId: operationParentId,
                Message: message,
                TypeName: typeName,
                Properties: properties);
        }

        private static TraceTelemetryEnvelope? ParseTraceTelemetry(
            JsonElement baseData,
            string name,
            string? operationId,
            string? operationParentId,
            IReadOnlyDictionary<string, string> properties)
        {
            if (!baseData.TryGetProperty("message", out var messageElement))
            {
                return null;
            }

            int? severity = null;
            if (baseData.TryGetProperty("severityLevel", out var severityElement))
            {
                if (severityElement.ValueKind == JsonValueKind.Number)
                {
                    severity = severityElement.GetInt32();
                }
                else if (severityElement.ValueKind == JsonValueKind.String)
                {
                    var severityString = severityElement.GetString();
                    if (!string.IsNullOrEmpty(severityString) &&
                        Enum.TryParse<TraceSeverityLevel>(severityString, out var severityEnum))
                    {
                        severity = (int)severityEnum;
                    }
                }
            }

            return new TraceTelemetryEnvelope(
                Name: name,
                OperationId: operationId ?? string.Empty,
                OperationParentId: operationParentId,
                Message: messageElement.GetString() ?? string.Empty,
                SeverityLevel: severity,
                Properties: properties);
        }

        private static DependencyTelemetryEnvelope? ParseDependencyTelemetry(
            JsonElement baseData,
            string name,
            string? operationId,
            string? operationParentId,
            IReadOnlyDictionary<string, string> properties)
        {
            if (!baseData.TryGetProperty("id", out var idElement) ||
                !baseData.TryGetProperty("type", out var typeElement) ||
                !baseData.TryGetProperty("target", out var targetElement) ||
                !baseData.TryGetProperty("data", out var dataElement) ||
                !baseData.TryGetProperty("resultCode", out var resultCodeElement) ||
                !baseData.TryGetProperty("success", out var successElement))
            {
                return null;
            }

            var dependencyId = idElement.GetString() ?? string.Empty;
            var dependencyType = typeElement.GetString() ?? string.Empty;
            var target = targetElement.GetString() ?? string.Empty;
            var data = dataElement.GetString() ?? string.Empty;
            var resultCode = resultCodeElement.GetString() ?? string.Empty;
            var success = successElement.GetBoolean();

            return new DependencyTelemetryEnvelope(
                Name: baseData.TryGetProperty("name", out var dependencyNameElement) ? dependencyNameElement.GetString() ?? name : name,
                OperationId: operationId ?? string.Empty,
                OperationParentId: operationParentId,
                Id: dependencyId,
                Type: dependencyType,
                Target: target,
                Data: data,
                ResultCode: resultCode,
                Success: success,
                Properties: properties);
        }

        private static TimeSpan ParseDuration(string? duration)
        {
            if (string.IsNullOrWhiteSpace(duration))
            {
                return TimeSpan.Zero;
            }

            if (TimeSpan.TryParse(duration, out var timeSpan))
            {
                return timeSpan;
            }

            return TimeSpan.Zero;
        }

        private static Uri? ParseUri(string? rawUrl)
        {
            return Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri) ? uri : null;
        }
    }

    /// <summary>
    /// Custom transport used to capture Azure Monitor exporter payloads without talking to the real ingestion endpoint.
    /// </summary>
    internal sealed class RecordingTransport : HttpPipelineTransport
    {
        private static readonly HttpPipelineTransport DefaultTransport = HttpClientTransport.Shared;
        private readonly TelemetryCollector collector;

        public RecordingTransport(TelemetryCollector collector)
        {
            this.collector = collector;
        }

        public override Request CreateRequest()
        {
            return DefaultTransport.CreateRequest();
        }

        public override void Process(HttpMessage message)
        {
            this.ProcessAsync(message).GetAwaiter().GetResult();
        }

        public override ValueTask ProcessAsync(HttpMessage message)
        {
            var telemetryItems = AzureMonitorPayloadParser.Parse(message.Request);
            this.collector.AddRange(telemetryItems);
            message.Response = new InMemoryResponse(200);
            return ValueTask.CompletedTask;
        }
    }

    internal sealed class InMemoryResponse : Response
    {
        private readonly int status;
        private readonly ResponseHeaders headers = new ResponseHeaders();

        public InMemoryResponse(int status)
        {
            this.status = status;
        }

        public override int Status => this.status;

        public override string ReasonPhrase => "OK";

        public override Stream? ContentStream { get; set; }

        public override string ClientRequestId { get; set; } = Guid.NewGuid().ToString();

        public override void Dispose()
        {
        }

        protected override bool TryGetHeader(string name, out string value)
        {
            value = string.Empty;
            return false;
        }

        protected override bool TryGetHeaderValues(string name, out IEnumerable<string> values)
        {
            values = Array.Empty<string>();
            return false;
        }

        protected override bool ContainsHeader(string name) => false;

        protected override IEnumerable<HttpHeader> EnumerateHeaders() => Array.Empty<HttpHeader>();

        public override ResponseHeaders Headers => this.headers;
    }
    /// <summary>
    /// Enum matching Azure Monitor's trace severity levels.
    /// </summary>
    internal enum TraceSeverityLevel
    {
        Verbose = 0,
        Information = 1,
        Warning = 2,
        Error = 3,
        Critical = 4
    }
}
