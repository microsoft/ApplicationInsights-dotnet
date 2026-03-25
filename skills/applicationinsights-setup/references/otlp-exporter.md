# OTLP Exporter

Export traces, metrics, and logs to any OpenTelemetry Protocol (OTLP) compatible backend alongside Azure Monitor.

## Package

```bash
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

## Setup

OTLP exporter works **alongside** Azure Monitor — data is sent to both:

```csharp
using OpenTelemetry.Exporter;

// Add OTLP as a secondary exporter for traces:
builder.Services.ConfigureOpenTelemetryTracerProvider(tracing =>
    tracing.AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("http://localhost:4317");
        options.Protocol = OtlpExportProtocol.Grpc;
    }));

// For metrics:
builder.Services.ConfigureOpenTelemetryMeterProvider(metrics =>
    metrics.AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("http://localhost:4317");
    }));

// For logs:
builder.Services.ConfigureOpenTelemetryLoggerProvider(logging =>
    logging.AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("http://localhost:4317");
    }));
```

## Environment Variable Configuration

| Variable | Default | Description |
|---|---|---|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `http://localhost:4317` | OTLP endpoint URL |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | `grpc` | `grpc` or `http/protobuf` |
| `OTEL_EXPORTER_OTLP_HEADERS` | — | Headers as `key=value`, comma-separated |
| `OTEL_EXPORTER_OTLP_TIMEOUT` | `10000` | Timeout in milliseconds |

## Common Backends

| Backend | Endpoint | Protocol |
|---|---|---|
| Aspire Dashboard | `http://localhost:4317` | gRPC |
| Jaeger | `http://localhost:4317` | gRPC |
| Grafana Tempo | `http://localhost:4317` | gRPC |
| Grafana Cloud | `https://otlp-gateway-*.grafana.net/otlp` | HTTP |

## Non-DI Usage (Console / Classic ASP.NET)

For apps using `TelemetryConfiguration` directly:

```csharp
var config = TelemetryConfiguration.CreateDefault();
config.ConnectionString = "InstrumentationKey=...;IngestionEndpoint=...";
config.ConfigureOpenTelemetryBuilder(otel =>
{
    otel.WithTracing(t => t.AddOtlpExporter(o =>
        o.Endpoint = new Uri("http://localhost:4317")));
    otel.WithMetrics(m => m.AddOtlpExporter(o =>
        o.Endpoint = new Uri("http://localhost:4317")));
});
```

## Notes

- Each signal (traces, metrics, logs) needs its own `AddOtlpExporter()` call
- `AddOtlpExporter()` with no arguments uses environment variables or defaults
