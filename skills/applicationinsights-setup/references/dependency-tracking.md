# Dependency Tracking Migration (2.x → 3.x)

## What Changed

- `DependencyCollectionOptions` property is **removed** from `ApplicationInsightsServiceOptions`
- Dependency tracking is now handled by OpenTelemetry instrumentation libraries
- HTTP and SQL dependencies are still auto-collected — no configuration needed

## Before (2.x)

```csharp
services.AddApplicationInsightsTelemetry(options =>
{
    options.DependencyCollectionOptions.EnableLegacyCorrelationHeadersInjection = true;
});
```

## After (3.x)

```csharp
// Delete the DependencyCollectionOptions line.
// Dependency tracking works automatically.
services.AddApplicationInsightsTelemetry();
```

## What's Auto-Collected in 3.x

- **HTTP client calls** — via `System.Net.Http` DiagnosticSource
- **SQL queries** — via `SqlClient` DiagnosticSource
- **Azure SDK calls** — via Azure SDK DiagnosticSource

## Customizing Dependency Collection

To customize (e.g., capture SQL text), configure the existing SQL instrumentation options — do not call `AddSqlClientInstrumentation()` again as the SDK already registers it:

```csharp
builder.Services.Configure<SqlClientTraceInstrumentationOptions>(options =>
{
    options.SetDbStatementForText = true;
});
```

See [sql-client.md](sql-client.md) for full details.
