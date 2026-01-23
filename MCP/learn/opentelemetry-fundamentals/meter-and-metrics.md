# Meter and Metrics in OpenTelemetry

**Category:** OpenTelemetry Fundamentals  
**Applies to:** Recording custom metrics with OpenTelemetry Meter API  
**Related:** [activity-source.md](activity-source.md)

## Overview

In Application Insights 3.x, custom metrics are recorded using OpenTelemetry's **Meter API** instead of TelemetryClient.TrackMetric(). Meters create instruments (counters, histograms, gauges) that automatically aggregate data.

## Key Concepts

### Meter
A factory for creating metric instruments. Similar to ActivitySource for traces.

### Instruments
- **Counter**: Monotonically increasing value (e.g., requests count)
- **Histogram**: Distribution of values (e.g., request duration)
- **ObservableGauge**: Point-in-time measurement (e.g., queue length)
- **UpDownCounter**: Value that can increase or decrease (e.g., active connections)

## Creating and Using Meters

### Basic Meter Setup

```csharp
using System.Diagnostics.Metrics;

public class OrderService
{
    private static readonly Meter Meter = new("MyCompany.OrderService", "1.0.0");
    
    private static readonly Counter<long> OrdersCreated = 
        Meter.CreateCounter<long>(
            "orders.created",
            unit: "{order}",
            description: "Number of orders created");
    
    private static readonly Histogram<double> OrderValue = 
        Meter.CreateHistogram<double>(
            "orders.value",
            unit: "USD",
            description: "Order value in USD");
    
    public async Task<Order> CreateOrderAsync(Order order)
    {
        await _repository.SaveAsync(order);
        
        // Increment counter
        OrdersCreated.Add(1, 
            new KeyValuePair<string, object?>("order.type", order.Type),
            new KeyValuePair<string, object?>("customer.segment", order.CustomerSegment));
        
        // Record value
        OrderValue.Record(order.Total,
            new KeyValuePair<string, object?>("order.type", order.Type));
        
        return order;
    }
}
```

### Register Meter with OpenTelemetry

```csharp
builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        otel.AddMeter("MyCompany.OrderService");
    });
```

## Counter: Tracking Cumulative Values

Use counters for values that only increase (requests, errors, items processed).

```csharp
public class PaymentService
{
    private static readonly Meter Meter = new("MyCompany.PaymentService");
    
    private static readonly Counter<long> PaymentsProcessed = 
        Meter.CreateCounter<long>("payments.processed");
    
    private static readonly Counter<long> PaymentsFailed = 
        Meter.CreateCounter<long>("payments.failed");
    
    public async Task<PaymentResult> ProcessPaymentAsync(Payment payment)
    {
        try
        {
            var result = await _gateway.ChargeAsync(payment);
            
            PaymentsProcessed.Add(1,
                new KeyValuePair<string, object?>("payment.method", payment.Method),
                new KeyValuePair<string, object?>("payment.currency", payment.Currency));
            
            return result;
        }
        catch (Exception ex)
        {
            PaymentsFailed.Add(1,
                new KeyValuePair<string, object?>("payment.method", payment.Method),
                new KeyValuePair<string, object?>("error.type", ex.GetType().Name));
            
            throw;
        }
    }
}
```

## Histogram: Measuring Distributions

Use histograms for measuring distributions (latency, size, duration).

```csharp
public class ApiClient
{
    private static readonly Meter Meter = new("MyCompany.ApiClient");
    
    private static readonly Histogram<double> RequestDuration = 
        Meter.CreateHistogram<double>(
            "http.client.request.duration",
            unit: "ms",
            description: "HTTP request duration");
    
    private static readonly Histogram<long> ResponseSize = 
        Meter.CreateHistogram<long>(
            "http.client.response.size",
            unit: "By",
            description: "HTTP response size in bytes");
    
    public async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request)
    {
        var startTime = Stopwatch.GetTimestamp();
        var response = await _httpClient.SendAsync(request);
        var elapsed = Stopwatch.GetElapsedTime(startTime);
        
        // Record duration
        RequestDuration.Record(elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("http.method", request.Method.Method),
            new KeyValuePair<string, object?>("http.status_code", (int)response.StatusCode));
        
        // Record response size
        if (response.Content != null)
        {
            var size = response.Content.Headers.ContentLength ?? 0;
            ResponseSize.Record(size,
                new KeyValuePair<string, object?>("http.method", request.Method.Method));
        }
        
        return response;
    }
}
```

