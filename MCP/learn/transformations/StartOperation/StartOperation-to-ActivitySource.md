# Migrate StartOperation to ActivitySource

**Category:** Transformation Guide  
**Applies to:** Custom instrumentation migration  
**Related:** [activity-source.md](../../opentelemetry-fundamentals/activity-source.md), [activity-vs-telemetry.md](../../concepts/activity-vs-telemetry.md)

## Overview

`TelemetryClient.StartOperation<T>()` is replaced by `ActivitySource.StartActivity()` in 3.x. This provides better performance, standards compliance (W3C Trace Context), and automatic distributed tracing.

## Key Differences

| Aspect | 2.x StartOperation | 3.x ActivitySource |
|--------|-------------------|-------------------|
| **Creation** | `telemetryClient.StartOperation<T>()` | `ActivitySource.StartActivity()` |
| **Type** | Telemetry-specific (Request/Dependency) | Generic Activity |
| **Properties** | `operation.Telemetry.Properties[key]` | `activity.SetTag(key, value)` |
| **Success** | `operation.Telemetry.Success = true` | `activity.SetStatus(ActivityStatusCode.Ok)` |
| **Disposal** | Must dispose to send | Must dispose to complete |
| **Injection** | Requires TelemetryClient DI | Static ActivitySource |
| **Performance** | Heavier (creates telemetry objects) | Lighter (Activity is native) |

## Basic Migration

### Before (2.x): StartOperation with RequestTelemetry

```csharp
public class OrderService
{
    private readonly TelemetryClient _telemetryClient;
    
    public OrderService(TelemetryClient telemetryClient)
    {
        _telemetryClient = telemetryClient;
    }
    
    public async Task<Order> CreateOrderAsync(Order order)
    {
        using var operation = _telemetryClient.StartOperation<RequestTelemetry>("CreateOrder");
        
        try
        {
            operation.Telemetry.Properties["orderId"] = order.Id.ToString();
            operation.Telemetry.Properties["customerId"] = order.CustomerId.ToString();
            
            await _repository.SaveAsync(order);
            
            operation.Telemetry.Success = true;
            return order;
        }
        catch (Exception ex)
        {
            operation.Telemetry.Success = false;
            _telemetryClient.TrackException(ex);
            throw;
        }
    }
}
```

### After (3.x): ActivitySource

```csharp
public class OrderService
{
    private static readonly ActivitySource ActivitySource = new("MyCompany.OrderService");
    
    // No TelemetryClient needed
    
    public async Task<Order> CreateOrderAsync(Order order)
    {
        using var activity = ActivitySource.StartActivity("CreateOrder");
        
        try
        {
            activity?.SetTag("order.id", order.Id);
            activity?.SetTag("customer.id", order.CustomerId);
            
            await _repository.SaveAsync(order);
            
            activity?.SetStatus(ActivityStatusCode.Ok);
            return order;
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

**Registration (Program.cs):**
```csharp
builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        // Register your ActivitySource
        otel.AddSource("MyCompany.OrderService");
    });
```

## DependencyTelemetry Migration

### Before (2.x): External Service Call

```csharp
public async Task<PaymentResult> ProcessPaymentAsync(decimal amount)
{
    using var operation = _telemetryClient.StartOperation<DependencyTelemetry>("ProcessPayment");
    operation.Telemetry.Type = "HTTP";
    operation.Telemetry.Target = "payment-api.example.com";
    operation.Telemetry.Data = $"POST /api/payments";
    
    try
    {
        var result = await _httpClient.PostAsync("https://payment-api.example.com/api/payments", content);
        
        operation.Telemetry.Success = result.IsSuccessStatusCode;
        operation.Telemetry.ResultCode = ((int)result.StatusCode).ToString();
        
        return await result.Content.ReadFromJsonAsync<PaymentResult>();
    }
    catch (Exception ex)
    {
        operation.Telemetry.Success = false;
        _telemetryClient.TrackException(ex);
        throw;
    }
}
```

### After (3.x): ActivityKind.Client

```csharp
public async Task<PaymentResult> ProcessPaymentAsync(decimal amount)
{
    using var activity = ActivitySource.StartActivity(
        "ProcessPayment", 
        ActivityKind.Client); // Client = outgoing dependency
    
    try
    {
        activity?.SetTag("http.method", "POST");
        activity?.SetTag("http.url", "https://payment-api.example.com/api/payments");
        activity?.SetTag("payment.amount", amount);
        
        // HttpClient automatically propagates trace context
        var result = await _httpClient.PostAsync("https://payment-api.example.com/api/payments", content);
        
        activity?.SetTag("http.status_code", (int)result.StatusCode);
        activity?.SetStatus(result.IsSuccessStatusCode ? 
            ActivityStatusCode.Ok : ActivityStatusCode.Error);
        
        return await result.Content.ReadFromJsonAsync<PaymentResult>();
    }
    catch (Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity?.RecordException(ex);
        throw;
    }
}
```

## Nested Operations

### Before (2.x): Manual Nesting

```csharp
public async Task ProcessOrderAsync(Order order)
{
    using var orderOp = _telemetryClient.StartOperation<RequestTelemetry>("ProcessOrder");
    
    // Child operation 1
    using (var validateOp = _telemetryClient.StartOperation<DependencyTelemetry>("ValidateOrder"))
    {
        validateOp.Telemetry.Type = "InProc";
        await ValidateOrderAsync(order);
        validateOp.Telemetry.Success = true;
    }
    
    // Child operation 2
    using (var saveOp = _telemetryClient.StartOperation<DependencyTelemetry>("SaveOrder"))
    {
        saveOp.Telemetry.Type = "InProc";
        await _repository.SaveAsync(order);
        saveOp.Telemetry.Success = true;
    }
    
    orderOp.Telemetry.Success = true;
}
```

### After (3.x): Automatic Nesting

```csharp
public async Task ProcessOrderAsync(Order order)
{
    using var activity = ActivitySource.StartActivity("ProcessOrder");
    
    // Child activities automatically linked to parent
    await ValidateOrderAsync(order);
    await SaveOrderAsync(order);
    
    activity?.SetStatus(ActivityStatusCode.Ok);
}

