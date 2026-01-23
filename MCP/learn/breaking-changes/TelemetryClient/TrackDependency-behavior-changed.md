# TrackDependency Behavior Changed

**Category:** Breaking Change  
**Applies to:** TelemetryClient API  
**Migration Effort:** Medium  
**Related:** [TrackRequest-behavior-changed.md](TrackRequest-behavior-changed.md), [activity-source.md](../../opentelemetry-fundamentals/activity-source.md)

## Change Summary

`TelemetryClient.TrackDependency()` still exists in 3.x but behaves differently. The method creates an `Activity` instead of `DependencyTelemetry`. Some properties are no longer supported, and timing/correlation works differently.

## Behavior Changes

| Aspect | 2.x Behavior | 3.x Behavior |
|--------|--------------|--------------|
| **Return Type** | `void` | `void` |
| **Object Created** | `DependencyTelemetry` | `Activity` (Kind=Client) |
| **Timing** | Must provide start time and duration | Can provide duration, or use StartOperation pattern |
| **Correlation** | Manual parent ID management | Automatic parent-child via Activity context |
| **Type/Target** | Separate Type and Target properties | Mapped to Activity tags |
| **Result Code** | String property | Mapped to `http.status_code` or similar |
| **Success** | Boolean property | Mapped to `ActivityStatusCode` |
| **Custom Properties** | `Properties` dictionary | Activity tags |
| **Metrics** | `Metrics` dictionary | **Not supported** (use Meter API) |

## Removed/Changed Features

### 1. Dependency Type Mapping

**2.x:**
```csharp
telemetryClient.TrackDependency(
    dependencyTypeName: "SQL",
    target: "mydb.database.windows.net",
    dependencyName: "SELECT * FROM Orders",
    data: "SELECT * FROM Orders WHERE CustomerId = @p0",
    startTime: startTime,
    duration: duration,
    resultCode: "200",
    success: true);
```

**3.x:**
```csharp
// Same method signature, but creates Activity
telemetryClient.TrackDependency(
    dependencyTypeName: "SQL",
    target: "mydb.database.windows.net",
    dependencyName: "SELECT * FROM Orders",
    data: "SELECT * FROM Orders WHERE CustomerId = @p0",
    startTime: startTime,
    duration: duration,
    resultCode: "0", // SQL result codes differ
    success: true);

// Activity created with these tags:
// - db.system = "SQL"
// - server.address = "mydb.database.windows.net"
// - db.statement = "SELECT * FROM Orders WHERE CustomerId = @p0"
// - db.operation.name = "SELECT * FROM Orders"
```

### 2. Metrics Parameter Removed

**2.x:**
```csharp
telemetryClient.TrackDependency(
    dependencyTypeName: "HTTP",
    target: "api.example.com",
    dependencyName: "GET /api/products",
    data: "https://api.example.com/api/products",
    startTime: startTime,
    duration: duration,
    resultCode: "200",
    success: true);

// Add metrics
var dependency = new DependencyTelemetry(...)
{
    Metrics =
    {
        ["ResponseSize"] = 1024,
        ["ConnectionPoolSize"] = 5
    }
};
telemetryClient.Track(dependency);
```

**3.x:**
```csharp
// TrackDependency has no metrics parameter
telemetryClient.TrackDependency(...);

// Use Meter API for metrics
private static readonly Meter Meter = new("MyApp.Dependencies");
private static readonly Histogram<long> ResponseSize = 
    Meter.CreateHistogram<long>("http.client.response.size");
private static readonly Histogram<int> ConnectionPoolSize = 
    Meter.CreateHistogram<int>("db.client.pool.size");

// Record metrics separately
ResponseSize.Record(1024, new KeyValuePair<string, object>("server.address", "api.example.com"));
ConnectionPoolSize.Record(5, new KeyValuePair<string, object>("db.system", "sql"));
```

### 3. Custom Properties Mapping

**2.x:**
```csharp
var dependency = new DependencyTelemetry(...)
{
    Properties =
    {
        ["UserId"] = userId,
        ["TenantId"] = tenantId,
        ["ApiVersion"] = "v2"
    }
};
telemetryClient.Track(dependency);
```

