# TelemetryClient.GetMetric() Simplified

**Category:** Breaking Change  
**Applies to:** TelemetryClient API  
**Migration Effort:** Medium  
**Related:** [metrics-parameter-removed.md](metrics-parameter-removed.md), [meter-and-metrics.md](../../opentelemetry-fundamentals/meter-and-metrics.md)

## Change Summary

The `GetMetric()` method family has been significantly simplified in 3.x. Complex configuration parameters (`MetricConfiguration`, `MetricAggregationScope`) have been removed. The simplified API now only supports basic dimensions. For advanced metrics, use the OpenTelemetry Meter API instead.

## API Comparison

### 2.x API (Complex Configuration)

```csharp
// Source: ApplicationInsights-dotnet-2x/BASE/src/Microsoft.ApplicationInsights/TelemetryClient.cs:1055-1063
public Metric GetMetric(
    string metricId,
    string dimension1Name,
    string dimension2Name,
    string dimension3Name,
    MetricConfiguration metricConfiguration)
{
    return this.GetOrCreateMetric(
        MetricAggregationScope.TelemetryConfiguration,
        new MetricIdentifier(MetricIdentifier.DefaultMetricNamespace, metricId, dimension1Name, dimension2Name, dimension3Name),
        metricConfiguration);
}
```

### 3.x API (Simplified)

```csharp
// Source: ApplicationInsights-dotnet/BASE/src/Microsoft.ApplicationInsights/TelemetryClient.cs:972-976
public Metric GetMetric(string metricId)
{
    return new Metric(this, metricId, null, null);
}

// Source: ApplicationInsights-dotnet/BASE/src/Microsoft.ApplicationInsights/TelemetryClient.cs:995-1000
public Metric GetMetric(string metricId, string dimension1Name)
{
    return new Metric(this, metricId, null, new[] { dimension1Name });
}

// REMOVED parameters:
// - MetricConfiguration metricConfiguration
// - MetricAggregationScope aggregationScope
// - MetricIdentifier (with namespace customization)
// - dimension2Name, dimension3Name, dimension4Name overloads
```

## Why It Changed

| Reason | Description |
|--------|-------------|
| **OpenTelemetry Standard** | OpenTelemetry Meter API is the standard for metrics - GetMetric was SDK-specific |
| **Simplified API Surface** | Complex configuration options were rarely used and confusing |
| **Better Aggregation** | OpenTelemetry Meter provides better aggregation with Views and Temporality control |
| **Multi-Dimensional Support** | Meter API supports unlimited dimensions via TagList, not limited to 4 |

## Migration Strategies

### Option 1: Simple Metric Migration

**When to use:** Basic GetMetric usage without custom configuration.

**2.x:**
```csharp
public class OrderService
{
    private readonly TelemetryClient telemetryClient;
    
    public void ProcessOrder(Order order)
    {
        // Get metric and track value
        var metric = telemetryClient.GetMetric("OrdersProcessed");
        metric.TrackValue(1);
        
        // With dimension
        var metricWithDim = telemetryClient.GetMetric("OrderValue", "Region");
        metricWithDim.TrackValue(order.Amount, "WestUS");
    }
}
```

**3.x:**
```csharp
using System.Diagnostics.Metrics;

public class OrderService
{
    private static readonly Meter Meter = new("MyApp.Orders");
    private static readonly Counter<int> OrdersProcessed = 
        Meter.CreateCounter<int>("orders.processed");
    private static readonly Histogram<double> OrderValue = 
        Meter.CreateHistogram<double>("order.value", unit: "USD");
    
    public void ProcessOrder(Order order)
    {
        // Track counter
        OrdersProcessed.Add(1);
        
        // Track histogram with dimension
        OrderValue.Record(order.Amount, 
            new KeyValuePair<string, object>("region", "WestUS"));
    }
}

// Register in Program.cs
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.ConfigureOpenTelemetryMeterProvider(builder =>
{
    builder.AddMeter("MyApp.Orders");
});
```

### Option 2: Multi-Dimensional Metrics

**When to use:** Metrics with multiple dimensions (2.x supported up to 4, 3.x supports unlimited).

**2.x:**
```csharp
public class RequestHandler
{
    private readonly TelemetryClient telemetryClient;
    
    public async Task HandleRequest(HttpRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Process request...
        
        stopwatch.Stop();
        
        // Multi-dimensional metric (limited to 4 dimensions)
        var metric = telemetryClient.GetMetric(
            "RequestDuration",
            "Endpoint",
            "Method",
            "StatusCode");
        
        metric.TrackValue(
            stopwatch.Elapsed.TotalMilliseconds,
            request.Path,
            request.Method,
            "200");
    }
}
```

