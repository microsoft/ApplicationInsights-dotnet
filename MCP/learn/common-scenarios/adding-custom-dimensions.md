# Adding Custom Dimensions (Properties) in 3.x

**Category:** Common Scenario  
**Applies to:** Application Insights .NET SDK 3.x  
**Related:** [properties-to-tags.md](../mappings/properties-to-tags.md), [SetTag.md](../api-reference/Activity/SetTag.md)

## Overview

Custom dimensions (called "Properties" in 2.x) are key-value pairs that enrich telemetry with custom data. In 3.x, these are added using **Activity.SetTag()**.

## Quick Solution

```csharp
// Add custom dimension to current Activity
Activity.Current?.SetTag("userId", "12345");
Activity.Current?.SetTag("orderType", "premium");
Activity.Current?.SetTag("itemCount", 42);
```

**Result in Azure Monitor:**
Custom Dimensions field contains:
- `userId`: "12345"
- `orderType`: "premium"
- `itemCount`: 42

## When to Add Custom Dimensions

Use custom dimensions to:
- **Track business metrics:** Order count, revenue, user segments
- **Debug issues:** Request IDs, correlation IDs, internal state
- **Filter telemetry:** Environment, feature flags, A/B test variants
- **Enrich context:** User roles, tenant IDs, geographic regions

## Method 1: Inline in Controller/Service

```csharp
public class OrderController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateOrder(Order order)
    {
        // Activity automatically created by ASP.NET Core
        // Add custom dimensions
        Activity.Current?.SetTag("order.id", order.Id);
        Activity.Current?.SetTag("customer.id", order.CustomerId);
        Activity.Current?.SetTag("order.total", order.TotalAmount);
        Activity.Current?.SetTag("order.item_count", order.Items.Count);
        Activity.Current?.SetTag("is_premium", order.IsPremium);
        
        await orderService.ProcessAsync(order);
        return Ok();
    }
}
```

## Method 2: Activity Processor (All Activities)

```csharp
public class CustomDimensionsProcessor : BaseProcessor<Activity>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public CustomDimensionsProcessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public override void OnStart(Activity activity)
    {
        // Only enrich server activities (requests)
        if (activity.Kind == ActivityKind.Server)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            
            // Add user context
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                activity.SetTag("user.id", httpContext.User.Identity.Name);
                
                var role = httpContext.User.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.IsNullOrEmpty(role))
                {
                    activity.SetTag("user.role", role);
                }
            }
            
            // Add request headers
            var correlationId = httpContext?.Request.Headers["X-Correlation-Id"].ToString();
            if (!string.IsNullOrEmpty(correlationId))
            {
                activity.SetTag("correlation.id", correlationId);
            }
            
            // Add query parameters
            var feature = httpContext?.Request.Query["feature"].ToString();
            if (!string.IsNullOrEmpty(feature))
            {
                activity.SetTag("feature.flag", feature);
            }
        }
    }
}

// Registration
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        var httpContextAccessor = builder.Services.BuildServiceProvider()
            .GetRequiredService<IHttpContextAccessor>();
        builder.AddProcessor(new CustomDimensionsProcessor(httpContextAccessor));
    });
```

## Method 3: Resource (Constant Values)

```csharp
// For values that DON'T change per request, use Resource
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.ConfigureResource(resource =>
            resource.AddAttributes(new[]
            {
                new KeyValuePair<string, object>("environment", "production"),
                new KeyValuePair<string, object>("version", "1.2.3"),
                new KeyValuePair<string, object>("datacenter", "west-us-2")
            }));
    });

// These appear in Custom Dimensions on EVERY telemetry item automatically
```

## Data Types

### Supported Types

```csharp
// String
Activity.Current?.SetTag("userId", "user123");

// Integer
Activity.Current?.SetTag("count", 42);

// Double
Activity.Current?.SetTag("amount", 99.99);

// Boolean
Activity.Current?.SetTag("isPremium", true);

// DateTime
Activity.Current?.SetTag("processedAt", DateTime.UtcNow);

// Array
Activity.Current?.SetTag("roles", new[] { "admin", "user" });
```

