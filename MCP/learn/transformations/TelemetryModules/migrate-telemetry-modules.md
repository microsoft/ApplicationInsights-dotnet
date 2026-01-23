# Migrate TelemetryModules to Instrumentation

**Category:** Transformation Guide  
**Applies to:** ITelemetryModule implementations  
**Related:** [telemetry-modules-removed.md](../../breaking-changes/Web/telemetry-modules-removed.md)

## Overview

TelemetryModules in 2.x provided automatic instrumentation for various frameworks (HTTP, dependencies, performance counters). In 3.x, OpenTelemetry provides equivalent instrumentation through built-in instrumentations.

## What Were TelemetryModules?

In 2.x, TelemetryModules were components that automatically collected telemetry:
- `RequestTrackingTelemetryModule` - HTTP requests
- `DependencyTrackingTelemetryModule` - Dependencies (HTTP, SQL, etc.)
- `PerformanceCollectorModule` - Performance counters
- `QuickPulseTelemetryModule` - Live Metrics
- `AppServicesHeartbeatTelemetryModule` - Heartbeat data

## In 2.x

### applicationinsights.config

```xml
<ApplicationInsights>
  <TelemetryModules>
    <Add Type="Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse.QuickPulseTelemetryModule"/>
    
    <Add Type="Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.PerformanceCollectorModule">
      <Counters>
        <Add PerformanceCounter="\Process(??APP_WIN32_PROC??)\% Processor Time"/>
        <Add PerformanceCounter="\Memory\Available Bytes"/>
      </Counters>
    </Add>
    
    <Add Type="Microsoft.ApplicationInsights.DependencyCollector.DependencyTrackingTelemetryModule">
      <ExcludeComponentCorrelationHttpHeadersOnDomains>
        <Add>localhost</Add>
      </ExcludeComponentCorrelationHttpHeadersOnDomains>
    </Add>
    
    <Add Type="Microsoft.ApplicationInsights.WindowsServer.AppServicesHeartbeatTelemetryModule"/>
  </TelemetryModules>
</ApplicationInsights>
```

### Code-Based Configuration

```csharp
services.Configure<TelemetryConfiguration>(config =>
{
    // Modules were configured through TelemetryConfiguration
    var perfModule = new PerformanceCollectorModule();
    perfModule.Counters.Add(new PerformanceCounterCollectionRequest(
        @"\Process(??APP_WIN32_PROC??)\% Processor Time", "ProcessorTime"));
    perfModule.Initialize(config);
    
    var dependencyModule = new DependencyTrackingTelemetryModule();
    dependencyModule.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("localhost");
    dependencyModule.Initialize(config);
});
```

## In 3.x

Modules are replaced by OpenTelemetry Instrumentations, automatically included with `AddApplicationInsightsTelemetry()`.

### Built-in Instrumentations

```csharp
// Most instrumentations are automatic!
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
});

// The above automatically includes:
// - ASP.NET Core instrumentation (HTTP requests)
// - HttpClient instrumentation (outgoing HTTP dependencies)
// - SQL Client instrumentation (SQL dependencies)
```

### Additional Instrumentations

```csharp
builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        // Add additional instrumentations if needed
        otel.AddAspNetCoreInstrumentation(options =>
        {
            options.Filter = (httpContext) =>
            {
                // Filter requests
                return !httpContext.Request.Path.StartsWithSegments("/health");
            };
        });
        
        otel.AddHttpClientInstrumentation(options =>
        {
            options.FilterHttpRequestMessage = (request) =>
            {
                // Filter outgoing requests
                return !request.RequestUri?.Host.Contains("localhost") ?? true;
            };
        });
        
        otel.AddSqlClientInstrumentation(options =>
        {
            options.SetDbStatementForText = true;
            options.SetDbStatementForStoredProcedure = true;
            options.RecordException = true;
        });
    });
```

## Module Migration Table

| 2.x Module | 3.x Equivalent | Auto-Included? |
|------------|----------------|----------------|
| `RequestTrackingTelemetryModule` | `AddAspNetCoreInstrumentation()` | ✅ Yes |
| `DependencyTrackingTelemetryModule` | `AddHttpClientInstrumentation()`, `AddSqlClientInstrumentation()` | ✅ Yes |
| `PerformanceCollectorModule` | N/A (Consider custom metrics with Meter) | ❌ No |
| `QuickPulseTelemetryModule` | Live Metrics (automatic) | ✅ Yes |
| `AppServicesHeartbeatTelemetryModule` | Automatic resource detection | ✅ Yes |
| `EventCounterCollectionModule` | `AddEventCountersInstrumentation()` | ❌ No |
| `AzureInstanceMetadataTelemetryModule` | `AddAzureInstanceMetadata()` | ❌ No |

## Specific Migrations

### RequestTrackingTelemetryModule

**2.x:**
```xml
<Add Type="Microsoft.ApplicationInsights.Web.RequestTrackingTelemetryModule">
  <Handlers>
    <Add>System.Web.Handlers.TransferRequestHandler</Add>
  </Handlers>
</Add>
```

**3.x:**
```csharp
// Automatic in ASP.NET Core
builder.Services.AddApplicationInsightsTelemetry();

// Configure if needed
builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        otel.AddAspNetCoreInstrumentation(options =>
        {
            options.Filter = (httpContext) => 
                httpContext.Request.Path != "/health";
            
            options.EnrichWithHttpRequest = (activity, request) =>
            {
                activity.SetTag("custom.header", 
                    request.Headers["X-Custom-Header"].ToString());
            };
        });
    });
```

