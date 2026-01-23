# Flush Behavior Changed

**Breaking Change Type:** Behavior Change  
**Applies to:** TelemetryClient.Flush() → MeterProvider/TracerProvider.ForceFlush()  
**Related:** [flush-removed.md](Flush-removed.md)

## Summary

The behavior of flushing telemetry has changed significantly in 3.x. Instead of `TelemetryClient.Flush()`, you must use `MeterProvider.ForceFlush()` and `TracerProvider.ForceFlush()`, with different timing guarantees and lifecycle management.

## Behavior Differences

### Flush API

**2.x Behavior:**
```csharp
// Single method flushes all telemetry types
_telemetryClient.Flush();

// Wait for flush to complete (recommended)
_telemetryClient.Flush();
await Task.Delay(TimeSpan.FromSeconds(5));
```

**3.x Behavior:**
```csharp
// Must flush both providers separately
var meterProvider = app.Services.GetRequiredService<MeterProvider>();
var tracerProvider = app.Services.GetRequiredService<TracerProvider>();

meterProvider.ForceFlush();
tracerProvider.ForceFlush();

// ForceFlush() blocks until complete (no delay needed)
```

**Impact:** Must obtain and flush both providers. Flush is synchronous and blocking.

### Timing Guarantees

**2.x Behavior:**
```csharp
// Flush() returns immediately
// Actual transmission happens asynchronously
// No guarantee when data is sent
_telemetryClient.Flush();
// Data may not be sent yet!
```

**3.x Behavior:**
```csharp
// ForceFlush() blocks until:
// 1. All pending data exported
// 2. Or timeout reached (default 30 seconds)
tracerProvider.ForceFlush(timeout: TimeSpan.FromSeconds(10));
// Data guaranteed sent or timeout reached
```

**Impact:** ForceFlush() provides stronger guarantees but blocks the calling thread.

### Timeout Handling

**2.x Behavior:**
```csharp
// No timeout parameter
// Must manually wait
_telemetryClient.Flush();
await Task.Delay(TimeSpan.FromSeconds(5)); // Hope it's enough
```

**3.x Behavior:**
```csharp
// Built-in timeout
bool success = tracerProvider.ForceFlush(timeout: TimeSpan.FromSeconds(5));
if (!success)
{
    _logger.LogWarning("Flush timed out");
}
```

**Impact:** Better timeout control, can detect flush failures.

### Application Shutdown

**2.x Behavior:**
```csharp
public class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        
        try
        {
            await host.RunAsync();
        }
        finally
        {
            // Flush on shutdown
            var telemetryClient = host.Services.GetService<TelemetryClient>();
            telemetryClient?.Flush();
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}
```

**3.x Behavior:**
```csharp
var builder = WebApplication.CreateBuilder(args);

// Register shutdown handler
builder.Services.AddHostedService<TelemetryFlushService>();

// Or use application lifetime events
var app = builder.Build();

app.Lifetime.ApplicationStopping.Register(() =>
{
    var meterProvider = app.Services.GetService<MeterProvider>();
    var tracerProvider = app.Services.GetService<TracerProvider>();
    
    meterProvider?.ForceFlush(TimeSpan.FromSeconds(5));
    tracerProvider?.ForceFlush(TimeSpan.FromSeconds(5));
});

app.Run();
```

**Impact:** Must explicitly register shutdown handlers or use HostedService.

## Common Migration Patterns

### Pattern 1: Console Application Flush

**2.x:**
```csharp
class Program
{
    static async Task Main(string[] args)
    {
        var config = TelemetryConfiguration.CreateDefault();
        config.InstrumentationKey = "your-key";
        var telemetryClient = new TelemetryClient(config);
        
        telemetryClient.TrackEvent("AppStarted");
        telemetryClient.TrackTrace("Processing data...");
        
        // Process data
        await ProcessDataAsync();
        
        // Flush before exit
        telemetryClient.Flush();
        await Task.Delay(TimeSpan.FromSeconds(5));
        
        Console.WriteLine("Done");
    }
}
```

**3.x:**
```csharp
class Program
{
    static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        
        builder.Services.AddApplicationInsightsTelemetry(options =>
        {
            options.ConnectionString = "your-connection-string";
        })
        .ConfigureOpenTelemetryBuilder(otel =>
        {
            otel.AddSource("MyApp");
        });
        
        using var host = builder.Build();
        
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("AppStarted");
        logger.LogInformation("Processing data...");
        
        // Process data
        await ProcessDataAsync();
        
        // Flush before exit - get providers
        var meterProvider = host.Services.GetService<MeterProvider>();
        var tracerProvider = host.Services.GetService<TracerProvider>();
        
        // ForceFlush blocks until complete
        meterProvider?.ForceFlush(TimeSpan.FromSeconds(5));
        tracerProvider?.ForceFlush(TimeSpan.FromSeconds(5));
        
        Console.WriteLine("Done");
    }
}
```

