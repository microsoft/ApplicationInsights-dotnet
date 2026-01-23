# Large ASP.NET Core Web Application Migration

**Category:** Complete Migration Example  
**Applies to:** Migrating large ASP.NET Core applications with extensive telemetry  
**Related:** [worker-service-migration.md](worker-service-migration.md), [console-app-migration.md](console-app-migration.md)

## Overview

This example demonstrates migrating a production ASP.NET Core web application with multiple custom initializers, processors, and extensive instrumentation.

## Application Architecture (Before)

```
MyWebApp/
├── Program.cs
├── Startup.cs
├── appsettings.json
├── applicationinsights.config
├── Telemetry/
│   ├── CustomInitializers/
│   │   ├── CloudRoleNameInitializer.cs
│   │   ├── UserContextInitializer.cs
│   │   └── EnvironmentInitializer.cs
│   └── CustomProcessors/
│       ├── FilterHealthChecksProcessor.cs
│       ├── SamplingProcessor.cs
│       └── EnrichmentProcessor.cs
├── Controllers/
│   ├── OrdersController.cs
│   └── CustomersController.cs
└── Services/
    ├── OrderService.cs
    └── PaymentService.cs
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
        services.AddSingleton<ITelemetryInitializer, EnvironmentInitializer>();
        
        // Configure telemetry processors
        services.Configure<TelemetryConfiguration>(config =>
        {
            config.TelemetryProcessorChainBuilder
                .Use(next => new FilterHealthChecksProcessor(next))
                .Use(next => new EnrichmentProcessor(next))
                .UseAdaptiveSampling(maxTelemetryItemsPerSecond: 5)
                .Build();
        });
        
        services.AddControllers();
        services.AddHttpContextAccessor();
        
        // Business services
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPaymentService, PaymentService>();
    }
    
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        
        app.UseRouting();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapHealthChecks("/health");
        });
    }
}
```

### CloudRoleNameInitializer.cs (2.x)

```csharp
public class CloudRoleNameInitializer : ITelemetryInitializer
{
    private readonly string _roleName;
    
    public CloudRoleNameInitializer(IConfiguration configuration)
    {
        _roleName = configuration["ApplicationInsights:RoleName"] ?? "MyWebApp";
    }
    
    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.Cloud.RoleName = _roleName;
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
            telemetry.Context.User.AuthenticatedUserId = httpContext.User.Identity.Name;
            
            if (telemetry is ISupportProperties properties)
            {
                properties.Properties["UserEmail"] = httpContext.User.FindFirst(ClaimTypes.Email)?.Value;
                properties.Properties["UserRoles"] = string.Join(",", GetUserRoles(httpContext.User));
            }
        }
    }
    
    private IEnumerable<string> GetUserRoles(ClaimsPrincipal user) =>
        user.FindAll(ClaimTypes.Role).Select(c => c.Value);
}
```

### FilterHealthChecksProcessor.cs (2.x)

```csharp
public class FilterHealthChecksProcessor : ITelemetryProcessor
{
    private ITelemetryProcessor Next { get; set; }
    
    public FilterHealthChecksProcessor(ITelemetryProcessor next)
    {
        Next = next;
    }
    
    public void Process(ITelemetry item)
    {
        if (item is RequestTelemetry request)
        {
            if (request.Url.AbsolutePath == "/health" || 
                request.Url.AbsolutePath == "/healthz")
            {
                return; // Don't send health checks
            }
        }
        
        Next.Process(item);
    }
}
```

