# ILogger Migration (2.x → 3.x)

## What Changed

- `ApplicationInsightsLoggerProvider` is **removed** — logging is automatic in 3.x
- No need to manually register log providers
- `ILogger` output is automatically exported to Application Insights

## Before (2.x)

```csharp
// Manual logger provider registration (no longer needed)
builder.Logging.AddApplicationInsights();

// Or via configuration
builder.Logging.AddFilter<ApplicationInsightsLoggerProvider>("", LogLevel.Information);
```

## After (3.x)

```csharp
// No logging setup needed — it's automatic.
// ILogger output flows to Application Insights via OpenTelemetry.

// To filter log levels, use standard ILoggingBuilder:
builder.Logging.AddFilter<OpenTelemetryLoggerProvider>("", LogLevel.Warning);
```

## Key Points

1. **Remove** any `AddApplicationInsights()` calls on `ILoggingBuilder` — they won't compile
2. **Remove** any `AddFilter<ApplicationInsightsLoggerProvider>` — use `AddFilter<OpenTelemetryLoggerProvider>` instead
3. Logs at `Information` level and above are sent by default
4. `EnableTraceBasedLogsSampler` (new in 3.x, default `true`) — logs follow parent trace sampling decisions
