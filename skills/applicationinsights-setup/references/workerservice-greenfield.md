# Worker Service — Greenfield Setup

## Overview

Add Application Insights telemetry to a .NET Worker Service (background services, message queue consumers, scheduled task runners, Windows Services / Linux daemons).

## Step 1: Add Package

```bash
dotnet add package Microsoft.ApplicationInsights.WorkerService
```

Or in `.csproj`:

```xml
<PackageReference Include="Microsoft.ApplicationInsights.WorkerService" Version="3.*" />
```

## Step 2: Configure in Program.cs

### Using Host.CreateDefaultBuilder

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
```

### Using Host.CreateApplicationBuilder (Modern Pattern)

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplicationInsightsTelemetryWorkerService();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
await host.RunAsync();
```

### With Configuration Options

```csharp
services.AddApplicationInsightsTelemetryWorkerService(options =>
{
    options.ConnectionString = configuration["ApplicationInsights:ConnectionString"];
    options.EnableDependencyTrackingTelemetryModule = true;
});
```

## Step 3: Configure Connection String

### Option A: Environment Variable (Recommended)

```bash
export APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=xxx;IngestionEndpoint=https://..."
```

### Option B: appsettings.json

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=xxx;IngestionEndpoint=https://..."
  }
}
```

## What Gets Instrumented Automatically

- **Dependency Tracking**: HTTP client calls, SQL queries, Azure SDK calls
- **Performance Counters**: CPU, memory, GC metrics
- **Exception Tracking**: Unhandled exceptions
- **Custom Telemetry**: Via `TelemetryClient` injection

## Adding Custom Telemetry

Use `ActivitySource` for custom distributed tracing (preferred over `TelemetryClient`):

```csharp
using System.Diagnostics;

public class Worker : BackgroundService
{
    private static readonly ActivitySource _activitySource = new("MyApp.Worker");
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var activity = _activitySource.StartActivity("ProcessBatch", ActivityKind.Internal);
            activity?.SetTag("batch.size", batchSize);

            // ... do work ...

            activity?.SetStatus(ActivityStatusCode.Ok);
            await Task.Delay(1000, stoppingToken);
        }
    }
}
```

Register the source:
```csharp
builder.Services.ConfigureOpenTelemetryTracerProvider(tracing =>
    tracing.AddSource("MyApp.Worker"));
```

For custom metrics, use `System.Diagnostics.Metrics.Meter`. See [custom-activities.md](custom-activities.md) and [custom-metrics.md](custom-metrics.md) for full details.

## Best Practices

1. **Use structured logging**: ILogger integration sends logs to Application Insights automatically
2. **Track operation context**: Use `ActivitySource.StartActivity` for long-running operations (preferred over `TelemetryClient.StartOperation`)
3. **Flush on shutdown**: Call `TelemetryClient.Flush()` before application exits
4. **Configure sampling**: For high-volume services, configure rate-limited (`TracesPerSecond`) or fixed-rate (`SamplingRatio`) sampling to control costs
