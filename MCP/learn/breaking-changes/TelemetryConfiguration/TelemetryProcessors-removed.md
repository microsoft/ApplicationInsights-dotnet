# TelemetryConfiguration.TelemetryProcessors Removed

**Category:** Breaking Change  
**Applies to:** TelemetryConfiguration API  
**Migration Effort:** Medium  
**Related:** [BaseProcessor-OnEnd.md](../../api-reference/BaseProcessor/OnEnd.md), [ActivityStatusCode.md](../../concepts/activity-status-code.md), [filtering.md](../../common-scenarios/filtering.md)

## Change Summary

The `TelemetryProcessors` collection and `TelemetryProcessorChainBuilder` have been removed from `TelemetryConfiguration` in 3.x. The `ITelemetryProcessor` pattern no longer exists. Use OpenTelemetry `BaseProcessor<Activity>` with the `OnEnd()` method to filter, sample, or drop telemetry.

## API Comparison

### 2.x API

```csharp
// Source: ApplicationInsights-dotnet-2x/BASE/src/Microsoft.ApplicationInsights/Extensibility/ITelemetryProcessor.cs:8-15
public interface ITelemetryProcessor
{
    void Process(ITelemetry item);
}

// TelemetryConfiguration usage
public class TelemetryConfiguration
{
    public ReadOnlyCollection<ITelemetryProcessor> TelemetryProcessors { get; }
    public TelemetryProcessorChainBuilder TelemetryProcessorChainBuilder { get; }
}
```

### 3.x API

```csharp
// REMOVED: ITelemetryProcessor interface does not exist
// REMOVED: TelemetryProcessors collection does not exist  
// REMOVED: TelemetryProcessorChainBuilder class does not exist

// Replacement: BaseProcessor<Activity> with OnEnd
using OpenTelemetry;

public class MyFilterProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Filter/drop telemetry by clearing Recorded flag
        if (ShouldFilter(activity))
        {
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
        }
    }
}
```

## Why It Changed

| Reason | Description |
|--------|-------------|
| **OpenTelemetry Standard** | OpenTelemetry uses processors with OnEnd, not ITelemetryProcessor chain |
| **Simplified Model** | No chain building - processors are registered directly |
| **Better Performance** | Activity-based filtering is more efficient |
| **Clear Semantics** | OnEnd for filtering, OnStart for enrichment |

## Migration Strategies

### Option 1: Simple Filtering

**When to use:** Filter out telemetry based on conditions.

**2.x:**
```csharp
// Source: ApplicationInsightsDemo/SuccessfulDependencyFilter.cs pattern
public class SuccessfulDependencyFilter : ITelemetryProcessor
{
    private ITelemetryProcessor Next { get; set; }
    
    public SuccessfulDependencyFilter(ITelemetryProcessor next)
    {
        this.Next = next;
    }
    
    public void Process(ITelemetry item)
    {
        // Filter out successful dependencies
        if (item is DependencyTelemetry dependency && dependency.Success == true)
        {
            return;  // Don't pass to next processor
        }
        
        this.Next.Process(item);
    }
}

// Registration
public void ConfigureServices(IServiceCollection services)
{
    services.AddApplicationInsightsTelemetry();
    services.AddApplicationInsightsTelemetryProcessor<SuccessfulDependencyFilter>();
}
```

**3.x:**
```csharp
using System.Diagnostics;
using OpenTelemetry;

public class SuccessfulDependencyFilter : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Filter out successful dependencies
        if (activity?.Kind == ActivityKind.Client)
        {
            if (activity.Status == ActivityStatusCode.Ok || activity.Status == ActivityStatusCode.Unset)
            {
                // Drop this telemetry by clearing the Recorded flag
                activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
            }
        }
    }
}

// Registration in Program.cs
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.ConfigureOpenTelemetryTracerProvider(builder =>
{
    builder.AddProcessor<SuccessfulDependencyFilter>();
});
```

### Option 2: Conditional Processing

**When to use:** Drop telemetry based on multiple criteria.

**2.x:**
```csharp
public class HealthCheckFilter : ITelemetryProcessor
{
    private ITelemetryProcessor Next { get; set; }
    
    public HealthCheckFilter(ITelemetryProcessor next)
    {
        this.Next = next;
    }
    
    public void Process(ITelemetry item)
    {
        if (item is RequestTelemetry request)
        {
            // Filter out health check requests
            if (request.Url?.AbsolutePath?.Contains("/health") == true)
            {
                return;
            }
        }
        
        this.Next.Process(item);
    }
}
```

