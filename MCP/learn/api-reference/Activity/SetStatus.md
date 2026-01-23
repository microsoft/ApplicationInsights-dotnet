---
title: Activity.SetStatus - Set Activity Success/Failure
category: api-reference
applies-to: 3.x
namespace: System.Diagnostics
related:
  - concepts/activity-status.md
  - mappings/success-to-status.md
  - api-reference/Activity/SetTag.md
source: System.Diagnostics.Activity (.NET BCL)
---

# Activity.SetStatus

## Signature

```csharp
namespace System.Diagnostics
{
    public class Activity
    {
        public Activity SetStatus(ActivityStatusCode code, string? description = null);
        
        public ActivityStatusCode Status { get; }
        public string? StatusDescription { get; }
    }
    
    public enum ActivityStatusCode
    {
        Unset = 0,
        Ok = 1,
        Error = 2
    }
}
```

## Description

Sets the status of an Activity, indicating whether the operation succeeded, failed, or is unset. This replaces the `Success` boolean property from 2.x telemetry.

## Parameters

- **code**: `ActivityStatusCode` - The status code (Unset, Ok, Error)
- **description**: `string?` - Optional description (typically used for Error status)

## Returns

Returns `this` Activity instance for method chaining.

## 2.x Equivalent

```csharp
// 2.x: Boolean success flag
request.Success = true;
dependency.Success = false;

// 3.x: Status code
activity.SetStatus(ActivityStatusCode.Ok);
activity.SetStatus(ActivityStatusCode.Error, "Payment failed");
```

## ActivityStatusCode Values

### Unset (0)
Default state. Status not explicitly set.

```csharp
// Default when Activity is created
var activity = activitySource.StartActivity("Operation");
// activity.Status == ActivityStatusCode.Unset
```

### Ok (1)
Operation completed successfully.

```csharp
activity.SetStatus(ActivityStatusCode.Ok);
// Maps to Success = true in AI
```

### Error (2)
Operation failed. Should include description.

```csharp
activity.SetStatus(ActivityStatusCode.Error, "Database connection failed");
// Maps to Success = false in AI
// StatusDescription becomes exception message
```

## Basic Usage

```csharp
var activity = Activity.Current;

try
{
    PerformOperation();
    activity?.SetStatus(ActivityStatusCode.Ok);
}
catch (Exception ex)
{
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    throw;
}
```

## Automatic Status Setting

Many instrumentation libraries set status automatically:

```csharp
// HTTP Client instrumentation
var response = await httpClient.GetAsync("https://api.example.com");
// Activity.Status set automatically:
// - 2xx → ActivityStatusCode.Ok
// - 4xx/5xx → ActivityStatusCode.Error
// - Exception → ActivityStatusCode.Error with description
```

## When to Set Status Manually

### Scenario 1: Custom Business Logic

```csharp
using (var activity = activitySource.StartActivity("ProcessPayment"))
{
    var result = await paymentService.ProcessAsync(payment);
    
    if (result.IsSuccess)
    {
        activity?.SetStatus(ActivityStatusCode.Ok);
    }
    else
    {
        // Business failure, not exception
        activity?.SetStatus(ActivityStatusCode.Error, result.ErrorMessage);
    }
}
```

### Scenario 2: Override Automatic Status

```csharp
try
{
    var response = await httpClient.GetAsync(url);
    
    // HTTP 404 is OK in this scenario
    if (response.StatusCode == HttpStatusCode.NotFound)
    {
        activity?.SetStatus(ActivityStatusCode.Ok);  // Override
    }
}
catch (Exception ex)
{
    activity?.SetStatus(ActivityStatusCode.Error, ex.ToString());
}
```

### Scenario 3: Validation Failures

```csharp
public async Task<IActionResult> CreateOrder([FromBody] OrderRequest request)
{
    var activity = Activity.Current;
    
    if (!ModelState.IsValid)
    {
        activity?.SetStatus(ActivityStatusCode.Error, "Validation failed");
        return BadRequest(ModelState);
    }
    
    // Continue processing...
    activity?.SetStatus(ActivityStatusCode.Ok);
    return Ok();
}
```

## Status Description Best Practices

```csharp
// ✅ GOOD: Concise error description
activity.SetStatus(ActivityStatusCode.Error, "Payment declined: Insufficient funds");

// ✅ GOOD: Exception message
activity.SetStatus(ActivityStatusCode.Error, ex.Message);

// ⚠️ AVOID: Full stack trace (use separately)
activity.SetStatus(ActivityStatusCode.Error, ex.ToString());  // Too verbose

// ⚠️ AVOID: Description for Ok status
activity.SetStatus(ActivityStatusCode.Ok, "Success");  // Unnecessary
```

## Integration with Exception Tracking

```csharp
try
{
    await ProcessOrderAsync(order);
    activity?.SetStatus(ActivityStatusCode.Ok);
}
catch (Exception ex)
{
    // Set status
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    
    // Record exception as separate event
    activity?.AddEvent(new ActivityEvent(
        "exception",
        tags: new ActivityTagsCollection
        {
            { "exception.type", ex.GetType().FullName },
            { "exception.message", ex.Message },
            { "exception.stacktrace", ex.StackTrace }
        }));
    
    throw;
}
```

