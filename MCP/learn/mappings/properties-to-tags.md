# Properties → Tags Mapping

**Category:** Mapping  
**Applies to:** Migration from Application Insights 2.x to 3.x  
**Related:** [SetTag.md](../api-reference/Activity/SetTag.md), [GetTagItem.md](../api-reference/Activity/GetTagItem.md)

## Overview

In Application Insights 2.x, custom data is stored in `Properties` (string dictionary) and `Metrics` (numeric dictionary). In 3.x with Activity, custom data is stored using **tags** via `Activity.SetTag()`.

## Core Mapping

| 2.x API | 3.x Activity API | Value Type |
|---------|------------------|------------|
| `telemetry.Properties[key] = value` | `activity.SetTag(key, value)` | Any type |
| `telemetry.Metrics[key] = value` | `activity.SetTag(key, value)` | Numeric types |
| `telemetry.Properties[key]` | `activity.GetTagItem(key) as string` | Retrieval |
| `telemetry.Context.GlobalProperties[key]` | Resource or Processor | Global values |

## Basic Usage

### 2.x: Properties Dictionary

```csharp
using Microsoft.ApplicationInsights.DataContracts;

// RequestTelemetry
var request = new RequestTelemetry();
request.Properties["userId"] = "12345";
request.Properties["orderType"] = "premium";
request.Metrics["itemCount"] = 42;

// DependencyTelemetry
var dependency = new DependencyTelemetry();
dependency.Properties["retryCount"] = "2";
dependency.Properties["cacheHit"] = "false";
```

### 3.x: Activity Tags

```csharp
using System.Diagnostics;

// Activity (any kind)
Activity.Current?.SetTag("userId", "12345");
Activity.Current?.SetTag("orderType", "premium");
Activity.Current?.SetTag("itemCount", 42); // Numeric value

Activity.Current?.SetTag("retryCount", 2); // Stored as integer
Activity.Current?.SetTag("cacheHit", false); // Stored as boolean
```

## Data Type Support

### 2.x: String-Only Properties

```csharp
// 2.x Properties dictionary only accepts strings
request.Properties["userId"] = "12345";
request.Properties["isActive"] = "true";  // Boolean as string
request.Properties["count"] = "42";       // Number as string

// Metrics dictionary for numeric values
request.Metrics["duration"] = 123.45;
request.Metrics["count"] = 42;
```

### 3.x: Multi-Type Tags

```csharp
// 3.x Activity tags support multiple types
activity.SetTag("userId", "12345");        // string
activity.SetTag("isActive", true);         // bool
activity.SetTag("count", 42);              // int
activity.SetTag("duration", 123.45);       // double
activity.SetTag("items", new[] { 1, 2, 3 }); // array
activity.SetTag("timestamp", DateTime.UtcNow); // DateTime

// Type preserved in OpenTelemetry, converted to string in Azure Monitor
```

## Reading Tag Values

### 2.x: Dictionary Access

```csharp
string userId = request.Properties["userId"];
double count = request.Metrics["itemCount"];

// Null check
if (request.Properties.TryGetValue("userId", out string userId))
{
    // Use userId
}
```

### 3.x: GetTagItem

```csharp
// GetTagItem returns object, cast to expected type
string userId = activity.GetTagItem("userId") as string;
int? count = activity.GetTagItem("itemCount") as int?;

// Null check
if (activity.GetTagItem("userId") is string userId)
{
    // Use userId
}

// Or pattern matching
if (activity.GetTagItem("count") is int count)
{
    // Use count as int
}
```

## Custom Dimensions Mapping

In Azure Monitor portal, both Properties and Tags appear as **Custom Dimensions**.

### 2.x Example

```csharp
var request = new RequestTelemetry
{
    Name = "GET /api/orders",
    Properties =
    {
        ["userId"] = "12345",
        ["orderType"] = "premium",
        ["region"] = "west-us"
    }
};
telemetryClient.TrackRequest(request);

// Azure Monitor Custom Dimensions:
// userId: "12345"
// orderType: "premium"
// region: "west-us"
```

