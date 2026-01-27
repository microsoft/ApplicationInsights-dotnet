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


#### Resource Detectors
- **Azure App Service Resource Detector**: Adds resource attributes for applications running in Azure App Service.
- **Azure VM Resource Detector**: Adds resource attributes for applications running in an Azure Virtual Machine.
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

### ApplicationInsightsServiceOptions

The following options are available when configuring Application Insights in version 3.x:

```csharp
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    // Connection string for the Application Insights resource
    options.ConnectionString = "InstrumentationKey=...";
    
    // AAD authentication credential (optional)
    // options.Credential = new DefaultAzureCredential();
    
    // Application version reported with telemetry
    // options.ApplicationVersion = "1.0.0";
    
    // Enable or disable Live Metrics (default: true)
    options.EnableQuickPulseMetricStream = true;
    
    // Rate-limited sampling: maximum traces per second (default: 5)
    // Use this for rate-based sampling to limit telemetry volume
    options.TracesPerSecond = 5.0;
    
    // Percentage-based sampling: ratio of telemetry to collect (0.0 to 1.0)
    // Use this instead of TracesPerSecond for percentage-based sampling
    // options.SamplingRatio = 0.5f;  // 50% of telemetry
    
    // Enable or disable Application Insights Standard Metrics (default: true)
    options.AddAutoCollectedMetricExtractor = true;
    
    // Enable or disable performance counter collection (default: true)
    options.EnablePerformanceCounterCollectionModule = true;
    
    // Enable or disable dependency tracking (default: true)
    // When false, outbound HTTP, SQL, and other dependency calls are not tracked
    options.EnableDependencyTrackingTelemetryModule = true;
    
    // Enable or disable request tracking (default: true) - ASP.NET Core only
    // When false, incoming HTTP requests are not tracked
    options.EnableRequestTrackingTelemetryModule = true;
    
    // Enable JavaScript snippet for authenticated user tracking (default: false)
    options.EnableAuthenticationTrackingJavaScript = false;
    
    // Enable or disable trace-based log sampling (default: true)
    // When true, logs are sampled based on the sampling decision of the associated trace
    options.EnableTraceBasedLogsSampler = true;
});
```

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

### Custom OpenTelemetry Processor Registration
Register any custom processors with:

```C#
builder.Services.ConfigureOpenTelemetryTracerProvider((sp, builder) =>
{
    builder.AddProcessor<MyActivityProcessor>();
});
---
