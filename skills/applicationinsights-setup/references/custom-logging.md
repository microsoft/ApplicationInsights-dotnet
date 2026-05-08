# Custom Logging

## Overview

In 3.x, `ILogger` output is **automatically exported** to Application Insights — no additional setup needed. Logs at `Information` level and above are sent by default.

**Important:** Do not use the old `TelemetryClient.TrackTrace()` API for new logging. Use `ILogger` instead — it integrates with OpenTelemetry's log pipeline, supports structured logging, and avoids performance overhead from the legacy API.

## Usage — High-Performance Logging

**Use the `[LoggerMessage]` source generator for all logging.** This avoids boxing, string allocation, and parsing on every log call. The older `_logger.LogInformation(...)` pattern allocates on every call even if the log level is disabled — .NET does not recommend it for production code.

### Source Generator (recommended, .NET 6+)

```csharp
public partial class OrderService
{
    private readonly ILogger<OrderService> _logger;

    public OrderService(ILogger<OrderService> logger) => _logger = logger;

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Processing order {OrderId} for {Total}")]
    partial void LogProcessing(string orderId, decimal total);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Order {OrderId} completed")]
    partial void LogCompleted(string orderId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Failed to process order {OrderId}")]
    partial void LogFailed(Exception ex, string orderId);

    public void ProcessOrder(string orderId, decimal total)
    {
        LogProcessing(orderId, total);
        try
        {
            // ... process ...
            LogCompleted(orderId);
        }
        catch (Exception ex)
        {
            LogFailed(ex, orderId);
            throw;
        }
    }
}
```

### LoggerMessage.Define (pre-.NET 6 or manual)

```csharp
public class OrderService
{
    private static readonly Action<ILogger, string, decimal, Exception?> _logProcessing =
        LoggerMessage.Define<string, decimal>(
            LogLevel.Information, new EventId(1, "ProcessingOrder"),
            "Processing order {OrderId} for {Total}");

    private readonly ILogger<OrderService> _logger;

    public OrderService(ILogger<OrderService> logger) => _logger = logger;

    public void ProcessOrder(string orderId, decimal total)
    {
        _logProcessing(_logger, orderId, total, null);
    }
}
```

## Why Avoid Old Logging APIs

| Pattern | Issue |
|---|---|
| `TelemetryClient.TrackTrace(message)` | Bypasses OpenTelemetry pipeline, no structured logging, no log level filtering, performance overhead from legacy channel |
| `_logger.LogInformation($"Order {id}")` | String interpolation allocates on every call, even when log level is disabled |
| `_logger.LogInformation("Order " + id)` | Same — string concatenation allocates unconditionally |
| `_logger.LogInformation("Order {Id}", id)` | **Acceptable** — deferred formatting, but still boxes value types |
| `[LoggerMessage]` source generator | **Best** — zero allocation, compile-time validation, no boxing |

## Controlling Log Levels

```csharp
// Filter by category and level
builder.Logging.AddFilter<OpenTelemetryLoggerProvider>("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter<OpenTelemetryLoggerProvider>("System", LogLevel.Warning);
builder.Logging.AddFilter<OpenTelemetryLoggerProvider>("MyApp", LogLevel.Debug);
```

Or in `appsettings.json`:
```json
{
  "Logging": {
    "OpenTelemetry": {
      "LogLevel": {
        "Default": "Information",
        "Microsoft": "Warning",
        "MyApp": "Debug"
      }
    }
  }
}
```

## Log Sampling

By default, `EnableTraceBasedLogsSampler` is `true` — logs follow the sampling decision of their parent trace. Logs without a parent trace are always exported.

To disable (export all logs regardless of trace sampling):
```csharp
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.EnableTraceBasedLogsSampler = false;
});
```

## Custom Log Scopes

Use scopes to add properties to all logs within a block. Scope properties become custom dimensions in Application Insights:

```csharp
using (_logger.BeginScope(new Dictionary<string, object>
{
    ["TransactionId"] = transactionId,
    ["UserId"] = userId
}))
{
    LogTransactionStarted();
    // ... all logs in this block include TransactionId and UserId as custom dimensions
    LogTransactionComplete();
}
```

> **Important:** Logging scopes are **disabled by default** in the 3.x SDK. Without the following configuration, `BeginScope` properties are silently dropped:
>
> ```csharp
> builder.Services.Configure<OpenTelemetryLoggerOptions>(options =>
> {
>     options.IncludeScopes = true;
> });
> ```

## How Logs Map to Application Insights

| ILogger | Application Insights |
|---|---|
| `LogInformation` / `LogWarning` / `LogDebug` | Trace telemetry |
| `LogError` / `LogCritical` with exception | Exception telemetry |
| Log with `EventId` and `microsoft.custom_event.name` attribute | Event telemetry |
| Structured log parameters | Custom dimensions |
| `BeginScope` properties | Custom dimensions |