### 3.x Example

```csharp
// Activity created by instrumentation
Activity.Current?.SetTag("userId", "12345");
Activity.Current?.SetTag("orderType", "premium");
Activity.Current?.SetTag("region", "west-us");

// Azure Monitor Custom Dimensions (identical result):
// userId: "12345"
// orderType: "premium"
// region: "west-us"
```

## Global Properties Migration

### 2.x: TelemetryConfiguration.GlobalProperties

```csharp
// Added to all telemetry items
TelemetryConfiguration.Active.TelemetryInitializers.Add(
    new ITelemetryInitializer
    {
        public void Initialize(ITelemetry telemetry)
        {
            telemetry.Context.GlobalProperties["environment"] = "production";
            telemetry.Context.GlobalProperties["version"] = "1.2.3";
        }
    });
```

### 3.x Option 1: Resource (Preferred for Constants)

```csharp
// Set once at startup via Resource
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.ConfigureResource(resource =>
            resource.AddAttributes(new[]
            {
                new KeyValuePair<string, object>("environment", "production"),
                new KeyValuePair<string, object>("version", "1.2.3")
            }));
    });

// Result: Added to ALL telemetry (traces, metrics, logs) automatically
```

### 3.x Option 2: Processor (For Dynamic Values)

```csharp
public class GlobalPropertiesProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        activity.SetTag("environment", "production");
        activity.SetTag("version", GetCurrentVersion());
    }
}

services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.AddProcessor(new GlobalPropertiesProcessor());
    });
```

## Semantic Conventions

OpenTelemetry defines **semantic conventions** for common attributes. Use these instead of custom names when applicable.

### HTTP Attributes

**2.x:**
```csharp
request.Properties["httpMethod"] = "GET";
request.Properties["requestPath"] = "/api/orders";
request.Properties["statusCode"] = "200";
```

**3.x (Semantic Conventions):**
```csharp
// Automatically set by instrumentation
activity.SetTag("http.request.method", "GET");
activity.SetTag("url.path", "/api/orders");
activity.SetTag("http.response.status_code", 200);
```

### User/Session Attributes

**2.x:**
```csharp
request.Context.User.Id = "user123";
request.Context.Session.Id = "session456";
```

**3.x (Semantic Conventions):**
```csharp
activity.SetTag("enduser.id", "user123");
activity.SetTag("session.id", "session456");
```

### Database Attributes

**2.x:**
```csharp
dependency.Properties["sqlCommand"] = "SELECT * FROM Users";
dependency.Properties["database"] = "MyDatabase";
```

**3.x (Semantic Conventions):**
```csharp
activity.SetTag("db.statement", "SELECT * FROM Users");
activity.SetTag("db.name", "MyDatabase");
activity.SetTag("db.system", "postgresql");
```

## Real-World Migration Examples

### Example 1: User Context Enrichment

**2.x:**
```csharp
public class UserContextInitializer : ITelemetryInitializer
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public void Initialize(ITelemetry telemetry)
    {
        var userId = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
        if (!string.IsNullOrEmpty(userId))
        {
            telemetry.Context.User.Id = userId;
            telemetry.Properties["userRole"] = 
                _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.Role)?.Value;
        }
    }
}
```

