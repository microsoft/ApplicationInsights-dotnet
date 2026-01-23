# ITelemetryProcessor → BaseProcessor\<Activity\> (Filtering with OnEnd)

**Category:** Transformation Pattern  
**Applies to:** Migration from Application Insights 2.x to 3.x  
**Related:** [activity-processor.md](../../concepts/activity-processor.md), [BaseProcessor OnEnd](../../api-reference/BaseProcessor/OnEnd.md)

## Overview

`ITelemetryProcessor` in 2.x is used to **filter or modify telemetry** before sending. In 3.x, this is replaced by `BaseProcessor<Activity>` using the **OnEnd** method for filtering.

## Key Differences

| 2.x ITelemetryProcessor | 3.x BaseProcessor\<Activity\> OnEnd |
|------------------------|-------------------------------------|
| Called before sending | Called when Activity ends |
| Can drop telemetry (don't call next) | Can drop by setting IsAllDataRequested = false |
| Chain of processors | Registered processors called in order |
| Works with all telemetry types | Works with Activity (traces) only |

## Basic Pattern

### 2.x: ITelemetryProcessor

```csharp
public class MyProcessor : ITelemetryProcessor
{
    private readonly ITelemetryProcessor _next;
    
    public MyProcessor(ITelemetryProcessor next)
    {
        _next = next;
    }
    
    public void Process(ITelemetry telemetry)
    {
        // Filter: Don't call _next to drop telemetry
        if (ShouldFilter(telemetry))
        {
            return; // Telemetry dropped
        }
        
        // Pass to next processor
        _next.Process(telemetry);
    }
}

// Registration
services.Configure<TelemetryConfiguration>(config =>
{
    config.TelemetryProcessorChainBuilder
        .Use(next => new MyProcessor(next))
        .Build();
});
```

### 3.x: BaseProcessor OnEnd

```csharp
public class MyProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Filter: Set IsAllDataRequested = false to drop telemetry
        if (ShouldFilter(activity))
        {
            activity.IsAllDataRequested = false; // Telemetry dropped
            return;
        }
        
        // Telemetry will be sent (default behavior)
    }
}

// Registration
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.AddProcessor(new MyProcessor());
    });
```

## Migration Examples

### Example 1: Filter Successful Dependencies

**2.x:**
```csharp
// From: ApplicationInsightsDemo/SuccessfulDependencyFilter.cs
public class SuccessfulDependencyFilter : ITelemetryProcessor
{
    private readonly ITelemetryProcessor _next;
    
    public SuccessfulDependencyFilter(ITelemetryProcessor next)
    {
        _next = next;
    }
    
    public void Process(ITelemetry telemetry)
    {
        // Filter out successful dependencies
        if (telemetry is DependencyTelemetry dependency)
        {
            if (dependency.Success == true)
            {
                return; // Don't send successful dependencies
            }
        }
        
        _next.Process(telemetry);
    }
}

services.Configure<TelemetryConfiguration>(config =>
{
    config.TelemetryProcessorChainBuilder
        .Use(next => new SuccessfulDependencyFilter(next))
        .Build();
});
```

**3.x:**
```csharp
public class SuccessfulDependencyFilter : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Filter out successful client activities (dependencies)
        if (activity.Kind == ActivityKind.Client)
        {
            if (activity.Status == ActivityStatusCode.Ok || 
                activity.Status == ActivityStatusCode.Unset)
            {
                activity.IsAllDataRequested = false; // Drop telemetry
                return;
            }
        }
        
        // Other activities sent normally
    }
}

services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.AddProcessor(new SuccessfulDependencyFilter());
    });
```

### Example 2: Filter Health Check Endpoints

**2.x:**
```csharp
public class HealthCheckFilter : ITelemetryProcessor
{
    private readonly ITelemetryProcessor _next;
    
    public void Process(ITelemetry telemetry)
    {
        if (telemetry is RequestTelemetry request)
        {
            // Filter health check endpoints
            if (request.Url?.AbsolutePath.StartsWith("/health") == true ||
                request.Url?.AbsolutePath.StartsWith("/ready") == true)
            {
                return; // Don't send health checks
            }
        }
        
        _next.Process(telemetry);
    }
}
```

**3.x:**
```csharp
public class HealthCheckFilter : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Filter health check requests
        if (activity.Kind == ActivityKind.Server)
        {
            var path = activity.GetTagItem("url.path") as string;
            
            if (path?.StartsWith("/health") == true ||
                path?.StartsWith("/ready") == true)
            {
                activity.IsAllDataRequested = false; // Drop telemetry
                return;
            }
        }
    }
}
```

### Example 3: Filter by HTTP Status Code

**2.x:**
```csharp
public class StatusCodeFilter : ITelemetryProcessor
{
    private readonly ITelemetryProcessor _next;
    
    public void Process(ITelemetry telemetry)
    {
        if (telemetry is RequestTelemetry request)
        {
            // Only send errors and slow requests
            if (int.TryParse(request.ResponseCode, out int statusCode))
            {
                bool isError = statusCode >= 400;
                bool isSlow = request.Duration.TotalSeconds > 5;
                
                if (!isError && !isSlow)
                {
                    return; // Drop successful fast requests
                }
            }
        }
        
        _next.Process(telemetry);
    }
}
```

**3.x:**
```csharp
public class StatusCodeFilter : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (activity.Kind == ActivityKind.Server)
        {
            var statusCode = activity.GetTagItem("http.response.status_code") as int?;
            
            // Only send errors and slow requests
            bool isError = statusCode >= 400;
            bool isSlow = activity.Duration.TotalSeconds > 5;
            
            if (!isError && !isSlow)
            {
                activity.IsAllDataRequested = false; // Drop telemetry
                return;
            }
        }
    }
}
```

### Example 4: Filter Synthetic Traffic

**2.x:**
```csharp
public class SyntheticTrafficFilter : ITelemetryProcessor
{
    private readonly ITelemetryProcessor _next;
    
    public void Process(ITelemetry telemetry)
    {
        // Filter synthetic traffic
        if (!string.IsNullOrEmpty(telemetry.Context.Operation.SyntheticSource))
        {
            return; // Don't send synthetic traffic
        }
        
        _next.Process(telemetry);
    }
}
```

**3.x:**
```csharp
public class SyntheticTrafficFilter : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Filter synthetic traffic
        var syntheticSource = activity.GetTagItem("ai.operation.synthetic_source");
        if (syntheticSource != null)
        {
            activity.IsAllDataRequested = false; // Drop telemetry
            return;
        }
    }
}
```

## Real-World Example: From ApplicationInsights-dotnet

**2.x Pattern:**
```csharp
// Typical 2.x filtering pattern
public class ClientErrorsOnlyFilter : ITelemetryProcessor
{
    private ITelemetryProcessor _next;
    
    public ClientErrorsOnlyFilter(ITelemetryProcessor next)
    {
        _next = next;
    }
    
    public void Process(ITelemetry telemetry)
    {
        if (telemetry is RequestTelemetry request)
        {
            if (request.Success == true)
            {
                return;
            }
        }
        
        _next.Process(telemetry);
    }
}
```

**3.x Pattern:**
```csharp
// From: ApplicationInsights-dotnet/WEB/Src/Web/WebTestActivityProcessor.cs
internal sealed class WebTestActivityProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (activity.Kind == ActivityKind.Server)
        {
            var userAgent = activity.GetTagItem("http.request.header.user_agent") as string;
            
            // Example of conditional enrichment (not filtering, but shows pattern)
            if (!string.IsNullOrEmpty(userAgent) && userAgent.Contains("AlwaysOn"))
            {
                activity.SetTag("ai.operation.synthetic_source", "AlwaysOn");
            }
            
            // Could filter here:
            // activity.IsAllDataRequested = false;
        }
    }
}
```

## Combining Filtering and Enrichment

### 2.x: Separate Initializer and Processor

```csharp
// Enrichment
public class MyInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Properties["enriched"] = "true";
    }
}

// Filtering
public class MyProcessor : ITelemetryProcessor
{
    private ITelemetryProcessor _next;
    
    public void Process(ITelemetry telemetry)
    {
        if (ShouldFilter(telemetry))
            return;
        _next.Process(telemetry);
    }
}
```

### 3.x: Single Processor

```csharp
public class MyProcessor : BaseProcessor<Activity>
{
    // Enrich at start
    public override void OnStart(Activity activity)
    {
        activity.SetTag("enriched", true);
    }
    
    // Filter at end
    public override void OnEnd(Activity activity)
    {
        if (ShouldFilter(activity))
        {
            activity.IsAllDataRequested = false;
            return;
        }
    }
}
```

## Advanced Filtering Patterns

### Pattern 1: Rate Limiting (Sample 10%)

**2.x:**
```csharp
public class SamplingProcessor : ITelemetryProcessor
{
    private readonly ITelemetryProcessor _next;
    private readonly Random _random = new Random();
    
    public void Process(ITelemetry telemetry)
    {
        // Keep only 10% of telemetry
        if (_random.NextDouble() > 0.1)
        {
            return; // Drop 90%
        }
        
        _next.Process(telemetry);
    }
}
```

**3.x:**
```csharp
public class SamplingProcessor : BaseProcessor<Activity>
{
    private readonly Random _random = new Random();
    
    public override void OnEnd(Activity activity)
    {
        // Keep only 10% of telemetry
        if (_random.NextDouble() > 0.1)
        {
            activity.IsAllDataRequested = false; // Drop 90%
            return;
        }
    }
}

// Or use built-in sampler:
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.AddTraceIdRatioBasedSampler(0.1); // 10% sampling
    });
```

### Pattern 2: Filter by Dependency Type

**2.x:**
```csharp
public class DependencyTypeFilter : ITelemetryProcessor
{
    private readonly ITelemetryProcessor _next;
    
    public void Process(ITelemetry telemetry)
    {
        if (telemetry is DependencyTelemetry dependency)
        {
            // Only send HTTP and SQL dependencies
            if (dependency.Type != "Http" && dependency.Type != "SQL")
            {
                return; // Drop other types
            }
        }
        
        _next.Process(telemetry);
    }
}
```

**3.x:**
```csharp
public class DependencyTypeFilter : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (activity.Kind == ActivityKind.Client)
        {
            // Check if HTTP
            var httpMethod = activity.GetTagItem("http.request.method");
            
            // Check if SQL
            var dbSystem = activity.GetTagItem("db.system");
            
            // Only keep HTTP and SQL
            if (httpMethod == null && dbSystem == null)
            {
                activity.IsAllDataRequested = false; // Drop other types
                return;
            }
        }
    }
}
```

### Pattern 3: Conditional Filtering Based on Environment

**2.x:**
```csharp
public class EnvironmentFilter : ITelemetryProcessor
{
    private readonly ITelemetryProcessor _next;
    private readonly IHostEnvironment _environment;
    
    public void Process(ITelemetry telemetry)
    {
        // In development, only send errors
        if (_environment.IsDevelopment())
        {
            if (telemetry is RequestTelemetry request && request.Success == true)
            {
                return;
            }
            if (telemetry is DependencyTelemetry dependency && dependency.Success == true)
            {
                return;
            }
        }
        
        _next.Process(telemetry);
    }
}
```

**3.x:**
```csharp
public class EnvironmentFilter : BaseProcessor<Activity>
{
    private readonly IHostEnvironment _environment;
    
    public EnvironmentFilter(IHostEnvironment environment)
    {
        _environment = environment;
    }
    
    public override void OnEnd(Activity activity)
    {
        // In development, only send errors
        if (_environment.IsDevelopment())
        {
            if (activity.Status == ActivityStatusCode.Ok || 
                activity.Status == ActivityStatusCode.Unset)
            {
                activity.IsAllDataRequested = false;
                return;
            }
        }
    }
}

services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        var environment = builder.Services.BuildServiceProvider()
            .GetRequiredService<IHostEnvironment>();
        builder.AddProcessor(new EnvironmentFilter(environment));
    });
```

## Filtering vs Sampling

### Filtering (Deterministic)
```csharp
// Always drop specific telemetry
public override void OnEnd(Activity activity)
{
    if (activity.GetTagItem("url.path") == "/health")
    {
        activity.IsAllDataRequested = false; // Always drop health checks
    }
}
```

### Sampling (Probabilistic)
```csharp
// Use OpenTelemetry built-in samplers
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        // Ratio-based sampling (10%)
        builder.AddTraceIdRatioBasedSampler(0.1);
        
        // Parent-based sampling (respect parent decision)
        builder.AddParentBasedSampler(new TraceIdRatioBasedSampler(0.1));
    });
```

## Performance Considerations

### 2.x: Processor Chain Overhead

```csharp
// Each processor in chain called sequentially
// If 5 processors, every telemetry goes through 5 function calls
public void Process(ITelemetry telemetry)
{
    // ... processing ...
    _next.Process(telemetry); // Call next processor
}
```

### 3.x: Parallel Processor Execution

```csharp
// OpenTelemetry processors can be optimized by SDK
// OnEnd called for each registered processor
public override void OnEnd(Activity activity)
{
    // ... processing ...
    // No need to call next - handled by SDK
}
```

## IsAllDataRequested vs Recorded

```csharp
public override void OnEnd(Activity activity)
{
    // Option 1: IsAllDataRequested = false (Preferred)
    // Tells exporters not to send this activity
    activity.IsAllDataRequested = false;
    
    // Option 2: activity.Recorded (Read-only, set at Activity creation)
    // Can check if Activity is being recorded
    if (!activity.Recorded)
    {
        // Activity not being recorded - skip processing
        return;
    }
}
```

**Use `IsAllDataRequested = false` for filtering in processors.**

## See Also

- [activity-processor.md](../../concepts/activity-processor.md) - Processor concept guide
- [OnEnd.md](../../api-reference/BaseProcessor/OnEnd.md) - OnEnd API reference
- [enrichment-with-onstart.md](./enrichment-with-onstart.md) - Enrichment pattern (OnStart)
- [IsAllDataRequested.md](../../api-reference/Activity/IsAllDataRequested.md) - Filtering property
- [filtering.md](../../common-scenarios/filtering-telemetry.md) - Filtering scenario guide
