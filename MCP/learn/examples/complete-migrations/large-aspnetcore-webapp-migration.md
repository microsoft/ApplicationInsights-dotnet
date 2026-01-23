# Large ASP.NET Core Web Application Migration

**Category:** Complete Migration Example  
**Applies to:** Large-scale ASP.NET Core applications with multiple custom components  
**Related:** [worker-service-migration.md](worker-service-migration.md), [console-app-migration.md](console-app-migration.md)

## Overview

This example demonstrates migrating a complete enterprise ASP.NET Core web application with custom initializers, processors, middleware, and background services from Application Insights 2.x to 3.x.

## Application Structure

```
LargeWebApp/
├─ Program.cs
├─ Startup.cs (2.x) or Program.cs (3.x)
├─ appsettings.json
├─ Telemetry/
│  ├─ CloudRoleNameInitializer.cs
│  ├─ UserContextInitializer.cs
│  ├─ TenantContextInitializer.cs
│  ├─ HealthCheckFilter.cs
│  ├─ SyntheticTrafficFilter.cs
│  └─ PerformanceMonitoringProcessor.cs
├─ Middleware/
│  ├─ RequestLoggingMiddleware.cs
│  └─ TenantResolutionMiddleware.cs
├─ Services/
│  ├─ OrderService.cs
│  ├─ PaymentService.cs
│  └─ NotificationService.cs
└─ BackgroundServices/
   ├─ DataSyncWorker.cs
   └─ ReportGenerationWorker.cs
```

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
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}
```

### Startup.cs (2.x)

```csharp
public class Startup
{
    public IConfiguration Configuration { get; }
    
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }
    
    public void ConfigureServices(IServiceCollection services)
    {
        // Add Application Insights
        services.AddApplicationInsightsTelemetry(Configuration);
        
        // Register custom telemetry initializers
        services.AddSingleton<ITelemetryInitializer, CloudRoleNameInitializer>();
        services.AddSingleton<ITelemetryInitializer, UserContextInitializer>();
        services.AddSingleton<ITelemetryInitializer, TenantContextInitializer>();
        
        // Configure telemetry processors
        services.Configure<TelemetryConfiguration>(config =>
        {
            config.TelemetryProcessorChainBuilder
                .Use(next => new HealthCheckFilter(next))
                .Use(next => new SyntheticTrafficFilter(next))
                .Use(next => new PerformanceMonitoringProcessor(next))
                .UseAdaptiveSampling(maxTelemetryItemsPerSecond: 5)
                .Build();
        });
        
        // Application services
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<INotificationService, NotificationService>();
        
        // Background services
        services.AddHostedService<DataSyncWorker>();
        services.AddHostedService<ReportGenerationWorker>();
        
        services.AddControllers();
        services.AddHttpContextAccessor();
    }
    
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        
        app.UseHttpsRedirection();
        app.UseRouting();
        
        // Custom middleware
        app.UseMiddleware<TenantResolutionMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();
        
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
```

### appsettings.json (2.x)

```json
{
  "ApplicationInsights": {
    "InstrumentationKey": "12345678-1234-1234-1234-123456789012"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

### CloudRoleNameInitializer.cs (2.x)

```csharp
public class CloudRoleNameInitializer : ITelemetryInitializer
{
    private readonly IConfiguration _configuration;
    
    public CloudRoleNameInitializer(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.Cloud.RoleName = _configuration["ServiceName"] ?? "LargeWebApp";
        telemetry.Context.Cloud.RoleInstance = Environment.MachineName;
    }
}
```

### UserContextInitializer.cs (2.x)

```csharp
public class UserContextInitializer : ITelemetryInitializer
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public UserContextInitializer(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public void Initialize(ITelemetry telemetry)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            telemetry.Context.User.Id = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            telemetry.Context.User.AuthenticatedUserId = httpContext.User.FindFirst(ClaimTypes.Email)?.Value;
            
            if (telemetry is ISupportProperties props)
            {
                props.Properties["UserRoles"] = string.Join(",", GetUserRoles(httpContext.User));
            }
        }
    }
    
    private IEnumerable<string> GetUserRoles(ClaimsPrincipal user)
    {
        return user.FindAll(ClaimTypes.Role).Select(c => c.Value);
    }
}
```

### HealthCheckFilter.cs (2.x)

```csharp
public class HealthCheckFilter : ITelemetryProcessor
{
    private ITelemetryProcessor Next { get; set; }
    
    public HealthCheckFilter(ITelemetryProcessor next)
    {
        Next = next;
    }
    
    public void Process(ITelemetry item)
    {
        if (item is RequestTelemetry request)
        {
            if (request.Url.AbsolutePath.StartsWith("/health") ||
                request.Url.AbsolutePath.StartsWith("/ready"))
            {
                return; // Don't send health check requests
            }
        }
        
        Next.Process(item);
    }
}
```

### OrderService.cs (2.x)

```csharp
public class OrderService : IOrderService
{
    private readonly TelemetryClient _telemetryClient;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<OrderService> _logger;
    
    public async Task<Order> CreateOrderAsync(Order order)
    {
        using var operation = _telemetryClient.StartOperation<RequestTelemetry>("CreateOrder");
        
        try
        {
            operation.Telemetry.Properties["orderId"] = order.Id.ToString();
            operation.Telemetry.Properties["customerId"] = order.CustomerId.ToString();
            operation.Telemetry.Metrics["orderAmount"] = order.TotalAmount;
            
            // Validate
            await ValidateOrderAsync(order);
            
            // Process payment
            var payment = await _paymentService.ProcessPaymentAsync(order);
            
            // Save order
            await SaveOrderAsync(order);
            
            _telemetryClient.TrackEvent("OrderCreated", new Dictionary<string, string>
            {
                ["OrderId"] = order.Id.ToString(),
                ["Amount"] = order.TotalAmount.ToString()
            });
            
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

## After: Application Insights 3.x

### Program.cs (3.x)

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Application Insights with OpenTelemetry
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
})
.ConfigureOpenTelemetryBuilder(otelBuilder =>
{
    // Configure Resource (Cloud Role Name)
    otelBuilder.ConfigureResource(resource =>
    {
        var serviceName = builder.Configuration["ServiceName"] ?? "LargeWebApp";
        var instanceName = Environment.MachineName;
        resource.AddService(serviceName, serviceInstance: instanceName, serviceVersion: "2.0.0");
    });
    
    // Register custom ActivitySources
    otelBuilder.AddSource("LargeWebApp.Orders");
    otelBuilder.AddSource("LargeWebApp.Payments");
    
    // Add processors (replaces initializers and processors)
    otelBuilder.AddProcessor<UserContextProcessor>();
    otelBuilder.AddProcessor<TenantContextProcessor>();
    otelBuilder.AddProcessor<HealthCheckFilter>();
    otelBuilder.AddProcessor<SyntheticTrafficFilter>();
    otelBuilder.AddProcessor<PerformanceMonitoringProcessor>();
    
    // Sampling
    otelBuilder.SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(0.1)));
});

// Application services
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Background services
builder.Services.AddHostedService<DataSyncWorker>();
builder.Services.AddHostedService<ReportGenerationWorker>();

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseRouting();

// Custom middleware
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseAuthorization();
app.MapControllers();

app.Run();
```

### appsettings.json (3.x)

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=12345678-1234-1234-1234-123456789012;IngestionEndpoint=https://..."
  },
  "ServiceName": "LargeWebApp",
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

### UserContextProcessor.cs (3.x)

```csharp
public class UserContextProcessor : BaseProcessor<Activity>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public UserContextProcessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public override void OnStart(Activity activity)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userEmail = httpContext.User.FindFirst(ClaimTypes.Email)?.Value;
            
            activity.SetTag("enduser.id", userId);
            activity.SetTag("enduser.email", userEmail);
            activity.SetTag("user.roles", string.Join(",", GetUserRoles(httpContext.User)));
        }
    }
    
    private IEnumerable<string> GetUserRoles(ClaimsPrincipal user)
    {
        return user.FindAll(ClaimTypes.Role).Select(c => c.Value);
    }
}
```

### TenantContextProcessor.cs (3.x)

```csharp
public class TenantContextProcessor : BaseProcessor<Activity>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public TenantContextProcessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public override void OnStart(Activity activity)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null && httpContext.Items.TryGetValue("TenantId", out var tenantId))
        {
            activity.SetTag("tenant.id", tenantId?.ToString());
            
            if (httpContext.Items.TryGetValue("TenantName", out var tenantName))
            {
                activity.SetTag("tenant.name", tenantName?.ToString());
            }
        }
    }
}
```

### HealthCheckFilter.cs (3.x)

```csharp
public class HealthCheckFilter : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (activity.Kind == ActivityKind.Server)
        {
            var path = activity.GetTagItem("url.path") as string;
            
            if (path != null && (path.StartsWith("/health") || path.StartsWith("/ready")))
            {
                activity.IsAllDataRequested = false;
                return;
            }
        }
    }
}
```

### SyntheticTrafficFilter.cs (3.x)

```csharp
public class SyntheticTrafficFilter : BaseProcessor<Activity>
{
    private static readonly HashSet<string> SyntheticUserAgents = new(StringComparer.OrdinalIgnoreCase)
    {
        "AlwaysOn",
        "AppInsightsMonitoring",
        "HealthCheck"
    };
    
    public override void OnEnd(Activity activity)
    {
        if (activity.Kind == ActivityKind.Server)
        {
            var userAgent = activity.GetTagItem("user_agent.original") as string;
            
            if (userAgent != null && SyntheticUserAgents.Any(ua => userAgent.Contains(ua)))
            {
                activity.IsAllDataRequested = false;
                return;
            }
        }
    }
}
```

### PerformanceMonitoringProcessor.cs (3.x)

```csharp
public class PerformanceMonitoringProcessor : BaseProcessor<Activity>
{
    private readonly ILogger<PerformanceMonitoringProcessor> _logger;
    
    public PerformanceMonitoringProcessor(ILogger<PerformanceMonitoringProcessor> logger)
    {
        _logger = logger;
    }
    
    public override void OnEnd(Activity activity)
    {
        if (activity.Kind == ActivityKind.Server)
        {
            var durationMs = activity.Duration.TotalMilliseconds;
            
            // Categorize performance
            if (durationMs < 100)
                activity.SetTag("performance.category", "fast");
            else if (durationMs < 1000)
                activity.SetTag("performance.category", "normal");
            else if (durationMs < 5000)
                activity.SetTag("performance.category", "slow");
            else
            {
                activity.SetTag("performance.category", "very_slow");
                
                // Log slow requests
                _logger.LogWarning(
                    "Slow request detected: {Path} took {Duration}ms",
                    activity.GetTagItem("url.path"),
                    durationMs);
            }
        }
    }
}
```

### OrderService.cs (3.x)

```csharp
public class OrderService : IOrderService
{
    private static readonly ActivitySource ActivitySource = new("LargeWebApp.Orders");
    private static readonly Meter Meter = new("LargeWebApp.Orders");
    private static readonly Counter<long> OrdersCreated = Meter.CreateCounter<long>("orders.created");
    private static readonly Histogram<double> OrderAmount = Meter.CreateHistogram<double>("order.amount", unit: "USD");
    
    private readonly IPaymentService _paymentService;
    private readonly ILogger<OrderService> _logger;
    
    public async Task<Order> CreateOrderAsync(Order order)
    {
        using var activity = ActivitySource.StartActivity("CreateOrder");
        
        try
        {
            activity?.SetTag("order.id", order.Id);
            activity?.SetTag("customer.id", order.CustomerId);
            
            // Validate
            await ValidateOrderAsync(order);
            
            // Process payment
            var payment = await _paymentService.ProcessPaymentAsync(order);
            
            // Save order
            await SaveOrderAsync(order);
            
            // Record metrics
            OrdersCreated.Add(1);
            OrderAmount.Record(order.TotalAmount);
            
            // Log event
            _logger.LogInformation(
                "OrderCreated: OrderId={OrderId}, Amount={Amount}",
                order.Id,
                order.TotalAmount);
            
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

## Key Migration Points

### 1. Startup Pattern Change

**2.x:** Separate `Program.cs` and `Startup.cs`  
**3.x:** Minimal hosting model in `Program.cs`

### 2. Configuration Consolidation

**2.x:** Split across Startup.ConfigureServices  
**3.x:** All in `ConfigureOpenTelemetryBuilder`

### 3. Initializers → Processors

All `ITelemetryInitializer` classes converted to `BaseProcessor<Activity>` with `OnStart()` override.

### 4. Processors → Processors

All `ITelemetryProcessor` classes converted to `BaseProcessor<Activity>` with `OnEnd()` override.

### 5. Custom Instrumentation

**2.x:** `TelemetryClient.StartOperation<T>()`  
**3.x:** `ActivitySource.StartActivity()`

### 6. Metrics

**2.x:** `TelemetryClient.TrackMetric()`, `TrackEvent()`  
**3.x:** OpenTelemetry `Meter` API

### 7. No More Chaining Parameter

**2.x:** Processors receive `next` parameter  
**3.x:** Framework handles pipeline automatically

## Performance Improvements

**2.x Total Lines:** ~500 lines  
**3.x Total Lines:** ~450 lines (-10%)

**Benefits:**
- Reduced allocations (Activity vs telemetry objects)
- Automatic instrumentation (less code)
- Better sampling (distributed tracing aware)
- Simplified DI (no TelemetryClient everywhere)

## Testing Strategy

```csharp
[Fact]
public async Task CreateOrder_CreatesActivityWithTags()
{
    var listener = new ActivityListener
    {
        ShouldListenTo = source => source.Name == "LargeWebApp.Orders",
        Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
    };
    
    ActivitySource.AddActivityListener(listener);
    
    var service = new OrderService(paymentService, logger);
    await service.CreateOrderAsync(testOrder);
    
    // Verify activity was created with expected tags
}
```

## See Also

- [worker-service-migration.md](worker-service-migration.md)
- [console-app-migration.md](console-app-migration.md)
- [activity-source.md](../../opentelemetry-fundamentals/activity-source.md)
