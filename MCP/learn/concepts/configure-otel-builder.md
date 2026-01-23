---
title: ConfigureOpenTelemetryBuilder - The Extensibility Point
category: concept
applies-to: 3.x
related:
  - concepts/activity-processor.md
  - concepts/resource-detector.md
  - api-reference/TelemetryConfiguration/ConfigureOpenTelemetryBuilder.md
  - common-scenarios/multi-exporter-setup.md
source: ApplicationInsights-dotnet/BASE/src/Microsoft.ApplicationInsights/Extensibility/TelemetryConfiguration.cs
---

# ConfigureOpenTelemetryBuilder - The Extensibility Point

## Overview

`ConfigureOpenTelemetryBuilder` is the **primary extensibility point** in Application Insights 3.x. It replaces multiple 2.x extensibility mechanisms (TelemetryInitializers, TelemetryProcessors, TelemetryModules, TelemetrySinks) with a single, powerful API.

## In 2.x: Multiple Extensibility Points

Application Insights 2.x had several ways to extend functionality:

```csharp
// 2.x: Multiple extension mechanisms

// 1. Telemetry Initializers
config.TelemetryInitializers.Add(new MyInitializer());

// 2. Telemetry Processors
config.TelemetryProcessorChainBuilder
    .Use((next) => new MyProcessor(next))
    .Build();

// 3. Telemetry Modules
config.TelemetryModules.Add(new DependencyTrackingTelemetryModule());

// 4. Multiple Sinks
config.TelemetrySinks.Add(new TelemetrySink { ... });

// 5. Custom Channel
config.TelemetryChannel = new MyCustomChannel();
```

## In 3.x: Single ConfigureOpenTelemetryBuilder API

```csharp
// 3.x: Single unified API
var config = new TelemetryConfiguration();
config.ConnectionString = "InstrumentationKey=...";

config.ConfigureOpenTelemetryBuilder(builder =>
{
    // Everything goes through this builder
    
    // Add custom processors (replaces Initializers/Processors)
    builder.AddProcessor(new MyActivityProcessor());
    
    // Add instrumentation (replaces TelemetryModules)
    builder.AddHttpClientInstrumentation();
    builder.AddSqlClientInstrumentation();
    
    // Add custom ActivitySource (custom telemetry)
    builder.AddSource("MyApp.*");
    
    // Configure resource (replaces Context.Cloud.RoleName, etc.)
    builder.ConfigureResource(resource =>
    {
        resource.AddService("MyServiceName", serviceInstanceId: "instance-1");
    });
    
    // Add additional exporters (replaces TelemetrySinks)
    builder.AddConsoleExporter();
    builder.AddOtlpExporter();
});
```

## API Signature

From `TelemetryConfiguration.cs`:

```csharp
/// <summary>
/// Allows extending the OpenTelemetry builder configuration.
/// </summary>
/// <remarks>
/// Use this to extend the telemetry pipeline with custom sources, processors, or exporters.
/// This can only be called before the configuration is built.
/// </remarks>
/// <param name="configure">Action to configure the OpenTelemetry builder.</param>
/// <exception cref="InvalidOperationException">
/// Thrown if the configuration has already been built.
/// </exception>
public void ConfigureOpenTelemetryBuilder(Action<IOpenTelemetryBuilder> configure)
{
    this.ThrowIfBuilt();
    
    if (configure == null)
    {
        throw new ArgumentNullException(nameof(configure));
    }
    
    // Chain the configurations
    var previousConfiguration = this.builderConfiguration;
    this.builderConfiguration = builder =>
    {
        previousConfiguration(builder);
        configure(builder);
    };
}
```

Key characteristics:
- Can be called **multiple times** - configurations are chained
- Must be called **before TelemetryClient is created** (before configuration is "built")
- Takes an `Action<IOpenTelemetryBuilder>` delegate

## IOpenTelemetryBuilder Extensions

The `IOpenTelemetryBuilder` interface provides many extension methods:

### 1. Tracing Extensions (Activities)