### OrdersController.cs (2.x)

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly TelemetryClient _telemetryClient;
    private readonly IOrderService _orderService;
    
    public OrdersController(TelemetryClient telemetryClient, IOrderService orderService)
    {
        _telemetryClient = telemetryClient;
        _orderService = orderService;
    }
    
    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        using var operation = _telemetryClient.StartOperation<RequestTelemetry>("CreateOrder");
        operation.Telemetry.Properties["customerId"] = request.CustomerId.ToString();
        
        try
        {
            var order = await _orderService.CreateOrderAsync(request);
            
            _telemetryClient.TrackEvent("OrderCreated", new Dictionary<string, string>
            {
                ["OrderId"] = order.Id.ToString(),
                ["TotalAmount"] = order.TotalAmount.ToString()
            });
            
            operation.Telemetry.Success = true;
            return Ok(order);
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

### OrderService.cs (2.x)

```csharp
public class OrderService : IOrderService
{
    private readonly TelemetryClient _telemetryClient;
    private readonly IPaymentService _paymentService;
    
    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        using var operation = _telemetryClient.StartOperation<DependencyTelemetry>("ValidateOrder");
        operation.Telemetry.Type = "InProc";
        
        try
        {
            await ValidateOrderAsync(request);
            operation.Telemetry.Success = true;
        }
        catch
        {
            operation.Telemetry.Success = false;
            throw;
        }
        
        var order = new Order { /* ... */ };
        await _paymentService.ProcessPaymentAsync(order);
        
        return order;
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
        var roleName = builder.Configuration["ApplicationInsights:RoleName"] ?? "MyWebApp";
        resource.AddService(
            serviceName: roleName,
            serviceVersion: "1.0.0",
            serviceInstanceId: Environment.MachineName);
    });
    
    // Register ActivitySources
    otelBuilder.AddSource("MyWebApp.Orders");
    otelBuilder.AddSource("MyWebApp.Payment");
    
    // Add processors (replaces initializers and processors)
    otelBuilder.AddProcessor<UserContextProcessor>();
    otelBuilder.AddProcessor<EnvironmentProcessor>();
    otelBuilder.AddProcessor<FilterHealthChecksProcessor>();
    otelBuilder.AddProcessor<EnrichmentProcessor>();
    
    // Sampling (replaces adaptive sampling)
    otelBuilder.SetSampler(new TraceIdRatioBasedSampler(0.1)); // 10% sampling
});

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

// Business services
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
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
            activity.SetTag("enduser.id", 
                httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            activity.SetTag("enduser.name", 
                httpContext.User.Identity.Name);
            activity.SetTag("user.email", 
                httpContext.User.FindFirst(ClaimTypes.Email)?.Value);
            activity.SetTag("user.roles", 
                string.Join(",", GetUserRoles(httpContext.User)));
        }
    }
    
    private IEnumerable<string> GetUserRoles(ClaimsPrincipal user) =>
        user.FindAll(ClaimTypes.Role).Select(c => c.Value);
}
```

### EnvironmentProcessor.cs (3.x)

```csharp
public class EnvironmentProcessor : BaseProcessor<Activity>
{
    private readonly string _environment;
    private readonly string _version;
    
    public EnvironmentProcessor(IWebHostEnvironment env, IConfiguration configuration)
    {
        _environment = env.EnvironmentName;
        _version = configuration["App:Version"] ?? "1.0.0";
    }
    
    public override void OnStart(Activity activity)
    {
        activity.SetTag("deployment.environment", _environment);
        activity.SetTag("service.version", _version);
    }
}
```

### FilterHealthChecksProcessor.cs (3.x)

```csharp
public class FilterHealthChecksProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (activity.Kind == ActivityKind.Server)
        {
            var path = activity.GetTagItem("url.path") as string;
            if (path == "/health" || path == "/healthz")
            {
                activity.IsAllDataRequested = false;
                return;
            }
        }
    }
}
```

### EnrichmentProcessor.cs (3.x)

```csharp
public class EnrichmentProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        // Add common enrichment for all activities
        activity.SetTag("app.name", "MyWebApp");
        activity.SetTag("machine.name", Environment.MachineName);
    }
}
```

### OrdersController.cs (3.x)

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private static readonly ActivitySource ActivitySource = new("MyWebApp.Orders");
    private readonly ILogger<OrdersController> _logger;
    private readonly IOrderService _orderService;
    
    public OrdersController(ILogger<OrdersController> logger, IOrderService orderService)
    {
        _logger = logger;
        _orderService = orderService;
    }
    
    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        using var activity = ActivitySource.StartActivity("CreateOrder");
        activity?.SetTag("customer.id", request.CustomerId);
        
        try
        {
            var order = await _orderService.CreateOrderAsync(request);
            
            // Use structured logging instead of TrackEvent
            _logger.LogInformation("OrderCreated: OrderId={OrderId}, TotalAmount={TotalAmount}",
                order.Id, order.TotalAmount);
            
            activity?.SetStatus(ActivityStatusCode.Ok);
            return Ok(order);
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

### OrderService.cs (3.x)

```csharp
public class OrderService : IOrderService
{
    private static readonly ActivitySource ActivitySource = new("MyWebApp.Orders");
    private readonly IPaymentService _paymentService;
    
    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        using var activity = ActivitySource.StartActivity("ValidateOrder");
        
        try
        {
            await ValidateOrderAsync(request);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
        
        var order = new Order { /* ... */ };
        await _paymentService.ProcessPaymentAsync(order);
        
        return order;
    }
}
```

### appsettings.json (3.x)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=...;IngestionEndpoint=https://...",
    "RoleName": "MyWebApp",
    "EnableAdaptiveSampling": false
  },
  "App": {
    "Version": "1.0.0"
  }
}
```

## Key Migration Changes

### 1. Startup → Program.cs

**2.x:** Separate Startup class  
**3.x:** Minimal hosting model in Program.cs

### 2. Initializers → Processors/Resource

**CloudRoleNameInitializer →** `ConfigureResource()`  
**UserContextInitializer →** `UserContextProcessor : BaseProcessor<Activity>`  
**EnvironmentInitializer →** `EnvironmentProcessor : BaseProcessor<Activity>`

### 3. Processors → BaseProcessor<Activity>

**FilterHealthChecksProcessor →** `OnEnd()` with `IsAllDataRequested = false`  
**SamplingProcessor →** `SetSampler(new TraceIdRatioBasedSampler())`  
**EnrichmentProcessor →** `OnStart()` with `SetTag()`

### 4. Custom Instrumentation

**StartOperation<T>() →** `ActivitySource.StartActivity()`  
**TrackEvent() →** `ILogger.LogInformation()`  
**TrackException() →** `activity.RecordException()`  
**Success property →** `activity.SetStatus()`

### 5. Removed Files

- ❌ `Startup.cs` - Merged into Program.cs
- ❌ `applicationinsights.config` - XML config not needed
- ❌ All ITelemetryInitializer classes - Converted to processors or resource
- ❌ All ITelemetryProcessor classes - Converted to BaseProcessor<Activity>

## Benefits of Migration

1. **Less Code:** ~40% reduction in telemetry code
2. **Better Performance:** ActivitySource is faster than StartOperation
3. **Standards Compliance:** W3C Trace Context for distributed tracing
4. **Simplified Configuration:** Single location in Program.cs
5. **Automatic Correlation:** No manual parent-child management

## Testing Checklist

- [ ] All HTTP requests logged correctly
- [ ] Dependencies (HTTP, SQL) tracked
- [ ] Custom activities appear with proper hierarchy
- [ ] Health checks filtered out
- [ ] User context enriched on authenticated requests
- [ ] Environment tags present on all telemetry
- [ ] Sampling working (10% of traffic)
- [ ] Exceptions captured with proper context
- [ ] Live Metrics functioning
- [ ] Application Map shows correct topology

## See Also

- [worker-service-migration.md](worker-service-migration.md)
- [console-app-migration.md](console-app-migration.md)
- [activity-source.md](../../opentelemetry-fundamentals/activity-source.md)