**3.x:**
```csharp
using System.Diagnostics.Metrics;

public class RequestHandler
{
    private static readonly Meter Meter = new("MyApp.Http");
    private static readonly Histogram<double> RequestDuration = 
        Meter.CreateHistogram<double>("http.request.duration", unit: "ms");
    
    public async Task HandleRequest(HttpRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Process request...
        
        stopwatch.Stop();
        
        // Multi-dimensional - unlimited dimensions via TagList
        var tags = new TagList
        {
            { "http.route", request.Path },
            { "http.method", request.Method },
            { "http.status_code", 200 },
            { "server.address", request.Host.ToString() },  // Additional dimensions
            { "user.authenticated", request.User?.Identity?.IsAuthenticated ?? false }
        };
        
        RequestDuration.Record(stopwatch.Elapsed.TotalMilliseconds, tags);
    }
}
```

### Option 3: Custom Metric Configuration

**When to use:** 2.x used MetricConfiguration for custom aggregation behavior.

**2.x:**
```csharp
public class PerformanceMonitor
{
    private readonly TelemetryClient telemetryClient;
    
    public void TrackPerformance(double value)
    {
        // Custom aggregation configuration
        var config = new MetricConfiguration(
            seriesCountLimit: 1000,
            valuesPerDimensionLimit: 100,
            new MetricSeriesConfigurationForMeasurement(restrictToUInt32Values: false));
        
        var metric = telemetryClient.GetMetric(
            new MetricIdentifier("MyNamespace", "PerformanceMetric", "Component"),
            config,
            MetricAggregationScope.TelemetryClient);
        
        metric.TrackValue(value, "Database");
    }
}
```

**3.x:**
```csharp
using System.Diagnostics.Metrics;
using OpenTelemetry.Metrics;

public class PerformanceMonitor
{
    private static readonly Meter Meter = new("MyNamespace.Performance");
    private static readonly Histogram<double> PerformanceMetric = 
        Meter.CreateHistogram<double>("performance.metric");
    
    public void TrackPerformance(double value, string component)
    {
        PerformanceMetric.Record(value, 
            new KeyValuePair<string, object>("component", component));
    }
}

// Configure aggregation via MeterProvider (in Program.cs)
builder.Services.ConfigureOpenTelemetryMeterProvider(builder =>
{
    builder.AddMeter("MyNamespace.Performance");
    
    // Configure View for custom aggregation behavior
    builder.AddView("performance.metric", new ExplicitBucketHistogramConfiguration
    {
        Boundaries = new double[] { 0, 10, 50, 100, 500, 1000, 5000 }
    });
});
```

## Common Scenarios

### Scenario 1: Request Counting with Dimensions

**2.x:**
```csharp
public class ApiMetrics
{
    private readonly TelemetryClient telemetryClient;
    
    public void RecordApiCall(string endpoint, string method, bool success)
    {
        var metric = telemetryClient.GetMetric("ApiCalls", "Endpoint", "Method", "Success");
        metric.TrackValue(1, endpoint, method, success.ToString());
    }
}
```

**3.x:**
```csharp
using System.Diagnostics.Metrics;

public class ApiMetrics
{
    private static readonly Meter Meter = new("MyApp.Api");
    private static readonly Counter<int> ApiCalls = 
        Meter.CreateCounter<int>("api.calls");
    
    public void RecordApiCall(string endpoint, string method, bool success)
    {
        ApiCalls.Add(1, 
            new KeyValuePair<string, object>("endpoint", endpoint),
            new KeyValuePair<string, object>("method", method),
            new KeyValuePair<string, object>("success", success));
    }
}
```

### Scenario 2: Performance Percentiles

**2.x:**
```csharp
public class DatabaseMetrics
{
    private readonly TelemetryClient telemetryClient;
    
    public void TrackQueryDuration(double durationMs, string queryType)
    {
        var config = MetricConfigurations.Common.Measurement();
        var metric = telemetryClient.GetMetric("QueryDuration", "QueryType", config);
        metric.TrackValue(durationMs, queryType);
    }
}
```

**3.x:**
```csharp
using System.Diagnostics.Metrics;

public class DatabaseMetrics
{
    private static readonly Meter Meter = new("MyApp.Database");
    private static readonly Histogram<double> QueryDuration = 
        Meter.CreateHistogram<double>("db.query.duration", unit: "ms");
    
    public void TrackQueryDuration(double durationMs, string queryType)
    {
        QueryDuration.Record(durationMs, 
            new KeyValuePair<string, object>("db.query.type", queryType));
    }
}

// Configure histogram buckets in Program.cs for percentile calculation
builder.Services.ConfigureOpenTelemetryMeterProvider(builder =>
{
    builder.AddView("db.query.duration", new ExplicitBucketHistogramConfiguration
    {
        // Buckets optimized for query duration (milliseconds)
        Boundaries = new double[] { 0, 5, 10, 25, 50, 75, 100, 250, 500, 750, 1000, 2500, 5000, 7500, 10000 }
    });
});
```

### Scenario 3: Business Metrics

