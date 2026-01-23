# Sending Telemetry to Multiple Destinations (Multi-Sink)

**Category:** Common Scenario  
**Applies to:** Application Insights .NET SDK 3.x  
**Related:** [ConfigureOpenTelemetryBuilder.md](../api-reference/TelemetryConfiguration/ConfigureOpenTelemetryBuilder.md)

## Overview

In 2.x, "Sinks" allowed sending telemetry to multiple Application Insights resources or exporters. In 3.x, this is achieved by **adding multiple exporters** via `ConfigureOpenTelemetryBuilder`.

## Quick Solution

```csharp
services.AddApplicationInsightsTelemetry(options =>
{
    // Primary Application Insights instance
    options.ConnectionString = "InstrumentationKey=key1...";
})
.ConfigureOpenTelemetryBuilder(builder =>
{
    // Secondary Application Insights instance
    builder.AddAzureMonitorTraceExporter(exporterOptions =>
    {
        exporterOptions.ConnectionString = "InstrumentationKey=key2...";
    });
    
    // Additional exporters
    builder.AddConsoleExporter();
    builder.AddOtlpExporter();
});
```

**Result:** Telemetry sent to primary AI, secondary AI, console, and OTLP endpoint.

## Use Cases

1. **Multiple Environments:** Send to development and production workspaces simultaneously
2. **Shared Monitoring:** Send to team workspace + central operations workspace
3. **Migration:** Send to 2.x and 3.x instances during migration period
4. **Multi-Platform:** Send to Application Insights + Prometheus + Jaeger
5. **Local Development:** Send to Application Insights + console for debugging

## Method 1: Multiple Application Insights Instances

```csharp
services.AddApplicationInsightsTelemetry(options =>
{
    // Primary workspace (team)
    options.ConnectionString = "InstrumentationKey=aaaa-1111...";
})
.ConfigureOpenTelemetryBuilder(builder =>
{
    // Secondary workspace (central operations)
    builder.AddAzureMonitorTraceExporter(exporterOptions =>
    {
        exporterOptions.ConnectionString = "InstrumentationKey=bbbb-2222...";
    });
    
    // Tertiary workspace (compliance/audit)
    builder.AddAzureMonitorTraceExporter(exporterOptions =>
    {
        exporterOptions.ConnectionString = "InstrumentationKey=cccc-3333...";
    });
});
```

**Note:** Each exporter sends the full telemetry stream. Use processors to filter if needed.

## Method 2: Application Insights + Console (Development)

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
})
.ConfigureOpenTelemetryBuilder(otelBuilder =>
{
    // In development, also export to console
    if (builder.Environment.IsDevelopment())
    {
        otelBuilder.AddConsoleExporter(options =>
        {
            options.Targets = ConsoleExporterOutputTargets.Console;
        });
    }
});
```

## Method 3: Application Insights + OTLP (Jaeger/Prometheus)

```csharp
services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = "InstrumentationKey=...";
})
.ConfigureOpenTelemetryBuilder(builder =>
{
    // Export to OTLP endpoint (Jaeger, Prometheus, etc.)
    builder.AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("http://localhost:4317");
        options.Protocol = OtlpExportProtocol.Grpc;
    });
});
```

## Method 4: Filtered Multi-Sink (Different Data to Different Destinations)

```csharp
services.AddApplicationInsightsTelemetry(options =>
{
    // Primary workspace gets everything
    options.ConnectionString = "InstrumentationKey=primary...";
})
.ConfigureOpenTelemetryBuilder(builder =>
{
    // Secondary workspace only gets errors
    builder.AddProcessor(new ErrorOnlyProcessor());
    builder.AddAzureMonitorTraceExporter(exporterOptions =>
    {
        exporterOptions.ConnectionString = "InstrumentationKey=errors...";
    });
});

// Processor that marks non-errors for filtering
public class ErrorOnlyProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // For secondary exporter, only send errors
        // Note: This affects ALL exporters - need more sophisticated approach
        
        if (activity.Status != ActivityStatusCode.Error)
        {
            // Can't selectively filter per exporter in standard way
            // Consider using custom exporter or separate ActivitySource
        }
    }
}
```

**Note:** Standard processors affect all exporters. For per-exporter filtering, use custom exporters or separate ActivitySources.

## Method 5: Separate ActivitySources for Different Destinations

```csharp
// Define two ActivitySources
private static readonly ActivitySource PrimarySource = new("MyApp.Primary");
private static readonly ActivitySource SecondarySource = new("MyApp.Secondary");

