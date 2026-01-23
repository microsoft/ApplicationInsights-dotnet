# TrackMetric Method Replaced with Meter API

**Category:** Breaking Change  
**Applies to:** TelemetryClient.TrackMetric  
**Severity:** Medium - Metrics patterns changed  
**Related:** [GetMetric-simplified.md](GetMetric-simplified.md), [meter.md](../../opentelemetry-fundamentals/meter.md)

## Breaking Change

`TelemetryClient.TrackMetric()` has been replaced by OpenTelemetry's Meter API for recording metrics.

## In 2.x

```csharp
public class OrderService
{
    private readonly TelemetryClient _telemetryClient;
    
    public async Task ProcessOrderAsync(Order order)
    {
        // Track single value
        _telemetryClient.TrackMetric("OrderAmount", order.TotalAmount);
        
        // Track with dimensions
        _telemetryClient.TrackMetric("OrderAmount", order.TotalAmount, 
            new Dictionary<string, string>
            {
                ["Region"] = order.Region,
                ["Priority"] = order.Priority.ToString()
            });
        
        await _repository.SaveAsync(order);
    }
}
```

## In 3.x

**Option 1: Use Meter API (Recommended)**

```csharp
public class OrderService
{
    private static readonly Meter Meter = new("MyCompany.OrderService");
    private static readonly Histogram<double> OrderAmountHistogram = 
        Meter.CreateHistogram<double>("order.amount", "USD", "Order amount in USD");
    
    public async Task ProcessOrderAsync(Order order)
    {
        // Record value with dimensions (tags)
        OrderAmountHistogram.Record(order.TotalAmount, 
            new KeyValuePair<string, object?>("region", order.Region),
            new KeyValuePair<string, object?>("priority", order.Priority.ToString()));
        
        await _repository.SaveAsync(order);
    }
}
```

Register in Program.cs:
```csharp
builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        // Register your Meter
        otel.AddMeter("MyCompany.OrderService");
    });
```

**Option 2: Continue Using GetMetric (Limited)**

```csharp
public class OrderService
{
    private readonly TelemetryClient _telemetryClient;
    
    public async Task ProcessOrderAsync(Order order)
    {
        // GetMetric still exists but simplified
        _telemetryClient.GetMetric("OrderAmount").TrackValue(order.TotalAmount);
        
        // With dimensions (max 10 dimensions)
        _telemetryClient.GetMetric("OrderAmount", "Region", "Priority")
            .TrackValue(order.TotalAmount, order.Region, order.Priority.ToString());
        
        await _repository.SaveAsync(order);
    }
}
```

## Key Differences

| Aspect | 2.x TrackMetric | 3.x Meter API |
|--------|----------------|---------------|
| **API** | `TrackMetric(name, value, properties)` | `Histogram.Record(value, tags)` |
| **Instantiation** | Per-call | Pre-created instruments |
| **Dimensions** | Dictionary<string, string> | KeyValuePair tags |
| **Instrument Types** | Single type | Counter, Histogram, Gauge, etc. |
| **Performance** | Slower | Much faster |
| **Standards** | Custom | OpenTelemetry standard |

## Meter Instrument Types

### Counter - Monotonically Increasing Values

```csharp
private static readonly Meter Meter = new("MyCompany.OrderService");
private static readonly Counter<long> OrderCounter = 
    Meter.CreateCounter<long>("orders.processed", description: "Number of orders processed");

public void ProcessOrder(Order order)
{
    OrderCounter.Add(1, 
        new KeyValuePair<string, object?>("status", "success"),
        new KeyValuePair<string, object?>("region", order.Region));
}
```

**Use for:** Count of operations, requests, errors, items processed

### Histogram - Distribution of Values

```csharp
private static readonly Histogram<double> OrderAmountHistogram = 
    Meter.CreateHistogram<double>("order.amount", "USD");

public void ProcessOrder(Order order)
{
    OrderAmountHistogram.Record(order.TotalAmount,
        new KeyValuePair<string, object?>("region", order.Region));
}
```

**Use for:** Request duration, response size, amounts, latencies

### ObservableGauge - Current Value at Observation Time

```csharp
private static readonly Meter Meter = new("MyCompany.OrderService");

public OrderService()
{
    Meter.CreateObservableGauge("queue.length", 
        () => _queue.Count,
        description: "Current queue length");
}
```

**Use for:** Memory usage, queue length, thread count, temperature

### UpDownCounter - Values That Can Increase or Decrease

```csharp
private static readonly UpDownCounter<int> ActiveConnectionsCounter = 
    Meter.CreateUpDownCounter<int>("connections.active");

public void ConnectionOpened()
{
    ActiveConnectionsCounter.Add(1);
}

public void ConnectionClosed()
{
    ActiveConnectionsCounter.Add(-1);
}
```

