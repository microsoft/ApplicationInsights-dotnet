# ITelemetryProcessor → BaseProcessor\<Activity\> Migration

## What Changed

`ITelemetryProcessor` is removed in 3.x. Convert **custom** processors to `BaseProcessor<Activity>` with `OnEnd` for filtering/enrichment.

**Note:** Built-in 2.x processors (e.g., `SamplingTelemetryProcessor`, `AdaptiveSamplingTelemetryProcessor`, `QuickPulseTelemetryProcessor`, `AutocollectedMetricsExtractor`) do **not** need manual migration — their functionality is handled automatically by 3.x. Remove any manual registrations of these built-in types.

## Before (2.x)

```csharp
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

public class HealthCheckFilter : ITelemetryProcessor
{
    private readonly ITelemetryProcessor _next;

    public HealthCheckFilter(ITelemetryProcessor next)
    {
        _next = next;
    }

    public void Process(ITelemetry item)
    {
        if (item is RequestTelemetry request && request.Url.AbsolutePath == "/health")
        {
            return; // Drop health check requests
        }
        _next.Process(item);
    }
}

// Registration:
services.AddApplicationInsightsTelemetryProcessor<HealthCheckFilter>();
```

## After (3.x)

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

// Registration:
builder.Services.ConfigureOpenTelemetryTracerProvider(tracing =>
    tracing.AddProcessor<HealthCheckFilterProcessor>());
```

## Key Differences

| 2.x Pattern | 3.x Equivalent |
|---|---|
| `ITelemetryProcessor.Process(ITelemetry)` | `BaseProcessor<Activity>.OnEnd(Activity)` |
| `services.AddApplicationInsightsTelemetryProcessor<T>()` | `.AddProcessor<T>()` on `TracerProviderBuilder` |
| Not calling `next.Process(item)` to drop | `data.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;` |

**Important:** Do not rely on skipping `base.OnEnd()` — `CompositeProcessor` iterates all processors regardless. The `Recorded` flag is the correct filtering mechanism.

## Telemetry Type Mapping

Old processors often check `if (telemetry is RequestTelemetry)`. In 3.x, check `Activity.Kind`:

| `ActivityKind` | Application Insights type |
|---|---|
| `Server` | `RequestTelemetry` |
| `Client` | `DependencyTelemetry` |
| `Producer` | `DependencyTelemetry` |
| `Consumer` | `RequestTelemetry` (message-driven) |
| `Internal` | `DependencyTelemetry` |

## Non-DI Registration (Console / Classic ASP.NET)

For apps using `TelemetryConfiguration` directly:

```csharp
var config = TelemetryConfiguration.CreateDefault();
config.ConnectionString = "InstrumentationKey=...;IngestionEndpoint=...";
config.ConfigureOpenTelemetryBuilder(otel =>
    otel.WithTracing(t => t.AddProcessor<HealthCheckFilterProcessor>()));
```

This replaces the 2.x pattern of `AddApplicationInsightsTelemetryProcessor<T>()` or adding processors via `TelemetryConfiguration.TelemetryProcessorChainBuilder`.

## If Your Processor Touches Both Traces and Logs

In 2.x, one `ITelemetryProcessor` handled all signal types. In 3.x, traces, logs, and metrics are separate pipelines. If the old processor checked for `TraceTelemetry`, `EventTelemetry`, or log severity, you need separate handling:

**For log filtering** (e.g., dropping logs by severity or category):
```csharp
// Use ILoggingBuilder filters — not a processor
builder.Logging.AddFilter<OpenTelemetryLoggerProvider>("Microsoft", LogLevel.Warning);
```

**For log enrichment or custom log processing** — use `BaseProcessor<LogRecord>`:
```csharp
public class LogEnrichmentProcessor : BaseProcessor<LogRecord>
{
    public override void OnEnd(LogRecord data)
    {
        var attributes = new List<KeyValuePair<string, object?>>(data.Attributes ?? [])
        {
            new("deployment.environment", "production")
        };
        data.Attributes = attributes;
    }
}

// Registration (DI):
builder.Services.ConfigureOpenTelemetryLoggerProvider(logging =>
    logging.AddProcessor<LogEnrichmentProcessor>());

// Registration (Non-DI):
config.ConfigureOpenTelemetryBuilder(otel =>
    otel.WithLogging(l => l.AddProcessor<LogEnrichmentProcessor>()));
```

If the old processor only touched trace types (`RequestTelemetry`, `DependencyTelemetry`), a single `BaseProcessor<Activity>` is sufficient — no log processor needed.