**3.x:**
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
        var userId = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
        if (!string.IsNullOrEmpty(userId))
        {
            activity.SetTag("enduser.id", userId);
            activity.SetTag("user.role", 
                _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.Role)?.Value);
        }
    }
}
```

### Example 2: Custom Business Metrics

**2.x:**
```csharp
// From: ApplicationInsightsDemo/Controllers/HomeController.cs
public IActionResult ProcessOrder(Order order)
{
    var request = new RequestTelemetry();
    request.Properties["orderId"] = order.Id.ToString();
    request.Properties["customerId"] = order.CustomerId.ToString();
    request.Metrics["orderAmount"] = order.TotalAmount;
    request.Metrics["itemCount"] = order.Items.Count;
    
    telemetryClient.TrackRequest(request);
    return Ok();
}
```

**3.x:**
```csharp
public IActionResult ProcessOrder(Order order)
{
    // Activity automatically created by ASP.NET Core
    Activity.Current?.SetTag("order.id", order.Id);
    Activity.Current?.SetTag("customer.id", order.CustomerId);
    Activity.Current?.SetTag("order.amount", order.TotalAmount);
    Activity.Current?.SetTag("order.item_count", order.Items.Count);
    
    return Ok();
}
```

### Example 3: Dependency Enrichment with Retry Info

**2.x:**
```csharp
public class RetryEnricher : ITelemetryProcessor
{
    private ITelemetryProcessor _next;
    
    public void Process(ITelemetry telemetry)
    {
        if (telemetry is DependencyTelemetry dependency)
        {
            if (dependency.Properties.ContainsKey("retry-after"))
            {
                dependency.Properties["wasRetried"] = "true";
            }
        }
        _next.Process(telemetry);
    }
}
```

**3.x:**
```csharp
public class RetryEnricher : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (activity.Kind == ActivityKind.Client)
        {
            if (activity.GetTagItem("retry-after") != null)
            {
                activity.SetTag("was_retried", true);
            }
        }
    }
}
```

## Tag Naming Conventions

### 2.x Style (camelCase, any format)
```csharp
request.Properties["userId"] = "123";
request.Properties["OrderType"] = "premium";
request.Properties["my-custom-field"] = "value";
```

### 3.x Style (OpenTelemetry conventions: lowercase with dots/underscores)
```csharp
activity.SetTag("user.id", "123");           // Preferred: semantic convention
activity.SetTag("order.type", "premium");     // Preferred: namespace.attribute
activity.SetTag("my_custom_field", "value");  // Acceptable: snake_case
```

**Recommendation:** Follow [OpenTelemetry semantic conventions](https://opentelemetry.io/docs/specs/semconv/) when available, use lowercase with dots/underscores for custom attributes.

## Performance Considerations

### 2.x: String Conversion Overhead

```csharp
// 2.x: All values converted to strings
request.Properties["count"] = count.ToString();        // Boxing + ToString
request.Metrics["duration"] = duration.TotalMilliseconds; // Separate dictionary
```

### 3.x: Native Type Support

```csharp
// 3.x: Native types preserved until export
activity.SetTag("count", count);           // No boxing or conversion
activity.SetTag("duration", duration.TotalMilliseconds); // Same dictionary
```

## Array and Object Tags

### Array Tags
```csharp
// 3.x supports array values
activity.SetTag("user.roles", new[] { "admin", "user" });
activity.SetTag("order.item_ids", new[] { 1, 2, 3 });

// Azure Monitor: Serialized as JSON array in Custom Dimensions
// Custom Dimensions: user.roles = ["admin","user"]
```

### Complex Objects
```csharp
// Not recommended: Complex objects are ToString()'ed
activity.SetTag("order", order); // Results in "OrderNamespace.Order"

// Recommended: Serialize complex objects explicitly
activity.SetTag("order.json", JsonSerializer.Serialize(order));
// Custom Dimensions: order.json = "{\"id\":123,\"amount\":99.99}"
```

## See Also

- [SetTag.md](../api-reference/Activity/SetTag.md) - SetTag API reference
- [GetTagItem.md](../api-reference/Activity/GetTagItem.md) - GetTagItem API reference
- [telemetry-to-activity.md](./telemetry-to-activity.md) - Telemetry type mapping
- [custom-dimensions.md](../common-scenarios/adding-custom-dimensions.md) - Custom dimensions scenario
- [OpenTelemetry Semantic Conventions](https://opentelemetry.io/docs/specs/semconv/)