**3.x:**
```csharp
// Use ActivitySource for custom properties
using var activity = ActivitySource.StartActivity("CallExternalApi", ActivityKind.Client);
activity?.SetTag("user.id", userId);
activity?.SetTag("tenant.id", tenantId);
activity?.SetTag("api.version", "v2");

// Or use TrackDependency with enrichment processor
telemetryClient.TrackDependency(...);
// Processor adds tags to Activity
```

## Migration Options

### Option 1: Continue Using TrackDependency (Limited)

**When to use:** Quick migration, basic dependency tracking without custom properties/metrics.

```csharp
// Works but limited
telemetryClient.TrackDependency(
    dependencyTypeName: "HTTP",
    target: "api.example.com",
    dependencyName: "GET /api/products",
    data: "https://api.example.com/api/products",
    startTime: DateTimeOffset.UtcNow,
    duration: duration,
    resultCode: "200",
    success: true);
```

**Limitations:**
- No custom properties on the call
- No metrics support
- Less control over Activity lifecycle

### Option 2: Migrate to ActivitySource (Recommended)

**When to use:** Full control, custom properties, better performance.

```csharp
private static readonly ActivitySource ActivitySource = new("MyApp.ExternalCalls");

public async Task<Product[]> GetProductsAsync()
{
    using var activity = ActivitySource.StartActivity(
        "GET /api/products",
        ActivityKind.Client);
    
    try
    {
        activity?.SetTag("http.request.method", "GET");
        activity?.SetTag("url.full", "https://api.example.com/api/products");
        activity?.SetTag("server.address", "api.example.com");
        activity?.SetTag("user.id", _currentUserId);
        
        var response = await _httpClient.GetAsync("https://api.example.com/api/products");
        
        activity?.SetTag("http.response.status_code", (int)response.StatusCode);
        activity?.SetTag("http.response.body.size", response.Content.Headers.ContentLength);
        
        if (response.IsSuccessStatusCode)
        {
            activity?.SetStatus(ActivityStatusCode.Ok);
            return await response.Content.ReadFromJsonAsync<Product[]>();
        }
        else
        {
            activity?.SetStatus(ActivityStatusCode.Error, $"HTTP {(int)response.StatusCode}");
            throw new HttpRequestException($"Request failed: {response.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity?.RecordException(ex);
        throw;
    }
}
```

### Option 3: Use StartOperation Pattern

**When to use:** Need automatic timing, similar to 2.x StartOperation.

```csharp
public async Task<Product[]> GetProductsAsync()
{
    using var operation = _telemetryClient.StartOperation<DependencyTelemetry>(
        "GET /api/products",
        operationType: "HTTP");
    
    operation.Telemetry.Type = "HTTP";
    operation.Telemetry.Target = "api.example.com";
    operation.Telemetry.Data = "https://api.example.com/api/products";
    
    try
    {
        var response = await _httpClient.GetAsync("https://api.example.com/api/products");
        
        operation.Telemetry.ResultCode = ((int)response.StatusCode).ToString();
        operation.Telemetry.Success = response.IsSuccessStatusCode;
        
        return await response.Content.ReadFromJsonAsync<Product[]>();
    }
    catch (Exception ex)
    {
        operation.Telemetry.Success = false;
        throw;
    }
}
```

## Common Dependency Types

### HTTP Dependencies

**2.x:**
```csharp
telemetryClient.TrackDependency(
    dependencyTypeName: "HTTP",
    target: "api.example.com",
    dependencyName: "GET /api/users",
    data: "https://api.example.com/api/users",
    startTime: startTime,
    duration: duration,
    resultCode: "200",
    success: true);
```

**3.x with ActivitySource:**
```csharp
using var activity = ActivitySource.StartActivity("GET /api/users", ActivityKind.Client);
activity?.SetTag("http.request.method", "GET");
activity?.SetTag("url.full", "https://api.example.com/api/users");
activity?.SetTag("server.address", "api.example.com");

// HttpClient automatically creates activities in 3.x
// Manual tracking only needed for custom scenarios
```

### SQL Dependencies

**2.x:**
```csharp
var startTime = DateTimeOffset.UtcNow;
var stopwatch = Stopwatch.StartNew();

var result = await connection.QueryAsync<Order>(sql);

stopwatch.Stop();
telemetryClient.TrackDependency(
    dependencyTypeName: "SQL",
    target: "mydb.database.windows.net",
    dependencyName: "GetOrders",
    data: sql,
    startTime: startTime,
    duration: stopwatch.Elapsed,
    resultCode: "0",
    success: true);
```

