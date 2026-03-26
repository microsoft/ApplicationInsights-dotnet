# TelemetryClient Migration (2.x → 3.x)

## Overview

`TelemetryClient` still works in 3.x but has several breaking changes to method signatures.

## Removed Overloads

### TrackEvent — 3-param overload removed

```csharp
// 2.x (removed):
client.TrackEvent("OrderPlaced", properties, metrics);
//                                           ^^^^^^^ IDictionary<string, double> removed

// 3.x:
client.TrackEvent("OrderPlaced", properties);
client.TrackMetric("OrderValue", orderValue); // Track metrics separately
```

### TrackException — 3-param overload removed

```csharp
// 2.x (removed):
client.TrackException(ex, properties, metrics);

// 3.x:
client.TrackException(ex, properties);
client.TrackMetric("ErrorProcessingTime", elapsed); // Track metrics separately
```

### TrackAvailability — 8-param overload removed

```csharp
// 2.x (removed):
client.TrackAvailability(name, timestamp, duration, location, success, message, properties, metrics);

// 3.x (7-param — track metrics separately):
client.TrackAvailability(name, timestamp, duration, location, success, message, properties);
client.TrackMetric("AvailabilityResponseTime", responseTime); // Track metrics separately
```

### TrackPageView — removed entirely

```csharp
// 2.x (removed — no equivalent in 3.x):
client.TrackPageView("HomePage");
client.TrackPageView(pageViewTelemetry);

// 3.x: Remove these API calls. There is no equivalent.
```

### TrackDependency — obsolete 5-param overload removed

```csharp
// 2.x (removed — was already [Obsolete]):
client.TrackDependency(dependencyName, data, startTime, duration, success);

// 3.x — use the full overload instead:
client.TrackDependency(dependencyTypeName, dependencyName, data, startTime, duration, success);
```

### GetMetric — simplified

```csharp
// 2.x (removed overloads):
client.GetMetric("Latency", metricConfiguration, metricAggregationScope);

// 3.x:
client.GetMetric("Latency"); // Simplified, no configuration/scope params
```

> **Note:** Metric names and namespace parameters must not contain spaces or special characters. Use alphanumeric characters, `.`, `-`, or `_` only.

### Constructor — parameterless removed

```csharp
// 2.x (removed):
var client = new TelemetryClient();

// 3.x:
var client = new TelemetryClient(telemetryConfiguration);
// Or inject via DI (preferred)
```

### InstrumentationKey property — removed

```csharp
// 2.x (removed):
client.InstrumentationKey = "xxx";

// 3.x:
// Use TelemetryConfiguration.ConnectionString instead
```

## Unchanged Methods

These work identically in 2.x and 3.x:
- `TrackTrace(string message, ...)`
- `TrackMetric(string name, double value, ...)` — **Note:** metric names must not contain spaces or special characters; use alphanumeric, `.`, `-`, or `_` only.
- `TrackRequest(string name, ...)`
- `TrackDependency(string type, string target, ...)` (full overload)
- `Flush()`
- `StartOperation<T>(string operationName)`
