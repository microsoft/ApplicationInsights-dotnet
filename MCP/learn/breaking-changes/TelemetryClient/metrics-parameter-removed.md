# Track Methods Metrics Parameter Removed

**Category:** Breaking Change  
**Applies to:** TelemetryClient API  
**Migration Effort:** Medium  
**Related:** [GetMetric-simplified.md](GetMetric-simplified.md), [meter-and-metrics.md](../../opentelemetry-fundamentals/meter-and-metrics.md)

## Change Summary

The `metrics` parameter has been removed from `TrackEvent()`, `TrackException()`, and `TrackAvailability()` methods in 3.x. Custom metrics must now be sent using the OpenTelemetry Meter API instead of being embedded in telemetry items.

## API Comparison

### 2.x API

```csharp
// Source: ApplicationInsights-dotnet-2x/BASE/src/Microsoft.ApplicationInsights/TelemetryClient.cs:116
public void TrackEvent(string eventName, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
{
    var telemetry = new EventTelemetry(eventName);
    
    if (properties != null && properties.Count > 0)
    {
        Utils.CopyDictionary(properties, telemetry.Properties);
    }
    
    if (metrics != null && metrics.Count > 0)
    {
        Utils.CopyDictionary(metrics, telemetry.Metrics);
    }
    
    this.TrackEvent(telemetry);
}
```

### 3.x API

```csharp
// Source: ApplicationInsights-dotnet/BASE/src/Microsoft.ApplicationInsights/TelemetryClient.cs
// REMOVED: metrics parameter no longer exists
public void TrackEvent(string eventName, IDictionary<string, string> properties = null)
{
    // Only properties parameter - no metrics
}

// Similar changes for:
// - TrackException(Exception exception, IDictionary<string, string> properties)
// - TrackAvailability(string name, DateTimeOffset timeStamp, TimeSpan duration, ...)
```

## Why It Changed

| Reason | Description |
|--------|-------------|
| **Semantic Separation** | OpenTelemetry separates signals: events/traces, metrics, and logs are distinct telemetry types |
| **Proper Aggregation** | Metrics should be pre-aggregated using Meter API, not sent as raw values with events |
| **Performance** | Sending metrics embedded in events creates high cardinality and poor performance |
| **Standards Compliance** | OpenTelemetry specification doesn't support metrics in event payloads |

## Migration Strategies

### Option 1: Migrate to Meter API (Recommended)

**When to use:** Numerical measurements that should be aggregated (counts, durations, sizes).

**2.x:**
```csharp
public class OrderService
{
    private readonly TelemetryClient telemetryClient;
    
    public void ProcessOrder(Order order)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Process order logic
        ProcessOrderInternal(order);
        
        stopwatch.Stop();
        
        // Track event with embedded metrics
        telemetryClient.TrackEvent("OrderProcessed", 
            properties: new Dictionary<string, string>
            {
                ["OrderId"] = order.Id.ToString(),
                ["Status"] = "Success"
            },
            metrics: new Dictionary<string, double>
            {
                ["ProcessingTimeMs"] = stopwatch.Elapsed.TotalMilliseconds,
                ["OrderValue"] = order.TotalAmount,
                ["ItemCount"] = order.Items.Count
            });
    }
}
```

**3.x:**
```csharp
using System.Diagnostics.Metrics;

public class OrderService
{
    private readonly TelemetryClient telemetryClient;
    private static readonly Meter Meter = new("MyApp.Orders");
    
    // Define metrics once as static fields
    private static readonly Histogram<double> ProcessingTime = 
        Meter.CreateHistogram<double>("order.processing.time", unit: "ms", 
            description: "Time taken to process an order");
    
    private static readonly Histogram<double> OrderValue = 
        Meter.CreateHistogram<double>("order.value", unit: "USD",
            description: "Value of processed orders");
    
    private static readonly Counter<int> OrdersProcessed = 
        Meter.CreateCounter<int>("orders.processed",
            description: "Number of orders processed");
    
    public void ProcessOrder(Order order)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Process order logic
        ProcessOrderInternal(order);
        
        stopwatch.Stop();
        
        // Track event (for business logic tracking)
        telemetryClient.TrackEvent("OrderProcessed", new Dictionary<string, string>
        {
            ["order.id"] = order.Id.ToString(),
            ["status"] = "Success"
        });
        
        // Record metrics separately
        ProcessingTime.Record(stopwatch.Elapsed.TotalMilliseconds, 
            new KeyValuePair<string, object>("order.status", "Success"));
        
        OrderValue.Record(order.TotalAmount,
            new KeyValuePair<string, object>("order.status", "Success"));
        
        OrdersProcessed.Add(1,
            new KeyValuePair<string, object>("order.status", "Success"));
    }
}

// Register Meter in Program.cs
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.ConfigureOpenTelemetryMeterProvider(builder =>
{
    builder.AddMeter("MyApp.Orders");
});
```

### Option 2: Store as Event Properties (Not Recommended)

**When to use:** Rare cases where you need to preserve exact values with events (avoid if possible).

