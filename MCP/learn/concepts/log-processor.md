---
title: Log Processor - Processing ILogger Logs in 3.x
category: concept
applies-to: 3.x
related:
  - concepts/activity-processor.md
  - concepts/opentelemetry-pipeline.md
source: OpenTelemetry.Logs, Microsoft.Extensions.Logging.ApplicationInsights
---

# Log Processor - Processing ILogger Logs in 3.x

## Overview

Log Processors in OpenTelemetry 3.x handle ILogger logs similar to how Activity Processors handle traces. They replace custom ILogger providers and allow enrichment/filtering of log entries before export to Application Insights.

## In 2.x: ILogger Integration

```csharp
// 2.x: ILogger with Application Insights provider
services.AddLogging(builder =>
{
    builder.AddApplicationInsights(config.ConnectionString);
});

// Logs automatically sent to Application Insights
logger.LogInformation("Order processed: {OrderId}", orderId);
```

## In 3.x: OpenTelemetry Logging

```csharp
// 3.x: ILogger integrated via OpenTelemetry
services.AddApplicationInsightsTelemetry();  // Includes logging

// Or explicit configuration
config.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.AddInstrumentation<ILogger>();
    builder.AddProcessor<MyLogProcessor>();
});

logger.LogInformation("Order processed: {OrderId}", orderId);
```

## BaseProcessor<LogRecord>

```csharp
using OpenTelemetry;
using OpenTelemetry.Logs;

public class MyLogProcessor : BaseProcessor<LogRecord>
{
    public override void OnEnd(LogRecord logRecord)
    {
        // Enrich log with custom attributes
        logRecord.Attributes ??= new List<KeyValuePair<string, object>>();
        
        // Add custom dimension
        logRecord.Attributes.Add(
            new KeyValuePair<string, object>("processor.added", "true"));
        
        // Filter logs
        if (ShouldFilter(logRecord))
        {
            // Mark to not export
            logRecord.Attributes.Add(
                new KeyValuePair<string, object>("_filtered", "true"));
        }
    }
}
```

## LogRecord Properties

```csharp
public class LogRecord
{
    public DateTimeOffset Timestamp { get; }
    public LogLevel LogLevel { get; }
    public string? FormattedMessage { get; }
    public string? CategoryName { get; }
    public EventId EventId { get; }
    public Exception? Exception { get; }
    public IReadOnlyList<KeyValuePair<string, object>>? Attributes { get; set; }
    public TraceId TraceId { get; }  // Correlation with Activity
    public SpanId SpanId { get; }    // Correlation with Activity
}
```

## Common Patterns

### Pattern 1: Add Environment to All Logs

```csharp
public class EnvironmentLogProcessor : BaseProcessor<LogRecord>
{
    private readonly string environment;
    
    public EnvironmentLogProcessor(string env)
    {
        environment = env;
    }
    
    public override void OnEnd(LogRecord logRecord)
    {
        logRecord.Attributes ??= new List<KeyValuePair<string, object>>();
        logRecord.Attributes.Add(
            new KeyValuePair<string, object>("deployment.environment", environment));
    }
}
```

### Pattern 2: Filter Noisy Logs

```csharp
public class NoisyLogFilterProcessor : BaseProcessor<LogRecord>
{
    public override void OnEnd(LogRecord logRecord)
    {
        // Filter out health check logs
        if (logRecord.CategoryName?.Contains("HealthCheck") == true)
        {
            // Drop this log
            logRecord.Attributes ??= new List<KeyValuePair<string, object>>();
            logRecord.Attributes.Add(
                new KeyValuePair<string, object>("_dropped", "true"));
        }
    }
}
```

### Pattern 3: Correlate Logs with Activities