**3.x with ActivitySource:**
```csharp
using var activity = ActivitySource.StartActivity("GetOrders", ActivityKind.Client);
activity?.SetTag("db.system", "mssql");
activity?.SetTag("server.address", "mydb.database.windows.net");
activity?.SetTag("db.name", "OrdersDB");
activity?.SetTag("db.statement", sql);

try
{
    var result = await connection.QueryAsync<Order>(sql);
    activity?.SetStatus(ActivityStatusCode.Ok);
    return result;
}
catch (SqlException ex)
{
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    activity?.RecordException(ex);
    throw;
}
```

### Azure Service Bus Dependencies

**2.x:**
```csharp
telemetryClient.TrackDependency(
    dependencyTypeName: "Azure Service Bus",
    target: "myservicebus.servicebus.windows.net",
    dependencyName: "orders-queue",
    data: "Send message",
    startTime: startTime,
    duration: duration,
    resultCode: "200",
    success: true);
```

**3.x with ActivitySource:**
```csharp
using var activity = ActivitySource.StartActivity("Send orders-queue", ActivityKind.Producer);
activity?.SetTag("messaging.system", "servicebus");
activity?.SetTag("messaging.destination.name", "orders-queue");
activity?.SetTag("server.address", "myservicebus.servicebus.windows.net");
activity?.SetTag("messaging.operation", "send");

await sender.SendMessageAsync(message);
```

### Azure Storage Dependencies

**2.x:**
```csharp
telemetryClient.TrackDependency(
    dependencyTypeName: "Azure blob",
    target: "mystorageaccount.blob.core.windows.net",
    dependencyName: "uploads/file.txt",
    data: "Upload",
    startTime: startTime,
    duration: duration,
    resultCode: "201",
    success: true);
```

**3.x with ActivitySource:**
```csharp
using var activity = ActivitySource.StartActivity("Upload blob", ActivityKind.Client);
activity?.SetTag("az.namespace", "Microsoft.Storage");
activity?.SetTag("server.address", "mystorageaccount.blob.core.windows.net");
activity?.SetTag("container.name", "uploads");
activity?.SetTag("blob.name", "file.txt");

await blobClient.UploadAsync(stream);
```

## Automatic Instrumentation

**Important:** In 3.x, many dependencies are automatically instrumented:

- **HttpClient** - Automatic (no manual tracking needed)
- **Azure SDK** - Automatic with Azure Monitor OpenTelemetry Distro
- **SQL** - Automatic with SqlClient instrumentation
- **Redis** - Automatic with StackExchange.Redis instrumentation

**Only manually track dependencies for:**
- Custom external services not covered by automatic instrumentation
- Legacy libraries without OpenTelemetry support
- Special business logic dependencies

## Azure Monitor Queries

### 2.x Query

```kusto
dependencies
| where type == "HTTP"
| where target == "api.example.com"
| where resultCode == "200"
| project timestamp, name, duration, customDimensions
```

### 3.x Query

```kusto
dependencies
| where customDimensions.["http.request.method"] == "GET"
| where customDimensions.["server.address"] == "api.example.com"
| where customDimensions.["http.response.status_code"] == "200"
| project timestamp, name, duration, customDimensions
```

## Migration Checklist

- [ ] Identify all `TrackDependency()` calls
- [ ] Determine if automatic instrumentation covers the dependency
- [ ] For custom dependencies, migrate to `ActivitySource.StartActivity()`
- [ ] Replace `dependencyTypeName` with semantic tags (e.g., `db.system`, `http.request.method`)
- [ ] Replace `target` with `server.address`
- [ ] Replace `resultCode` with semantic tags (e.g., `http.response.status_code`)
- [ ] Replace custom properties with `Activity.SetTag()`
- [ ] Replace metrics with Meter API
- [ ] Update Azure Monitor queries to use new tag names
- [ ] Test distributed tracing (parent-child relationships)
- [ ] Verify dependency type mapping in Azure Monitor

## See Also

- [TrackRequest-behavior-changed.md](TrackRequest-behavior-changed.md)
- [activity-source.md](../../opentelemetry-fundamentals/activity-source.md)
- [activity-kinds.md](../../opentelemetry-fundamentals/activity-kinds.md)
- [semantic-conventions.md](../../opentelemetry-fundamentals/semantic-conventions.md)
