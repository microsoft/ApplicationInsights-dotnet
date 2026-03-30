# Custom Metrics

## Overview

Create custom metrics using the `System.Diagnostics.Metrics` API. These are automatically exported to Application Insights when Azure Monitor is configured.

## Setup

Define a `Meter` and create instruments:

```csharp
using System.Diagnostics.Metrics;

public class OrderMetrics
{
    private readonly Counter<long> _ordersPlaced;
    private readonly Histogram<double> _orderProcessingTime;

    public OrderMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("MyApp.Orders");
        _ordersPlaced = meter.CreateCounter<long>("orders.placed", "orders", "Total orders placed");
        _orderProcessingTime = meter.CreateHistogram<double>("orders.processing_time", "ms", "Order processing time");
    }

    public void RecordOrderPlaced() => _ordersPlaced.Add(1);
    public void RecordProcessingTime(double ms) => _orderProcessingTime.Record(ms);
}
```

## Register and Use

```csharp
// Register in DI
builder.Services.AddSingleton<OrderMetrics>();

// Register the meter with OpenTelemetry so it's exported
builder.Services.ConfigureOpenTelemetryMeterProvider(metrics =>
    metrics.AddMeter("MyApp.Orders"));
```

```csharp
// Use in your code
public class OrderController : ControllerBase
{
    private readonly OrderMetrics _metrics;

    public OrderController(OrderMetrics metrics) => _metrics = metrics;

    [HttpPost]
    public IActionResult PlaceOrder(Order order)
    {
        var sw = Stopwatch.StartNew();
        // ... process order ...
        _metrics.RecordOrderPlaced();
        _metrics.RecordProcessingTime(sw.Elapsed.TotalMilliseconds);
        return Ok();
    }
}
```

## Instrument Types

| Instrument | Use Case | Example |
|---|---|---|
| `Counter<T>` | Running total | Orders placed, errors |
| `Histogram<T>` | Distribution | Response time, payload size |
| `UpDownCounter<T>` | Value that goes up and down | Active connections, queue depth |
| `ObservableGauge<T>` | Point-in-time snapshot | CPU usage, memory |

## Non-DI Usage (Console / Classic ASP.NET)

Without DI, create meters directly and register manually:

```csharp
using System.Diagnostics.Metrics;

// Create meter directly (no IMeterFactory)
var meter = new Meter("MyApp.Orders");
var ordersPlaced = meter.CreateCounter<long>("orders.placed");

// Register with OpenTelemetry
var config = TelemetryConfiguration.CreateDefault();
config.ConnectionString = "InstrumentationKey=...;IngestionEndpoint=...";
config.ConfigureOpenTelemetryBuilder(otel =>
    otel.WithMetrics(m => m.AddMeter("MyApp.Orders")));

// Use directly
ordersPlaced.Add(1);
```

## Notes

- The meter name passed to `AddMeter()` must match the name used when creating the `Meter`
- Metrics appear in Application Insights under Custom Metrics
- Use `IMeterFactory` (available via DI in .NET 8+) for testable meters, or `new Meter()` for non-DI
- Tags added to measurements become metric dimensions in Application Insights
