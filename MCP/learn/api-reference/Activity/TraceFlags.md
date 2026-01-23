---
title: Activity.ActivityTraceFlags - Sampling Decision
category: api-reference
applies-to: 3.x
namespace: System.Diagnostics
related:
  - concepts/opentelemetry-pipeline.md
  - common-scenarios/sampling-telemetry.md
source: System.Diagnostics.Activity (.NET BCL)
---

# Activity.ActivityTraceFlags

## Signature

```csharp
namespace System.Diagnostics
{
    public class Activity
    {
        public ActivityTraceFlags ActivityTraceFlags { get; }
    }
    
    [Flags]
    public enum ActivityTraceFlags
    {
        None = 0,
        Recorded = 1
    }
}
```

**Note:** While `ActivityTraceFlags` has the `[Flags]` attribute, it only contains two mutually exclusive values. An Activity is either `Recorded` (sampled) or `None` (not sampled) - no bitwise combinations are used.

## Description

Indicates whether the Activity is **sampled** (recorded and exported) or **not sampled** (dropped). This is a read-only property set by the sampler during Activity creation and propagated via the W3C `traceparent` header.

## ActivityTraceFlags Values

### None (0)
Activity is **not sampled** - will not be recorded or exported to Application Insights.

```csharp
if (activity.ActivityTraceFlags == ActivityTraceFlags.None)
{
    // Activity is dropped, won't appear in Application Insights
}
```

### Recorded (1)
Activity is **sampled** - will be recorded and exported to Application Insights.

```csharp
if (activity.ActivityTraceFlags == ActivityTraceFlags.Recorded)
{
    // Activity will appear in Application Insights
}
```

## 2.x Equivalent

In 2.x, sampling was controlled differently:

```csharp
using OpenTelemetry.Trace;

// 2.x: Sampling via TelemetrySink and sampling processors
telemetryConfiguration.TelemetryProcessors.Add(
    new AdaptiveSamplingTelemetryProcessor());

// 3.x: Sampling via OpenTelemetry Sampler
builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        otel.SetSampler(new TraceIdRatioBasedSampler(0.1)); // 10% sampling
    });
```

## How Sampling Works

```
Activity Creation
      ↓
Sampler evaluates
      ↓
Sets ActivityTraceFlags
      ↓
┌─────────────────────┬──────────────────────┐
│  Recorded (1)       │  None (0)            │
│  ✅ Recorded         │  ❌ Dropped          │
│  ✅ Exported to AI   │  ❌ Not exported     │
│  ✅ Propagated       │  ⚠️  Propagated      │
│                     │     but not recorded │
└─────────────────────┴──────────────────────┘
```

## W3C traceparent Header Propagation

TraceFlags is propagated in the W3C `traceparent` header:

```http
traceparent: 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01
                                                                  ^^
                                                            trace-flags
```

**Format:** `{version}-{trace-id}-{parent-id}-{trace-flags}`

- `01` = Recorded (sampled)
- `00` = None (not sampled)

## Checking if Activity is Sampled

```csharp
var activity = Activity.Current;

// Check if sampled
if (activity?.ActivityTraceFlags.HasFlag(ActivityTraceFlags.Recorded) == true)
{
    // Activity is being recorded
    Console.WriteLine("Activity is sampled");
}
else
{
    // Activity is not being recorded
    Console.WriteLine("Activity is not sampled (will be dropped)");
}
```

**Related Property:** `activity.IsAllDataRequested` is another property that indicates whether detailed data should be recorded. Both properties work together:
- `ActivityTraceFlags.Recorded` = Should be exported to backend (Application Insights)
- `IsAllDataRequested` = Should record detailed data during execution

## Use Case: Conditional Expensive Operations

```csharp
public override void OnStart(Activity activity)
{
    // Only do expensive enrichment if activity will be recorded
    if (activity.ActivityTraceFlags.HasFlag(ActivityTraceFlags.Recorded))
    {
        // Expensive operation (e.g., database lookup)
        var userProfile = GetUserProfile(activity.GetTagItem("user.id") as string);
        activity.SetTag("user.tier", userProfile.Tier);
        activity.SetTag("user.region", userProfile.Region);
    }
}
```

## Use Case: Skip Processing for Dropped Activities

```csharp
public class ExpensiveProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Skip processing if not sampled
        if (activity.ActivityTraceFlags == ActivityTraceFlags.None)
        {
            return;
        }
        
        // Expensive processing only for sampled activities
        var complexMetrics = CalculateComplexMetrics(activity);
        activity.SetTag("metrics.calculation", complexMetrics);
    }
}
```

## Important: TraceFlags is Read-Only

```csharp
var activity = Activity.Current;

// ❌ CANNOT change TraceFlags
// activity.ActivityTraceFlags = ActivityTraceFlags.Recorded; // Compile error - readonly!

// ✅ Sampling decision made at Activity creation by Sampler
using (var activity = activitySource.StartActivity("Operation"))
{
    // ActivityTraceFlags already set by sampler
    var isRecorded = activity.ActivityTraceFlags.HasFlag(ActivityTraceFlags.Recorded);
}
```

## Sampler Configuration

TraceFlags is set by the configured sampler:

