---
title: Activity.Kind - Request vs Dependency
category: api-reference
applies-to: 3.x
namespace: System.Diagnostics
related:
  - concepts/activity-kinds.md
  - concepts/activity-vs-telemetry.md
source: System.Diagnostics.Activity (.NET BCL)
---

# Activity.Kind

## Signature

```csharp
namespace System.Diagnostics
{
    public class Activity
    {
        public ActivityKind Kind { get; }
    }
    
    public enum ActivityKind
    {
        Internal = 0,
        Server = 1,
        Client = 2,
        Producer = 3,
        Consumer = 4
    }
}
```

## Description

Indicates the relationship of the Activity to its parent, determining whether it appears as a **Request** or **Dependency** in Application Insights. Set at Activity creation and cannot be changed.

## 2.x Equivalent

```csharp
// 2.x: Different classes
RequestTelemetry request = new RequestTelemetry();
DependencyTelemetry dependency = new DependencyTelemetry();

// 3.x: Single Activity with Kind
var request = activitySource.StartActivity("Operation", ActivityKind.Server);
var dependency = activitySource.StartActivity("Call", ActivityKind.Client);
```

## ActivityKind Values

| Kind | 2.x Type | AI Type | Usage |
|------|----------|---------|-------|
| `Server` | `RequestTelemetry` | Request | Incoming HTTP, gRPC |
| `Client` | `DependencyTelemetry` | Dependency | Outgoing HTTP, database |
| `Producer` | `DependencyTelemetry` | Dependency | Send to queue |
| `Consumer` | `RequestTelemetry` | Request | Receive from queue |
| `Internal` | `DependencyTelemetry` | Dependency | Internal span |

## Setting Kind at Creation

```csharp
// ActivityKind is set when creating the Activity
var request = activitySource.StartActivity(
    "HandleRequest", 
    ActivityKind.Server);  // Request in AI

var dependency = activitySource.StartActivity(
    "CallDatabase", 
    ActivityKind.Client);  // Dependency in AI

// Kind cannot be changed after creation
// dependency.Kind = ActivityKind.Server;  // READONLY!
```

## Server (Incoming Requests)

Represents incoming requests to your service.

```csharp
// ASP.NET Core automatically creates Server activities
[HttpGet]
public IActionResult Get()
{
    // Activity.Current.Kind == ActivityKind.Server
    // Appears as Request in Application Insights
    return Ok();
}
```

**Application Insights:**
- Type: `requests`
- Appears in: Request table, Application Map (as your service)

## Client (Outgoing Dependencies)

Represents outgoing calls from your service.

```csharp
// HttpClient automatically creates Client activities
await httpClient.GetAsync("https://api.example.com");
// Activity.Kind == ActivityKind.Client
// Appears as Dependency in Application Insights
```

**Application Insights:**
- Type: `dependencies`
- Appears in: Dependencies table, Application Map (as arrow to external service)

## Producer (Message Send)

Represents sending a message to a queue/topic.

```csharp
using (var activity = activitySource.StartActivity(
    "SendMessage", 
    ActivityKind.Producer))
{
    activity?.SetTag("messaging.system", "AzureServiceBus");
    activity?.SetTag("messaging.destination", "orders-queue");
    
    await queueClient.SendAsync(message);
}
```

**Application Insights:**
- Type: `dependencies`
- Dependency Type: `Azure Service Bus` or `Azure Queue`

## Consumer (Message Receive)

Represents receiving and processing a message from a queue.

```csharp
public async Task ProcessMessageAsync(ServiceBusReceivedMessage message)
{
    using (var activity = activitySource.StartActivity(
        "ProcessMessage", 
        ActivityKind.Consumer))
    {
        activity?.SetTag("messaging.system", "AzureServiceBus");
        activity?.SetTag("messaging.source", "orders-queue");
        
        await ProcessOrderAsync(message);
    }
}
```

**Application Insights:**
- Type: `requests`
- Source: Queue name

## Internal (Internal Spans)

Represents internal operations that don't cross service boundaries.

```csharp
using (var activity = activitySource.StartActivity(
    "ProcessBusinessLogic", 
    ActivityKind.Internal))
{
    // Internal processing
    await CalculateOrderTotalAsync(order);
}
```

**Application Insights:**
- Type: `dependencies`
- Dependency Type: `InProc`

## Checking Kind in Processors

```csharp
public class KindSpecificProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        switch (activity.Kind)
        {
            case ActivityKind.Server:
                // Handle incoming requests
                EnrichRequest(activity);
                break;
                
            case ActivityKind.Client:
                // Handle outgoing dependencies
                EnrichDependency(activity);
                break;
                
            case ActivityKind.Producer:
            case ActivityKind.Consumer:
                // Handle messaging
                EnrichMessaging(activity);
                break;
                
            case ActivityKind.Internal:
                // Handle internal spans
                EnrichInternal(activity);
                break;
        }
    }
}
```

