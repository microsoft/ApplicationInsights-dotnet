---
title: OpenTelemetry Pipeline - How Telemetry Flows in 3.x
category: concept
applies-to: 3.x
related:
  - concepts/activity-processor.md
  - concepts/configure-otel-builder.md
  - transformations/ITelemetryInitializer/overview.md
  - transformations/ITelemetryProcessor/overview.md
source: OpenTelemetry .NET SDK architecture
---

# OpenTelemetry Pipeline - How Telemetry Flows in 3.x

## Overview

Understanding the OpenTelemetry pipeline is crucial for migrating from Application Insights 2.x. The pipeline architecture is fundamentally different, replacing the 2.x chain of initializers and processors with OpenTelemetry's instrumentation and processor model.

## 2.x Pipeline Architecture

```
Application Code
    ↓
TelemetryClient.Track*()
    ↓
TelemetryInitializers (enrichment)
    ↓
TelemetryProcessors (filtering/sampling)
    ↓
TelemetryChannel (batching)
    ↓
Azure Monitor
```

### 2.x Flow

```csharp
// 2.x: Explicit tracking
telemetryClient.TrackRequest(request);
    → All Initializers called (enrich)
    → Processor chain called (filter/sample)
    → Channel buffers and sends
    → Azure Monitor ingests
```

## 3.x Pipeline Architecture

```
Application Code / Instrumentation
    ↓
ActivitySource / Meter / ILogger
    ↓
OpenTelemetry TracerProvider / MeterProvider / LoggerProvider
    ↓
Processors (OnStart/OnEnd)
    ↓
Exporters (Azure Monitor, Console, OTLP)
    ↓
Azure Monitor / Other Backends
```

### 3.x Flow

```csharp
// 3.x: Automatic or explicit instrumentation
HttpClient.GetAsync() // Built-in instrumentation creates Activity
    → TracerProvider receives Activity
    → Processors.OnStart() called (early enrichment)
    → Operation executes
    → Processors.OnEnd() called (final enrichment/filtering)
    → Exporters receive finalized data
    → Azure Monitor Exporter sends to AI
```

## Key Architectural Differences

| Component | 2.x | 3.x |
|-----------|-----|-----|
| **Telemetry Creation** | Manual `Track*()` calls | Automatic via instrumentation libraries |
| **Enrichment** | ITelemetryInitializer | BaseProcessor<Activity>.OnEnd |
| **Filtering** | ITelemetryProcessor chain | BaseProcessor<Activity>.OnEnd |
| **Chaining** | Manual `next.Process()` | Automatic by SDK |
| **Transport** | ITelemetryChannel | Exporter |
| **Multiple Destinations** | TelemetrySinks | Multiple Exporters |

## 3.x Pipeline Components in Detail

### 1. Instrumentation (Data Source)

Where telemetry originates:

```csharp
// Built-in .NET instrumentation
HttpClient httpClient = new();
await httpClient.GetAsync("https://api.example.com");
// ↑ Automatically creates Activity (no manual tracking needed)

// Custom instrumentation
using (var activity = activitySource.StartActivity("ProcessOrder"))
{
    activity?.SetTag("order.id", orderId);
    // Manual instrumentation
}
```

### 2. Providers (Collection)

Collect telemetry from instrumentation:

```csharp
config.ConfigureOpenTelemetryBuilder(builder =>
{
    // TracerProvider - collects Activities (traces)
    builder.AddSource("MyApp.*");
    builder.AddHttpClientInstrumentation();
    
    // MeterProvider - collects metrics
    builder.AddMeter("MyApp.Metrics");
    
    // LoggerProvider - collects logs
    builder.AddInstrumentation<ILogger>();
});
```

### 3. Processors (Enrichment/Filtering)

Process telemetry before export:

```csharp
public class MyProcessor : BaseProcessor<Activity>
{
    // Called when Activity starts
    public override void OnStart(Activity activity)
    {
        activity.SetTag("processor.start", DateTime.UtcNow);
    }
    
    // Called when Activity ends
    public override void OnEnd(Activity activity)
    {
        // Enrich
        activity.SetTag("custom.dimension", "value");
        
        // Filter
        if (ShouldDrop(activity))
        {
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
        }
    }
}
```

### 4. Exporters (Transmission)

Send telemetry to backends:

```csharp
config.ConfigureOpenTelemetryBuilder(builder =>
{
    // Primary: Azure Monitor Exporter (automatic)
    // Secondary: Console for debugging
    builder.AddConsoleExporter();
    
    // Tertiary: OTLP for other tools
    builder.AddOtlpExporter();
});
```

## Complete Pipeline Example

```csharp
// Setup (once at startup)
var config = new TelemetryConfiguration();
config.ConnectionString = "InstrumentationKey=...";

config.ConfigureOpenTelemetryBuilder(builder =>
{
    // 1. Register instrumentation (data sources)
    builder.AddHttpClientInstrumentation();
    builder.AddAspNetCoreInstrumentation();
    builder.AddSource("MyApp.*");
    
    // 2. Add processors (enrichment/filtering)
    builder.AddProcessor(new EnvironmentProcessor());
    builder.AddProcessor(new HealthCheckFilterProcessor());
    
    // 3. Configure resource (service identity)
    builder.ConfigureResource(resource =>
    {
        resource.AddService("MyService");
    });
    
    // 4. Exporters configured automatically (Azure Monitor)
    // Can add more:
    builder.AddConsoleExporter();
});

// Runtime
var client = new TelemetryClient(config);

// Telemetry flows automatically:
await httpClient.GetAsync("https://api.example.com");

// Flow:
// 1. HttpClient instrumentation creates Activity
// 2. TracerProvider collects it
// 3. EnvironmentProcessor.OnStart() called
// 4. HTTP request executes
// 5. EnvironmentProcessor.OnEnd() called
// 6. HealthCheckFilterProcessor.OnEnd() called
// 7. Azure Monitor Exporter sends to AI
// 8. Console Exporter logs to console
```

## Processor Execution Order

Processors are called in registration order:

```csharp
builder.AddProcessor(new Processor1());  // Called first
builder.AddProcessor(new Processor2());  // Called second
builder.AddProcessor(new Processor3());  // Called third

// Execution for each Activity:
// 1. Processor1.OnStart()
// 2. Processor2.OnStart()
// 3. Processor3.OnStart()
// --- Activity executes ---
// 4. Processor1.OnEnd()
// 5. Processor2.OnEnd()
// 6. Processor3.OnEnd()
// 7. Export to Azure Monitor
```

### No Manual Chaining Needed

Unlike 2.x, you don't manage the chain:

```csharp
// 2.x: Manual chaining
public class MyProcessor : ITelemetryProcessor
{
    private ITelemetryProcessor next;
    
    public MyProcessor(ITelemetryProcessor next)
    {
        this.next = next;  // Manual chain management
    }
    
    public void Process(ITelemetry item)
    {
        // Do work
        this.next.Process(item);  // Must call next!
    }
}

// 3.x: Automatic chaining
public class MyProcessor : BaseProcessor<Activity>
{
    // No constructor needed
    // No next parameter
    // No manual forwarding
    
    public override void OnEnd(Activity activity)
    {
        // Do work
        // Automatically forwarded to next processor
    }
}
```

## Automatic Instrumentation

One of the biggest differences: much telemetry is automatic in 3.x.

### 2.x: Manual Tracking

```csharp
// 2.x: Must manually track everything
using (var operation = telemetryClient.StartOperation<DependencyTelemetry>("HTTP GET"))
{
    try
    {
        operation.Telemetry.Type = "HTTP";
        operation.Telemetry.Target = "api.example.com";
        
        var response = await httpClient.GetAsync("https://api.example.com");
        
        operation.Telemetry.Success = response.IsSuccessStatusCode;
        operation.Telemetry.ResultCode = ((int)response.StatusCode).ToString();
    }
    catch (Exception ex)
    {
        operation.Telemetry.Success = false;
        telemetryClient.TrackException(ex);
        throw;
    }
}
```

