# StartOperation<T> Removed

**Category:** Breaking Change  
**Applies to:** TelemetryClient API  
**Severity:** High - Common pattern affected

## What Changed

The `TelemetryClient.StartOperation<T>()` methods have been removed in 3.x. This was the primary way to create custom operations in 2.x.

## In 2.x

```csharp
private readonly TelemetryClient _telemetryClient;

public async Task ProcessOrderAsync(Order order)
{
    using var operation = _telemetryClient.StartOperation<RequestTelemetry>("ProcessOrder");
    operation.Telemetry.Properties["orderId"] = order.Id.ToString();
    
    try
    {
        await _orderService.ProcessAsync(order);
        operation.Telemetry.Success = true;
    }
    catch (Exception ex)
    {
        operation.Telemetry.Success = false;
        _telemetryClient.TrackException(ex);
        throw;
    }
}
```

**Variants removed:**
- `StartOperation<RequestTelemetry>(string operationName)`
- `StartOperation<DependencyTelemetry>(string operationName)`
- `StartOperation<T>(string operationName, string operationId, string parentOperationId)`

## In 3.x

Use `ActivitySource` to create custom operations:

```csharp
private static readonly ActivitySource ActivitySource = new("MyService");

public async Task ProcessOrderAsync(Order order)
{
    using var activity = ActivitySource.StartActivity("ProcessOrder");
    activity?.SetTag("order.id", order.Id);
    
    try
    {
        await _orderService.ProcessAsync(order);
        activity?.SetStatus(ActivityStatusCode.Ok);
    }
    catch (Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity?.RecordException(ex);
        throw;
    }
}
```

**Register ActivitySource in startup:**

```csharp
builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        otel.AddSource("MyService");
    });
```

## Migration Steps

### Step 1: Create Static ActivitySource

```csharp
// One ActivitySource per component/service
private static readonly ActivitySource ActivitySource = new("MyCompany.MyService");
```

### Step 2: Replace StartOperation Calls

```csharp
// Before (2.x)
using var operation = _telemetryClient.StartOperation<RequestTelemetry>("OperationName");

// After (3.x)
using var activity = ActivitySource.StartActivity("OperationName");
```

### Step 3: Replace Property Access

```csharp
// Before (2.x)
operation.Telemetry.Properties["key"] = "value";
operation.Telemetry.Success = true;

// After (3.x)
activity?.SetTag("key", "value");
activity?.SetStatus(ActivityStatusCode.Ok);
```

### Step 4: Register ActivitySource

```csharp
builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        otel.AddSource("MyCompany.MyService");
    });
```

## Operation Type Mapping

| 2.x Type | 3.x ActivityKind | Use Case |
|----------|------------------|----------|
| `RequestTelemetry` | `ActivityKind.Server` | Incoming HTTP requests (automatic) |
| `RequestTelemetry` | `ActivityKind.Internal` | Manual business operations |
| `DependencyTelemetry` (HTTP) | `ActivityKind.Client` | Outgoing HTTP calls (automatic) |
| `DependencyTelemetry` (SQL) | `ActivityKind.Client` | Database calls (automatic) |
| `DependencyTelemetry` (InProc) | `ActivityKind.Internal` | Internal operations |

```csharp
// Specify ActivityKind
using var activity = ActivitySource.StartActivity(
    "OperationName", 
    ActivityKind.Internal);
```

## Common Patterns

### Pattern 1: Request Operation

```csharp
// Before (2.x)
using var operation = _telemetryClient.StartOperation<RequestTelemetry>("ProcessRequest");
operation.Telemetry.Url = new Uri("http://internal/process");
operation.Telemetry.ResponseCode = "200";
operation.Telemetry.Success = true;

// After (3.x)
using var activity = ActivitySource.StartActivity("ProcessRequest", ActivityKind.Internal);
activity?.SetTag("url.full", "http://internal/process");
activity?.SetTag("http.response.status_code", 200);
activity?.SetStatus(ActivityStatusCode.Ok);
```

### Pattern 2: Dependency Operation

```csharp
// Before (2.x)
using var operation = _telemetryClient.StartOperation<DependencyTelemetry>("FetchData");
operation.Telemetry.Type = "HTTP";
operation.Telemetry.Target = "api.example.com";
operation.Telemetry.Data = "GET /api/data";
operation.Telemetry.Success = true;

// After (3.x)
using var activity = ActivitySource.StartActivity("FetchData", ActivityKind.Client);
activity?.SetTag("http.request.method", "GET");
activity?.SetTag("server.address", "api.example.com");
activity?.SetTag("url.path", "/api/data");
activity?.SetStatus(ActivityStatusCode.Ok);
```

### Pattern 3: Nested Operations

```csharp
// Before (2.x)
using var parentOp = _telemetryClient.StartOperation<RequestTelemetry>("Parent");
{
    using var childOp = _telemetryClient.StartOperation<DependencyTelemetry>("Child");
    // Automatic parent-child relationship
}

// After (3.x)
using var parentActivity = ActivitySource.StartActivity("Parent");
{
    using var childActivity = ActivitySource.StartActivity("Child");
    // Automatic parent-child relationship via Activity.Current
}
```

## Why This Changed

1. **OpenTelemetry Standard**: Activity is the standard OpenTelemetry tracing primitive
2. **Better Performance**: ActivitySource is more efficient than TelemetryClient operations
3. **No TelemetryClient Dependency**: Reduces coupling to Application Insights APIs
4. **Automatic Context Propagation**: Activity.Current handles context automatically
5. **Framework Support**: ASP.NET Core, HttpClient, etc. create Activities automatically

## Impact

- **High**: StartOperation was commonly used for custom instrumentation
- **Refactoring Required**: All StartOperation calls must be replaced
- **Registration Required**: ActivitySource must be registered in startup

## See Also

- [activity-source.md](../../opentelemetry-fundamentals/activity-source.md)
- [activity-vs-telemetry.md](../../concepts/activity-vs-telemetry.md)
- [StartActivity.md](../../api-reference/ActivitySource/StartActivity.md)
