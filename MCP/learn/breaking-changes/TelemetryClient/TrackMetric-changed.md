# TrackMetric Method Changes

**Category:** Breaking Change  
**Applies to:** TelemetryClient.TrackMetric  
**Severity:** Medium - Changed behavior  
**Related:** [GetMetric-simplified.md](GetMetric-simplified.md)

## Breaking Change

`TelemetryClient.TrackMetric()` behavior has changed significantly in 3.x. The overload with `metrics` parameter is removed, and metric aggregation works differently.

## In 2.x

```csharp
public class OrderService
{
    private readonly TelemetryClient _telemetryClient;
    
    public void ProcessOrder(Order order)
    {
        // Simple metric
        _telemetryClient.TrackMetric("OrdersProcessed", 1);
        
        // Metric with dimensions
        _telemetryClient.TrackMetric(
            "OrderValue",
            order.TotalAmount,
            new Dictionary<string, string>
            {
                ["Region"] = order.Region,
                ["CustomerType"] = order.CustomerType
            });
        
        // Event with metrics parameter (removed in 3.x)
        _telemetryClient.TrackEvent(
            "OrderPlaced",
            properties: new Dictionary<string, string> { ["OrderId"] = order.Id },
            metrics: new Dictionary<string, double> { ["Amount"] = order.TotalAmount });
    }
}
```

## In 3.x

**Option 1: Use OpenTelemetry Meter API (Recommended)**

```csharp
public class OrderService
{
    private static readonly Meter Meter = new("MyCompany.OrderService");
    private static readonly Counter<long> OrdersProcessed = Meter.CreateCounter<long>("orders.processed");
    private static readonly Histogram<double> OrderValue = Meter.CreateHistogram<double>("order.value");
    
    public void ProcessOrder(Order order)
    {
        // Simple counter
        OrdersProcessed.Add(1);
        
        // Histogram with dimensions
        OrderValue.Record(
            order.TotalAmount,
            new KeyValuePair<string, object?>("region", order.Region),
            new KeyValuePair<string, object?>("customer.type", order.CustomerType));
    }
}
```

Register meter in Program.cs:
```csharp
builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        otel.AddMeter("MyCompany.OrderService");
    });
```

**Option 2: Still Use TrackMetric (Limited)**

```csharp
public class OrderService
{
    private readonly TelemetryClient _telemetryClient;
    
    public void ProcessOrder(Order order)
    {
        // Basic TrackMetric still works
        _telemetryClient.TrackMetric("OrdersProcessed", 1);
        
        // But dimensions must use MetricTelemetry
        var metric = new MetricTelemetry("OrderValue", order.TotalAmount);
        metric.Properties["Region"] = order.Region;
        metric.Properties["CustomerType"] = order.CustomerType;
        _telemetryClient.Track(metric);
    }
}
```

**Option 3: Use GetMetric (Pre-Aggregation)**

```csharp
public class OrderService
{
    private readonly TelemetryClient _telemetryClient;
    
    public void ProcessOrder(Order order)
    {
        // GetMetric for pre-aggregated metrics
        _telemetryClient
            .GetMetric("OrderValue", "Region", "CustomerType")
            .TrackValue(order.TotalAmount, order.Region, order.CustomerType);
    }
}
```

## Key Changes

| Feature | 2.x | 3.x |
|---------|-----|-----|
| **Basic TrackMetric** | ✅ Supported | ✅ Supported (but prefer Meter) |
| **TrackMetric with properties** | ✅ Dictionary parameter | ❌ Removed - use MetricTelemetry |
| **Event metrics parameter** | ✅ TrackEvent(..., metrics) | ❌ Removed completely |
| **Aggregation** | Server-side | Client-side (Meter API) |
| **Dimensions** | Properties dictionary | Tags in Meter API |
| **Pre-aggregation** | GetMetric() | GetMetric() (simplified) |

## Removed: Event with Metrics Parameter

```csharp
// 2.x - This is REMOVED in 3.x
_telemetryClient.TrackEvent(
    "OrderPlaced",
    properties: new Dictionary<string, string> { ["OrderId"] = orderId },
    metrics: new Dictionary<string, double> { ["Amount"] = 99.99 });

// 3.x - Use separate metric
_logger.LogInformation("OrderPlaced: OrderId={OrderId}", orderId);
OrderValueHistogram.Record(99.99);
```

## Migration Patterns

### Pattern 1: Simple Counter

```csharp
// 2.x
_telemetryClient.TrackMetric("RequestCount", 1);

// 3.x - Meter API
private static readonly Meter Meter = new("MyApp");
private static readonly Counter<long> RequestCount = Meter.CreateCounter<long>("request.count");

RequestCount.Add(1);
```

### Pattern 2: Metric with Dimensions

