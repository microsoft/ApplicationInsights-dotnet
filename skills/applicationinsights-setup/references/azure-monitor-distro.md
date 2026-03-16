# Azure Monitor OpenTelemetry Distro

## Overview

The Azure Monitor OpenTelemetry Distro (`Azure.Monitor.OpenTelemetry.AspNetCore`) is a batteries-included package that configures OpenTelemetry with sensible defaults for Azure Monitor / Application Insights.

## What It Provides

One line of code enables full observability:

```csharp
builder.Services.AddOpenTelemetry().UseAzureMonitor();
```

### Automatic Instrumentation
- **ASP.NET Core** — HTTP requests (traces)
- **HttpClient** — Outgoing HTTP calls (dependencies)
- **SQL Client** — Database calls
- **Azure SDK** — Azure service calls

### Automatic Export
- Traces → Application Insights (requests, dependencies)
- Metrics → Application Insights (performance counters, custom metrics)
- Logs → Application Insights (traces, exceptions)

### Automatic Enrichment
- Cloud role name detection
- Kubernetes metadata
- Azure App Service metadata

## Configuration

### Connection String (Required)

Option 1: Environment variable (recommended)
```bash
APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=xxx;IngestionEndpoint=https://...
```

Option 2: appsettings.json
```json
{
  "AzureMonitor": {
    "ConnectionString": "InstrumentationKey=xxx;IngestionEndpoint=https://..."
  }
}
```

Option 3: Code
```csharp
builder.Services.AddOpenTelemetry().UseAzureMonitor(options =>
{
    options.ConnectionString = "InstrumentationKey=xxx;...";
});
```

### Optional Configuration

```csharp
builder.Services.AddOpenTelemetry().UseAzureMonitor(options =>
{
    options.SamplingRatio = 0.1f; // 10% sampling
    options.EnableLiveMetrics = false;
    options.Resource = ResourceBuilder.CreateDefault()
        .AddService("MyService", "1.0.0");
});
```

## Package Reference

```xml
<PackageReference Include="Azure.Monitor.OpenTelemetry.AspNetCore" Version="1.3.0" />
```

## When to Use

| Scenario | Recommendation |
|---|---|
| New ASP.NET Core app | Use Distro |
| Existing AI SDK 2.x app | Migrate to Distro |
| Need custom exporters | Consider manual OpenTelemetry setup |
| Non-ASP.NET Core (Worker/Console) | Use `Microsoft.ApplicationInsights.WorkerService` or `Azure.Monitor.OpenTelemetry.Exporter` directly |
