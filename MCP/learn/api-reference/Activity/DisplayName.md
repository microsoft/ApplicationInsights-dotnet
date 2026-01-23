---
title: Activity.DisplayName - Operation Name
category: api-reference
applies-to: 3.x
namespace: System.Diagnostics
related:
  - concepts/activity-vs-telemetry.md
  - api-reference/Activity/OperationName.md
source: System.Diagnostics.Activity (.NET BCL)
---

# Activity.DisplayName

## Signature

```csharp
namespace System.Diagnostics
{
    public class Activity
    {
        public string DisplayName { get; set; }
        public string OperationName { get; }  // Readonly, set at creation
    }
}
```

## Description

The human-readable name of the operation. Maps to `name` field in Application Insights (e.g., `"GET /api/orders"`, `"ProcessPayment"`). Can be modified after Activity creation, unlike `OperationName`.

## 2.x Equivalent

```csharp
// 2.x: Name property
request.Name = "GET /api/orders";
dependency.Name = "SQL: GetOrders";

// 3.x: DisplayName property
activity.DisplayName = "GET /api/orders";
```

## Basic Usage

```csharp
using (var activity = activitySource.StartActivity("Operation"))
{
    // Set display name
    activity.DisplayName = "Process Order #12345";
    
    // activity.OperationName = "Operation" (readonly, from creation)
    // activity.DisplayName = "Process Order #12345" (changeable)
}
```

## DisplayName vs OperationName

| Property | Mutable? | Typical Value | Purpose |
|----------|----------|---------------|---------|
| `OperationName` | ❌ No | `"HttpClient.GET"` | Activity type/category |
| `DisplayName` | ✅ Yes | `"GET /api/orders"` | Human-readable name |

```csharp
var activity = activitySource.StartActivity("ProcessOrder");
// activity.OperationName = "ProcessOrder" (fixed)

activity.DisplayName = "Process order #12345";
// activity.DisplayName = "Process order #12345" (changed)

// Later...
activity.DisplayName = "Process order #12345 (completed)";
// Can be updated multiple times
```

## Automatic DisplayName Setting

ASP.NET Core automatically sets DisplayName for HTTP requests:

```csharp
// GET /api/orders/123
// Automatic DisplayName: "GET /api/orders/{id}"
```

HTTP Client automatically sets DisplayName for dependencies:

```csharp
await httpClient.GetAsync("https://api.example.com/orders");
// Automatic DisplayName: "GET"
```

## Custom DisplayName Patterns

### Pattern 1: Include Key Business ID

```csharp
using (var activity = activitySource.StartActivity("ProcessOrder"))
{
    activity.DisplayName = $"Process Order {orderId}";
    // Easier to find in Application Insights
}
```

### Pattern 2: Include Status

```csharp
using (var activity = activitySource.StartActivity("Payment"))
{
    activity.DisplayName = "Process Payment";
    
    try
    {
        await ProcessPaymentAsync();
        activity.DisplayName = "Process Payment (Success)";
    }
    catch
    {
        activity.DisplayName = "Process Payment (Failed)";
        throw;
    }
}
```

### Pattern 3: Route Template Instead of Raw Path

```csharp
public class RouteDisplayNameProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        if (activity.Kind == ActivityKind.Server)
        {
            // Change from "/api/orders/123" to "/api/orders/{id}"
            var route = GetRouteTemplate();
            var method = activity.GetTagItem("http.request.method");
            
            activity.DisplayName = $"{method} {route}";
        }
    }
}
```

## Application Insights Mapping

DisplayName becomes the `name` field:

```kusto
requests
| where name == "GET /api/orders/{id}"
| summarize count() by name
```

```kusto
dependencies
| where name == "SQL: GetOrdersByUser"
| summarize avg(duration) by name
```

## Real-World Example: Processor

```csharp
public class EnhancedDisplayNameProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        if (activity.Kind == ActivityKind.Server)
        {
            // Enhance HTTP request name
            var method = activity.GetTagItem("http.request.method") as string;
            var route = activity.GetTagItem("http.route") as string;
            
            if (!string.IsNullOrEmpty(method) && !string.IsNullOrEmpty(route))
            {
                activity.DisplayName = $"{method} {route}";
            }
        }
        else if (activity.Kind == ActivityKind.Client)
        {
            // Enhance HTTP dependency name
            var method = activity.GetTagItem("http.request.method") as string;
            var host = activity.GetTagItem("server.address") as string;
            var path = activity.GetTagItem("url.path") as string;
            
            if (!string.IsNullOrEmpty(method) && !string.IsNullOrEmpty(host))
            {
                activity.DisplayName = $"{method} {host}{path}";
            }
        }
    }
    
    public override void OnEnd(Activity activity)
    {
        // Add success/failure to name
        if (activity.Status == ActivityStatusCode.Error)
        {
            activity.DisplayName += " (Failed)";
        }
    }
}
```

## Database Operations

```csharp
using (var activity = activitySource.StartActivity("Database"))
{
    activity.DisplayName = "SQL: GetOrdersByUser";
    activity.SetTag("db.statement", "SELECT * FROM Orders WHERE UserId = @userId");
    
    var orders = await db.GetOrdersByUserAsync(userId);
}
```

## Message Queue Operations

```csharp
using (var activity = activitySource.StartActivity("MessageQueue"))
{
    activity.DisplayName = $"Send to {queueName}";
    activity.SetTag("messaging.destination", queueName);
    activity.SetTag("messaging.system", "AzureServiceBus");
    
    await queueClient.SendAsync(message);
}
```

## Best Practices

```csharp
// ✅ GOOD: Descriptive, template-based
activity.DisplayName = "GET /api/orders/{id}";
activity.DisplayName = "SQL: GetOrderById";
activity.DisplayName = "Process Order Payment";

// ❌ AVOID: Too specific (high cardinality)
activity.DisplayName = $"GET /api/orders/{orderId}";  // Different for every order
activity.DisplayName = $"Process Order at {DateTime.Now}";  // Different every time

// ❌ AVOID: Too generic
activity.DisplayName = "Process";
activity.DisplayName = "Operation";

// ✅ GOOD: Balance specificity and cardinality
activity.DisplayName = "Process Order";
activity.SetTag("order.id", orderId);  // Specific ID in tag, not name
```

## High Cardinality Warning

⚠️ **Avoid high cardinality in DisplayName** - it affects Application Insights aggregation:

```csharp
// ❌ BAD: Unique name for every request
activity.DisplayName = $"Order {orderId} for {userId} at {timestamp}";
// Creates millions of unique operation names!

// ✅ GOOD: Template name + specific tags
activity.DisplayName = "Process Order";
activity.SetTag("order.id", orderId);
activity.SetTag("user.id", userId);
// Single operation name, details in tags
```

## Query Examples

Find all failed requests:
```kusto
requests
| where name == "POST /api/orders"
| where success == false
```

Average duration by operation:
```kusto
requests
| summarize avg(duration) by name
| order by avg_duration desc
```

Most common operations:
```kusto
requests
| summarize count() by name
| top 10 by count_
```

## See Also

- [concepts/activity-vs-telemetry.md](../../concepts/activity-vs-telemetry.md) - Activity basics
- [Activity.OperationName](OperationName.md) - Readonly operation name

## References

- **Source**: `System.Diagnostics.Activity` (.NET BCL)
- **Activity Class**: https://learn.microsoft.com/dotnet/api/system.diagnostics.activity
