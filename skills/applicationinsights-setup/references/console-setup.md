# Console App — Setup

## Overview

Add Application Insights telemetry to a .NET Console application that does not use the Generic Host (`Host.CreateDefaultBuilder`). If your console app uses the Generic Host, use the Worker Service setup instead.

## Step 1: Add Package

```bash
dotnet add package Microsoft.ApplicationInsights --version 3.*
```

## Step 2: Configure TelemetryClient

```csharp
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

// Create configuration
var config = TelemetryConfiguration.CreateDefault();
config.ConnectionString = "InstrumentationKey=xxx;IngestionEndpoint=https://...";

// Or use environment variable: APPLICATIONINSIGHTS_CONNECTION_STRING

var client = new TelemetryClient(config);

try
{
    // Your application logic
    client.TrackEvent("AppStarted");
    client.TrackMetric("ItemsProcessed", count);

    // Track dependencies manually
    using var operation = client.StartOperation<DependencyTelemetry>("ProcessBatch");
    // ... do work ...
    operation.Telemetry.Success = true;
}
catch (Exception ex)
{
    client.TrackException(ex);
    throw;
}
finally
{
    // Flush before exit — critical for console apps
    client.Flush();
    // Give the channel time to send
    await Task.Delay(TimeSpan.FromSeconds(5));
}
```

## Step 3: Configure Connection String

### Option A: Environment Variable

```bash
export APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=xxx;IngestionEndpoint=https://..."
```

### Option B: In Code

```csharp
config.ConnectionString = "InstrumentationKey=xxx;IngestionEndpoint=https://...";
```

## Important: Flush Before Exit

Console applications must call `client.Flush()` before exiting. Without this, buffered telemetry will be lost. Add a short delay after flush to allow the channel to transmit.

## For Generic Host Console Apps

If your console app uses `Host.CreateDefaultBuilder` or `Host.CreateApplicationBuilder`, use the Worker Service package instead:

```bash
dotnet add package Microsoft.ApplicationInsights.WorkerService --version 3.0.0-rc1
```

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddApplicationInsightsTelemetryWorkerService();
var host = builder.Build();
await host.RunAsync();
```

This provides automatic dependency injection, lifecycle management, and telemetry flushing.