## Mapping to Application Insights

| ActivityStatusCode | AI Success | AI ResultCode | Notes |
|-------------------|-----------|--------------|--------|
| `Unset` | `true` | (varies) | Default success |
| `Ok` | `true` | `200`, `0`, etc. | Explicit success |
| `Error` | `false` | `500`, error code | Explicit failure |

### Query in Application Insights

```kusto
requests
| extend status = customDimensions.['otel.status_code']
| extend statusDesc = customDimensions.['otel.status_description']
| where success == false
| project timestamp, name, success, status, statusDesc
```

## Real-World Example: Order Processing

```csharp
public async Task<OrderResult> ProcessOrderAsync(Order order)
{
    using (var activity = activitySource.StartActivity("ProcessOrder"))
    {
        activity?.SetTag("order.id", order.Id);
        activity?.SetTag("order.amount", order.Total);
        
        try
        {
            // Validate inventory
            if (!await ValidateInventoryAsync(order))
            {
                activity?.SetStatus(
                    ActivityStatusCode.Error, 
                    "Insufficient inventory");
                return OrderResult.Failed("Out of stock");
            }
            
            // Process payment
            var paymentResult = await paymentService.ChargeAsync(order);
            if (!paymentResult.Success)
            {
                activity?.SetStatus(
                    ActivityStatusCode.Error,
                    $"Payment failed: {paymentResult.Reason}");
                return OrderResult.Failed(paymentResult.Reason);
            }
            
            // Success
            activity?.SetStatus(ActivityStatusCode.Ok);
            activity?.SetTag("order.completed", true);
            return OrderResult.Success(order.Id);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
```

## HTTP Status Code Mapping

ASP.NET Core automatically sets ActivityStatusCode based on HTTP response:

| HTTP Status | ActivityStatusCode | Success |
|-------------|-------------------|---------|
| 200-299 | `Ok` | `true` |
| 300-399 | `Unset` | `true` |
| 400-499 | `Error` | `false` |
| 500-599 | `Error` | `false` |

Override if needed:

```csharp
public IActionResult GetUser(string userId)
{
    var activity = Activity.Current;
    var user = userService.FindById(userId);
    
    if (user == null)
    {
        // 404 is OK in this API - not an error
        activity?.SetStatus(ActivityStatusCode.Ok);
        activity?.SetTag("user.not_found", true);
        return NotFound();
    }
    
    return Ok(user);
}
```

## Status vs Tags

```csharp
// Status: Overall operation outcome
activity.SetStatus(ActivityStatusCode.Error, "Validation failed");

// Tags: Detailed context
activity.SetTag("validation.errors", errorCount);
activity.SetTag("validation.field", failedField);

// Both together provide complete picture
```

## Processor Pattern

```csharp
public class StatusEnrichmentProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Ensure status is set
        if (activity.Status == ActivityStatusCode.Unset)
        {
            // Default to Ok if no exception
            if (activity.GetTagItem("error") == null)
            {
                activity.SetStatus(ActivityStatusCode.Ok);
            }
        }
        
        // Add status as tag for easier querying
        activity.SetTag("operation.status", activity.Status.ToString());
        
        if (activity.Status == ActivityStatusCode.Error)
        {
            activity.SetTag("operation.error_description", 
                activity.StatusDescription);
        }
    }
}
```

## Method Chaining

```csharp
Activity.Current?
    .SetTag("order.id", orderId)
    .SetTag("order.amount", amount)
    .SetStatus(ActivityStatusCode.Ok);
```

## Common Mistakes

```csharp
// ❌ MISTAKE 1: Setting Ok status unnecessarily
activity.SetStatus(ActivityStatusCode.Ok);  // Usually automatic

// ❌ MISTAKE 2: Not providing description for errors
activity.SetStatus(ActivityStatusCode.Error);  // Should include description

// ❌ MISTAKE 3: Setting status on null activity
Activity.Current.SetStatus(ActivityStatusCode.Ok);  // May throw

// ❌ MISTAKE 4: Setting status multiple times inconsistently
activity.SetStatus(ActivityStatusCode.Ok);
// ... later ...
activity.SetStatus(ActivityStatusCode.Error);  // Last one wins

// ✅ CORRECT
Activity.Current?.SetStatus(ActivityStatusCode.Error, "Payment declined");
```

## Performance Considerations

✅ **Lightweight**: Status is just an enum + optional string
✅ **No allocation**: Unless description is provided
✅ **Fast**: No external calls

## See Also

- [concepts/activity-status.md](../../concepts/activity-status.md) - Complete status guide
- [mappings/success-to-status.md](../../mappings/success-to-status.md) - Migration from Success property
- [Activity.SetTag](SetTag.md) - Add custom dimensions
- [Activity.AddEvent](AddEvent.md) - Record exceptions

## References

- **Source**: `System.Diagnostics.Activity` (.NET BCL)
- **OpenTelemetry Status**: https://opentelemetry.io/docs/specs/otel/trace/api/#set-status
- **Activity Class**: https://learn.microsoft.com/dotnet/api/system.diagnostics.activity
