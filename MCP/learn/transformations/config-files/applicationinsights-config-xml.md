# applicationinsights.config XML File Migration

**Category:** Transformation Guide  
**Applies to:** Classic ASP.NET applications using applicationinsights.config  
**Related:** [appsettings-json.md](appsettings-json.md)

## Overview

The XML-based `applicationinsights.config` file used in classic ASP.NET applications is not supported in 3.x. Configuration moves to code-based setup or `appsettings.json`.

## Before: applicationinsights.config (2.x)

### Typical applicationinsights.config

```xml
<?xml version="1.0" encoding="utf-8"?>
<ApplicationInsights xmlns="http://schemas.microsoft.com/ApplicationInsights/2013/Settings">
  <InstrumentationKey>12345678-1234-1234-1234-123456789012</InstrumentationKey>
  
  <TelemetryInitializers>
    <Add Type="MyApp.CloudRoleNameInitializer, MyApp"/>
    <Add Type="MyApp.UserContextInitializer, MyApp"/>
  </TelemetryInitializers>
  
  <TelemetryProcessors>
    <Add Type="Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse.QuickPulseTelemetryProcessor, Microsoft.AI.PerfCounterCollector"/>
    <Add Type="MyApp.FilteringProcessor, MyApp"/>
  </TelemetryProcessors>
  
  <TelemetryModules>
    <Add Type="Microsoft.ApplicationInsights.DependencyCollector.DependencyTrackingTelemetryModule, Microsoft.AI.DependencyCollector">
      <ExcludeComponentCorrelationHttpHeadersOnDomains>
        <Add>core.windows.net</Add>
      </ExcludeComponentCorrelationHttpHeadersOnDomains>
    </Add>
    
    <Add Type="Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.PerformanceCollectorModule, Microsoft.AI.PerfCounterCollector">
      <Counters>
        <Add PerformanceCounter="\Process(??APP_WIN32_PROC??)\% Processor Time" ReportAs="CPU" />
        <Add PerformanceCounter="\Memory\Available Bytes" ReportAs="Memory Available" />
      </Counters>
    </Add>
    
    <Add Type="Microsoft.ApplicationInsights.Web.RequestTrackingTelemetryModule, Microsoft.AI.Web"/>
    <Add Type="Microsoft.ApplicationInsights.Web.ExceptionTrackingTelemetryModule, Microsoft.AI.Web"/>
  </TelemetryModules>
  
  <TelemetryChannel Type="Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.ServerTelemetryChannel, Microsoft.AI.ServerTelemetryChannel">
    <MaxTelemetryBufferCapacity>100</MaxTelemetryBufferCapacity>
  </TelemetryChannel>
</ApplicationInsights>
```

## After: Program.cs (3.x)

### Code-Based Configuration

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry(options =>
{
    // InstrumentationKey → ConnectionString
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
})
.ConfigureOpenTelemetryBuilder(otelBuilder =>
{
    // Cloud role name (replaces CloudRoleNameInitializer)
    otelBuilder.ConfigureResource(resource =>
    {
        resource.AddService("MyApp", serviceVersion: "1.0.0");
    });
    
    // TelemetryInitializers → Processors
    otelBuilder.AddProcessor<UserContextProcessor>();
    
    // TelemetryProcessors → Processors
    otelBuilder.AddProcessor<FilteringProcessor>();
    
    // TelemetryModules are now automatic via instrumentation
    // - DependencyTrackingTelemetryModule → Automatic via HttpClient instrumentation
    // - RequestTrackingTelemetryModule → Automatic via ASP.NET Core instrumentation
    // - ExceptionTrackingTelemetryModule → Automatic exception tracking
});

var app = builder.Build();
```

### Configuration via appsettings.json (Optional)

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=12345678-1234-1234-1234-123456789012;IngestionEndpoint=https://...",
    "CloudRoleName": "MyApp",
    "EnableAdaptiveSampling": false,
    "EnablePerformanceCounterCollectionModule": true
  }
}
```

## Element-by-Element Migration

### InstrumentationKey → ConnectionString

```xml
<!-- 2.x -->
<InstrumentationKey>12345678-1234-1234-1234-123456789012</InstrumentationKey>
```

```csharp
// 3.x
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = "InstrumentationKey=12345678-1234-1234-1234-123456789012";
});
```

### TelemetryInitializers → Processors

```xml
<!-- 2.x -->
<TelemetryInitializers>
  <Add Type="MyApp.CloudRoleNameInitializer, MyApp"/>
  <Add Type="MyApp.UserContextInitializer, MyApp"/>
</TelemetryInitializers>
```

```csharp
// 3.x
builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        // Convert initializers to processors
        otel.AddProcessor<UserContextProcessor>();
        
        // Cloud role name via resource
        otel.ConfigureResource(r => r.AddService("MyApp"));
    });
```

### TelemetryProcessors → Processors

```xml
<!-- 2.x -->
<TelemetryProcessors>
  <Add Type="MyApp.FilteringProcessor, MyApp"/>
  <Add Type="MyApp.EnrichmentProcessor, MyApp"/>
</TelemetryProcessors>
```

```csharp
// 3.x
builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        otel.AddProcessor<FilteringProcessor>();
        otel.AddProcessor<EnrichmentProcessor>();
    });
```

### TelemetryModules → Automatic Instrumentation

```xml
<!-- 2.x -->
<TelemetryModules>
  <Add Type="Microsoft.ApplicationInsights.DependencyCollector.DependencyTrackingTelemetryModule, Microsoft.AI.DependencyCollector"/>
  <Add Type="Microsoft.ApplicationInsights.Web.RequestTrackingTelemetryModule, Microsoft.AI.Web"/>
</TelemetryModules>
```