```csharp
config.ConfigureOpenTelemetryBuilder(builder =>
{
    // Add custom Activity Processors
    builder.AddProcessor(new MyActivityProcessor());
    builder.AddProcessor(new FilteringProcessor());
    
    // Register ActivitySources to listen to
    builder.AddSource("MyCompany.*");
    builder.AddSource("ThirdParty.Library");
    
    // Add instrumentation libraries
    builder.AddHttpClientInstrumentation(options =>
    {
        options.FilterHttpRequestMessage = (req) => 
            !req.RequestUri.AbsolutePath.Contains("/health");
    });
    
    builder.AddSqlClientInstrumentation(options =>
    {
        options.SetDbStatementForText = true;
    });
    
    builder.AddAspNetCoreInstrumentation();
});
```

### 2. Resource Configuration

```csharp
config.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.ConfigureResource(resource =>
    {
        // Set service name (maps to Cloud.RoleName)
        resource.AddService(
            serviceName: "MyService",
            serviceNamespace: "MyCompany.Production",
            serviceVersion: "1.2.3",
            serviceInstanceId: Environment.MachineName);
        
        // Add custom resource attributes
        resource.AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = "production",
            ["team.name"] = "platform"
        });
        
        // Add resource detector
        resource.AddDetector(new MyCustomResourceDetector());
    });
});
```

### 3. Metrics Configuration

```csharp
config.ConfigureOpenTelemetryBuilder(builder =>
{
    // Add custom Meter
    builder.AddMeter("MyApp.Metrics");
    
    // Add metric processors
    builder.AddMetricReader(new MyMetricReader());
});
```

### 4. Logging Configuration

```csharp
config.ConfigureOpenTelemetryBuilder(builder =>
{
    // Add log processors
    builder.AddProcessor<MyLogProcessor>();
    
    // Configure ILogger integration
    builder.AddInstrumentation<ILogger>((logger, options) =>
    {
        // Custom logger configuration
    });
});
```

### 5. Multiple Exporters (Replaces TelemetrySinks)

```csharp
config.ConfigureOpenTelemetryBuilder(builder =>
{
    // Export to multiple destinations
    
    // Console (for debugging)
    builder.AddConsoleExporter();
    
    // Another Azure Monitor instance
    builder.AddAzureMonitorExporter(options =>
    {
        options.ConnectionString = "Secondary-AI-Resource";
    });
    
    // OTLP endpoint (Jaeger, Prometheus, etc.)
    builder.AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("http://localhost:4317");
    });
    
    // Zipkin
    builder.AddZipkinExporter();
});
```

## Common Patterns

### Pattern 1: Add Custom Activity Processor

```csharp
var config = new TelemetryConfiguration();
config.ConnectionString = "...";

config.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.AddProcessor(new MyActivityProcessor());
});
```

### Pattern 2: Filter Out Specific HTTP Requests

```csharp
config.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.AddHttpClientInstrumentation(options =>
    {
        options.FilterHttpRequestMessage = (httpRequestMessage) =>
        {
            // Don't track requests to /health or /metrics
            var path = httpRequestMessage.RequestUri?.AbsolutePath;
            return !path?.Contains("/health") == true &&
                   !path?.Contains("/metrics") == true;
        };
    });
});
```

### Pattern 3: Set Cloud Role Name (Service Name)

```csharp
config.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.ConfigureResource(resource =>
    {
        resource.AddService(
            serviceName: "MyService",  // → Cloud.RoleName
            serviceInstanceId: Environment.MachineName);  // → Cloud.RoleInstance
    });
});
```

### Pattern 4: Multiple Telemetry Destinations

```csharp
config.ConfigureOpenTelemetryBuilder(builder =>
{
    // Primary: Azure Monitor (automatically added by AI SDK)
    
    // Secondary: Another Azure Monitor resource
    builder.AddAzureMonitorExporter(options =>
    {
        options.ConnectionString = "InstrumentationKey=secondary-key;...";
    });
    
    // Tertiary: Local debugging
    builder.AddConsoleExporter();
});
```

