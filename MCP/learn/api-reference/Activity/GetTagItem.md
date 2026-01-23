---
title: Activity.GetTagItem - Read Tag Values
category: api-reference
applies-to: 3.x
namespace: System.Diagnostics
related:
  - api-reference/Activity/SetTag.md
  - concepts/activity-tags-vs-baggage.md
source: System.Diagnostics.Activity (.NET BCL)
---

# Activity.GetTagItem

## Signature

```csharp
namespace System.Diagnostics
{
    public class Activity
    {
        public object? GetTagItem(string key);
        
        // Enumerate all tags
        public IEnumerable<KeyValuePair<string, object?>> TagObjects { get; }
    }
}
```

## Description

Retrieves the value of a tag by key. Returns `null` if the tag doesn't exist.

## Parameters

- **key**: `string` - The tag name to retrieve

## Returns

`object?` - The tag value, or `null` if not found

## Basic Usage

```csharp
var activity = Activity.Current;

// Get a tag value
var userId = activity?.GetTagItem("user.id");

// Check if tag exists
if (activity?.GetTagItem("order.id") != null)
{
    // Tag exists
}
```

## Type Casting

```csharp
// String tag
var userId = activity?.GetTagItem("user.id") as string;

// Numeric tags
var statusCode = activity?.GetTagItem("http.response.status_code");
if (statusCode is int code)
{
    Console.WriteLine($"Status: {code}");
}

// Boolean tags
var isPremium = activity?.GetTagItem("user.is_premium");
if (isPremium is bool premium && premium)
{
    Console.WriteLine("Premium user");
}

// Safe conversion
var amount = Convert.ToDouble(activity?.GetTagItem("order.amount"));
```

## Enumerate All Tags

```csharp
var activity = Activity.Current;

// Iterate all tags
foreach (var tag in activity.TagObjects)
{
    Console.WriteLine($"{tag.Key} = {tag.Value}");
}

// Filter tags
var customTags = activity.TagObjects
    .Where(t => t.Key.StartsWith("custom."))
    .ToDictionary(t => t.Key, t => t.Value);
```

## Real-World Example: Activity Processor

```csharp
public class UserContextProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Read existing tag
        var userId = activity.GetTagItem("user.id") as string;
        
        if (!string.IsNullOrEmpty(userId))
        {
            // Enrich with additional user data
            var userTier = GetUserTier(userId);
            activity.SetTag("user.tier", userTier);
            
            var userRegion = GetUserRegion(userId);
            activity.SetTag("user.region", userRegion);
        }
    }
    
    private string GetUserTier(string userId)
    {
        // Lookup logic
        return "premium";
    }
    
    private string GetUserRegion(string userId)
    {
        return "us-east";
    }
}
```

## Conditional Logic Based on Tags

```csharp
public class SensitiveDataProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Check if this is a payment operation
        var operationName = activity.DisplayName;
        if (operationName.Contains("Payment") || 
            activity.GetTagItem("payment.id") != null)
        {
            // Redact sensitive data
            RedactCreditCard(activity);
        }
    }
    
    private void RedactCreditCard(Activity activity)
    {
        var cardNumber = activity.GetTagItem("payment.card_number") as string;
        if (!string.IsNullOrEmpty(cardNumber) && cardNumber.Length > 4)
        {
            // Keep only last 4 digits
            var redacted = "****" + cardNumber.Substring(cardNumber.Length - 4);
            activity.SetTag("payment.card_number", redacted);
        }
    }
}
```

## Read HTTP Tags

```csharp
var activity = Activity.Current;

// HTTP method
var method = activity?.GetTagItem("http.request.method") as string;

// HTTP status code
var statusCode = activity?.GetTagItem("http.response.status_code");
if (statusCode is int code)
{
    if (code >= 500)
    {
        // Server error
    }
}

// URL path
var path = activity?.GetTagItem("url.path") as string;

// User agent
var userAgent = activity?.GetTagItem("http.request.header.user_agent") as string;
```

