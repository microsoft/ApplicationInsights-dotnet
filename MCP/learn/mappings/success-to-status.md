# Success Property → ActivityStatusCode Mapping

**Category:** Mapping  
**Applies to:** Migration from Application Insights 2.x to 3.x  
**Related:** [activity-status.md](../concepts/activity-status.md), [SetStatus.md](../api-reference/Activity/SetStatus.md)

## Overview

In Application Insights 2.x, telemetry has a boolean `Success` property indicating operation success/failure. In 3.x with Activity, this is replaced by `ActivityStatusCode` enum with three states: **Ok**, **Error**, and **Unset**.

## Core Mapping

| 2.x Success | 3.x ActivityStatusCode | Meaning |
|-------------|------------------------|---------|
| `true` | `ActivityStatusCode.Ok` | Operation succeeded |
| `false` | `ActivityStatusCode.Error` | Operation failed |
| Not set | `ActivityStatusCode.Unset` | Status not explicitly set |

## Basic Usage

### 2.x: Boolean Success

```csharp
using Microsoft.ApplicationInsights.DataContracts;

// RequestTelemetry
var request = new RequestTelemetry
{
    Name = "GET /api/users",
    Success = true,  // Boolean
    ResponseCode = "200"
};

// DependencyTelemetry
var dependency = new DependencyTelemetry
{
    Name = "GET https://api.example.com",
    Success = false,  // Boolean
    ResultCode = "500"
};
```

### 3.x: ActivityStatusCode Enum

```csharp
using System.Diagnostics;

// Activity (any kind)
Activity.Current?.SetStatus(ActivityStatusCode.Ok);
Activity.Current?.SetStatus(ActivityStatusCode.Error, "Operation failed");

// Or check status
if (Activity.Current?.Status == ActivityStatusCode.Error)
{
    // Handle error
}
```

## Status States Explained

### ActivityStatusCode.Ok
- **Meaning:** Operation completed successfully
- **When to use:** Explicit success confirmation
- **Example:** HTTP 200-299 responses, successful database queries

```csharp
Activity.Current?.SetStatus(ActivityStatusCode.Ok);
```

### ActivityStatusCode.Error
- **Meaning:** Operation failed
- **When to use:** Any error condition
- **Example:** HTTP 400+ responses, exceptions, timeouts
- **Includes description:** Error message/details

```csharp
Activity.Current?.SetStatus(ActivityStatusCode.Error, "Database connection failed");
```

### ActivityStatusCode.Unset
- **Meaning:** Status not explicitly set
- **When to use:** Default state, let instrumentation determine
- **Example:** Operation in progress, status unknown

```csharp
// Default state - no need to set explicitly
// Instrumentation will set based on outcome
```

## Automatic Status Setting

Most instrumentations **automatically set status** based on operation outcome:

### ASP.NET Core (Server Activities)

```csharp
// Automatically set by ASP.NET Core instrumentation:
// - HTTP 200-399: ActivityStatusCode.Unset (not explicitly set)
// - HTTP 400-499: ActivityStatusCode.Unset (client error, not server error)
// - HTTP 500-599: ActivityStatusCode.Error
// - Exception thrown: ActivityStatusCode.Error

// Azure Monitor mapping:
// - HTTP 200-399: Success = true
// - HTTP 400-599: Success = false
// - Exception: Success = false
```

### HttpClient (Client Activities)

```csharp
// Automatically set by HttpClient instrumentation:
// - HTTP 200-399: ActivityStatusCode.Unset
// - HTTP 400+: ActivityStatusCode.Error
// - Exception: ActivityStatusCode.Error

// Azure Monitor mapping:
// - HTTP 200-399: Success = true
// - HTTP 400+: Success = false
```

### SQL Client (Client Activities)

```csharp
// Automatically set by SQL instrumentation:
// - Query success: ActivityStatusCode.Unset
// - Query exception: ActivityStatusCode.Error

// Azure Monitor mapping:
// - Success: Success = true
// - Exception: Success = false
```

## Manual Status Override

You can override automatic status in a processor:

```csharp
public class CustomStatusProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Override status based on custom logic
        if (activity.Kind == ActivityKind.Server)
        {
            var statusCode = activity.GetTagItem("http.response.status_code") as int?;
            
            // Treat 404 as success (not error)
            if (statusCode == 404)
            {
                activity.SetStatus(ActivityStatusCode.Ok);
            }
            
            // Treat specific 200 responses as errors based on body
            if (statusCode == 200)
            {
                var errorFlag = activity.GetTagItem("response.contains.error") as bool?;
                if (errorFlag == true)
                {
                    activity.SetStatus(ActivityStatusCode.Error, "Business logic error");
                }
            }
        }
    }
}
```

## Migration Examples

### Example 1: Request Success Tracking

**2.x:**
```csharp
public IActionResult ProcessOrder(Order order)
{
    var sw = Stopwatch.StartNew();
    bool success = false;
    
    try
    {
        orderService.Process(order);
        success = true;
        return Ok();
    }
    catch (Exception ex)
    {
        telemetryClient.TrackException(ex);
        return StatusCode(500);
    }
    finally
    {
        telemetryClient.TrackRequest(new RequestTelemetry
        {
            Name = "POST /api/orders",
            Success = success,
            Duration = sw.Elapsed
        });
    }
}
```

**3.x:**
```csharp
public IActionResult ProcessOrder(Order order)
{
    try
    {
        orderService.Process(order);
        
        // ASP.NET Core automatically creates Activity and sets status
        // based on response status code (200 = success)
        return Ok();
    }
    catch (Exception ex)
    {
        // Activity.Status automatically set to Error
        // Exception automatically recorded
        logger.LogError(ex, "Order processing failed");
        return StatusCode(500);
    }
}

// No manual telemetry tracking needed!
```

### Example 2: Dependency Success Tracking

**2.x:**
```csharp
public async Task<string> FetchDataAsync(string url)
{
    var sw = Stopwatch.StartNew();
    bool success = false;
    string resultCode = null;
    
    try
    {
        var response = await httpClient.GetAsync(url);
        resultCode = ((int)response.StatusCode).ToString();
        success = response.IsSuccessStatusCode;
        
        var content = await response.Content.ReadAsStringAsync();
        return content;
    }
    catch (Exception ex)
    {
        telemetryClient.TrackException(ex);
        resultCode = "0";
        throw;
    }
    finally
    {
        telemetryClient.TrackDependency(new DependencyTelemetry
        {
            Name = $"GET {url}",
            Type = "Http",
            Success = success,
            ResultCode = resultCode,
            Duration = sw.Elapsed
        });
    }
}
```

**3.x:**
```csharp
public async Task<string> FetchDataAsync(string url)
{
    // HttpClient instrumentation automatically creates Activity
    // Status automatically set based on HTTP response
    var response = await httpClient.GetAsync(url);
    
    // Automatically tracked:
    // - ActivityKind.Client
    // - http.response.status_code
    // - Status: Ok for 2xx, Error for 4xx/5xx
    
    return await response.Content.ReadAsStringAsync();
}

// No manual telemetry tracking needed!
```

### Example 3: Custom Success Logic

**2.x:**
```csharp
public class CustomSuccessProcessor : ITelemetryProcessor
{
    private ITelemetryProcessor _next;
    
    public void Process(ITelemetry telemetry)
    {
        if (telemetry is RequestTelemetry request)
        {
            // Custom success logic: 404 is not a failure
            if (request.ResponseCode == "404")
            {
                request.Success = true;
            }
            
            // Business logic error: 200 with error flag is failure
            if (request.ResponseCode == "200" && 
                request.Properties.ContainsKey("business-error"))
            {
                request.Success = false;
            }
        }
        
        _next.Process(telemetry);
    }
}
```

