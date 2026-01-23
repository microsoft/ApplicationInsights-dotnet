# Sampling Configuration Changes

**Category:** Transformation Guide  
**Applies to:** Sampling configuration in 3.x  
**Related:** [sampling-strategies.md](../../common-scenarios/sampling-strategies.md)

## Overview

Sampling configuration has changed significantly from 2.x to 3.x. Adaptive sampling is not available; instead, use fixed-ratio sampling with `TraceIdRatioBasedSampler`.

## Key Changes

| 2.x | 3.x | Notes |
|-----|-----|-------|
| AdaptiveSamplingTelemetryProcessor | TraceIdRatioBasedSampler | Fixed ratio only |
| SamplingTelemetryProcessor | TraceIdRatioBasedSampler | Same |
| Dynamic sampling rate | Fixed sampling rate | No adaptive behavior |
| Per-telemetry-type sampling | All-or-nothing sampling | Entire trace sampled or not |

## Adaptive Sampling (2.x) → Fixed Sampling (3.x)

### Before (2.x)

```csharp
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;

public void ConfigureServices(IServiceCollection services)
{
    services.AddApplicationInsightsTelemetry();
    
    services.Configure<TelemetryConfiguration>(config =>
    {
        var builder = config.TelemetryProcessorChainBuilder;
        
        builder.Use(next => new AdaptiveSamplingTelemetryProcessor(next)
        {
            MaxTelemetryItemsPerSecond = 5,
            InitialSamplingPercentage = 100,
            MinSamplingPercentage = 0.1,
            MaxSamplingPercentage = 100,
            EvaluationInterval = TimeSpan.FromSeconds(15),
            SamplingPercentageDecreaseTimeout = TimeSpan.FromMinutes(2),
            SamplingPercentageIncreaseTimeout = TimeSpan.FromMinutes(15),
            MovingAverageRatio = 0.25
        });
        
        builder.Build();
    });
}
```

### After (3.x)

```csharp
using OpenTelemetry.Trace;

public void ConfigureServices(IServiceCollection services)
{
    services.AddApplicationInsightsTelemetry()
        .ConfigureOpenTelemetryBuilder(otel =>
        {
            // Fixed 10% sampling
            otel.SetSampler(new TraceIdRatioBasedSampler(0.1));
        });
}
```

## Configuration-Based Sampling

### Before (2.x)

```json
{
  "ApplicationInsights": {
    "EnableAdaptiveSampling": true,
    "SamplingSettings": {
      "MaxTelemetryItemsPerSecond": 5
    }
  }
}
```

### After (3.x)

```json
{
  "ApplicationInsights": {
    "ConnectionString": "..."
  },
  "OpenTelemetry": {
    "Sampling": {
      "Ratio": 0.1
    }
  }
}
```

```csharp
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        var samplingRatio = Configuration.GetValue<double>("OpenTelemetry:Sampling:Ratio", 1.0);
        otel.SetSampler(new TraceIdRatioBasedSampler(samplingRatio));
    });
```

## Per-Type Sampling Removed

### Before (2.x)

```csharp
// Different sampling rates for different telemetry types
builder.Use(next => new SamplingTelemetryProcessor(next)
{
    SamplingPercentage = 50,
    ExcludedTypes = "Request;Exception", // Don't sample requests/exceptions
    IncludedTypes = "Dependency;Event" // Only sample these
});
```

### After (3.x)

Per-type sampling is not supported. Use fixed-ratio sampling for entire traces:

```csharp
// All telemetry in a trace sampled together
otel.SetSampler(new TraceIdRatioBasedSampler(0.5)); // 50% of traces
```

To approximate per-type behavior, use processors:

```csharp
public class CriticalOperationSampler : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Always include errors and specific operations
        if (activity.Status == ActivityStatusCode.Error ||
            activity.DisplayName == "CriticalOperation")
        {
            // Already sampled - nothing to do
            return;
        }
        
        // For other operations, respect sampling decision
        // (Can't override TraceIdRatioBasedSampler decision)
    }
}
```

## Environment-Based Sampling

