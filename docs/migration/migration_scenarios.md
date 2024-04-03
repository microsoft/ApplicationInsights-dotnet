
# Scenarios

TODO: REMOVE THIS DOC. MOVE SUBJECTS TO INDIVIDUAL DOCS, SMALLER TO DIGEST

This doc captures specific scenarios that Application Insights .NET SDK users may have and provides instructions to migrate their applications to OpenTelemetry and the Azure Monitor Exporter.

> NOTE:
> The Azure Monitor .NET Exporter does not yet have full feature parity with the classic Application Insights SDK.

## Onboarding

The Application Insights .NET SDK had multiple ways to get started and required you to select a TelemetryChannel (either `InMemoryChannel` or `ServerTelemetryChannel`) which provided different features.

For OpenTelemetry, each of the 3 telemetry signals (Traces, Metrics, and Logs) have a unique configuration. Instead of TelemetryChannels, you'll configure an Azure Monitor Exporter for each telemetry signal. We provide a one-line extension method for AspNetCore that configures all 3 signals at once. You can also manually configure each signal individually.

### AspNetCore Configuration

Prerequisite: using ASP.NET Core, follow these steps.

Using Application Insights:
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationInsightsTelemetry();
```

Using OpenTelemetry:

1. Take a dependency on our package: https://www.nuget.org/packages/Azure.Monitor.OpenTelemetry.AspNetCore

2. Invoke `UseAzureMonitor()` to configure an Azure Monitor Exporter for all three signals
    ```csharp
    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddOpenTelemetry().UseAzureMonitor();
    ```

We've written a detailed onboarding guide for this scenario here: [Enable Azure Monitor OpenTelemetry for ASP.NET Core](https://learn.microsoft.com/azure/azure-monitor/app/opentelemetry-enable?tabs=aspnetcore)

## Automatically Collecting Telemetry

TODO: INSTRUMENTATION LIBRARIES

## Enriching Telemetry

TODO: ITELEMETRYINITIALIZER

https://docs.microsoft.com/azure/azure-monitor/app/api-filtering-sampling

## Filtering

TODO: TELEMETRYPROCESSOR

https://docs.microsoft.com/azure/azure-monitor/app/api-filtering-sampling

## Correlation

TODO

https://docs.microsoft.com/azure/azure-monitor/app/correlation

## Sampling

TODO

https://docs.microsoft.com/azure/azure-monitor/app/sampling