// Configure exporters
services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = "InstrumentationKey=primary...";
})
.ConfigureOpenTelemetryBuilder(builder =>
{
    // Primary exporter: Subscribe to Primary source
    builder.AddSource("MyApp.Primary");
    
    // Secondary exporter: Subscribe to Secondary source
    builder.AddAzureMonitorTraceExporter(exporterOptions =>
    {
        exporterOptions.ConnectionString = "InstrumentationKey=secondary...";
    });
    builder.AddSource("MyApp.Secondary");
});

// Usage: Create activities from specific sources
using (var activity = PrimarySource.StartActivity("PrimaryOperation"))
{
    // Goes to primary Application Insights
}

using (var activity = SecondarySource.StartActivity("SecondaryOperation"))
{
    // Goes to secondary Application Insights
}
```

## Migration from 2.x Sinks

### 2.x: TelemetrySink

```csharp
services.Configure<TelemetryConfiguration>(config =>
{
    // Create sink for secondary workspace
    var secondaryConfig = new TelemetryConfiguration
    {
        ConnectionString = "InstrumentationKey=secondary...",
        TelemetryProcessors =
        {
            new ErrorsOnlyProcessor()
        }
    };
    
    var sink = new TelemetrySink(secondaryConfig, "SecondarySink");
    
    // Add sink to primary configuration
    config.TelemetryProcessors.Add(sink);
});
```

### 3.x: Multiple Exporters

```csharp
services.AddApplicationInsightsTelemetry(options =>
{
    // Primary workspace
    options.ConnectionString = "InstrumentationKey=primary...";
})
.ConfigureOpenTelemetryBuilder(builder =>
{
    // Secondary workspace
    builder.AddAzureMonitorTraceExporter(exporterOptions =>
    {
        exporterOptions.ConnectionString = "InstrumentationKey=secondary...";
    });
    
    // Processors apply to all exporters
    builder.AddProcessor(new ErrorsOnlyProcessor());
});
```

## Real-World Examples

### Example 1: Team + Central Operations

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddApplicationInsightsTelemetry(options =>
    {
        // Team workspace (detailed telemetry)
        options.ConnectionString = Configuration["ApplicationInsights:TeamConnectionString"];
    })
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        // Central operations workspace (errors only)
        var centralConnectionString = Configuration["ApplicationInsights:CentralConnectionString"];
        if (!string.IsNullOrEmpty(centralConnectionString))
        {
            builder.AddAzureMonitorTraceExporter(exporterOptions =>
            {
                exporterOptions.ConnectionString = centralConnectionString;
            });
        }
    });
}
```

### Example 2: Production + Staging Monitoring

```csharp
services.AddApplicationInsightsTelemetry(options =>
{
    // Production workspace
    options.ConnectionString = Configuration["ApplicationInsights:ProductionConnectionString"];
})
.ConfigureOpenTelemetryBuilder(builder =>
{
    // Staging workspace (for comparison during deployment)
    if (Environment.GetEnvironmentVariable("DEPLOYMENT_STAGE") == "canary")
    {
        builder.AddAzureMonitorTraceExporter(exporterOptions =>
        {
            exporterOptions.ConnectionString = Configuration["ApplicationInsights:StagingConnectionString"];
        });
    }
});
```

### Example 3: Hybrid Monitoring (Application Insights + Prometheus)

```csharp
services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = Configuration["ApplicationInsights:ConnectionString"];
})
.ConfigureOpenTelemetryBuilder(builder =>
{
    // Export metrics to Prometheus
    builder.AddPrometheusExporter(options =>
    {
        options.StartHttpListener = true;
        options.HttpListenerPrefixes = new[] { "http://localhost:9090/" };
    });
    
    // Export traces to Jaeger
    builder.AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri(Configuration["Jaeger:Endpoint"]);
        options.Protocol = OtlpExportProtocol.Grpc;
    });
});
```

## Configuration

### From appsettings.json

```json
{
  "ApplicationInsights": {
    "Primary": {
      "ConnectionString": "InstrumentationKey=primary-key..."
    },
    "Secondary": {
      "ConnectionString": "InstrumentationKey=secondary-key...",
      "Enabled": true
    },
    "Tertiary": {
      "ConnectionString": "InstrumentationKey=tertiary-key...",
      "Enabled": false
    }
  }
}
```

```csharp
services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = Configuration["ApplicationInsights:Primary:ConnectionString"];
})
.ConfigureOpenTelemetryBuilder(builder =>
{
    // Secondary (conditional)
    if (Configuration.GetValue<bool>("ApplicationInsights:Secondary:Enabled"))
    {
        builder.AddAzureMonitorTraceExporter(exporterOptions =>
        {
            exporterOptions.ConnectionString = Configuration["ApplicationInsights:Secondary:ConnectionString"];
        });
    }
    
    // Tertiary (conditional)
    if (Configuration.GetValue<bool>("ApplicationInsights:Tertiary:Enabled"))
    {
        builder.AddAzureMonitorTraceExporter(exporterOptions =>
        {
            exporterOptions.ConnectionString = Configuration["ApplicationInsights:Tertiary:ConnectionString"];
        });
    }
});
```

