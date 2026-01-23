---
title: Activity.GetBaggageItem - Read Propagated Context
category: api-reference
applies-to: 3.x
namespace: System.Diagnostics
related:
  - api-reference/Activity/SetBaggage.md
  - concepts/activity-tags-vs-baggage.md
source: System.Diagnostics.Activity (.NET BCL)
---

# Activity.GetBaggageItem

## Signature

```csharp
namespace System.Diagnostics
{
    public class Activity
    {
        public string? GetBaggageItem(string key);
        
        public IEnumerable<KeyValuePair<string, string?>> Baggage { get; }
    }
}
```

## Description

Retrieves baggage value by key. Baggage is propagated from parent Activities and across service boundaries.

## Parameters

- **key**: `string` - The baggage key to retrieve

## Returns

`string?` - The baggage value, or `null` if not found

## Basic Usage

```csharp
var activity = Activity.Current;

// Get baggage value
var tenantId = activity?.GetBaggageItem("tenant.id");
var correlationId = activity?.GetBaggageItem("correlation.id");

// Check if exists
if (activity?.GetBaggageItem("feature.flag") != null)
{
    // Baggage exists
}
```

## Enumerate All Baggage

```csharp
var activity = Activity.Current;

// Iterate all baggage items
foreach (var item in activity.Baggage)
{
    Console.WriteLine($"{item.Key} = {item.Value}");
}

// Convert to dictionary
var baggageDict = activity.Baggage.ToDictionary(x => x.Key, x => x.Value);
```

## Propagation Example

```csharp
// Service A sets baggage
using (var activity = activitySource.StartActivity("ServiceA"))
{
    activity?.SetBaggage("tenant.id", "tenant-123");
    await httpClient.GetAsync("https://service-b/api/process");
}

// Service B receives baggage automatically
public IActionResult Process()
{
    var activity = Activity.Current;
    var tenantId = activity?.GetBaggageItem("tenant.id");
    // tenantId == "tenant-123" (propagated via HTTP header!)
    
    return Ok();
}
```

## Real-World Example: Multi-Tenant Database Selection

```csharp
public class TenantAwareRepository<T>
{
    private readonly ITenantDatabaseFactory dbFactory;
    
    public async Task<T> GetByIdAsync(string id)
    {
        // Get tenant from baggage
        var tenantId = Activity.Current?.GetBaggageItem("tenant.id");
        
        if (string.IsNullOrEmpty(tenantId))
        {
            throw new InvalidOperationException("Tenant context not found");
        }
        
        // Select tenant-specific database
        var db = dbFactory.GetDatabase(tenantId);
        return await db.GetByIdAsync<T>(id);
    }
}
```

## Pattern: Feature Flag Evaluation

```csharp
public class CheckoutService
{
    public async Task<CheckoutResult> ProcessAsync(Cart cart)
    {
        var activity = Activity.Current;
        
        // Check feature flag from baggage
        var useNewCheckout = bool.TryParse(
            activity?.GetBaggageItem("feature.new_checkout"),
            out var enabled) && enabled;
        
        if (useNewCheckout)
        {
            return await ProcessNewCheckoutAsync(cart);
        }
        else
        {
            return await ProcessLegacyCheckoutAsync(cart);
        }
    }
}
```

## Pattern: A/B Test Variant

```csharp
public class RecommendationService
{
    public async Task<List<Product>> GetRecommendationsAsync(string userId)
    {
        var activity = Activity.Current;
        
        // Get experiment variant from baggage
        var variant = activity?.GetBaggageItem("experiment.variant");
        var experimentId = activity?.GetBaggageItem("experiment.id");
        
        return variant switch
        {
            "control" => await GetControlRecommendationsAsync(userId),
            "variant_a" => await GetVariantARecommendationsAsync(userId),
            "variant_b" => await GetVariantBRecommendationsAsync(userId),
            _ => await GetDefaultRecommendationsAsync(userId)
        };
    }
}
```

## Pattern: Correlation ID Logging

```csharp
public class OrderService
{
    private readonly ILogger<OrderService> logger;
    
    public async Task CreateOrderAsync(Order order)
    {
        var activity = Activity.Current;
        var correlationId = activity?.GetBaggageItem("correlation.id");
        
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["OrderId"] = order.Id
        }))
        {
            logger.LogInformation("Processing order");
            // All logs include correlation ID
            
            await ProcessOrderAsync(order);
        }
    }
}
```

## Type Conversion

Baggage values are always strings. Convert as needed:

```csharp
var activity = Activity.Current;

// String (no conversion needed)
var tenantId = activity?.GetBaggageItem("tenant.id");

// Boolean
var useNewFeature = bool.TryParse(
    activity?.GetBaggageItem("feature.enabled"),
    out var enabled) && enabled;

// Integer
var timeout = int.TryParse(
    activity?.GetBaggageItem("request.timeout"),
    out var timeoutValue) ? timeoutValue : 30;

// Enum
var priority = Enum.TryParse<Priority>(
    activity?.GetBaggageItem("request.priority"),
    out var priorityValue) ? priorityValue : Priority.Normal;
```

## Null Safety

