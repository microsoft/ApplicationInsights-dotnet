---
title: Activity vs Telemetry Types (RequestTelemetry, DependencyTelemetry)
category: concept
applies-to: both
related:
  - concepts/activity-processor.md
  - mappings/telemetry-to-activity.md
  - api-reference/Activity/SetTag.md
source: System.Diagnostics.Activity, Microsoft.ApplicationInsights.DataContracts
---

# Activity vs Telemetry Types

## Overview

Application Insights 2.x used custom telemetry types (`RequestTelemetry`, `DependencyTelemetry`, etc.), while 3.x uses the standard .NET `Activity` class from `System.Diagnostics`. Understanding this fundamental shift is critical for migration.

## In 2.x: Telemetry Types

Application Insights 2.x defined proprietary telemetry classes:

```csharp
// Source: ApplicationInsights-dotnet-2x/BASE/src/Microsoft.ApplicationInsights/DataContracts/

// For HTTP requests, page views
public class RequestTelemetry : ITelemetry
{
    public string Name { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public TimeSpan Duration { get; set; }
    public string ResponseCode { get; set; }
    public bool Success { get; set; }
    public IDictionary<string, string> Properties { get; }
    public TelemetryContext Context { get; }
    // ... more properties
}

// For outbound dependencies (HTTP, SQL, etc.)
public class DependencyTelemetry : ITelemetry
{
    public string Name { get; set; }
    public string Type { get; set; }
    public string Target { get; set; }
    public string Data { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public IDictionary<string, string> Properties { get; }
    // ... more properties
}

// Usage in 2.x
var telemetry = new RequestTelemetry
{
    Name = "GET /api/users",
    Timestamp = DateTimeOffset.UtcNow,
    Duration = TimeSpan.FromMilliseconds(150),
    ResponseCode = "200",
    Success = true
};
telemetry.Properties["UserId"] = "12345";
telemetryClient.TrackRequest(telemetry);
```

## In 3.x: Activity (OpenTelemetry Spans)

Application Insights 3.x uses the standard .NET `Activity` class, which represents a distributed trace span in OpenTelemetry:

```csharp
// Source: System.Diagnostics.Activity (.NET BCL)

public class Activity : IDisposable
{
    public string DisplayName { get; set; }              // Maps to Name
    public ActivityKind Kind { get; set; }               // Server, Client, Internal, etc.
    public ActivityStatusCode Status { get; set; }       // Ok, Error, Unset
    public DateTime StartTimeUtc { get; }                // Automatic
    public TimeSpan Duration { get; }                    // Automatic
    
    // Tags = custom properties/dimensions
    public Activity SetTag(string key, object? value);   // Maps to Properties
    public object? GetTagItem(string key);
    
    // Baggage = propagated context
    public Activity SetBaggage(string key, string? value);
    
    // ... more members
}

// Usage in 3.x (via TelemetryClient which creates Activities internally)
using (var activity = telemetryClient.StartOperation<RequestTelemetry>("GET /api/users"))
{
    // Activity is created automatically
    // In 3.x, this creates an Activity with ActivityKind.Server
    activity.Telemetry.ResponseCode = "200";
    activity.Telemetry.Success = true;
    
    // Or directly with Activity:
    activity.Telemetry.Properties["UserId"] = "12345";
    // In 3.x internally: Activity.Current.SetTag("UserId", "12345");
}
```

## Key Differences

| Aspect | 2.x Telemetry | 3.x Activity |
|--------|--------------|--------------|
| **Type** | Custom AI types | Standard .NET `Activity` |
| **Standard** | Application Insights proprietary | OpenTelemetry / W3C Trace Context |
| **Scope** | Request or Dependency | Unified "span" concept |
| **Kind** | Separate classes | `ActivityKind` enum (Server, Client, Internal, Producer, Consumer) |
| **Properties** | `Properties` dictionary | `SetTag()` / `GetTagItem()` |
| **Context** | `TelemetryContext` | Activity propagation + Resource |
| **Timing** | Manual `Timestamp` + `Duration` | Automatic (start/stop) |
| **Success** | `bool Success` | `ActivityStatusCode` (Ok, Error, Unset) |
| **ResponseCode** | `string ResponseCode` | Tag: `http.response.status_code` |

## Conceptual Mapping

### RequestTelemetry → Activity (Kind = Server)

```csharp
// 2.x
var request = new RequestTelemetry
{
    Name = "GET /api/users",
    ResponseCode = "200",
    Success = true,
    Duration = TimeSpan.FromMilliseconds(150)
};
request.Properties["custom.dimension"] = "value";

// 3.x equivalent (Activity with ActivityKind.Server)
var activity = new Activity("GET /api/users")
    .SetTag("http.response.status_code", 200)
    .SetStatus(ActivityStatusCode.Ok)  // Success = true
    .SetTag("custom.dimension", "value");
// Duration is automatic from Start() to Stop()
```

### DependencyTelemetry → Activity (Kind = Client)