### Pattern 5: Custom Instrumentation Source

```csharp
// Define your ActivitySource
public static class Instrumentation
{
    public static readonly ActivitySource ActivitySource = 
        new ActivitySource("MyCompany.MyApp", "1.0.0");
}

// Register it
config.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.AddSource("MyCompany.MyApp");
});

// Use it
using (var activity = Instrumentation.ActivitySource.StartActivity("ProcessOrder"))
{
    activity?.SetTag("order.id", orderId);
    // ... process order
}
```

## Chaining Multiple Calls

You can call `ConfigureOpenTelemetryBuilder` multiple times - they're chained:

```csharp
// First call
config.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.AddProcessor(new Processor1());
});

// Second call - adds to the first, doesn't replace
config.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.AddProcessor(new Processor2());
});

// Both processors will be registered
```

This is useful for modular configuration:

```csharp
// In Startup.cs
services.AddApplicationInsightsTelemetry();

// In a separate module/extension
public static void ConfigureMyModuleTelemetry(TelemetryConfiguration config)
{
    config.ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.AddProcessor(new MyModuleProcessor());
    });
}
```

## ASP.NET Core Integration

In ASP.NET Core, you can also configure via DI:

```csharp
// appsettings.json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=..."
  }
}

// Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddApplicationInsightsTelemetry();
    
    // Get TelemetryConfiguration from DI and configure it
    services.Configure<TelemetryConfiguration>(config =>
    {
        config.ConfigureOpenTelemetryBuilder(builder =>
        {
            builder.AddProcessor(new MyProcessor());
        });
    });
    
    // Or configure OpenTelemetry directly
    services.ConfigureOpenTelemetryTracerProvider((sp, builder) =>
    {
        builder.AddProcessor(sp.GetRequiredService<MyProcessor>());
    });
}
```

## Timing Considerations

`ConfigureOpenTelemetryBuilder` **must be called before the first TelemetryClient is created**:

```csharp
var config = new TelemetryConfiguration();
config.ConnectionString = "...";

// ✅ Good: Configure before creating TelemetryClient
config.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.AddProcessor(new MyProcessor());
});

var client = new TelemetryClient(config);  // Configuration is "built" here

// ❌ Bad: Too late - throws InvalidOperationException
config.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.AddProcessor(new AnotherProcessor());  // THROWS!
});
```

## Migration from 2.x

| 2.x API | 3.x Equivalent |
|---------|---------------|
| `config.TelemetryInitializers.Add(...)` | `builder.AddProcessor(new ActivityProcessor())` |
| `config.TelemetryProcessorChainBuilder.Use(...)` | `builder.AddProcessor(new ActivityProcessor())` |
| `config.TelemetryModules.Add(...)` | `builder.AddHttpClientInstrumentation()`, etc. |
| `config.TelemetrySinks.Add(...)` | `builder.AddAzureMonitorExporter()`, etc. |
| `config.TelemetryChannel = ...` | Built-in via Azure Monitor Exporter |
| `telemetry.Context.Cloud.RoleName` | `builder.ConfigureResource(r => r.AddService("name"))` |

## See Also

- [activity-processor.md](activity-processor.md) - Understanding Activity Processors
- [resource-detector.md](resource-detector.md) - Resource configuration
- [common-scenarios/multi-exporter-setup.md](../common-scenarios/multi-exporter-setup.md) - Multiple exporters
- [transformations/TelemetryConfiguration/](../transformations/TelemetryConfiguration/) - Migration guides
- [api-reference/TelemetryConfiguration/ConfigureOpenTelemetryBuilder.md](../api-reference/TelemetryConfiguration/ConfigureOpenTelemetryBuilder.md) - Detailed API docs

## References

- **Source Code**: `ApplicationInsights-dotnet/BASE/src/Microsoft.ApplicationInsights/Extensibility/TelemetryConfiguration.cs`
- **IOpenTelemetryBuilder**: OpenTelemetry .NET SDK
- **OpenTelemetry Configuration**: https://opentelemetry.io/docs/languages/net/configuration/