**2.x:**
```csharp
public class SalesMetrics
{
    private readonly TelemetryClient telemetryClient;
    
    public void TrackSale(decimal amount, string productCategory, string region)
    {
        var metric = telemetryClient.GetMetric("SalesRevenue", "Category", "Region");
        metric.TrackValue((double)amount, productCategory, region);
    }
}
```

**3.x:**
```csharp
using System.Diagnostics.Metrics;

public class SalesMetrics
{
    private static readonly Meter Meter = new("MyApp.Sales");
    private static readonly Histogram<decimal> SalesRevenue = 
        Meter.CreateHistogram<decimal>("sales.revenue", unit: "USD");
    
    public void TrackSale(decimal amount, string productCategory, string region)
    {
        SalesRevenue.Record(amount, 
            new KeyValuePair<string, object>("product.category", productCategory),
            new KeyValuePair<string, object>("region", region));
    }
}
```

## Meter API Advantages Over GetMetric

### 1. Unlimited Dimensions

**2.x Limitation:**
```csharp
// Maximum 4 dimensions
var metric = telemetryClient.GetMetric("Metric", "Dim1", "Dim2", "Dim3", "Dim4");
```

**3.x Capability:**
```csharp
// Unlimited dimensions via TagList
var tags = new TagList
{
    { "dim1", value1 },
    { "dim2", value2 },
    { "dim3", value3 },
    { "dim4", value4 },
    { "dim5", value5 },
    // ... as many as needed
};
histogram.Record(value, tags);
```

### 2. Better Type Safety

**2.x:**
```csharp
// All values are double
metric.TrackValue(42.0);  // Must cast int to double
```

**3.x:**
```csharp
// Type-specific instruments
Counter<int> intCounter = meter.CreateCounter<int>("count");
Counter<long> longCounter = meter.CreateCounter<long>("count.large");
Histogram<decimal> decimalHist = meter.CreateHistogram<decimal>("money");
```

### 3. Instrument-Specific Semantics

**2.x:**
```csharp
// Same API for all metric types
metric.TrackValue(1);  // Is this a count, gauge, or measurement?
```

**3.x:**
```csharp
// Clear semantic meaning
counter.Add(1);              // Monotonically increasing count
upDownCounter.Add(-1);       // Can increase or decrease
histogram.Record(duration);  // Value distribution
observableGauge.Observe();   // Current value snapshot
```

### 4. Configuration via Views

**3.x Views provide powerful aggregation control:**
```csharp
builder.Services.ConfigureOpenTelemetryMeterProvider(builder =>
{
    // Rename metric
    builder.AddView(
        instrumentName: "old.metric.name",
        name: "new.metric.name");
    
    // Custom histogram buckets
    builder.AddView("http.request.duration", new ExplicitBucketHistogramConfiguration
    {
        Boundaries = new double[] { 0, 50, 100, 500, 1000, 5000 }
    });
    
    // Drop specific dimensions
    builder.AddView("high.cardinality.metric", new MetricStreamConfiguration
    {
        TagKeys = new string[] { "keep.this.dimension" }  // Others dropped
    });
});
```

## Azure Monitor Queries

### 2.x Query (GetMetric)

```kusto
customMetrics
| where name == "OrderValue"
| extend Region = tostring(customDimensions.Region)
| summarize sum(value), avg(value), percentile(value, 95) by Region, bin(timestamp, 1h)
```

### 3.x Query (Meter API)

```kusto
customMetrics
| where name == "order.value"
| extend region = tostring(customDimensions.region)
| summarize sum(value), avg(value), percentile(value, 95) by region, bin(timestamp, 1h)
```

## Migration Checklist

- [ ] Identify all `GetMetric()` calls in codebase
- [ ] For each GetMetric usage:
  - [ ] Determine appropriate Meter instrument type:
    - `Counter<T>` for monotonically increasing values
    - `Histogram<T>` for value distributions
    - `UpDownCounter<T>` for values that increase and decrease
    - `ObservableGauge<T>` for point-in-time observations
  - [ ] Create static `Meter` instance with appropriate name
  - [ ] Create static instrument fields
  - [ ] Replace `metric.TrackValue()` with instrument-specific methods (`Add()`, `Record()`)
- [ ] Convert dimensions to TagList or KeyValuePair parameters
- [ ] Register meters in `ConfigureOpenTelemetryMeterProvider()`
- [ ] If using custom MetricConfiguration:
  - [ ] Replace with Views in MeterProvider configuration
  - [ ] Configure histogram boundaries if needed
- [ ] Update Azure Monitor queries if metric names changed
- [ ] Test metrics appear correctly in Azure Monitor

## See Also

- [metrics-parameter-removed.md](metrics-parameter-removed.md) - Track methods metrics parameter removal
- [meter-and-metrics.md](../../opentelemetry-fundamentals/meter-and-metrics.md) - OpenTelemetry Meter API fundamentals
- [custom-metrics.md](../../common-scenarios/custom-metrics.md) - Custom metrics patterns
