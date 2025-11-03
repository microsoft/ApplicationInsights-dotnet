using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Azure.Core.Pipeline;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.NLogTarget;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Layouts;
using NLog.Targets;
using OpenTelemetry;
using OpenTelemetry.Internal;
using OpenTelemetry.Logs;

#nullable enable

#pragma warning disable IL2026 // Tests intentionally use reflection on private members
#pragma warning disable IL2070
namespace Microsoft.ApplicationInsights.NLogTarget.Tests
{
    internal sealed class TelemetryTestEnvironment : IDisposable
    {
        private readonly TelemetryCollector collector = new();
        private readonly RecordingTransport transport;
        private readonly List<IDisposable> trackedDisposables = new();
        private readonly List<TelemetryClient> trackedClients = new();
        private string instrumentationKey = string.Empty;

        public TelemetryTestEnvironment()
        {
            this.transport = new RecordingTransport(this.collector);
        }

        public TelemetryCollector Collector => this.collector;

        public TelemetryConfiguration CreateConfiguration(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("A connection string is required to create a telemetry configuration.", nameof(connectionString));
            }

            this.instrumentationKey = ExtractInstrumentationKey(connectionString);

            var configuration = new TelemetryConfiguration();
            configuration.ConfigureOpenTelemetryBuilder(builder =>
            {
                builder.Services.AddSingleton<HttpPipelineTransport>(this.transport);
                builder.Services.AddOptions<AzureMonitorExporterOptions>()
                       .Configure<HttpPipelineTransport>((options, transport) =>
                       {
                           options.Transport = transport;
                           options.DisableOfflineStorage = true;
                       });
                builder.Services.Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Trace);
                builder.WithLogging(logging =>
                    logging.AddProcessor(new SimpleLogRecordExportProcessor(
                        new AzureMonitorRecordingLogExporter(this.collector, () => this.instrumentationKey))));
            });

