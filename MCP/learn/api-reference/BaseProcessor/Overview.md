---
title: BaseProcessor<Activity> - Activity Processing Overview
category: api-reference
applies-to: 3.x
namespace: OpenTelemetry
related:
  - concepts/activity-processor.md
  - transformations/ITelemetryInitializer/overview.md
  - transformations/ITelemetryProcessor/overview.md
source: OpenTelemetry.BaseProcessor<T>
---

# BaseProcessor&lt;Activity&gt;

## Signature

```csharp
namespace OpenTelemetry
{
    public abstract class BaseProcessor<T> : IDisposable where T : class
    {
        public virtual void OnStart(T data) { }
        public virtual void OnEnd(T data) { }
        public virtual void ForceFlush(int timeoutMilliseconds = -1) { return true; }
        public virtual void Shutdown(int timeoutMilliseconds = -1) { return true; }
        protected virtual void Dispose(bool disposing) { }
    }
}
```

## Description

Base class for processing Activities (traces) in OpenTelemetry. Replaces both `ITelemetryInitializer` and `ITelemetryProcessor` from Application Insights 2.x.

## 2.x Equivalent

```csharp
// 2.x: Initializer (enrichment)
public class MyInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.Cloud.RoleName = "MyService";
    }
}

// 2.x: Processor (filtering)
public class MyProcessor : ITelemetryProcessor
{
    public void Process(ITelemetry item)
    {
        if (ShouldKeep(item))
        {
            next.Process(item);
        }
    }
}

// 3.x: Single BaseProcessor (both enrichment and filtering)
public class MyProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Enrich
        activity.SetTag("service.name", "MyService");
        
        // Filter
        if (!ShouldKeep(activity))
        {
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
        }
    }
}
```

## Lifecycle Methods

### OnStart

Called when an Activity **starts**, before the operation executes.

```csharp
public override void OnStart(Activity activity)
{
    // Called BEFORE the operation executes
    activity.SetTag("processor.start_time", DateTime.UtcNow);
}
```

**Use OnStart for:**
- Early tagging
- Start timestamps
- Propagating context
- Recording initial state

### OnEnd

Called when an Activity **ends**, after the operation completes.

```csharp
public override void OnEnd(Activity activity)
{
    // Called AFTER the operation completes
    activity.SetTag("processor.end_time", DateTime.UtcNow);
    
    // Status and duration are available
    if (activity.Status == ActivityStatusCode.Error)
    {
        activity.SetTag("needs_review", true);
    }
}
```

**Use OnEnd for:**
- Final enrichment
- Filtering based on results
- Status-based logic
- Recording final state
- Most common use case!

## Basic Processor Example

```csharp
using OpenTelemetry;
using System.Diagnostics;

public class MyProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Add custom dimension
        activity.SetTag("environment", "production");
        activity.SetTag("processor_version", "1.0.0");
    }
}
```

## Registration

```csharp
var config = new TelemetryConfiguration();
config.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.AddProcessor<MyProcessor>();
    
    // Or with instance
    builder.AddProcessor(new MyProcessor());
    
    // Or with factory
    builder.AddProcessor(sp => 
        new MyProcessor(sp.GetRequiredService<IConfiguration>()));
});
```

## Real-World Examples

### Example 1: Environment Enrichment

```csharp
public class EnvironmentProcessor : BaseProcessor<Activity>
{
    private readonly string environment;
    private readonly string version;
    
    public EnvironmentProcessor(IConfiguration config)
    {
        environment = config["Environment"];
        version = config["Version"];
    }
    
    public override void OnEnd(Activity activity)
    {
        activity.SetTag("deployment.environment", environment);
        activity.SetTag("service.version", version);
        activity.SetTag("host.name", Environment.MachineName);
    }
}
```

### Example 2: Health Check Filter

```csharp
public class HealthCheckFilterProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (activity.Kind == ActivityKind.Server)
        {
            var path = activity.GetTagItem("url.path") as string;
            
            if (path == "/health" || path == "/healthz" || path == "/ready")
            {
                // Drop health check requests
                activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
            }
        }
    }
}
```

### Example 3: Synthetic Traffic Detection

```csharp
// From ApplicationInsights-dotnet/WEB/Src/Web/SyntheticUserAgentActivityProcessor.cs
internal sealed class SyntheticUserAgentActivityProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (activity.Kind == ActivityKind.Server)
        {
            var userAgent = activity.GetTagItem("http.request.header.user_agent") as string;
            
            if (!string.IsNullOrEmpty(userAgent))
            {
                if (userAgent.Contains("AlwaysOn") || 
                    userAgent.Contains("AppInsights") ||
                    userAgent.Contains("HealthCheck"))
                {
                    activity.SetTag("ai.operation.synthetic_source", 
                        "Application Insights Availability Monitoring");
                }
            }
        }
    }
}
```

### Example 4: User Context Enrichment

```csharp
public class UserContextProcessor : BaseProcessor<Activity>
{
    private readonly IHttpContextAccessor httpContextAccessor;
    
    public UserContextProcessor(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }
    
    public override void OnEnd(Activity activity)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            activity.SetTag("user.id", httpContext.User.Identity.Name);
            
            var tenantClaim = httpContext.User.FindFirst("tenant_id");
            if (tenantClaim != null)
            {
                activity.SetTag("tenant.id", tenantClaim.Value);
            }
        }
    }
}
```

## OnStart vs OnEnd

| Aspect | OnStart | OnEnd |
|--------|---------|-------|
| **Timing** | Before operation | After operation |
| **Status** | Unknown | Known (Ok/Error) |
| **Duration** | 0 | Complete |
| **Result** | Not available | Available |
| **Common use** | Early tagging | Enrichment, filtering |