## Available Exporters

### Azure Monitor Trace Exporter

```csharp
builder.AddAzureMonitorTraceExporter(options =>
{
    options.ConnectionString = "...";
});
```

### Console Exporter

```csharp
builder.AddConsoleExporter(options =>
{
    options.Targets = ConsoleExporterOutputTargets.Console;
});
```

### OTLP Exporter (Jaeger, Prometheus, etc.)

```csharp
builder.AddOtlpExporter(options =>
{
    options.Endpoint = new Uri("http://localhost:4317");
    options.Protocol = OtlpExportProtocol.Grpc;
});
```

### Zipkin Exporter

```csharp
builder.AddZipkinExporter(options =>
{
    options.Endpoint = new Uri("http://localhost:9411/api/v2/spans");
});
```

### Prometheus Exporter (Metrics)

```csharp
builder.AddPrometheusExporter();
```

## Performance Considerations

### Multiple Exporters = Multiple Network Calls

```csharp
// Each exporter sends telemetry independently
// 3 exporters = 3x network traffic
services.AddApplicationInsightsTelemetry(options => { ... })
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.AddAzureMonitorTraceExporter(...); // Network call 1
        builder.AddAzureMonitorTraceExporter(...); // Network call 2
        builder.AddOtlpExporter(...);              // Network call 3
    });
```

**Impact:** Higher CPU, memory, and network usage. Monitor application performance.

### Use Sampling to Reduce Volume

```csharp
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        // Sample 10% for primary
        builder.AddTraceIdRatioBasedSampler(0.1);
        
        builder.AddAzureMonitorTraceExporter(...);
        builder.AddAzureMonitorTraceExporter(...);
    });
```

## Verification

### Check in Azure Monitor

1. Navigate to each Application Insights workspace
2. Verify telemetry appears in each
3. Check "Application Map" shows same service in multiple workspaces

### Check in Code (Debugging)

```csharp
// Enable OpenTelemetry diagnostics
AppContext.SetSwitch("OpenTelemetry.Exporter.Console.UseJson", true);
Environment.SetEnvironmentVariable("OTEL_LOG_LEVEL", "Debug");

// Check console output to see exporter activity
```

## Common Issues

### Issue 1: Telemetry Only in Primary

**Cause:** Secondary exporter not registered correctly.

**Solution:** Verify exporter added via `ConfigureOpenTelemetryBuilder`.

### Issue 2: High Costs (Duplicate Data)

**Cause:** Each exporter sends full telemetry stream.

**Solution:** Use sampling or filters to reduce volume.

### Issue 3: Connection String Not Found

**Cause:** Configuration not loaded correctly.

**Solution:** Verify appsettings.json and configuration binding.

## Best Practices

1. **Use Conditionally:** Enable secondary exporters based on configuration flags
2. **Sample Aggressively:** Reduce costs by sampling for secondary destinations
3. **Monitor Performance:** Watch CPU/memory/network when using multiple exporters
4. **Separate Concerns:** Primary for detailed monitoring, secondary for alerts/overview
5. **Test Thoroughly:** Verify all exporters receiving telemetry

## Complete Example

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = Configuration["ApplicationInsights:Primary"];
    })
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        // Secondary Application Insights (central ops)
        var secondaryConnectionString = Configuration["ApplicationInsights:Secondary"];
        if (!string.IsNullOrEmpty(secondaryConnectionString))
        {
            builder.AddAzureMonitorTraceExporter(exporterOptions =>
            {
                exporterOptions.ConnectionString = secondaryConnectionString;
            });
        }
        
        // Console exporter (development only)
        if (Environment.IsDevelopment())
        {
            builder.AddConsoleExporter();
        }
        
        // OTLP exporter (Jaeger for distributed tracing)
        var jaegerEndpoint = Configuration["Jaeger:Endpoint"];
        if (!string.IsNullOrEmpty(jaegerEndpoint))
        {
            builder.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(jaegerEndpoint);
            });
        }
        
        // Sampling (10% to reduce costs)
        builder.AddTraceIdRatioBasedSampler(0.1);
    });
}
```

## See Also

- [ConfigureOpenTelemetryBuilder.md](../api-reference/TelemetryConfiguration/ConfigureOpenTelemetryBuilder.md) - Configuration API
- [filtering-telemetry.md](./filtering-telemetry.md) - Filtering scenario
- [OpenTelemetry Exporters](https://opentelemetry.io/docs/instrumentation/net/exporters/)
