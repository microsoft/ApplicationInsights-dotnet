# Duration Property → Activity.Duration Mapping

**Category:** Mapping  
**Applies to:** Migration from Application Insights 2.x to 3.x  
**Related:** [activity-vs-telemetry.md](../concepts/activity-vs-telemetry.md)

## Overview

In Application Insights 2.x, telemetry duration must be **manually tracked** and set on the Duration property. In 3.x, Activity **automatically tracks duration** from start to stop, eliminating manual timing code.

## Core Mapping

| 2.x Approach | 3.x Activity | Timing |
|--------------|--------------|--------|
| `telemetry.Duration = TimeSpan` (manual) | `Activity.Duration` (automatic) | Calculated from StartTimeUtc to stop |
| `Stopwatch` + manual calculation | Built-in timing | No Stopwatch needed |
| Set in `finally` block | Read after Activity.Stop() | Automatic |

## Basic Usage

### 2.x: Manual Duration Tracking

```csharp
using Microsoft.ApplicationInsights.DataContracts;
using System.Diagnostics;

// RequestTelemetry
var sw = Stopwatch.StartNew();
try
{
    // Operation
}
finally
{
    var request = new RequestTelemetry
    {
        Name = "GET /api/users",
        Duration = sw.Elapsed  // Manual calculation
    };
    telemetryClient.TrackRequest(request);
}

// DependencyTelemetry
sw.Restart();
try
{
    await httpClient.GetAsync("https://api.example.com");
}
finally
{
    var dependency = new DependencyTelemetry
    {
        Name = "GET /api",
        Duration = sw.Elapsed  // Manual calculation
    };
    telemetryClient.TrackDependency(dependency);
}
```

### 3.x: Automatic Duration Tracking

```csharp
using System.Diagnostics;

// Activity automatically tracks duration
using var activity = activitySource.StartActivity("MyOperation");

// Operation code here

// Duration automatically calculated when Activity.Dispose() is called
// No Stopwatch, no manual timing needed!

// Read duration (after stop):
TimeSpan duration = activity.Duration;
```

## How Activity Timing Works

### Lifecycle

```csharp
// Start
var activity = activitySource.StartActivity("Operation");
// activity.StartTimeUtc = DateTime.UtcNow (recorded)

// ... operation executes ...

// Stop
activity.Stop(); // or activity.Dispose()
// activity.Duration = (DateTime.UtcNow - activity.StartTimeUtc)
```

### Properties

```csharp
activity.StartTimeUtc  // DateTime when activity started
activity.Duration      // TimeSpan from start to stop (auto-calculated)

// No end time property - calculate if needed:
DateTime endTime = activity.StartTimeUtc + activity.Duration;
```

## Migration Examples

### Example 1: Request Duration

**2.x:**
```csharp
public async Task<IActionResult> ProcessOrder(Order order)
{
    var sw = Stopwatch.StartNew();
    bool success = false;
    
    try
    {
        await orderService.ProcessAsync(order);
        success = true;
        return Ok();
    }
    catch
    {
        return StatusCode(500);
    }
    finally
    {
        telemetryClient.TrackRequest(new RequestTelemetry
        {
            Name = "POST /api/orders",
            Duration = sw.Elapsed,  // Manual timing
            Success = success
        });
    }
}
```

**3.x:**
```csharp
public async Task<IActionResult> ProcessOrder(Order order)
{
    // ASP.NET Core automatically creates Activity
    // Duration tracked automatically from request start to response sent
    
    await orderService.ProcessAsync(order);
    return Ok();
    
    // Activity.Duration available in OnEnd processor if needed
}
```

### Example 2: Dependency Duration

**2.x:**
```csharp
public async Task<string> FetchDataAsync(string url)
{
    var sw = Stopwatch.StartNew();
    bool success = false;
    
    try
    {
        var response = await httpClient.GetAsync(url);
        success = response.IsSuccessStatusCode;
        return await response.Content.ReadAsStringAsync();
    }
    finally
    {
        telemetryClient.TrackDependency(new DependencyTelemetry
        {
            Name = $"GET {url}",
            Type = "Http",
            Duration = sw.Elapsed,  // Manual timing
            Success = success
        });
    }
}
```

