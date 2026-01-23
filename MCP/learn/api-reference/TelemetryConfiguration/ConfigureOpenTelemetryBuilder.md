# TelemetryConfiguration.ConfigureOpenTelemetryBuilder

**Category:** API Reference  
**Applies to:** Application Insights .NET SDK 3.x  
**Related:** [configure-otel-builder.md](../../concepts/configure-otel-builder.md), [IOpenTelemetryBuilder](../IOpenTelemetryBuilder/README.md)

## Overview

`ConfigureOpenTelemetryBuilder` is the **primary extensibility point** in Application Insights 3.x, replacing multiple 2.x extension mechanisms (ITelemetryInitializer, ITelemetryProcessor, TelemetryModules, Channel, Sinks).

## Signature

```csharp
// From: ApplicationInsights-dotnet/BASE/src/Microsoft.ApplicationInsights/Extensibility/TelemetryConfiguration.cs
public TelemetryConfiguration ConfigureOpenTelemetryBuilder(Action<IOpenTelemetryBuilder> configure)
```

## Purpose

Provides access to the OpenTelemetry `TracerProviderBuilder` through `IOpenTelemetryBuilder` interface, enabling:
- Adding Activity processors (replaces ITelemetryInitializer/ITelemetryProcessor)
- Configuring instrumentation libraries
- Adding resource detectors (replaces TelemetryContext configuration)
- Configuring additional exporters (replaces Sinks)
- Setting samplers (replaces sampling configuration)

## Usage Pattern

```csharp
services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = "InstrumentationKey=...";
})
.ConfigureOpenTelemetryBuilder(builder =>
{
    // Add custom processor
    builder.AddProcessor(new MyCustomActivityProcessor());
    
    // Add resource detector
    builder.ConfigureResource(resource => 
        resource.AddDetector(new MyResourceDetector()));
    
    // Add another exporter (multi-sink scenario)
    builder.AddOtlpExporter();
});
```

## Real-World Examples

### Example 1: Adding Custom Processor (from WebTestActivityProcessor)

```csharp
// From: ApplicationInsights-dotnet/WEB/Src/Web/ApplicationInsightsExtensions.cs
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.AddProcessor(new WebTestActivityProcessor());
        builder.AddProcessor(new SyntheticUserAgentActivityProcessor());
    });
```

### Example 2: Configuring Cloud Role Name via Resource

```csharp
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.ConfigureResource(resource =>
            resource.AddService(serviceName: "MyServiceName", 
                               serviceVersion: "1.0.0"));
    });
// Result: Cloud.RoleName = "MyServiceName", Cloud.RoleInstance = hostname
```

### Example 3: Multi-Sink Scenario (2.x Sinks replacement)

```csharp
services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = "InstrumentationKey=key1...";
})
.ConfigureOpenTelemetryBuilder(builder =>
{
    // Second Application Insights instance
    builder.AddAzureMonitorTraceExporter(exporterOptions =>
    {
        exporterOptions.ConnectionString = "InstrumentationKey=key2...";
    });
    
    // Additional exporters
    builder.AddConsoleExporter();
    builder.AddOtlpExporter();
});
```

## Migration from 2.x

### Before (2.x): Multiple Extension Points

```csharp
// Initializer
services.AddSingleton<ITelemetryInitializer, MyInitializer>();

// Processor
services.Configure<TelemetryConfiguration>(config =>
{
    config.TelemetryProcessorChainBuilder
        .Use(next => new MyProcessor(next))
        .Build();
});

// Module
services.AddSingleton<ITelemetryModule, MyModule>();

// Sink
services.Configure<TelemetryConfiguration>(config =>
{
    var sink = new TelemetryConfiguration
    {
        ConnectionString = "InstrumentationKey=key2..."
    };
    config.TelemetryProcessors.Add(new TelemetrySink(sink));
});
```

### After (3.x): Single ConfigureOpenTelemetryBuilder

```csharp
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        // Replaces ITelemetryInitializer
        builder.AddProcessor(new MyEnrichmentProcessor());
        
        // Replaces ITelemetryProcessor
        builder.AddProcessor(new MyFilterProcessor());
        
        // Replaces ITelemetryModule (use instrumentation instead)
        builder.AddHttpClientInstrumentation();
        
        // Replaces Sink
        builder.AddAzureMonitorTraceExporter(options =>
        {
            options.ConnectionString = "InstrumentationKey=key2...";
        });
    });
```

## Timing Considerations

**CRITICAL:** `ConfigureOpenTelemetryBuilder` must be called **after** `AddApplicationInsightsTelemetry()` in the service registration chain:

```csharp
// ✅ CORRECT - Chained call
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder => { ... });

// ❌ WRONG - Separate statement may not work
services.AddApplicationInsightsTelemetry();
services.ConfigureOpenTelemetryBuilder(builder => { ... }); // Method doesn't exist on IServiceCollection
```

## Return Value

Returns the same `IServiceCollection` for further chaining.

## Common Patterns

### Pattern 1: Enrichment (Replaces ITelemetryInitializer)
```csharp
builder.AddProcessor(new MyEnrichmentProcessor()); // OnStart for enrichment
```

### Pattern 2: Filtering (Replaces ITelemetryProcessor)
```csharp
builder.AddProcessor(new MyFilterProcessor()); // OnEnd with Activity.IsAllDataRequested
```

### Pattern 3: Cloud Role Configuration
```csharp
builder.ConfigureResource(r => r.AddService("MyService"));
```

### Pattern 4: Additional Exporters
```csharp
builder.AddConsoleExporter();
builder.AddOtlpExporter();
```

## Related APIs

- [IOpenTelemetryBuilder](../IOpenTelemetryBuilder/README.md) - Interface exposed to configuration action
- [BaseProcessor\<Activity\>](../BaseProcessor/README.md) - Processor base class
- [IResourceDetector](../IResourceDetector/README.md) - Resource detection interface

## See Also

- [configure-otel-builder.md](../../concepts/configure-otel-builder.md) - Detailed concept guide
- [ITelemetryInitializer migration](../../transformations/ITelemetryInitializer/enrichment-with-onstart.md)
- [ITelemetryProcessor migration](../../transformations/ITelemetryProcessor/filtering-with-onend.md)
- [Multi-sink scenarios](../../common-scenarios/sending-to-multiple-destinations.md)
