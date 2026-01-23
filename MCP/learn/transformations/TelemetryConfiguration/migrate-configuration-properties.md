# Migrating TelemetryConfiguration Properties

**Category:** Transformation Pattern  
**Applies to:** Migration from Application Insights 2.x to 3.x  
**Related:** [ConfigureOpenTelemetryBuilder.md](../../api-reference/TelemetryConfiguration/ConfigureOpenTelemetryBuilder.md)

## Overview

Many `TelemetryConfiguration` properties from 2.x are either deprecated, moved, or replaced with OpenTelemetry equivalents in 3.x.

## Property Migration Table

| 2.x Property | 3.x Equivalent | Migration Path |
|--------------|----------------|----------------|
| `ConnectionString` | `ApplicationInsightsServiceOptions.ConnectionString` | Set in AddApplicationInsightsTelemetry |
| `InstrumentationKey` | `ConnectionString` | Use ConnectionString format |
| `TelemetryInitializers` | `AddProcessor()` (OnStart) | Use BaseProcessor\<Activity\> |
| `TelemetryProcessors` | `AddProcessor()` (OnEnd) | Use BaseProcessor\<Activity\> |
| `TelemetryChannel` | `AddAzureMonitorTraceExporter` | Configure via exporter options |
| `DisableTelemetry` | Not available | Remove from config |
| `TelemetryProcessorChainBuilder` | `ConfigureOpenTelemetryBuilder` | Use fluent API |

## ConnectionString

### 2.x

```csharp
services.Configure<TelemetryConfiguration>(config =>
{
    config.ConnectionString = "InstrumentationKey=...";
    // Or
    config.InstrumentationKey = "...";
});
```

### 3.x

```csharp
services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = "InstrumentationKey=...";
});

// Or from configuration
services.AddApplicationInsightsTelemetry();
// Uses: Configuration["ApplicationInsights:ConnectionString"]
```

**appsettings.json:**
```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=...;IngestionEndpoint=https://..."
  }
}
```

## TelemetryInitializers

### 2.x

```csharp
services.AddSingleton<ITelemetryInitializer, MyInitializer>();

// Or directly on configuration
services.Configure<TelemetryConfiguration>(config =>
{
    config.TelemetryInitializers.Add(new MyInitializer());
});
```

### 3.x

```csharp
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.AddProcessor(new MyProcessor()); // OnStart for initialization
    });

public class MyProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        // Enrichment logic here
        activity.SetTag("custom.property", "value");
    }
}
```

**See:** [enrichment-with-onstart.md](../ITelemetryInitializer/enrichment-with-onstart.md)

## TelemetryProcessors

### 2.x

```csharp
services.Configure<TelemetryConfiguration>(config =>
{
    config.TelemetryProcessorChainBuilder
        .Use(next => new MyProcessor(next))
        .Use(next => new AnotherProcessor(next))
        .Build();
});
```

### 3.x

```csharp
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.AddProcessor(new MyProcessor());
        builder.AddProcessor(new AnotherProcessor());
    });

public class MyProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Processing/filtering logic here
        if (ShouldFilter(activity))
        {
            activity.IsAllDataRequested = false;
        }
    }
}
```

**See:** [filtering-with-onend.md](../ITelemetryProcessor/filtering-with-onend.md)

## TelemetryChannel

### 2.x

```csharp
services.Configure<TelemetryConfiguration>(config =>
{
    var channel = new ServerTelemetryChannel
    {
        EndpointAddress = "https://custom-endpoint.com",
        MaxTelemetryBufferCapacity = 1000,
        DeveloperMode = true
    };
    config.TelemetryChannel = channel;
});
```

### 3.x

```csharp
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        // Channel configuration via exporter options
        builder.AddAzureMonitorTraceExporter(exporterOptions =>
        {
            // Endpoint configured via ConnectionString
            exporterOptions.ConnectionString = "InstrumentationKey=...;IngestionEndpoint=https://custom-endpoint.com";
            
            // Buffer configuration (if available in exporter)
            // Note: Some 2.x channel options may not have direct equivalents
        });
    });
```

**Note:** Many channel-specific configurations don't have direct equivalents in 3.x OpenTelemetry exporters.

## DisableTelemetry

### 2.x

```csharp
services.Configure<TelemetryConfiguration>(config =>
{
    config.DisableTelemetry = true;
});
```

### 3.x

```csharp
// Option 1: Don't register Application Insights
// services.AddApplicationInsightsTelemetry(); // Comment out

// Option 2: Conditional registration
if (!Configuration.GetValue<bool>("DisableTelemetry"))
{
    services.AddApplicationInsightsTelemetry();
}

// Option 3: Filter all telemetry
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        if (Configuration.GetValue<bool>("DisableTelemetry"))
        {
            builder.AddProcessor(new DisableAllProcessor());
        }
    });

public class DisableAllProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        activity.IsAllDataRequested = false; // Drop all
    }
}
```

## TelemetryProcessorChainBuilder

### 2.x

```csharp
services.Configure<TelemetryConfiguration>(config =>
{
    config.TelemetryProcessorChainBuilder
        .Use(next => new FilterProcessor(next))
        .Use(next => new EnrichmentProcessor(next))
        .Use(next => new SamplingProcessor(next))
        .Build();
});
```

### 3.x

```csharp
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        // Order matters - processors execute in registration order
        builder.AddProcessor(new FilterProcessor());
        builder.AddProcessor(new EnrichmentProcessor());
        
        // Sampling via built-in sampler
        builder.AddTraceIdRatioBasedSampler(0.1); // 10% sampling
    });
```

## Sampling Configuration

### 2.x: Adaptive Sampling