**Note:** In Azure Monitor, all values appear as strings in Custom Dimensions (types preserved in OpenTelemetry pipeline).

## Migration from 2.x

### 2.x: Properties Dictionary

```csharp
// Manual telemetry tracking
var request = new RequestTelemetry
{
    Name = "POST /api/orders",
    Properties =
    {
        ["userId"] = "12345",
        ["orderType"] = "premium",
        ["itemCount"] = "42"  // Must be string
    }
};
telemetryClient.TrackRequest(request);

// Or in initializer
public class PropertiesInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Properties["customProperty"] = "value";
    }
}
```

### 3.x: Activity Tags

```csharp
// Automatic telemetry tracking
public IActionResult CreateOrder(Order order)
{
    // Activity automatically created by ASP.NET Core
    Activity.Current?.SetTag("userId", "12345");
    Activity.Current?.SetTag("orderType", "premium");
    Activity.Current?.SetTag("itemCount", 42);  // Can be any type
    
    return Ok();
}

// Or in processor
public class PropertiesProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        activity.SetTag("customProperty", "value");
    }
}
```

## Naming Conventions

### OpenTelemetry Semantic Conventions (Preferred)

```csharp
// User attributes
activity.SetTag("enduser.id", userId);
activity.SetTag("enduser.role", role);

// HTTP attributes
activity.SetTag("http.request.method", "GET");
activity.SetTag("http.response.status_code", 200);

// Database attributes
activity.SetTag("db.system", "postgresql");
activity.SetTag("db.name", "orders");
activity.SetTag("db.statement", "SELECT * FROM orders");

// URL attributes
activity.SetTag("url.path", "/api/orders");
activity.SetTag("url.query", "status=pending");

// Server attributes
activity.SetTag("server.address", "api.example.com");
activity.SetTag("server.port", 443);
```

### Custom Attributes

```csharp
// Use namespace.attribute format
activity.SetTag("order.id", orderId);
activity.SetTag("order.type", orderType);
activity.SetTag("order.total", totalAmount);
activity.SetTag("customer.id", customerId);
activity.SetTag("customer.tier", "premium");

// Use lowercase with dots or underscores
activity.SetTag("feature.flag.enabled", true);
activity.SetTag("ab_test_variant", "B");
```

**Avoid:** camelCase, PascalCase, hyphens (prefer dots or underscores).

## Real-World Examples

### Example 1: E-commerce Order Processing

```csharp
public class OrderController : ControllerBase
{
    [HttpPost("orders")]
    public async Task<IActionResult> CreateOrder(CreateOrderRequest request)
    {
        // Add business metrics
        Activity.Current?.SetTag("order.id", request.OrderId);
        Activity.Current?.SetTag("customer.id", request.CustomerId);
        Activity.Current?.SetTag("order.total", request.TotalAmount);
        Activity.Current?.SetTag("order.currency", request.Currency);
        Activity.Current?.SetTag("order.item_count", request.Items.Count);
        Activity.Current?.SetTag("payment.method", request.PaymentMethod);
        
        // Add segmentation
        Activity.Current?.SetTag("customer.tier", GetCustomerTier(request.CustomerId));
        Activity.Current?.SetTag("is_first_order", await IsFirstOrder(request.CustomerId));
        
        // Add feature flags
        Activity.Current?.SetTag("feature.express_checkout", IsExpressCheckoutEnabled());
        
        await orderService.ProcessAsync(request);
        return Ok();
    }
}
```

### Example 2: Multi-Tenant SaaS

```csharp
public class TenantEnrichmentProcessor : BaseProcessor<Activity>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITenantService _tenantService;
    
    public override void OnStart(Activity activity)
    {
        if (activity.Kind == ActivityKind.Server)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var tenantId = httpContext?.Request.Headers["X-Tenant-Id"].ToString();
            
            if (!string.IsNullOrEmpty(tenantId))
            {
                activity.SetTag("tenant.id", tenantId);
                
                // Enrich with tenant metadata
                var tenant = _tenantService.GetTenant(tenantId);
                if (tenant != null)
                {
                    activity.SetTag("tenant.name", tenant.Name);
                    activity.SetTag("tenant.plan", tenant.PlanType);
                    activity.SetTag("tenant.region", tenant.Region);
                }
            }
        }
    }
}
```

