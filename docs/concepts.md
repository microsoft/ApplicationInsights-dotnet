# Application Insights DotNet SDK Concepts
This lists the high level concepts of the AI DotNet SDK and links to detailed guides to help you get started.

## Connection String
The connection string identifies the application insights resource to send telemetry to. Read more [here](https://learn.microsoft.com/en-us/azure/azure-monitor/app/connection-strings).

## TelemetryClient
The `TelemetryClient` class provides methods for sending different types of telemetry. Read more [here](../BASE/README.md#using-telemetryclient).

## TelemetryConfiguration
TelemetryConfiguration provides a mechanism to configure certain settings on the telemetry client, such as:
- The connection string
- Sampling settings
- AAD authentication
- Offline storage

Read more [here](../BASE/README.md#configuration). 
In addition, these settings are configurable via applicationinsights.config and will soon be a part of the schema for appsettings.json as well.

## OpenTelemetry

### OpenTelemetry vocabulary
- Traces/Spans: See [this documentation](https://opentelemetry.io/docs/concepts/signals/traces/). This is not to be confused with Application Insights traces, which are analogous to OpenTelemetry logs in ApplicationInsights 3.x.
- ActivitySource: An `ActivitySource` is OpenTelemetry's mechanism for creating distributed trace spans (called `Activity` in .NET). Think of it as a factory that creates telemetry for a specific component or subsystem in your application. By default, OpenTelemetry only collects Activities from sources you explicitly register. When you create a custom `ActivitySource` in your code, you must register its name (or name pattern) so the SDK knows to collect telemetry from it.
- [Logs](https://opentelemetry.io/docs/concepts/signals/logs/)
- [Metrics](https://opentelemetry.io/docs/concepts/signals/metrics/)
- [Processors](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs/trace/extending-the-sdk#processor). These are meant to replace custom TelemetryInitializers/Modules & Filtering.
- Samplers: Allow you to configure the amount of telemetry you can send. In this SDK, we have not yet implented the ability to use [baked-in OpenTelemetry samplers](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs/trace/extending-the-sdk#sampler), though we provide settings via TelemetryConfiguration to use our Application Insights percentage based sampler or our custom rate-limited sampler.
- [Resource detectors](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/docs/resources/README.md)
- [OpenTelemetry Exporters](https://opentelemetry.io/docs/languages/dotnet/exporters/)

Application Insights 3.x is built on **OpenTelemetry**, an industry-standard observability framework. Understanding this foundation will help you make better decisions about when to use `TelemetryClient` versus native OpenTelemetry APIs.

### Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│  Your Application Code                                  │
├─────────────────────────────────────────────────────────┤
│  TelemetryClient API (Compatibility Layer)              │
│  ↓ Translates to OpenTelemetry primitives               │
├─────────────────────────────────────────────────────────┤
│  OpenTelemetry SDK                                      │
│  • Activity (Traces/Spans)                              │
│  • LogRecord (Logs)                                     │
│  • Metrics                                              │
│  • Resource Detectors (run at startup)                  │
├─────────────────────────────────────────────────────────┤
│  Activity Processors / Log Processors                   │
│  • Enrichment                                           │
│  • Filtering                                            │
│  • Sampling                                             │
├─────────────────────────────────────────────────────────┤
│  Azure Monitor Exporter                                 │
│  ↓ Converts to Application Insights schema              │
├─────────────────────────────────────────────────────────┤
│  Azure Monitor / Application Insights                   │
└─────────────────────────────────────────────────────────┘
```

**Key Mappings:**
- `TrackEvent()` → `LogRecord` with custom event marker
- `TrackDependency()` → `Activity` with `ActivityKind.Client` (outbound calls to external services)
- `TrackRequest()` → `Activity` with `ActivityKind.Server` (inbound requests)
- `TrackException()` → `LogRecord` with exception
- `TrackTrace()` → `LogRecord`
- `TrackMetric()` → OpenTelemetry Histogram

**When using OpenTelemetry Activity directly, choose the appropriate ActivityKind:**
- `ActivityKind.Client` - Outbound synchronous calls (HTTP requests, database queries, cache calls)
- `ActivityKind.Server` - Inbound synchronous requests (API endpoints, RPC handlers)
- `ActivityKind.Producer` - Outbound asynchronous messages (publishing to queue/topic)
- `ActivityKind.Consumer` - Inbound asynchronous messages (consuming from queue/topic)
- `ActivityKind.Internal` - Internal operations within your application (not crossing process boundaries)


### Custom Instrumentation

You can create custom distributed trace spans using OpenTelemetry's `ActivitySource`. This is useful for tracking operations within your application that aren't automatically instrumented.

> **Note:** Before using `ActivitySource`, make sure you understand the concept. See [Understanding ActivitySource](#understanding-activitysource) in the Configuration section.

**Example: Custom instrumentation for a data service**

```csharp
using System.Diagnostics;

public class DataService
{
    // 1. Create a static ActivitySource with a descriptive name
    private static readonly ActivitySource ActivitySource = new("MyApp.DataService");
    
    public async Task<User> GetUserAsync(string userId)
    {
        // 2. Start an Activity to track this operation
        using var activity = ActivitySource.StartActivity("GetUser");
        
        // 3. Add tags (attributes) to provide context
        activity?.SetTag("user.id", userId);
        activity?.SetTag("db.system", "postgresql");
        
        var user = await _database.GetUserAsync(userId);
        
        activity?.SetTag("user.found", user != null);
        
        return user;
    }
}
```

**Register the ActivitySource in TelemetryConfiguration:**

```csharp
configuration.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.WithTracing(tracing =>
    {
        // Register by exact name
        tracing.AddSource("MyApp.DataService");
    
        // Or use wildcards to register multiple sources at once
        tracing.AddSource("MyApp.*");
    });
});
```

**What gets sent to Application Insights?**

When you call `StartActivity()`, OpenTelemetry creates a span that:
- Appears as a **Dependency** in Application Insights (if started within a request context)
- Includes all tags as **Custom Properties**
- Captures duration automatically
- Links to parent operations for end-to-end tracing

**Naming Conventions:**

Use hierarchical naming for your ActivitySources to make wildcard registration easier:
- `MyCompany.OrderService` ✓
- `MyCompany.OrderService.Validation` ✓  
- `MyCompany.InventoryService` ✓

Then register with: `builder.WithTracing(tracing => tracing.AddSource("MyCompany.*"))`

### Advanced Scenarios

Let's explore advanced scenarios for enriching, filtering, and optimizing your telemetry collection. These techniques leverage OpenTelemetry's extensibility model to give you fine-grained control over what telemetry is collected and how it's processed.

#### Enriching Telemetry with Activity Processors

Activity Processors replace `ITelemetryInitializer` from version 2.x, for requests and dependencies. They enrich or modify telemetry before export:

```csharp
using System.Diagnostics;
using OpenTelemetry;

public class CustomEnrichmentProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Add custom tags to all activities
        activity?.SetTag("app.environment", "Production");
        activity?.SetTag("app.version", "1.2.3");
        
        // Conditionally enrich
        if (activity?.OperationName == "ProcessOrder")
        {
            activity?.SetTag("business.critical", "true");
        }
        
        // Add resource information
        activity?.SetTag("host.name", Environment.MachineName);
    }
}
```

##### Register the processor:

**For the base and web packages**
```csharp
configuration.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.WithTracing(tracing => tracing.AddProcessor<CustomEnrichmentProcessor>());
});
```

**For AspNetCore & WorkerService**
```csharp
builder.Services.ConfigureOpenTelemetryTracerProvider((sp, tracerBuilder) =>
{
    tracerBuilder.AddProcessor(new CustomEnrichmentProcessor());
});
```

> **Note:** The generic `AddProcessor<T>()` overload cannot be used inside `ConfigureOpenTelemetryTracerProvider` because the `ServiceProvider` has already been created at that point. Use the instance-based `AddProcessor(new T())` form instead.

#### Filtering Telemetry with Activity Processors

Filter out unwanted telemetry to reduce volume and costs:

```csharp
public class FilteringProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Filter out health check requests
        if (activity?.DisplayName?.Contains("/health") == true)
        {
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
            return;
        }
        
        // Filter out specific dependencies
        if (activity?.Kind == ActivityKind.Client && 
            activity?.GetTagItem("http.url")?.ToString()?.Contains("internal-service") == true)
        {
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
        }
    }
}
```

##### Register the processor:

**For the base and web packages**
```csharp
configuration.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.WithTracing(tracing => tracing.AddProcessor<FilteringProcessor>());
});
```

**For AspNetCore & WorkerService**
```csharp
builder.Services.ConfigureOpenTelemetryTracerProvider((sp, tracerBuilder) =>
{
    tracerBuilder.AddProcessor(new FilteringProcessor());
});
```

#### Log Processors
It is also possible to add enrichment or filtering for log-based telemetry (exceptions, Application Insights traces, custom events) via log processors:

```csharp
using OpenTelemetry;
using OpenTelemetry.Logs;

