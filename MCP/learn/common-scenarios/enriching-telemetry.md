# Enriching Telemetry with Custom Properties

**Category:** Common Scenario  
**Applies to:** Adding custom properties, tags, and enrichment to telemetry  
**Related:** [activity-processor.md](../concepts/activity-processor.md), [OnStart.md](../api-reference/BaseProcessor/OnStart.md), [SetTag.md](../api-reference/Activity/SetTag.md)

## Overview

In Application Insights 3.x, you enrich telemetry by adding tags to Activities using `SetTag()` or by using a BaseProcessor to add properties to all telemetry.

## Approach 1: Direct Enrichment with SetTag

Add custom properties directly when creating Activities.

```csharp
private static readonly ActivitySource ActivitySource = new("MyService");

public async Task<Order> CreateOrderAsync(Order order)
{
    using var activity = ActivitySource.StartActivity("CreateOrder");
    
    // Add custom properties
    activity?.SetTag("order.id", order.Id);
    activity?.SetTag("order.total", order.Total);
    activity?.SetTag("customer.id", order.CustomerId);
    activity?.SetTag("environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
    
    await _repository.SaveAsync(order);
    
    return order;
}
```

## Approach 2: Global Enrichment with BaseProcessor

Add properties to all telemetry using OnStart.

### Basic Global Enrichment

```csharp
public class GlobalPropertiesProcessor : BaseProcessor<Activity>
{
    private readonly string _machineName;
    private readonly string _environment;
    
    public GlobalPropertiesProcessor()
    {
        _machineName = Environment.MachineName;
        _environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
    }
    
    public override void OnStart(Activity activity)
    {
        // Add properties to all telemetry
        activity.SetTag("machine.name", _machineName);
        activity.SetTag("deployment.environment", _environment);
        activity.SetTag("application.version", Assembly.GetEntryAssembly()?.GetName().Version?.ToString());
    }
}
```

Register in Program.cs:

```csharp
builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        otel.AddProcessor(new GlobalPropertiesProcessor());
    });
```

## Approach 3: User Context Enrichment

Add user information to telemetry.

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
        if (httpContext == null) return;
        
        // Add user information
        if (httpContext.User?.Identity?.IsAuthenticated == true)
        {
            activity.SetTag("user.id", httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            activity.SetTag("user.email", httpContext.User.FindFirst(ClaimTypes.Email)?.Value);
            activity.SetTag("user.roles", string.Join(",", GetUserRoles(httpContext.User)));
        }
        
        // Add request information
        activity.SetTag("client.ip", httpContext.Connection.RemoteIpAddress?.ToString());
        activity.SetTag("user.agent", httpContext.Request.Headers["User-Agent"].ToString());
    }
    
    private IEnumerable<string> GetUserRoles(ClaimsPrincipal user)
    {
        return user.FindAll(ClaimTypes.Role).Select(c => c.Value);
    }
}
```

Register with dependency injection:

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        otel.AddProcessor<UserContextProcessor>();
    });
```

## Approach 4: Multi-Tenant Enrichment

Add tenant information for multi-tenant applications.

```csharp
public class TenantContextProcessor : BaseProcessor<Activity>
{
    private readonly ITenantService _tenantService;
    
    public TenantContextProcessor(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }
    
    public override void OnStart(Activity activity)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (tenantId != null)
        {
            activity.SetTag("tenant.id", tenantId);
            activity.SetTag("tenant.name", _tenantService.GetTenantName(tenantId));
            activity.SetTag("tenant.tier", _tenantService.GetTenantTier(tenantId));
        }
    }
}
```

## Approach 5: Request-Specific Enrichment

Add properties based on HTTP request characteristics.

```csharp
public class RequestEnrichmentProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        // Only enrich HTTP server activities
        if (activity.Kind != ActivityKind.Server) return;
        
        var path = activity.GetTagItem("url.path") as string;
        var method = activity.GetTagItem("http.request.method") as string;
        
        // Add business context based on route
        if (path?.StartsWith("/api/orders") == true)
        {
            activity.SetTag("business.domain", "OrderManagement");
            activity.SetTag("business.criticality", "High");
        }
        else if (path?.StartsWith("/api/customers") == true)
        {
            activity.SetTag("business.domain", "CustomerManagement");
            activity.SetTag("business.criticality", "Medium");
        }
        
        // Tag admin operations
        if (path?.StartsWith("/admin") == true)
        {
            activity.SetTag("operation.type", "Administrative");
            activity.SetTag("requires.audit", "true");
        }
    }
}
```

## Approach 6: Database Query Enrichment

Add information about database operations.

```csharp
public class DatabaseEnrichmentProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        // Check if this is a database operation
        var dbSystem = activity.GetTagItem("db.system") as string;
        if (string.IsNullOrEmpty(dbSystem)) return;
        
        var statement = activity.GetTagItem("db.statement") as string;
        
        // Categorize query type
        if (!string.IsNullOrEmpty(statement))
        {
            if (statement.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                activity.SetTag("db.operation.type", "Read");
            }
            else if (statement.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase) ||
                     statement.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase) ||
                     statement.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase))
            {
                activity.SetTag("db.operation.type", "Write");
            }
        }
        
        // Add custom properties
        activity.SetTag("db.connection.pool", GetConnectionPoolName());
    }
    
    private string GetConnectionPoolName()
    {
        // Return connection pool identifier
        return "MainPool";
    }
}
```