### Example 3: A/B Testing

```csharp
public class ABTestProcessor : BaseProcessor<Activity>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IExperimentService _experimentService;
    
    public override void OnStart(Activity activity)
    {
        if (activity.Kind == ActivityKind.Server)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var userId = httpContext?.User?.Identity?.Name;
            
            if (!string.IsNullOrEmpty(userId))
            {
                // Get experiment variants for user
                var variants = _experimentService.GetUserVariants(userId);
                
                foreach (var (experimentName, variant) in variants)
                {
                    activity.SetTag($"experiment.{experimentName}", variant);
                }
            }
        }
    }
}
```

## Querying Custom Dimensions in Azure Monitor

### Kusto Query (Log Analytics)

```kusto
requests
| where timestamp > ago(1h)
| where customDimensions.userId == "12345"
| project timestamp, name, customDimensions.orderType, customDimensions.itemCount
```

### Filter by Custom Dimension

```kusto
requests
| where timestamp > ago(1d)
| where customDimensions.environment == "production"
| where customDimensions["is_premium"] == "true"
| summarize count() by tostring(customDimensions.orderType)
```

## Performance Considerations

### Avoid Excessive Dimensions

```csharp
// ❌ BAD: Too many dimensions (slows processing, increases costs)
for (int i = 0; i < 100; i++)
{
    activity.SetTag($"item_{i}", itemValues[i]);
}

// ✅ GOOD: Aggregate or summarize
activity.SetTag("total_items", itemValues.Length);
activity.SetTag("total_value", itemValues.Sum());
```

### Avoid Large String Values

```csharp
// ❌ BAD: Large serialized objects
activity.SetTag("full_request", JsonSerializer.Serialize(largeObject));

// ✅ GOOD: Extract relevant fields
activity.SetTag("request.id", largeObject.Id);
activity.SetTag("request.type", largeObject.Type);
```

### Use Resource for Constants

```csharp
// ❌ BAD: Set constant value in processor (called per-request)
public override void OnStart(Activity activity)
{
    activity.SetTag("environment", "production"); // Same for every request
}

// ✅ GOOD: Set constant value in Resource (set once at startup)
builder.ConfigureResource(resource =>
    resource.AddAttributes(new[]
    {
        new KeyValuePair<string, object>("environment", "production")
    }));
```

## Sensitive Data

### Avoid PII in Custom Dimensions

```csharp
// ❌ BAD: Sensitive data
activity.SetTag("email", "user@example.com");
activity.SetTag("credit_card", "4111-1111-1111-1111");
activity.SetTag("ssn", "123-45-6789");

// ✅ GOOD: Anonymized or hashed
activity.SetTag("user.id", HashUserId(email));
activity.SetTag("payment.method.last4", "1111");
```

## Common Patterns

### Pattern 1: Correlation IDs

```csharp
Activity.Current?.SetTag("correlation.id", HttpContext.Request.Headers["X-Correlation-Id"]);
Activity.Current?.SetTag("request.id", Guid.NewGuid().ToString());
```

### Pattern 2: Geographic Region

```csharp
Activity.Current?.SetTag("region", GetRegionFromIP(httpContext.Connection.RemoteIpAddress));
Activity.Current?.SetTag("datacenter", Environment.GetEnvironmentVariable("DATACENTER"));
```

### Pattern 3: Performance Metrics

```csharp
Activity.Current?.SetTag("cache.hit", cacheHit);
Activity.Current?.SetTag("db.query.rows", rowCount);
Activity.Current?.SetTag("processing.duration_ms", processingTime.TotalMilliseconds);
```

## See Also

- [properties-to-tags.md](../mappings/properties-to-tags.md) - Properties mapping guide
- [SetTag.md](../api-reference/Activity/SetTag.md) - SetTag API reference
- [enrichment-with-onstart.md](../transformations/ITelemetryInitializer/enrichment-with-onstart.md) - Enrichment pattern
- [resource-detector.md](../concepts/resource-detector.md) - Resource for constants
- [OpenTelemetry Semantic Conventions](https://opentelemetry.io/docs/specs/semconv/)