```csharp
using OpenTelemetry.Trace;

// Always sample (all activities recorded)
otel.SetSampler(new AlwaysOnSampler());
// All activities: ActivityTraceFlags = Recorded

// Never sample (all activities dropped)
otel.SetSampler(new AlwaysOffSampler());
// All activities: ActivityTraceFlags = None

// Ratio-based sampling (e.g., 10%)
otel.SetSampler(new TraceIdRatioBasedSampler(0.1));
// 10% of activities: ActivityTraceFlags = Recorded
// 90% of activities: ActivityTraceFlags = None

// Parent-based sampling (follow parent's decision)
otel.SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(0.1)));
// Respects parent activity's sampling decision
```

## Default Sampler

```csharp
using OpenTelemetry.Trace;

// Application Insights SDK default sampler behavior:
// Uses AlwaysOnSampler (100% sampling) by default
// To change, configure explicitly:

builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        // Use parent-based sampling (respects upstream decisions)
        otel.SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(0.1)));
    });
```

## Propagation Across Services

### Service A (Entry Point)
```csharp
using OpenTelemetry.Trace;

// Service A has 10% sampling
otel.SetSampler(new TraceIdRatioBasedSampler(0.1));

using (var activity = activitySource.StartActivity("ServiceA"))
{
    // ActivityTraceFlags determined by sampler (10% chance of Recorded)
    
    // Calls Service B
    await httpClient.GetAsync("https://service-b/api");
    // Sends traceparent with trace-flags
}
```

### Service B (Downstream)
```csharp
using OpenTelemetry.Trace;

// Service B uses parent-based sampler
otel.SetSampler(new ParentBasedSampler(new AlwaysOnSampler()));

public IActionResult Api()
{
    var activity = Activity.Current;
    // ActivityTraceFlags inherited from Service A!
    // If Service A sampled: Recorded
    // If Service A not sampled: None
    
    return Ok();
}
```

## Real-World Example: Adaptive Sampling

```csharp
using OpenTelemetry.Trace;

public class AdaptiveRateSampler : Sampler
{
    private double currentRate = 1.0; // Start at 100%
    
    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
    {
        // Adjust rate based on telemetry volume
        AdjustSamplingRate();
        
        var shouldSample = Random.Shared.NextDouble() < currentRate;
        
        // Return appropriate SamplingResult
        if (shouldSample)
        {
            return new SamplingResult(SamplingDecision.RecordAndSample);
        }
        else
        {
            return new SamplingResult(SamplingDecision.Drop);
        }
    }
    
    private void AdjustSamplingRate()
    {
        // Lower rate if too much telemetry
        // Increase rate if volume is low
    }
}
```

## Application Insights Behavior

```csharp
// ActivityTraceFlags.Recorded → Exported to Application Insights
// Appears in: requests, dependencies, traces tables

// ActivityTraceFlags.None → NOT exported
// Does NOT appear in Application Insights
```

## Important Notes

1. **Read-Only**: Cannot change TraceFlags after Activity creation
2. **Propagated**: TraceFlags travels with traceparent header to downstream services
3. **Performance**: Use to skip expensive operations for non-sampled activities
4. **Default**: Application Insights uses parent-based sampling (follows parent's decision)
5. **Head-based**: Sampling decision made at root activity, propagated to all children

## Common Patterns

### Pattern 1: Skip Expensive Enrichment
```csharp
public override void OnStart(Activity activity)
{
    if (!activity.ActivityTraceFlags.HasFlag(ActivityTraceFlags.Recorded))
        return; // Skip for non-sampled activities
    
    // Expensive enrichment only for sampled activities
    EnrichWithDatabaseData(activity);
}
```

### Pattern 2: Logging Sampled Activities Only
```csharp
public override void OnEnd(Activity activity)
{
    if (activity.ActivityTraceFlags.HasFlag(ActivityTraceFlags.Recorded))
    {
        logger.LogInformation(
            "Sampled activity completed: {DisplayName} in {Duration}ms",
            activity.DisplayName,
            activity.Duration.TotalMilliseconds);
    }
}
```

### Pattern 3: Metrics for All, Traces for Sampled

**Key Insight:** Non-sampled activities still exist in memory during execution and can be used for lightweight metrics aggregation. Only the detailed trace export to Application Insights is skipped.

```csharp
public override void OnEnd(Activity activity)
{
    // Metrics aggregation for ALL activities (lightweight, no export cost)
    // Non-sampled activities can still contribute to aggregated metrics
    metricCollector.RecordDuration(activity.DisplayName, activity.Duration);
    metricCollector.IncrementCounter(activity.DisplayName);
    
    // Detailed trace export only for sampled activities (expensive)
    // This is what gets sent to Application Insights
    if (activity.ActivityTraceFlags.HasFlag(ActivityTraceFlags.Recorded))
    {
        RecordDetailedTrace(activity);
    }
}
```

## See Also

- [OpenTelemetry Pipeline](../../concepts/opentelemetry-pipeline.md) - How sampling fits in pipeline
- [Sampling Telemetry](../../common-scenarios/sampling-telemetry.md) - Sampling strategies and configuration
- [Activity.SetTag](SetTag.md) - Adding custom dimensions
- [Activity.SetStatus](SetStatus.md) - Setting success/failure status

## References

- **W3C Trace Context:** https://www.w3.org/TR/trace-context/#trace-flags
- **OpenTelemetry Sampling:** https://opentelemetry.io/docs/specs/otel/trace/sdk/#sampling