## ObservableGauge: Point-in-Time Measurements

Use observable gauges for measuring current state (queue length, memory usage, temperature).

```csharp
public class QueueMonitor
{
    private static readonly Meter Meter = new("MyCompany.QueueMonitor");
    private readonly IQueueClient _queueClient;
    
    public QueueMonitor(IQueueClient queueClient)
    {
        _queueClient = queueClient;
        
        // Create observable gauge
        Meter.CreateObservableGauge(
            "queue.length",
            observeValue: GetQueueLength,
            unit: "{message}",
            description: "Current queue length");
        
        Meter.CreateObservableGauge(
            "queue.oldest_message_age",
            observeValue: GetOldestMessageAge,
            unit: "s",
            description: "Age of oldest message in seconds");
    }
    
    private int GetQueueLength()
    {
        return _queueClient.GetApproximateMessageCount();
    }
    
    private double GetOldestMessageAge()
    {
        var oldestMessage = _queueClient.PeekOldestMessage();
        return oldestMessage != null 
            ? (DateTime.UtcNow - oldestMessage.EnqueuedTime).TotalSeconds 
            : 0;
    }
}
```

## UpDownCounter: Tracking Current Values

Use up-down counters for values that can increase or decrease (active connections, items in cache).

```csharp
public class ConnectionPool
{
    private static readonly Meter Meter = new("MyCompany.ConnectionPool");
    
    private static readonly UpDownCounter<int> ActiveConnections = 
        Meter.CreateUpDownCounter<int>(
            "db.connections.active",
            unit: "{connection}",
            description: "Number of active database connections");
    
    public Connection AcquireConnection()
    {
        var connection = _pool.GetConnection();
        
        // Increment when connection acquired
        ActiveConnections.Add(1,
            new KeyValuePair<string, object?>("pool.name", "MainPool"));
        
        return connection;
    }
    
    public void ReleaseConnection(Connection connection)
    {
        _pool.ReturnConnection(connection);
        
        // Decrement when connection released
        ActiveConnections.Add(-1,
            new KeyValuePair<string, object?>("pool.name", "MainPool"));
    }
}
```

## Migration from 2.x TrackMetric

### Before (2.x): TrackMetric

```csharp
public class OrderService
{
    private readonly TelemetryClient _telemetryClient;
    
    public async Task CreateOrderAsync(Order order)
    {
        await _repository.SaveAsync(order);
        
        _telemetryClient.TrackMetric("OrdersCreated", 1, 
            new Dictionary<string, string>
            {
                ["OrderType"] = order.Type,
                ["CustomerSegment"] = order.CustomerSegment
            });
        
        _telemetryClient.TrackMetric("OrderValue", order.Total,
            new Dictionary<string, string>
            {
                ["OrderType"] = order.Type
            });
    }
}
```

### After (3.x): Meter API

```csharp
public class OrderService
{
    private static readonly Meter Meter = new("MyCompany.OrderService");
    
    private static readonly Counter<long> OrdersCreated = 
        Meter.CreateCounter<long>("orders.created");
    
    private static readonly Histogram<double> OrderValue = 
        Meter.CreateHistogram<double>("orders.value", unit: "USD");
    
    public async Task CreateOrderAsync(Order order)
    {
        await _repository.SaveAsync(order);
        
        OrdersCreated.Add(1,
            new KeyValuePair<string, object?>("order.type", order.Type),
            new KeyValuePair<string, object?>("customer.segment", order.CustomerSegment));
        
        OrderValue.Record(order.Total,
            new KeyValuePair<string, object?>("order.type", order.Type));
    }
}
```

## Advanced Patterns

### Reusable Metric Tags

```csharp
public class MetricTags
{
    public static TagList CreateTags(string service, string operation)
    {
        return new TagList
        {
            { "service.name", service },
            { "operation.name", operation }
        };
    }
}

// Usage
var tags = MetricTags.CreateTags("OrderService", "CreateOrder");
OrdersCreated.Add(1, tags);
```