## Pattern: Conditional Enrichment

```csharp
public class ConditionalEnrichmentProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Only enrich API requests
        if (activity.Kind == ActivityKind.Server)
        {
            var path = activity.GetTagItem("url.path") as string;
            
            if (path?.StartsWith("/api/") == true)
            {
                // Add API-specific tags
                activity.SetTag("api.version", "v2");
                activity.SetTag("api.category", GetApiCategory(path));
            }
        }
    }
}
```

## Pattern: Tag-Based Filtering

```csharp
public class HealthCheckFilterProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Check if this is a health check request
        var path = activity.GetTagItem("url.path") as string;
        var userAgent = activity.GetTagItem("http.request.header.user_agent") as string;
        
        if (path == "/health" || path == "/healthz" ||
            userAgent?.Contains("HealthCheck") == true)
        {
            // Drop health checks
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
        }
    }
}
```

## Pattern: Copy Tags from Parent

```csharp
public class TenantPropagationProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        // Check if parent has tenant tag
        var parent = activity.Parent;
        var tenantId = parent?.GetTagItem("tenant.id");
        
        if (tenantId != null && activity.GetTagItem("tenant.id") == null)
        {
            // Copy tenant from parent
            activity.SetTag("tenant.id", tenantId);
        }
    }
}
```

## Null Safety

```csharp
// ❌ BAD: NullReferenceException risk
var userId = Activity.Current.GetTagItem("user.id");

// ✅ GOOD: Null-safe
var userId = Activity.Current?.GetTagItem("user.id");

// ✅ GOOD: With type check
if (Activity.Current?.GetTagItem("user.id") is string id)
{
    // Safe to use 'id'
}
```

## Tag Existence Check

```csharp
var activity = Activity.Current;

// Check if tag exists
bool hasUserId = activity?.GetTagItem("user.id") != null;

// Check with type
bool hasPremiumFlag = activity?.GetTagItem("is.premium") is bool;

// Get with default
var timeout = activity?.GetTagItem("request.timeout") as int? ?? 30;
```

## Performance Considerations

✅ **Fast**: O(1) hashtable lookup
✅ **No allocation**: Unless boxing/unboxing occurs
⚠️ **Enumeration**: `TagObjects` enumerates all tags - cache if needed

```csharp
// ✅ GOOD: Single lookup
var userId = activity.GetTagItem("user.id");

// ⚠️ AVOID: Multiple lookups in loop
for (int i = 0; i < 1000; i++)
{
    var userId = activity.GetTagItem("user.id");  // Lookup every iteration
}

// ✅ BETTER: Cache the value
var userId = activity.GetTagItem("user.id");
for (int i = 0; i < 1000; i++)
{
    // Use cached userId
}
```

## Common Patterns

### Pattern 1: Read, Modify, Write

```csharp
var count = activity.GetTagItem("request.count") as int? ?? 0;
activity.SetTag("request.count", count + 1);
```

### Pattern 2: Conditional Add

```csharp
if (activity.GetTagItem("user.id") == null)
{
    activity.SetTag("user.id", GetCurrentUserId());
}
```

### Pattern 3: Tag Migration

```csharp
// Migrate old tag name to new name
var oldValue = activity.GetTagItem("old.tag.name");
if (oldValue != null)
{
    activity.SetTag("new.tag.name", oldValue);
    activity.SetTag("old.tag.name", null);  // Remove old
}
```

## See Also

- [Activity.SetTag](SetTag.md) - Set tag values
- [concepts/activity-tags-vs-baggage.md](../../concepts/activity-tags-vs-baggage.md) - Tags vs Baggage
- [Activity.GetBaggageItem](GetBaggageItem.md) - Read baggage values

## References

- **Source**: `System.Diagnostics.Activity` (.NET BCL)
- **Activity Class**: https://learn.microsoft.com/dotnet/api/system.diagnostics.activity
