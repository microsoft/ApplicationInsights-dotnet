# TrackRequest Behavior Changed

**Category:** Breaking Change  
**Applies to:** TelemetryClient API  
**Migration Effort:** Medium  
**Related:** [TrackDependency-behavior-changed.md](TrackDependency-behavior-changed.md), [activity-source.md](../../opentelemetry-fundamentals/activity-source.md)

## Change Summary

`TelemetryClient.TrackRequest()` still exists in 3.x but behaves differently. The method creates an `Activity` (Kind=Server) instead of `RequestTelemetry`. Some properties are no longer supported, and ASP.NET Core automatically tracks requests (manual tracking rarely needed).

## Behavior Changes

| Aspect | 2.x Behavior | 3.x Behavior |
|--------|--------------|--------------|
| **Return Type** | `void` | `void` |
| **Object Created** | `RequestTelemetry` | `Activity` (Kind=Server) |
| **Timing** | Must provide start time and duration | Can provide duration, or use StartOperation |
| **Correlation** | Manual correlation context | Automatic via Activity context |
| **URL** | `Uri` property | Mapped to `url.full` tag |
| **Response Code** | String property | Mapped to `http.response.status_code` |
| **Success** | Boolean property | Mapped to `ActivityStatusCode` |
| **Custom Properties** | `Properties` dictionary | Activity tags |
| **Metrics** | `Metrics` dictionary | **Not supported** (use Meter API) |

## When Manual Tracking is Needed

### ASP.NET Core

**2.x:** Automatic via middleware  
**3.x:** Automatic via OpenTelemetry ASP.NET Core instrumentation

**Manual tracking NOT needed** in ASP.NET Core 3.x for HTTP requests.

### Non-HTTP Scenarios

**Manual tracking needed for:**
- Background job executions
- Queue message processing
- Scheduled tasks
- Non-HTTP entry points

## Removed/Changed Features

### 1. Metrics Parameter Removed

**2.x:**
```csharp
var request = new RequestTelemetry
{
    Name = "ProcessOrder",
    Url = new Uri("internal://orders/process"),
    StartTime = startTime,
    Duration = duration,
    ResponseCode = "200",
    Success = true,
    Metrics =
    {
        ["OrderCount"] = 5,
        ["ProcessingTimeMs"] = 1234
    }
};
telemetryClient.Track(request);
```

**3.x:**
```csharp
// TrackRequest has no metrics parameter
telemetryClient.TrackRequest(
    name: "ProcessOrder",
    startTime: startTime,
    duration: duration,
    responseCode: "200",
    success: true);

// Use Meter API for metrics
private static readonly Meter Meter = new("MyApp.Orders");
private static readonly Counter<int> OrdersProcessed = 
    Meter.CreateCounter<int>("orders.processed");
private static readonly Histogram<double> ProcessingTime = 
    Meter.CreateHistogram<double>("order.processing.time", unit: "ms");

OrdersProcessed.Add(5);
ProcessingTime.Record(1234);
```

### 2. Custom Properties

**2.x:**
```csharp
var request = new RequestTelemetry
{
    Name = "ProcessOrder",
    Properties =
    {
        ["UserId"] = userId,
        ["OrderId"] = orderId,
        ["Priority"] = "High"
    }
};
telemetryClient.Track(request);
```

**3.x:**
```csharp
// Use ActivitySource for custom properties
using var activity = ActivitySource.StartActivity("ProcessOrder", ActivityKind.Server);
activity?.SetTag("user.id", userId);
activity?.SetTag("order.id", orderId);
activity?.SetTag("priority", "High");
```

### 3. Context Properties

**2.x:**
```csharp
var request = new RequestTelemetry
{
    Name = "ProcessOrder",
    Context =
    {
        User = { Id = userId },
        Session = { Id = sessionId },
        Operation = { Id = operationId, ParentId = parentId }
    }
};
```

**3.x:**
```csharp
// User/session context via BaseProcessor<Activity>
public class UserContextProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        activity.SetTag("enduser.id", userId);
        activity.SetTag("session.id", sessionId);
        // Operation ID = Activity.TraceId (automatic)
        // Parent ID = Activity.ParentId (automatic)
    }
}
```

## Migration Options

### Option 1: Remove Manual Tracking (ASP.NET Core)

**When to use:** ASP.NET Core applications with HTTP requests.