### Pattern 2: ASP.NET Core Application Shutdown

**2.x:**
```csharp
public class Startup
{
    public void Configure(IApplicationBuilder app, IApplicationLifetime lifetime)
    {
        lifetime.ApplicationStopping.Register(() =>
        {
            var telemetryClient = app.ApplicationServices.GetService<TelemetryClient>();
            telemetryClient?.Flush();
            Task.Delay(TimeSpan.FromSeconds(5)).Wait();
        });
    }
}
```

**3.x:**
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        // Configure OpenTelemetry
    });

var app = builder.Build();

app.Lifetime.ApplicationStopping.Register(() =>
{
    var meterProvider = app.Services.GetService<MeterProvider>();
    var tracerProvider = app.Services.GetService<TracerProvider>();
    
    meterProvider?.ForceFlush(TimeSpan.FromSeconds(5));
    tracerProvider?.ForceFlush(TimeSpan.FromSeconds(5));
});

app.Run();
```

### Pattern 3: Background Service with Flush

**2.x:**
```csharp
public class DataProcessorService : BackgroundService
{
    private readonly TelemetryClient _telemetryClient;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessBatchAsync();
            
            // Flush after each batch
            _telemetryClient.Flush();
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
        
        // Final flush on shutdown
        _telemetryClient.Flush();
        await Task.Delay(TimeSpan.FromSeconds(5));
    }
}
```

**3.x:**
```csharp
public class DataProcessorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataProcessorService> _logger;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessBatchAsync();
            
            // Flush after each batch
            FlushTelemetry(TimeSpan.FromSeconds(2));
            
            await Task.Delay(TimeSpan.FromSeconds(100), stoppingToken);
        }
        
        // Final flush on shutdown
        FlushTelemetry(TimeSpan.FromSeconds(5));
    }
    
    private void FlushTelemetry(TimeSpan timeout)
    {
        try
        {
            var meterProvider = _serviceProvider.GetService<MeterProvider>();
            var tracerProvider = _serviceProvider.GetService<TracerProvider>();
            
            var meterSuccess = meterProvider?.ForceFlush(timeout) ?? true;
            var tracerSuccess = tracerProvider?.ForceFlush(timeout) ?? true;
            
            if (!meterSuccess || !tracerSuccess)
            {
                _logger.LogWarning("Telemetry flush timed out");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flushing telemetry");
        }
    }
}
```

### Pattern 4: Unit Test Flush

**2.x:**
```csharp
[TestMethod]
public async Task TestOperation()
{
    var config = TelemetryConfiguration.CreateDefault();
    config.InstrumentationKey = "test-key";
    var telemetryClient = new TelemetryClient(config);
    
    // Perform operation
    telemetryClient.TrackEvent("TestEvent");
    
    // Flush for test
    telemetryClient.Flush();
    await Task.Delay(TimeSpan.FromSeconds(2));
    
    // Assertions...
}
```

**3.x:**
```csharp
[TestMethod]
public void TestOperation()
{
    var services = new ServiceCollection();
    
    services.AddLogging();
    services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = "test-connection-string";
    })
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        otel.AddSource("MyApp.Tests");
    });
    
    using var serviceProvider = services.BuildServiceProvider();
    var logger = serviceProvider.GetRequiredService<ILogger<MyTest>>();
    
    // Perform operation
    logger.LogInformation("TestEvent");
    
    // Flush for test
    var meterProvider = serviceProvider.GetService<MeterProvider>();
    var tracerProvider = serviceProvider.GetService<TracerProvider>();
    
    meterProvider?.ForceFlush(TimeSpan.FromSeconds(2));
    tracerProvider?.ForceFlush(TimeSpan.FromSeconds(2));
    
    // Assertions...
}
```

### Pattern 5: Hosted Service for Automatic Flush

**3.x Best Practice:**
```csharp
public class TelemetryFlushService : IHostedService
{
    private readonly MeterProvider _meterProvider;
    private readonly TracerProvider _tracerProvider;
    private readonly ILogger<TelemetryFlushService> _logger;
    
    public TelemetryFlushService(
        IServiceProvider serviceProvider,
        ILogger<TelemetryFlushService> logger)
    {
        _meterProvider = serviceProvider.GetService<MeterProvider>();
        _tracerProvider = serviceProvider.GetService<TracerProvider>();
        _logger = logger;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Telemetry flush service started");
        return Task.CompletedTask;
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Flushing telemetry on shutdown...");
        
        var timeout = TimeSpan.FromSeconds(5);
        
        var meterSuccess = _meterProvider?.ForceFlush(timeout) ?? true;
        var tracerSuccess = _tracerProvider?.ForceFlush(timeout) ?? true;
        
        if (meterSuccess && tracerSuccess)
        {
            _logger.LogInformation("Telemetry flushed successfully");
        }
        else
        {
            _logger.LogWarning("Telemetry flush timed out");
        }
        
        return Task.CompletedTask;
    }
}