```csharp
// 2.x
var dependency = new DependencyTelemetry
{
    Type = "HTTP",
    Target = "api.example.com",
    Name = "GET /users",
    Data = "https://api.example.com/users",
    Success = true,
    Duration = TimeSpan.FromMilliseconds(50)
};

// 3.x equivalent (Activity with ActivityKind.Client)
var activity = new Activity("GET /users")
    .SetTag("http.url", "https://api.example.com/users")
    .SetTag("server.address", "api.example.com")
    .SetTag("http.request.method", "GET")
    .SetStatus(ActivityStatusCode.Ok);
activity.Kind = ActivityKind.Client;  // Identifies this as a dependency
```

## ActivityKind Determines Telemetry Type

The `ActivityKind` enum in 3.x determines what type of telemetry this represents:

| ActivityKind | 2.x Equivalent | Use Case |
|--------------|----------------|----------|
| `Server` | `RequestTelemetry` | Incoming HTTP request, RPC call |
| `Client` | `DependencyTelemetry` | Outgoing HTTP call, database query, queue message send |
| `Internal` | _(no direct equivalent)_ | Internal operation within the app |
| `Producer` | `DependencyTelemetry` | Publishing to queue/event hub |
| `Consumer` | `RequestTelemetry` | Consuming from queue/event hub |

## Context and Correlation

```csharp
// 2.x: TelemetryContext for correlation
request.Context.Operation.Id = "trace-id";
request.Context.Operation.ParentId = "parent-span-id";
request.Context.Cloud.RoleName = "MyService";

// 3.x: Activity handles correlation automatically via W3C Trace Context
// TraceId and SpanId are built-in properties
activity.TraceId;   // Distributed trace ID (maps to Operation.Id)
activity.SpanId;    // This span's ID
activity.ParentSpanId;  // Parent span ID

// Cloud role is set via Resource attributes (see resource-detector.md)
```

## Why This Change?

### Benefits of Activity over Custom Telemetry Types:

1. **Industry Standard**: OpenTelemetry is the CNCF standard for observability
2. **Interoperability**: Works with Prometheus, Jaeger, Zipkin, etc., not just Azure Monitor
3. **Built-in .NET**: No custom SDK types, part of .NET runtime
4. **W3C Trace Context**: Standard distributed tracing propagation
5. **Unified Model**: One type for all spans (requests, dependencies, internal)
6. **Automatic Instrumentation**: .NET libraries create Activities automatically (HttpClient, SqlClient, etc.)

## Migration Implications

When migrating from 2.x to 3.x:

1. **Custom telemetry creation**: Instead of `new RequestTelemetry()`, use `Activity` or let instrumentation create them
2. **Properties**: Replace `telemetry.Properties[key]` with `activity.SetTag(key, value)`
3. **Success**: Replace `telemetry.Success = bool` with `activity.SetStatus(ActivityStatusCode.Ok/Error)`
4. **Kind awareness**: Understand that `ActivityKind` determines whether it's a request or dependency
5. **Automatic timing**: Don't set `Timestamp`/`Duration` manually, use `activity.Start()`/`Stop()`

## Common Patterns

### Pattern 1: Enriching Request Telemetry (2.x) → Enriching Server Activities (3.x)

```csharp
// 2.x: ITelemetryInitializer
public void Initialize(ITelemetry telemetry)
{
    if (telemetry is RequestTelemetry request)
    {
        request.Properties["enriched"] = "value";
    }
}

// 3.x: Activity Processor
public override void OnEnd(Activity activity)
{
    if (activity.Kind == ActivityKind.Server)  // Server = Request
    {
        activity.SetTag("enriched", "value");
    }
}
```

### Pattern 2: Filtering Dependency Telemetry (2.x) → Filtering Client Activities (3.x)

```csharp
// 2.x: ITelemetryProcessor
public void Process(ITelemetry item)
{
    if (item is DependencyTelemetry dep && dep.Success)
    {
        return;  // Filter out successful dependencies
    }
    next.Process(item);
}

// 3.x: Activity Processor
public override void OnEnd(Activity activity)
{
    if (activity.Kind == ActivityKind.Client &&  // Client = Dependency
        activity.Status == ActivityStatusCode.Ok)
    {
        // Drop this activity (don't record)
        activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
    }
}
```

## See Also

- [activity-processor.md](activity-processor.md) - How to process Activities
- [activity-kinds.md](activity-kinds.md) - Detailed ActivityKind guide
- [activity-status.md](activity-status.md) - ActivityStatusCode vs Success
- [mappings/telemetry-to-activity.md](../mappings/telemetry-to-activity.md) - Complete property mapping
- [api-reference/Activity/SetTag.md](../api-reference/Activity/SetTag.md) - Using tags (properties)

## References

- **System.Diagnostics.Activity**: .NET BCL, part of runtime
- **OpenTelemetry Specification**: https://opentelemetry.io/docs/specs/otel/trace/api/
- **W3C Trace Context**: https://www.w3.org/TR/trace-context/
