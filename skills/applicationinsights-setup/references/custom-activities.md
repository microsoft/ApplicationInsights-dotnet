# Custom Activities (Distributed Tracing)

## Overview

In 3.x, `ActivitySource` and `Activity` are the primary APIs for custom distributed tracing. This is the OpenTelemetry-native approach and is **preferred over `TelemetryClient.StartOperation`**.

## Setup

### Define an ActivitySource

```csharp
using System.Diagnostics;

public static class MyAppTracing
{
    public static readonly ActivitySource Source = new("MyApp.Orders", "1.0.0");
}
```

### Register with OpenTelemetry

The SDK must know about your source to export its activities:

```csharp
// DI (ASP.NET Core / Worker Service)
builder.Services.ConfigureOpenTelemetryTracerProvider(tracing =>
    tracing.AddSource("MyApp.Orders"));

// Non-DI (Console / Classic ASP.NET)
config.ConfigureOpenTelemetryBuilder(otel =>
    otel.WithTracing(t => t.AddSource("MyApp.Orders")));
```

## Creating Activities

### Basic Activity

```csharp
using var activity = MyAppTracing.Source.StartActivity("ProcessOrder");
// ... do work ...
```

### With Kind

Use `ActivityKind` to control how Application Insights classifies the telemetry:

```csharp
// Shows as a dependency in Application Insights
using var activity = MyAppTracing.Source.StartActivity("CallPaymentService", ActivityKind.Client);

// Shows as a request in Application Insights
using var activity = MyAppTracing.Source.StartActivity("HandleMessage", ActivityKind.Consumer);

// Shows as an internal operation
using var activity = MyAppTracing.Source.StartActivity("ValidateOrder", ActivityKind.Internal);
```

| `ActivityKind` | Application Insights type |
|---|---|
| `Server` | Request |
| `Client` | Dependency |
| `Consumer` | Request (message-driven) |
| `Producer` | Dependency |
| `Internal` | Dependency (default if not specified) |

### Adding Tags (Custom Properties)

```csharp
using var activity = MyAppTracing.Source.StartActivity("ProcessOrder");
activity?.SetTag("order.id", orderId);
activity?.SetTag("order.total", total);
activity?.SetTag("customer.tier", "premium");
```

Tags become custom properties/dimensions in Application Insights.

### Setting Status

```csharp
using var activity = MyAppTracing.Source.StartActivity("ProcessOrder");
try
{
    // ... do work ...
    activity?.SetStatus(ActivityStatusCode.Ok);
}
catch (Exception ex)
{
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    activity?.RecordException(ex); // Adds exception as a span event
    throw;
}
```

### Nested Activities

Activities automatically form a parent-child hierarchy. Inner activities are linked to the outer one via `TraceId` and `ParentId`:

```csharp
public async Task ProcessOrderAsync(string orderId)
{
    using var orderActivity = MyAppTracing.Source.StartActivity("ProcessOrder", ActivityKind.Internal);
    orderActivity?.SetTag("order.id", orderId);

    // This activity becomes a child of ProcessOrder
    await ValidateOrderAsync(orderId);

    // This activity also becomes a child of ProcessOrder
    await ChargePaymentAsync(orderId);

    orderActivity?.SetStatus(ActivityStatusCode.Ok);
}

private async Task ValidateOrderAsync(string orderId)
{
    using var activity = MyAppTracing.Source.StartActivity("ValidateOrder", ActivityKind.Internal);
    // ... validation logic ...
    activity?.SetStatus(ActivityStatusCode.Ok);
}

private async Task ChargePaymentAsync(string orderId)
{
    using var activity = MyAppTracing.Source.StartActivity("ChargePayment", ActivityKind.Client);
    activity?.SetTag("payment.provider", "stripe");
    // ... payment call (any outgoing HTTP call here also becomes a child) ...
    activity?.SetStatus(ActivityStatusCode.Ok);
}
```

This produces a trace tree in Application Insights:
```
ProcessOrder (Internal → Dependency)
├── ValidateOrder (Internal → Dependency)
└── ChargePayment (Client → Dependency)
    └── HTTP POST https://api.stripe.com/... (auto-collected)
```

### Linking to Other Traces

```csharp
var linkedContext = new ActivityContext(traceId, spanId, ActivityTraceFlags.Recorded);
using var activity = MyAppTracing.Source.StartActivity(
    "ProcessBatchItem",
    ActivityKind.Internal,
    parentContext: default,
    links: [new ActivityLink(linkedContext)]);
```

## Best Practices

1. **Use `ActivitySource` over `TelemetryClient.StartOperation`** — `ActivitySource` is the OpenTelemetry standard and works with all exporters, not just Application Insights
2. **Use null-conditional (`activity?.`)** — `StartActivity` returns `null` if no listener is registered or sampling drops it
3. **One `ActivitySource` per logical component** — e.g., `MyApp.Orders`, `MyApp.Payments`, `MyApp.Inventory`
4. **Keep activity names stable** — they become operation names; avoid including dynamic IDs in the name
5. **Set `ActivityKind` appropriately** — it controls how Application Insights classifies the telemetry (request vs dependency)