```csharp
// 2.x
_telemetryClient.TrackMetric(
    "ResponseTime",
    responseTime,
    new Dictionary<string, string>
    {
        ["Endpoint"] = endpoint,
        ["StatusCode"] = statusCode.ToString()
    });

// 3.x - Meter API
private static readonly Histogram<double> ResponseTime = 
    Meter.CreateHistogram<double>("response.time", unit: "ms");

ResponseTime.Record(
    responseTime,
    new KeyValuePair<string, object?>("endpoint", endpoint),
    new KeyValuePair<string, object?>("http.status_code", statusCode));
```

### Pattern 3: Business Metrics

```csharp
// 2.x
_telemetryClient.TrackMetric("Revenue", revenue);
_telemetryClient.TrackMetric("ItemsSold", itemCount);

// 3.x - Meter API
private static readonly Counter<decimal> Revenue = 
    Meter.CreateCounter<decimal>("sales.revenue", unit: "USD");
private static readonly Counter<long> ItemsSold = 
    Meter.CreateCounter<long>("sales.items");

Revenue.Add(revenue);
ItemsSold.Add(itemCount);
```

### Pattern 4: Gauge (Instantaneous Value)

```csharp
// 2.x
_telemetryClient.TrackMetric("ActiveConnections", connectionCount);

// 3.x - Meter API with ObservableGauge
private static int _activeConnections = 0;
private static readonly ObservableGauge<int> ActiveConnections = 
    Meter.CreateObservableGauge("active.connections", () => _activeConnections);

// Update value
_activeConnections = connectionCount;
```

## Meter API Instrument Types

| Instrument | Use Case | Example |
|------------|----------|---------|
| **Counter** | Always increasing (count, total) | Requests processed, bytes sent |
| **Histogram** | Statistical distribution | Response times, request sizes |
| **ObservableGauge** | Current value snapshot | Active connections, queue depth |
| **ObservableCounter** | Cumulative value read async | Total CPU time |

## Complete Migration Example

### Before (2.x)

```csharp
public class MetricsService
{
    private readonly TelemetryClient _telemetryClient;
    
    public void RecordOrderMetrics(Order order)
    {
        // Counter
        _telemetryClient.TrackMetric("OrdersPlaced", 1);
        
        // Value with dimensions
        _telemetryClient.TrackMetric(
            "OrderAmount",
            order.TotalAmount,
            new Dictionary<string, string>
            {
                ["Region"] = order.Region,
                ["PaymentMethod"] = order.PaymentMethod
            });
        
        // Event with metrics
        _telemetryClient.TrackEvent(
            "LargeOrder",
            properties: new Dictionary<string, string> { ["OrderId"] = order.Id.ToString() },
            metrics: new Dictionary<string, double> { ["Amount"] = order.TotalAmount });
    }
}
```

### After (3.x)

```csharp
public class MetricsService
{
    private static readonly Meter Meter = new("MyCompany.Orders");
    private static readonly Counter<long> OrdersPlaced = 
        Meter.CreateCounter<long>("orders.placed");
    private static readonly Histogram<double> OrderAmount = 
        Meter.CreateHistogram<double>("order.amount", unit: "USD");
    
    private readonly ILogger<MetricsService> _logger;
    
    public void RecordOrderMetrics(Order order)
    {
        // Counter
        OrdersPlaced.Add(1);
        
        // Histogram with dimensions
        OrderAmount.Record(
            order.TotalAmount,
            new KeyValuePair<string, object?>("region", order.Region),
            new KeyValuePair<string, object?>("payment.method", order.PaymentMethod));
        
        // Event logging (no metrics parameter)
        if (order.TotalAmount > 1000)
        {
            _logger.LogInformation(
                "LargeOrder: OrderId={OrderId}, Amount={Amount}",
                order.Id,
                order.TotalAmount);
        }
    }
}
```

Registration:
```csharp
builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        otel.AddMeter("MyCompany.Orders");
    });
```

## Querying Metrics in Azure Monitor

### 2.x Query

```kusto
customMetrics
| where name == "OrderAmount"
| project timestamp, value, customDimensions.Region, customDimensions.PaymentMethod
```

### 3.x Query

```kusto
customMetrics
| where name == "order.amount"
| project timestamp, value, customDimensions.region, customDimensions["payment.method"]
```

## Migration Checklist

- [ ] Identify all `TrackMetric()` calls
- [ ] Create `Meter` instance for each component
- [ ] Choose appropriate instrument type (Counter, Histogram, Gauge)
- [ ] Create instruments as static fields
- [ ] Convert dimension dictionaries to KeyValuePair tags
- [ ] Register meters in `ConfigureOpenTelemetryBuilder`
- [ ] Remove `metrics` parameter from `TrackEvent()` calls
- [ ] Update queries/dashboards for new metric names
- [ ] Test metrics appear in Application Insights

## See Also

- [GetMetric-simplified.md](GetMetric-simplified.md)
- [OpenTelemetry Metrics API](https://opentelemetry.io/docs/specs/otel/metrics/api/)
- [meter.md](../../opentelemetry-fundamentals/meter.md)
