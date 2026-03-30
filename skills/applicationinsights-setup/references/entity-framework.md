# Entity Framework Core Instrumentation

## Package

```bash
dotnet add package OpenTelemetry.Instrumentation.EntityFrameworkCore
```

## Setup

EF Core is auto-instrumented via DiagnosticSource — basic span collection works without this package. Install it to customize options.

Unlike SQL Client and HTTP (which the SDK auto-registers), EF Core instrumentation must be explicitly added via `AddEntityFrameworkCoreInstrumentation()`:

```csharp
builder.Services.ConfigureOpenTelemetryTracerProvider(tracing =>
    tracing.AddEntityFrameworkCoreInstrumentation(options =>
    {
        options.SetDbStatementForText = true;  // Include SQL text (be careful with PII)
        options.SetDbStatementForStoredProcedure = true;
    }));
```

## Options

| Option | Default | Description |
|---|---|---|
| `SetDbStatementForText` | `false` | Include raw SQL in `db.statement`. May contain PII |
| `SetDbStatementForStoredProcedure` | `true` | Include stored procedure names |
| `EnrichWithIDbCommand` | `null` | `Action<Activity, IDbCommand>` to add custom tags |
| `Filter` | `null` | `Func<string, string, bool>` — return `false` to suppress |

## Metrics

EF Core 9+ emits built-in metrics via the `Microsoft.EntityFrameworkCore` meter (active connections, queries per second, compiled queries, etc.). Register the meter to collect these:

```csharp
// DI
builder.Services.ConfigureOpenTelemetryMeterProvider(metrics =>
    metrics.AddMeter("Microsoft.EntityFrameworkCore"));

// Non-DI
config.ConfigureOpenTelemetryBuilder(otel =>
    otel.WithMetrics(m => m.AddMeter("Microsoft.EntityFrameworkCore")));
```

This is separate from the trace instrumentation package — traces give you per-query spans, metrics give you aggregate counters and gauges.

## Non-DI Usage (Console / Classic ASP.NET)

For apps using `TelemetryConfiguration` directly instead of `builder.Services`:

```csharp
var config = TelemetryConfiguration.CreateDefault();
config.ConnectionString = "InstrumentationKey=...;IngestionEndpoint=...";
config.ConfigureOpenTelemetryBuilder(otel =>
    otel.WithTracing(t => t.AddEntityFrameworkCoreInstrumentation()));
```

## Notes

- Works with all EF Core providers (SQL Server, PostgreSQL, SQLite, MySQL)
- The package is only needed to customize options — basic spans are collected without it
- `SetDbStatementForText = true` captures raw SQL which may contain sensitive data
