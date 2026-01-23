# Data Mapping Between OpenTelemetry and Azure Monitor

**Category:** Azure Monitor Exporter  
**Applies to:** Understanding how OpenTelemetry data maps to Application Insights schema  
**Related:** [activity-vs-telemetry.md](../concepts/activity-vs-telemetry.md)

## Overview

The Azure Monitor OpenTelemetry Exporter automatically maps OpenTelemetry semantic conventions to Application Insights telemetry schema. Understanding this mapping is crucial for querying data and maintaining compatibility.

## Activity → Request/Dependency Mapping

### Server Activities → Requests Table

Activities with `ActivityKind.Server` map to the `requests` table in Application Insights.

| OpenTelemetry (Activity) | Azure Monitor (requests) | Notes |
|--------------------------|--------------------------|-------|
| `Activity.TraceId` | `operation_Id` | W3C Trace ID |
| `Activity.SpanId` | `id` | W3C Span ID |
| `Activity.ParentSpanId` | `operation_ParentId` | Parent span ID |
| `Activity.DisplayName` | `name` | Operation name |
| `Activity.Duration` | `duration` | Request duration |
| `Activity.Status` | `success` | Ok → true, Error → false |
| `url.full` tag | `url` | Full request URL |
| `http.request.method` tag | `customDimensions.http_request_method` | HTTP method |
| `http.response.status_code` tag | `resultCode` | HTTP status code |
| `server.address` tag | `customDimensions.server_address` | Server address |
| Custom tags via `SetTag()` | `customDimensions.*` | Custom properties |

### Client Activities → Dependencies Table

Activities with `ActivityKind.Client` map to the `dependencies` table.

| OpenTelemetry (Activity) | Azure Monitor (dependencies) | Notes |
|--------------------------|------------------------------|-------|
| `Activity.TraceId` | `operation_Id` | W3C Trace ID |
| `Activity.SpanId` | `id` | W3C Span ID |
| `Activity.ParentSpanId` | `operation_ParentId` | Parent span ID |
| `Activity.DisplayName` | `name` | Dependency name |
| `Activity.Duration` | `duration` | Dependency duration |
| `Activity.Status` | `success` | Ok → true, Error → false |
| `db.system` tag | `type` | Database, HTTP, etc. |
| `server.address` tag | `target` | Target server/resource |
| `db.statement` tag | `data` | SQL query or command |
| `http.response.status_code` tag | `resultCode` | HTTP status code |
| Custom tags via `SetTag()` | `customDimensions.*` | Custom properties |

### HTTP Client Example

```csharp
// OpenTelemetry Activity
using var activity = ActivitySource.StartActivity("GET /api/users", ActivityKind.Client);
activity?.SetTag("http.request.method", "GET");
activity?.SetTag("url.full", "https://api.example.com/api/users");
activity?.SetTag("server.address", "api.example.com");
activity?.SetTag("http.response.status_code", 200);

// Maps to dependencies table:
// - name: "GET /api/users"
// - type: "HTTP"
// - target: "api.example.com"
// - resultCode: "200"
// - success: true
// - customDimensions.http_request_method: "GET"
// - customDimensions.url_full: "https://api.example.com/api/users"
```

### Database Client Example

```csharp
// OpenTelemetry Activity
using var activity = ActivitySource.StartActivity("SELECT users", ActivityKind.Client);
activity?.SetTag("db.system", "postgresql");
activity?.SetTag("db.name", "production");
activity?.SetTag("server.address", "db.example.com");
activity?.SetTag("server.port", 5432);
activity?.SetTag("db.statement", "SELECT * FROM users WHERE id = $1");

// Maps to dependencies table:
// - name: "SELECT users"
// - type: "postgresql"
// - target: "db.example.com | production"
// - data: "SELECT * FROM users WHERE id = $1"
// - success: true
// - customDimensions.db_system: "postgresql"
// - customDimensions.server_port: "5432"
```

## Internal Activities → Dependencies Table

Activities with `ActivityKind.Internal` also map to `dependencies` table with `type = "InProc"`.

