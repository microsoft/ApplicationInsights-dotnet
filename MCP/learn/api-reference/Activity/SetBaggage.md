---
title: Activity.SetBaggage - Set Propagated Context
category: api-reference
applies-to: 3.x
namespace: System.Diagnostics
related:
  - concepts/activity-tags-vs-baggage.md
  - api-reference/Activity/GetBaggageItem.md
  - api-reference/Activity/SetTag.md
source: System.Diagnostics.Activity (.NET BCL)
---

# Activity.SetBaggage

## Signature

```csharp
namespace System.Diagnostics
{
    public class Activity
    {
        public Activity SetBaggage(string key, string? value);
        
        public IEnumerable<KeyValuePair<string, string?>> Baggage { get; }
    }
}
```

## Description

Sets baggage (key-value pair) that is **propagated to all child Activities** and across service boundaries via HTTP headers. Use for correlation data that needs to flow through your entire distributed system.

## Parameters

- **key**: `string` - The baggage key
- **value**: `string?` - The baggage value (must be string, unlike tags)

## Returns

Returns `this` Activity instance for method chaining.

## Key Differences from SetTag

| Feature | SetTag | SetBaggage |
|---------|--------|-----------|
| **Propagated?** | ❌ No - local only | ✅ Yes - to all children |
| **Use for** | Telemetry dimensions | Correlation context |
| **Appears in AI** | ✅ Automatic | ⚠️ Only if copied to tag |
| **Size limit** | ~10MB | ~8KB total |
| **Performance** | Fast | Slower (HTTP headers) |
| **Value type** | `object?` | `string?` only |

## When to Use Baggage

✅ **Use baggage for:**
- Correlation IDs
- Tenant/Customer IDs
- Feature flags
- Experiment/Variant IDs
- Security contexts (with caution)

❌ **Don't use baggage for:**
- Large data (> 1KB per item)
- Sensitive secrets (visible in network traces)
- Telemetry dimensions (use tags instead)
- Data that doesn't need propagation

## Basic Usage

```csharp
var activity = Activity.Current;

// Set baggage (propagates to children)
activity?.SetBaggage("correlation.id", "abc-123");
activity?.SetBaggage("tenant.id", "tenant-456");

// Remove baggage
activity?.SetBaggage("temporary.id", null);
```

## Propagation Example

```csharp
// Service A
using (var activity = activitySource.StartActivity("ServiceA"))
{
    activity?.SetBaggage("tenant.id", "tenant-123");
    activity?.SetBaggage("user.id", "user-456");
    
    // Call Service B
    await httpClient.GetAsync("https://service-b/api/validate");
}

// Service B - automatically receives baggage
public async Task<IActionResult> Validate()
{
    var activity = Activity.Current;
    
    // Baggage propagated automatically!
    var tenantId = activity?.GetBaggageItem("tenant.id");
    // tenantId == "tenant-123"
    
    var userId = activity?.GetBaggageItem("user.id");
    // userId == "user-456"
    
    // Use tenant context
    var db = GetDatabaseForTenant(tenantId);
    return Ok();
}
```

## HTTP Header Propagation

```http
GET /api/validate HTTP/1.1
Host: service-b
traceparent: 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01
baggage: tenant.id=tenant-123,user.id=user-456
```

Baggage is automatically encoded/decoded in the `baggage` HTTP header.

## Real-World Example: Multi-Tenant System

```csharp
// API Gateway (Entry Point)
[ApiController]
public class GatewayController : ControllerBase
{
    [HttpPost("orders")]
    public async Task<IActionResult> CreateOrder([FromBody] OrderRequest request)
    {
        var activity = Activity.Current;
        
        // Extract tenant from authentication
        var tenantId = User.FindFirst("tenant_id")?.Value;
        
        // Set as baggage - will propagate to all downstream services
        activity?.SetBaggage("tenant.id", tenantId);
        activity?.SetBaggage("correlation.id", Guid.NewGuid().ToString());
        
        // Also set as tag for telemetry
        activity?.SetTag("tenant.id", tenantId);
        
        // Call order service
        var orderId = await orderService.CreateAsync(request);
        return Ok(new { orderId });
    }
}

// Order Service (Downstream)
public class OrderService
{
    public async Task<string> CreateAsync(OrderRequest request)
    {
        using (var activity = activitySource.StartActivity("CreateOrder"))
        {
            // Automatically has tenant.id from baggage!
            var tenantId = Activity.Current?.GetBaggageItem("tenant.id");
            
            // Use tenant-specific database
            var db = GetTenantDatabase(tenantId);
            
            // Call inventory service (tenantId propagates further)
            await inventoryService.ReserveAsync(request.Items);
            
            return await db.Orders.InsertAsync(request);
        }
    }
}

// Inventory Service (Further Downstream)
public class InventoryService
{
    public async Task ReserveAsync(List<Item> items)
    {
        using (var activity = activitySource.StartActivity("ReserveInventory"))
        {
            // Still has tenant.id from original gateway request!
            var tenantId = Activity.Current?.GetBaggageItem("tenant.id");
            var correlationId = Activity.Current?.GetBaggageItem("correlation.id");
            
            var db = GetTenantDatabase(tenantId);
            // ... reserve inventory
        }
    }
}
```

## Feature Flags via Baggage

```csharp
// API Gateway
public IActionResult ProcessRequest()
{
    var activity = Activity.Current;
    
    // Propagate feature flag to all services
    var useNewCheckout = featureFlags.IsEnabled("new-checkout");
    activity?.SetBaggage("feature.new_checkout", useNewCheckout.ToString());
    
    return Ok();
}

// Downstream Service
public async Task Checkout(Cart cart)
{
    var activity = Activity.Current;
    var useNewCheckout = bool.Parse(
        activity?.GetBaggageItem("feature.new_checkout") ?? "false");
    
    if (useNewCheckout)
    {
        await newCheckoutService.ProcessAsync(cart);
    }
    else
    {
        await legacyCheckoutService.ProcessAsync(cart);
    }
}
```

