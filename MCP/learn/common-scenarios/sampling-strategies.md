# Sampling Strategies

**Category:** Common Scenario  
**Applies to:** Controlling telemetry volume and costs  
**Related:** [activity-processor.md](../concepts/activity-processor.md), [IsAllDataRequested.md](../api-reference/Activity/IsAllDataRequested.md)

## Overview

Application Insights 3.x uses OpenTelemetry's sampling approaches instead of 2.x adaptive sampling. You control sampling through built-in samplers and custom processors.

## Sampling in 3.x

### Key Changes from 2.x

| 2.x | 3.x |
|-----|-----|
| Adaptive sampling (automatic) | Fixed-ratio sampling (manual configuration) |
| Server-side sampling | Client-side sampling only |
| `AdaptiveSamplingTelemetryProcessor` | `TraceIdRatioBasedSampler` |
| Sampling rate adjusts dynamically | Fixed sampling rate |
| Configured in `ApplicationInsights.config` or code | Configured in OpenTelemetry setup |

## Built-in Samplers

### 1. TraceIdRatioBasedSampler (Recommended)

Samples based on trace ID for consistent sampling across distributed traces.

```csharp
builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        // Sample 10% of all traces
        otel.SetSampler(new TraceIdRatioBasedSampler(0.1));
    });
```

**How it works:**
- Uses trace ID to make sampling decision
- All spans in same trace get same decision
- Maintains distributed trace integrity
- Deterministic: same trace ID always gets same decision

### 2. AlwaysOnSampler

Samples 100% of telemetry (default behavior).

```csharp
builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        otel.SetSampler(new AlwaysOnSampler());
    });
```

### 3. AlwaysOffSampler

Samples 0% of telemetry (for testing/disabling).

```csharp
builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        otel.SetSampler(new AlwaysOffSampler());
    });
```

### 4. ParentBasedSampler

Respects parent span's sampling decision.

```csharp
builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        // If parent is sampled, sample child
        // If no parent, use TraceIdRatioBasedSampler at 10%
        otel.SetSampler(new ParentBasedSampler(
            new TraceIdRatioBasedSampler(0.1)));
    });
```

## Custom Sampling with BaseProcessor

### Approach 1: Conditional Sampling

Sample different rates based on criteria.

```csharp
public class ConditionalSamplingProcessor : BaseProcessor<Activity>
{
    private readonly Random _random = new Random();
    
    public override void OnEnd(Activity activity)
    {
        // Always keep errors
        if (activity.Status == ActivityStatusCode.Error)
        {
            return;
        }
        
        // Sample successful requests based on path
        if (activity.Kind == ActivityKind.Server)
        {
            var path = activity.GetTagItem("url.path") as string;
            
            // Keep 100% of admin operations
            if (path?.StartsWith("/admin") == true)
            {
                return;
            }
            
            // Keep 50% of API calls
            if (path?.StartsWith("/api") == true)
            {
                if (_random.NextDouble() > 0.5)
                {
                    activity.IsAllDataRequested = false;
                }
                return;
            }
            
            // Keep 10% of other requests
            if (_random.NextDouble() > 0.1)
            {
                activity.IsAllDataRequested = false;
            }
        }
    }
}
```

### Approach 2: Smart Sampling (Keep Interesting Telemetry)

Keep errors, slow operations, and sample the rest.

```csharp
public class SmartSamplingProcessor : BaseProcessor<Activity>
{
    private readonly double _normalSamplingRate;
    private readonly TimeSpan _slowThreshold;
    private readonly Random _random = new Random();
    
    public SmartSamplingProcessor(
        double normalSamplingRate = 0.1,
        TimeSpan? slowThreshold = null)
    {
        _normalSamplingRate = normalSamplingRate;
        _slowThreshold = slowThreshold ?? TimeSpan.FromSeconds(3);
    }
    
    public override void OnEnd(Activity activity)
    {
        // Always keep errors
        if (activity.Status == ActivityStatusCode.Error)
        {
            return;
        }
        
        // Always keep slow operations
        if (activity.Duration >= _slowThreshold)
        {
            return;
        }
        
        // Sample normal operations
        if (_random.NextDouble() > _normalSamplingRate)
        {
            activity.IsAllDataRequested = false;
        }
    }
}
```

Register:

```csharp
builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        // Sample 10% at trace level
        otel.SetSampler(new TraceIdRatioBasedSampler(0.1));
        
        // Then apply smart filtering on top
        otel.AddProcessor(new SmartSamplingProcessor(
            normalSamplingRate: 0.1,
            slowThreshold: TimeSpan.FromSeconds(3)));
    });
```

### Approach 3: User-Based Sampling

Sample more for specific users (e.g., beta testers).

```csharp
public class UserBasedSamplingProcessor : BaseProcessor<Activity>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Random _random = new Random();
    
    public UserBasedSamplingProcessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public override void OnEnd(Activity activity)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return;
        
        var userId = httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        // 100% sampling for beta testers
        if (IsBetaTester(userId))
        {
            return;
        }
        
        // 50% sampling for premium users
        if (IsPremiumUser(userId))
        {
            if (_random.NextDouble() > 0.5)
            {
                activity.IsAllDataRequested = false;
            }
            return;
        }
        
        // 10% sampling for regular users
        if (_random.NextDouble() > 0.1)
        {
            activity.IsAllDataRequested = false;
        }
    }
    
    private bool IsBetaTester(string userId) => 
        userId != null && userId.StartsWith("beta_");
    
    private bool IsPremiumUser(string userId) => 
        userId != null && userId.StartsWith("premium_");
}
```

### Approach 4: Time-Based Sampling

Higher sampling during business hours.

```csharp
public class TimeBasedSamplingProcessor : BaseProcessor<Activity>
{
    private readonly Random _random = new Random();
    
    public override void OnEnd(Activity activity)
    {
        var hour = DateTime.UtcNow.Hour;
        
        // Business hours (9 AM - 5 PM UTC): 50% sampling
        if (hour >= 9 && hour < 17)
        {
            if (_random.NextDouble() > 0.5)
            {
                activity.IsAllDataRequested = false;
            }
            return;
        }
        
        // Off hours: 10% sampling
        if (_random.NextDouble() > 0.1)
        {
            activity.IsAllDataRequested = false;
        }
    }
}
```

## Configuration-Based Sampling

### appsettings.json

```json
{
  "Sampling": {
    "BaseSamplingRate": 0.1,
    "ErrorSamplingRate": 1.0,
    "SlowThresholdSeconds": 3,
    "Paths": {
      "/api/health": 0.01,
      "/api/orders": 0.5,
      "/admin": 1.0
    }
  }
}
```

### Processor

```csharp
public class ConfigurableSamplingProcessor : BaseProcessor<Activity>
{
    private readonly double _baseSamplingRate;
    private readonly double _errorSamplingRate;
    private readonly TimeSpan _slowThreshold;
    private readonly Dictionary<string, double> _pathRates;
    private readonly Random _random = new Random();
    
    public ConfigurableSamplingProcessor(IConfiguration configuration)
    {
        var config = configuration.GetSection("Sampling");
        
        _baseSamplingRate = config.GetValue<double>("BaseSamplingRate", 0.1);
        _errorSamplingRate = config.GetValue<double>("ErrorSamplingRate", 1.0);
        _slowThreshold = TimeSpan.FromSeconds(
            config.GetValue<double>("SlowThresholdSeconds", 3));
        
        _pathRates = config.GetSection("Paths")
            .Get<Dictionary<string, double>>() ?? new();
    }
    
    public override void OnEnd(Activity activity)
    {
        double samplingRate = _baseSamplingRate;
        
        // Errors
        if (activity.Status == ActivityStatusCode.Error)
        {
            samplingRate = _errorSamplingRate;
        }
        // Slow operations
        else if (activity.Duration >= _slowThreshold)
        {
            samplingRate = 1.0;
        }
        // Path-specific rates
        else if (activity.Kind == ActivityKind.Server)
        {
            var path = activity.GetTagItem("url.path") as string;
            if (path != null && _pathRates.TryGetValue(path, out var pathRate))
            {
                samplingRate = pathRate;
            }
        }
        
        // Apply sampling
        if (_random.NextDouble() > samplingRate)
        {
            activity.IsAllDataRequested = false;
        }
    }
}
```

## Migration from 2.x Adaptive Sampling

### Before (2.x)

