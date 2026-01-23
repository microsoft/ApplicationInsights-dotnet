---
title: ActivityStatusCode - Success vs Error
category: concept
applies-to: 3.x
related:
  - concepts/activity-vs-telemetry.md
  - concepts/activity-processor.md
  - api-reference/Activity/SetStatus.md
source: System.Diagnostics.ActivityStatusCode
---

# ActivityStatusCode - Success vs Error

## Overview

In Application Insights 3.x, `ActivityStatusCode` replaces the 2.x `bool Success` property for indicating whether an operation succeeded or failed. It provides more nuanced status reporting with three states instead of two.

## In 2.x: Boolean Success Property

```csharp
// 2.x - Simple true/false
var request = new RequestTelemetry
{
    Success = true,  // or false
    ResponseCode = "200"
};

var dependency = new DependencyTelemetry
{
    Success = false,
    ResultCode = "500"
};

// In initializer
public void Initialize(ITelemetry telemetry)
{
    if (telemetry is RequestTelemetry req)
    {
        if (req.ResponseCode == "404")
        {
            req.Success = true;  // Override - 404 not an error
        }
    }
}
```

## In 3.x: ActivityStatusCode Enum

```csharp
// Source: System.Diagnostics.ActivityStatusCode
public enum ActivityStatusCode
{
    Unset = 0,  // Status not explicitly set
    Ok = 1,     // Success (was Success = true)
    Error = 2   // Failure (was Success = false)
}

// Setting status
var activity = Activity.Current;
activity.SetStatus(ActivityStatusCode.Ok);        // Success
activity.SetStatus(ActivityStatusCode.Error);     // Failure
activity.SetStatus(ActivityStatusCode.Unset);     // Clear/default

// Optional: Include description with Error
activity.SetStatus(ActivityStatusCode.Error, "Database connection failed");

// Checking status
if (activity.Status == ActivityStatusCode.Ok) { }
if (activity.Status == ActivityStatusCode.Error) { }
```

## StatusCode Values Explained

### Unset (0) - Default/Not Set
- Default value when Activity is created
- Means status hasn't been explicitly determined yet
- Instrumentation may infer success/failure from other signals (HTTP status code, exception, etc.)
- **Generally treated as success** if no errors occurred

```csharp
var activity = new Activity("operation");
activity.Start();
// activity.Status == ActivityStatusCode.Unset (default)
activity.Stop();
// If no exceptions and no explicit SetStatus, remains Unset (treated as success)
```

### Ok (1) - Success
- Explicitly indicates successful completion
- Maps to `Success = true` in 2.x
- Operation completed without errors

```csharp
activity.SetStatus(ActivityStatusCode.Ok);
// Maps to Success = true in Application Insights
```

### Error (2) - Failure
- Explicitly indicates failure
- Maps to `Success = false` in 2.x
- Optional description provides error context

```csharp
try
{
    // operation
    activity.SetStatus(ActivityStatusCode.Ok);
}
catch (Exception ex)
{
    activity.SetStatus(ActivityStatusCode.Error, ex.Message);
    throw;
}
// Maps to Success = false in Application Insights
```

## Automatic Status Setting

Many instrumentations set status automatically:

### ASP.NET Core (Server activities)
```csharp
// Automatic status based on HTTP response
200-299 → Unset (success)
400-499 → Unset (client error, not server failure)
500-599 → Error (server error)
Unhandled exception → Error
```

### HttpClient (Client activities)
```csharp
// Automatic status based on HTTP response
200-299 → Unset (success)
400-599 → Error (any HTTP error)
Network exception → Error
```

### SqlClient (Client activities)
```csharp
// Automatic status
Query successful → Unset
Exception → Error
```

## Common Patterns in Activity Processors

### Pattern 1: Override Status Based on Business Logic

```csharp
// Similar to 2.x pattern of setting Success = true for 404
public class ClientErrorProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (activity.Kind == ActivityKind.Server)
        {
            var statusCode = activity.GetTagItem("http.response.status_code");
            if (statusCode != null && int.TryParse(statusCode.ToString(), out int code))
            {
                // Treat 4xx as success (client errors, not our fault)
                if (code >= 400 && code < 500)
                {
                    activity.SetStatus(ActivityStatusCode.Ok);
                    activity.SetTag("status.overridden", "4xx_to_success");
                }
            }
        }
    }
}
```

### Pattern 2: Set Status Based on Custom Conditions

```csharp
public class BusinessRuleProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        var businessError = activity.GetTagItem("business.error");
        if (businessError != null && bool.Parse(businessError.ToString()))
        {
            // Mark as error even if HTTP was 200
            activity.SetStatus(ActivityStatusCode.Error, "Business rule violation");
        }
    }
}
```

### Pattern 3: Filter Based on Status

