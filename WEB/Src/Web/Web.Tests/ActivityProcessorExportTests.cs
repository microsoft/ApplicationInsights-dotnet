namespace Microsoft.ApplicationInsights.Web.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using Azure;
    using Azure.Core;
    using Azure.Core.Pipeline;
    using Azure.Monitor.OpenTelemetry.Exporter;
    using Microsoft.ApplicationInsights.Web.Helpers;
    using OpenTelemetry;
    using OpenTelemetry.Trace;
    using Xunit;

    /// <summary>
    /// Runs the Web activity processors through the Azure Monitor exporter and asserts on the exported envelope,
    /// so that tags written under keys the exporter does not map (which would land in customDimensions) are caught.
    /// </summary>
    public class ActivityProcessorExportTests : IDisposable
    {
        private const string SourceName = "Microsoft.ApplicationInsights.Web.Tests.Export";
        private const string AvailabilityMonitoringSource = "Application Insights Availability Monitoring";

        private static readonly ActivitySource Source = new ActivitySource(SourceName);

        private static readonly string[] KeysThatMustNotBeProperties =
        {
            "ai.user.id",
            "session.id",
            "session.isFirst",
            "ai.operation.syntheticSource",
            "enduser.account",
            "microsoft.session.id",
            "microsoft.synthetic_source",
            "microsoft.user.account_id",
            "enduser.pseudo.id",
        };

        public ActivityProcessorExportTests()
        {
            HttpContext.Current = null;
        }

        public void Dispose()
        {
            HttpContext.Current = null;
        }

        [Fact]
        public void ExportedRequestHasUserSessionSyntheticSourceAndAccountContextTags()
        {
            // Arrange
            string now = DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture);
            var context = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string> { { "User-Agent", "YandexBot 123" } });
            context.AddRequestCookie(new HttpCookie("ai_user", "anonUser|" + now));
            context.AddRequestCookie(new HttpCookie("ai_session", "session123|" + now + "|" + now));
            context.WithAuthCookie("authUser123|account456");

            // Act
            var envelope = ExportSingleRequest();

            // Assert
            var tags = envelope.GetProperty("tags");
            Assert.Equal("anonUser", GetString(tags, "ai.user.id"));
            Assert.Equal("session123", GetString(tags, "ai.session.id"));
            Assert.Equal("Bot", GetString(tags, "ai.operation.syntheticSource"));
            Assert.Equal("authUser123", GetString(tags, "ai.user.authUserId"));
            Assert.Equal("account456", GetString(tags, "ai.user.accountId"));
            AssertNoContextKeysInProperties(envelope);
        }

        [Fact]
        public void ExportedRequestFromAvailabilityTestTakesPrecedenceOverBotAndCookies()
        {
            // Arrange
            string now = DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture);
            var context = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
            {
                { "User-Agent", "YandexBot 123" },
                { "SyntheticTest-Location", "LOCATION" },
                { "SyntheticTest-RunId", "RUNID" },
            });
            context.AddRequestCookie(new HttpCookie("ai_user", "anonUser|" + now));
            context.AddRequestCookie(new HttpCookie("ai_session", "session123|" + now + "|" + now));

            // Act
            var envelope = ExportSingleRequest();

            // Assert
            var tags = envelope.GetProperty("tags");
            Assert.Equal(AvailabilityMonitoringSource, GetString(tags, "ai.operation.syntheticSource"));
            Assert.Equal("LOCATION_RUNID", GetString(tags, "ai.user.id"));
            Assert.Equal("RUNID", GetString(tags, "ai.session.id"));
            AssertNoContextKeysInProperties(envelope);
        }

        private static JsonElement ExportSingleRequest()
        {
            var transport = new CapturingTransport();

            // Processors are registered in the same order as ApplicationInsightsExtensions.
            using (var tracerProvider = Sdk.CreateTracerProviderBuilder()
                .AddSource(SourceName)
                .AddProcessor(new WebTestActivityProcessor())
                .AddProcessor(new SyntheticUserAgentActivityProcessor())
                .AddProcessor(new SessionActivityProcessor())
                .AddProcessor(new UserActivityProcessor())
                .AddProcessor(new AuthenticatedUserIdActivityProcessor())
                .AddProcessor(new AccountIdActivityProcessor())
                .AddProcessor(new ClientIpHeaderActivityProcessor())
                .AddAzureMonitorTraceExporter(o =>
                {
                    // The exporter caches its transmitter (and transport) per connection string, so use a unique one per test.
                    o.ConnectionString = "InstrumentationKey=" + Guid.NewGuid().ToString();
                    o.Transport = transport;
                    o.DisableOfflineStorage = true;
                    o.SamplingRatio = 1.0F;
                    o.TracesPerSecond = null;
                })
                .Build())
            {
                using (var activity = Source.StartActivity("GET /test", ActivityKind.Server))
                {
                    Assert.NotNull(activity);
                    activity.SetTag("http.request.method", "GET");
                    activity.SetTag("url.scheme", "http");
                    activity.SetTag("server.address", "localhost");
                    activity.SetTag("url.path", "/test");
                    activity.SetTag("http.response.status_code", 200);
                }

                Assert.True(tracerProvider.ForceFlush(10000));
            }

            return transport.Envelopes.Single(e => e.GetProperty("name").GetString() == "Request");
        }

        private static string GetString(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var value) ? value.GetString() : null;
        }

        private static void AssertNoContextKeysInProperties(JsonElement envelope)
        {
            var baseData = envelope.GetProperty("data").GetProperty("baseData");
            if (!baseData.TryGetProperty("properties", out var properties))
            {
                return;
            }

            foreach (var key in KeysThatMustNotBeProperties)
            {
                Assert.False(properties.TryGetProperty(key, out _), $"'{key}' was exported as a custom property instead of a context tag.");
            }
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
