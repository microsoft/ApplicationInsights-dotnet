---
title: Activity Tags vs Baggage - When to Use Each
category: concept
applies-to: 3.x
related:
  - concepts/activity-vs-telemetry.md
  - api-reference/Activity/SetTag.md
  - mappings/properties-to-tags.md
source: System.Diagnostics.Activity
---

# Activity Tags vs Baggage - When to Use Each

## Overview

Activities have two mechanisms for storing key-value data: **Tags** and **Baggage**. Understanding when to use each is critical for proper telemetry and distributed tracing.

## Quick Reference

| Feature | Tags | Baggage |
|---------|------|---------|
| **Propagated?** | ❌ No - local to this activity | ✅ Yes - propagated to children |
| **Use for** | Telemetry data, custom dimensions | Distributed context, correlation data |
| **2.x Equivalent** | `telemetry.Properties[key]` | `telemetry.Context.Properties[key]` |
| **Appears in AI** | ✅ Yes - as custom dimensions | ⚠️ Only if explicitly copied to tags |
| **Size limit** | ~10MB per activity | Small (~8KB total) |
| **Performance** | Fast (not propagated) | Slower (propagated in headers) |

## Activity Tags

Tags are **local metadata** attached to an Activity. They appear in Application Insights as custom dimensions but are NOT propagated to child activities.

### Setting Tags

```csharp
var activity = Activity.Current;

// Set a tag
activity?.SetTag("user.id", "12345");
activity?.SetTag("order.amount", 199.99);
activity?.SetTag("custom.dimension", "value");

// Tags appear in Application Insights as custom dimensions
```

### Reading Tags

```csharp
// Get a tag value
var userId = activity?.GetTagItem("user.id");

// Enumerate all tags
foreach (var tag in activity.TagObjects)
{
    Console.WriteLine($"{tag.Key} = {tag.Value}");
}
```

### Common Use Cases for Tags

1. **Custom dimensions** - Business metrics visible in AI Portal
2. **HTTP request details** - status codes, paths, methods
3. **User information** - user ID, session ID, tenant ID
4. **Performance metrics** - item counts, sizes, durations
5. **Error details** - error codes, categories, descriptions

### Example: HTTP Request Tags

```csharp
// ASP.NET Core automatically sets these tags
activity.SetTag("http.request.method", "GET");
activity.SetTag("url.path", "/api/orders/123");
activity.SetTag("http.response.status_code", 200);
activity.SetTag("server.address", "localhost");
activity.SetTag("server.port", 5000);

// Your custom tags
activity.SetTag("user.id", User.Identity.Name);
activity.SetTag("tenant.id", GetTenantId());
```

## Activity Baggage

Baggage is **propagated context** that flows to all child activities. It's transmitted in HTTP headers and shared across service boundaries.

### Setting Baggage

```csharp
var activity = Activity.Current;

// Set baggage (will propagate to all children)
activity?.SetBaggage("correlation.id", "abc-123");
activity?.SetBaggage("tenant.id", "tenant-456");
```

### Reading Baggage

```csharp
// Get baggage value
var correlationId = activity?.GetBaggageItem("correlation.id");

// Enumerate all baggage
foreach (var item in activity.Baggage)
{
    Console.WriteLine($"{item.Key} = {item.Value}");
}
```

### Baggage Propagation

```csharp
// Service A
using (var activity = activitySource.StartActivity("ProcessOrder"))
{
    activity?.SetBaggage("order.id", "12345");
    activity?.SetBaggage("user.id", "user-789");
    
    // Call Service B
    await httpClient.GetAsync("https://service-b/api/validate");
    // Baggage is automatically sent in traceparent/tracestate headers
}

// Service B - receives baggage automatically
public async Task<IActionResult> Validate()
{
    var orderId = Activity.Current?.GetBaggageItem("order.id");
    // orderId = "12345" - propagated from Service A!
    
    var userId = Activity.Current?.GetBaggageItem("user.id");
    // userId = "user-789"
}
```

### Common Use Cases for Baggage

1. **Correlation IDs** - Track requests across services
2. **Tenant/Customer IDs** - Multi-tenant scenarios
3. **Feature flags** - Propagate feature state
4. **Experiment IDs** - A/B testing contexts
5. **Security tokens** - (Use with caution - size limits!)

### Important Baggage Limitations

⚠️ **Size Limit**: ~8KB total across all baggage items
⚠️ **Performance**: Every baggage item adds HTTP header overhead
⚠️ **Security**: Baggage is visible in network traces - don't store secrets!
⚠️ **Not automatic telemetry**: Baggage doesn't appear in AI unless explicitly copied to tags

## Tags vs Baggage Decision Tree

```
Do I need this data in child activities/downstream services?
├─ Yes → Use Baggage
│  └─ Examples: correlation ID, tenant ID, feature flags
│
└─ No → Use Tags
   └─ Examples: HTTP status, user agent, custom dimensions
```

## Mapping from Application Insights 2.x

### Properties → Tags

```csharp
// 2.x: Local properties
request.Properties["user.id"] = "12345";
request.Properties["order.amount"] = "199.99";

// 3.x: Tags (same behavior - local to this telemetry)
activity.SetTag("user.id", "12345");
activity.SetTag("order.amount", 199.99);
```

### Context Properties → Baggage

