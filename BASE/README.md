## NuGet Packages

- [Microsoft.ApplicationInsights](https://www.nuget.org/packages/Microsoft.ApplicationInsights/)
[![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.svg)](https://www.nuget.org/packages/Microsoft.ApplicationInsights/)

# Application Insights for .NET - Core SDK
 The `Microsoft.ApplicationInsights` package provides the foundational `TelemetryClient` API for sending telemetry from any .NET application to ApplicationInsights.

## Table of Contents

- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Installation](#installation)
  - [Get a Connection String](#get-a-connection-string)
  - [Basic Configuration](#basic-configuration)
- [Using TelemetryClient](#using-telemetryclient)
  - [Tracking Events](#tracking-events)
  - [Tracking Metrics](#tracking-metrics)
  - [Tracking Dependencies](#tracking-dependencies)
  - [Tracking Exceptions](#tracking-exceptions)
  - [Tracking Requests](#tracking-requests)
  - [Tracking Traces](#tracking-traces)
  - [Tracking Availability](#tracking-availability)
  - [Tracking Page Views](#tracking-page-views)
- [Configuration](#configuration)
  - [TelemetryConfiguration](#telemetryconfiguration)
  - [Setting Context Properties](#setting-context-properties)
  - [Dependency Injection](#dependency-injection)

## Getting Started

### Prerequisites

- .NET Framework 4.6.2+ or .NET 8.0+
- An Azure Application Insights resource ([create one in the portal][AIKey])

### Installation

Install the NuGet package using the Package Manager Console:

```powershell
Install-Package Microsoft.ApplicationInsights
```

Or using the .NET CLI:

```bash
dotnet add package Microsoft.ApplicationInsights
```

### Get a Connection String

To use the Application Insights SDK, you need a **Connection String** from your Application Insights resource:

1. Navigate to your Application Insights resource in the [Azure Portal](https://portal.azure.com)
2. In the "Overview" section, copy the Connection String
3. The Connection String format is: `InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://...`

For more details, see [how to obtain a connection string](https://learn.microsoft.com/en-us/azure/azure-monitor/app/create-workspace-resource?tabs=portal#get-the-connection-string).

### Basic Configuration

#### Manual Configuration

Initialize `TelemetryConfiguration` and `TelemetryClient` in your application:

```csharp
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

var configuration = TelemetryConfiguration.CreateDefault();
configuration.ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://...";

var telemetryClient = new TelemetryClient(configuration);

// Start tracking telemetry
telemetryClient.TrackEvent("ApplicationStarted");
```

#### Ensure Telemetry is Sent

Telemetry is batched and sent asynchronously by the OpenTelemetry SDK. When your application exits, call `Flush()` to ensure all telemetry is transmitted:

```csharp
telemetryClient.Flush();

```

---

## Using TelemetryClient

Let's explore the `TelemetryClient` API. This familiar API from Application Insights 2.x is preserved as a compatibility layer, making it easy to track custom telemetry without directly interacting with OpenTelemetry primitives.

The `TelemetryClient` class provides methods for tracking different types of telemetry.

### Tracking Events

Track custom business events:

```csharp
using Microsoft.ApplicationInsights.DataContracts;

// Simple event
telemetryClient.TrackEvent("UserLoggedIn");

// Event with properties
telemetryClient.TrackEvent("OrderProcessed", 
    properties: new Dictionary<string, string>
    {
        { "OrderId", "12345" },
        { "Category", "Electronics" },
        { "PaymentMethod", "CreditCard" }
    });

// For numeric values with events, use properties (converted to strings)
var orderEvent = new EventTelemetry("OrderProcessed");
orderEvent.Properties["OrderId"] = "12345";
orderEvent.Properties["OrderValue"] = "249.99";
orderEvent.Properties["ProcessingTimeMs"] = "1234.5";
telemetryClient.TrackEvent(orderEvent);
```

### Tracking Metrics

Track numeric measurements:

```csharp
using Microsoft.ApplicationInsights.DataContracts;

// Single metric value
telemetryClient.TrackMetric("QueueLength", 42);

// Metric with properties for segmentation
telemetryClient.TrackMetric(
    name: "ResponseTime",
    value: 123.45,
    properties: new Dictionary<string, string>
    {
        { "Endpoint", "/api/users" },
        { "StatusCode", "200" }
    });

// Using MetricTelemetry object
var metric = var metric = new MetricTelemetry("RequestDuration", 123.45);
telemetryClient.TrackMetric(metric);
```

### Tracking Dependencies

Track calls to external services (databases, HTTP APIs, queues):

```csharp
var startTime = DateTimeOffset.UtcNow;
var timer = System.Diagnostics.Stopwatch.StartNew();

try
{
    // Make external call
    var result = await httpClient.GetAsync("https://api.example.com/data");
    
    // Track successful dependency
    telemetryClient.TrackDependency(
        dependencyTypeName: "HTTP",
        dependencyName: "GET /data",
        data: "https://api.example.com/data",
        startTime: startTime,
        duration: timer.Elapsed,
        success: result.IsSuccessStatusCode);
}
catch (Exception ex)
{
    // Track failed dependency
    var failedDependency = new DependencyTelemetry
    {
        Type = "HTTP",
        Name = "GET /data",
        Data = "https://api.example.com/data",
        Timestamp = startTime,
        Duration = timer.Elapsed,
        Success = false
    };
    telemetryClient.TrackDependency(failedDependency);
    
    throw;
}
```

### Tracking Exceptions

Track exceptions with context:

```csharp
try
{
    // Application code
    ProcessOrder(orderId);
}
catch (Exception ex)
{
    telemetryClient.TrackException(ex, new Dictionary<string, string>
    {
        { "OrderId", orderId },
        { "Operation", "ProcessOrder" }
    });
    
    // Re-throw or handle
    throw;
}
```
For more examples of sending exception telemetry, please reference the [ExceptionTelemetryExamples](../examples/BasicConsoleApp/ExceptionTelemetryExamples.cs).

### Tracking Requests

Track incoming requests (useful for non-web applications):

```csharp
var startTime = DateTimeOffset.UtcNow;
var timer = System.Diagnostics.Stopwatch.StartNew();

try
{
    // Process request
    var result = await ProcessMessageAsync(message);
    
    telemetryClient.TrackRequest(
        name: "ProcessMessage",
        startTime: startTime,
        duration: timer.Elapsed,
        responseCode: "200",
        success: true);
}
catch (Exception ex)
{
    var failedRequest = new RequestTelemetry
    {
        Name = "ProcessMessage",
        Timestamp = startTime,
        Duration = timer.Elapsed,
        ResponseCode = "500",
        Success = false
    };
    telemetryClient.TrackRequest(failedRequest);
    
    telemetryClient.TrackException(ex);
    throw;
}
```

### Tracking Traces

Track diagnostic log messages:

```csharp
using Microsoft.ApplicationInsights.DataContracts;

// Simple trace
telemetryClient.TrackTrace("Processing started");

// Trace with severity and properties
telemetryClient.TrackTrace(
    message: "Order validation failed",
    severityLevel: SeverityLevel.Warning,
    properties: new Dictionary<string, string>
    {
        { "OrderId", "12345" },
        { "ValidationErrors", "Missing required field: CustomerEmail" }
    });
```
For more examples of sending Application Insights traces, please see the [BasicConsoleApp example](../examples/BasicConsoleApp/Program.cs).

**Severity Levels:**
- `Verbose` - Detailed diagnostic information
- `Information` - General informational messages
- `Warning` - Warning messages for potentially harmful situations
- `Error` - Error events that might still allow the application to continue
- `Critical` - Critical failures requiring immediate attention

### Tracking Availability

Track availability tests (synthetic monitoring):

```csharp
using Microsoft.ApplicationInsights.DataContracts;

var startTime = DateTimeOffset.UtcNow;
var timer = System.Diagnostics.Stopwatch.StartNew();
bool success = false;
string message = null;

try
{
    // Perform availability check
    var response = await httpClient.GetAsync("https://myapp.azurewebsites.net/health");
    success = response.IsSuccessStatusCode;
    message = $"Status: {response.StatusCode}";
}
catch (Exception ex)
{
    success = false;
    message = ex.Message;
}

// Syntax 1 - with AvailabilityTelemetry object
var availability = new AvailabilityTelemetry
{
    Name = "Health Check",
    RunLocation = Environment.MachineName,
    Success = success,
    Duration = timer.Elapsed,
    Timestamp = startTime,
    Message = message
};

telemetryClient.TrackAvailability(availability);

// Syntax 2 - with individual parameters
telemetryClient.TrackAvailability(
    name: "Health Check",
    timeStamp: startTime,
    duration: timer.Elapsed,
    runLocation: Environment.MachineName,
    success: success,
    message: message);
```

### Tracking Page Views

**Note:** `TrackPageView()` is not available in version 3.x. For server-side page view tracking, use `TrackEvent()` or `TrackRequest()` instead:

```csharp
// Track page view as an event
telemetryClient.TrackEvent("PageView", new Dictionary<string, string>
{
    { "PageName", "ProductDetails" },
    { "Url", "https://myapp.com/products/12345" },
    { "ProductId", "12345" },
    { "Category", "Electronics" }
});

// Or track as a request for page loads
telemetryClient.TrackRequest(
    name: "GET /products/12345",
    startTime: DateTimeOffset.UtcNow,
    duration: TimeSpan.FromMilliseconds(150),
    responseCode: "200",
    success: true);
```

For client-side page view tracking, use the [Application Insights JavaScript SDK](https://learn.microsoft.com/azure/azure-monitor/app/javascript).

---

## Configuration

Now that you've seen how to track telemetry with `TelemetryClient`, let's explore how to configure the SDK to suit your application's needs. Configuration in version 3.x centers around `TelemetryConfiguration` and OpenTelemetry's builder pattern.

### TelemetryConfiguration

The `TelemetryConfiguration` class controls SDK behavior:

```csharp
var configuration = TelemetryConfiguration.CreateDefault();
configuration.ConnectionString = "InstrumentationKey=...;IngestionEndpoint=...";
```

#### Telemetry Configuration Properties 

`TelemetryConfiguration` exposes properties that configure settings related to sampling, offline storage, and live metrics emission. 

| Property | Type | Default (when null) | Description |
|----------|------|---------------------|-------------|
| `SamplingRatio` | `float?` | None | Percentage of opentelemetry traces to sample (0.0 to 1.0) |
| `TracesPerSecond` | `double?` | 5 | Traces per second (rate-limited sampling) |
| `StorageDirectory` | `string` | Platform-specific* | Directory for offline telemetry storage |
| `DisableOfflineStorage` | `bool?` | `false` | When `true`, disables offline storage for failed transmissions |
| `EnableLiveMetrics` | `bool?` | `true` | Enables Live Metrics stream in Azure Portal |
| `EnableTraceBasedLogsSampler` | `bool?` | `true` | Applies trace sampling decisions to related logs |

*Storage directory defaults: Windows: `%LOCALAPPDATA%\Microsoft\AzureMonitor`, Linux/macOS: `$TMPDIR/Microsoft/AzureMonitor`

> **⚠️ Sampling Configuration:** Configure **either** `SamplingRatio` **or** `TracesPerSecond`, not both. Use `SamplingRatio` for percentage-based sampling (e.g., keep 50% of telemetry). Use `TracesPerSecond` for rate-limited sampling (e.g., keep at most 5 OpenTelemetry traces per second regardless of load).

**Example:**

```csharp
var configuration = TelemetryConfiguration.CreateDefault();
configuration.ConnectionString = "InstrumentationKey=...;IngestionEndpoint=...";

// Sampling: Choose ONE of these approaches
configuration.SamplingRatio = 0.5f;       // Keep 50% of telemetry
// configuration.TracesPerSecond = 5.0;   // OR: Keep max 5 traces/second

// Offline storage
configuration.StorageDirectory = @"C:\AppData\MyApp\Telemetry";
configuration.DisableOfflineStorage = false;

// Features
configuration.EnableLiveMetrics = true;
configuration.EnableTraceBasedLogsSampler = true;
```

#### Configuring OpenTelemetry Integration

In version 3.x, you can extend the SDK using OpenTelemetry's extensibility model. Use `ConfigureOpenTelemetryBuilder()` to access the underlying OpenTelemetry configuration:

```csharp
configuration.ConfigureOpenTelemetryBuilder(builder =>
{
    // Add custom ActivitySources (explained below)
    builder.WithTracing(tracing => tracing.AddSource("MyApp.*"));
    
    // Add processors
    builder.WithTracing(tracing => tracing.AddProcessor<CustomEnrichmentProcessor>());
});
```
Note: Setting an OpenTelemetry Sampler via `builder.SetSampler()` is currently unsupported and will lead to unexpected sampling behavior.



### Setting Context Properties

In version 3.x, the following properties remain publicly settable on telemetry items:

**Available Context Properties:**
| Context | Properties | Notes |
|---------|-----------|-------|
| `User` | `Id`, `AuthenticatedUserId`, `UserAgent` | Be mindful of PII |
| `Operation` | `Name`| |
| `Location` | `Ip` | |
| `GlobalProperties` | (dictionary) | Custom key-value pairs |

**Example: Setting context on a telemetry item**

```csharp
var request = new RequestTelemetry
{
    Name = "ProcessOrder",
    Timestamp = DateTimeOffset.UtcNow,
    Duration = TimeSpan.FromMilliseconds(150),
    ResponseCode = "200",
    Success = true
};

// Set user context (be mindful of PII)
request.Context.User.Id = userId;
request.Context.User.AuthenticatedUserId = authenticatedUserId;

// Set operation context
request.Context.Operation.Name = "ProcessOrder";

// Set location context
request.Context.Location.Ip = clientIpAddress;

// Set custom properties via GlobalProperties
request.Context.GlobalProperties["Environment"] = "Production";
request.Context.GlobalProperties["DataCenter"] = "WestUS";

telemetryClient.TrackRequest(request);
```

**Recommended: Use OpenTelemetry Resource Attributes**

For service-level context (cloud role, version, environment), the recommended approach in 3.x is to use OpenTelemetry Resource attributes, which apply to all telemetry automatically:

```csharp
configuration.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.ConfigureResource(r => r
        .AddService(
            serviceName: "OrderProcessingService",       // Maps to Cloud.RoleName
            serviceVersion: "1.2.3",                     // Maps to Application Version
            serviceInstanceId: Environment.MachineName)  // Maps to Cloud.RoleInstance
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = "Production"
        }));
});
```

### Dependency Injection

In applications using Microsoft.Extensions.DependencyInjection:

```csharp
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Register TelemetryConfiguration
services.AddSingleton(sp =>
{
    var config = TelemetryConfiguration.CreateDefault();
    config.ConnectionString = "InstrumentationKey=...;IngestionEndpoint=...";
    
    return config;
});

// Register TelemetryClient
services.AddSingleton<TelemetryClient>(sp =>
{
    var config = sp.GetRequiredService<TelemetryConfiguration>();
    return new TelemetryClient(config);
});

// Inject into your services
public class OrderService
{
    private readonly TelemetryClient _telemetryClient;
    
    public OrderService(TelemetryClient telemetryClient)
    {
        _telemetryClient = telemetryClient;
    }
    
    public void ProcessOrder(string orderId)
    {
        _telemetryClient.TrackEvent("OrderProcessed", new Dictionary<string, string>
        {
            { "OrderId", orderId }
        });
    }
}
```
---