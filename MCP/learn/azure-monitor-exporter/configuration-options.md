# Azure Monitor Exporter Configuration

**Category:** Azure Monitor Exporter  
**Applies to:** Application Insights .NET SDK 3.x  
**Related:** [ConfigureOpenTelemetryBuilder.md](../api-reference/TelemetryConfiguration/ConfigureOpenTelemetryBuilder.md)

## Overview

The Azure Monitor OpenTelemetry Exporter sends telemetry from OpenTelemetry SDK to Application Insights. Configuration is done via `ApplicationInsightsServiceOptions` and exporter-specific options.

## Basic Configuration

```csharp
services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = "InstrumentationKey=...;IngestionEndpoint=https://...";
});
```

## Connection String Format

```
InstrumentationKey={guid};IngestionEndpoint=https://{region}.in.applicationinsights.azure.com/;LiveEndpoint=https://{region}.livediagnostics.monitor.azure.com/
```

### Minimal Connection String

```
InstrumentationKey=12345678-1234-1234-1234-123456789abc
```

### Full Connection String

```
InstrumentationKey=12345678-1234-1234-1234-123456789abc;IngestionEndpoint=https://westus2-1.in.applicationinsights.azure.com/;LiveEndpoint=https://westus2.livediagnostics.monitor.azure.com/
```

## Configuration Options

### From appsettings.json

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=..."
  }
}
```

```csharp
services.AddApplicationInsightsTelemetry();
// Automatically reads from Configuration["ApplicationInsights:ConnectionString"]
```

### From Environment Variable

```bash
APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=...
```

```csharp
services.AddApplicationInsightsTelemetry();
// Automatically reads from environment variable
```

### Explicit Configuration

```csharp
services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["MyCustomPath:ConnectionString"];
    options.EnableAdaptiveSampling = false;
    options.EnablePerformanceCounterCollectionModule = true;
});
```

## Secondary Exporters

```csharp
services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = "InstrumentationKey=primary...";
})
.ConfigureOpenTelemetryBuilder(builder =>
{
    // Add secondary Application Insights instance
    builder.AddAzureMonitorTraceExporter(exporterOptions =>
    {
        exporterOptions.ConnectionString = "InstrumentationKey=secondary...";
    });
});
```

## Exporter Options

```csharp
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.AddAzureMonitorTraceExporter(options =>
        {
            options.ConnectionString = "InstrumentationKey=...";
            // Additional exporter-specific options here
        });
    });
```

## Regional Endpoints

```csharp
// West US 2
options.ConnectionString = "InstrumentationKey=...;IngestionEndpoint=https://westus2-1.in.applicationinsights.azure.com/";

// East US
options.ConnectionString = "InstrumentationKey=...;IngestionEndpoint=https://eastus-8.in.applicationinsights.azure.com/";

// West Europe
options.ConnectionString = "InstrumentationKey=...;IngestionEndpoint=https://westeurope-5.in.applicationinsights.azure.com/";
```

## Authentication

### Connection String (Default)

```csharp
services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = "InstrumentationKey=...";
});
```

### Azure AD / Managed Identity (Future)

```csharp
// Managed Identity support may be added in future versions
// Check documentation for latest authentication options
```

## Verification

```csharp
// Enable diagnostics
Environment.SetEnvironmentVariable("OTEL_LOG_LEVEL", "Debug");

// Check logs for successful export
// Look for: "Exporter sending telemetry to Application Insights"
```

## See Also

- [ConfigureOpenTelemetryBuilder.md](../api-reference/TelemetryConfiguration/ConfigureOpenTelemetryBuilder.md)
- [sending-to-multiple-destinations.md](../common-scenarios/sending-to-multiple-destinations.md)