```csharp
// 2.x: Context properties (propagated)
CallContext.LogicalSetData("correlation.id", "abc-123");
// or
telemetry.Context.Properties["correlation.id"] = "abc-123";

// 3.x: Baggage (propagated to children)
activity.SetBaggage("correlation.id", "abc-123");
```

## Making Baggage Visible in Application Insights

Baggage doesn't automatically appear in telemetry. To see it, copy to tags:

```csharp
public class BaggageToTagsProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Copy all baggage to tags so it appears in AI
        foreach (var baggageItem in activity.Baggage)
        {
            activity.SetTag($"baggage.{baggageItem.Key}", baggageItem.Value);
        }
    }
}

// Register processor
config.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.AddProcessor(new BaggageToTagsProcessor());
});
```

## Real-World Example: Multi-Tenant Application

```csharp
// Service A: API Gateway
public async Task<IActionResult> ProcessRequest()
{
    using (var activity = activitySource.StartActivity("ProcessRequest"))
    {
        var tenantId = GetTenantFromAuth();
        
        // Use BAGGAGE for tenant ID - needs to flow to all services
        activity?.SetBaggage("tenant.id", tenantId);
        
        // Use TAGS for local telemetry
        activity?.SetTag("request.path", Request.Path);
        activity?.SetTag("user.agent", Request.Headers["User-Agent"]);
        
        // Call downstream service
        var result = await orderService.CreateOrder();
        
        activity?.SetTag("order.created", true);
        return Ok(result);
    }
}

// Service B: Order Service (downstream)
public class OrderService
{
    public async Task CreateOrder()
    {
        using (var activity = activitySource.StartActivity("CreateOrder"))
        {
            // Automatically receives tenant.id from baggage!
            var tenantId = Activity.Current?.GetBaggageItem("tenant.id");
            
            // Use correct tenant database
            var db = GetDatabaseForTenant(tenantId);
            
            // Local tags for this operation
            activity?.SetTag("database.name", db.Name);
            activity?.SetTag("order.item_count", items.Count);
        }
    }
}
```

## Performance Considerations

### Tags
- ✅ Fast - no propagation overhead
- ✅ No size limit (reasonable - ~10MB)
- ✅ No network overhead
- ❌ Not available in child activities

### Baggage
- ❌ Slower - propagated in every HTTP call
- ❌ Size limited (~8KB total)
- ❌ Network overhead (headers)
- ✅ Available in all child activities

### Best Practice

```csharp
// ✅ GOOD: Use baggage sparingly
activity.SetBaggage("tenant.id", tenantId);  // Small, critical
activity.SetBaggage("correlation.id", correlationId);

// ❌ BAD: Don't put large data in baggage
activity.SetBaggage("user.profile", JsonSerializer.Serialize(largeProfile));
activity.SetBaggage("request.body", requestBody);

// ✅ GOOD: Use tags for large or non-critical data
activity.SetTag("user.profile", JsonSerializer.Serialize(profile));
activity.SetTag("request.body", requestBody);
```

## HTTP Header Propagation

### Tags (Not Propagated)
```
GET /api/orders HTTP/1.1
Host: service-b
traceparent: 00-trace123-span456-01
(tags are NOT in headers)
```

### Baggage (Propagated)
```
GET /api/orders HTTP/1.1
Host: service-b
traceparent: 00-trace123-span456-01
baggage: tenant.id=tenant-456,correlation.id=abc-123
(baggage IS in headers)
```

## OpenTelemetry Semantic Conventions

For standard attributes, prefer Tags with semantic convention names:

```csharp
// HTTP - use standard semantic conventions as Tags
activity.SetTag("http.request.method", "POST");
activity.SetTag("http.response.status_code", 201);
activity.SetTag("server.address", "api.example.com");

// User - custom tags
activity.SetTag("enduser.id", userId);
activity.SetTag("enduser.role", userRole);

// Database - standard tags
activity.SetTag("db.system", "postgresql");
activity.SetTag("db.name", "orders");
activity.SetTag("db.statement", "SELECT * FROM orders WHERE id = ?");
```

## Migration Checklist

When migrating from 2.x:

1. ✅ Replace `telemetry.Properties[key]` → `activity.SetTag(key, value)`
2. ✅ Replace `CallContext.LogicalSetData(key, value)` → `activity.SetBaggage(key, value)`
3. ⚠️ Review all context propagation - ensure using baggage for cross-service data
4. ⚠️ Minimize baggage usage - only critical correlation data
5. ✅ Use tags for all telemetry-only data

## See Also

- [activity-vs-telemetry.md](activity-vs-telemetry.md) - Activity fundamentals
- [api-reference/Activity/SetTag.md](../api-reference/Activity/SetTag.md) - SetTag API details
- [mappings/properties-to-tags.md](../mappings/properties-to-tags.md) - Properties mapping guide
- [common-scenarios/custom-dimensions.md](../common-scenarios/custom-dimensions.md) - Adding custom dimensions

## References

- **Activity.SetTag**: `System.Diagnostics.Activity`
- **Activity.SetBaggage**: `System.Diagnostics.Activity`
- **W3C Baggage**: https://www.w3.org/TR/baggage/
- **OpenTelemetry Baggage**: https://opentelemetry.io/docs/specs/otel/baggage/api/
