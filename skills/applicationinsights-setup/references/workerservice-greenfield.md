# Worker Service — Greenfield Setup

## Overview

Add Application Insights telemetry to a .NET Worker Service (background services, message queue consumers, scheduled task runners, Windows Services / Linux daemons).

## Step 1: Add Package

```bash
dotnet add package Microsoft.ApplicationInsights.WorkerService --version 3.0.0-rc1
```

Or in `.csproj`:

```xml
<PackageReference Include="Microsoft.ApplicationInsights.WorkerService" Version="3.0.0-rc1" />
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

Inject `TelemetryClient` into your worker:

```csharp
public class Worker : BackgroundService
{
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<Worker> _logger;

    public Worker(TelemetryClient telemetryClient, ILogger<Worker> logger)
    {
        _telemetryClient = telemetryClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _telemetryClient.TrackEvent("WorkerIteration");
            _telemetryClient.TrackMetric("ItemsProcessed", processedCount);

            await Task.Delay(1000, stoppingToken);
        }
    }
}
```

## Best Practices

1. **Use structured logging**: ILogger integration sends logs to Application Insights automatically
2. **Track operation context**: Use `TelemetryClient.StartOperation` for long-running operations
3. **Flush on shutdown**: Call `TelemetryClient.Flush()` before application exits
4. **Configure sampling**: For high-volume services, configure adaptive sampling to control costs
