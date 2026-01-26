# Application Insights for ASP.NET Core

The Microsoft Application Insights for ASP.NET Core SDK sends telemetry data to Azure Monitor following the OpenTelemetry Specification. This library can be used to instrument your ASP.NET Core applications to collect and send telemetry data to Azure Monitor for analysis and monitoring, powering experiences in Application Insights.

> **Note**: For Worker Services, background services, and console applications, see [Worker Service documentation](WorkerService.md).

## NuGet Packages

- **[Microsoft.ApplicationInsights.AspNetCore](https://www.nuget.org/packages/Microsoft.ApplicationInsights.AspNetCore/)**
[![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.AspNetCore.svg)](https://nuget.org/packages/Microsoft.ApplicationInsights.AspNetCore)

- **[Microsoft.ApplicationInsights.WorkerService](https://www.nuget.org/packages/Microsoft.ApplicationInsights.WorkerService/)** - For Worker Services and console apps, see [Worker Service documentation](WorkerService.md)
[![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.WorkerService.svg)](https://nuget.org/packages/Microsoft.ApplicationInsights.WorkerService)

## Getting Started

### Prerequisites

- **Azure Subscription:** To use Azure services, including Azure Monitor, you'll need a subscription. If you do not have an existing Azure account, you may sign up for a [free trial](https://azure.microsoft.com/free/dotnet/) or use your [Visual Studio Subscription](https://visualstudio.microsoft.com/subscriptions/) benefits when you [create an account](https://azure.microsoft.com/account).

- **Azure Application Insights Connection String:** To send telemetry data to the monitoring service you'll need a connection string from Azure Application Insights. If you are not familiar with creating Azure resources, you may wish to follow the step-by-step guide for [Create an Application Insights resource](https://learn.microsoft.com/azure/azure-monitor/app/create-new-resource) and [copy the connection string](https://learn.microsoft.com/azure/azure-monitor/app/sdk-connection-string?tabs=net#find-your-connection-string).

- **ASP.NET Core App:** An ASP.NET Core application is required to instrument it with Application Insights. You can either bring your own app or follow the [Get started with ASP.NET Core MVC](https://learn.microsoft.com/aspnet/core/tutorials/first-mvc-app/start-mvc) to create a new one.

### What is Included

The Microsoft.ApplicationInsights.AspNetCore package is built on top of OpenTelemetry and includes:

#### Traces
- **ASP.NET Core Instrumentation**: Provides automatic tracing for incoming HTTP requests to ASP.NET Core applications.
- **HTTP Client Instrumentation**: Provides automatic tracing for outgoing HTTP requests made using [System.Net.Http.HttpClient](https://learn.microsoft.com/dotnet/api/system.net.http.httpclient).
- **SQL Client Instrumentation**: Provides automatic tracing for SQL queries executed using the [Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient) and [System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient) packages.

#### Metrics
- **Application Insights Standard Metrics**: Provides automatic collection of Application Insights Standard metrics.
- **ASP.NET Core and HTTP Client Metrics Instrumentation**: Metrics collection is selective based on the .NET runtime version:
  - **.NET 8.0 and above**: Utilizes built-in Metrics `Microsoft.AspNetCore.Hosting` and `System.Net.Http` from .NET. For a detailed list of metrics produced, refer to the [Microsoft.AspNetCore.Hosting](https://learn.microsoft.com/dotnet/core/diagnostics/built-in-metrics-aspnetcore#microsoftaspnetcorehosting) and [System.Net.Http](https://learn.microsoft.com/dotnet/core/diagnostics/built-in-metrics-system-net#systemnethttp) metrics documentation.
  - **.NET 7.0 and below**: Falls back to ASP.NET Core Instrumentation and HTTP Client Instrumentation. For a detailed list of metrics produced, refer to the [ASP.NET Core Instrumentation](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.AspNetCore/README.md#list-of-metrics-produced) and [HTTP Client Instrumentation](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.Http/README.md#list-of-metrics-produced) documentation.

#### Logs
- Logs created with `Microsoft.Extensions.Logging`. See [Logging in .NET Core and ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/logging) for more details on how to create and configure logging.
- [Azure SDK logs](https://learn.microsoft.com/dotnet/azure/sdk/logging) are recorded as a subset of `Microsoft.Extensions.Logging`.

#### Resource Detectors
- **Azure App Service Resource Detector**: Adds resource attributes for applications running in Azure App Service.
- **Azure VM Resource Detector**: Adds resource attributes for applications running in an Azure Virtual Machine.
- **Azure Container Apps Resource Detector**: Adds resource attributes for applications running in Azure Container Apps.
- **ASP.NET Core Environment Resource Detector**: Adds resource attributes from the ASP.NET Core environment configuration.

> **Note**: Resource attributes are used to set the cloud role and role instance. Most other resource attributes are currently ignored.

#### Live Metrics
- Integrated support for [Live Metrics](https://learn.microsoft.com/azure/azure-monitor/app/live-stream) enabling real-time monitoring of application performance.

#### Azure Monitor Exporter
- Uses the [Azure Monitor OpenTelemetry Exporter](https://www.nuget.org/packages/Azure.Monitor.OpenTelemetry.Exporter/) to send traces, metrics, and logs data to Azure Monitor.

### Migrating from Application Insights SDK 2.x?

If you are currently using the Application Insights SDK 2.x and want to migrate to the OpenTelemetry-based 3.x version, please see the [Migration Guidance](#migration-from-2x-to-3x) section below.

### Install the Package

Install the Microsoft.ApplicationInsights.AspNetCore package from [NuGet](https://www.nuget.org/):

```dotnetcli
dotnet add package Microsoft.ApplicationInsights.AspNetCore
```

### Enabling Application Insights in Your Application

The following examples demonstrate how to integrate Application Insights into your ASP.NET Core application.

#### Example 1: Using Environment Variable for Connection String

To enable Application Insights, add `AddApplicationInsightsTelemetry()` to your `Program.cs` file and set the `APPLICATIONINSIGHTS_CONNECTION_STRING` environment variable to the connection string from your Application Insights resource.

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Application Insights telemetry
builder.Services.AddApplicationInsightsTelemetry();

// Add other services for your application
builder.Services.AddMvc();

var app = builder.Build();
```

#### Example 2: Using Connection String in Code

To enable Application Insights with a connection string specified in code, add `AddApplicationInsightsTelemetry()` with the `ApplicationInsightsServiceOptions` containing the connection string.

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Application Insights telemetry with connection string
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://...";
});

// Add other services for your application
builder.Services.AddMvc();

var app = builder.Build();
```

#### Example 3: Using Configuration

To enable Application Insights using configuration from `appsettings.json`, add `AddApplicationInsightsTelemetry()` with `IConfiguration`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Application Insights telemetry from configuration
builder.Services.AddApplicationInsightsTelemetry(builder.Configuration);

// Add other services for your application
builder.Services.AddMvc();

var app = builder.Build();
```

In your `appsettings.json`:

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://..."
  }
}
```

> **Note**: Multiple calls to `AddApplicationInsightsTelemetry()` will not result in multiple providers. Only a single `TracerProvider`, `MeterProvider`, and `LoggerProvider` will be created.


## Configuration

### Connection String

The connection string can be configured in multiple ways (listed in order of precedence):

1. `ApplicationInsightsServiceOptions.ConnectionString` property set in code
2. `APPLICATIONINSIGHTS_CONNECTION_STRING` environment variable
3. `ApplicationInsights:ConnectionString` configuration setting

> **Important**: Version 3.x uses **ConnectionString** instead of InstrumentationKey. InstrumentationKey-based configuration is deprecated.

### ApplicationInsightsServiceOptions

The following options are available when configuring Application Insights in version 3.x:

```csharp
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    // Connection string for the Application Insights resource
    options.ConnectionString = "InstrumentationKey=...";
    
    // Enable or disable Live Metrics (default: true)
    options.EnableQuickPulseMetricStream = true;
});
```

> **Note**: In version 3.x, many properties from `ApplicationInsightsServiceOptions` in 2.x are no longer functional because they related to telemetry modules, processors, and channels that have been replaced by OpenTelemetry components. The properties shown above are the ones that are actively used in the 3.x OpenTelemetry-based implementation.

## Advanced Configuration

### Using TelemetryClient for Custom Telemetry

You can inject `TelemetryClient` to send custom telemetry:

```csharp
public class MyController : Controller
{
    private readonly TelemetryClient _telemetryClient;
    
    public MyController(TelemetryClient telemetryClient)
    {
        _telemetryClient = telemetryClient;
    }
    
    public IActionResult Index()
    {
        // Track custom event
        _telemetryClient.TrackEvent("MyCustomEvent");
        
        // Track custom metric
        _telemetryClient.TrackMetric("MyMetric", 42);
        
        // Track custom trace
        _telemetryClient.TrackTrace("My trace message");
        
        return View();
    }
}
```

> **Note**: In version 3.x, `TelemetryClient` is a shim layer that translates Application Insights API calls to OpenTelemetry signals. For new code, consider using OpenTelemetry APIs directly.

### OpenTelemetry Extensibility

The 3.x SDK is built on OpenTelemetry, allowing you to extend and customize telemetry collection using OpenTelemetry APIs.

#### Adding Custom ActivitySource to Traces

```csharp
using System.Diagnostics;

// Create an ActivitySource
private static readonly ActivitySource MyActivitySource = 
    new ActivitySource("MyCompany.MyProduct.MyLibrary");

// Use it to create activities
using var activity = MyActivitySource.StartActivity("OperationName");
activity?.SetTag("key", "value");

// Register it with OpenTelemetry
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.ConfigureOpenTelemetryTracerProvider((sp, builder) => 
{
    builder.AddSource("MyCompany.MyProduct.MyLibrary");
});
```

#### Adding Custom Meter to Metrics

```csharp
using System.Diagnostics.Metrics;

// Create a Meter and instruments
private static readonly Meter MyMeter = new Meter("MyCompany.MyProduct.MyLibrary");
private static readonly Counter<int> MyCounter = MyMeter.CreateCounter<int>("my_counter");

// Use it to record metrics
MyCounter.Add(1, new KeyValuePair<string, object?>("key", "value"));

// Register it with OpenTelemetry
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.ConfigureOpenTelemetryMeterProvider((sp, builder) => 
{
    builder.AddMeter("MyCompany.MyProduct.MyLibrary");
});
```

#### Adding Additional Instrumentation

If you need to instrument a library or framework that isn't included by default, you can add additional instrumentation using OpenTelemetry Instrumentation packages. For example, to add instrumentation for gRPC clients:

```csharp
// Install: dotnet add package OpenTelemetry.Instrumentation.GrpcNetClient

builder.Services.AddApplicationInsightsTelemetry();

builder.Services.ConfigureOpenTelemetryTracerProvider((sp, builder) => 
{
    builder.AddGrpcClientInstrumentation();
});
```

#### Customizing ASP.NET Core Instrumentation

```csharp
using OpenTelemetry.Instrumentation.AspNetCore;

builder.Services.AddApplicationInsightsTelemetry();

builder.Services.Configure<AspNetCoreTraceInstrumentationOptions>(options =>
{
    options.RecordException = true;
    options.Filter = (httpContext) =>
    {
        // Only collect telemetry about HTTP GET requests
        return HttpMethods.IsGet(httpContext.Request.Method);
    };
});
```

#### Customizing HTTP Client Instrumentation

```csharp
using OpenTelemetry.Instrumentation.Http;

builder.Services.AddApplicationInsightsTelemetry();

builder.Services.Configure<HttpClientTraceInstrumentationOptions>(options =>
{
    options.RecordException = true;
    options.FilterHttpRequestMessage = (httpRequestMessage) =>
    {
        // Only collect telemetry about HTTP GET requests
        return HttpMethods.IsGet(httpRequestMessage.Method.Method);
    };
});
```

#### Customizing SQL Client Instrumentation

```csharp
// Install: dotnet add package --prerelease OpenTelemetry.Instrumentation.SqlClient

using OpenTelemetry.Instrumentation.SqlClient;

builder.Services.AddApplicationInsightsTelemetry();

builder.Services.ConfigureOpenTelemetryTracerProvider((sp, builder) => 
{
    builder.AddSqlClientInstrumentation(options =>
    {
        options.RecordException = true;
    });
});
```

#### Adding Custom Resource

To modify the resource attributes:

```csharp
using OpenTelemetry.Resources;

builder.Services.AddApplicationInsightsTelemetry();

builder.Services.ConfigureOpenTelemetryTracerProvider((sp, builder) => 
{
    builder.ConfigureResource(resourceBuilder => 
        resourceBuilder.AddService("my-service-name", "my-service-namespace", "1.0.0")
    );
});
```

You can also configure the Resource using environment variables:

| Environment variable       | Description                                        |
| -------------------------- | -------------------------------------------------- |
| `OTEL_RESOURCE_ATTRIBUTES` | Key-value pairs to be used as resource attributes. See the [Resource SDK specification](https://github.com/open-telemetry/opentelemetry-specification/blob/v1.5.0/specification/resource/sdk.md#specifying-resource-information-via-an-environment-variable) for more details. |
| `OTEL_SERVICE_NAME`        | Sets the value of the `service.name` resource attribute. If `service.name` is also provided in `OTEL_RESOURCE_ATTRIBUTES`, then `OTEL_SERVICE_NAME` takes precedence. |

#### Adding Another Exporter

Application Insights uses the Azure Monitor exporter to send data to Application Insights. However, if you need to send data to other services, you can add another exporter:

```csharp
// Install: dotnet add package OpenTelemetry.Exporter.Console

builder.Services.AddApplicationInsightsTelemetry();

builder.Services.ConfigureOpenTelemetryTracerProvider((sp, builder) => 
{
    builder.AddConsoleExporter();
});
```

#### Disabling Live Metrics

By default, Live Metrics is enabled. To disable it:

```csharp
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.EnableQuickPulseMetricStream = false;
});
```

#### Dropping Specific Metrics Instruments

To exclude specific instruments from being collected:

```csharp
using OpenTelemetry.Metrics;

builder.Services.ConfigureOpenTelemetryMeterProvider((sp, builder) =>
{
    builder.AddView(
        instrumentName: "http.server.active_requests", 
        MetricStreamConfiguration.Drop
    );
});
```

Refer to [Drop an instrument](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs/metrics/customizing-the-sdk#drop-an-instrument) for more examples.

### Log Scopes

Log [scopes](https://learn.microsoft.com/dotnet/core/extensions/logging#log-scopes) allow you to add additional properties to the logs generated by your application. Although Application Insights supports scopes, this feature is off by default in OpenTelemetry. To leverage log scopes, you must explicitly enable them.

To include scopes with your logs:

```csharp
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;

builder.Services.Configure<OpenTelemetryLoggerOptions>(loggingOptions =>
{
    loggingOptions.IncludeScopes = true;
});
```

When using `ILogger` scopes, use a `List<KeyValuePair<string, object?>>` or `IReadOnlyList<KeyValuePair<string, object?>>` as the state for best performance. All logs written within the context of the scope will include the specified information. Azure Monitor will add these scope values to the Log's CustomProperties.

```csharp
List<KeyValuePair<string, object?>> scope =
[
    new("scopeKey", "scopeValue")
];

using (logger.BeginScope(scope))
{
    logger.LogInformation("Example message.");
}
```

## Migration from 2.x to 3.x

Version 3.x of the Application Insights SDK represents a significant architectural change. The SDK is now built on top of OpenTelemetry, which means many of the Application Insights-specific concepts and APIs have been replaced with OpenTelemetry equivalents.

### Breaking Changes

#### What Has Been Removed

The following features are **no longer available** in version 3.x:

1. **Telemetry Modules** - All telemetry modules have been removed. Telemetry collection is now handled by OpenTelemetry instrumentation libraries.

2. **Telemetry Initializers** - Telemetry initializers are no longer supported. Use OpenTelemetry processors or resource detectors instead.

3. **Telemetry Processors** - Custom telemetry processors are not supported. Use OpenTelemetry processors.

4. **ITelemetryChannel** - Custom telemetry channels are not supported. The Azure Monitor OpenTelemetry Exporter handles telemetry transmission.

5. **TelemetryConfiguration Builder Pattern** - Direct configuration of `TelemetryConfiguration` through builder pattern is no longer supported. Use `ApplicationInsightsServiceOptions` and OpenTelemetry configuration APIs.

6. **RequestCollectionOptions properties** - Most properties in `RequestCollectionOptions` have been removed. Use OpenTelemetry instrumentation options instead (e.g., `AspNetCoreTraceInstrumentationOptions`).

7. **InstrumentationKey** - Configuration via InstrumentationKey is deprecated. Use ConnectionString instead.

8. **PerformanceCollectorModule** - Performance counter collection works differently in 3.x. Set `EnablePerformanceCounterCollectionModule = true` in options.

9. **AutoCollectMetricExtractor** - Metric extraction has changed. Use `AddAutoCollectedMetricExtractor` option.

10. **JavaScript Snippet Injection** - The way JavaScript snippet is injected has changed. Use `IJavaScriptSnippet` service.

#### What Has Changed

1. **AddApplicationInsightsTelemetry()** - Still the primary way to add Application Insights, but now configures OpenTelemetry under the hood.

2. **TelemetryClient** - Still available for backward compatibility, but is now a shim layer over OpenTelemetry APIs. For new code, consider using OpenTelemetry APIs directly (`ActivitySource`, `Meter`, `ILogger`).

3. **Configuration** - Connection strings are now configured via `ApplicationInsightsServiceOptions.ConnectionString` or `APPLICATIONINSIGHTS_CONNECTION_STRING` environment variable.

5. **Live Metrics** - Still supported via the `EnableQuickPulseMetricStream` option.

### Migration Steps

#### Step 1: Update Package Reference

Update your package reference to version 3.x:

```xml
<PackageReference Include="Microsoft.ApplicationInsights.AspNetCore" Version="3.*" />
```

#### Step 2: Update Connection String Configuration

Replace InstrumentationKey with ConnectionString:

**Before (2.x):**
```csharp
services.AddApplicationInsightsTelemetry(options =>
{
    options.InstrumentationKey = "00000000-0000-0000-0000-000000000000";
});
```

**After (3.x):**
```csharp
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://...";
});
```

#### Step 3: Replace Telemetry Initializers and Processors with OpenTelemetry Processors

**Important Difference:** In 2.x, ITelemetryInitializer and ITelemetryProcessor applied to **all telemetry types** (requests, dependencies, traces, events, metrics, exceptions). In 3.x, OpenTelemetry uses **separate mechanisms for different signal types**:

- **Activity Processors** (`BaseProcessor<Activity>`) - Apply to traces (requests, dependencies, and custom activities)
- **Log Processors** (`BaseProcessor<LogRecord>`) - Apply to logs (traces, events, exceptions logged via ILogger)
- **Metric Views** - Transform/filter metrics (OpenTelemetry doesn't use processors for metrics; use `AddView()` instead)

Here are common migration patterns:

**Example: Adding Custom Properties to All Telemetry**

**Before (2.x) - Using ITelemetryInitializer:**
```csharp
public class MyTelemetryInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.GlobalProperties["Environment"] = "Production";
        telemetry.Context.GlobalProperties["CustomerId"] = GetCustomerId();
    }
}

// Registration
services.AddApplicationInsightsTelemetry();
services.AddSingleton<ITelemetryInitializer, MyTelemetryInitializer>();
```

**After (3.x) - Using OpenTelemetry Processor:**
```csharp
using System.Diagnostics;
using OpenTelemetry;

public class MyActivityProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        activity.SetTag("Environment", "Production");
        activity.SetTag("CustomerId", GetCustomerId());
    }
    
    private string GetCustomerId()
    {
        // Your logic here
        return "12345";
    }
}

// Registration
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.ConfigureOpenTelemetryTracerProvider((sp, builder) =>
{
    builder.AddProcessor<MyActivityProcessor>();
});
```

**Example: Filtering Telemetry**

**Before (2.x) - Using ITelemetryProcessor:**
```csharp
public class MyTelemetryProcessor : ITelemetryProcessor
{
    private ITelemetryProcessor Next { get; set; }

    public MyTelemetryProcessor(ITelemetryProcessor next)
    {
        Next = next;
    }

    public void Process(ITelemetry item)
    {
        // Filter out requests to health check endpoint
        if (item is RequestTelemetry request && 
            request.Url.AbsolutePath.Contains("/health"))
        {
            return; // Don't send
        }
        
        Next.Process(item);
    }
}

// Registration
services.Configure<TelemetryConfiguration>(config =>
{
    var builder = config.DefaultTelemetrySink.TelemetryProcessorChainBuilder;
    builder.Use(next => new MyTelemetryProcessor(next));
    builder.Build();
});
```

**After (3.x) - Using OpenTelemetry Processor:**
```csharp
using System.Diagnostics;
using OpenTelemetry;

public class FilteringActivityProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Filter out requests to health check endpoint
        if (activity.DisplayName.Contains("/health") || 
            activity.GetTagItem("url.path")?.ToString()?.Contains("/health") == true)
        {
            // Mark as not recorded to prevent export
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
        }
    }
}

// Registration
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.ConfigureOpenTelemetryTracerProvider((sp, builder) =>
{
    builder.AddProcessor<FilteringActivityProcessor>();
});
```

> **Note**: For filtering, also consider using instrumentation options like `AspNetCoreTraceInstrumentationOptions.Filter` for better performance.

**Example: Enriching with User Context**

**Before (2.x) - Using ITelemetryInitializer:**
```csharp
public class UserTelemetryInitializer : ITelemetryInitializer
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Initialize(ITelemetry telemetry)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context?.User?.Identity?.IsAuthenticated == true)
        {
            telemetry.Context.User.AuthenticatedUserId = context.User.Identity.Name;
        }
    }
}
```

**After (3.x) - Using OpenTelemetry Processor:**
```csharp
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using OpenTelemetry;

public class UserActivityProcessor : BaseProcessor<Activity>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserActivityProcessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override void OnStart(Activity activity)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context?.User?.Identity?.IsAuthenticated == true)
        {
            activity.SetTag("enduser.id", context.User.Identity.Name);
        }
    }
}

// Registration (processor with dependencies)
builder.Services.AddHttpContextAccessor();
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.ConfigureOpenTelemetryTracerProvider((sp, builder) =>
{
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    builder.AddProcessor(new UserActivityProcessor(httpContextAccessor));
});
```

**Example: Processing Logs (Traces, Events, Exceptions)**

**Before (2.x) - Using ITelemetryInitializer (applies to all types):**
```csharp
public class MyTelemetryInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        // Applied to requests, dependencies, traces, events, exceptions, etc.
        telemetry.Context.GlobalProperties["Version"] = "1.0.0";
        
        if (telemetry is TraceTelemetry trace)
        {
            trace.Properties["ProcessedBy"] = "MyInitializer";
        }
        
        if (telemetry is EventTelemetry evt)
        {
            evt.Properties["ProcessedBy"] = "MyInitializer";
        }
    }
}
```

**After (3.x) - Using Enrichment at Log Time:**
```csharp
using Microsoft.Extensions.Logging;

// Define compile-time logging methods with structured properties
internal static partial class LoggerExtensions
{
    [LoggerMessage(LogLevel.Information, "Processing request for CustomerId: `{customerId}`, Version: `{version}`")]
    public static partial void LogProcessingRequest(this ILogger logger, string customerId, string version);
}

// Usage
_logger.LogProcessingRequest(GetCustomerId(), "1.0.0");
```

**Alternative - Using Log Scopes for Context:**
```csharp
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;

// Enable scopes
builder.Services.Configure<OpenTelemetryLoggerOptions>(options =>
{
    options.IncludeScopes = true;
});

// Use scopes to add context to all logs
List<KeyValuePair<string, object?>> scope =
[
    new("Version", "1.0.0"),
    new("ProcessedBy", "MyLogProcessor")
];

// Define logging methods
internal static partial class LoggerExtensions
{
    [LoggerMessage(LogLevel.Information, "This log will include scope properties")]
    public static partial void LogWithScopeInfo(this ILogger logger);
    
    [LoggerMessage(LogLevel.Warning, "This log will also include scope properties")]
    public static partial void LogWithScopeWarning(this ILogger logger);
}

// Usage
using (_logger.BeginScope(scope))
{
    _logger.LogWithScopeInfo();
    _logger.LogWithScopeWarning();
}
```

> **Migration Tip**: If your 2.x initializer/processor handled multiple telemetry types, you'll need to create separate processors in 3.x:
> - Activity Processor for Request/Dependency telemetry
> - Log Processor for Trace/Event/Exception telemetry (logged via ILogger)
> - Use TelemetryClient shim for backward compatibility with TrackEvent(), TrackTrace(), TrackException()
```

#### Step 4: Replace RequestCollectionOptions

**Before (2.x):**
```csharp
services.AddApplicationInsightsTelemetry(options =>
{
    options.RequestCollectionOptions.TrackExceptions = true;
    options.RequestCollectionOptions.InjectResponseHeaders = true;
});
```

**After (3.x):**

Use OpenTelemetry instrumentation options:

```csharp
builder.Services.AddApplicationInsightsTelemetry();

builder.Services.Configure<AspNetCoreTraceInstrumentationOptions>(options =>
{
    options.RecordException = true;
    options.Filter = (httpContext) => 
    {
        // Custom filtering logic
        return true;
    };
});
```

#### Step 5: Update Custom Telemetry Code

For new code, consider using OpenTelemetry APIs instead of TelemetryClient:

**Custom Traces:**

**Before (2.x):**
```csharp
_telemetryClient.TrackTrace("My trace message");
```

**After (3.x) - Using TelemetryClient (backward compatible):**
```csharp
_telemetryClient.TrackTrace("My trace message");
```

**After (3.x) - Using OpenTelemetry (recommended for new code):**
```csharp
// Define compile-time logging method
internal static partial class LoggerExtensions
{
    [LoggerMessage(LogLevel.Information, "My trace message")]
    public static partial void LogMyTrace(this ILogger logger);
}

// Usage
_logger.LogMyTrace();
```

**Custom Metrics:**

**Before (2.x):**
```csharp
_telemetryClient.TrackMetric("MyMetric", 42);
```

**After (3.x) - Using TelemetryClient (backward compatible):**
```csharp
_telemetryClient.TrackMetric("MyMetric", 42);
```

**After (3.x) - Using OpenTelemetry (recommended for new code):**
```csharp
// Create a Meter
private static readonly Meter MyMeter = new Meter("MyCompany.MyProduct");
private static readonly Counter<int> MyCounter = MyMeter.CreateCounter<int>("MyMetric");

// Use it
MyCounter.Add(42);
```

**Custom Activities/Spans:**

**Before (2.x):**
```csharp
var operation = _telemetryClient.StartOperation<DependencyTelemetry>("MyOperation");
try
{
    // Do work
}
finally
{
    _telemetryClient.StopOperation(operation);
}
```

**After (3.x) - Using TelemetryClient (backward compatible):**
```csharp
var operation = _telemetryClient.StartOperation<DependencyTelemetry>("MyOperation");
try
{
    // Do work
}
finally
{
    _telemetryClient.StopOperation(operation);
}
```

**After (3.x) - Using OpenTelemetry (recommended for new code):**
```csharp
// Create an ActivitySource
private static readonly ActivitySource MyActivitySource = new ActivitySource("MyCompany.MyProduct");

// Register it with OpenTelemetry
builder.Services.ConfigureOpenTelemetryTracerProvider((sp, builder) =>
{
    builder.AddSource("MyCompany.MyProduct");
});

// Use it
using var activity = MyActivitySource.StartActivity("MyOperation");
// Do work
```

#### Step 6: Test Thoroughly

After migrating, thoroughly test your application to ensure:
- Telemetry is being collected and sent to Azure Monitor
- Custom telemetry is working as expected
- Performance is acceptable
- All required telemetry signals (traces, metrics, logs) are being captured

### Migration Checklist

- [ ] Update package reference to 3.x
- [ ] Replace InstrumentationKey with ConnectionString
- [ ] Remove all ITelemetryInitializer implementations
- [ ] Remove all ITelemetryProcessor implementations
- [ ] Remove all custom ITelemetryModule implementations
- [ ] Replace RequestCollectionOptions with AspNetCoreTraceInstrumentationOptions
- [ ] Test all custom telemetry collection
- [ ] Verify Live Metrics is working (if enabled)
- [ ] Verify all expected telemetry appears in Azure Monitor
- [ ] Consider migrating custom telemetry to OpenTelemetry APIs for new code

## Troubleshooting

### Self-Diagnostics

OpenTelemetry provides a [self-diagnostics feature](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry/README.md#troubleshooting) to collect internal logs.

To enable self-diagnostics, create a configuration file at your application's directory:

**OTEL_DIAGNOSTICS.json**
```json
{
    "LogDirectory": ".",
    "FileSize": 1024,
    "LogLevel": "Warning"
}
```

### Azure SDK Instrumentation

Azure SDK instrumentation is supported under the experimental feature flag which can be enabled using one of the following ways:

* Set the `AZURE_EXPERIMENTAL_ENABLE_ACTIVITY_SOURCE` environment variable to `true`.

* Set the Azure.Experimental.EnableActivitySource context switch to true in your app's code:
    ```csharp
    AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);
    ```

* Add the RuntimeHostConfigurationOption setting to your project file:
    ```xml
    <ItemGroup>
        <RuntimeHostConfigurationOption Include="Azure.Experimental.EnableActivitySource" Value="true" />
    </ItemGroup>
    ```

## Repository Structure

```
NETCORE\
    ApplicationInsights.AspNetCore.sln - Main Solution

    src\
        Microsoft.ApplicationInsights.AspNetCore - Application Insights for ASP.NET Core
        Microsoft.ApplicationInsights.WorkerService - Application Insights for Worker Services
        Shared - Shared code between packages

    test\
        Microsoft.ApplicationInsights.AspNetCore.Tests - Unit tests
        FunctionalTestUtils - Test utilities for functional tests
        MVCFramework.FunctionalTests - Functional tests for MVC applications
        WebApi.FunctionalTests - Functional tests for Web API applications
        EmptyApp.FunctionalTests - Functional tests for empty applications
        PerfTest - Performance tests
```

## Contributing

See [CONTRIBUTING.md](https://github.com/microsoft/ApplicationInsights-dotnet/blob/main/CONTRIBUTING.md) for details on contribution process.

## Additional Resources

- [Application Insights Documentation](https://docs.microsoft.com/azure/azure-monitor/app/asp-net-core)
- [OpenTelemetry .NET Documentation](https://github.com/open-telemetry/opentelemetry-dotnet)
- [Azure Monitor OpenTelemetry Exporter](https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/monitor/Azure.Monitor.OpenTelemetry.Exporter)
- [Migration Guide for OpenTelemetry](https://learn.microsoft.com/azure/azure-monitor/app/opentelemetry-dotnet-migrate?tabs=aspnetcore)

## Support

If you encounter any issues or have questions:
- Check the [troubleshooting guide](https://learn.microsoft.com/azure/azure-monitor/app/asp-net-core#troubleshooting)
- Review [GitHub Issues](https://github.com/microsoft/ApplicationInsights-dotnet/issues)
- Consult [Microsoft Q&A](https://learn.microsoft.com/answers/tags/azure-monitor/)

---

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