### 3.x: Automatic Tracking

```csharp
// 3.x: Completely automatic
var response = await httpClient.GetAsync("https://api.example.com");
// ↑ Activity created, started, tagged, stopped, exported automatically
// No manual tracking code needed!
```

## Multi-Signal Pipeline

3.x handles three telemetry signals in parallel:

### Traces (Activities)
```
ActivitySource → TracerProvider → Processors → Exporters
```

### Metrics (Measurements)
```
Meter → MeterProvider → Metric Processors → Exporters
```

### Logs (ILogger)
```
ILogger → LoggerProvider → Log Processors → Exporters
```

Each signal has its own pipeline but shares configuration:

```csharp
config.ConfigureOpenTelemetryBuilder(builder =>
{
    // Traces
    builder.AddHttpClientInstrumentation();  // Activities
    builder.AddProcessor<MyActivityProcessor>();
    
    // Metrics
    builder.AddMeter("MyApp.Metrics");
    builder.AddMetricReader(new MyMetricReader());
    
    // Logs
    builder.AddInstrumentation<ILogger>();
    builder.AddProcessor<MyLogProcessor>();
    
    // All three export to same destinations
});
```

## Sampling in the Pipeline

### 2.x Sampling
```csharp
// 2.x: Sampling done in processor
public class SamplingProcessor : ITelemetryProcessor
{
    public void Process(ITelemetry item)
    {
        if (ShouldSample())
        {
            next.Process(item);  // Keep it
        }
        // else drop it
    }
}
```

### 3.x Sampling
```csharp
// 3.x: Sampling via ActivityTraceFlags
public class SamplingProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        if (!ShouldSample())
        {
            // Mark as not recorded (early decision)
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
        }
    }
}

// Or use built-in samplers
config.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.SetSampler(new TraceIdRatioBasedSampler(0.1));  // 10% sampling
});
```

## Resource vs Per-Telemetry Data

### 2.x: All Data Per-Telemetry
```csharp
// 2.x: Set on every telemetry item
public void Initialize(ITelemetry telemetry)
{
    telemetry.Context.Cloud.RoleName = "MyService";  // Repeated for every item
    telemetry.Context.Component.Version = "1.2.3";   // Repeated for every item
}
```

### 3.x: Resource Set Once
```csharp
// 3.x: Set once for all telemetry
config.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.ConfigureResource(resource =>
    {
        resource.AddService("MyService", serviceVersion: "1.2.3");
        // Set once, applied to all telemetry automatically
    });
});
```

This is a significant performance improvement.

## Migration Pattern Summary

| 2.x Pattern | 3.x Equivalent |
|-------------|---------------|
| Manual `Track*()` | Automatic instrumentation |
| `ITelemetryInitializer` | `BaseProcessor<Activity>.OnEnd` |
| `ITelemetryProcessor` chain | `BaseProcessor<Activity>.OnEnd` |
| `ITelemetryChannel` | Exporter (automatic) |
| `TelemetryContext` | Resource attributes |
| `TelemetrySink` | Additional Exporter |

## See Also

- [activity-processor.md](activity-processor.md) - Processor details
- [configure-otel-builder.md](configure-otel-builder.md) - Pipeline configuration
- [transformations/ITelemetryInitializer/overview.md](../transformations/ITelemetryInitializer/overview.md) - Initializer migration
- [transformations/ITelemetryProcessor/overview.md](../transformations/ITelemetryProcessor/overview.md) - Processor migration

## References

- **OpenTelemetry .NET SDK**: https://github.com/open-telemetry/opentelemetry-dotnet
- **OpenTelemetry Architecture**: https://opentelemetry.io/docs/specs/otel/overview/