### DependencyTrackingTelemetryModule

**2.x:**
```csharp
var module = new DependencyTrackingTelemetryModule();
module.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("localhost");
module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.EventHubs");
```

**3.x:**
```csharp
builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        // HTTP dependencies
        otel.AddHttpClientInstrumentation(options =>
        {
            options.FilterHttpRequestMessage = (request) =>
            {
                return !request.RequestUri?.Host.Contains("localhost") ?? true;
            };
            
            options.EnrichWithHttpRequestMessage = (activity, request) =>
            {
                activity.SetTag("http.custom", "value");
            };
        });
        
        // SQL dependencies
        otel.AddSqlClientInstrumentation(options =>
        {
            options.SetDbStatementForText = true;
            options.RecordException = true;
        });
        
        // Additional ActivitySource
        otel.AddSource("Microsoft.Azure.EventHubs");
    });
```

### PerformanceCollectorModule

**2.x:**
```csharp
var module = new PerformanceCollectorModule();
module.Counters.Add(new PerformanceCounterCollectionRequest(
    @"\Process(??APP_WIN32_PROC??)\% Processor Time", "ProcessorTime"));
module.Counters.Add(new PerformanceCounterCollectionRequest(
    @"\Memory\Available Bytes", "AvailableMemory"));
```

**3.x:** Use Meter API for custom metrics
```csharp
using System.Diagnostics.Metrics;

public class PerformanceMetrics
{
    private static readonly Meter Meter = new("MyApp.Performance");
    private readonly PerformanceCounter _cpuCounter;
    private readonly PerformanceCounter _memoryCounter;
    
    public PerformanceMetrics()
    {
        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
        
        // Observable gauges
        Meter.CreateObservableGauge("process.cpu.utilization", 
            () => _cpuCounter.NextValue(),
            unit: "%");
        
        Meter.CreateObservableGauge("system.memory.available", 
            () => _memoryCounter.NextValue(),
            unit: "MB");
    }
}

// Register in Program.cs
builder.Services.AddSingleton<PerformanceMetrics>();
builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        otel.AddMeter("MyApp.Performance");
    });
```

### QuickPulseTelemetryModule (Live Metrics)

**2.x:**
```xml
<Add Type="Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse.QuickPulseTelemetryModule"/>
```

**3.x:** Automatically enabled
```csharp
// Live Metrics automatically works with Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// No additional configuration needed!
```

### Custom TelemetryModule

**2.x:**
```csharp
public class CustomTelemetryModule : ITelemetryModule
{
    private TelemetryClient _telemetryClient;
    private Timer _timer;
    
    public void Initialize(TelemetryConfiguration configuration)
    {
        _telemetryClient = new TelemetryClient(configuration);
        _timer = new Timer(CollectMetrics, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }
    
    private void CollectMetrics(object state)
    {
        _telemetryClient.TrackMetric("CustomMetric", GetMetricValue());
    }
    
    public void Dispose()
    {
        _timer?.Dispose();
    }
}
```

**3.x:** Use BackgroundService or Hosted Service
```csharp
public class CustomMetricsService : BackgroundService
{
    private static readonly Meter Meter = new("MyApp.Custom");
    private static readonly Histogram<double> CustomMetric = 
        Meter.CreateHistogram<double>("custom.metric");
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            CustomMetric.Record(GetMetricValue());
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
    
    private double GetMetricValue() => Random.Shared.NextDouble() * 100;
}

// Register
builder.Services.AddHostedService<CustomMetricsService>();
builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        otel.AddMeter("MyApp.Custom");
    });
```

## Available OpenTelemetry Instrumentations

```csharp
builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        // ASP.NET Core (HTTP requests)
        otel.AddAspNetCoreInstrumentation();
        
        // HttpClient (outgoing HTTP)
        otel.AddHttpClientInstrumentation();
        
        // SQL Client
        otel.AddSqlClientInstrumentation();
        
        // gRPC
        otel.AddGrpcClientInstrumentation();
        
        // Azure SDK
        otel.AddAzureClientInstrumentation();
        
        // Entity Framework Core
        otel.AddEntityFrameworkCoreInstrumentation();
        
        // Event Counters
        otel.AddEventCountersInstrumentation();
        
        // Process instrumentation
        otel.AddProcessInstrumentation();
        
        // Runtime instrumentation
        otel.AddRuntimeInstrumentation();
    });
```

## Migration Checklist

- [ ] Identify all TelemetryModules in applicationinsights.config or code
- [ ] Verify automatic instrumentations cover your needs
- [ ] Add explicit instrumentations for non-default scenarios
- [ ] Migrate PerformanceCollectorModule to Meter API
- [ ] Convert custom TelemetryModules to BackgroundService/HostedService
- [ ] Remove applicationinsights.config file
- [ ] Test all telemetry types (requests, dependencies, metrics)
- [ ] Verify Live Metrics still works

## See Also

- [telemetry-modules-removed.md](../../breaking-changes/Web/telemetry-modules-removed.md)
- [instrumentation-libraries.md](../../opentelemetry-fundamentals/instrumentation-libraries.md)
- [meter.md](../../opentelemetry-fundamentals/meter.md)