            configuration.ConnectionString = connectionString;
            this.trackedDisposables.Add(configuration);
            return configuration;
        }

        public void ConfigureTarget(ApplicationInsightsTarget target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            var connectionString = this.ResolveConnectionString(target);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Target must resolve to a non-empty connection string before telemetry configuration can be applied.", nameof(target));
            }

            var configuration = this.CreateConfiguration(connectionString);
            this.InjectConfiguration(target, configuration);
        }

        public async Task WaitForTelemetryAsync(int expectedItemCount, TimeSpan? timeout = null)
        {
            var limit = timeout ?? TimeSpan.FromSeconds(5);
            var deadline = DateTime.UtcNow + limit;
            while (DateTime.UtcNow < deadline)
            {
                this.FlushTelemetryClients();

                if (this.collector.Items.Count >= expectedItemCount)
                {
                    return;
                }

                await Task.Delay(100).ConfigureAwait(false);
            }

            this.FlushTelemetryClients();
            await Task.Delay(200).ConfigureAwait(false);
        }

        public void Dispose()
        {
            foreach (var disposable in this.trackedDisposables)
            {
                disposable.Dispose();
            }

            this.trackedDisposables.Clear();
            this.trackedClients.Clear();
        }

        private void FlushTelemetryClients()
        {
            foreach (var client in this.trackedClients)
            {
                client.Flush();
            }
        }

        private void InjectConfiguration(ApplicationInsightsTarget target, TelemetryConfiguration configuration)
        {
            var targetType = target.GetType();
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

            var telemetryConfigurationField = targetType.GetField("telemetryConfiguration", Flags);
            var telemetryClientField = targetType.GetField("telemetryClient", Flags);

            var existingConfiguration = telemetryConfigurationField?.GetValue(target) as TelemetryConfiguration;
            existingConfiguration?.Dispose();

            var telemetryClient = new TelemetryClient(configuration);
            this.trackedClients.Add(telemetryClient);
            telemetryConfigurationField?.SetValue(target, configuration);
            telemetryClientField?.SetValue(target, telemetryClient);
        }

        private string ResolveConnectionString(ApplicationInsightsTarget target)
        {
            var layoutField = target.GetType().GetField("connectionStringLayout", BindingFlags.Instance | BindingFlags.NonPublic);
            if (layoutField?.GetValue(target) is Layout layout)
            {
                return layout.Render(NLog.LogEventInfo.CreateNullEvent());
            }

            return string.Empty;
        }

        private static string ExtractInstrumentationKey(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return string.Empty;
            }

            var segments = connectionString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var segment in segments)
            {
                var kvp = segment.Split(new[] { '=' }, 2);
                if (kvp.Length == 2 && kvp[0].Trim().Equals("InstrumentationKey", StringComparison.OrdinalIgnoreCase))
                {
                    return kvp[1].Trim();
                }
            }

            return string.Empty;
        }
        private sealed class AzureMonitorRecordingLogExporter : BaseExporter<LogRecord>
        {
            private readonly TelemetryCollector collector;
            private readonly Func<string> instrumentationKeyAccessor;
            private readonly string sdkVersion;

            public AzureMonitorRecordingLogExporter(TelemetryCollector collector, Func<string> instrumentationKeyAccessor)
            {
                this.collector = collector;
                this.instrumentationKeyAccessor = instrumentationKeyAccessor;
                this.sdkVersion = SdkVersionHelper.GetExpectedSdkVersion("nlog:", typeof(ApplicationInsightsTarget));
            }

            public override ExportResult Export(in Batch<LogRecord> batch)
            {
                var buffered = new List<AzureMonitorTelemetryEnvelope>();

                foreach (var record in batch)
                {
                    var envelope = this.CreateEnvelope(record);
                    if (envelope != null)
                    {
                        buffered.Add(envelope);
                    }
                }

                if (buffered.Count > 0)
                {
                    this.collector.AddRange(buffered);
                }

                return ExportResult.Success;
            }

            private AzureMonitorTelemetryEnvelope? CreateEnvelope(LogRecord record)
            {
                var instrumentationKey = this.instrumentationKeyAccessor() ?? string.Empty;
                var timestamp = record.Timestamp != default ? record.Timestamp : DateTimeOffset.UtcNow;
                var properties = this.ExtractProperties(record);

                if (!properties.ContainsKey("ai.internal.sdkVersion"))
                {
                    properties["ai.internal.sdkVersion"] = this.sdkVersion;
                }

                if (record.Exception != null)
                {
                    return this.CreateExceptionEnvelope(record, instrumentationKey, timestamp, properties);
                }

                return this.CreateTraceEnvelope(record, instrumentationKey, timestamp, properties);
            }

            private AzureMonitorTelemetryEnvelope CreateTraceEnvelope(
                LogRecord record,
                string instrumentationKey,
                DateTimeOffset timestamp,
                Dictionary<string, string> properties)
            {
                var message = ResolveMessage(record);
                var severity = MapSeverity(record.LogLevel);

                if (!string.IsNullOrEmpty(message) && !properties.ContainsKey("FormattedMessage"))
                {
                    this.AddProperty(properties, "FormattedMessage", message);
                }

                return new TraceTelemetryEnvelope(
                    Name: record.CategoryName ?? "Microsoft.ApplicationInsights.Message",
                    InstrumentationKey: instrumentationKey,
                    Timestamp: timestamp,
                    OperationId: string.Empty,
                    OperationParentId: null,
                    Message: message,
                    SeverityLevel: severity,
                    Properties: properties);
            }

            private AzureMonitorTelemetryEnvelope CreateExceptionEnvelope(
                LogRecord record,
                string instrumentationKey,
                DateTimeOffset timestamp,
                Dictionary<string, string> properties)
            {
                var exception = record.Exception!;
                var message = string.Concat(exception.GetType().ToString(), ": ", exception.Message ?? string.Empty);

                var formattedMessage = record.FormattedMessage;
                if (!properties.ContainsKey("Message") && !string.IsNullOrEmpty(formattedMessage))
                {
                    properties["Message"] = formattedMessage!;
                }

                var typeName = exception.GetType().FullName;
                if (string.IsNullOrEmpty(typeName))
                {
                    typeName = exception.GetType().Name;
                }

                return new ExceptionTelemetryEnvelope(
                    Name: record.CategoryName ?? exception.GetType().Name,
                    InstrumentationKey: instrumentationKey,
                    Timestamp: timestamp,
                    OperationId: string.Empty,
                    OperationParentId: null,
                    Message: message,
                    TypeName: typeName,
                    Properties: properties);
            }

            private Dictionary<string, string> ExtractProperties(LogRecord record)
            {
                var properties = new Dictionary<string, string>(StringComparer.Ordinal);

                if (record.State is IEnumerable<KeyValuePair<string, object>> state)
                {
                    foreach (var pair in state)
                    {
                        if (string.Equals(pair.Key, "{OriginalFormat}", StringComparison.Ordinal))
                        {
                            continue;
                        }

                        this.AddProperty(properties, pair.Key, pair.Value);
                    }
                }

                if (record.Attributes != null)
                {
                    foreach (var attribute in record.Attributes)
                    {
                        if (string.Equals(attribute.Key, "{OriginalFormat}", StringComparison.Ordinal))
                        {
                            continue;
                        }

                        this.AddProperty(properties, attribute.Key, attribute.Value);
                    }
                }

                return properties;
            }

            private void AddProperty(IDictionary<string, string> properties, string key, object? value)
            {
                if (string.IsNullOrEmpty(key) || value == null)
                {
                    return;
                }

                var stringValue = Convert.ToString(value, CultureInfo.InvariantCulture);
                if (stringValue == null)
                {
                    return;
                }

                if (properties.TryGetValue(key, out var existingValue))
                {
                    if (string.Equals(existingValue, stringValue, StringComparison.Ordinal))
                    {
                        return;
                    }

                    var suffix = 1;
                    string candidate;
                    do
                    {
                        candidate = string.Concat(key, "_", suffix.ToString(CultureInfo.InvariantCulture));
                        suffix++;
                    }
                    while (properties.ContainsKey(candidate));

                    key = candidate;
                }

                properties[key] = stringValue;
            }

            private static string ResolveMessage(LogRecord record)
            {
                var formattedMessage = record.FormattedMessage;
                if (!string.IsNullOrEmpty(formattedMessage))
                {
                    return formattedMessage!;
                }

                if (record.Body != null)
                {
                    var bodyText = record.Body.ToString();
                    if (!string.IsNullOrEmpty(bodyText))
                    {
                        return bodyText!;
                    }
                }

                if (record.State is IEnumerable<KeyValuePair<string, object>> state)
                {
                    foreach (var pair in state)
                    {
                        if (string.Equals(pair.Key, "{OriginalFormat}", StringComparison.Ordinal) && pair.Value != null)
                        {
                            var stateMessage = pair.Value.ToString();
                            if (!string.IsNullOrEmpty(stateMessage))
                            {
                                return stateMessage!;
                            }
                        }
                    }
                }

                return string.Empty;
            }

            private static int? MapSeverity(LogLevel logLevel)
            {
                return logLevel switch
                {
                    LogLevel.Trace => (int)SeverityLevel.Verbose,
                    LogLevel.Debug => (int)SeverityLevel.Verbose,
                    LogLevel.Information => (int)SeverityLevel.Information,
                    LogLevel.Warning => (int)SeverityLevel.Warning,
                    LogLevel.Error => (int)SeverityLevel.Error,
                    LogLevel.Critical => (int)SeverityLevel.Critical,
                    _ => null,
                };
            }
        }
    }
}
#pragma warning restore IL2070
#pragma warning restore IL2026