```csharp
// 3.x - Automatic (no configuration needed)
// ASP.NET Core middleware automatically tracks requests
// HttpClient instrumentation automatically tracks dependencies
builder.Services.AddApplicationInsightsTelemetry();
```

### Performance Counters

```xml
<!-- 2.x -->
<TelemetryModules>
  <Add Type="Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.PerformanceCollectorModule, Microsoft.AI.PerfCounterCollector">
    <Counters>
      <Add PerformanceCounter="\Process(??APP_WIN32_PROC??)\% Processor Time" ReportAs="CPU" />
    </Counters>
  </Add>
</TelemetryModules>
```

```csharp
// 3.x - Via options
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.EnablePerformanceCounterCollectionModule = true;
});
```

### Sampling Configuration

```xml
<!-- 2.x -->
<TelemetryProcessors>
  <Add Type="Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.AdaptiveSamplingTelemetryProcessor, Microsoft.AI.ServerTelemetryChannel">
    <MaxTelemetryItemsPerSecond>5</MaxTelemetryItemsPerSecond>
  </Add>
</TelemetryProcessors>
```

```csharp
// 3.x - Fixed-ratio sampling
builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        otel.SetSampler(new TraceIdRatioBasedSampler(0.1)); // 10%
    });
```

### TelemetryChannel → Not Configurable

```xml
<!-- 2.x -->
<TelemetryChannel Type="Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.ServerTelemetryChannel, Microsoft.AI.ServerTelemetryChannel">
  <MaxTelemetryBufferCapacity>100</MaxTelemetryBufferCapacity>
</TelemetryChannel>
```

```csharp
// 3.x - Channel is managed internally, not user-configurable
// No equivalent configuration
```

## Complete Migration Example

### Before: Full applicationinsights.config

```xml
<?xml version="1.0" encoding="utf-8"?>
<ApplicationInsights xmlns="http://schemas.microsoft.com/ApplicationInsights/2013/Settings">
  <InstrumentationKey>12345678-1234-1234-1234-123456789012</InstrumentationKey>
  
  <TelemetryInitializers>
    <Add Type="MyApp.CloudRoleNameInitializer, MyApp"/>
    <Add Type="MyApp.EnvironmentInitializer, MyApp"/>
  </TelemetryInitializers>
  
  <TelemetryProcessors>
    <Add Type="MyApp.HealthCheckFilter, MyApp"/>
    <Add Type="Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.AdaptiveSamplingTelemetryProcessor, Microsoft.AI.ServerTelemetryChannel">
      <MaxTelemetryItemsPerSecond>5</MaxTelemetryItemsPerSecond>
    </Add>
  </TelemetryProcessors>
  
  <TelemetryModules>
    <Add Type="Microsoft.ApplicationInsights.DependencyCollector.DependencyTrackingTelemetryModule, Microsoft.AI.DependencyCollector"/>
    <Add Type="Microsoft.ApplicationInsights.Web.RequestTrackingTelemetryModule, Microsoft.AI.Web"/>
  </TelemetryModules>
</ApplicationInsights>
```

### After: Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
})
.ConfigureOpenTelemetryBuilder(otelBuilder =>
{
    // Cloud role name
    otelBuilder.ConfigureResource(resource =>
    {
        var roleName = builder.Configuration["ApplicationInsights:CloudRoleName"] ?? "MyApp";
        resource.AddService(roleName);
    });
    
    // Custom processors (converted from initializers and processors)
    otelBuilder.AddProcessor<EnvironmentProcessor>();
    otelBuilder.AddProcessor<HealthCheckFilter>();
    
    // Sampling
    otelBuilder.SetSampler(new TraceIdRatioBasedSampler(0.1));
});

// Request and dependency tracking is automatic
var app = builder.Build();
app.Run();
```

## Migration Checklist

- [ ] Remove `applicationinsights.config` file
- [ ] Convert `InstrumentationKey` to `ConnectionString` in appsettings.json or code
- [ ] Convert `TelemetryInitializers` to `BaseProcessor<Activity>` classes
- [ ] Convert `TelemetryProcessors` to `BaseProcessor<Activity>` classes
- [ ] Remove `TelemetryModules` (automatic in 3.x)
- [ ] Convert adaptive sampling to fixed-ratio sampling
- [ ] Remove `TelemetryChannel` configuration
- [ ] Register processors via `ConfigureOpenTelemetryBuilder`
- [ ] Test that telemetry still flows correctly

## Common Patterns

### Pattern 1: Simple App (No Customization)

```xml
<!-- 2.x -->
<ApplicationInsights>
  <InstrumentationKey>...</InstrumentationKey>
</ApplicationInsights>
```

```csharp
// 3.x
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = "InstrumentationKey=...";
});
```

### Pattern 2: Custom Initializers/Processors

```xml
<!-- 2.x -->
<TelemetryInitializers>
  <Add Type="MyApp.MyInitializer, MyApp"/>
</TelemetryInitializers>
<TelemetryProcessors>
  <Add Type="MyApp.MyProcessor, MyApp"/>
</TelemetryProcessors>
```

```csharp
// 3.x
builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        otel.AddProcessor<MyProcessor>(); // Both initializers and processors
    });
```

### Pattern 3: Performance Counters Enabled

```xml
<!-- 2.x -->
<TelemetryModules>
  <Add Type="Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.PerformanceCollectorModule, ..."/>
</TelemetryModules>
```

```csharp
// 3.x
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.EnablePerformanceCounterCollectionModule = true;
});
```

## See Also

- [appsettings-json.md](appsettings-json.md)
- [TelemetryInitializers-removed.md](../TelemetryConfiguration/TelemetryInitializers-removed.md)
- [TelemetryProcessors-removed.md](../TelemetryConfiguration/TelemetryProcessors-removed.md)
