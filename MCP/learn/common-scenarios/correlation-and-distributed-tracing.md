# Correlation and Distributed Tracing in 3.x

**Category:** Common Scenario  
**Applies to:** Application Insights .NET SDK 3.x  
**Related:** [activity-vs-telemetry.md](../concepts/activity-vs-telemetry.md)

## Overview

Distributed tracing tracks requests as they flow through multiple services. In 3.x, this uses **W3C Trace Context** standard automatically via Activity, replacing 2.x's custom correlation headers.

## Quick Solution

```csharp
// Correlation works automatically in 3.x!
services.AddApplicationInsightsTelemetry();

// ASP.NET Core automatically:
// 1. Reads W3C traceparent header
// 2. Creates Activity with parent trace ID
// 3. Passes traceparent to downstream calls
// 4. Everything is correlated in Application Map
```

**No manual correlation code needed!**

## W3C Trace Context

### Header Format

```
traceparent: 00-{trace-id}-{parent-id}-{flags}
tracestate: {vendor-specific-data}
```

**Example:**
```
traceparent: 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01
tracestate: ai=...
```

### Components

- **trace-id:** 128-bit identifier for entire distributed trace (32 hex chars)
- **parent-id:** 64-bit identifier for parent span (16 hex chars)  
- **flags:** Sampling decision and other flags

## Automatic Correlation

### ASP.NET Core (Incoming Requests)

```csharp
// Automatically handles W3C traceparent header
public class OrderController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateOrder(Order order)
    {
        // Activity.Current already exists with:
        // - TraceId from traceparent header (or new if not present)
        // - ParentId from traceparent header
        // - W3C format automatically used
        
        Console.WriteLine($"TraceId: {Activity.Current.TraceId}");
        Console.WriteLine($"SpanId: {Activity.Current.SpanId}");
        Console.WriteLine($"ParentSpanId: {Activity.Current.ParentSpanId}");
        
        await orderService.ProcessAsync(order);
        return Ok();
    }
}
```

### HttpClient (Outgoing Requests)

```csharp
// HttpClient automatically adds traceparent header
public class PaymentService
{
    private readonly HttpClient _httpClient;
    
    public async Task<bool> ProcessPayment(Payment payment)
    {
        // traceparent header automatically added to request
        var response = await _httpClient.PostAsJsonAsync(
            "https://payment-api.example.com/process", 
            payment);
        
        // Downstream service receives traceparent header
        // and creates Activity with same TraceId
        return response.IsSuccessStatusCode;
    }
}
```

## Trace Hierarchy

### Parent-Child Relationship

```
Service A (TraceId: abc123, SpanId: 111)
  └─ HTTP Call to Service B (TraceId: abc123, SpanId: 222, ParentSpanId: 111)
      └─ SQL Query (TraceId: abc123, SpanId: 333, ParentSpanId: 222)
```

### Viewing in Azure Monitor

```kusto
// Find all operations in a trace
dependencies
| where timestamp > ago(1h)
| where operation_Id == "abc123..."
| project timestamp, name, type, duration, operation_Id, id, operation_ParentId
| order by timestamp asc
```

## Custom Correlation

### Manual Activity Creation

```csharp
private static readonly ActivitySource MyActivitySource = new("MyCompany.MyService");

public async Task ProcessOrderAsync(Order order)
{
    // Create custom Activity (child of current)
    using var activity = MyActivitySource.StartActivity("ProcessOrder");
    
    // Automatically inherits TraceId from parent
    activity?.SetTag("order.id", order.Id);
    activity?.SetTag("order.amount", order.Amount);
    
    // Child operations will have this Activity as parent
    await ValidateOrderAsync(order);
    await SaveOrderAsync(order);
    
    // Activity automatically stopped and duration recorded
}

// Register ActivitySource
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.AddSource("MyCompany.MyService");
    });
```

### Cross-Process Correlation

```csharp
// Service A: Create trace context
public async Task<string> SendMessageAsync()
{
    var message = new
    {
        Data = "...",
        // Capture current trace context
        TraceId = Activity.Current?.TraceId.ToString(),
        SpanId = Activity.Current?.SpanId.ToString()
    };
    
    await messageQueue.SendAsync(JsonSerializer.Serialize(message));
    return message.TraceId;
}

// Service B: Restore trace context
public async Task ProcessMessageAsync(string messageJson)
{
    var message = JsonSerializer.Deserialize<Message>(messageJson);
    
    // Create Activity with remote parent
    var parentContext = new ActivityContext(
        ActivityTraceId.CreateFromString(message.TraceId),
        ActivitySpanId.CreateFromString(message.SpanId),
        ActivityTraceFlags.Recorded);
    
    using var activity = MyActivitySource.StartActivity(
        "ProcessMessage",
        ActivityKind.Consumer,
        parentContext);
    
    // Now correlated with original request!
    await ProcessAsync(message.Data);
}
```

## Correlation Fields in Azure Monitor

### Mapping

| Activity Property | Azure Monitor Field | Purpose |
|------------------|---------------------|---------|
| `TraceId` | `operation_Id` | Groups related operations |
| `SpanId` | `id` | Identifies this operation |
| `ParentSpanId` | `operation_ParentId` | Links to parent operation |

### Example Query