private async Task ValidateOrderAsync(Order order)
{
    using var activity = ActivitySource.StartActivity("ValidateOrder");
    // ... validation logic ...
    activity?.SetStatus(ActivityStatusCode.Ok);
}

private async Task SaveOrderAsync(Order order)
{
    using var activity = ActivitySource.StartActivity("SaveOrder");
    await _repository.SaveAsync(order);
    activity?.SetStatus(ActivityStatusCode.Ok);
}
```

**Key Benefit:** Parent-child relationship is automatic through `Activity.Current`.

## StartOperation with ParentId

### Before (2.x): Manual Parent Linking

```csharp
public void ProcessMessage(string message, string parentId)
{
    var operation = _telemetryClient.StartOperation<RequestTelemetry>("ProcessMessage", parentId);
    
    try
    {
        // Process message
        operation.Telemetry.Success = true;
    }
    finally
    {
        _telemetryClient.StopOperation(operation);
    }
}
```

### After (3.x): ActivityContext Parent

```csharp
public void ProcessMessage(string message, string traceparent)
{
    // Parse W3C traceparent header
    ActivityContext parentContext = default;
    if (!string.IsNullOrEmpty(traceparent))
    {
        parentContext = ActivityContext.Parse(traceparent, tracestate: null);
    }
    
    using var activity = ActivitySource.StartActivity(
        "ProcessMessage",
        ActivityKind.Consumer, // Consumer = message queue
        parentContext);
    
    try
    {
        // Process message
        activity?.SetStatus(ActivityStatusCode.Ok);
    }
    catch (Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity?.RecordException(ex);
        throw;
    }
}
```

## Custom Metrics in Operations

### Before (2.x)

```csharp
using var operation = _telemetryClient.StartOperation<RequestTelemetry>("ProcessBatch");

operation.Telemetry.Metrics["processedCount"] = 100;
operation.Telemetry.Metrics["failedCount"] = 5;

operation.Telemetry.Success = true;
```

### After (3.x)

```csharp
using var activity = ActivitySource.StartActivity("ProcessBatch");

// Use tags for counts (dimensions)
activity?.SetTag("processed.count", 100);
activity?.SetTag("failed.count", 5);

// Or use Meter for true metrics
var meter = new Meter("MyCompany.OrderService");
var processedCounter = meter.CreateCounter<int>("orders.processed");
var failedCounter = meter.CreateCounter<int>("orders.failed");

processedCounter.Add(100);
failedCounter.Add(5);

activity?.SetStatus(ActivityStatusCode.Ok);
```

## Background Job Instrumentation

### Before (2.x)

```csharp
public async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        using var operation = _telemetryClient.StartOperation<RequestTelemetry>("ProcessQueue");
        
        try
        {
            await ProcessMessagesAsync();
            operation.Telemetry.Success = true;
        }
        catch (Exception ex)
        {
            operation.Telemetry.Success = false;
            _telemetryClient.TrackException(ex);
        }
        
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
    }
}
```

### After (3.x)

```csharp
private static readonly ActivitySource ActivitySource = new("MyCompany.BackgroundWorker");

public async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        using var activity = ActivitySource.StartActivity(
            "ProcessQueue", 
            ActivityKind.Internal); // Internal = background work
        
        try
        {
            await ProcessMessagesAsync();
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
        }
        
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
    }
}
```

## Migration Checklist

- [ ] Create static `ActivitySource` for each component
- [ ] Replace `StartOperation<RequestTelemetry>` with `StartActivity()`
- [ ] Replace `StartOperation<DependencyTelemetry>` with `StartActivity(kind: ActivityKind.Client)`
- [ ] Convert `operation.Telemetry.Properties` to `activity.SetTag()`
- [ ] Replace `Success = true/false` with `SetStatus(ActivityStatusCode)`
- [ ] Replace `TrackException` with `RecordException`
- [ ] Remove `TelemetryClient` dependency injection
- [ ] Register `ActivitySource` in `ConfigureOpenTelemetryBuilder`
- [ ] Update parent linking to use `ActivityContext`
- [ ] Remove manual `StopOperation` calls (use `using` statement)

## Performance Benefits

**2.x:** Creating operation allocates telemetry objects, requires DI lookup
```csharp
// Allocates RequestTelemetry, properties dictionary, etc.
using var operation = _telemetryClient.StartOperation<RequestTelemetry>("name");
```

**3.x:** Activity is lightweight, native .NET primitive
```csharp
// Much lighter allocation, may not allocate if not sampled
using var activity = ActivitySource.StartActivity("name");
```

## See Also

- [activity-source.md](../../opentelemetry-fundamentals/activity-source.md)
- [activity-kinds.md](../../concepts/activity-kinds.md)
- [activity-vs-telemetry.md](../../concepts/activity-vs-telemetry.md)
