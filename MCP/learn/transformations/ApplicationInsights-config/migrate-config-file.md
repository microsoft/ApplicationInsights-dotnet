# Configuration File Migration (applicationinsights.config)

**Category:** Transformation Pattern  
**Applies to:** Migrating from applicationinsights.config to appsettings.json  
**Related:** [migrate-configuration-properties.md](../TelemetryConfiguration/migrate-configuration-properties.md)

## Overview

Application Insights 2.x used `ApplicationInsights.config` XML file for configuration. In 3.x, configuration is done through:
- **appsettings.json** (preferred for ASP.NET Core)
- **Code-based configuration** in Program.cs
- **Environment variables**

## File Location Changes

| 2.x | 3.x |
|-----|-----|
| `ApplicationInsights.config` (project root) | `appsettings.json` |
| XML format | JSON format |
| Loaded automatically | Loaded via IConfiguration |

## Basic Configuration Migration

### Before (2.x): ApplicationInsights.config

```xml
<?xml version="1.0" encoding="utf-8"?>
<ApplicationInsights xmlns="http://schemas.microsoft.com/ApplicationInsights/2013/Settings">
  <InstrumentationKey>00000000-0000-0000-0000-000000000000</InstrumentationKey>
  <TelemetryInitializers>
    <Add Type="Microsoft.ApplicationInsights.DependencyCollector.HttpDependenciesParsingTelemetryInitializer, Microsoft.AI.DependencyCollector"/>
    <Add Type="MyApp.CustomTelemetryInitializer, MyApp"/>
  </TelemetryInitializers>
  <TelemetryProcessors>
    <Add Type="Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse.QuickPulseTelemetryProcessor, Microsoft.AI.PerfCounterCollector"/>
    <Add Type="Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.AdaptiveSamplingTelemetryProcessor, Microsoft.AI.ServerTelemetryChannel">
      <MaxTelemetryItemsPerSecond>5</MaxTelemetryItemsPerSecond>
    </Add>
  </TelemetryProcessors>
  <TelemetryChannel Type="Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.ServerTelemetryChannel, Microsoft.AI.ServerTelemetryChannel"/>
</ApplicationInsights>
```

### After (3.x): appsettings.json + Program.cs

**appsettings.json:**

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://westus2-1.in.applicationinsights.azure.com/",
    "CloudRoleName": "MyApp"
  },
  "OpenTelemetry": {
    "Sampling": {
      "Ratio": 0.2
    }
  }
}
```

**Program.cs:**

```csharp
builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        // Resource configuration (Cloud Role Name)
        otel.ConfigureResource(resource =>
        {
            var roleName = builder.Configuration["ApplicationInsights:CloudRoleName"] ?? "MyApp";
            resource.AddService(serviceName: roleName);
        });
        
        // Custom processors
        otel.AddProcessor<CustomProcessor>();
        
        // Sampling
        var samplingRatio = builder.Configuration.GetValue<double>("OpenTelemetry:Sampling:Ratio", 1.0);
        otel.SetSampler(new TraceIdRatioBasedSampler(samplingRatio));
    });
```

## Telemetry Initializers Migration

### Before (2.x): config

```xml
<TelemetryInitializers>
  <Add Type="MyApp.CustomTelemetryInitializer, MyApp"/>
  <Add Type="MyApp.UserContextInitializer, MyApp"/>
</TelemetryInitializers>
```

### After (3.x): Code-based

```csharp
// Telemetry Initializers → BaseProcessor with OnStart
otel.AddProcessor(sp => new CustomProcessor(
    sp.GetRequiredService<IConfiguration>()));
otel.AddProcessor<UserContextProcessor>();
```

## Telemetry Processors Migration

### Before (2.x): config

```xml
<TelemetryProcessors>
  <Add Type="Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.AdaptiveSamplingTelemetryProcessor, Microsoft.AI.ServerTelemetryChannel">
    <MaxTelemetryItemsPerSecond>5</MaxTelemetryItemsPerSecond>
    <IncludedTypes>Request;Dependency</IncludedTypes>
  </Add>
  <Add Type="MyApp.FilteringProcessor, MyApp">
    <ExcludedPaths>/health;/metrics</ExcludedPaths>
  </Add>
