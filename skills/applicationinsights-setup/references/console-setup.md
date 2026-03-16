# Console App — Setup

## Overview

For console applications, the **recommended approach** is to use the Worker Service package with the Generic Host. This gives you automatic dependency injection, lifecycle management, telemetry flushing, and auto-collected dependencies — the same experience as ASP.NET Core.

For console apps that cannot use the Generic Host (simple scripts, class libraries with standalone telemetry), a manual `TelemetryConfiguration` + `TelemetryClient` approach is available below.

## Recommended: Worker Service Package with Generic Host

### Step 1: Add Package

```bash
dotnet add package Microsoft.ApplicationInsights.WorkerService
```

### Step 2: Configure in Program.cs

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplicationInsightsTelemetryWorkerService();
builder.Services.AddHostedService<MyWorker>();

var host = builder.Build();
await host.RunAsync();
```

Or with `Host.CreateDefaultBuilder`:

```csharp
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.AddHostedService<MyWorker>();
    })
    .Build();

await host.RunAsync();
```

### Step 3: Configure Connection String

Set via environment variable (recommended):
```bash
export APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=xxx;IngestionEndpoint=https://..."
```

Or in `appsettings.json`:
```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=xxx;IngestionEndpoint=https://..."
  }
}
```

This provides automatic dependency tracking (HTTP, SQL), performance counters, exception tracking, ILogger integration, and graceful telemetry flushing on shutdown.

For full Worker Service details, see [workerservice-greenfield.md](workerservice-greenfield.md).

---

## Alternative: Manual Setup (No Generic Host)

Use this approach for simple console scripts or class libraries that need standalone telemetry without hosting infrastructure.

### Step 1: Add Package

```bash
dotnet add package Microsoft.ApplicationInsights --version 3.*
```

### Step 2: Configure TelemetryClient

```csharp
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using System.Diagnostics;

// Create configuration
using var config = TelemetryConfiguration.CreateDefault();
config.ConnectionString = "InstrumentationKey=xxx;IngestionEndpoint=https://...";

// Or use environment variable: APPLICATIONINSIGHTS_CONNECTION_STRING

var client = new TelemetryClient(config);
var activitySource = new ActivitySource("MyConsoleApp");

try
{
    // Your application logic
    client.TrackEvent("AppStarted");
    client.TrackMetric("ItemsProcessed", count);

    // Use ActivitySource for operation tracking
    using var activity = activitySource.StartActivity("ProcessBatch", ActivityKind.Internal);
    // ... do work ...
    activity?.SetStatus(ActivityStatusCode.Ok);
}
catch (Exception ex)
{
    client.TrackException(ex);
    throw;
}
finally
{
    // Flush before exit — critical for console apps without hosting
    client.Flush();
}
```

### Important: Flush Before Exit

Without the Generic Host, there is no automatic shutdown hook. You **must** call `client.Flush()` before exiting, otherwise buffered telemetry will be lost.

### Limitations of Manual Setup

- No automatic dependency tracking (HTTP, SQL) — you must track dependencies manually or add OpenTelemetry instrumentation via `config.ConfigureOpenTelemetryBuilder(...)`
- No automatic ILogger integration
- No graceful shutdown / flush — you must handle it yourself
- No DI — services like `TelemetryClient` are created manually