public class CustomLogProcessor : BaseProcessor<LogRecord>
{
    public override void OnEnd(LogRecord logRecord)
        {
            var attributes = new List<KeyValuePair<string, object?>>
            {
                new("app.environment", "Production"),
            };

            if (logRecord.Attributes != null)
            {
                attributes.AddRange(logRecord.Attributes);
            }

            logRecord.Attributes = attributes;
        }
}
```

##### Register the processor:

**For the base and web packages**
```csharp
configuration.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.WithLogging(logging => logging.AddProcessor<CustomLogProcessor>());
});
```

**For AspNetCore & WorkerService**
```csharp
builder.Services.ConfigureOpenTelemetryLoggerProvider((sp, loggerBuilder) =>
{
    loggerBuilder.AddProcessor(new CustomLogProcessor());
});
```

#### Resource Detectors

Resource Detectors can add contextual information which flow to an `_APPRESOURCEPREVIEW_` metric:

```csharp
using OpenTelemetry.Resources;

public class CustomResourceDetector : IResourceDetector
{
    public Resource Detect()
    {
        var attributes = new Dictionary<string, object>
        {
            { "service.name", "OrderProcessingService" },
            { "service.version", "1.2.3" },
            { "deployment.environment", "Production" },
            { "service.instance.id", Environment.MachineName },
            { "custom.datacenter", "WestUS" }
        };
        
        return new Resource(attributes);
    }
}
```

Resource detectors are typically used to enrich telemetry by detecting the specific environment an application is running in - as an example, this SDK internally implements its own resource detector that determines whether it is an aspnetcore application.

**Register the detector:**

```csharp
configuration.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.ConfigureResource(resourceBuilder =>
    {
        resourceBuilder.AddDetector(new CustomResourceDetector());
    });
});
```
---


## Disabling Telemetry

You can disable all telemetry collection using the `DisableTelemetry` property:

```C#
var configuration = TelemetryConfiguration.CreateDefault();
configuration.ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000";
configuration.DisableTelemetry = true;
var tc = new TelemetryClient(configuration);
```

When `DisableTelemetry` is set to `true`, the SDK internally sets the `OTEL_SDK_DISABLED` environment variable to `true` before building the OpenTelemetry SDK. This causes the OpenTelemetry SDK (version 1.15.0+) to return no-op implementations for all telemetry signals, preventing any telemetry data from being collected or exported.

**Note:** The `DisableTelemetry` property must be set before the first `TelemetryClient` is created, as the OpenTelemetry SDK is built at that time.

### Disabling Telemetry in DI Scenarios (ASP.NET Core / Worker Service)

For dependency injection scenarios, you must configure `DisableTelemetry` **before** calling `AddApplicationInsightsTelemetry()`:

```C#
public void ConfigureServices(IServiceCollection services)
{
    // Configure DisableTelemetry BEFORE AddApplicationInsightsTelemetry
    services.Configure<TelemetryConfiguration>(tc => tc.DisableTelemetry = true);

    // Add and initialize the Application Insights SDK
    services.AddApplicationInsightsTelemetry();
}
```

This ensures the `OTEL_SDK_DISABLED` environment variable is set before the OpenTelemetry SDK is initialized.