```csharp
services.Configure<TelemetryConfiguration>(config =>
{
    var samplingSettings = new SamplingPercentageEstimatorSettings
    {
        MaxTelemetryItemsPerSecond = 5
    };
    
    config.TelemetryProcessorChainBuilder
        .UseAdaptiveSampling(samplingSettings)
        .Build();
});
```

### 3.x: Fixed Ratio Sampling

```csharp
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        // Fixed ratio sampling (10%)
        builder.AddTraceIdRatioBasedSampler(0.1);
        
        // Or parent-based sampling (respect parent decision)
        builder.AddParentBasedSampler(new TraceIdRatioBasedSampler(0.1));
    });
```

**Note:** Adaptive sampling not available in 3.x OpenTelemetry SDK. Use fixed ratio sampling.

## DeveloperMode

### 2.x

```csharp
services.Configure<TelemetryConfiguration>(config =>
{
    config.TelemetryChannel.DeveloperMode = true; // Flush immediately
});
```

### 3.x

```csharp
// No direct equivalent
// Use console exporter for local development
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        if (Environment.IsDevelopment())
        {
            builder.AddConsoleExporter();
        }
    });
```

## MaxTelemetryItemsPerSecond

### 2.x

```csharp
services.Configure<TelemetryConfiguration>(config =>
{
    var channel = new ServerTelemetryChannel
    {
        MaxTelemetryItemsPerSecond = 100
    };
    config.TelemetryChannel = channel;
});
```

### 3.x

```csharp
// No direct equivalent
// Use sampling to control volume
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        // Reduce volume via sampling
        builder.AddTraceIdRatioBasedSampler(0.5); // 50%
    });
```

## Complete Migration Example

### 2.x Configuration

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddApplicationInsightsTelemetry();
    
    services.Configure<TelemetryConfiguration>(config =>
    {
        // Connection
        config.ConnectionString = Configuration["ApplicationInsights:ConnectionString"];
        
        // Initializers
        config.TelemetryInitializers.Add(new CloudRoleInitializer());
        config.TelemetryInitializers.Add(new UserContextInitializer());
        
        // Processors
        config.TelemetryProcessorChainBuilder
            .Use(next => new HealthCheckFilter(next))
            .Use(next => new ErrorsOnlyProcessor(next))
            .UseAdaptiveSampling(maxTelemetryItemsPerSecond: 5)
            .Build();
        
        // Channel
        var channel = new ServerTelemetryChannel
        {
            DeveloperMode = Environment.IsDevelopment()
        };
        config.TelemetryChannel = channel;
    });
}
```

### 3.x Configuration

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddApplicationInsightsTelemetry(options =>
    {
        // Connection
        options.ConnectionString = Configuration["ApplicationInsights:ConnectionString"];
    })
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        // Resource (replaces CloudRoleInitializer)
        builder.ConfigureResource(resource =>
            resource.AddService(Configuration["ApplicationInsights:CloudRoleName"]));
        
        // Processors
        var httpContextAccessor = builder.Services.BuildServiceProvider()
            .GetRequiredService<IHttpContextAccessor>();
        
        builder.AddProcessor(new UserContextProcessor(httpContextAccessor));
        builder.AddProcessor(new HealthCheckFilter());
        builder.AddProcessor(new ErrorsOnlyProcessor());
        
        // Sampling (fixed ratio, not adaptive)
        builder.AddTraceIdRatioBasedSampler(0.1); // 10%
        
        // Console exporter for development (replaces DeveloperMode)
        if (Environment.IsDevelopment())
        {
            builder.AddConsoleExporter();
        }
    });
}
```

## Deprecated Properties

These 2.x properties have no equivalent in 3.x:

| 2.x Property | Reason |
|--------------|--------|
| `ApplicationVersion` | Use Resource `service.version` |
| `DefaultTelemetrySink` | Use multiple exporters |
| `TelemetryProcessors.Count` | Not applicable |
| `TelemetryChannel.EndpointAddress` | Use ConnectionString |

## Configuration File Migration

### 2.x: ApplicationInsights.config

```xml
<?xml version="1.0" encoding="utf-8"?>
<ApplicationInsights xmlns="http://schemas.microsoft.com/ApplicationInsights/2013/Settings">
  <ConnectionString>InstrumentationKey=...</ConnectionString>
  <TelemetryInitializers>
    <Add Type="MyApp.CloudRoleInitializer, MyApp"/>
  </TelemetryInitializers>
  <TelemetryProcessors>
    <Add Type="MyApp.HealthCheckFilter, MyApp"/>
  </TelemetryProcessors>
  <TelemetryChannel Type="Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.ServerTelemetryChannel, Microsoft.AI.ServerTelemetryChannel">
    <DeveloperMode>true</DeveloperMode>
  </TelemetryChannel>
</ApplicationInsights>
```

### 3.x: appsettings.json + Code

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=...",
    "CloudRoleName": "MyService"
  }
}
```

```csharp
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.ConfigureResource(resource =>
            resource.AddService(Configuration["ApplicationInsights:CloudRoleName"]));
        
        builder.AddProcessor(new HealthCheckFilter());
        
        if (Environment.IsDevelopment())
        {
            builder.AddConsoleExporter();
        }
    });
```

**Note:** 3.x does not use ApplicationInsights.config file.

## See Also

- [ConfigureOpenTelemetryBuilder.md](../../api-reference/TelemetryConfiguration/ConfigureOpenTelemetryBuilder.md) - API reference
- [enrichment-with-onstart.md](../ITelemetryInitializer/enrichment-with-onstart.md) - Initializer migration
- [filtering-with-onend.md](../ITelemetryProcessor/filtering-with-onend.md) - Processor migration
- [context-to-resource.md](../../mappings/context-to-resource.md) - Context migration
