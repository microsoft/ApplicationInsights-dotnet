---
title: Activity Processor (BaseProcessor<Activity>)
category: concept
applies-to: 3.x
related:
  - concepts/activity-vs-telemetry.md
  - api-reference/BaseProcessor/OnStart.md
  - api-reference/BaseProcessor/OnEnd.md
  - transformations/ITelemetryInitializer/to-activity-processor.md
source: OpenTelemetry.BaseProcessor<T>, ApplicationInsights 3.x processors
---

# Activity Processor (BaseProcessor<Activity>)

## Overview

In Application Insights 3.x, `BaseProcessor<Activity>` from OpenTelemetry is the replacement for both `ITelemetryInitializer` and `ITelemetryProcessor` from 2.x. It allows you to enrich, filter, or modify telemetry data (represented as `Activity` objects) as it flows through the pipeline.

## In 2.x: ITelemetryInitializer and ITelemetryProcessor

Application Insights 2.x used two separate interfaces:

```csharp
// For enrichment - ran early in pipeline
public interface ITelemetryInitializer
{
    void Initialize(ITelemetry telemetry);
}

// For filtering/processing - ran later with chaining
public interface ITelemetryProcessor
{
    ITelemetryProcessor Next { get; set; }
    void Process(ITelemetry item);
}

// Usage
public class MyInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        if (telemetry is RequestTelemetry request)
        {
            request.Properties["custom"] = "value";
        }
    }
}

public class MyProcessor : ITelemetryProcessor
{
    private ITelemetryProcessor next;
    
    public MyProcessor(ITelemetryProcessor next)
    {
        this.next = next;
    }
    
    public void Process(ITelemetry item)
    {
        if (ShouldKeep(item))
        {
            next.Process(item);  // Manual chaining
        }
    }
}
```

## In 3.x: BaseProcessor<Activity>

OpenTelemetry 3.x uses a unified `BaseProcessor<Activity>` for both enrichment and filtering:

```csharp
// Source: opentelemetry-dotnet/src/OpenTelemetry/BaseProcessor.cs
public abstract class BaseProcessor<T> : IDisposable
{
    // Called when Activity starts (for early enrichment)
    public virtual void OnStart(T data) { }
    
    // Called when Activity ends (for final enrichment/filtering)
    public virtual void OnEnd(T data) { }
    
    // Called during flush operations
    protected virtual bool OnForceFlush(int timeoutMilliseconds) { return true; }
    
    // Called during shutdown
    protected virtual bool OnShutdown(int timeoutMilliseconds) { return true; }
    
    // Resource cleanup
    protected virtual void Dispose(bool disposing) { }
}

// Usage for Activity processing
public class MyActivityProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        // Enrich when activity starts
        activity.SetTag("custom.tag", "value");
    }
    
    public override void OnEnd(Activity activity)
    {
        // Enrich or filter when activity completes
        if (!ShouldKeep(activity))
        {
            // Drop activity by removing Recorded flag
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
        }
    }
}
```

## Real Example from Application Insights 3.x

### WebTestActivityProcessor (from AI 3.x codebase)

```csharp
// Source: ApplicationInsights-dotnet/WEB/Src/Web/Web/WebTestActivityProcessor.cs
namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Diagnostics;
    using OpenTelemetry;

    internal sealed class WebTestActivityProcessor : BaseProcessor<Activity>
    {
        public override void OnEnd(Activity activity)
        {
            if (activity == null)
            {
                return;
            }

            // Check for synthetic test header
            var syntheticTestTag = activity.GetTagItem("http.request.header.synthetic_test");
            if (syntheticTestTag != null)
            {
                // Set synthetic source
                activity.SetTag("ai.operation.synthetic_source", "Application Insights Availability Monitoring");
            }
        }
    }
}
```

This processor:
1. Inherits from `BaseProcessor<Activity>`
2. Overrides `OnEnd` to process completed activities
3. Checks for HTTP headers via tags
4. Sets synthetic source attribute for availability tests

### SyntheticUserAgentActivityProcessor (from AI 3.x codebase)

```csharp
// Source: ApplicationInsights-dotnet/WEB/Src/Web/Web/SyntheticUserAgentActivityProcessor.cs
namespace Microsoft.ApplicationInsights.Web
{
    using System.Diagnostics;
    using OpenTelemetry;

    internal sealed class SyntheticUserAgentActivityProcessor : BaseProcessor<Activity>
    {
        public override void OnEnd(Activity activity)
        {
            if (activity == null)
            {
                return;
            }

            var userAgent = activity.GetTagItem("http.request.header.user_agent") as string;
            
            if (!string.IsNullOrEmpty(userAgent))
            {
                // Check if user agent indicates bot/synthetic traffic
                if (userAgent.Contains("bot") || userAgent.Contains("spider"))
                {
                    activity.SetTag("ai.operation.synthetic_source", userAgent);
                }
            }
        }
    }
}
```

## Key Differences from 2.x

| Aspect | 2.x (Initializer/Processor) | 3.x (BaseProcessor<Activity>) |
|--------|---------------------------|-------------------------------|
| **Interfaces** | Two separate interfaces | Single base class |
| **Chaining** | Manual via `Next` property | Automatic by OpenTelemetry SDK |
| **Timing** | Initialize = early, Process = later | OnStart = early, OnEnd = later |
| **Telemetry Type** | ITelemetry (base interface) | Activity (concrete class) |
| **Filtering** | Return early or don't call Next | Set ActivityTraceFlags.Recorded = false |
| **Lifecycle** | No lifecycle hooks | OnForceFlush, OnShutdown, Dispose |
| **Type Checking** | `if (telemetry is RequestTelemetry)` | `if (activity.Kind == ActivityKind.Server)` |