```csharp
// ❌ BAD: NullReferenceException risk
var tenantId = Activity.Current.GetBaggageItem("tenant.id");

// ✅ GOOD: Null-safe
var tenantId = Activity.Current?.GetBaggageItem("tenant.id");

// ✅ GOOD: With default
var tenantId = Activity.Current?.GetBaggageItem("tenant.id") ?? "default";

// ✅ GOOD: With validation
var tenantId = Activity.Current?.GetBaggageItem("tenant.id");
if (string.IsNullOrEmpty(tenantId))
{
    throw new InvalidOperationException("Tenant context required");
}
```

## Pattern: Baggage Processor

```csharp
public class BaggageEnrichmentProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        // Read baggage
        var tenantId = activity.GetBaggageItem("tenant.id");
        var correlationId = activity.GetBaggageItem("correlation.id");
        
        // Copy to tags for visibility in Application Insights
        if (!string.IsNullOrEmpty(tenantId))
        {
            activity.SetTag("tenant.id", tenantId);
        }
        
        if (!string.IsNullOrEmpty(correlationId))
        {
            activity.SetTag("correlation.id", correlationId);
        }
    }
}
```

## Pattern: Conditional Baggage

```csharp
public class RequestHandler
{
    public async Task HandleAsync(HttpRequest request)
    {
        var activity = Activity.Current;
        
        // Check if correlation ID already exists
        var correlationId = activity?.GetBaggageItem("correlation.id");
        
        if (string.IsNullOrEmpty(correlationId))
        {
            // Generate new one if missing
            correlationId = Guid.NewGuid().ToString();
            activity?.SetBaggage("correlation.id", correlationId);
        }
        
        // Use correlation ID
        logger.LogInformation("Processing request {CorrelationId}", correlationId);
    }
}
```

## Accessing Parent Baggage

Baggage is automatically inherited from parent Activities:

```csharp
// Parent activity
using (var parent = activitySource.StartActivity("Parent"))
{
    parent?.SetBaggage("tenant.id", "tenant-123");
    
    // Child activity
    using (var child = activitySource.StartActivity("Child"))
    {
        // Child automatically has parent's baggage
        var tenantId = child?.GetBaggageItem("tenant.id");
        // tenantId == "tenant-123"
    }
}
```

## Baggage from HTTP Headers

When receiving HTTP requests, baggage is automatically extracted from headers:

```http
GET /api/process HTTP/1.1
Host: service
baggage: tenant.id=tenant-123,correlation.id=abc-456
```

```csharp
[HttpGet]
public IActionResult Process()
{
    var activity = Activity.Current;
    
    // Automatically available from HTTP header
    var tenantId = activity?.GetBaggageItem("tenant.id");
    // tenantId == "tenant-123"
    
    var correlationId = activity?.GetBaggageItem("correlation.id");
    // correlationId == "abc-456"
    
    return Ok();
}
```

## Performance Considerations

✅ **Fast**: O(1) lookup in baggage dictionary
✅ **No allocation**: String value returned directly
✅ **Inherited**: No cost for inheriting from parent

## Common Patterns Summary

### Pattern 1: Database Context

```csharp
var tenantId = Activity.Current?.GetBaggageItem("tenant.id");
var db = dbFactory.GetDatabase(tenantId);
```

### Pattern 2: Feature Flag

```csharp
var enabled = bool.Parse(
    Activity.Current?.GetBaggageItem("feature.flag") ?? "false");
```

### Pattern 3: Correlation

```csharp
var correlationId = Activity.Current?.GetBaggageItem("correlation.id");
logger.LogInformation("Request {CorrelationId}", correlationId);
```

### Pattern 4: Copy to Tags

```csharp
var value = activity.GetBaggageItem("important.context");
activity.SetTag("important.context", value);  // Visible in AI
```

## Common Mistakes

```csharp
// ❌ MISTAKE 1: Forgetting null check
var tenantId = Activity.Current.GetBaggageItem("tenant.id");

// ❌ MISTAKE 2: Using GetBaggageItem for tags
var statusCode = Activity.Current.GetBaggageItem("http.status_code");
// Should use GetTagItem!

// ❌ MISTAKE 3: Not handling missing baggage
var tenantId = Activity.Current?.GetBaggageItem("tenant.id");
var db = GetDatabase(tenantId);  // May be null!

// ✅ CORRECT
var tenantId = Activity.Current?.GetBaggageItem("tenant.id");
if (string.IsNullOrEmpty(tenantId))
{
    throw new InvalidOperationException("Tenant context required");
}
var db = GetDatabase(tenantId);
```

## See Also

- [Activity.SetBaggage](SetBaggage.md) - Set baggage
- [Activity.GetTagItem](GetTagItem.md) - Read tags (not propagated)
- [concepts/activity-tags-vs-baggage.md](../../concepts/activity-tags-vs-baggage.md) - Complete guide

## References

- **Source**: `System.Diagnostics.Activity` (.NET BCL)
- **W3C Baggage**: https://www.w3.org/TR/baggage/
- **Activity Class**: https://learn.microsoft.com/dotnet/api/system.diagnostics.activity
