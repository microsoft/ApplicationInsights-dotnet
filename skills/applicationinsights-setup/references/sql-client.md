# SQL Client Instrumentation

## Package

```bash
dotnet add package OpenTelemetry.Instrumentation.SqlClient
```

## Setup

The SDK already registers SQL client instrumentation. To customize options, use the DI options pattern:

```csharp
using OpenTelemetry.Instrumentation.SqlClient;

builder.Services.Configure<SqlClientTraceInstrumentationOptions>(options =>
{
    options.SetDbStatementForText = true;   // Include SQL text
    options.RecordException = true;          // Record exception details on spans
});
```

**Do not call `AddSqlClientInstrumentation()` again** — the SDK already registers it. Use `services.Configure<SqlClientTraceInstrumentationOptions>` to customize options without duplicating instrumentation.

## Options

| Option | Default | Description |
|---|---|---|
| `SetDbStatementForText` | `false` | Include SQL command text in `db.statement`. May contain PII |
| `SetDbStatementForStoredProcedure` | `true` | Include stored procedure name |
| `RecordException` | `false` | Record exception details as span events |
| `EnableConnectionLevelAttributes` | `false` | Add `server.address` and `server.port` |
| `Enrich` | `null` | `Action<Activity, string, object>` for custom tags |
| `Filter` | `null` | `Func<object, bool>` — return `false` to suppress |

## Non-DI Usage (Console / Classic ASP.NET)

For apps using `TelemetryConfiguration` directly:

```csharp
var config = TelemetryConfiguration.CreateDefault();
config.ConnectionString = "InstrumentationKey=...;IngestionEndpoint=...";
config.ConfigureOpenTelemetryBuilder(otel =>
{
    otel.Services.Configure<SqlClientTraceInstrumentationOptions>(options =>
    {
        options.SetDbStatementForText = true;
        options.RecordException = true;
    });
});
```

## Notes

- Works with both `System.Data.SqlClient` and `Microsoft.Data.SqlClient`
- If using EF Core with SQL Server, EF Core instrumentation captures the same spans — use this for raw `SqlCommand`/`SqlConnection` calls
- `SetDbStatementForText = true` captures raw SQL which may contain sensitive data
