---
title: ActivityKind - Determining Telemetry Type
category: concept
applies-to: 3.x
related:
  - concepts/activity-vs-telemetry.md
  - concepts/activity-processor.md
  - mappings/telemetry-to-activity.md
source: System.Diagnostics.ActivityKind
---

# ActivityKind - Determining Telemetry Type

## Overview

In Application Insights 3.x, the `ActivityKind` enum determines what type of telemetry an Activity represents. This replaces the 2.x approach of having separate `RequestTelemetry` and `DependencyTelemetry` classes.

## In 2.x: Separate Telemetry Classes

```csharp
// Different classes for different telemetry types
var request = new RequestTelemetry();      // Incoming request
var dependency = new DependencyTelemetry();  // Outgoing call
var event = new EventTelemetry();          // Custom event
```

## In 3.x: ActivityKind Enum

```csharp
// Source: System.Diagnostics.ActivityKind
public enum ActivityKind
{
    Internal = 0,    // Internal operation
    Server = 1,      // Incoming request (was RequestTelemetry)
    Client = 2,      // Outgoing dependency (was DependencyTelemetry)
    Producer = 3,    // Message producer (queue/event hub send)
    Consumer = 4     // Message consumer (queue/event hub receive)
}

// Single Activity class, Kind determines type
var activity = new Activity("operation-name");
activity.Kind = ActivityKind.Server;  // This is a request
```

## ActivityKind Values Explained

### Server - Incoming Requests
**2.x equivalent**: `RequestTelemetry`

Represents incoming HTTP requests, RPC calls, or any work triggered by an external caller.

```csharp
// Automatic in ASP.NET Core - HttpClient creates Server activities
// Manual creation:
var activity = new Activity("GET /api/users");
activity.Kind = ActivityKind.Server;
activity.Start();
// ... handle request ...
activity.Stop();

// In processor - check if it's a request:
public override void OnEnd(Activity activity)
{
    if (activity.Kind == ActivityKind.Server)
    {
        // This is a request (was RequestTelemetry)
        var statusCode = activity.GetTagItem("http.response.status_code");
    }
}
```

**Common tags for Server activities:**
- `http.request.method` - HTTP method (GET, POST, etc.)
- `http.route` - Route template
- `http.response.status_code` - Response status
- `url.path` - Request path
- `url.query` - Query string

### Client - Outgoing Dependencies
**2.x equivalent**: `DependencyTelemetry`

Represents outgoing HTTP calls, database queries, or any external service calls made by your application.

```csharp
// Automatic - HttpClient, SqlClient create Client activities
using var httpClient = new HttpClient();
// Creates Activity with Kind = Client automatically
var response = await httpClient.GetAsync("https://api.example.com/data");

// Manual creation:
var activity = new Activity("GET https://api.example.com");
activity.Kind = ActivityKind.Client;
activity.Start();
// ... make external call ...
activity.Stop();

// In processor - check if it's a dependency:
public override void OnEnd(Activity activity)
{
    if (activity.Kind == ActivityKind.Client)
    {
        // This is a dependency (was DependencyTelemetry)
        var targetUrl = activity.GetTagItem("http.url");
    }
}
```

**Common tags for Client activities:**
- `http.request.method` - HTTP method
- `http.url` or `url.full` - Target URL
- `server.address` - Target server
- `server.port` - Target port
- `db.system` - Database type (for SQL)
- `db.statement` - SQL query (for SQL)

### Internal - Internal Operations
**2.x equivalent**: No direct equivalent (similar to manually tracked operations)

Represents internal operations that don't cross process boundaries. Used for detailed tracing within your application.

```csharp
// Create internal span for business logic
var activity = new Activity("CalculatePricing");
activity.Kind = ActivityKind.Internal;
activity.Start();
// ... business logic ...
activity.Stop();

// In processor:
public override void OnEnd(Activity activity)
{
    if (activity.Kind == ActivityKind.Internal)
    {
        // Internal operation - won't appear as dependency or request
        // Usually only sent when part of a traced request
    }
}
```

**Use cases:**
- Business logic operations
- Data processing steps
- Algorithm executions
- Cache operations (when not using cache instrumentation)

### Producer - Message/Event Publishing
**2.x equivalent**: `DependencyTelemetry` with Type="Queue" or Type="EventHub"

Represents publishing a message to a queue or event stream (async, non-blocking).

```csharp
// Azure Service Bus, Event Hubs, Kafka producer
var activity = new Activity("Send to OrderQueue");
activity.Kind = ActivityKind.Producer;
activity.SetTag("messaging.system", "azureservicebus");
activity.SetTag("messaging.destination.name", "order-queue");
activity.Start();
// ... send message ...
activity.Stop();

// In processor:
public override void OnEnd(Activity activity)
{
    if (activity.Kind == ActivityKind.Producer)
    {
        // Message was sent to queue/topic
        var destination = activity.GetTagItem("messaging.destination.name");
    }
}
```