```csharp
public class CorrelationLogProcessor : BaseProcessor<LogRecord>
{
    public override void OnEnd(LogRecord logRecord)
    {
        // LogRecord automatically has TraceId and SpanId
        // but you can add custom correlation
        
        var currentActivity = Activity.Current;
        if (currentActivity != null)
        {
            logRecord.Attributes ??= new List<KeyValuePair<string, object>>();
            
            // Add custom correlation tags from activity
            var userId = currentActivity.GetTagItem("user.id");
            if (userId != null)
            {
                logRecord.Attributes.Add(
                    new KeyValuePair<string, object>("user.id", userId));
            }
        }
    }
}
```

## Registration

```csharp
// In TelemetryConfiguration
config.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.AddProcessor<MyLogProcessor>();
});

// In ASP.NET Core
services.AddLogging(logging =>
{
    logging.AddOpenTelemetry(options =>
    {
        options.AddProcessor<MyLogProcessor>();
    });
});
```

## Log Levels and Application Insights

| ILogger Level | AI Severity | Description |
|---------------|-------------|-------------|
| Trace | Verbose (0) | Detailed debug info |
| Debug | Verbose (0) | Debug diagnostics |
| Information | Information (1) | General information |
| Warning | Warning (2) | Warning messages |
| Error | Error (3) | Error events |
| Critical | Critical (4) | Critical failures |

## Automatic Correlation

Logs are automatically correlated with Activities:

```csharp
using (var activity = activitySource.StartActivity("ProcessOrder"))
{
    logger.LogInformation("Processing order {OrderId}", orderId);
    // ↑ Log automatically has activity.TraceId and activity.SpanId
    //   Shows up under the same operation in AI Portal
    
    try
    {
        ProcessOrder(orderId);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to process order {OrderId}", orderId);
        // ↑ Also correlated to same operation
    }
}
```

## Structured Logging

```csharp
// Structured properties become custom dimensions
logger.LogInformation(
    "Order {OrderId} processed for {UserId} with amount {Amount}",
    orderId,  // → customDimensions.OrderId
    userId,   // → customDimensions.UserId
    amount);  // → customDimensions.Amount
```

## Log Processor vs Activity Processor

| Aspect | Log Processor | Activity Processor |
|--------|--------------|-------------------|
| **Processes** | ILogger logs | Activities (traces) |
| **Type** | `BaseProcessor<LogRecord>` | `BaseProcessor<Activity>` |
| **Use for** | Log enrichment/filtering | Trace enrichment/filtering |
| **Correlation** | Automatic (TraceId/SpanId) | Creates correlation context |

## Migration from 2.x

### 2.x: Custom ILogger Provider

```csharp
// 2.x: Custom logger provider
public class CustomLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new CustomLogger(categoryName);
    }
}

public class CustomLogger : ILogger
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, 
        TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        // Custom logging logic
    }
}

services.AddLogging(builder =>
{
    builder.AddProvider(new CustomLoggerProvider());
});
```

### 3.x: Log Processor

```csharp
// 3.x: Log processor
public class CustomLogProcessor : BaseProcessor<LogRecord>
{
    public override void OnEnd(LogRecord logRecord)
    {
        // Custom log processing logic
        logRecord.Attributes ??= new List<KeyValuePair<string, object>>();
        logRecord.Attributes.Add(
            new KeyValuePair<string, object>("custom.field", "value"));
    }
}

config.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.AddProcessor<CustomLogProcessor>();
});
```

## Performance Considerations

- Log processors are called for **every log entry**
- Keep processing fast - avoid expensive operations
- Filter early to avoid processing unwanted logs
- Use log levels appropriately (don't log everything at Debug)

## See Also

- [activity-processor.md](activity-processor.md) - Activity processing
- [opentelemetry-pipeline.md](opentelemetry-pipeline.md) - Complete pipeline
- [concepts/configure-otel-builder.md](configure-otel-builder.md) - Configuration

## References

- **OpenTelemetry Logging**: https://opentelemetry.io/docs/specs/otel/logs/
- **ILogger**: Microsoft.Extensions.Logging
