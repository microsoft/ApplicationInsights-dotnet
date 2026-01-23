# Complete Worker Service Migration Example

**Category:** Complete Migration Example  
**Applies to:** Migrating .NET Worker Service from Application Insights 2.x to 3.x  
**Related:** [activity-source.md](../../opentelemetry-fundamentals/activity-source.md)

## Overview

This example shows migrating a background worker service (hosted service) from Application Insights 2.x to 3.x, including scheduled jobs and message queue processing.

## Before: Application Insights 2.x

### Program.cs (2.x)

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddApplicationInsightsTelemetryWorkerService();
                
                // Custom telemetry
                services.AddSingleton<ITelemetryInitializer, CloudRoleNameInitializer>();
                services.Configure<TelemetryConfiguration>(config =>
                {
                    config.TelemetryProcessorChainBuilder
                        .Use(next => new ErrorsOnlyProcessor(next))
                        .Build();
                });
                
                services.AddHostedService<OrderProcessingWorker>();
                services.AddHostedService<QueueProcessingWorker>();
            });
}
```

### OrderProcessingWorker.cs (2.x)

```csharp
public class OrderProcessingWorker : BackgroundService
{
    private readonly ILogger<OrderProcessingWorker> _logger;
    private readonly TelemetryClient _telemetryClient;
    private readonly IOrderService _orderService;
    
    public OrderProcessingWorker(
        ILogger<OrderProcessingWorker> logger,
        TelemetryClient telemetryClient,
        IOrderService orderService)
    {
        _logger = logger;
        _telemetryClient = telemetryClient;
        _orderService = orderService;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var operation = _telemetryClient.StartOperation<RequestTelemetry>("ProcessOrders");
            
            try
            {
                _logger.LogInformation("Processing orders at: {time}", DateTimeOffset.Now);
                
                var orders = await _orderService.GetPendingOrdersAsync();
                operation.Telemetry.Properties["orderCount"] = orders.Count.ToString();
                
                foreach (var order in orders)
                {
                    await ProcessOrderAsync(order);
                }
                
                operation.Telemetry.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing orders");
                operation.Telemetry.Success = false;
                _telemetryClient.TrackException(ex);
            }
            
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
    
    private async Task ProcessOrderAsync(Order order)
    {
        using var operation = _telemetryClient.StartOperation<DependencyTelemetry>("ProcessSingleOrder");
        operation.Telemetry.Type = "InProc";
        operation.Telemetry.Properties["orderId"] = order.Id.ToString();
        
        try
        {
            await _orderService.ProcessAsync(order);
            operation.Telemetry.Success = true;
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

### QueueProcessingWorker.cs (2.x)

```csharp
public class QueueProcessingWorker : BackgroundService
{
    private readonly ILogger<QueueProcessingWorker> _logger;
    private readonly TelemetryClient _telemetryClient;
    private readonly IQueueClient _queueClient;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _queueClient.ReceiveMessagesAsync(stoppingToken))
        {
            using var operation = _telemetryClient.StartOperation<RequestTelemetry>("ProcessQueueMessage");
            operation.Telemetry.Properties["messageId"] = message.Id;
            
            try
            {
                await ProcessMessageAsync(message);
                await _queueClient.CompleteAsync(message);
                operation.Telemetry.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message {MessageId}", message.Id);
                operation.Telemetry.Success = false;
                _telemetryClient.TrackException(ex);
                await _queueClient.AbandonAsync(message);
            }
        }
    }
}
```

## After: Application Insights 3.x

### Program.cs (3.x)

```csharp
var builder = Host.CreateApplicationBuilder(args);

// Add Application Insights with OpenTelemetry
builder.Services.AddApplicationInsightsTelemetryWorkerService(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
})
.ConfigureOpenTelemetryBuilder(otelBuilder =>
{
    // Configure Resource (Cloud Role Name)
    otelBuilder.ConfigureResource(resource =>
    {
        var roleName = builder.Configuration["ApplicationInsights:CloudRoleName"] ?? "OrderWorker";
        resource.AddService(serviceName: roleName, serviceVersion: "1.0.0");
    });
    
    // Register custom ActivitySource
    otelBuilder.AddSource("OrderWorker.*");
    
    // Add processors
    otelBuilder.AddProcessor(new ErrorsOnlyProcessor());
});

builder.Services.AddHostedService<OrderProcessingWorker>();
builder.Services.AddHostedService<QueueProcessingWorker>();

var host = builder.Build();
host.Run();
```

### OrderProcessingWorker.cs (3.x)

```csharp
public class OrderProcessingWorker : BackgroundService
{
    private static readonly ActivitySource ActivitySource = new("OrderWorker.OrderProcessing");
    
    private readonly ILogger<OrderProcessingWorker> _logger;
    private readonly IOrderService _orderService;
    
    public OrderProcessingWorker(
        ILogger<OrderProcessingWorker> logger,
        IOrderService orderService)
    {
        _logger = logger;
        _orderService = orderService;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Create Activity for batch processing
            using var activity = ActivitySource.StartActivity(
                "ProcessOrders", 
                ActivityKind.Internal);
            
            try
            {
                _logger.LogInformation("Processing orders at: {time}", DateTimeOffset.Now);
                
                var orders = await _orderService.GetPendingOrdersAsync();
                activity?.SetTag("order.count", orders.Count);
                
                foreach (var order in orders)
                {
                    await ProcessOrderAsync(order);
                }
                
                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing orders");
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.RecordException(ex);
            }
            
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
    
    private async Task ProcessOrderAsync(Order order)
    {
        // Child activity automatically linked to parent
        using var activity = ActivitySource.StartActivity(
            "ProcessSingleOrder", 
            ActivityKind.Internal);
        
        activity?.SetTag("order.id", order.Id);
        
        try
        {
            await _orderService.ProcessAsync(order);
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

### QueueProcessingWorker.cs (3.x)

```csharp
public class QueueProcessingWorker : BackgroundService
{
    private static readonly ActivitySource ActivitySource = new("OrderWorker.QueueProcessing");
    
    private readonly ILogger<QueueProcessingWorker> _logger;
    private readonly IQueueClient _queueClient;
    
    public QueueProcessingWorker(
        ILogger<QueueProcessingWorker> logger,
        IQueueClient queueClient)
    {
        _logger = logger;
        _queueClient = queueClient;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _queueClient.ReceiveMessagesAsync(stoppingToken))
        {
            // Restore trace context from message properties
            ActivityContext parentContext = default;
            if (message.Properties.TryGetValue("traceparent", out var traceparent))
            {
                parentContext = ActivityContext.Parse(traceparent, null);
            }
            
            // Create Activity as Consumer with parent context
            using var activity = ActivitySource.StartActivity(
                "ProcessQueueMessage",
                ActivityKind.Consumer,
                parentContext);
            
            activity?.SetTag("message.id", message.Id);
            
            try
            {
                await ProcessMessageAsync(message);
                await _queueClient.CompleteAsync(message);
                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message {MessageId}", message.Id);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.RecordException(ex);
                await _queueClient.AbandonAsync(message);
            }
        }
    }
    
    private async Task ProcessMessageAsync(QueueMessage message)
    {
        using var activity = ActivitySource.StartActivity("ProcessMessageContent");
        
        // Process message
        var data = JsonSerializer.Deserialize<OrderData>(message.Body);
        activity?.SetTag("order.id", data.OrderId);
        
        // ... processing logic ...
    }
}
```

### ErrorsOnlyProcessor.cs (3.x)

```csharp
public class ErrorsOnlyProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Only send errors and slow operations
        if (activity.Status != ActivityStatusCode.Error && 
            activity.Duration.TotalSeconds < 5)
        {
            activity.IsAllDataRequested = false;
            return;
        }
    }
}
```

## Key Differences

### Removed
- `TelemetryClient` - No longer needed
- `StartOperation<T>()` - Replaced by ActivitySource.StartActivity()
- Manual success tracking - Automatic via ActivityStatusCode
- Manual property assignment - Use SetTag()

### Added
- `ActivitySource` - Static per-component
- `ActivityKind` specification - Server/Client/Internal/Producer/Consumer
- Automatic parent-child relationships
- W3C trace context propagation

### Simplified
- No manual telemetry creation
- No operation disposal tracking
- No success/failure manual setting
- Automatic correlation

## Message Queue Correlation

### Sending Side (Producer)

```csharp
public class MessageSender
{
    private static readonly ActivitySource ActivitySource = new("OrderWorker.MessageSender");
    