## A/B Testing with Baggage

```csharp
// Entry point
activity?.SetBaggage("experiment.variant", GetExperimentVariant());
activity?.SetBaggage("experiment.id", "checkout-test-001");

// All downstream services know the experiment context
var variant = Activity.Current?.GetBaggageItem("experiment.variant");
var experimentId = Activity.Current?.GetBaggageItem("experiment.id");
```

## Making Baggage Visible in Application Insights

Baggage is NOT automatically sent to Application Insights. To see it, copy to tags:

```csharp
public class BaggageToTagsProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Copy all baggage to tags for visibility in AI
        foreach (var item in activity.Baggage)
        {
            activity.SetTag($"baggage.{item.Key}", item.Value);
        }
    }
}

// Register
config.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.AddProcessor(new BaggageToTagsProcessor());
});
```

## Size Limitations

⚠️ **W3C Baggage Limit**: ~8KB total for all baggage items combined

```csharp
// ✅ GOOD: Small, critical correlation data
activity.SetBaggage("tenant.id", "t-123");           // ~15 bytes
activity.SetBaggage("correlation.id", "abc-456");    // ~20 bytes
activity.SetBaggage("user.id", "u-789");             // ~15 bytes
// Total: ~50 bytes

// ❌ BAD: Large data
activity.SetBaggage("user.profile", JsonSerializer.Serialize(largeProfile));
activity.SetBaggage("request.body", requestBody);
// Can exceed 8KB limit, causing data loss!
```

## Performance Considerations

### Baggage adds overhead:
- 📦 Serialized in every HTTP request (header size)
- 🌐 Network bandwidth cost
- 🔄 Parsing/encoding on every hop

### Best practices:
```csharp
// ✅ GOOD: Minimal, essential baggage
activity.SetBaggage("tenant.id", tenantId);

// ❌ BAD: Many large items
activity.SetBaggage("data1", largeString1);
activity.SetBaggage("data2", largeString2);
activity.SetBaggage("data3", largeString3);
// Heavy network overhead!
```

## Security Considerations

⚠️ **Baggage is visible in network traces** - don't store secrets!

```csharp
// ❌ DANGEROUS: Secrets in baggage
activity.SetBaggage("api.key", secretApiKey);
activity.SetBaggage("password", userPassword);
// Visible in HTTP headers, logs, traces!

// ✅ SAFE: Only IDs and non-sensitive data
activity.SetBaggage("tenant.id", tenantId);
activity.SetBaggage("session.id", sessionId);
```

## Baggage vs Tags Decision Tree

```
Does this data need to be available in downstream services?
│
├─ YES
│  │
│  ├─ Is it small (< 100 bytes)?
│  │  └─ YES → Use SetBaggage
│  │
│  └─ NO → Consider alternatives (Activity.TraceId, custom headers)
│
└─ NO
   └─ Use SetTag (telemetry only)
```

## Common Patterns

### Pattern 1: Correlation ID

```csharp
// Generate once at entry point
activity?.SetBaggage("correlation.id", Guid.NewGuid().ToString());

// Available everywhere downstream
var correlationId = Activity.Current?.GetBaggageItem("correlation.id");
```

### Pattern 2: Tenant Context

```csharp
// Set at authentication boundary
activity?.SetBaggage("tenant.id", authenticatedTenantId);

// All services use correct tenant context
var tenantId = Activity.Current?.GetBaggageItem("tenant.id");
var tenantDb = dbFactory.GetDatabase(tenantId);
```

### Pattern 3: Conditional Baggage

```csharp
// Only set baggage if missing
if (activity?.GetBaggageItem("correlation.id") == null)
{
    activity?.SetBaggage("correlation.id", GenerateCorrelationId());
}
```

## Method Chaining

```csharp
Activity.Current?
    .SetBaggage("tenant.id", tenantId)
    .SetBaggage("correlation.id", correlationId)
    .SetTag("user.id", userId);  // Mix with tags
```

## Common Mistakes

```csharp
// ❌ MISTAKE 1: Using baggage for large data
activity.SetBaggage("request.body", largeJson);  // Too big!

// ❌ MISTAKE 2: Using baggage for sensitive data
activity.SetBaggage("credit.card", cardNumber);  // Security risk!

// ❌ MISTAKE 3: Using baggage for telemetry-only data
activity.SetBaggage("http.status_code", "200");  // Should be tag!

// ❌ MISTAKE 4: Forgetting it's string-only
activity.SetBaggage("count", 123);  // Compiler error! Must be string

// ✅ CORRECT
activity.SetBaggage("correlation.id", correlationId);
activity.SetTag("http.status_code", 200);
activity.SetTag("count", 123);
```

## 2.x Equivalent

```csharp
// 2.x: CallContext for propagation
CallContext.LogicalSetData("tenant.id", tenantId);

// 3.x: Baggage
activity?.SetBaggage("tenant.id", tenantId);
```

## See Also

- [Activity.GetBaggageItem](GetBaggageItem.md) - Read baggage
- [Activity.SetTag](SetTag.md) - Set local tags
- [concepts/activity-tags-vs-baggage.md](../../concepts/activity-tags-vs-baggage.md) - Complete guide

## References

- **Source**: `System.Diagnostics.Activity` (.NET BCL)
- **W3C Baggage**: https://www.w3.org/TR/baggage/
- **OpenTelemetry Baggage**: https://opentelemetry.io/docs/specs/otel/baggage/api/