**2.x:**
```csharp
// Manual tracking in controller
[HttpPost("orders")]
public async Task<IActionResult> CreateOrder([FromBody] Order order)
{
    var startTime = DateTimeOffset.UtcNow;
    var stopwatch = Stopwatch.StartNew();
    
    try
    {
        var result = await _orderService.CreateOrderAsync(order);
        
        _telemetryClient.TrackRequest(
            name: "POST /api/orders",
            startTime: startTime,
            duration: stopwatch.Elapsed,
            responseCode: "200",
            success: true);
        
        return Ok(result);
    }
    catch
    {
        _telemetryClient.TrackRequest(
            name: "POST /api/orders",
            startTime: startTime,
            duration: stopwatch.Elapsed,
            responseCode: "500",
            success: false);
        throw;
    }
}
```

**3.x:**
```csharp
// Automatic tracking - just add tags to current Activity
[HttpPost("orders")]
public async Task<IActionResult> CreateOrder([FromBody] Order order)
{
    var activity = Activity.Current;
    activity?.SetTag("order.id", order.Id);
    activity?.SetTag("customer.id", order.CustomerId);
    
    var result = await _orderService.CreateOrderAsync(order);
    return Ok(result);
}
```

### Option 2: Migrate to ActivitySource (Background Jobs)

**When to use:** Non-HTTP scenarios like background jobs, queue processing.

**2.x:**
```csharp
public class OrderProcessorService : BackgroundService
{
    private readonly TelemetryClient _telemetryClient;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var startTime = DateTimeOffset.UtcNow;
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                await ProcessOrdersAsync();
                
                _telemetryClient.TrackRequest(
                    name: "ProcessOrders",
                    startTime: startTime,
                    duration: stopwatch.Elapsed,
                    responseCode: "200",
                    success: true);
            }
            catch (Exception ex)
            {
                _telemetryClient.TrackRequest(
                    name: "ProcessOrders",
                    startTime: startTime,
                    duration: stopwatch.Elapsed,
                    responseCode: "500",
                    success: false);
                
                _telemetryClient.TrackException(ex);
            }
            
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

**3.x:**
```csharp
public class OrderProcessorService : BackgroundService
{
    private static readonly ActivitySource ActivitySource = new("MyApp.BackgroundJobs");
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var activity = ActivitySource.StartActivity("ProcessOrders", ActivityKind.Server);
            
            try
            {
                await ProcessOrdersAsync();
                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.RecordException(ex);
            }
            
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}