</TelemetryProcessors>
```

### After (3.x): Code-based + config

**appsettings.json:**

```json
{
  "OpenTelemetry": {
    "Sampling": {
      "Ratio": 0.2
    }
  },
  "Filtering": {
    "ExcludedPaths": ["/health", "/metrics"]
  }
}
```

**Program.cs:**

```csharp
otel.AddProcessor(sp => new FilteringProcessor(
    sp.GetRequiredService<IConfiguration>()
        .GetSection("Filtering:ExcludedPaths")
        .Get<string[]>() ?? Array.Empty<string>()));

otel.SetSampler(new TraceIdRatioBasedSampler(
    builder.Configuration.GetValue<double>("OpenTelemetry:Sampling:Ratio", 1.0)));
```

## Telemetry Modules Migration

### Before (2.x): config

```xml
<TelemetryModules>
  <Add Type="Microsoft.ApplicationInsights.DependencyCollector.DependencyTrackingTelemetryModule, Microsoft.AI.DependencyCollector">
    <EnableSqlCommandTextInstrumentation>true</EnableSqlCommandTextInstrumentation>
  </Add>
  <Add Type="Microsoft.ApplicationInsights.WindowsServer.DeveloperModeWithDebuggerAttachedTelemetryModule, Microsoft.AI.WindowsServer"/>
</TelemetryModules>
```

### After (3.x): Built-in instrumentation

```csharp
// Dependency tracking enabled automatically
builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        // HTTP, SQL automatically instrumented
        // Configure via OpenTelemetry instrumentation packages
        
        // For SQL command text, use:
        otel.AddSqlClientInstrumentation(options =>
        {
            options.SetDbStatementForText = true;
        });
    });
```

## Connection String Migration

### Before (2.x): InstrumentationKey

```xml
<InstrumentationKey>00000000-0000-0000-0000-000000000000</InstrumentationKey>
```

### After (3.x): ConnectionString

**appsettings.json:**

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://westus2-1.in.applicationinsights.azure.com/"
  }
}
```

**Program.cs:**

```csharp
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
});
```

## Complete Migration Example

### Before (2.x): ApplicationInsights.config

```xml
<?xml version="1.0" encoding="utf-8"?>
<ApplicationInsights xmlns="http://schemas.microsoft.com/ApplicationInsights/2013/Settings">
  <InstrumentationKey>12345678-1234-1234-1234-123456789012</InstrumentationKey>
  
  <TelemetryInitializers>
    <Add Type="MyApp.CloudRoleNameInitializer, MyApp"/>
    <Add Type="MyApp.UserContextInitializer, MyApp"/>
    <Add Type="MyApp.CustomPropertiesInitializer, MyApp"/>
  </TelemetryInitializers>
  
  <TelemetryProcessors>
    <Add Type="Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse.QuickPulseTelemetryProcessor, Microsoft.AI.PerfCounterCollector"/>
    <Add Type="MyApp.HealthCheckFilterProcessor, MyApp"/>
    <Add Type="MyApp.ErrorOnlyProcessor, MyApp"/>
    <Add Type="Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.AdaptiveSamplingTelemetryProcessor, Microsoft.AI.ServerTelemetryChannel">
      <MaxTelemetryItemsPerSecond>5</MaxTelemetryItemsPerSecond>
    </Add>
  </TelemetryProcessors>
  
  <TelemetryModules>
    <Add Type="Microsoft.ApplicationInsights.DependencyCollector.DependencyTrackingTelemetryModule, Microsoft.AI.DependencyCollector"/>
    <Add Type="Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.PerformanceCollectorModule, Microsoft.AI.PerfCounterCollector"/>
  </TelemetryModules>
  
  <TelemetryChannel Type="Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.ServerTelemetryChannel, Microsoft.AI.ServerTelemetryChannel">
    <DeveloperMode>false</DeveloperMode>
  </TelemetryChannel>
</ApplicationInsights>
```