**Common tags for Producer activities:**
- `messaging.system` - Messaging system (azureservicebus, kafka, rabbitmq)
- `messaging.destination.name` - Queue/topic name
- `messaging.operation` - publish, send, etc.

### Consumer - Message/Event Consumption
**2.x equivalent**: `RequestTelemetry` for queue-triggered operations

Represents receiving and processing a message from a queue or event stream.

```csharp
// Azure Functions with Service Bus trigger creates Consumer activities
var activity = new Activity("Process OrderMessage");
activity.Kind = ActivityKind.Consumer;
activity.SetTag("messaging.system", "azureservicebus");
activity.SetTag("messaging.source.name", "order-queue");
activity.Start();
// ... process message ...
activity.Stop();

// In processor:
public override void OnEnd(Activity activity)
{
    if (activity.Kind == ActivityKind.Consumer)
    {
        // Message was received from queue/topic
        var source = activity.GetTagItem("messaging.source.name");
    }
}
```

**Common tags for Consumer activities:**
- `messaging.system` - Messaging system
- `messaging.source.name` - Queue/topic name
- `messaging.operation` - receive, process, etc.
- `messaging.message.id` - Message ID

## Mapping to Application Insights

When sent to Azure Monitor, ActivityKind maps to Application Insights telemetry types:

| ActivityKind | AI Telemetry Type | Shows in Portal As |
|--------------|-------------------|-------------------|
| `Server` | Request | Requests |
| `Client` | Dependency | Dependencies |
| `Internal` | Dependency (type: InProc) | Dependencies (Internal) |
| `Producer` | Dependency (type: Queue) | Dependencies (Queue) |
| `Consumer` | Request | Requests (background) |

## Common Patterns in Activity Processors

### Pattern 1: Filter by Kind

```csharp
public class KindSpecificProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        switch (activity.Kind)
        {
            case ActivityKind.Server:
                // Only process incoming requests
                EnrichRequest(activity);
                break;
                
            case ActivityKind.Client:
                // Only process outgoing dependencies
                EnrichDependency(activity);
                break;
                
            case ActivityKind.Internal:
                // Usually skip or minimal processing
                break;
        }
    }
}
```

### Pattern 2: Different Logic per Kind

```csharp
public class ResponseTimeProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        var duration = activity.Duration.TotalMilliseconds;
        
        if (activity.Kind == ActivityKind.Server && duration > 1000)
        {
            // Slow request
            activity.SetTag("performance.slow_request", true);
        }
        else if (activity.Kind == ActivityKind.Client && duration > 5000)
        {
            // Slow dependency
            activity.SetTag("performance.slow_dependency", true);
        }
    }
}
```

### Pattern 3: Kind-based Filtering

```csharp
public class SuccessfulClientFilterProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Only filter successful Client (dependency) calls
        if (activity.Kind == ActivityKind.Client && 
            activity.Status == ActivityStatusCode.Ok)
        {
            // Drop successful dependencies to reduce volume
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
        }
        // Keep all Server (request) activities
    }
}
```

## Migration from 2.x

### 2.x: Type checking with `is`

```csharp
// 2.x - ITelemetryInitializer
public void Initialize(ITelemetry telemetry)
{
    if (telemetry is RequestTelemetry request)
    {
        // Process request
    }
    else if (telemetry is DependencyTelemetry dependency)
    {
        // Process dependency
    }
}
```

### 3.x: ActivityKind checking

```csharp
// 3.x - BaseProcessor<Activity>
public override void OnEnd(Activity activity)
{
    if (activity.Kind == ActivityKind.Server)
    {
        // Process request (was RequestTelemetry)
    }
    else if (activity.Kind == ActivityKind.Client)
    {
        // Process dependency (was DependencyTelemetry)
    }
}
```

## Performance Tip

ActivityKind is an enum, so comparison is very fast (integer comparison):

```csharp
// Fast - enum comparison
if (activity.Kind == ActivityKind.Server) { }

// Also fast - switch on enum
switch (activity.Kind)
{
    case ActivityKind.Server: /*...*/ break;
    case ActivityKind.Client: /*...*/ break;
}
```

## Default Values

- Activities created manually default to `ActivityKind.Internal`
- Instrumentation libraries set appropriate Kind automatically:
  - ASP.NET Core → `Server`
  - HttpClient → `Client`
  - SqlClient → `Client`
  - Azure SDK → `Client` (or `Producer`/`Consumer` for messaging)

## See Also

- [activity-vs-telemetry.md](activity-vs-telemetry.md) - Activity fundamentals
- [activity-processor.md](activity-processor.md) - Processing activities
- [activity-status.md](activity-status.md) - Activity status codes
- [mappings/telemetry-to-activity.md](../mappings/telemetry-to-activity.md) - Complete mapping guide

## References

- **System.Diagnostics.ActivityKind**: .NET BCL
- **OpenTelemetry Span Kind**: https://opentelemetry.io/docs/specs/otel/trace/api/#spankind
- **Semantic Conventions**: https://opentelemetry.io/docs/specs/semconv/