```csharp
// Program.cs or Startup.cs
services.AddApplicationInsightsTelemetry();

services.Configure<TelemetryConfiguration>(config =>
{
    var builder = config.DefaultTelemetrySink.TelemetryProcessorChainBuilder;
    
    builder.UseAdaptiveSampling(
        maxTelemetryItemsPerSecond: 5,
        excludedTypes: "Event");
    
    builder.Build();
});
```

### After (3.x)

```csharp
builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        // Fixed 10% sampling (roughly equivalent to 5 items/sec at moderate load)
        otel.SetSampler(new TraceIdRatioBasedSampler(0.1));
        
        // Add smart sampling to keep errors
        otel.AddProcessor(new SmartSamplingProcessor(normalSamplingRate: 0.1));
    });
```

## Sampling Decision Flow

```
1. Sampler (at Activity creation time)
   ↓
2. Is sampled? (Activity.Recorded)
   ↓
3. If recorded, run through processors
   ↓
4. Processor can drop via IsAllDataRequested = false
   ↓
5. Export to Application Insights
```

## Best Practices

### 1. Use TraceIdRatioBasedSampler for Consistency

```csharp
// Good: Consistent sampling across distributed trace
otel.SetSampler(new TraceIdRatioBasedSampler(0.1));
```

### 2. Combine Sampler with Processor Filtering

```csharp
// Sampler: Reduce overall volume (10%)
otel.SetSampler(new TraceIdRatioBasedSampler(0.1));

// Processor: Keep all errors even if sampled out
otel.AddProcessor(new KeepErrorsProcessor());
```

### 3. Always Keep Errors

```csharp
public override void OnEnd(Activity activity)
{
    if (activity.Status == ActivityStatusCode.Error)
    {
        return; // Never drop errors
    }
    
    // Apply sampling logic
}
```

### 4. Monitor Sampling Impact

Add sampling metadata:

```csharp
public override void OnEnd(Activity activity)
{
    if (_random.NextDouble() > _samplingRate)
    {
        activity.SetTag("sampling.dropped", "true");
        activity.IsAllDataRequested = false;
    }
}
```

### 5. Consider Cost vs. Observability

```csharp
// High traffic, low value: 1% sampling
if (path == "/api/health")
{
    samplingRate = 0.01;
}

// Critical business operations: 100% sampling
if (path?.StartsWith("/api/orders") == true)
{
    samplingRate = 1.0;
}
```

## Testing Sampling

```csharp
[Fact]
public void SmartSampling_KeepsErrors()
{
    var processor = new SmartSamplingProcessor(normalSamplingRate: 0.0);
    var activity = new Activity("Test").Start();
    
    activity.SetStatus(ActivityStatusCode.Error, "Test error");
    activity.Stop();
    
    processor.OnEnd(activity);
    
    // Error should not be dropped
    Assert.True(activity.IsAllDataRequested);
}

[Fact]
public void SmartSampling_KeepsSlowOperations()
{
    var processor = new SmartSamplingProcessor(
        normalSamplingRate: 0.0,
        slowThreshold: TimeSpan.FromMilliseconds(100));
    
    var activity = new Activity("Test").Start();
    Thread.Sleep(150);
    activity.Stop();
    
    processor.OnEnd(activity);
    
    // Slow operation should not be dropped
    Assert.True(activity.IsAllDataRequested);
}
```

## Performance Impact

**Sampler:** Evaluated at Activity creation (minimal overhead)  
**Processor:** Evaluated at Activity end (after work is done)

```csharp
// Low overhead: Sampler drops early
otel.SetSampler(new TraceIdRatioBasedSampler(0.1));

// Higher overhead: Processor runs after Activity completes
otel.AddProcessor(new CustomSamplingProcessor());
```

## Cost Estimation

**Formula:** `Daily Cost = (Requests per day) × (Sampling rate) × (Cost per million)`

Example:
- 10 million requests/day
- 10% sampling rate
- $2.88 per million data points

Daily cost: 10M × 0.1 × ($2.88/1M) = **$2.88/day** ≈ **$86/month**

Adjust sampling rate to meet budget:
- 5% sampling = $43/month
- 1% sampling = $8.60/month

## See Also

- [IsAllDataRequested.md](../api-reference/Activity/IsAllDataRequested.md)
- [activity-processor.md](../concepts/activity-processor.md)
- [filtering-telemetry.md](filtering-telemetry.md)
- [OpenTelemetry Sampling Spec](https://opentelemetry.io/docs/specs/otel/trace/sdk/#sampling)
