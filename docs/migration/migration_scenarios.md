
# Scenarios

**TODO: CONSIDER MERGING THESE TWO DOCUMENTS WHEN FINISHED**

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


### Manual configuration

Using Application Insights:
```csharp
var telemetryConfig = new TelemetryConfiguration();
var telemetryClient = new TelemetryClient(telemetryConfig);
```

Using OpenTelemetry:

1. Take a dependency on our package: https://www.nuget.org/packages/Azure.Monitor.OpenTelemetry.Exporter

2. Configure each signal and add the Azure Monitor Exporter. It's important to note that the instances of Providers and LoggerFactory should be kept active through the process lifetime.

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

## Manually Sending Telemetry

The Application Insights .NET SDK allowed users to manually create telemetry using the `TelemetryClient` and methods such as `TrackRequest()` or `TrackDependency()`. This section describes how to manually produce those telemetry types using OpenTelemetry.

### DependencyTelemetry

A Dependency represents a call from your app to an external service or storage, such as a REST API or SQL. OpenTelemetry models this as a Client Span.

Using Application Insights:
```
TelemetryClient.TrackDependency()
```

Using OpenTelemetry:
```csharp
using (var activity = activitySource.StartActivity("DependencyName", ActivityKind.Client))
{
    activity?.SetTag("customproperty", "custom value");
}
```

### RequestTelemetry

A Request represents an incomming request received by your app. OpenTelemetry models this as a Server Span.

Using Application Insights:
```
TelemetryClient.TrackRequest()
``` 

Using OpenTelemetry:
```csharp
using (var activity = activitySource.StartActivity("RequestName", ActivityKind.Server))
{
    activity?.SetTag("customproperty", "custom value");
}
```

### CustomEvents

Not yet supported in OpenTelemetry.

Using Application Insights:
```csharp
TelemetryClient.TrackEvent()
```

### CustomMetrics

Using Application Insights:
- To report an individual metric
    ```
    TelemetryClient.TrackMetric()
    ```
- To create pre-aggregated metrics
    ```
    TelemetryClient.GetMetric()
    ```

Using OpenTelemetry:

TODO: OTEL EXAMPLE

### AvailabilityTelemetry

Not yet supported in OpenTelemetry.

Using Application Insights:
```
TelemetryClient.TrackAvailability()
```

### TraceTelemetry

Trace telemetry in Application Insights represents some log statement.

Using Application Insights:
```
TelemetryClient.TrackTrace()
``` 

Using OpenTelemetry:

OpenTelemetly leverages .NET's ILogger.
Please review the best practices: 
- https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs/logs
- https://learn.microsoft.com/dotnet/core/extensions/logging

TODO: OTEL EXAMPLE

### ExceptionTelemetry

An Exception typically represents an exception that causes an operation to fail.

Using Application Insights:
```
TelemetryClient.TrackException()
```

Using OpenTelemetry:
- To log an Exception using an `Activity`:
    ```csharp
    using (var activity = activitySource.StartActivity("ExceptionExample"))
    {
        try
        {
            // Try to execute some code.
            throw new Exception("Test exception");
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            activity?.RecordException(ex);
        }
    }
    ```

- To log an Exception using `ILogger`:
    ```csharp
    var logger = loggerFactory.CreateLogger("logCategoryName");

    try
    {
        // Try to execute some code.
        throw new Exception("Test Exception");
    }
    catch (Exception ex)
    {
        logger.Log(
            logLevel: LogLevel.Error,
            eventId: 0,
            exception: ex,
            message: "Hello {name}.",
            args: new object[] { "World" });
    }
    ```

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
