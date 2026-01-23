# ActivitySource and Custom Activities

**Category:** OpenTelemetry Fundamentals  
**Applies to:** Application Insights .NET SDK 3.x  
**Related:** [activity-vs-telemetry.md](../concepts/activity-vs-telemetry.md)

## Overview

`ActivitySource` is the OpenTelemetry mechanism for creating custom Activities (spans) to track operations in your code. It replaces manual `TelemetryClient.StartOperation()` from 2.x.

## Quick Solution

```csharp
// Define ActivitySource (typically static, per-assembly)
private static readonly ActivitySource MyActivitySource = new("MyCompany.MyService");

// Create Activity
using var activity = MyActivitySource.StartActivity("MyOperation");
activity?.SetTag("order.id", orderId);

// Operation code here

// Activity automatically stopped when disposed
```

## Creating ActivitySource

### Naming Convention

```csharp
// Format: Company.Product.Component
private static readonly ActivitySource Source = new("Contoso.OrderService.Processing");

// Or use assembly name
private static readonly ActivitySource Source = new(
    Assembly.GetExecutingAssembly().GetName().Name);
```

### Registration

```csharp
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        // Subscribe to your ActivitySource
        builder.AddSource("Contoso.OrderService.Processing");
        
        // Or wildcard
        builder.AddSource("Contoso.*");
    });
```

## Creating Activities

### Basic Activity

```csharp
using var activity = MyActivitySource.StartActivity("ProcessOrder");

// Activity is now current (Activity.Current == activity)
// Child operations will automatically parent to this activity

await ProcessOrderAsync(order);

// Activity stopped and recorded when disposed
```

### Activity with Kind

```csharp
// Server: Incoming requests
using var activity = MyActivitySource.StartActivity("HandleRequest", ActivityKind.Server);

// Client: Outgoing calls
using var activity = MyActivitySource.StartActivity("CallExternalAPI", ActivityKind.Client);

// Internal: Internal operations
using var activity = MyActivitySource.StartActivity("ProcessData", ActivityKind.Internal);

// Producer: Send message
using var activity = MyActivitySource.StartActivity("SendMessage", ActivityKind.Producer);

// Consumer: Receive message
using var activity = MyActivitySource.StartActivity("ProcessMessage", ActivityKind.Consumer);
```

### Activity with Tags

```csharp
using var activity = MyActivitySource.StartActivity("ProcessOrder");
activity?.SetTag("order.id", order.Id);
activity?.SetTag("customer.id", order.CustomerId);
activity?.SetTag("order.total", order.TotalAmount);
```

## Real-World Examples

### Example 1: Background Job Processing

```csharp
public class OrderProcessingJob
{
    private static readonly ActivitySource ActivitySource = 
        new("OrderService.BackgroundJobs");
    
    public async Task ProcessOrdersAsync()
    {
        using var activity = ActivitySource.StartActivity(
            "ProcessOrders", 
            ActivityKind.Internal);
        
        activity?.SetTag("job.type", "order_processing");
        
        var orders = await GetPendingOrdersAsync();
        activity?.SetTag("order.count", orders.Count);
        
        foreach (var order in orders)
        {
            await ProcessSingleOrderAsync(order);
        }
    }
    
    private async Task ProcessSingleOrderAsync(Order order)
    {
        // Child activity automatically linked to parent
        using var activity = ActivitySource.StartActivity("ProcessSingleOrder");
        activity?.SetTag("order.id", order.Id);
        
        try
        {
            await order.ProcessAsync();
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}
```

### Example 2: Multi-Step Business Process

```csharp
public class OrderService
{
    private static readonly ActivitySource ActivitySource = 
        new("OrderService.Core");
    
    public async Task<OrderResult> CreateOrderAsync(OrderRequest request)
    {
        using var activity = ActivitySource.StartActivity("CreateOrder");
        activity?.SetTag("order.type", request.OrderType);
        
        // Step 1: Validate
        using (var validateActivity = ActivitySource.StartActivity("ValidateOrder"))
        {
            await ValidateAsync(request);
            validateActivity?.SetTag("validation.result", "passed");
        }
        
        // Step 2: Calculate price
        decimal price;
        using (var priceActivity = ActivitySource.StartActivity("CalculatePrice"))
        {
            price = await CalculatePriceAsync(request);
            priceActivity?.SetTag("price", price);
        }
        
        // Step 3: Save
        Order order;
        using (var saveActivity = ActivitySource.StartActivity("SaveOrder"))
        {
            order = await SaveOrderAsync(request, price);
            saveActivity?.SetTag("order.id", order.Id);
        }
        
        activity?.SetTag("order.id", order.Id);
        return new OrderResult { OrderId = order.Id, Price = price };
    }
}
```

### Example 3: External API Call Wrapper

```csharp
public class PaymentApiClient
{
    private static readonly ActivitySource ActivitySource = 
        new("OrderService.PaymentClient");
    private readonly HttpClient _httpClient;
    
    public async Task<PaymentResult> ProcessPaymentAsync(Payment payment)
    {
        // Create Client activity (dependency)
        using var activity = ActivitySource.StartActivity(
            "ProcessPayment", 
            ActivityKind.Client);
        
        activity?.SetTag("payment.amount", payment.Amount);
        activity?.SetTag("payment.currency", payment.Currency);
        
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "https://payment-api.example.com/process", 
                payment);
            
            activity?.SetTag("http.response.status_code", (int)response.StatusCode);
            
            if (response.IsSuccessStatusCode)
            {
                activity?.SetStatus(ActivityStatusCode.Ok);
                return await response.Content.ReadFromJsonAsync<PaymentResult>();
            }
            else
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Payment failed");
                throw new PaymentException("Payment processing failed");
            }
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}
```

## Migration from 2.x

### 2.x: StartOperation

```csharp
using var operation = _telemetryClient.StartOperation<RequestTelemetry>("MyOperation");
operation.Telemetry.Properties["orderId"] = orderId.ToString();

try
{
    // Operation code
    operation.Telemetry.Success = true;
}
catch (Exception ex)
{
    operation.Telemetry.Success = false;
    _telemetryClient.TrackException(ex);
    throw;
}
```

### 3.x: ActivitySource

```csharp
using var activity = MyActivitySource.StartActivity("MyOperation");
activity?.SetTag("order.id", orderId);

try
{
    // Operation code
    activity?.SetStatus(ActivityStatusCode.Ok);
}
catch (Exception ex)
{
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    activity?.RecordException(ex);
    throw;
}
```

## Activity Lifecycle

```csharp
// 1. Create ActivitySource (once, typically static)
private static readonly ActivitySource Source = new("MyService");

// 2. Start Activity
var activity = Source.StartActivity("Operation");
// activity.StartTimeUtc is recorded
// Activity becomes Activity.Current

// 3. Use Activity
activity?.SetTag("key", "value");
activity?.AddEvent(new ActivityEvent("checkpoint"));

// 4. Stop Activity
activity?.Stop(); // or activity?.Dispose()
// activity.Duration is calculated
// OnEnd processors called
// Exported to Application Insights
```

## Best Practices

1. **One ActivitySource per assembly/component**
2. **Use descriptive operation names**
3. **Always dispose Activities (use `using`)**
4. **Check for null** (`activity?.` pattern)
5. **Register ActivitySource** with OpenTelemetry

## See Also

- [activity-vs-telemetry.md](../concepts/activity-vs-telemetry.md)
- [activity-kinds.md](../concepts/activity-kinds.md)
- [SetTag.md](../api-reference/Activity/SetTag.md)