// Register ActivitySource
builder.Services.ConfigureOpenTelemetryBuilder(otelBuilder =>
{
    otelBuilder.AddSource("MyApp.BackgroundJobs");
});
```

### Option 3: Queue Message Processing

**2.x:**
```csharp
public async Task ProcessMessageAsync(ServiceBusReceivedMessage message)
{
    var startTime = DateTimeOffset.UtcNow;
    var stopwatch = Stopwatch.StartNew();
    
    try
    {
        // Extract parent context
        var parentId = message.ApplicationProperties.TryGetValue("Diagnostic-Id", out var id) 
            ? id.ToString() 
            : null;
        
        var request = _telemetryClient.StartOperation<RequestTelemetry>(
            "ProcessMessage",
            operationId: message.MessageId,
            parentOperationId: parentId);
        
        using (request)
        {
            request.Telemetry.Properties["QueueName"] = "orders-queue";
            request.Telemetry.Properties["MessageId"] = message.MessageId;
            
            await ProcessMessageContentAsync(message);
            
            request.Telemetry.Success = true;
            request.Telemetry.ResponseCode = "200";
        }
    }
    catch (Exception ex)
    {
        // Error tracking
    }
}
```

**3.x:**
```csharp
public async Task ProcessMessageAsync(ServiceBusReceivedMessage message)
{
    // Extract parent context (W3C Trace Context from Service Bus)
    ActivityContext parentContext = default;
    if (message.ApplicationProperties.TryGetValue("Diagnostic-Id", out var diagnosticId))
    {
        // Parse W3C Trace Context
        parentContext = ActivityContext.Parse(diagnosticId.ToString(), null);
    }
    
    using var activity = ActivitySource.StartActivity(
        "ProcessMessage",
        ActivityKind.Consumer,
        parentContext);
    
    activity?.SetTag("messaging.system", "servicebus");
    activity?.SetTag("messaging.destination.name", "orders-queue");
    activity?.SetTag("messaging.message.id", message.MessageId);
    activity?.SetTag("messaging.operation", "process");
    
    try
    {
        await ProcessMessageContentAsync(message);
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

## Common Scenarios

### 1. Scheduled Task

**2.x:**
```csharp
public async Task RunScheduledTaskAsync()
{
    using var operation = _telemetryClient.StartOperation<RequestTelemetry>("DailyReport");
    
    try
    {
        operation.Telemetry.Properties["TaskType"] = "Scheduled";
        operation.Telemetry.Properties["Schedule"] = "Daily 2 AM";
        
        await GenerateReportAsync();
        
        operation.Telemetry.Success = true;
    }
    catch (Exception ex)
    {
        operation.Telemetry.Success = false;
        _telemetryClient.TrackException(ex);
        throw;
    }
}
```

**3.x:**
```csharp
public async Task RunScheduledTaskAsync()
{
    using var activity = ActivitySource.StartActivity("DailyReport", ActivityKind.Server);
    
    activity?.SetTag("task.type", "Scheduled");
    activity?.SetTag("task.schedule", "Daily 2 AM");
    
    try
    {
        await GenerateReportAsync();
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

### 2. gRPC Service Method

**2.x:**
```csharp
public override async Task<OrderResponse> CreateOrder(OrderRequest request, ServerCallContext context)
{
    using var operation = _telemetryClient.StartOperation<RequestTelemetry>("CreateOrder");
    operation.Telemetry.Properties["Service"] = "OrderService";
    operation.Telemetry.Properties["Method"] = "CreateOrder";
    
    try
    {
        var order = await _orderService.CreateAsync(request);
        operation.Telemetry.Success = true;
        return new OrderResponse { OrderId = order.Id };
    }
    catch (Exception ex)
    {
        operation.Telemetry.Success = false;
        throw;
    }
}
```

**3.x:**
```csharp
public override async Task<OrderResponse> CreateOrder(OrderRequest request, ServerCallContext context)
{
    // gRPC instrumentation is automatic in 3.x
    var activity = Activity.Current;
    activity?.SetTag("order.customer_id", request.CustomerId);
    activity?.SetTag("order.priority", request.Priority);
    
    var order = await _orderService.CreateAsync(request);
    return new OrderResponse { OrderId = order.Id };
}
```

### 3. Timer-Triggered Function

**2.x:**
```csharp
[FunctionName("OrderCleanup")]
public async Task Run([TimerTrigger("0 0 * * *")] TimerInfo timer)
{
    using var operation = _telemetryClient.StartOperation<RequestTelemetry>("OrderCleanup");
    
    try
    {
        var deletedCount = await CleanupOldOrdersAsync();
        operation.Telemetry.Properties["DeletedOrders"] = deletedCount.ToString();
        operation.Telemetry.Success = true;
    }
    catch (Exception ex)
    {
        operation.Telemetry.Success = false;
        throw;
    }
}
```

**3.x:**
```csharp
[FunctionName("OrderCleanup")]
public async Task Run([TimerTrigger("0 0 * * *")] TimerInfo timer)
{
    using var activity = ActivitySource.StartActivity("OrderCleanup", ActivityKind.Server);
    
    try
    {
        var deletedCount = await CleanupOldOrdersAsync();
        activity?.SetTag("orders.deleted_count", deletedCount);
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

## Azure Monitor Queries

### 2.x Query

```kusto
requests
| where name == "ProcessOrders"
| where success == true
| project timestamp, name, duration, customDimensions
```

### 3.x Query

```kusto
requests
| where name == "ProcessOrders"
| where success == true
| project timestamp, name, duration, customDimensions
// Note: customDimensions now contain Activity tags
```

## Migration Checklist

- [ ] Identify all `TrackRequest()` calls
- [ ] For ASP.NET Core HTTP requests, remove manual tracking (automatic in 3.x)
- [ ] For background jobs, migrate to `ActivitySource.StartActivity()` with `ActivityKind.Server`
- [ ] For queue processing, migrate to `ActivitySource` with `ActivityKind.Consumer`
- [ ] Replace custom properties with `Activity.SetTag()`
- [ ] Replace metrics with Meter API
- [ ] Update parent context extraction for distributed tracing (W3C Trace Context)
- [ ] Test correlation in distributed scenarios
- [ ] Update Azure Monitor queries if needed
- [ ] Verify request telemetry appears in Azure Monitor

## See Also

- [TrackDependency-behavior-changed.md](TrackDependency-behavior-changed.md)
- [activity-source.md](../../opentelemetry-fundamentals/activity-source.md)
- [activity-kinds.md](../../opentelemetry-fundamentals/activity-kinds.md)
- [correlation-and-distributed-tracing.md](../../concepts/correlation-and-distributed-tracing.md)