```csharp
public class TimingProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        // Status not set yet
        // Duration = 0
        activity.SetTag("start_timestamp", DateTime.UtcNow);
    }
    
    public override void OnEnd(Activity activity)
    {
        // Status is set (Ok/Error)
        // Duration is complete
        activity.SetTag("end_timestamp", DateTime.UtcNow);
        activity.SetTag("duration_ms", activity.Duration.TotalMilliseconds);
        
        if (activity.Status == ActivityStatusCode.Error)
        {
            activity.SetTag("error_logged", true);
        }
    }
}
```

## Filtering Activities

To drop an Activity, clear the `Recorded` flag:

```csharp
public class FilterProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (ShouldDrop(activity))
        {
            // Remove Recorded flag - activity won't be exported
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
        }
    }
    
    private bool ShouldDrop(Activity activity)
    {
        // Health checks
        if (activity.GetTagItem("url.path") as string == "/health")
            return true;
            
        // Successful dependencies under 100ms
        if (activity.Kind == ActivityKind.Client && 
            activity.Status == ActivityStatusCode.Ok &&
            activity.Duration.TotalMilliseconds < 100)
            return true;
            
        return false;
    }
}
```

## Processor Chain

Processors are called in registration order:

```csharp
config.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.AddProcessor<Processor1>();  // Called first
    builder.AddProcessor<Processor2>();  // Called second
    builder.AddProcessor<Processor3>();  // Called third
});

// Execution for each Activity:
// 1. Processor1.OnStart()
// 2. Processor2.OnStart()
// 3. Processor3.OnStart()
// --- Activity executes ---
// 4. Processor1.OnEnd()
// 5. Processor2.OnEnd()
// 6. Processor3.OnEnd()
// 7. Export to Azure Monitor
```

No manual chaining needed (unlike 2.x `ITelemetryProcessor`).

## Dependency Injection

```csharp
public class MyProcessor : BaseProcessor<Activity>
{
    private readonly ILogger<MyProcessor> logger;
    private readonly IConfiguration config;
    
    public MyProcessor(ILogger<MyProcessor> logger, IConfiguration config)
    {
        this.logger = logger;
        this.config = config;
    }
    
    public override void OnEnd(Activity activity)
    {
        var feature = config["FeatureFlags:NewFeature"];
        activity.SetTag("feature.new_feature", feature);
        
        logger.LogDebug("Processed activity: {ActivityName}", 
            activity.DisplayName);
    }
}

// Registration with DI
services.AddSingleton<MyProcessor>();
config.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.AddProcessor(sp => sp.GetRequiredService<MyProcessor>());
});
```

## Performance Considerations

✅ **Do:**
- Keep processing fast (< 1ms)
- Cache expensive lookups
- Use OnEnd for most logic
- Check activity.Kind before processing

❌ **Avoid:**
- Heavy I/O operations
- Synchronous HTTP calls
- Large allocations
- Complex regex in hot path

```csharp
// ✅ GOOD: Fast processing
public class FastProcessor : BaseProcessor<Activity>
{
    private readonly string cachedValue;
    
    public FastProcessor()
    {
        cachedValue = GetExpensiveValue();  // Cache at construction
    }
    
    public override void OnEnd(Activity activity)
    {
        activity.SetTag("cached_value", cachedValue);  // Fast
    }
}

// ❌ BAD: Slow processing
public class SlowProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        var value = GetExpensiveValue();  // Called for EVERY activity!
        activity.SetTag("value", value);
    }
}
```

## Disposal

Processors are disposed when OpenTelemetry shuts down:

```csharp
public class DisposableProcessor : BaseProcessor<Activity>
{
    private readonly HttpClient httpClient;
    
    public DisposableProcessor()
    {
        httpClient = new HttpClient();
    }
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            httpClient?.Dispose();
        }
        base.Dispose(disposing);
    }
}
```

## Common Patterns

### Pattern 1: Conditional Processing

```csharp
public override void OnEnd(Activity activity)
{
    // Only process HTTP requests
    if (activity.Kind == ActivityKind.Server)
    {
        EnrichHttpRequest(activity);
    }
}
```

### Pattern 2: Status-Based Logic

```csharp
public override void OnEnd(Activity activity)
{
    if (activity.Status == ActivityStatusCode.Error)
    {
        activity.SetTag("requires_investigation", true);
        activity.SetTag("alert_sent", SendAlert(activity));
    }
}
```

### Pattern 3: Tag-Based Routing

```csharp
public override void OnEnd(Activity activity)
{
    var controller = activity.GetTagItem("http.route") as string;
    
    if (controller?.StartsWith("/api/admin") == true)
    {
        activity.SetTag("api.category", "admin");
        activity.SetTag("requires_audit", true);
    }
}
```

## See Also

- [concepts/activity-processor.md](../../concepts/activity-processor.md) - Complete processor guide
- [transformations/ITelemetryInitializer/overview.md](../../transformations/ITelemetryInitializer/overview.md) - Migrating initializers
- [transformations/ITelemetryProcessor/overview.md](../../transformations/ITelemetryProcessor/overview.md) - Migrating processors

## References

- **Source**: OpenTelemetry.BaseProcessor&lt;T&gt;
- **Real Example**: [ApplicationInsights-dotnet/WEB/Src/Web/SyntheticUserAgentActivityProcessor.cs](https://github.com/microsoft/ApplicationInsights-dotnet/blob/main/WEB/Src/Web/SyntheticUserAgentActivityProcessor.cs)
