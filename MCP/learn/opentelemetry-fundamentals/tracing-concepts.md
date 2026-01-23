# OpenTelemetry Tracing Concepts

**Category:** OpenTelemetry Fundamentals  
**Applies to:** Understanding distributed tracing in OpenTelemetry  
**Related:** [activity-vs-telemetry.md](../concepts/activity-vs-telemetry.md), [activity-source.md](activity-source.md)

## Overview

OpenTelemetry uses Activities (Spans in OTel terminology) as the fundamental unit of tracing. Understanding these concepts is essential for migrating from Application Insights 2.x.

## Core Concepts

### Trace

A trace represents a complete request journey through your distributed system.

```
Trace (OrderId: 12345)
├─ Span: POST /api/orders        [Service A]
   ├─ Span: ValidateOrder        [Service A]
   ├─ Span: HTTP POST inventory  [Service A → Service B]
   │  └─ Span: POST /api/check   [Service B]
   │     └─ Span: SQL SELECT     [Service B]
   └─ Span: PublishEvent         [Service A]
      └─ Span: Queue Send        [Service A]
```

**Properties:**
- `TraceId`: Unique identifier for the entire trace (maps to `operation_Id` in Azure Monitor)
- Spans within a trace share the same `TraceId`
- Duration: Time from first span start to last span end

### Span (Activity in .NET)

A span represents a single operation within a trace.

```csharp
using var activity = ActivitySource.StartActivity("ProcessOrder");
// Activity represents a span
// In .NET: Activity
// In OpenTelemetry: Span
```

**Properties:**
- `SpanId`: Unique identifier for this span (maps to `id` in Azure Monitor)
- `ParentSpanId`: Links to parent span
- `TraceId`: Links to parent trace
- `StartTime`: When operation started
- `Duration`: How long operation took
- `Status`: Ok, Error, or Unset
- `Tags`: Key-value attributes
- `Events`: Time-stamped log messages
- `Links`: Relationships to other spans

### Context Propagation

How trace context flows across service boundaries.

**W3C Trace Context Header:**
```
traceparent: 00-{trace-id}-{parent-id}-{flags}
Example: 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01
         │  │                                │                  └─ Flags (sampled)
         │  │                                └─ Parent Span ID
         │  └─ Trace ID
         └─ Version
```

**Automatic Propagation:**

```csharp
// Service A: Outgoing HTTP request automatically includes traceparent header
using var httpClient = new HttpClient();
var response = await httpClient.GetAsync("https://service-b/api/data");

// Service B: ASP.NET Core automatically extracts traceparent and links spans
```

## Span Lifecycle

### 1. Creation

```csharp
// Create span
using var activity = ActivitySource.StartActivity(
    name: "ProcessOrder",
    kind: ActivityKind.Internal);

// Activity is automatically:
// - Assigned a SpanId
// - Linked to parent (if exists)
// - Associated with current TraceId
// - Started (timestamp recorded)
```

### 2. Active Span

```csharp
// Activity is current
var current = Activity.Current; // Returns our activity

// Child activities automatically link
using var child = ActivitySource.StartActivity("ChildOperation");
// child.ParentSpanId == activity.SpanId
```

### 3. Enrichment

```csharp
// Add tags (attributes)
activity?.SetTag("order.id", orderId);
activity?.SetTag("customer.id", customerId);

// Add events
activity?.AddEvent(new ActivityEvent("OrderValidated"));

// Record exception
try
{
    await ProcessOrderAsync();
}
catch (Exception ex)
{
    activity?.RecordException(ex);
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    throw;
}
```

### 4. Completion

```csharp
// Activity completes when disposed
} // activity.Stop() called automatically
  // Duration calculated
  // OnEnd processors invoked
  // Exported to Application Insights
```

## Activity Kinds

Different span types for different operation patterns.

### Server

Handles incoming requests.

```csharp
// ASP.NET Core automatically creates Server activities
// You can create custom ones:
using var activity = ActivitySource.StartActivity(
    "HandleWebhook",
    ActivityKind.Server);
```

**Azure Monitor Mapping:** `request` table

### Client

Makes outgoing requests.

```csharp
// HttpClient automatically creates Client activities
// You can create custom ones:
using var activity = ActivitySource.StartActivity(
    "CallExternalAPI",
    ActivityKind.Client);
```

**Azure Monitor Mapping:** `dependencies` table

### Internal

Internal operations that don't cross boundaries.

```csharp
using var activity = ActivitySource.StartActivity(
    "CalculateTotal",
    ActivityKind.Internal);
```

**Azure Monitor Mapping:** `dependencies` table (type: "InProc")

### Producer

Sends messages to a queue/topic.

```csharp
using var activity = ActivitySource.StartActivity(
    "SendOrderMessage",
    ActivityKind.Producer);
```

**Azure Monitor Mapping:** `dependencies` table

### Consumer

Receives messages from a queue/topic.

```csharp
using var activity = ActivitySource.StartActivity(
    "ProcessOrderMessage",
    ActivityKind.Consumer);
```

**Azure Monitor Mapping:** `request` table

## Tags (Attributes)

Key-value pairs describing the span.

### Standard Tags (Semantic Conventions)

```csharp
// HTTP
activity.SetTag("http.request.method", "POST");
activity.SetTag("url.path", "/api/orders");
activity.SetTag("http.response.status_code", 200);

// Database
activity.SetTag("db.system", "postgresql");
activity.SetTag("db.name", "orders");
activity.SetTag("db.statement", "SELECT * FROM orders WHERE id = @p0");

// Messaging
activity.SetTag("messaging.system", "rabbitmq");
activity.SetTag("messaging.destination.name", "orders");
```