```csharp
using var activity = ActivitySource.StartActivity("ProcessOrder", ActivityKind.Internal);
activity?.SetTag("order.id", "12345");

// Maps to dependencies table:
// - name: "ProcessOrder"
// - type: "InProc"
// - success: true
// - customDimensions.order_id: "12345"
```

## LogRecord → Traces Table

ILogger messages map to the `traces` table.

| OpenTelemetry (LogRecord) | Azure Monitor (traces) | Notes |
|---------------------------|------------------------|-------|
| `LogRecord.TraceId` | `operation_Id` | Current Activity's TraceId |
| `LogRecord.SpanId` | `operation_ParentId` | Current Activity's SpanId |
| `LogRecord.Timestamp` | `timestamp` | Log timestamp |
| `LogRecord.Body` | `message` | Log message |
| `LogRecord.LogLevel` | `severityLevel` | 0=Verbose, 1=Information, etc. |
| Log parameters | `customDimensions.*` | Structured log data |
| Exception | `customDimensions.exception` | Exception details |

### Example

```csharp
_logger.LogInformation("Processing order {OrderId} for customer {CustomerId}", 
    orderId, customerId);

// Maps to traces table:
// - message: "Processing order 12345 for customer 67890"
// - severityLevel: 1 (Information)
// - operation_Id: current trace ID
// - customDimensions.OrderId: "12345"
// - customDimensions.CustomerId: "67890"
```

## Exception Mapping

Exceptions recorded via `Activity.RecordException()` or thrown in Activities map to the `exceptions` table.

```csharp
try
{
    await ProcessOrderAsync(order);
}
catch (Exception ex)
{
    activity?.RecordException(ex);
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    throw;
}

// Maps to exceptions table:
// - type: exception type (e.g., "ArgumentNullException")
// - message: exception message
// - outerMessage: outer exception message (if any)
// - operation_Id: current trace ID
// - customDimensions.StackTrace: full stack trace
```

## Custom Metrics Mapping

OpenTelemetry metrics map to the `customMetrics` table.

| OpenTelemetry (Metric) | Azure Monitor (customMetrics) | Notes |
|------------------------|-------------------------------|-------|
| Instrument name | `name` | Metric name |
| Measurement value | `value` | Metric value |
| Tags | `customDimensions.*` | Metric dimensions |
| Unit | Not directly mapped | Included in name/description |

### Example

```csharp
private static readonly Counter<long> OrdersCreated = 
    Meter.CreateCounter<long>("orders.created");

OrdersCreated.Add(1,
    new KeyValuePair<string, object?>("order.type", "Online"));

// Maps to customMetrics table:
// - name: "orders.created"
// - value: 1
// - customDimensions.order_type: "Online"
```

## Cloud Role Name Mapping

Resource attributes map to cloud role fields.

```csharp
otelBuilder.ConfigureResource(resource =>
{
    resource.AddService(serviceName: "OrderService", serviceVersion: "1.0.0");
});

// Maps to:
// - cloud_RoleName: "OrderService"
// - cloud_RoleInstance: hostname
// - application_Version: "1.0.0"
```

## Tag Name Transformations

Azure Monitor transforms some tag names for compatibility.

| OpenTelemetry Tag | Azure Monitor Field | Notes |
|-------------------|---------------------|-------|
| `url.path` | `customDimensions.url_path` | Dots replaced with underscores |
| `http.request.method` | `customDimensions.http_request_method` | Dots replaced with underscores |
| `user.id` | `user_Id` | Special handling for user context |
| `enduser.id` | `user_Id` | OpenTelemetry semantic convention |

## Querying Mapped Data

### Query Requests (Server Activities)

```kql
requests
| where timestamp > ago(1h)
| project 
    timestamp,
    operation_Id,  // TraceId
    id,            // SpanId
    name,          // Activity.DisplayName
    duration,      // Activity.Duration
    success,       // Activity.Status
    resultCode,    // HTTP status code
    url,           // Full URL
    customDimensions
| take 100
```

### Query Dependencies (Client Activities)

```kql
dependencies
| where timestamp > ago(1h)
| where type == "HTTP"  // or "SQL", "InProc", etc.
| project 
    timestamp,
    operation_Id,  // TraceId
    name,          // Activity.DisplayName
    type,          // Dependency type
    target,        // Target server
    data,          // SQL query, etc.
    duration,
    success,
    resultCode,
    customDimensions
| take 100
```