**2.x:**
```csharp
telemetryClient.TrackEvent("OrderProcessed",
    properties: new Dictionary<string, string>
    {
        ["OrderId"] = orderId
    },
    metrics: new Dictionary<string, double>
    {
        ["ProcessingTime"] = 1234.5
    });
```

**3.x (Anti-pattern - avoid):**
```csharp
// Storing metrics as string properties loses aggregation capability
telemetryClient.TrackEvent("OrderProcessed", new Dictionary<string, string>
{
    ["order.id"] = orderId,
    ["processing.time"] = "1234.5",  // Stored as string, can't aggregate
    ["item.count"] = "5"
});

// Better: Use Meter API for actual metrics (see Option 1)
```

## Common Scenarios

### Scenario 1: Exception with Metrics

**2.x:**
```csharp
try
{
    ProcessData();
}
catch (Exception ex)
{
    telemetryClient.TrackException(ex,
        properties: new Dictionary<string, string>
        {
            ["Operation"] = "DataProcessing"
        },
        metrics: new Dictionary<string, double>
        {
            ["RecordsProcessed"] = recordsProcessed,
            ["FailurePoint"] = currentIndex
        });
}
```

**3.x:**
```csharp
// Define metrics
private static readonly Meter Meter = new("MyApp.DataProcessing");
private static readonly Counter<long> RecordsProcessed = 
    Meter.CreateCounter<long>("records.processed");
private static readonly Counter<long> ProcessingErrors = 
    Meter.CreateCounter<long>("processing.errors");

try
{
    ProcessData();
}
catch (Exception ex)
{
    // Track exception (no metrics parameter)
    telemetryClient.TrackException(ex, new Dictionary<string, string>
    {
        ["operation"] = "DataProcessing",
        ["failure.point"] = currentIndex.ToString()
    });
    
    // Record metrics separately
    RecordsProcessed.Add(recordsProcessed, 
        new KeyValuePair<string, object>("status", "failed"));
    
    ProcessingErrors.Add(1,
        new KeyValuePair<string, object>("operation", "DataProcessing"));
}
```

### Scenario 2: Availability Test with Metrics

**2.x:**
```csharp
var stopwatch = Stopwatch.StartNew();
bool success = false;

try
{
    var response = await httpClient.GetAsync(endpoint);
    success = response.IsSuccessStatusCode;
}
catch
{
    success = false;
}
finally
{
    stopwatch.Stop();
}

telemetryClient.TrackAvailability("MyApiEndpoint", 
    DateTimeOffset.UtcNow, 
    stopwatch.Elapsed,
    "West US",
    success,
    properties: new Dictionary<string, string>
    {
        ["Endpoint"] = endpoint
    },
    metrics: new Dictionary<string, double>
    {
        ["ResponseSize"] = responseSize,
        ["ConnectionTime"] = connectionTimeMs
    });
```

**3.x:**
```csharp
// Define metrics
private static readonly Meter Meter = new("MyApp.Availability");
private static readonly Histogram<long> ResponseSize = 
    Meter.CreateHistogram<long>("http.response.size", unit: "bytes");
private static readonly Histogram<double> ConnectionTime = 
    Meter.CreateHistogram<double>("http.connection.time", unit: "ms");

var stopwatch = Stopwatch.StartNew();
bool success = false;
long responseSize = 0;
double connectionTimeMs = 0;

try
{
    var response = await httpClient.GetAsync(endpoint);
    success = response.IsSuccessStatusCode;
    responseSize = response.Content.Headers.ContentLength ?? 0;
}
catch
{
    success = false;
}
finally
{
    stopwatch.Stop();
}

// Track availability (no metrics parameter)
telemetryClient.TrackAvailability("MyApiEndpoint",
    DateTimeOffset.UtcNow,
    stopwatch.Elapsed,
    "West US",
    success,
    properties: new Dictionary<string, string>
    {
        ["endpoint"] = endpoint
    });

// Record metrics separately
ResponseSize.Record(responseSize,
    new KeyValuePair<string, object>("endpoint", endpoint),
    new KeyValuePair<string, object>("success", success));

ConnectionTime.Record(connectionTimeMs,
    new KeyValuePair<string, object>("endpoint", endpoint));
```

### Scenario 3: Business Event with Measurements

**2.x:**
```csharp
public void RecordSale(decimal amount, int quantity, string productId)
{
    telemetryClient.TrackEvent("SaleCompleted",
        properties: new Dictionary<string, string>
        {
            ["ProductId"] = productId,
            ["PaymentMethod"] = "CreditCard"
        },
        metrics: new Dictionary<string, double>
        {
            ["SaleAmount"] = (double)amount,
            ["Quantity"] = quantity,
            ["DiscountApplied"] = discountAmount
        });
}
```