    public async Task SendOrderMessageAsync(Order order)
    {
        using var activity = ActivitySource.StartActivity(
            "SendOrderMessage", 
            ActivityKind.Producer);
        
        activity?.SetTag("order.id", order.Id);
        
        var message = new QueueMessage
        {
            Body = JsonSerializer.Serialize(order),
            Properties = new Dictionary<string, string>
            {
                // Propagate trace context
                ["traceparent"] = Activity.Current?.Id ?? string.Empty
            }
        };
        
        await _queueClient.SendAsync(message);
    }
}
```

### Receiving Side (Consumer)

```csharp
// Already shown in QueueProcessingWorker.cs above
// Extract traceparent from message.Properties
// Use ActivityContext.Parse() to restore parent
// Create Activity with ActivityKind.Consumer and parent context
```

## Testing

```csharp
[Fact]
public async Task ProcessOrders_CreatesActivity()
{
    var listener = new ActivityListener
    {
        ShouldListenTo = source => source.Name == "OrderWorker.OrderProcessing",
        Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        ActivityStarted = activity => { /* Verify */ }
    };
    
    ActivitySource.AddActivityListener(listener);
    
    var worker = new OrderProcessingWorker(logger, orderService);
    
    // Test execution
    await worker.StartAsync(CancellationToken.None);
    
    // Verify Activity created with correct tags
}
```

## Performance Improvements

**2.x:** Manual telemetry tracking adds overhead
```csharp
// Each operation requires:
// - TelemetryClient injection
// - StartOperation call
// - Manual property setting
// - Success/failure tracking
// - Exception tracking
```

**3.x:** Lightweight Activity creation
```csharp
// Minimal overhead:
// - Static ActivitySource
// - Automatic timing
// - Automatic hierarchy
// - Optional (null-safe) operations
```

## Summary of Benefits

1. **Less Code:** ~50% reduction in telemetry code
2. **Automatic Correlation:** Parent-child relationships automatic
3. **Standard Compliance:** W3C Trace Context
4. **Better Performance:** Reduced allocations and overhead
5. **Cleaner Code:** No TelemetryClient dependencies

## See Also

- [activity-source.md](../../opentelemetry-fundamentals/activity-source.md)
- [correlation-and-distributed-tracing.md](../../common-scenarios/correlation-and-distributed-tracing.md)
- [activity-processor.md](../../concepts/activity-processor.md)
