# Track Methods Signature Changes

**Category:** Breaking Change  
**Applies to:** TelemetryClient API  
**Severity:** Medium - Parameters removed

## What Changed

Several `TelemetryClient.Track*()` methods had their signatures simplified by removing parameters that are now handled differently in OpenTelemetry.

## Methods Affected

### TrackDependency

**Before (2.x):**
```csharp
void TrackDependency(
    string dependencyTypeName,
    string target,
    string dependencyName,
    string data,
    DateTimeOffset startTime,
    TimeSpan duration,
    string resultCode,
    bool success)
```

**After (3.x):**
```csharp
void TrackDependency(
    string dependencyTypeName,
    string target,
    string dependencyName,
    string data,
    DateTimeOffset startTime,
    TimeSpan duration,
    string resultCode,
    bool success)
```

Actually, in 3.x you should use `ActivitySource` instead:

```csharp
private static readonly ActivitySource ActivitySource = new("MyService");

using var activity = ActivitySource.StartActivity("DependencyName", ActivityKind.Client);
activity?.SetTag("db.system", "mssql");
activity?.SetTag("server.address", "myserver.database.windows.net");
activity?.SetTag("db.statement", "SELECT * FROM Orders");
// Duration tracked automatically
// Status set via SetStatus()
```

### TrackRequest

**Before (2.x):**
```csharp
void TrackRequest(
    string name,
    DateTimeOffset startTime,
    TimeSpan duration,
    string responseCode,
    bool success)
```

**After (3.x):**

Manual request tracking is rare in 3.x. ASP.NET Core creates Activities automatically. For manual cases:

```csharp
using var activity = ActivitySource.StartActivity("RequestName", ActivityKind.Server);
activity?.SetTag("http.request.method", "POST");
activity?.SetTag("url.path", "/api/orders");
activity?.SetTag("http.response.status_code", 200);
activity?.SetStatus(ActivityStatusCode.Ok);
// Duration tracked automatically
```

### TrackException

**Before (2.x):**
```csharp
void TrackException(
    Exception exception,
    IDictionary<string, string> properties = null,
    IDictionary<string, double> metrics = null)
```

**After (3.x):**

The `metrics` parameter is removed. Use separate metric tracking:

```csharp
// Exception tracking (no metrics parameter)
_telemetryClient.TrackException(exception, properties);

// Metrics tracked separately
_telemetryClient.GetMetric("ExceptionCount").TrackValue(1);
```

Or use Activity.RecordException():

```csharp
activity?.RecordException(exception);
activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
```

### TrackEvent

**Before (2.x):**
```csharp
void TrackEvent(
    string eventName,
    IDictionary<string, string> properties = null,
    IDictionary<string, double> metrics = null)
```

**After (3.x):**

The `metrics` parameter is removed:

```csharp
// Event tracking (no metrics parameter)
_telemetryClient.TrackEvent(eventName, properties);

// Metrics tracked separately
_telemetryClient.GetMetric("EventMetric").TrackValue(value);
```

## Migration Guide

### Dependency Tracking Migration

```csharp
// Before (2.x)
var startTime = DateTimeOffset.UtcNow;
try
{
    var result = await CallDatabaseAsync();
    var duration = DateTimeOffset.UtcNow - startTime;
    
    _telemetryClient.TrackDependency(
        "SQL",
        "mydb.database.windows.net",
        "GetOrders",
        "SELECT * FROM Orders",
        startTime,
        duration,
        "200",
        true);
}
catch (Exception ex)
{
    var duration = DateTimeOffset.UtcNow - startTime;
    _telemetryClient.TrackDependency(
        "SQL",
        "mydb.database.windows.net",
        "GetOrders",
        "SELECT * FROM Orders",
        startTime,
        duration,
        "500",
        false);
    throw;
}

// After (3.x)
using var activity = ActivitySource.StartActivity("GetOrders", ActivityKind.Client);
activity?.SetTag("db.system", "mssql");
activity?.SetTag("server.address", "mydb.database.windows.net");
activity?.SetTag("db.statement", "SELECT * FROM Orders");

try
{
    var result = await CallDatabaseAsync();
    activity?.SetStatus(ActivityStatusCode.Ok);
}
catch (Exception ex)
{
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    activity?.RecordException(ex);
    throw;
}
// Duration calculated automatically from activity start/stop
```

### Exception with Metrics Migration

```csharp
// Before (2.x)
try
{
    ProcessData();
}
catch (Exception ex)
{
    var properties = new Dictionary<string, string>
    {
        ["Operation"] = "ProcessData"
    };
    
    var metrics = new Dictionary<string, double>
    {
        ["RetryCount"] = 3,
        ["DataSize"] = 1024
    };
    
    _telemetryClient.TrackException(ex, properties, metrics);
}

// After (3.x)
try
{
    ProcessData();
}
catch (Exception ex)
{
    var properties = new Dictionary<string, string>
    {
        ["Operation"] = "ProcessData"
    };
    
    // Track exception (no metrics)
    _telemetryClient.TrackException(ex, properties);
    
    // Track metrics separately
    _telemetryClient.GetMetric("RetryCount").TrackValue(3);
    _telemetryClient.GetMetric("DataSize").TrackValue(1024);
}
```

### Event with Metrics Migration

```csharp
// Before (2.x)
_telemetryClient.TrackEvent(
    "OrderProcessed",
    new Dictionary<string, string> { ["OrderId"] = orderId },
    new Dictionary<string, double> { ["Amount"] = 99.99, ["ItemCount"] = 3 });

// After (3.x)
_telemetryClient.TrackEvent(
    "OrderProcessed",
    new Dictionary<string, string> { ["OrderId"] = orderId });

// Track metrics separately
_telemetryClient.GetMetric("OrderAmount").TrackValue(99.99);
_telemetryClient.GetMetric("OrderItemCount").TrackValue(3);
```

## Key Points

1. **Metrics Separated**: Metrics parameter removed from TrackException and TrackEvent
2. **Use ActivitySource**: Prefer ActivitySource over manual Track* methods for dependencies/requests
3. **Automatic Timing**: Activities track duration automatically
4. **Better Semantics**: Activity tags use OpenTelemetry semantic conventions

## Workaround for Existing Code

If you have extensive code using metrics parameter:

```csharp
public static class TelemetryClientExtensions
{
    public static void TrackExceptionWithMetrics(
        this TelemetryClient telemetryClient,
        Exception exception,
        IDictionary<string, string> properties = null,
        IDictionary<string, double> metrics = null)
    {
        telemetryClient.TrackException(exception, properties);
        
        if (metrics != null)
        {
            foreach (var metric in metrics)
            {
                telemetryClient.GetMetric(metric.Key).TrackValue(metric.Value);
            }
        }
    }
}
```

## See Also

- [StartOperation-removed.md](StartOperation-removed.md)
- [activity-source.md](../../opentelemetry-fundamentals/activity-source.md)
- [GetMetric-simplified.md](GetMetric-simplified.md)