// Register in Program.cs
builder.Services.AddHostedService<TelemetryFlushService>();
```

## Flush vs Dispose

### 2.x Behavior

```csharp
// Dispose() calls Flush() automatically
using (var config = TelemetryConfiguration.CreateDefault())
using (var client = new TelemetryClient(config))
{
    client.TrackEvent("Event");
    // Flush happens automatically on Dispose
}
```

### 3.x Behavior

```csharp
// Dispose() does NOT automatically flush!
using var serviceProvider = services.BuildServiceProvider();
var meterProvider = serviceProvider.GetService<MeterProvider>();

// Must explicitly flush before dispose
meterProvider?.ForceFlush(TimeSpan.FromSeconds(5));
meterProvider?.Dispose(); // Dispose after flush

// Or use shutdown which flushes then disposes
meterProvider?.Shutdown(TimeSpan.FromSeconds(5));
```

**Impact:** Must explicitly call `ForceFlush()` or `Shutdown()` before disposal.

## Shutdown vs ForceFlush

### ForceFlush

```csharp
// ForceFlush: Export pending data, continue accepting new data
tracerProvider.ForceFlush(TimeSpan.FromSeconds(5));
// Can still record new activities after flush
```

### Shutdown

```csharp
// Shutdown: Flush AND stop accepting new data
tracerProvider.Shutdown(TimeSpan.FromSeconds(5));
// Cannot record new activities after shutdown
```

**When to use:**
- **ForceFlush:** Periodic flushing, checkpoints, batch processing
- **Shutdown:** Application exit, test cleanup, final flush

## Performance Considerations

| Aspect | 2.x Flush | 3.x ForceFlush |
|---|---|---|
| Blocking | No (async) | Yes (synchronous) |
| Guarantees | Weak (hope it sends) | Strong (blocks until sent) |
| Timeout | Manual delay | Built-in parameter |
| Thread Safety | Not guaranteed | Thread-safe |
| Default Timeout | N/A (must delay) | 30 seconds |

**Recommendations:**
- Don't call `ForceFlush()` on hot paths (blocking!)
- Use background tasks for periodic flushing
- Set reasonable timeouts (5-10 seconds)
- Use `Shutdown()` for final flush before exit

## Common Issues

### Issue 1: Missing Flush on Shutdown

**Problem:**
```csharp
var app = builder.Build();
app.Run();
// No flush - last telemetry lost!
```

**Solution:**
```csharp
var app = builder.Build();

app.Lifetime.ApplicationStopping.Register(() =>
{
    var meterProvider = app.Services.GetService<MeterProvider>();
    var tracerProvider = app.Services.GetService<TracerProvider>();
    
    meterProvider?.ForceFlush(TimeSpan.FromSeconds(5));
    tracerProvider?.ForceFlush(TimeSpan.FromSeconds(5));
});

app.Run();
```

### Issue 2: Blocking Main Thread

**Problem:**
```csharp
// Flushing on every request - bad!
[HttpPost]
public IActionResult ProcessOrder([FromBody] Order order)
{
    // ... process order ...
    
    tracerProvider.ForceFlush(); // Blocks request!
    return Ok();
}
```

**Solution:**
```csharp
// Don't flush on hot paths
// Let automatic batching handle it
[HttpPost]
public IActionResult ProcessOrder([FromBody] Order order)
{
    // ... process order ...
    return Ok();
    // Telemetry sent automatically via batch processor
}
```

### Issue 3: Provider Not Available

**Problem:**
```csharp
// Providers not registered in DI
var tracerProvider = app.Services.GetService<TracerProvider>();
// tracerProvider is null!
```

**Solution:**
```csharp
// Ensure providers are registered
builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        // This registers both providers in DI
    });
```

## Migration Checklist

- [ ] Replace `TelemetryClient.Flush()` with provider flush calls
- [ ] Obtain `MeterProvider` and `TracerProvider` from DI
- [ ] Call `ForceFlush()` on both providers
- [ ] Add shutdown handlers for application exit
- [ ] Remove manual `Task.Delay()` after flush
- [ ] Set appropriate flush timeouts (5-10 seconds)
- [ ] Use `Shutdown()` instead of `Dispose()` for final cleanup
- [ ] Consider using HostedService for automatic flush
- [ ] Test that telemetry is sent before application exit
- [ ] Don't flush on hot paths (performance impact)

## See Also

- [Flush-removed.md](Flush-removed.md)
- [console-app-migration.md](../../examples/complete-migrations/console-app-migration.md)
- [resource-detectors.md](../../concepts/resource-detectors.md)