### Query with Custom Dimensions

```kql
requests
| where timestamp > ago(1h)
| extend 
    orderType = tostring(customDimensions.order_type),
    customerId = tostring(customDimensions.customer_id)
| where orderType == "Online"
| summarize count() by bin(timestamp, 5m), orderType
```

### Query Entire Trace

```kql
let traceId = "4bf92f3577b34da6a3ce929d0e0e4736";
union requests, dependencies
| where operation_Id == traceId
| project timestamp, itemType, name, duration, success
| order by timestamp asc
```

## Standard Semantic Conventions

Follow OpenTelemetry semantic conventions for better Azure Monitor integration:

### HTTP Server

```csharp
activity.SetTag("http.request.method", "GET");
activity.SetTag("url.full", "https://example.com/api/users");
activity.SetTag("url.path", "/api/users");
activity.SetTag("http.response.status_code", 200);
activity.SetTag("server.address", "example.com");
```

### HTTP Client

```csharp
activity.SetTag("http.request.method", "POST");
activity.SetTag("url.full", "https://api.example.com/users");
activity.SetTag("server.address", "api.example.com");
activity.SetTag("server.port", 443);
activity.SetTag("http.response.status_code", 201);
```

### Database

```csharp
activity.SetTag("db.system", "postgresql");
activity.SetTag("db.name", "production");
activity.SetTag("db.statement", "SELECT * FROM users WHERE id = $1");
activity.SetTag("server.address", "db.example.com");
activity.SetTag("server.port", 5432);
```

### Messaging

```csharp
activity.SetTag("messaging.system", "rabbitmq");
activity.SetTag("messaging.destination.name", "orders-queue");
activity.SetTag("messaging.operation", "publish");
activity.SetTag("messaging.message.id", messageId);
```

## Custom Dimension Best Practices

### 1. Use Consistent Naming

```csharp
// Good: Consistent naming
activity.SetTag("order.id", orderId);
activity.SetTag("order.type", orderType);
activity.SetTag("order.total", orderTotal);

// Avoid: Inconsistent naming
activity.SetTag("OrderID", orderId);
activity.SetTag("orderType", orderType);
activity.SetTag("order_total", orderTotal);
```

### 2. Avoid High Cardinality

```csharp
// Good: Low cardinality
activity.SetTag("customer.segment", "Enterprise");  // Few unique values

// Avoid: High cardinality
activity.SetTag("customer.id", "12345");  // Many unique values
```

### 3. Use Appropriate Data Types

```csharp
// Azure Monitor stores all tags as strings
activity.SetTag("order.id", orderId.ToString());
activity.SetTag("order.total", orderTotal.ToString("F2"));
activity.SetTag("is.priority", isPriority.ToString().ToLower());
```

## Migration Considerations

### 2.x Custom Properties

```csharp
// 2.x
operation.Telemetry.Properties["CustomerId"] = customerId;

// 3.x
activity?.SetTag("customer.id", customerId);

// Query in both versions
// customDimensions.customer_id (note underscore in 3.x)
```

### 2.x Context Properties

```csharp
// 2.x
telemetry.Context.User.Id = userId;
telemetry.Context.Session.Id = sessionId;

// 3.x
activity?.SetTag("enduser.id", userId);  // Maps to user_Id
activity?.SetTag("session.id", sessionId);
```

## Limitations

1. **Field Name Transformations**: Dots (`.`) in tag names become underscores (`_`) in customDimensions
2. **Data Type Conversions**: All custom dimensions stored as strings in Azure Monitor
3. **Field Length Limits**: Azure Monitor has field length limits (8KB for customDimensions)
4. **Cardinality**: High cardinality dimensions may impact query performance

## See Also

- [activity-vs-telemetry.md](../concepts/activity-vs-telemetry.md)
- [SetTag.md](../api-reference/Activity/SetTag.md)
- [configuration-options.md](configuration-options.md)
- [OpenTelemetry Semantic Conventions](https://opentelemetry.io/docs/specs/semconv/)