**3.x:**
```csharp
public async Task<string> FetchDataAsync(string url)
{
    // HttpClient instrumentation automatically creates Activity
    // Duration tracked automatically from request start to response received
    
    var response = await httpClient.GetAsync(url);
    return await response.Content.ReadAsStringAsync();
    
    // No manual timing needed!
}
```

### Example 3: Custom Operation Duration

**2.x:**
```csharp
public void ProcessBatch(List<Item> items)
{
    var sw = Stopwatch.StartNew();
    
    foreach (var item in items)
    {
        ProcessItem(item);
    }
    
    telemetryClient.TrackDependency(new DependencyTelemetry
    {
        Name = "BatchProcess",
        Type = "InProc",
        Duration = sw.Elapsed,  // Manual timing
        Success = true
    });
}
```

**3.x:**
```csharp
public void ProcessBatch(List<Item> items)
{
    // Create Activity for custom operation
    using var activity = activitySource.StartActivity("BatchProcess", 
        ActivityKind.Internal);
    
    foreach (var item in items)
    {
        ProcessItem(item);
    }
    
    // Duration automatically tracked when using-block exits
    // Available in OnEnd processor: activity.Duration
}
```

## Reading Duration in Processors

### OnStart: Duration is Zero

```csharp
public class MyProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        // Activity just started
        Console.WriteLine(activity.Duration); // 00:00:00 (zero)
        
        // StartTimeUtc is available
        Console.WriteLine(activity.StartTimeUtc); // Actual start time
    }
}
```

### OnEnd: Duration is Available

```csharp
public class DurationProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Activity completed, duration calculated
        Console.WriteLine(activity.Duration); // Actual duration
        
        // Example: Log slow operations
        if (activity.Duration.TotalSeconds > 5)
        {
            activity.SetTag("slow_operation", true);
            logger.LogWarning("Slow operation: {Name} took {Duration}s", 
                activity.DisplayName, activity.Duration.TotalSeconds);
        }
    }
}
```

## Azure Monitor Mapping

Activity Duration maps directly to Duration field in Azure Monitor:

```csharp
// Activity
activity.StartTimeUtc = 2024-01-15 10:00:00.000 UTC
activity.Duration = 00:00:01.234 (1.234 seconds)

// Azure Monitor
timestamp = 2024-01-15 10:00:00.000
duration = 1234 (milliseconds)
```

**Note:** Azure Monitor stores duration in **milliseconds**, Activity uses **TimeSpan**.

## Performance Comparison

### 2.x: Manual Timing Overhead

```csharp
// Every operation requires:
// 1. Stopwatch allocation
// 2. Manual start
// 3. Manual stop
// 4. Duration calculation
// 5. Telemetry creation with duration

var sw = Stopwatch.StartNew();  // Allocation + start
try
{
    // Operation
}
finally
{
    var duration = sw.Elapsed;  // Stop + calculation
    telemetryClient.TrackRequest(new RequestTelemetry
    {
        Duration = duration  // Manual assignment
    });
}
```

### 3.x: Built-in Timing

```csharp
// Activity timing is built-in:
// 1. StartTimeUtc recorded automatically
// 2. Duration calculated automatically on stop
// 3. No extra allocations

using var activity = activitySource.StartActivity("Operation");
// Operation
// Duration automatically available
```

**Efficiency:** Activity timing is built into the framework, no additional allocations or manual tracking required.

## Sub-Operation Timing

### 2.x: Nested Timing with Multiple Stopwatches