**3.x:**
```csharp
using System.Diagnostics;
using OpenTelemetry;

public class HealthCheckFilter : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (activity?.Kind == ActivityKind.Server)
        {
            var route = activity.GetTagItem("http.route")?.ToString();
            var target = activity.GetTagItem("http.target")?.ToString();
            
            // Filter out health check requests
            if (route == "/health" || target?.Contains("/health") == true)
            {
                activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
            }
        }
    }
}
```

### Option 3: Sampling

**When to use:** Reduce telemetry volume by sampling.

**2.x:**
```csharp
public class CustomSamplingProcessor : ITelemetryProcessor
{
    private readonly double samplingPercentage;
    private ITelemetryProcessor Next { get; set; }
    
    public CustomSamplingProcessor(ITelemetryProcessor next, double samplingPercentage)
    {
        this.Next = next;
        this.samplingPercentage = samplingPercentage;
    }
    
    public void Process(ITelemetry item)
    {
        double percentage = 100 * SamplingScoreGenerator.GetSamplingScore(item);
        if (percentage < samplingPercentage)
        {
            this.Next.Process(item);
        }
    }
}
```

**3.x:**
```csharp
// Use built-in OpenTelemetry sampling
builder.Services.ConfigureOpenTelemetryTracerProvider(builder =>
{
    // Parent-based sampler with 10% sampling
    builder.SetSampler(new ParentBasedSampler(
        new TraceIdRatioBasedSampler(0.1)));  // 10% sampling
});

// Or custom sampling processor
public class CustomSamplingProcessor : BaseProcessor<Activity>
{
    private readonly double samplingRatio;
    
    public CustomSamplingProcessor(double samplingRatio)
    {
        this.samplingRatio = samplingRatio;
    }
    
    public override void OnEnd(Activity activity)
    {
        // Simple hash-based sampling
        if (activity != null)
        {
            var hash = activity.TraceId.ToHexString().GetHashCode();
            var normalizedHash = Math.Abs(hash) / (double)int.MaxValue;
            
            if (normalizedHash > samplingRatio)
            {
                activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
            }
        }
    }
}
```

## Common Scenarios

### Scenario 1: Filter by URL Pattern

**2.x:**
```csharp
public class UrlPatternFilter : ITelemetryProcessor
{
    private ITelemetryProcessor Next { get; set; }
    private readonly Regex excludePattern;
    
    public UrlPatternFilter(ITelemetryProcessor next)
    {
        this.Next = next;
        this.excludePattern = new Regex(@"/api/internal/.*");
    }
    
    public void Process(ITelemetry item)
    {
        if (item is RequestTelemetry request && 
            excludePattern.IsMatch(request.Url?.AbsolutePath ?? ""))
        {
            return;
        }
        
        this.Next.Process(item);
    }
}
```

**3.x:**
```csharp
using System.Diagnostics;
using System.Text.RegularExpressions;
using OpenTelemetry;

public class UrlPatternFilter : BaseProcessor<Activity>
{
    private readonly Regex excludePattern = new Regex(@"/api/internal/.*");
    
    public override void OnEnd(Activity activity)
    {
        if (activity?.Kind == ActivityKind.Server)
        {
            var route = activity.GetTagItem("http.route")?.ToString();
            var target = activity.GetTagItem("http.target")?.ToString();
            
            if (excludePattern.IsMatch(route ?? target ?? ""))
            {
                activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
            }
        }
    }
}
```

### Scenario 2: Filter by Response Code

**2.x:**
```csharp
public class SuccessfulRequestFilter : ITelemetryProcessor
{
    private ITelemetryProcessor Next { get; set; }
    
    public SuccessfulRequestFilter(ITelemetryProcessor next)
    {
        this.Next = next;
    }
    
    public void Process(ITelemetry item)
    {
        if (item is RequestTelemetry request && 
            request.Success == true && 
            request.ResponseCode == "200")
        {
            return;  // Don't track successful 200 responses
        }
        
        this.Next.Process(item);
    }
}
```

**3.x:**
```csharp
using System.Diagnostics;
using OpenTelemetry;

public class SuccessfulRequestFilter : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (activity?.Kind == ActivityKind.Server)
        {
            var statusCode = activity.GetTagItem("http.response.status_code")?.ToString();
            
            if (activity.Status == ActivityStatusCode.Ok && statusCode == "200")
            {
                activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
            }
        }
    }
}
```

### Scenario 3: Filter by Custom Property