**Use for:** Active connections, items in queue, concurrent operations

## Migration Examples

### Simple Metric

```csharp
// 2.x
_telemetryClient.TrackMetric("ResponseTime", responseTime);

// 3.x
private static readonly Histogram<double> ResponseTimeHistogram = 
    Meter.CreateHistogram<double>("response.time", "ms");

ResponseTimeHistogram.Record(responseTime);
```

### Metric with Dimensions

```csharp
// 2.x
_telemetryClient.TrackMetric("RequestCount", 1, new Dictionary<string, string>
{
    ["Endpoint"] = "/api/orders",
    ["Method"] = "POST",
    ["Status"] = "200"
});

// 3.x
private static readonly Counter<long> RequestCounter = 
    Meter.CreateCounter<long>("http.server.requests");

RequestCounter.Add(1,
    new KeyValuePair<string, object?>("http.route", "/api/orders"),
    new KeyValuePair<string, object?>("http.method", "POST"),
    new KeyValuePair<string, object?>("http.status_code", 200));
```

### Batch Metrics

```csharp
// 2.x
foreach (var item in items)
{
    _telemetryClient.TrackMetric("ItemSize", item.Size);
}

// 3.x - More efficient
private static readonly Histogram<long> ItemSizeHistogram = 
    Meter.CreateHistogram<long>("item.size", "bytes");

foreach (var item in items)
{
    ItemSizeHistogram.Record(item.Size,
        new KeyValuePair<string, object?>("item.type", item.Type));
}
```

## Performance Comparison

**2.x:** Each TrackMetric call creates telemetry object
```csharp
// Allocates new MetricTelemetry object per call
_telemetryClient.TrackMetric("metric", value); // ~500ns + allocation
```

**3.x:** Pre-created instruments, minimal allocation
```csharp
// Instrument created once
private static readonly Histogram<double> MyHistogram = 
    Meter.CreateHistogram<double>("metric");

// Recording is very fast
MyHistogram.Record(value); // ~50ns, no allocation
```

**Result:** 10x faster, significantly less GC pressure

## Querying Metrics in Azure Monitor

### 2.x Metrics Query

```kusto
customMetrics
| where name == "OrderAmount"
| project timestamp, value, customDimensions.Region
```

### 3.x Metrics Query

```kusto
customMetrics
| where name == "order.amount"
| project timestamp, value, customDimensions.region
```

**Note:** Dimension names are lowercase in 3.x (following OpenTelemetry conventions)

## Common Patterns

### Request Duration

```csharp
// 2.x
var sw = Stopwatch.StartNew();
await ProcessRequestAsync();
_telemetryClient.TrackMetric("RequestDuration", sw.ElapsedMilliseconds);

// 3.x
private static readonly Histogram<double> RequestDurationHistogram = 
    Meter.CreateHistogram<double>("request.duration", "ms");

var sw = Stopwatch.StartNew();
await ProcessRequestAsync();
RequestDurationHistogram.Record(sw.ElapsedMilliseconds,
    new KeyValuePair<string, object?>("operation", "process"));
```

### Business Metrics

```csharp
// 2.x
_telemetryClient.TrackMetric("Revenue", order.TotalAmount, new Dictionary<string, string>
{
    ["Product"] = order.ProductId,
    ["Country"] = order.Country
});

// 3.x
private static readonly Histogram<decimal> RevenueHistogram = 
    Meter.CreateHistogram<decimal>("revenue", "USD");

RevenueHistogram.Record(order.TotalAmount,
    new KeyValuePair<string, object?>("product.id", order.ProductId),
    new KeyValuePair<string, object?>("country", order.Country));
```

## Migration Checklist

- [ ] Identify all `TrackMetric()` calls
- [ ] Determine appropriate instrument type (Counter, Histogram, Gauge, UpDownCounter)
- [ ] Create static Meter instance per component
- [ ] Create pre-defined instruments
- [ ] Replace TrackMetric with instrument.Record()/Add()
- [ ] Convert dimension dictionaries to KeyValuePair tags
- [ ] Register Meter in ConfigureOpenTelemetryBuilder
- [ ] Update queries to use new metric names
- [ ] Test metrics appear in Application Insights

## Why This Changed

1. **Performance**: Pre-created instruments are 10x faster than per-call telemetry
2. **Standards**: OpenTelemetry metrics are industry standard
3. **Semantic**: Different instrument types have specific meanings
4. **Aggregation**: Better support for aggregation and cardinality management

## See Also

- [GetMetric-simplified.md](GetMetric-simplified.md)
- [meter.md](../../opentelemetry-fundamentals/meter.md)
- [OpenTelemetry Metrics Specification](https://opentelemetry.io/docs/specs/otel/metrics/)