```csharp
public async Task ProcessOrderAsync(Order order)
{
    var swTotal = Stopwatch.StartNew();
    
    // Validate
    var swValidate = Stopwatch.StartNew();
    await ValidateAsync(order);
    telemetryClient.TrackDependency(new DependencyTelemetry
    {
        Name = "Validate",
        Duration = swValidate.Elapsed
    });
    
    // Process
    var swProcess = Stopwatch.StartNew();
    await ProcessAsync(order);
    telemetryClient.TrackDependency(new DependencyTelemetry
    {
        Name = "Process",
        Duration = swProcess.Elapsed
    });
    
    telemetryClient.TrackRequest(new RequestTelemetry
    {
        Name = "ProcessOrder",
        Duration = swTotal.Elapsed
    });
}
```

### 3.x: Nested Activities with Automatic Timing

```csharp
public async Task ProcessOrderAsync(Order order)
{
    // Parent activity automatically created by ASP.NET Core
    
    // Child activity 1
    using (var validateActivity = activitySource.StartActivity("Validate"))
    {
        await ValidateAsync(order);
        // Duration automatically tracked
    }
    
    // Child activity 2
    using (var processActivity = activitySource.StartActivity("Process"))
    {
        await ProcessAsync(order);
        // Duration automatically tracked
    }
    
    // Parent activity duration includes all child activities
    // All durations automatic, hierarchical relationship preserved
}
```

## Real-World Example: From ApplicationInsightsDemo

**2.x:**
```csharp
// From: ApplicationInsightsDemo/Controllers/HomeController.cs
public async Task<IActionResult> ComplexOperation()
{
    var swTotal = Stopwatch.StartNew();
    
    try
    {
        // Step 1: Database query
        var swDb = Stopwatch.StartNew();
        var data = await dbContext.Users.ToListAsync();
        _telemetryClient.TrackDependency(new DependencyTelemetry
        {
            Name = "Query Users",
            Type = "SQL",
            Duration = swDb.Elapsed,
            Success = true
        });
        
        // Step 2: External API call
        var swApi = Stopwatch.StartNew();
        var enrichedData = await httpClient.PostAsJsonAsync("https://api.example.com/enrich", data);
        _telemetryClient.TrackDependency(new DependencyTelemetry
        {
            Name = "POST /enrich",
            Type = "Http",
            Duration = swApi.Elapsed,
            Success = enrichedData.IsSuccessStatusCode
        });
        
        return Ok(enrichedData);
    }
    finally
    {
        _telemetryClient.TrackRequest(new RequestTelemetry
        {
            Name = "POST /api/complex",
            Duration = swTotal.Elapsed,
            Success = true
        });
    }
}
```

**3.x:**
```csharp
public async Task<IActionResult> ComplexOperation()
{
    // All timing automatic via instrumentation
    
    // Step 1: EF Core instrumentation creates Activity automatically
    var data = await dbContext.Users.ToListAsync();
    // Activity.Duration tracked automatically
    
    // Step 2: HttpClient instrumentation creates Activity automatically
    var enrichedData = await httpClient.PostAsJsonAsync("https://api.example.com/enrich", data);
    // Activity.Duration tracked automatically
    
    // Parent Activity (ASP.NET Core) tracks total duration automatically
    return Ok(enrichedData);
}

// No Stopwatch instances needed!
// No manual timing code!
// All durations tracked automatically with hierarchical relationships!
```

## Custom Duration Logic

If you need to report a different duration than actual execution time:

```csharp
public class CustomDurationProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Activity.Duration is read-only, but you can:
        
        // 1. Add custom duration tag
        activity.SetTag("custom.duration.ms", CalculateCustomDuration());
        
        // 2. Or modify start time (affects duration calculation)
        // Not recommended - Activity timing should reflect actual execution
        
        // Azure Monitor will use Activity.Duration for built-in duration field
    }
}
```

## See Also

- [activity-vs-telemetry.md](../concepts/activity-vs-telemetry.md) - Activity concept
- [telemetry-to-activity.md](./telemetry-to-activity.md) - Telemetry type mapping
- [Activity.StartTimeUtc](../api-reference/Activity/StartTimeUtc.md) - Start time property
- [Activity.Duration](../api-reference/Activity/Duration.md) - Duration property
