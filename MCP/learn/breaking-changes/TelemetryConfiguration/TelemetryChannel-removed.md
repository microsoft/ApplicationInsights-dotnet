# TelemetryConfiguration.TelemetryChannel Removed

**Category:** Breaking Change  
**Applies to:** TelemetryConfiguration API  
**Migration Effort:** Medium  
**Related:** [connection-string.md](../../azure-monitor-exporter/connection-string.md), [exporter-configuration.md](../../azure-monitor-exporter/exporter-configuration.md)

## Change Summary

The `TelemetryChannel` property has been removed from `TelemetryConfiguration` in 3.x. Custom `ITelemetryChannel` implementations are no longer supported. Channel configuration is now handled by the Azure Monitor OpenTelemetry Exporter through the `ConnectionString` property.

## API Comparison

### 2.x API

```csharp
// Source: ApplicationInsights-dotnet-2x patterns
public class TelemetryConfiguration
{
    public ITelemetryChannel TelemetryChannel { get; set; }
}

// Custom channel implementations
public class ServerTelemetryChannel : ITelemetryChannel
{
    public bool? DeveloperMode { get; set; }
    public string EndpointAddress { get; set; }
    public void Send(ITelemetry item) { }
    public void Flush() { }
}
```

### 3.x API

```csharp
// REMOVED: TelemetryChannel property does not exist
// REMOVED: ITelemetryChannel interface

// Configuration via ConnectionString and exporter options
public class TelemetryConfiguration
{
    public string ConnectionString { get; set; }
}
```

## Migration Strategies

### Option 1: Basic Channel Configuration

**2.x:**
```csharp
var config = TelemetryConfiguration.CreateDefault();
var channel = new ServerTelemetryChannel
{
    DeveloperMode = true,
    EndpointAddress = "https://custom-endpoint.azurewebsites.net/v2/track"
};
config.TelemetryChannel = channel;
```

**3.x:**
```csharp
var config = TelemetryConfiguration.CreateDefault();
// Channel configuration via ConnectionString
config.ConnectionString = "InstrumentationKey=abc123-...;IngestionEndpoint=https://custom-endpoint/";
```

### Option 2: ASP.NET Core Configuration

**2.x:**
```csharp
services.AddApplicationInsightsTelemetry(options =>
{
    options.InstrumentationKey = "abc123-...";
    options.DeveloperMode = true;
    options.EndpointAddress = "https://custom-endpoint/v2/track";
});
```

**3.x:**
```csharp
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = "InstrumentationKey=abc123-...;IngestionEndpoint=https://custom-endpoint/";
    options.DeveloperMode = true;
});
```

### Option 3: Custom Exporter Configuration

**2.x:**
```csharp
var channel = new InMemoryChannel
{
    MaxTelemetryBufferCapacity = 1000,
    MaxTransmissionBufferCapacity = 500
};
config.TelemetryChannel = channel;
```

**3.x:**
```csharp
builder.Services.ConfigureOpenTelemetryTracerProvider(builder =>
{
    builder.AddAzureMonitorTraceExporter(options =>
    {
        options.ConnectionString = "InstrumentationKey=...";
        // Exporter-specific configuration
    });
});
```

## Common Scenarios

### Scenario 1: Developer Mode

**2.x:**
```csharp
var channel = new ServerTelemetryChannel { DeveloperMode = true };
TelemetryConfiguration.Active.TelemetryChannel = channel;
```

**3.x:**
```csharp
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.DeveloperMode = true;  // Fast flushing for development
});
```

### Scenario 2: Custom Endpoint

**2.x:**
```csharp
var channel = new ServerTelemetryChannel
{
    EndpointAddress = "https://dc.services.visualstudio.com/v2/track"
};
config.TelemetryChannel = channel;
```

**3.x:**
```csharp
config.ConnectionString = "InstrumentationKey=abc123-...;IngestionEndpoint=https://dc.services.visualstudio.com/";
```

### Scenario 3: In-Memory Channel for Testing

**2.x:**
```csharp
var channel = new InMemoryChannel();
config.TelemetryChannel = channel;
```

**3.x:**
```csharp
// Use ConnectionString with test endpoint
config.ConnectionString = "InstrumentationKey=test-key;IngestionEndpoint=https://test-endpoint/";
```

## Migration Checklist

- [ ] Identify all `TelemetryChannel` property usages
- [ ] Replace `EndpointAddress` with `IngestionEndpoint` in ConnectionString
- [ ] Move `DeveloperMode` to ApplicationInsightsServiceOptions
- [ ] Remove custom `ITelemetryChannel` implementations
- [ ] Update to use Azure Monitor exporter configuration

## See Also

- [connection-string.md](../../azure-monitor-exporter/connection-string.md) - ConnectionString format
- [exporter-configuration.md](../../azure-monitor-exporter/exporter-configuration.md) - Exporter options