See: [OpenTelemetry Semantic Conventions](https://opentelemetry.io/docs/specs/semconv/)

### Custom Tags

```csharp
// Business-specific
activity.SetTag("order.id", orderId);
activity.SetTag("tenant.id", tenantId);
activity.SetTag("feature.flag", "enabled");
```

## Events

Time-stamped messages within a span.

```csharp
// Simple event
activity?.AddEvent(new ActivityEvent("OrderValidated"));

// Event with attributes
activity?.AddEvent(new ActivityEvent(
    "PaymentProcessed",
    tags: new ActivityTagsCollection
    {
        ["payment.amount"] = 99.99,
        ["payment.method"] = "CreditCard"
    }));

// Exception as event
try
{
    await ProcessOrderAsync();
}
catch (Exception ex)
{
    activity?.RecordException(ex); // Adds exception event
    throw;
}
```

**Azure Monitor Mapping:** Events are included in span data (not separate table)

## Status

Indicates span outcome.

```csharp
// Success (default)
activity?.SetStatus(ActivityStatusCode.Ok);

// Error
activity?.SetStatus(ActivityStatusCode.Error, "Validation failed");

// Unset (when outcome unknown)
activity?.SetStatus(ActivityStatusCode.Unset);
```

**Azure Monitor Mapping:**
- `Ok` → `success = true`
- `Error` → `success = false`
- `Unset` → `success = true` (default)

## Links

Connect spans across traces.

```csharp
// Create activity with links to other traces
var links = new[]
{
    new ActivityLink(otherActivityContext1),
    new ActivityLink(otherActivityContext2)
};

using var activity = ActivitySource.StartActivity(
    name: "BatchProcess",
    kind: ActivityKind.Internal,
    links: links);
```

**Use Cases:**
- Batch processing (link to all source traces)
- Fan-in operations (multiple inputs → one output)
- Following up on async operations

## Baggage

Cross-cutting context propagated with trace.

```csharp
// Set baggage
Baggage.SetBaggage("tenant.id", tenantId);
Baggage.SetBaggage("user.id", userId);

// Read baggage (in another service)
var tenantId = Baggage.GetBaggage("tenant.id");
```

**Note:** Baggage is propagated in `baggage` HTTP header, separate from `traceparent`.

**Warning:** Baggage adds overhead to every request. Use sparingly.

## Sampling

Decides whether to record span.

```csharp
// Check if activity is recorded
if (Activity.Current?.Recorded == true)
{
    // This span will be exported
}
```

**Sampling Decisions:**
- Made at trace creation (root span)
- Propagated to child spans
- Can be overridden by processors

## Context Flow

### Same Process

```csharp
using var parent = ActivitySource.StartActivity("Parent");

// Child automatically linked
using var child = ActivitySource.StartActivity("Child");
// child.ParentSpanId == parent.SpanId
```

### Across HTTP

```csharp
// Service A
using var activity = ActivitySource.StartActivity("CallServiceB");
var response = await httpClient.GetAsync("https://service-b/api");
// traceparent header automatically added

// Service B (ASP.NET Core)
// Activity automatically created with parent context from header
```

### Across Message Queue

```csharp
// Producer
using var activity = ActivitySource.StartActivity("SendMessage", ActivityKind.Producer);
var message = new Message
{
    Body = data,
    Properties = { ["traceparent"] = Activity.Current?.Id }
};
await queue.SendAsync(message);

// Consumer
var traceparent = message.Properties["traceparent"];
var parentContext = ActivityContext.Parse(traceparent, null);

using var activity = ActivitySource.StartActivity(
    "ProcessMessage",
    ActivityKind.Consumer,
    parentContext);
```

## Migration from 2.x Concepts

| 2.x Concept | 3.x Concept |
|-------------|-------------|
| `RequestTelemetry` | Activity with `ActivityKind.Server` |
| `DependencyTelemetry` | Activity with `ActivityKind.Client` |
| `operation_Id` | `TraceId` |
| `id` | `SpanId` |
| `operation_ParentId` | `ParentSpanId` |
| Properties dictionary | Tags (SetTag) |
| Custom events | ActivityEvent |
| `StartOperation<T>()` | `ActivitySource.StartActivity()` |

## Best Practices

### 1. Use Semantic Conventions

```csharp
// Good: Standard attributes
activity.SetTag("http.request.method", "GET");

// Avoid: Custom naming
activity.SetTag("method", "GET");
```

### 2. Set Appropriate Activity Kind

```csharp
// HTTP client
using var activity = ActivitySource.StartActivity("API Call", ActivityKind.Client);

// Internal operation
using var activity = ActivitySource.StartActivity("Calculate", ActivityKind.Internal);
```

### 3. Record Exceptions

```csharp
try
{
    await ProcessAsync();
}
catch (Exception ex)
{
    activity?.RecordException(ex);
    activity?.SetStatus(ActivityStatusCode.Error);
    throw;
}
```

### 4. Use Meaningful Names

```csharp
// Good: Descriptive
ActivitySource.StartActivity("ProcessOrderPayment");

// Bad: Generic
ActivitySource.StartActivity("Process");
```

## See Also

- [activity-vs-telemetry.md](../concepts/activity-vs-telemetry.md)
- [activity-source.md](activity-source.md)
- [correlation-and-distributed-tracing.md](../common-scenarios/correlation-and-distributed-tracing.md)
- [OpenTelemetry Tracing Specification](https://opentelemetry.io/docs/specs/otel/trace/api/)
