# Application Insights for Worker Services

The Microsoft Application Insights for Worker Services SDK sends telemetry data to Azure Monitor following the OpenTelemetry Specification. This library can be used to instrument your .NET Worker Services, background services, and console applications to collect and send telemetry data to Azure Monitor for analysis and monitoring.

> **Note**: For ASP.NET Core web applications, see [ASP.NET Core documentation](Readme.md).

## NuGet Package

- **[Microsoft.ApplicationInsights.WorkerService](https://www.nuget.org/packages/Microsoft.ApplicationInsights.WorkerService/)**
[![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.WorkerService.svg)](https://nuget.org/packages/Microsoft.ApplicationInsights.WorkerService)

## Getting Started

### Prerequisites

- **Azure Subscription:** To use Azure services, including Azure Monitor, you'll need a subscription. If you do not have an existing Azure account, you may sign up for a [free trial](https://azure.microsoft.com/free/dotnet/) or use your [Visual Studio Subscription](https://visualstudio.microsoft.com/subscriptions/) benefits when you [create an account](https://azure.microsoft.com/account).

- **Azure Application Insights Connection String:** To send telemetry data to the monitoring service you'll need a connection string from Azure Application Insights. If you are not familiar with creating Azure resources, you may wish to follow the step-by-step guide for [Create an Application Insights resource](https://learn.microsoft.com/azure/azure-monitor/app/create-new-resource) and [copy the connection string](https://learn.microsoft.com/azure/azure-monitor/app/sdk-connection-string?tabs=net#find-your-connection-string).

### What is Included

The Microsoft.ApplicationInsights.WorkerService package is built on top of OpenTelemetry and includes:

- **HTTP Client Instrumentation**: Automatic tracing for outgoing HTTP requests
- **SQL Client Instrumentation**: Automatic tracing for SQL queries
- **Application Insights Standard Metrics**: Automatic collection of standard metrics
- **Logs via Microsoft.Extensions.Logging**: Full logging integration
- **Azure Resource Detectors**: Automatic detection of Azure environment (App Service, VM, Container Apps)
- **Live Metrics**: Real-time monitoring support
- **Azure Monitor Exporter**: Sends all telemetry to Azure Monitor

### Install the Package

```dotnetcli
dotnet add package Microsoft.ApplicationInsights.WorkerService
```

### Enabling Application Insights

#### Example 1: Worker Service (Background Service)

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Add Application Insights telemetry
builder.Services.AddApplicationInsightsTelemetryWorkerService();

// Add your worker service
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
```

#### Example 2: Console Application

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Add Application Insights telemetry
builder.Services.AddApplicationInsightsTelemetryWorkerService(options =>
{
    options.ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://...";
});

var host = builder.Build();

// Your application logic here

await host.RunAsync();
```

#### Example 3: Using Configuration

In your `appsettings.json`:

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://..."
  }
}
```

In your `Program.cs`:

```csharp
var builder = Host.CreateApplicationBuilder(args);

// Add Application Insights telemetry from configuration
builder.Services.AddApplicationInsightsTelemetryWorkerService(builder.Configuration);

var host = builder.Build();
host.Run();
```

## Configuration

### Connection String

The connection string can be configured in multiple ways (listed in order of precedence):

1. `ApplicationInsightsServiceOptions.ConnectionString` property set in code
2. `APPLICATIONINSIGHTS_CONNECTION_STRING` environment variable
3. `ApplicationInsights:ConnectionString` configuration setting

### ApplicationInsightsServiceOptions

Most configuration options available in ASP.NET Core also apply to Worker Services, except for request-specific options:

- `EnableRequestTrackingTelemetryModule`
- `EnableAuthenticationTrackingJavaScript`
- `RequestCollectionOptions`

```csharp
builder.Services.AddApplicationInsightsTelemetryWorkerService(options =>
{
    options.ConnectionString = "InstrumentationKey=...";
    options.EnableQuickPulseMetricStream = true;
    
    // Rate-limited sampling: maximum traces per second (default: 5)
    // Use this for rate-based sampling to limit telemetry volume
    options.TracesPerSecond = 5.0;
    
    // Percentage-based sampling: ratio of telemetry to collect (0.0 to 1.0)
    // Use this instead of TracesPerSecond for percentage-based sampling
    // options.SamplingRatio = 0.5f;  // 50% of telemetry
    
    // Enable or disable trace-based log sampling (default: true)
    // When true, logs are sampled based on the sampling decision of the associated trace
    options.EnableTraceBasedLogsSampler = true;
});
```

For detailed configuration options, see the [ASP.NET Core Configuration documentation](Readme.md#configuration).

## Usage in Worker Services

### Basic Worker Service Example

```csharp
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly TelemetryClient _telemetryClient;

    public Worker(ILogger<Worker> logger, TelemetryClient telemetryClient)
    {
        _logger = logger;
        _telemetryClient = telemetryClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            
            // Track custom events
            _telemetryClient.TrackEvent("WorkerExecuted");
            
            // Track custom metrics
            _telemetryClient.TrackMetric("QueueSize", 42);
            
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
```

## Advanced Configuration

All advanced configuration options from ASP.NET Core are available for Worker Services:

- **OpenTelemetry Extensibility**: Add custom ActivitySource, Meter, or additional instrumentation
- **TelemetryClient**: Use for custom telemetry tracking
- **OpenTelemetry Processors**: Add custom processors for traces, logs, and metrics
- **Log Scopes**: Enable and use log scopes for contextual logging
- **Resource Attributes**: Customize resource attributes

For detailed examples and guidance, see the [ASP.NET Core Advanced Configuration documentation](Readme.md#advanced-configuration).

## Examples

Complete working examples are available in the repository:

- [Worker Service Example](../examples/WorkerService/)

## Key Differences from ASP.NET Core

| Feature | ASP.NET Core | Worker Service |
|---------|--------------|----------------|
| Extension Method | `AddApplicationInsightsTelemetry()` | `AddApplicationInsightsTelemetryWorkerService()` |
| Host Type | `WebApplication` | `Host` |
| ASP.NET Core Instrumentation | ✅ Included (HTTP requests) | ❌ Not included |
| HTTP Client Instrumentation | ✅ Included | ✅ Included |
| SQL Client Instrumentation | ✅ Included | ✅ Included |
| Live Metrics | ✅ Supported | ✅ Supported |
| Custom Telemetry | ✅ TelemetryClient + OpenTelemetry APIs | ✅ TelemetryClient + OpenTelemetry APIs |
