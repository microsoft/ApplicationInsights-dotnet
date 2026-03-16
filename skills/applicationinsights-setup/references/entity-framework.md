# Entity Framework Core Instrumentation

## Package

```bash
dotnet add package OpenTelemetry.Instrumentation.EntityFrameworkCore
```

## Setup

EF Core is auto-instrumented via DiagnosticSource — basic span collection works without this package. Install it to customize:

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
