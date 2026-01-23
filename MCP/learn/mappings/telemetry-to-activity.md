# 2.x Telemetry Types → 3.x Activity Mapping

**Category:** Mapping  
**Applies to:** Migration from Application Insights 2.x to 3.x  
**Related:** [activity-vs-telemetry.md](../concepts/activity-vs-telemetry.md), [activity-kinds.md](../concepts/activity-kinds.md)

## Overview

In Application Insights 2.x, telemetry is represented by specific classes (RequestTelemetry, DependencyTelemetry, etc.). In 3.x, **all distributed tracing telemetry uses the standard .NET `Activity` class** from System.Diagnostics.

The Activity's `Kind` property determines which 2.x telemetry type it maps to in Azure Monitor.

## Core Mapping

| 2.x Telemetry Type | 3.x Representation | Activity.Kind | Azure Monitor Type |
|-------------------|-------------------|---------------|-------------------|
| `RequestTelemetry` | `Activity` | `ActivityKind.Server` | Request |
| `DependencyTelemetry` | `Activity` | `ActivityKind.Client` | Dependency |
| `DependencyTelemetry` | `Activity` | `ActivityKind.Producer` | Dependency |
| `DependencyTelemetry` | `Activity` | `ActivityKind.Consumer` | Dependency (or Request) |
| `DependencyTelemetry` | `Activity` | `ActivityKind.Internal` | Dependency |
| `TraceTelemetry` | `ILogger` | N/A | Trace |
| `ExceptionTelemetry` | `ILogger` / `Activity` | N/A | Exception |
| `MetricTelemetry` | `Meter` | N/A | Metric |
| `EventTelemetry` | `ILogger` / `Activity.AddEvent` | N/A | Event |

## Detailed Mappings

### RequestTelemetry → Activity (Kind = Server)

**2.x:**
```csharp
using Microsoft.ApplicationInsights.DataContracts;

var request = new RequestTelemetry
{
    Name = "GET /api/users",
    ResponseCode = "200",
    Success = true,
    Duration = TimeSpan.FromMilliseconds(245),
    Url = new Uri("https://example.com/api/users"),
    Properties = { ["userId"] = "123" },
    Context = 
    {
        User = { Id = "user123" },
        Session = { Id = "session456" }
    }
};
client.TrackRequest(request);
```

**3.x:**
```csharp
using System.Diagnostics;

// Activity automatically created by ASP.NET Core instrumentation
// Activity.Current is available in controller/middleware

Activity.Current.DisplayName = "GET /api/users";
Activity.Current.SetStatus(ActivityStatusCode.Ok);
Activity.Current.SetTag("userId", "123");
Activity.Current.SetTag("enduser.id", "user123");
Activity.Current.SetTag("session.id", "session456");

// Kind is automatically set to ActivityKind.Server by instrumentation
// Duration tracked automatically from Activity.StartTimeUtc to Activity.StopTimeUtc
// HTTP status code automatically captured as "http.response.status_code"
```

### DependencyTelemetry → Activity (Kind = Client)

**2.x:**
```csharp
using Microsoft.ApplicationInsights.DataContracts;

var dependency = new DependencyTelemetry
{
    Name = "GET https://api.example.com/data",
    Type = "Http",
    Data = "https://api.example.com/data",
    Target = "api.example.com",
    Success = true,
    ResultCode = "200",
    Duration = TimeSpan.FromMilliseconds(123),
    Properties = { ["retry.count"] = "2" }
};
client.TrackDependency(dependency);
```

**3.x:**
```csharp
using System.Diagnostics;

// Activity automatically created by HttpClient instrumentation
// Activity created inside HttpClient.SendAsync

var activity = Activity.Current; // Available during HttpClient call
activity.SetTag("retry.count", "2");

// Kind automatically set to ActivityKind.Client by instrumentation
// Target automatically captured as "server.address"
// Type automatically set based on protocol (HTTP/SQL/Azure/etc.)
// ResultCode automatically captured as "http.response.status_code"
```

### TraceTelemetry → ILogger

**2.x:**
```csharp
using Microsoft.ApplicationInsights.DataContracts;

client.TrackTrace("User logged in", SeverityLevel.Information, 
    new Dictionary<string, string> { ["userId"] = "123" });
```

**3.x:**
```csharp
using Microsoft.Extensions.Logging;

// ILogger automatically sends to Application Insights
logger.LogInformation("User logged in. UserId: {UserId}", "123");

// Alternative: Activity.AddEvent for trace-level events
Activity.Current?.AddEvent(new ActivityEvent("User logged in", 
    tags: new ActivityTagsCollection { ["userId"] = "123" }));
```

### ExceptionTelemetry → ILogger + Activity

**2.x:**
```csharp
using Microsoft.ApplicationInsights.DataContracts;

try
{
    // code
}
catch (Exception ex)
{
    var exceptionTelemetry = new ExceptionTelemetry(ex)
    {
        SeverityLevel = SeverityLevel.Error,
        Properties = { ["userId"] = "123" }
    };
    client.TrackException(exceptionTelemetry);
}
```

**3.x:**
```csharp
using Microsoft.Extensions.Logging;

try
{
    // code
}
catch (Exception ex)
{
    // ILogger captures exception
    logger.LogError(ex, "Operation failed. UserId: {UserId}", "123");
    
    // Activity also records exception
    Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);
    Activity.Current?.RecordException(ex);
}
```

## Property Mappings

### RequestTelemetry Properties

