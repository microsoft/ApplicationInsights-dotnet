# TrackEvent Method Removed

**Category:** Breaking Change  
**Applies to:** TelemetryClient.TrackEvent  
**Severity:** High - Common usage pattern  
**Related:** [TrackTrace-behavior-changed.md](TrackTrace-behavior-changed.md)

## Breaking Change

`TelemetryClient.TrackEvent()` method has been removed in 3.x.

## In 2.x

```csharp
public class OrderService
{
    private readonly TelemetryClient _telemetryClient;
    
    public async Task PlaceOrderAsync(Order order)
    {
        // Track business event
        _telemetryClient.TrackEvent("OrderPlaced", new Dictionary<string, string>
        {
            ["OrderId"] = order.Id.ToString(),
            ["CustomerId"] = order.CustomerId.ToString(),
            ["TotalAmount"] = order.TotalAmount.ToString()
        });
        
        await _repository.SaveAsync(order);
    }
}
```

## In 3.x

**Option 1: Use Structured Logging (Recommended)**

```csharp
public class OrderService
{
    private readonly ILogger<OrderService> _logger;
    
    public async Task PlaceOrderAsync(Order order)
    {
        // Use structured logging instead
        _logger.LogInformation(
            "OrderPlaced: OrderId={OrderId}, CustomerId={CustomerId}, TotalAmount={TotalAmount}",
            order.Id,
            order.CustomerId,
            order.TotalAmount);
        
        await _repository.SaveAsync(order);
    }
}
```

Logs are automatically sent to Application Insights as trace telemetry with custom dimensions.

**Option 2: Use Activity Tags**

```csharp
public class OrderService
{
    private static readonly ActivitySource ActivitySource = new("MyCompany.OrderService");
    
    public async Task PlaceOrderAsync(Order order)
    {
        using var activity = ActivitySource.StartActivity("PlaceOrder");
        
        // Add event as tags
        activity?.SetTag("event.name", "OrderPlaced");
        activity?.SetTag("order.id", order.Id);
        activity?.SetTag("customer.id", order.CustomerId);
        activity?.SetTag("order.total", order.TotalAmount);
        
        await _repository.SaveAsync(order);
        
        activity?.SetStatus(ActivityStatusCode.Ok);
    }
}
```

**Option 3: Use Activity Events**

```csharp
public class OrderService
{
    private static readonly ActivitySource ActivitySource = new("MyCompany.OrderService");
    
    public async Task PlaceOrderAsync(Order order)
    {
        using var activity = ActivitySource.StartActivity("PlaceOrder");
        
        // Add event to activity
        var eventTags = new ActivityTagsCollection
        {
            ["order.id"] = order.Id,
            ["customer.id"] = order.CustomerId,
            ["order.total"] = order.TotalAmount
        };
        
        activity?.AddEvent(new ActivityEvent("OrderPlaced", tags: eventTags));
        
        await _repository.SaveAsync(order);
        
        activity?.SetStatus(ActivityStatusCode.Ok);
    }
}
```

## When to Use Each Option

| Scenario | Recommendation |
|----------|----------------|
| **Business events for analytics** | Structured logging (`ILogger`) |
| **Diagnostic events within operation** | Activity.AddEvent() |
| **Metadata for current operation** | Activity.SetTag() |
| **High-volume operational events** | Structured logging with appropriate log level |

## Migration Examples

### Simple Event

```csharp
// 2.x
_telemetryClient.TrackEvent("UserLoggedIn", new Dictionary<string, string>
{
    ["UserId"] = userId
});

// 3.x
_logger.LogInformation("UserLoggedIn: UserId={UserId}", userId);
```

### Event with Metrics

```csharp
// 2.x
_telemetryClient.TrackEvent("OrderPlaced",
    properties: new Dictionary<string, string> { ["OrderId"] = orderId },
    metrics: new Dictionary<string, double> { ["Amount"] = 99.99 });

// 3.x
_logger.LogInformation(
    "OrderPlaced: OrderId={OrderId}, Amount={Amount}",
    orderId,
    99.99);
```

### Multiple Events in Sequence

```csharp
// 2.x
_telemetryClient.TrackEvent("OrderValidationStarted");
await ValidateOrderAsync(order);
_telemetryClient.TrackEvent("OrderValidationCompleted");

_telemetryClient.TrackEvent("PaymentProcessingStarted");
await ProcessPaymentAsync(order);
_telemetryClient.TrackEvent("PaymentProcessingCompleted");

// 3.x
using var activity = ActivitySource.StartActivity("ProcessOrder");

activity?.AddEvent(new ActivityEvent("OrderValidationStarted"));
await ValidateOrderAsync(order);
activity?.AddEvent(new ActivityEvent("OrderValidationCompleted"));

activity?.AddEvent(new ActivityEvent("PaymentProcessingStarted"));
await ProcessPaymentAsync(order);
activity?.AddEvent(new ActivityEvent("PaymentProcessingCompleted"));
```

## Querying in Azure Monitor

### 2.x Events Query

```kusto
customEvents
| where name == "OrderPlaced"
| project timestamp, customDimensions.OrderId, customDimensions.TotalAmount
```

### 3.x Alternatives

**From Logs:**
```kusto
traces
| where message contains "OrderPlaced"
| project timestamp, customDimensions.OrderId, customDimensions.TotalAmount
```

**From Activity Tags:**
```kusto
requests
| where customDimensions["event.name"] == "OrderPlaced"
| project timestamp, customDimensions["order.id"], customDimensions["order.total"]
```

**From Activity Events:**
```kusto
requests
| mv-expand events = customDimensions.events
| where events.name == "OrderPlaced"
| project timestamp, events.attributes
```

## Why Was This Removed?

1. **Telemetry Consolidation**: OpenTelemetry focuses on traces, metrics, and logs (not custom events)
2. **Overlap with Logging**: Events are better represented as structured logs
3. **Activity Events**: Activity timeline events provide better context
4. **Simplified Model**: Fewer telemetry types to understand

## Alternatives Comparison

| Feature | TrackEvent (2.x) | Structured Logging (3.x) | Activity.AddEvent (3.x) |
|---------|------------------|-------------------------|------------------------|
| **Purpose** | Business events | Diagnostic logging | Operation timeline |
| **Storage** | customEvents table | traces table | Part of request/dependency |
| **Searchability** | Excellent | Excellent | Limited (part of parent) |
| **Volume** | High overhead | Low overhead | Minimal overhead |
| **Correlation** | Automatic | Automatic | Automatic |
| **Best For** | Analytics | Logging & monitoring | Detailed operation flow |

## Migration Checklist

- [ ] Identify all `TrackEvent()` calls
- [ ] Determine if event is for analytics or diagnostics
- [ ] Replace with appropriate alternative:
  - [ ] `ILogger` for business events
  - [ ] `Activity.AddEvent()` for operation timeline
  - [ ] `Activity.SetTag()` for operation metadata
- [ ] Update queries/dashboards to use new tables
- [ ] Test event data appears in Application Insights

## See Also

- [TrackTrace-behavior-changed.md](TrackTrace-behavior-changed.md)
- [TrackMetric-replaced.md](TrackMetric-replaced.md)
- [Custom Events Documentation](https://docs.microsoft.com/azure/azure-monitor/app/api-custom-events-metrics)
