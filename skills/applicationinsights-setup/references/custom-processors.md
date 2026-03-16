# Custom Processors (Filtering & Enrichment)

## Overview

Use `BaseProcessor<Activity>` to filter or enrich trace spans, and `BaseProcessor<LogRecord>` for logs. Register via `.AddProcessor<T>()`.

## Filtering — Drop Spans

Clear the `Recorded` flag to prevent export:

```csharp
using System.Diagnostics;
using OpenTelemetry;

public class HealthCheckFilterProcessor : BaseProcessor<Activity>
{
    private static readonly HashSet<string> SuppressedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health", "/ready", "/healthz", "/liveness"
    };

    public override void OnEnd(Activity data)
    {
        var path = data.GetTagItem("url.path")?.ToString()
                ?? data.GetTagItem("http.route")?.ToString();

        if (path != null && SuppressedPaths.Contains(path))
        {
            data.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
        }
    }
}
```

**Important:** Do not rely on skipping `base.OnEnd()`. The `Recorded` flag is the correct filtering mechanism.

## Enrichment — Add Tags

Use `OnStart` to attach tags before the span completes:

```csharp
public class EnrichmentProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity data)
    {
        data.SetTag("deployment.environment", "production");
        data.SetTag("cloud.region", "eastus");
    }
}
```

## Registration

### DI (ASP.NET Core / Worker Service)

```csharp
builder.Services.ConfigureOpenTelemetryTracerProvider(tracing =>
    tracing.AddProcessor<HealthCheckFilterProcessor>()
           .AddProcessor<EnrichmentProcessor>());
```

### Non-DI (Console / Classic ASP.NET)

```csharp
var config = TelemetryConfiguration.CreateDefault();
config.ConnectionString = "InstrumentationKey=...;IngestionEndpoint=...";
config.ConfigureOpenTelemetryBuilder(otel =>
    otel.WithTracing(t => t
        .AddProcessor<HealthCheckFilterProcessor>()
        .AddProcessor<EnrichmentProcessor>()));
```

## Activity Properties

Useful properties on `Activity` for processor logic:
- `DisplayName` — operation name
- `Kind` — `Server` (request), `Client` (dependency), `Internal`, `Producer`, `Consumer`
- `Duration` — elapsed time
- `Status` — status code
- `GetTagItem("key")` — read any tag
- `SetTag("key", value)` — set/overwrite a tag
- `Events` — span events
- `TraceId`, `SpanId`, `ParentId` — correlation IDs
