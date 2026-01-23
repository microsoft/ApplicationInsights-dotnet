---
title: Activity.SetTag - Add Custom Dimensions
category: api-reference
applies-to: 3.x
namespace: System.Diagnostics
related:
  - concepts/activity-tags-vs-baggage.md
  - mappings/properties-to-tags.md
  - api-reference/Activity/GetTagItem.md
source: System.Diagnostics.Activity (.NET BCL)
---

# Activity.SetTag

## Signature

```csharp
namespace System.Diagnostics
{
    public class Activity
    {
        public Activity? SetTag(string key, object? value);
    }
}
```

## Description

Sets a tag (key-value pair) on the current Activity. Tags appear as **customDimensions** in Application Insights and are **local to this Activity** (not propagated to children).

## Parameters

- **key**: `string` - The tag name. Use lowercase with dots (e.g., `"user.id"`, `"order.amount"`)
- **value**: `object?` - The tag value. Can be string, int, bool, double, array, or null

## Returns

Returns `this` Activity instance for method chaining, or `null` if the Activity is null.

## 2.x Equivalent

```csharp
// 2.x: Set custom dimension
telemetry.Properties["user.id"] = "12345";
telemetry.Properties["order.amount"] = "199.99";

// 3.x: Set tag
activity?.SetTag("user.id", "12345");
activity?.SetTag("order.amount", 199.99);
```

## Basic Usage

```csharp
var activity = Activity.Current;

// String value
activity?.SetTag("user.id", "12345");

// Numeric value
activity?.SetTag("order.amount", 199.99);
activity?.SetTag("item.count", 5);

// Boolean value
activity?.SetTag("is.premium", true);

// Null value (removes tag)
activity?.SetTag("optional.field", null);
```

## Method Chaining

```csharp
Activity.Current?
    .SetTag("user.id", userId)
    .SetTag("tenant.id", tenantId)
    .SetTag("order.amount", amount);
```

## Supported Value Types

| .NET Type | Example | AI CustomDimension |
|-----------|---------|-------------------|
| string | `"value"` | `"value"` |
| int | `123` | `"123"` |
| long | `123L` | `"123"` |
| double | `99.99` | `"99.99"` |
| bool | `true` | `"True"` |
| DateTime | `DateTime.UtcNow` | `"2024-01-15T10:30:00Z"` |
| array | `new[] { "a", "b" }` | `["a","b"]` (JSON) |
| null | `null` | (tag removed) |

## Semantic Conventions

Follow OpenTelemetry semantic conventions for standard attributes:

```csharp
// HTTP attributes
activity.SetTag("http.request.method", "POST");
activity.SetTag("http.response.status_code", 201);
activity.SetTag("url.full", "https://api.example.com/orders");
activity.SetTag("server.address", "api.example.com");
activity.SetTag("server.port", 443);

// User attributes
activity.SetTag("enduser.id", userId);
activity.SetTag("enduser.role", "admin");

// Database attributes
activity.SetTag("db.system", "postgresql");
activity.SetTag("db.name", "orders");
activity.SetTag("db.statement", "SELECT * FROM orders WHERE id = ?");
activity.SetTag("db.operation", "SELECT");

// Cloud attributes
activity.SetTag("cloud.provider", "azure");
activity.SetTag("cloud.region", "eastus");
```

## Custom Business Attributes

```csharp
// Order processing
activity.SetTag("order.id", orderId);
activity.SetTag("order.amount", totalAmount);
activity.SetTag("order.item_count", items.Count);
activity.SetTag("order.currency", "USD");

// User context
activity.SetTag("user.id", User.Identity.Name);
activity.SetTag("user.tenant", tenantId);
activity.SetTag("user.subscription_tier", "premium");

// Feature flags
activity.SetTag("feature.new_checkout", featureFlags.NewCheckout);
activity.SetTag("experiment.variant", experimentVariant);
```

## Array Values

```csharp
// String array
activity.SetTag("order.items", new[] { "item1", "item2", "item3" });
// AI: "order.items": ["item1","item2","item3"]

// Number array
activity.SetTag("response.sizes", new[] { 1024, 2048, 4096 });
// AI: "response.sizes": [1024,2048,4096]
```

## Complex Objects (Serialization)

```csharp
using System.Text.Json;

// Serialize complex object
var orderDetails = new { Id = orderId, Total = amount, Items = itemCount };
activity.SetTag("order.details", JsonSerializer.Serialize(orderDetails));
// AI: "order.details": "{\"Id\":\"123\",\"Total\":199.99,\"Items\":5}"

// ⚠️ Keep serialized data reasonably small
```