### Conditional Metrics

```csharp
public class OrderService
{
    private static readonly Counter<long> HighValueOrders = 
        Meter.CreateCounter<long>("orders.high_value");
    
    public async Task CreateOrderAsync(Order order)
    {
        await _repository.SaveAsync(order);
        
        // Only track high-value orders
        if (order.Total > 1000)
        {
            HighValueOrders.Add(1,
                new KeyValuePair<string, object?>("order.type", order.Type));
        }
    }
}
```

### Metrics with Computed Values

```csharp
public class CacheMonitor
{
    private static readonly Meter Meter = new("MyCompany.Cache");
    private readonly IMemoryCache _cache;
    
    public CacheMonitor(IMemoryCache cache)
    {
        _cache = cache;
        
        Meter.CreateObservableGauge(
            "cache.hit_ratio",
            observeValue: () =>
            {
                var stats = _cache.GetStatistics();
                return stats.TotalHits > 0 
                    ? (double)stats.TotalHits / (stats.TotalHits + stats.TotalMisses)
                    : 0;
            },
            unit: "1",
            description: "Cache hit ratio");
    }
}
```

## Best Practices

### 1. Use Descriptive Metric Names

Follow [OpenTelemetry semantic conventions](https://opentelemetry.io/docs/specs/semconv/):

```csharp
// Good: Follows conventions
Meter.CreateCounter<long>("http.server.requests");
Meter.CreateHistogram<double>("http.server.duration", unit: "ms");

// Good: Custom with clear namespace
Meter.CreateCounter<long>("mycompany.orders.created");
```

### 2. Choose Appropriate Instrument Type

```csharp
// Counter: Only increases
Meter.CreateCounter<long>("requests.total");

// Histogram: Distribution matters
Meter.CreateHistogram<double>("request.duration", unit: "ms");

// ObservableGauge: Current state
Meter.CreateObservableGauge("queue.length", () => GetQueueLength());

// UpDownCounter: Can increase or decrease
Meter.CreateUpDownCounter<int>("connections.active");
```

### 3. Use Meaningful Units

```csharp
Meter.CreateHistogram<double>("request.duration", unit: "ms");
Meter.CreateHistogram<long>("response.size", unit: "By");
Meter.CreateCounter<long>("orders.created", unit: "{order}");
Meter.CreateHistogram<double>("order.value", unit: "USD");
```

### 4. Add Useful Dimensions with Tags

```csharp
// Good: Useful dimensions
Counter.Add(1,
    new KeyValuePair<string, object?>("http.method", "POST"),
    new KeyValuePair<string, object?>("http.status_code", 200));

// Avoid: High cardinality
Counter.Add(1,
    new KeyValuePair<string, object?>("user.id", userId)); // Too many unique values
```

### 5. Reuse Meter Instances

```csharp
// Good: Static meter
private static readonly Meter Meter = new("MyCompany.MyService");

// Bad: Creating new meter each time
public void RecordMetric()
{
    var meter = new Meter("MyCompany.MyService"); // Don't do this
}
```

## Querying Metrics in Azure Monitor

```kql
// Query counter
customMetrics
| where name == "orders.created"
| summarize Sum = sum(value) by bin(timestamp, 1h), tostring(customDimensions.order_type)

// Query histogram (percentiles)
customMetrics
| where name == "request.duration"
| summarize 
    P50 = percentile(value, 50),
    P95 = percentile(value, 95),
    P99 = percentile(value, 99)
    by bin(timestamp, 5m)

// Query gauge (latest value)
customMetrics
| where name == "queue.length"
| summarize arg_max(timestamp, value) by bin(timestamp, 1m)
```

## Performance Considerations

- Meters and instruments have minimal overhead
- Metrics are pre-aggregated before export
- Use observable gauges sparingly (called on export interval)
- Avoid high-cardinality tags (user IDs, request IDs)

## See Also

- [activity-source.md](activity-source.md)
- [enriching-telemetry.md](../common-scenarios/enriching-telemetry.md)
- [OpenTelemetry Metrics API](https://opentelemetry.io/docs/specs/otel/metrics/api/)