**3.x:**
```csharp
public class CustomStatusProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (activity.Kind == ActivityKind.Server)
        {
            var statusCode = activity.GetTagItem("http.response.status_code") as int?;
            
            // Custom success logic: 404 is not a failure
            if (statusCode == 404)
            {
                activity.SetStatus(ActivityStatusCode.Ok);
            }
            
            // Business logic error: 200 with error flag is failure
            if (statusCode == 200 && 
                activity.GetTagItem("business-error") != null)
            {
                activity.SetStatus(ActivityStatusCode.Error, "Business logic error");
            }
        }
    }
}
```

## Status with Error Description

### 2.x: Success + Exception

```csharp
var request = new RequestTelemetry
{
    Success = false
};
telemetryClient.TrackException(new ExceptionTelemetry(ex));
```

### 3.x: Status + Description

```csharp
// Status includes error description
Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);

// Or record exception separately
Activity.Current?.SetStatus(ActivityStatusCode.Error, "Operation failed");
Activity.Current?.RecordException(ex);
```

## Azure Monitor Mapping

Activity Status is mapped to Success field in Azure Monitor:

| ActivityStatusCode | StatusDescription | Azure Monitor Success | Azure Monitor Details |
|-------------------|-------------------|----------------------|----------------------|
| `Ok` | Any | `true` | Explicit success |
| `Error` | "Timeout" | `false` | Error with description |
| `Error` | null | `false` | Error without description |
| `Unset` | N/A | `true` (if HTTP 2xx-3xx) | Inferred from response code |
| `Unset` | N/A | `false` (if HTTP 4xx-5xx) | Inferred from response code |

## Real-World Example: From ApplicationInsightsDemo

**2.x (ApplicationInsightsDemo):**
```csharp
// From: ApplicationInsightsDemo/Controllers/HomeController.cs
public async Task<IActionResult> CallExternalApi()
{
    var sw = Stopwatch.StartNew();
    var dependency = new DependencyTelemetry
    {
        Name = "External API Call",
        Type = "Http",
        Target = "api.example.com",
        Timestamp = DateTimeOffset.UtcNow
    };
    
    try
    {
        var response = await _httpClient.GetAsync("https://api.example.com/data");
        dependency.ResultCode = ((int)response.StatusCode).ToString();
        dependency.Success = response.IsSuccessStatusCode;
        
        var content = await response.Content.ReadAsStringAsync();
        return Content(content);
    }
    catch (Exception ex)
    {
        dependency.Success = false;
        _telemetryClient.TrackException(ex);
        throw;
    }
    finally
    {
        dependency.Duration = sw.Elapsed;
        _telemetryClient.TrackDependency(dependency);
    }
}
```

**3.x (Migrated):**
```csharp
public async Task<IActionResult> CallExternalApi()
{
    // HttpClient instrumentation handles everything automatically
    var response = await _httpClient.GetAsync("https://api.example.com/data");
    
    // Activity automatically created with:
    // - ActivityKind.Client
    // - http.request.method = GET
    // - server.address = api.example.com
    // - http.response.status_code = {status}
    // - Status = Error (if 4xx/5xx) or Unset (if 2xx/3xx)
    // - Duration tracked automatically
    
    var content = await response.Content.ReadAsStringAsync();
    return Content(content);
}

// No manual telemetry tracking required!
// Exception handling automatic via ASP.NET Core instrumentation
```

## Status Decision Tree

```
Is operation complete?
├─ Yes
│  └─ Did it succeed?
│     ├─ Yes → ActivityStatusCode.Ok
│     └─ No → ActivityStatusCode.Error (+ description)
└─ No (still running)
   └─ ActivityStatusCode.Unset (default)
```

## Best Practices

1. **Let instrumentation set status automatically** - Most scenarios are handled
2. **Use ActivityStatusCode.Error with description** - Provides context
3. **Override in OnEnd processor** - Not during operation
4. **Unset means "let instrumentation decide"** - Not "unknown error"

## See Also

- [activity-status.md](../concepts/activity-status.md) - Status concept guide
- [SetStatus.md](../api-reference/Activity/SetStatus.md) - SetStatus API reference
- [telemetry-to-activity.md](./telemetry-to-activity.md) - Telemetry type mapping
- [RecordException.md](../api-reference/Activity/RecordException.md) - Exception recording