## Real-World Example: HTTP Request Processing

```csharp
[HttpPost("orders")]
public async Task<IActionResult> CreateOrder([FromBody] OrderRequest request)
{
    // Current Activity.Kind == ActivityKind.Server (ASP.NET Core)
    var requestActivity = Activity.Current;
    
    // Validate inventory (internal operation)
    using (var validateActivity = activitySource.StartActivity(
        "ValidateInventory", 
        ActivityKind.Internal))
    {
        await ValidateInventoryAsync(request.Items);
    }
    
    // Call payment service (outgoing HTTP)
    using (var paymentActivity = activitySource.StartActivity(
        "ProcessPayment", 
        ActivityKind.Client))
    {
        await httpClient.PostAsync("https://payment-service/charge", ...);
    }
    
    // Send confirmation email (message queue)
    using (var emailActivity = activitySource.StartActivity(
        "SendConfirmationEmail", 
        ActivityKind.Producer))
    {
        await queueClient.SendAsync(emailMessage);
    }
    
    return Ok();
}

// Application Insights shows:
// - Request: POST /orders (Server)
//   - Dependency: ValidateInventory (Internal)
//   - Dependency: POST https://payment-service (Client)
//   - Dependency: Send to email-queue (Producer)
```

## Application Map Visualization

```
          ┌──────────────┐
Browser───│  Your API    │
          │ (Server)     │
          └──────────────┘
                 │
         ┌───────┴───────┐
         │               │
    (Client)        (Producer)
         │               │
         ▼               ▼
  ┌──────────┐    ┌──────────┐
  │ Payment  │    │ Queue    │
  │ Service  │    │          │
  └──────────┘    └──────────┘
```

## Query by Kind in Application Insights

```kusto
// All incoming requests (Server, Consumer)
requests
| where timestamp > ago(1h)

// All outgoing dependencies (Client, Producer, Internal)
dependencies
| where timestamp > ago(1h)

// Specific dependency types
dependencies
| where type == "HTTP"  // Client
| where type == "Azure Service Bus"  // Producer
| where type == "InProc"  // Internal
```

## Common Patterns

### Pattern 1: Filter Health Checks (Server Only)

```csharp
public class HealthCheckFilterProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (activity.Kind == ActivityKind.Server)
        {
            var path = activity.GetTagItem("url.path") as string;
            if (path == "/health" || path == "/healthz")
            {
                activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
            }
        }
    }
}
```

### Pattern 2: Enrich Dependencies (Client Only)

```csharp
public class DependencyEnrichmentProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (activity.Kind == ActivityKind.Client)
        {
            var host = activity.GetTagItem("server.address") as string;
            
            // Add environment based on host
            if (host?.Contains(".prod.") == true)
            {
                activity.SetTag("dependency.environment", "production");
            }
        }
    }
}
```

### Pattern 3: Track Message Processing (Consumer)

```csharp
public class MessageProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        if (activity.Kind == ActivityKind.Consumer)
        {
            activity.SetTag("message.processor", "OrderProcessor");
            activity.SetTag("message.start", DateTime.UtcNow);
        }
    }
}
```

## Automatic Instrumentation Sets Kind

Most instrumentation libraries set ActivityKind automatically:

```csharp
// ASP.NET Core → Server
app.MapGet("/api/orders", () => { });  // ActivityKind.Server

// HttpClient → Client
await httpClient.GetAsync(url);  // ActivityKind.Client

// Azure Service Bus → Producer/Consumer
await sender.SendMessageAsync(message);  // ActivityKind.Producer
await receiver.ReceiveMessageAsync();  // ActivityKind.Consumer

// Entity Framework → Client
await dbContext.Orders.ToListAsync();  // ActivityKind.Client
```

## Best Practices

```csharp
// ✅ GOOD: Use correct Kind
var clientCall = activitySource.StartActivity("CallAPI", ActivityKind.Client);
var serverHandler = activitySource.StartActivity("HandleRequest", ActivityKind.Server);

// ❌ WRONG: Incorrect Kind
var clientCall = activitySource.StartActivity("CallAPI", ActivityKind.Server);
// This is an outgoing call, should be Client!

// ✅ GOOD: Internal for business logic
var internal = activitySource.StartActivity("CalculateTotal", ActivityKind.Internal);

// ❌ AVOID: Default kind when specific is better
var activity = activitySource.StartActivity("CallAPI");  // Defaults to Internal
// Should be Client for HTTP call
```

## See Also

- [concepts/activity-kinds.md](../../concepts/activity-kinds.md) - Complete Kind guide
- [concepts/activity-vs-telemetry.md](../../concepts/activity-vs-telemetry.md) - Activity fundamentals

## References

- **Source**: `System.Diagnostics.Activity` (.NET BCL)
- **Activity Class**: https://learn.microsoft.com/dotnet/api/system.diagnostics.activity