**3.x:**
```csharp
// Define metrics
private static readonly Meter Meter = new("MyApp.Sales");
private static readonly Histogram<decimal> SaleAmount = 
    Meter.CreateHistogram<decimal>("sale.amount", unit: "USD");
private static readonly Histogram<int> SaleQuantity = 
    Meter.CreateHistogram<int>("sale.quantity", unit: "items");
private static readonly Counter<int> SalesCount = 
    Meter.CreateCounter<int>("sales.count");

public void RecordSale(decimal amount, int quantity, string productId)
{
    // Track business event
    telemetryClient.TrackEvent("SaleCompleted", new Dictionary<string, string>
    {
        ["product.id"] = productId,
        ["payment.method"] = "CreditCard"
    });
    
    // Record metrics with dimensions
    var tags = new TagList
    {
        { "product.id", productId },
        { "payment.method", "CreditCard" }
    };
    
    SaleAmount.Record(amount, tags);
    SaleQuantity.Record(quantity, tags);
    SalesCount.Add(1, tags);
}
```

## Meter API Instrument Types

### Counter (Monotonic Increasing)

**Use for:** Counts that only increase (requests, errors, items sold).

```csharp
private static readonly Counter<int> RequestCount = 
    Meter.CreateCounter<int>("http.requests.count");

RequestCount.Add(1, 
    new KeyValuePair<string, object>("http.method", "GET"),
    new KeyValuePair<string, object>("http.status_code", 200));
```

### Histogram (Value Distribution)

**Use for:** Measurements where distribution matters (latency, size, duration).

```csharp
private static readonly Histogram<double> RequestDuration = 
    Meter.CreateHistogram<double>("http.request.duration", unit: "ms");

RequestDuration.Record(125.4,
    new KeyValuePair<string, object>("http.method", "POST"),
    new KeyValuePair<string, object>("http.route", "/api/orders"));
```

### UpDownCounter (Can Increase or Decrease)

**Use for:** Values that go up and down (active connections, queue length).

```csharp
private static readonly UpDownCounter<int> ActiveConnections = 
    Meter.CreateUpDownCounter<int>("active.connections");

ActiveConnections.Add(1);   // Connection opened
ActiveConnections.Add(-1);  // Connection closed
```

### ObservableGauge (Current Value)

**Use for:** Point-in-time measurements (memory usage, temperature).

```csharp
private static readonly ObservableGauge<long> MemoryUsage = 
    Meter.CreateObservableGauge<long>("process.memory.usage", 
        () => GC.GetTotalMemory(false),
        unit: "bytes");
```

## Registration in Program.cs

### ASP.NET Core

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
});

// Register custom meters
builder.Services.ConfigureOpenTelemetryMeterProvider(meterBuilder =>
{
    meterBuilder.AddMeter("MyApp.Orders");
    meterBuilder.AddMeter("MyApp.Sales");
    meterBuilder.AddMeter("MyApp.DataProcessing");
});

var app = builder.Build();
```

### Console Application / Worker Service

```csharp
var services = new ServiceCollection();

services.AddApplicationInsightsTelemetryWorkerService(options =>
{
    options.ConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
});

services.ConfigureOpenTelemetryMeterProvider(builder =>
{
    builder.AddMeter("MyApp.Orders");
});

var serviceProvider = services.BuildServiceProvider();
```

## Azure Monitor Queries

### 2.x Query (Embedded Metrics)

```kusto
customEvents
| where name == "OrderProcessed"
| extend ProcessingTime = todouble(customMeasurements.ProcessingTimeMs)
| extend OrderValue = todouble(customMeasurements.OrderValue)
| summarize avg(ProcessingTime), sum(OrderValue) by bin(timestamp, 1h)
```

### 3.x Query (Separate Metrics)

```kusto
// Events
customEvents
| where name == "OrderProcessed"
| summarize count() by bin(timestamp, 1h)

// Metrics
customMetrics
| where name == "order.processing.time"
| summarize avg(value), percentile(value, 95) by bin(timestamp, 1h)

customMetrics
| where name == "order.value"
| summarize sum(value) by bin(timestamp, 1h)
```

## Migration Checklist

- [ ] Identify all `TrackEvent()`, `TrackException()`, and `TrackAvailability()` calls with `metrics` parameter
- [ ] For each metrics dictionary entry:
  - [ ] Determine appropriate instrument type (Counter, Histogram, UpDownCounter, ObservableGauge)
  - [ ] Create static Meter and instrument fields
  - [ ] Replace embedded metrics with instrument.Record() or .Add() calls
- [ ] Register custom meters in Program.cs using `ConfigureOpenTelemetryMeterProvider()`
- [ ] Update Azure Monitor queries:
  - [ ] Change from `customEvents.customMeasurements` to `customMetrics` table
  - [ ] Update dashboard queries and alerts
- [ ] Test metric reporting in Azure Monitor
- [ ] Consider adding dimensions (tags) to metrics for better filtering
- [ ] Remove `metrics` parameter from all Track method calls

## See Also

- [GetMetric-simplified.md](GetMetric-simplified.md) - GetMetric() changes
- [meter-and-metrics.md](../../opentelemetry-fundamentals/meter-and-metrics.md) - OpenTelemetry Meter API fundamentals
- [custom-metrics.md](../../common-scenarios/custom-metrics.md) - Custom metrics patterns