```kusto
// Find entire distributed trace
let traceId = "4bf92f3577b34da6a3ce929d0e0e4736";

union requests, dependencies
| where operation_Id == traceId
| project 
    timestamp, 
    itemType, 
    name, 
    id, 
    operation_ParentId, 
    duration
| order by timestamp asc

// Visualize in Application Map
// Azure Portal > Application Insights > Application Map
```

## Migration from 2.x

### 2.x: Custom Correlation Headers

```csharp
// 2.x used custom headers: Request-Id, Request-Context
public class CorrelationInitializer : ITelemetryInitializer
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public void Initialize(ITelemetry telemetry)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        
        // Manual correlation header reading
        var requestId = httpContext?.Request.Headers["Request-Id"].ToString();
        if (!string.IsNullOrEmpty(requestId))
        {
            telemetry.Context.Operation.Id = ExtractOperationId(requestId);
            telemetry.Context.Operation.ParentId = requestId;
        }
    }
}

// Manual header propagation
httpClient.DefaultRequestHeaders.Add("Request-Id", GetRequestId());
```

### 3.x: W3C Trace Context (Automatic)

```csharp
// No code needed! Automatically handled by:
// - ASP.NET Core instrumentation
// - HttpClient instrumentation
// - Activity infrastructure

services.AddApplicationInsightsTelemetry();

// Headers automatically managed:
// traceparent: 00-{trace-id}-{span-id}-{flags}
// tracestate: ai=...
```

## Advanced Scenarios

### Custom Trace ID Generation

```csharp
public class CustomIdGenerator : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        // Custom business correlation ID
        var businessId = GetBusinessCorrelationId();
        activity.SetTag("business.correlation.id", businessId);
        
        // TraceId still used for technical correlation
    }
}
```

### Baggage Propagation

```csharp
// Add baggage (propagated to all child Activities)
Activity.Current?.SetBaggage("tenant.id", "tenant-123");
Activity.Current?.SetBaggage("user.id", "user-456");

// Child activities automatically have baggage
public async Task ChildOperation()
{
    var tenantId = Activity.Current?.GetBaggageItem("tenant.id");
    var userId = Activity.Current?.GetBaggageItem("user.id");
    
    // Use for routing, filtering, etc.
}
```

### Correlation Across Message Queues

```csharp
// Producer
public async Task SendMessageAsync(Order order)
{
    var message = new QueueMessage
    {
        Body = JsonSerializer.Serialize(order),
        Properties = new Dictionary<string, string>
        {
            ["traceparent"] = Activity.Current?.Id ?? string.Empty
        }
    };
    
    await queueClient.SendAsync(message);
}

// Consumer
public async Task ProcessMessageAsync(QueueMessage message)
{
    // Extract traceparent from message
    if (message.Properties.TryGetValue("traceparent", out var traceparent))
    {
        var parentContext = ActivityContext.Parse(traceparent, null);
        
        using var activity = MyActivitySource.StartActivity(
            "ProcessQueueMessage",
            ActivityKind.Consumer,
            parentContext);
        
        await ProcessAsync(message.Body);
    }
}
```

## Application Map

### Prerequisites for Accurate Map

1. **Cloud Role Name set:** Each service has unique `service.name`
2. **Instrumentation enabled:** ASP.NET Core, HttpClient, SQL, etc.
3. **Correlation working:** W3C trace context flowing between services

### Example Setup (Microservices)

```csharp
// Service A: Frontend
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.ConfigureResource(resource =>
            resource.AddService("Frontend"));
    });

// Service B: Order API
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.ConfigureResource(resource =>
            resource.AddService("OrderAPI"));
    });

// Service C: Payment API
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.ConfigureResource(resource =>
            resource.AddService("PaymentAPI"));
    });
```

**Result:** Application Map shows: Frontend → OrderAPI → PaymentAPI

## Troubleshooting

### Issue 1: Broken Correlation

**Symptom:** Operations appear isolated in Application Map

**Causes:**
- Missing HttpClient instrumentation
- Manual HttpClient without propagation
- Incorrect ActivitySource registration

**Solution:**
```csharp
// Ensure HttpClient created via IHttpClientFactory
services.AddHttpClient();

// Or manually enable instrumentation
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.AddHttpClientInstrumentation();
    });
```

### Issue 2: TraceId Changes Between Services

**Symptom:** Each service has different `operation_Id`

**Cause:** traceparent header not propagated

**Solution:** Verify HttpClient instrumentation enabled

### Issue 3: No Parent-Child Relationships

**Symptom:** All operations have same `operation_Id` but no hierarchy

**Cause:** `operation_ParentId` not set correctly

**Solution:** Check Activity parent context

## Performance Impact

Correlation has **minimal performance impact**:
- Header parsing: ~microseconds
- Activity creation: ~microseconds  
- Memory: ~few KB per Activity

**Best Practices:**
1. Don't create Activities in tight loops
2. Use ActivitySource sparingly
3. Let instrumentations handle most Activities

## See Also

- [activity-vs-telemetry.md](../concepts/activity-vs-telemetry.md) - Activity concept
- [setting-cloud-role-name.md](./setting-cloud-role-name.md) - Service identification
- [W3C Trace Context Specification](https://www.w3.org/TR/trace-context/)
- [Application Map Documentation](https://learn.microsoft.com/azure/azure-monitor/app/app-map)