## Updating Existing Tags

```csharp
// Tags can be overwritten
activity.SetTag("status", "pending");
// ... later ...
activity.SetTag("status", "completed");  // Overwrites previous value
```

## Removing Tags

```csharp
// Set to null to remove
activity.SetTag("temporary.field", "value");
activity.SetTag("temporary.field", null);  // Removed
```

## Tag Visibility in Application Insights

```csharp
activity.SetTag("user.id", "12345");
activity.SetTag("order.amount", 199.99);
```

**Application Insights Portal:**
```
requests
| where name == "POST /api/orders"
| extend userId = customDimensions.['user.id']
| extend orderAmount = customDimensions.['order.amount']
```

## Real-World Example: ASP.NET Core Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] OrderRequest request)
    {
        var activity = Activity.Current;
        
        // Enrich with business context
        activity?.SetTag("user.id", User.Identity.Name);
        activity?.SetTag("tenant.id", GetTenantId());
        activity?.SetTag("order.currency", request.Currency);
        activity?.SetTag("order.item_count", request.Items.Count);
        activity?.SetTag("order.total_amount", request.Total);
        
        try
        {
            var orderId = await orderService.CreateAsync(request);
            
            activity?.SetTag("order.id", orderId);
            activity?.SetTag("order.created", true);
            
            return Ok(new { orderId });
        }
        catch (PaymentException ex)
        {
            activity?.SetTag("payment.error", ex.Message);
            activity?.SetTag("payment.code", ex.Code);
            throw;
        }
    }
}
```

## Performance Considerations

✅ **Fast**: Tags are stored in-memory, no external calls
✅ **Efficient**: Only serialized when exported
⚠️ **Size**: Keep individual tag values reasonable (< 8KB)
⚠️ **Count**: Hundreds of tags per Activity are fine, thousands may impact performance

## Best Practices

```csharp
// ✅ GOOD: Lowercase with dots
activity.SetTag("user.id", userId);
activity.SetTag("order.total_amount", amount);

// ❌ AVOID: PascalCase or mixed case (not semantic convention)
activity.SetTag("UserId", userId);
activity.SetTag("OrderTotal", amount);

// ✅ GOOD: Typed values
activity.SetTag("order.amount", 199.99);  // double
activity.SetTag("item.count", 5);         // int

// ❌ AVOID: Everything as string
activity.SetTag("order.amount", "199.99");  // loses type
activity.SetTag("item.count", "5");         // loses type

// ✅ GOOD: Check for null
Activity.Current?.SetTag("user.id", userId);

// ❌ BAD: NullReferenceException risk
Activity.Current.SetTag("user.id", userId);  // throws if null
```

## Tags vs Baggage

| Feature | SetTag | SetBaggage |
|---------|--------|-----------|
| **Propagated?** | ❌ No | ✅ Yes |
| **Use for** | Telemetry data | Correlation data |
| **Visible in AI** | ✅ Yes | ⚠️ Only if copied to tag |
| **Size limit** | ~10MB | ~8KB total |
| **Performance** | Fast | Slower (propagation) |

```csharp
// Local telemetry data → Tag
activity.SetTag("http.response.status_code", 200);

// Cross-service correlation → Baggage
activity.SetBaggage("correlation.id", correlationId);
```

## Common Mistakes

```csharp
// ❌ MISTAKE 1: Using baggage for telemetry
activity.SetBaggage("order.amount", amount);  // Wrong! Use SetTag

// ❌ MISTAKE 2: Serializing everything
activity.SetTag("request", JsonSerializer.Serialize(largeRequest));  // Too big!

// ❌ MISTAKE 3: Forgetting null check
Activity.Current.SetTag("user.id", userId);  // May throw NullReferenceException

// ❌ MISTAKE 4: Using PascalCase (inconsistent with OpenTelemetry)
activity.SetTag("OrderId", orderId);  // Use "order.id" instead

// ✅ CORRECT
Activity.Current?.SetTag("order.id", orderId);
```

## See Also

- [Activity.GetTagItem](GetTagItem.md) - Read tag values
- [Activity.SetBaggage](SetBaggage.md) - Set propagated context
- [concepts/activity-tags-vs-baggage.md](../../concepts/activity-tags-vs-baggage.md) - Tags vs Baggage guide
- [mappings/properties-to-tags.md](../../mappings/properties-to-tags.md) - Migration from Properties

## References

- **Source**: `System.Diagnostics.Activity` (.NET BCL)
- **Semantic Conventions**: https://opentelemetry.io/docs/specs/semconv/
- **Activity Class**: https://learn.microsoft.com/dotnet/api/system.diagnostics.activity