### Pattern: Different Rates per Environment

```csharp
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        
        double samplingRatio = env switch
        {
            "Development" => 1.0,    // 100% in dev
            "Staging" => 0.5,        // 50% in staging
            "Production" => 0.1,     // 10% in production
            _ => 0.1
        };
        
        otel.SetSampler(new TraceIdRatioBasedSampler(samplingRatio));
    });
```

## Custom Sampling Logic

### Pattern: Always Sample Errors

```csharp
using OpenTelemetry.Trace;

public class AlwaysSampleErrorsSampler : Sampler
{
    private readonly TraceIdRatioBasedSampler _baseSampler;
    
    public AlwaysSampleErrorsSampler(double ratio)
    {
        _baseSampler = new TraceIdRatioBasedSampler(ratio);
    }
    
    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
    {
        // Check if this is an error (simplified - actual implementation more complex)
        foreach (var tag in samplingParameters.Tags)
        {
            if (tag.Key == "error" && tag.Value?.ToString() == "true")
            {
                return new SamplingResult(SamplingDecision.RecordAndSample);
            }
        }
        
        // Fallback to ratio-based sampling
        return _baseSampler.ShouldSample(samplingParameters);
    }
}
```

Usage:

```csharp
otel.SetSampler(new AlwaysSampleErrorsSampler(0.1));
```

## Migration Strategy

### Option 1: Direct Conversion

Convert adaptive sampling to fixed ratio:

```csharp
// 2.x: MaxTelemetryItemsPerSecond = 5
// Estimate: 5 items/sec × 60 sec = 300 items/min
// If you generate 3000 items/min, you need 10% sampling

otel.SetSampler(new TraceIdRatioBasedSampler(0.1));
```

### Option 2: Monitor and Adjust

```csharp
// Start with higher ratio
otel.SetSampler(new TraceIdRatioBasedSampler(0.5)); // 50%

// Monitor ingestion volume in Azure Monitor
// Adjust ratio based on actual vs. desired volume
```

### Option 3: Environment-Specific

```csharp
// Production: Low sampling to control costs
// Non-production: High sampling for debugging

double ratio = env == "Production" ? 0.1 : 1.0;
otel.SetSampler(new TraceIdRatioBasedSampler(ratio));
```

## Sampling Behavior Differences

### 2.x Adaptive Sampling
- Dynamically adjusts sampling rate
- Per-telemetry-type sampling possible
- Can sample part of a distributed trace
- Adapts to volume changes automatically

### 3.x Fixed-Ratio Sampling
- Fixed sampling rate (no dynamic adjustment)
- Entire trace sampled or dropped
- Consistent sampling across distributed systems
- TraceId-based (deterministic)
- Simpler, more predictable

## Testing Sampling

```csharp
[Fact]
public void Sampler_DropsSomeActivities()
{
    var sampler = new TraceIdRatioBasedSampler(0.5); // 50%
    
    int sampled = 0;
    int total = 1000;
    
    for (int i = 0; i < total; i++)
    {
        var activity = new Activity("Test").Start();
        
        var result = sampler.ShouldSample(new SamplingParameters(
            activity.Context,
            activity.TraceId,
            activity.DisplayName,
            ActivityKind.Internal,
            null,
            null));
        
        if (result.Decision == SamplingDecision.RecordAndSample)
        {
            sampled++;
        }
        
        activity.Stop();
    }
    
    // Should be approximately 50%
    Assert.InRange(sampled, 450, 550);
}
```

## Best Practices

1. **Use Fixed Ratios**: Adaptive sampling not available; plan for fixed ratio
2. **Environment-Based**: Higher sampling in dev/test, lower in production
3. **Monitor Costs**: Watch ingestion volume and adjust ratio as needed
4. **Whole Traces**: Remember sampling applies to entire trace (all spans)
5. **Critical Operations**: Consider always-sample logic for errors and critical paths

## See Also

- [sampling-strategies.md](../../common-scenarios/sampling-strategies.md)
- [TraceIdRatioBasedSampler.md](../../api-reference/Samplers/TraceIdRatioBasedSampler.md)