```csharp
public class SuccessfulCallsFilterProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Drop all successful client calls to reduce volume
        if (activity.Kind == ActivityKind.Client)
        {
            // Check both Ok and Unset (both mean success)
            if (activity.Status == ActivityStatusCode.Ok || 
                activity.Status == ActivityStatusCode.Unset)
            {
                activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
            }
        }
    }
}
```

### Pattern 4: Enrich Errors with Context

```csharp
public class ErrorEnrichmentProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (activity.Status == ActivityStatusCode.Error)
        {
            // Add tags for all errors
            activity.SetTag("error.captured", true);
            activity.SetTag("error.timestamp", DateTimeOffset.UtcNow);
            activity.SetTag("error.environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
            
            // Parse status description for keywords
            var description = activity.StatusDescription;
            if (!string.IsNullOrEmpty(description))
            {
                if (description.Contains("timeout", StringComparison.OrdinalIgnoreCase))
                {
                    activity.SetTag("error.type", "timeout");
                }
                else if (description.Contains("connection", StringComparison.OrdinalIgnoreCase))
                {
                    activity.SetTag("error.type", "connection");
                }
            }
        }
    }
}
```

## Migration from 2.x

### 2.x: Setting Success

```csharp
// 2.x
telemetry.Success = true;   // Success
telemetry.Success = false;  // Failure

// In initializer
if (request.ResponseCode == "404")
{
    request.Success = true;
}
```

### 3.x: Setting Status

```csharp
// 3.x
activity.SetStatus(ActivityStatusCode.Ok);      // Success
activity.SetStatus(ActivityStatusCode.Error);   // Failure

// In processor
var statusCode = activity.GetTagItem("http.response.status_code");
if (statusCode?.ToString() == "404")
{
    activity.SetStatus(ActivityStatusCode.Ok);
}
```

### 2.x: Checking Success

```csharp
// 2.x
if (request.Success == true) { }
if (!dependency.Success) { }
```

### 3.x: Checking Status

```csharp
// 3.x - Check for success (Ok or Unset)
if (activity.Status == ActivityStatusCode.Ok || 
    activity.Status == ActivityStatusCode.Unset)
{
    // Success
}

// Check for failure
if (activity.Status == ActivityStatusCode.Error)
{
    // Failure
}
```

## Relationship with HTTP Status Codes

### Best Practice Mapping

| HTTP Status | ActivityStatusCode | Reasoning |
|-------------|-------------------|-----------|
| 200-299 | Unset or Ok | Success |
| 300-399 | Unset | Redirect (success) |
| 400-499 | Unset or Ok | Client error (not server failure) |
| 500-599 | Error | Server error |
| Network error | Error | Failed to connect |

### Reading HTTP Status from Activity

```csharp
var statusCodeTag = activity.GetTagItem("http.response.status_code");
if (statusCodeTag != null && int.TryParse(statusCodeTag.ToString(), out int httpStatus))
{
    // HTTP status is available
    if (httpStatus >= 500)
    {
        activity.SetStatus(ActivityStatusCode.Error);
    }
}
```

## Status Description

ActivityStatusCode.Error can include an optional description:

```csharp
// With description (recommended for errors)
activity.SetStatus(ActivityStatusCode.Error, "Connection timeout after 30s");

// Reading description
if (activity.Status == ActivityStatusCode.Error)
{
    var description = activity.StatusDescription;
    // Use description for diagnostics
}
```

**Important**: StatusDescription is **not sent to Application Insights** as a separate field but may be included in error details.

## Mapping to Application Insights

| ActivityStatusCode | AI Success Field | Impact |
|-------------------|------------------|---------|
| `Unset` | `true` | Success (default inference) |
| `Ok` | `true` | Explicit success |
| `Error` | `false` | Failure |

## Real Example: Error Handling

```csharp
// Source: Example pattern from OpenTelemetry docs
public class OrderProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Check for exceptions recorded
        var exceptionEvents = activity.Events
            .Where(e => e.Name == "exception")
            .ToList();
            
        if (exceptionEvents.Any())
        {
            // Exception occurred, mark as error if not already
            if (activity.Status != ActivityStatusCode.Error)
            {
                activity.SetStatus(ActivityStatusCode.Error, "Exception occurred during processing");
            }
        }
    }
}
```

## See Also

- [activity-vs-telemetry.md](activity-vs-telemetry.md) - Activity fundamentals
- [activity-processor.md](activity-processor.md) - Processing activities
- [activity-kinds.md](activity-kinds.md) - Activity kinds
- [api-reference/Activity/SetStatus.md](../api-reference/Activity/SetStatus.md) - SetStatus API details

## References

- **System.Diagnostics.Activity**: .NET BCL
- **OpenTelemetry Status**: https://opentelemetry.io/docs/specs/otel/trace/api/#set-status