**2.x:**
```csharp
public class CustomPropertyFilter : ITelemetryProcessor
{
    private ITelemetryProcessor Next { get; set; }
    
    public CustomPropertyFilter(ITelemetryProcessor next)
    {
        this.Next = next;
    }
    
    public void Process(ITelemetry item)
    {
        if (item is ISupportProperties itemWithProps)
        {
            if (itemWithProps.Properties.TryGetValue("ExcludeFromTelemetry", out string value) 
                && value == "true")
            {
                return;
            }
        }
        
        this.Next.Process(item);
    }
}
```

**3.x:**
```csharp
using System.Diagnostics;
using OpenTelemetry;

public class CustomPropertyFilter : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        var excludeTag = activity?.GetTagItem("exclude.from.telemetry")?.ToString();
        
        if (excludeTag == "true")
        {
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
        }
    }
}
```

### Scenario 4: Chain Multiple Filters

**2.x:**
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddApplicationInsightsTelemetry();
    
    // Build processor chain
    services.AddApplicationInsightsTelemetryProcessor<HealthCheckFilter>();
    services.AddApplicationInsightsTelemetryProcessor<SuccessfulDependencyFilter>();
    services.AddApplicationInsightsTelemetryProcessor<UrlPatternFilter>();
}
```

**3.x:**
```csharp
// Program.cs - processors are called in registration order
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.ConfigureOpenTelemetryTracerProvider(builder =>
{
    // Processors are called in order: OnStart then OnEnd
    builder.AddProcessor<HealthCheckFilter>();
    builder.AddProcessor<SuccessfulDependencyFilter>();
    builder.AddProcessor<UrlPatternFilter>();
});
```

## Filtering vs. Dropping Telemetry

### Marking as Not Recorded

```csharp
// Drop telemetry - won't be exported
public override void OnEnd(Activity activity)
{
    activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
}
```

### Setting Error Status

```csharp
// Keep telemetry but mark as error
public override void OnEnd(Activity activity)
{
    activity.SetStatus(ActivityStatusCode.Error, "Validation failed");
}
```

### Modifying Before Export

```csharp
// Redact sensitive data before export
public override void OnEnd(Activity activity)
{
    var url = activity.GetTagItem("url.full")?.ToString();
    if (url != null)
    {
        // Redact query string
        activity.SetTag("url.full", url.Split('?')[0]);
    }
}
```

## Built-in Sampling Options

### Trace ID Ratio-Based Sampling

```csharp
builder.Services.ConfigureOpenTelemetryTracerProvider(builder =>
{
    // Sample 10% of traces
    builder.SetSampler(new TraceIdRatioBasedSampler(0.1));
});
```

### Parent-Based Sampling

```csharp
builder.Services.ConfigureOpenTelemetryTracerProvider(builder =>
{
    // Respect parent sampling decision, otherwise sample 10%
    builder.SetSampler(new ParentBasedSampler(
        new TraceIdRatioBasedSampler(0.1)));
});
```

### Always On/Off Sampling

```csharp
builder.Services.ConfigureOpenTelemetryTracerProvider(builder =>
{
    builder.SetSampler(new AlwaysOnSampler());   // Sample everything
    // or
    builder.SetSampler(new AlwaysOffSampler());  // Sample nothing
});
```

## Migration Checklist

- [ ] Identify all `ITelemetryProcessor` implementations
- [ ] For each processor, analyze its filtering logic
- [ ] Create `BaseProcessor<Activity>` classes:
  - [ ] Use `OnEnd()` for filtering (not OnStart)
  - [ ] Use `activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded` to drop
  - [ ] Check `Activity.Kind` to determine telemetry type
  - [ ] Use `GetTagItem()` to access properties
- [ ] Update registration:
  - [ ] Remove `services.AddApplicationInsightsTelemetryProcessor<T>()`
  - [ ] Add `builder.AddProcessor<T>()` in `ConfigureOpenTelemetryTracerProvider`
- [ ] Consider using built-in samplers for sampling scenarios
- [ ] Test that filtering works as expected
- [ ] Verify dropped telemetry doesn't appear in Azure Monitor

## See Also

- [TelemetryInitializers-removed.md](TelemetryInitializers-removed.md) - Initializer/enrichment migration
- [BaseProcessor-OnEnd.md](../../api-reference/BaseProcessor/OnEnd.md) - OnEnd method details
- [filtering.md](../../common-scenarios/filtering.md) - Filtering patterns
- [sampling.md](../../common-scenarios/sampling.md) - Sampling strategies
- [activity-processor.md](../../concepts/activity-processor.md) - BaseProcessor concept