## Approach 7: Configuration-Driven Enrichment

Use configuration to control enrichment properties.

### appsettings.json

```json
{
  "TelemetryEnrichment": {
    "Properties": {
      "ApplicationName": "MyService",
      "Region": "West US",
      "DataCenter": "DC01"
    }
  }
}
```

### Processor

```csharp
public class ConfigurableEnrichmentProcessor : BaseProcessor<Activity>
{
    private readonly Dictionary<string, string> _properties;
    
    public ConfigurableEnrichmentProcessor(IConfiguration configuration)
    {
        _properties = configuration
            .GetSection("TelemetryEnrichment:Properties")
            .Get<Dictionary<string, string>>() ?? new Dictionary<string, string>();
    }
    
    public override void OnStart(Activity activity)
    {
        foreach (var kvp in _properties)
        {
            activity.SetTag(kvp.Key, kvp.Value);
        }
    }
}
```

Register:

```csharp
builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        otel.AddProcessor(sp => 
            new ConfigurableEnrichmentProcessor(
                sp.GetRequiredService<IConfiguration>()));
    });
```

## Approach 8: Performance Metrics Enrichment

Add performance-related properties.

```csharp
public class PerformanceEnrichmentProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Add duration categorization
        var durationMs = activity.Duration.TotalMilliseconds;
        
        if (durationMs < 100)
            activity.SetTag("performance.category", "Fast");
        else if (durationMs < 1000)
            activity.SetTag("performance.category", "Normal");
        else if (durationMs < 5000)
            activity.SetTag("performance.category", "Slow");
        else
            activity.SetTag("performance.category", "VerySlow");
        
        // Add performance bucket
        var bucket = ((int)(durationMs / 100)) * 100;
        activity.SetTag("performance.bucket", $"{bucket}ms");
    }
}
```

## Migration from 2.x

### Before (2.x): TelemetryInitializer

```csharp
public class CustomPropertiesInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        if (telemetry is ISupportProperties propertiesTelemetry)
        {
            propertiesTelemetry.Properties["MachineName"] = Environment.MachineName;
            propertiesTelemetry.Properties["Environment"] = "Production";
        }
    }
}
```

### After (3.x): BaseProcessor

```csharp
public class CustomPropertiesProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        activity.SetTag("MachineName", Environment.MachineName);
        activity.SetTag("Environment", "Production");
    }
}
```

## Best Practices

### 1. Use OnStart for Most Enrichment

```csharp
public override void OnStart(Activity activity)
{
    // Properties available throughout activity lifecycle
    activity.SetTag("property", "value");
}
```

### 2. Use OnEnd for Computed Properties

```csharp
public override void OnEnd(Activity activity)
{
    // Compute based on final state
    var category = CategorizeActivity(activity);
    activity.SetTag("category", category);
}
```

### 3. Avoid Heavy Operations in Processors

```csharp
// Bad: Synchronous database call
public override void OnStart(Activity activity)
{
    var data = _database.GetData(); // SLOW!
    activity.SetTag("data", data);
}

// Good: Use cached or pre-computed data
private readonly string _cachedData;

public CustomProcessor()
{
    _cachedData = ComputeData();
}

public override void OnStart(Activity activity)
{
    activity.SetTag("data", _cachedData);
}
```

### 4. Use Semantic Conventions

Follow [OpenTelemetry semantic conventions](https://opentelemetry.io/docs/specs/semconv/) for standard attributes:

```csharp
// Good: Standard attributes
activity.SetTag("deployment.environment", "production");
activity.SetTag("service.version", "1.0.0");
activity.SetTag("host.name", Environment.MachineName);

// Acceptable: Custom attributes with namespacing
activity.SetTag("mycompany.tenant.id", tenantId);
activity.SetTag("mycompany.feature.flag", "enabled");
```

### 5. Check Activity Kind Before Enriching

```csharp
public override void OnStart(Activity activity)
{
    // Only enrich HTTP requests
    if (activity.Kind == ActivityKind.Server)
    {
        // Add HTTP-specific enrichment
    }
    
    // Only enrich database calls
    if (activity.GetTagItem("db.system") != null)
    {
        // Add database-specific enrichment
    }
}
```

## Testing

```csharp
[Fact]
public void Processor_AddsGlobalProperties()
{
    var processor = new GlobalPropertiesProcessor();
    var activity = new Activity("Test").Start();
    
    processor.OnStart(activity);
    
    Assert.NotNull(activity.GetTagItem("machine.name"));
    Assert.NotNull(activity.GetTagItem("deployment.environment"));
    
    activity.Stop();
}
```

## Performance Considerations

- **Cache static values** in processor constructor
- **Avoid async operations** in processors
- **Check activity kind** before expensive enrichment
- **Use string values** (primitives are converted to strings in Azure Monitor)

## See Also

- [OnStart.md](../api-reference/BaseProcessor/OnStart.md)
- [SetTag.md](../api-reference/Activity/SetTag.md)
- [activity-processor.md](../concepts/activity-processor.md)
- [setting-cloud-role-name.md](setting-cloud-role-name.md)
