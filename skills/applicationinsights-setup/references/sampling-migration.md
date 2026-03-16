# Sampling Migration (2.x → 3.x)

## What Changed

- `EnableAdaptiveSampling` property is **removed**
- 3.x uses `TracesPerSecond` (rate-limited, default `5`) or `SamplingRatio` (fixed-rate)

## Before (2.x)

```csharp
services.AddApplicationInsightsTelemetry(options =>
{
    options.EnableAdaptiveSampling = false; // Disable sampling
});
```

## After (3.x)

```csharp
// Collect everything (no sampling):
services.AddApplicationInsightsTelemetry(options =>
{
    options.SamplingRatio = 1.0f;
    options.TracesPerSecond = null; // Must clear when using SamplingRatio
});

// OR fixed-rate 50% sampling:
services.AddApplicationInsightsTelemetry(options =>
{
    options.SamplingRatio = 0.5f;
    options.TracesPerSecond = null; // Must clear when using SamplingRatio
});

// OR rate-limited (default behavior, 5 traces/sec):
services.AddApplicationInsightsTelemetry(options =>
{
    options.TracesPerSecond = 5.0;
});
```

**Important:** When using `SamplingRatio`, set `TracesPerSecond = null` — otherwise rate-limited sampling takes precedence.

### Via Environment Variables

```bash
# Rate-limited:
OTEL_TRACES_SAMPLER=microsoft.rate_limited
OTEL_TRACES_SAMPLER_ARG=10

# Fixed percentage:
OTEL_TRACES_SAMPLER=microsoft.fixed_percentage
OTEL_TRACES_SAMPLER_ARG=0.5
```

## Comparison

| 2.x | 3.x |
|---|---|
| `EnableAdaptiveSampling = true` (default) | `TracesPerSecond = 5` (default) — no config needed |
| `EnableAdaptiveSampling = false` | `SamplingRatio = 1.0f` |
| Custom `ITelemetryProcessorFactory` for sampling | `SamplingRatio` or custom `Sampler` via OpenTelemetry |

## Advanced: Custom OpenTelemetry Sampler

For complex sampling needs (e.g., always sample errors, sample by route):

```csharp
builder.Services.ConfigureOpenTelemetryTracerProvider(tracing =>
    tracing.SetSampler(new ParentBasedSampler(
        new TraceIdRatioBasedSampler(0.1)))); // 10%
```