| 2.x Property | 3.x Activity Equivalent | Notes |
|--------------|------------------------|-------|
| `Name` | `Activity.DisplayName` | Operation name |
| `ResponseCode` | `Activity.GetTagItem("http.response.status_code")` | HTTP status |
| `Success` | `Activity.Status == ActivityStatusCode.Ok` | Boolean → StatusCode |
| `Duration` | `Activity.Duration` | Auto-tracked |
| `Url` | `Activity.GetTagItem("url.full")` | Full URL |
| `Properties[key]` | `Activity.SetTag(key, value)` | Custom properties |
| `Context.User.Id` | `Activity.SetTag("enduser.id", userId)` | User identifier |
| `Context.Session.Id` | `Activity.SetTag("session.id", sessionId)` | Session identifier |
| `Context.Cloud.RoleName` | Resource: `service.name` | Set via IResourceDetector |

### DependencyTelemetry Properties

| 2.x Property | 3.x Activity Equivalent | Notes |
|--------------|------------------------|-------|
| `Name` | `Activity.DisplayName` | Operation name |
| `Type` | `Activity.GetTagItem("db.system")` or `"http"` | Dependency type |
| `Target` | `Activity.GetTagItem("server.address")` | Target server |
| `Data` | `Activity.GetTagItem("db.statement")` or `"url.full"` | Command/URL |
| `Success` | `Activity.Status == ActivityStatusCode.Ok` | Boolean → StatusCode |
| `ResultCode` | `Activity.GetTagItem("http.response.status_code")` | Result code |
| `Duration` | `Activity.Duration` | Auto-tracked |
| `Properties[key]` | `Activity.SetTag(key, value)` | Custom properties |

## ActivityKind Details

### Server → Request

```csharp
// Automatically created by ASP.NET Core
if (activity.Kind == ActivityKind.Server)
{
    // This will appear as a Request in Azure Monitor
    // Automatically captures:
    // - http.request.method
    // - url.path, url.query, url.scheme
    // - http.response.status_code
    // - server.address, server.port
}
```

### Client → Dependency

```csharp
// Automatically created by HttpClient instrumentation
if (activity.Kind == ActivityKind.Client)
{
    // This will appear as a Dependency in Azure Monitor
    // Type automatically determined from instrumentation:
    // - HTTP calls → "Http"
    // - SQL queries → "SQL"
    // - Azure Storage → "Azure blob", "Azure table", etc.
}
```

### Internal → Dependency

```csharp
// Manually created activities for internal operations
using var activity = activitySource.StartActivity("ProcessOrder", ActivityKind.Internal);

// Appears as Dependency with Type = "InProc"
// Useful for tracking internal operations in application map
```

### Producer/Consumer → Dependency

```csharp
// Producer (message sender)
using var activity = activitySource.StartActivity("SendMessage", ActivityKind.Producer);
// Appears as Dependency with Type = "Queue Message | Azure Service Bus" (or similar)

// Consumer (message receiver)
using var activity = activitySource.StartActivity("ProcessMessage", ActivityKind.Consumer);
// Can appear as Request or Dependency depending on instrumentation configuration
```

## Type Determination

Activity **Kind** determines the Azure Monitor telemetry type, but additional semantic conventions determine the **Type** field:

### HTTP Dependencies

```csharp
activity.Kind = ActivityKind.Client;
activity.SetTag("http.request.method", "GET");
// Azure Monitor Type = "Http"
```

### Database Dependencies

```csharp
activity.Kind = ActivityKind.Client;
activity.SetTag("db.system", "postgresql");
// Azure Monitor Type = "SQL"
```

### Azure Storage Dependencies

```csharp
activity.Kind = ActivityKind.Client;
activity.SetTag("az.namespace", "Microsoft.Storage");
activity.SetTag("server.address", "mystorageaccount.blob.core.windows.net");
// Azure Monitor Type = "Azure blob"
```

## Real-World Migration Example

### 2.x: Manual Telemetry Tracking

```csharp
// From: ApplicationInsightsDemo/Controllers/HomeController.cs
public async Task<IActionResult> Index()
{
    var sw = Stopwatch.StartNew();
    
    try
    {
        var data = await httpClient.GetStringAsync("https://api.example.com/data");
        
        telemetryClient.TrackDependency(new DependencyTelemetry
        {
            Name = "GET /data",
            Type = "Http",
            Data = "https://api.example.com/data",
            Target = "api.example.com",
            Success = true,
            Duration = sw.Elapsed
        });
        
        return View();
    }
    catch (Exception ex)
    {
        telemetryClient.TrackException(ex);
        throw;
    }
}
```

### 3.x: Automatic Instrumentation

```csharp
// HttpClient instrumentation automatically creates Activity
public async Task<IActionResult> Index()
{
    // Activity automatically created by HttpClient instrumentation
    var data = await httpClient.GetStringAsync("https://api.example.com/data");
    
    // Exception automatically recorded on Activity by ASP.NET Core instrumentation
    // No manual telemetry tracking needed!
    
    return View();
}

// Optional: Enrich with custom data
public async Task<IActionResult> Index()
{
    var data = await httpClient.GetStringAsync("https://api.example.com/data");
    
    // Current Activity represents the HttpClient call
    // Can enrich during or after the call (in OnEnd processor)
    Activity.Current?.SetTag("custom.attribute", "value");
    
    return View();
}
```

## See Also

- [activity-vs-telemetry.md](../concepts/activity-vs-telemetry.md) - Core concept explanation
- [activity-kinds.md](../concepts/activity-kinds.md) - ActivityKind details
- [properties-to-tags.md](./properties-to-tags.md) - Property system mapping
- [context-to-resource.md](./context-to-resource.md) - TelemetryContext mapping
- [OpenTelemetry Semantic Conventions](https://opentelemetry.io/docs/specs/semconv/)
