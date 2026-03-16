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
});

// OR fixed-rate 50% sampling:
services.AddApplicationInsightsTelemetry(options =>
{
    options.SamplingRatio = 0.5f;
});

// OR rate-limited (default behavior, 5 traces/sec):
services.AddApplicationInsightsTelemetry(options =>
{
    options.TracesPerSecond = 5.0;
});
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