### After (3.x): appsettings.json + Program.cs

**appsettings.json:**

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=12345678-1234-1234-1234-123456789012;IngestionEndpoint=https://westus2-1.in.applicationinsights.azure.com/",
    "CloudRoleName": "MyApp"
  },
  "OpenTelemetry": {
    "Sampling": {
      "Ratio": 0.2
    }
  },
  "CustomProperties": {
    "Environment": "Production",
    "Team": "MyTeam"
  }
}
```

**Program.cs:**

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
})
.ConfigureOpenTelemetryBuilder(otel =>
{
    // Configure Resource (Cloud Role Name)
    var roleName = builder.Configuration["ApplicationInsights:CloudRoleName"] ?? "MyApp";
    otel.ConfigureResource(resource =>
    {
        resource.AddService(serviceName: roleName);
    });
    
    // Add custom processors (replace initializers and processors)
    otel.AddProcessor(sp => new CustomPropertiesProcessor(
        sp.GetRequiredService<IConfiguration>().GetSection("CustomProperties")));
    otel.AddProcessor<UserContextProcessor>();
    otel.AddProcessor<HealthCheckFilterProcessor>();
    otel.AddProcessor<ErrorOnlyProcessor>();
    
    // Configure sampling
    var samplingRatio = builder.Configuration.GetValue<double>("OpenTelemetry:Sampling:Ratio", 1.0);
    otel.SetSampler(new TraceIdRatioBasedSampler(samplingRatio));
});

var app = builder.Build();
app.Run();
```

## Environment Variable Configuration

### Before (2.x): Not commonly used

### After (3.x): First-class support

```bash
# Set connection string
export APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=..."

# Set cloud role name
export OTEL_SERVICE_NAME="MyApp"

# Set environment
export OTEL_RESOURCE_ATTRIBUTES="deployment.environment=production"
```

```csharp
// Automatically reads from environment variables
builder.Services.AddApplicationInsightsTelemetry();
```

## Migration Checklist

- [ ] Copy InstrumentationKey to ConnectionString in appsettings.json
- [ ] Convert TelemetryInitializers to BaseProcessor with OnStart
- [ ] Convert TelemetryProcessors to BaseProcessor with OnEnd
- [ ] Replace AdaptiveSampling with TraceIdRatioBasedSampler
- [ ] Set CloudRoleName via ConfigureResource
- [ ] Remove ApplicationInsights.config file
- [ ] Test all custom telemetry logic
- [ ] Verify telemetry appears in Azure Monitor

## Configuration Sources Priority

3.x follows standard ASP.NET Core configuration precedence:

1. Command-line arguments (highest)
2. Environment variables
3. appsettings.{Environment}.json
4. appsettings.json
5. Default values (lowest)

```csharp
// Override from environment variable
builder.Configuration["ApplicationInsights:ConnectionString"] = 
    Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING") 
    ?? builder.Configuration["ApplicationInsights:ConnectionString"];
```

## Benefits of 3.x Configuration

1. **Standard .NET Configuration**: Uses IConfiguration
2. **Environment-Specific**: appsettings.Development.json, appsettings.Production.json
3. **Type-Safe**: Bind to strongly-typed options classes
4. **Testable**: Easy to mock IConfiguration
5. **Flexible**: Code, JSON, environment variables, Azure Key Vault, etc.

## See Also

- [migrate-configuration-properties.md](../TelemetryConfiguration/migrate-configuration-properties.md)
- [enrichment-with-onstart.md](../ITelemetryInitializer/enrichment-with-onstart.md)
- [filtering-with-onend.md](../ITelemetryProcessor/filtering-with-onend.md)
- [configuration-options.md](../../azure-monitor-exporter/configuration-options.md)