## When to Use OnStart vs OnEnd

### OnStart - Early Enrichment
- Called when Activity is created/started
- Use for adding context available at start time
- Lightweight operations only (critical path)
- Cannot filter (activity hasn't completed yet)

```csharp
public override void OnStart(Activity activity)
{
    // Add tags based on initial context
    activity.SetTag("environment", Environment.GetEnvironmentVariable("ENV"));
    activity.SetTag("machine.name", Environment.MachineName);
}
```

### OnEnd - Final Enrichment or Filtering
- Called when Activity completes
- Use for enrichment based on outcome (status, duration, etc.)
- Use for filtering/sampling decisions
- Access to complete activity information

```csharp
public override void OnEnd(Activity activity)
{
    // Enrich based on outcome
    if (activity.Status == ActivityStatusCode.Error)
    {
        activity.SetTag("requires.investigation", true);
    }
    
    // Filter based on conditions
    if (activity.Duration < TimeSpan.FromMilliseconds(10))
    {
        activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
    }
}
```

## Common Patterns

### Pattern 1: Enrichment (replaces ITelemetryInitializer)

```csharp
// 2.x: ITelemetryInitializer
public class CustomPropertiesInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.Cloud.RoleName = "MyService";
        telemetry.Properties["app.version"] = "1.0.0";
    }
}

// 3.x: BaseProcessor<Activity>
public class CustomPropertiesProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Cloud role is set via Resource (see resource-detector.md)
        activity.SetTag("app.version", "1.0.0");
    }
}
```

### Pattern 2: Filtering (replaces ITelemetryProcessor)

```csharp
// 2.x: ITelemetryProcessor
public class SuccessfulDependencyFilter : ITelemetryProcessor
{
    private ITelemetryProcessor next;
    
    public void Process(ITelemetry item)
    {
        if (item is DependencyTelemetry dep && dep.Success)
        {
            return;  // Don't call next - drops telemetry
        }
        next.Process(item);
    }
}

// 3.x: BaseProcessor<Activity>
public class SuccessfulDependencyFilterProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Client activities = dependencies
        if (activity.Kind == ActivityKind.Client && 
            activity.Status == ActivityStatusCode.Ok)
        {
            // Drop successful client calls
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
        }
    }
}
```

### Pattern 3: Conditional Enrichment

```csharp
// 3.x: Enrich based on conditions
public class ClientErrorProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Only for server activities (requests)
        if (activity.Kind != ActivityKind.Server)
        {
            return;
        }
        
        // Check HTTP status code
        var statusCode = activity.GetTagItem("http.response.status_code");
        if (statusCode != null && int.TryParse(statusCode.ToString(), out int code))
        {
            if (code >= 400 && code < 500)
            {
                // Override to success for 4xx errors
                activity.SetStatus(ActivityStatusCode.Ok);
                activity.SetTag("override.4xx", true);
            }
        }
    }
}
```

## Registration

### 2.x Registration

```csharp
// Initializers
services.AddSingleton<ITelemetryInitializer, MyInitializer>();

// Processors
services.AddApplicationInsightsTelemetryProcessor<MyProcessor>();
```

### 3.x Registration

```csharp
// During configuration
var config = new TelemetryConfiguration();
config.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.AddProcessor(new MyActivityProcessor());
});

// Or in ASP.NET Core
services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = "...";
});

services.Configure<TelemetryConfiguration>(config =>
{
    config.ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.AddProcessor(new MyActivityProcessor());
    });
});
```

## Performance Considerations

1. **OnStart and OnEnd are synchronous and critical path**
   - Keep logic fast and non-blocking
   - Avoid I/O, network calls, or heavy computation
   - Don't throw exceptions

2. **Use ActivityKind for type filtering**
   ```csharp
   // Fast - checks enum
   if (activity.Kind == ActivityKind.Server) { }
   
   // Avoid - no type checking needed in 3.x
   // if (telemetry is RequestTelemetry) { }
   ```

3. **Tag access is efficient**
   ```csharp
   // Good - direct tag access
   var value = activity.GetTagItem("key");
   
   // Better for enumeration - use EnumerateTagObjects()
   foreach (var tag in activity.EnumerateTagObjects())
   {
       // Process tags
   }
   ```

## Thread Safety

- `OnStart` and `OnEnd` must be thread-safe
- Can be called concurrently for different activities
- SDK handles thread safety for processor registration/lifecycle
- No manual synchronization needed for per-activity operations

## See Also

- [activity-vs-telemetry.md](activity-vs-telemetry.md) - Understanding Activity
- [activity-kinds.md](activity-kinds.md) - ActivityKind enum details
- [api-reference/BaseProcessor/OnStart.md](../api-reference/BaseProcessor/OnStart.md) - OnStart API
- [api-reference/BaseProcessor/OnEnd.md](../api-reference/BaseProcessor/OnEnd.md) - OnEnd API
- [transformations/ITelemetryInitializer/to-activity-processor.md](../transformations/ITelemetryInitializer/to-activity-processor.md) - Migration guide
- [transformations/ITelemetryProcessor/to-activity-processor.md](../transformations/ITelemetryProcessor/to-activity-processor.md) - Migration guide

## References

- **OpenTelemetry BaseProcessor**: `opentelemetry-dotnet/src/OpenTelemetry/BaseProcessor.cs`
- **AI 3.x Processors**: `ApplicationInsights-dotnet/WEB/Src/Web/Web/*ActivityProcessor.cs`
- **OpenTelemetry Docs**: https://opentelemetry.io/docs/specs/otel/trace/sdk/#span-processor
