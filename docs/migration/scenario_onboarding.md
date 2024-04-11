# Onboarding

The Application Insights .NET SDK had multiple ways to get started and required you to select a TelemetryChannel (either `InMemoryChannel` or `ServerTelemetryChannel`) which provided different features.

For OpenTelemetry, each of the 3 telemetry signals (Traces, Metrics, and Logs) have a unique configuration. Instead of TelemetryChannels, you'll configure an Azure Monitor Exporter for each telemetry signal. We provide a one-line extension method for AspNetCore that configures all 3 signals at once. You can also manually configure each signal individually.

## ASP.NET Core

Prerequisite: using ASP.NET Core.

Using Application Insights:
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationInsightsTelemetry();
```

Using OpenTelemetry:

1. Take a dependency on our package: https://www.nuget.org/packages/Azure.Monitor.OpenTelemetry.AspNetCore

2. Use `AddOpenTelemetry()` to add the OpenTelemetry SDK to your ServiceCollection. Then invoke `UseAzureMonitor()` to configure an Azure Monitor Exporter for all three signals.
    ```csharp
    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddOpenTelemetry().UseAzureMonitor();
    ```

We've written a detailed onboarding guide for this scenario here: [Enable Azure Monitor OpenTelemetry for ASP.NET Core](https://learn.microsoft.com/azure/azure-monitor/app/opentelemetry-enable?tabs=aspnetcore)

## All Other .NET Applications

Prerequisite: using a currently supported version of .NET.

Using Application Insights:
```csharp
var telemetryConfiguration = new TelemetryConfiguration()
{
    ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000"
};
var telemetryClient = new TelemetryClient(telemetryConfiguration);
```

Using OpenTelemetry:

1. Take a dependency on our package: https://www.nuget.org/packages/Azure.Monitor.OpenTelemetry.Exporter

2. Use the OpenTelemetry SDK to create a Provider for the Traces and Metrics signals. Use the Add Azure Monitor Exporter method to individually configure our AzureMonitorExporter for  Use `AddOpenTelemetry()` to add the OpenTelemetry SDK to your ServiceCollection. Then invoke `UseAzureMonitor()` to configure an Azure Monitor Exporter for all three signals.
    ```csharp
    // Create a new tracer provider builder and add an Azure Monitor trace exporter to the tracer provider builder.
    // It is important to keep the TracerProvider instance active throughout the process lifetime.
    // See https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs/trace#tracerprovider-management
    var tracerProvider = Sdk.CreateTracerProviderBuilder()
        .AddAzureMonitorTraceExporter();

    // Add an Azure Monitor metric exporter to the metrics provider builder.
    // It is important to keep the MetricsProvider instance active throughout the process lifetime.
    // See https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs/metrics#meterprovider-management
    var metricsProvider = Sdk.CreateMeterProviderBuilder()
        .AddAzureMonitorMetricExporter();

    // Create a new logger factory.
    // It is important to keep the LoggerFactory instance active throughout the process lifetime.
    // See https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs/logs#logger-management
    var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddOpenTelemetry(options =>
        {
            options.AddAzureMonitorLogExporter();
        });
    });
    ```

We've written a detailed onboarding guide for this scenario here: [Enable Azure Monitor OpenTelemetry for .NET](https://learn.microsoft.com/azure/azure-monitor/app/opentelemetry-enable?tabs=net)
