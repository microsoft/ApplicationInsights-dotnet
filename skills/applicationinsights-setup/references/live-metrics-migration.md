# Live Metrics Migration (2.x → 3.x)

## What Changed

- `EnableQuickPulseMetricStream` is **unchanged** (default `true`) — no action needed
- Live Metrics continues to work in 3.x
- The underlying implementation uses OpenTelemetry but the behavior is the same
- The 2.x built-in `QuickPulseTelemetryProcessor` and `QuickPulseTelemetryModule` are no longer needed — 3.x handles Live Metrics internally. Remove any manual registrations of these types.

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
