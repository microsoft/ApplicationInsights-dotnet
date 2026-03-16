# Live Metrics Migration (2.x → 3.x)

## What Changed

- `EnableQuickPulseMetricStream` is **unchanged** (default `true`) — no action needed
- Live Metrics continues to work in 3.x
- The underlying implementation uses OpenTelemetry but the behavior is the same

## Typical Scenario

If your 2.x code sets `EnableQuickPulseMetricStream`:

```csharp
// This is safe — unchanged in 3.x:
services.AddApplicationInsightsTelemetry(options =>
{
    options.EnableQuickPulseMetricStream = true; // default, can be left out
});
```

No migration action required. Live Metrics works identically.

## Disabling Live Metrics

```csharp
services.AddApplicationInsightsTelemetry(options =>
{
    options.EnableQuickPulseMetricStream = false;
});
```

Or with the Distro:

```csharp
builder.Services.AddOpenTelemetry().UseAzureMonitor(options =>
{
    options.EnableLiveMetrics = false;
});
```
