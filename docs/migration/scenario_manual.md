# Manually Sending Telemetry

The Application Insights .NET SDK allowed users to manually create telemetry using the `TelemetryClient` and methods such as `TrackRequest()` or `TrackDependency()`. This section describes how to manually produce those telemetry types using OpenTelemetry.

## DataTypes

It's important to recognize that some of the datatypes shown in the Application Insights UX will have new types in the OpenTelemetry SDKs.

| Application Insights DataType | OpenTelemetry DataType               | .NET Implementation                  |
|-------------------------------|--------------------------------------|--------------------------------------|
| `Requests`                    | `Spans` (Server, Producer)           | System.Diagnostics.Activity          |
| `Dependency`                  | `Spans` (Client, Internal, Consumer) | System.Diagnostics.Activity          |
| `CustomMetrics`               | `Metrics`                            | System.Diagnostics.Metrics.Meter     |
| `Traces`                      | `Logs`                               | Microsoft.Extensions.Logging.ILogger |

Review these documents to learn more:
 - [Data Collection Basics of Azure Monitor Application Insights](https://learn.microsoft.com/azure/azure-monitor/app/opentelemetry-overview).
 - [Application Insights telemetry data model](https://learn.microsoft.com/azure/azure-monitor/app/data-model-complete)

## DependencyTelemetry

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

## RequestTelemetry

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

## CustomEvents

Not yet supported in OpenTelemetry.

Using Application Insights:
```csharp
TelemetryClient.TrackEvent()
```

## CustomMetrics

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

TODO: OTEL EXAMPLE. THIS COULD BE A SEPARATE DOC BY ITSELF!

## AvailabilityTelemetry

Not yet supported in OpenTelemetry.

Using Application Insights:
```
TelemetryClient.TrackAvailability()
```

## TraceTelemetry

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

## ExceptionTelemetry

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
