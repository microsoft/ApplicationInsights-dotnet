## NuGet Packages

- [Microsoft.ApplicationInsights](https://www.nuget.org/packages/Microsoft.ApplicationInsights/)
[![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.svg)](https://www.nuget.org/packages/Microsoft.ApplicationInsights/)

# Application Insights for .NET - Core SDK

This repository contains the core .NET SDK for Application Insights. [Application Insights][AILandingPage] is a service that monitors the availability, performance, and usage of your applications. The `Microsoft.ApplicationInsights` package provides the foundational `TelemetryClient` API for sending telemetry from any .NET application.

> **For platform-specific scenarios:** If you're building an ASP.NET Core application, see the [ASP.NET Core SDK documentation](../NETCORE/Readme.md). For Worker Services, background tasks, or console applications, see the [Worker Service SDK documentation](../NETCORE/WorkerService.md). These packages provide automatic telemetry collection and simplified configuration.

## What's New in Version 3.x

Version 3.x represents a major architectural shift in the Application Insights SDK:

- **Built on OpenTelemetry**: The SDK now uses [OpenTelemetry](https://opentelemetry.io/) as the underlying telemetry collection framework with the [Azure Monitor Exporter](https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/monitor/Azure.Monitor.OpenTelemetry.Exporter) for transmission.
- **TelemetryClient as Shim**: The familiar `TelemetryClient` API is preserved as a compatibility layer that translates calls into OpenTelemetry primitives (Activity, ActivityEvent, LogRecord).
- **OpenTelemetry Extensibility**: You can extend telemetry collection using standard OpenTelemetry patterns (Activity Processors, Resource Detectors, custom instrumentation).
- **Unified Observability**: Seamlessly integrates with the broader OpenTelemetry ecosystem, allowing you to send telemetry to multiple backends.

**Breaking Changes from 2.x:**
- `ITelemetryInitializer` and `ITelemetryProcessor` are deprecated in favor of OpenTelemetry's Activity Processors and Log Processors
- Configuration model has changed to use OpenTelemetry's builder pattern
- Some context properties are now populated differently through OpenTelemetry Resource Detectors

See the [Migration Guide](#migrating-from-2x-to-3x) for detailed upgrade instructions.

## Quick Start: Choose Your Path

Select the option that best describes your situation:

- **📦 Building an ASP.NET Core web application?** → Use the [ASP.NET Core SDK](../NETCORE/Readme.md) for automatic instrumentation
- **⚙️ Building a Worker Service, console app, or background service?** → Use the [Worker Service SDK](../NETCORE/WorkerService.md) for simplified configuration
- **🔧 Need the core TelemetryClient API for custom scenarios?** → Continue reading this document
- **🔄 Migrating from Application Insights 2.x?** → Jump to the [Migration Guide](#migrating-from-2x-to-3x)
- **🎓 Want to understand how it works under the hood?** → Start with [OpenTelemetry Integration](#opentelemetry-integration)

---

## Table of Contents

- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Installation](#installation)
  - [Get a Connection String](#get-a-connection-string)
  - [Basic Configuration](#basic-configuration)
- [OpenTelemetry Integration](#opentelemetry-integration)
  - [Architecture Overview](#architecture-overview)
  - [Accessing OpenTelemetry APIs](#accessing-opentelemetry-apis)
  - [Custom Instrumentation](#custom-instrumentation)
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
- [Advanced Scenarios](#advanced-scenarios)
  - [Enriching Telemetry with Activity Processors](#enriching-telemetry-with-activity-processors)
  - [Filtering Telemetry](#filtering-telemetry)
  - [Custom Resource Attributes](#custom-resource-attributes)
  - [Sampling](#sampling)
- [Migrating from 2.x to 3.x](#migrating-from-2x-to-3x)
  - [Configuration Changes](#configuration-changes)
  - [Replacing ITelemetryInitializer](#replacing-itelemetryinitializer)
  - [Replacing ITelemetryProcessor](#replacing-itelemetryprocessor)
- [Troubleshooting](#troubleshooting)
- [SDK Layering](#sdk-layering)
- [Contributing](#contributing)

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

For more details, see [how to obtain a connection string][AIKey].

### Basic Configuration

#### Manual Configuration

Initialize `TelemetryConfiguration` and `TelemetryClient` in your application:

```csharp
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

var configuration = new TelemetryConfiguration
{
    ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://..."
};

var telemetryClient = new TelemetryClient(configuration);

// Start tracking telemetry
telemetryClient.TrackEvent("ApplicationStarted");
```

#### Configuration with Dependency Injection

For applications using dependency injection (console apps, worker services):

```csharp
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<TelemetryConfiguration>(sp =>
        {
            var config = new TelemetryConfiguration
            {
                ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000;..."
            };
            return config;
        });
        
        services.AddSingleton<TelemetryClient>(sp =>
        {
            var config = sp.GetRequiredService<TelemetryConfiguration>();
            return new TelemetryClient(config);
        });
    })
    .Build();

var telemetryClient = host.Services.GetRequiredService<TelemetryClient>();
telemetryClient.TrackEvent("ApplicationStarted");

await host.RunAsync();
```

#### Ensure Telemetry is Sent

Telemetry is batched and sent asynchronously by the OpenTelemetry SDK. When your application exits, call `Flush()` to ensure all telemetry is transmitted:

```csharp
telemetryClient.Flush();

```

## OpenTelemetry Integration

Before diving into the `TelemetryClient` API, it's important to understand the architecture of Application Insights 3.x. This SDK is built on **OpenTelemetry**, an industry-standard observability framework. Understanding this foundation will help you make better decisions about when to use `TelemetryClient` versus native OpenTelemetry APIs.

### Architecture Overview

Application Insights 3.x is built on OpenTelemetry:

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
├─────────────────────────────────────────────────────────┤
│  Activity Processors / Log Processors                   │
│  • Enrichment                                           │
│  • Filtering                                            │
│  • Sampling                                             │
├─────────────────────────────────────────────────────────┤
│  Azure Monitor Exporter                                 │
│  ↓ Converts to Application Insights schema             │
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

### Accessing OpenTelemetry APIs

You can use OpenTelemetry APIs directly alongside TelemetryClient. This gives you access to the full power of OpenTelemetry while maintaining compatibility with Application Insights.

> **Important:** If you haven't already, read [Understanding ActivitySource](#understanding-activitysource) in the Configuration section to learn about ActivitySource registration.

**Example: Combining OpenTelemetry and TelemetryClient**

```csharp
using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

public class OrderService
{
    // Create an ActivitySource for this service
    private static readonly ActivitySource ActivitySource = new("MyApp.OrderService");
    private readonly TelemetryClient _telemetryClient;
    
    public OrderService(TelemetryClient telemetryClient)
    {
        _telemetryClient = telemetryClient;
    }
    
    public async Task ProcessOrderAsync(string orderId)
    {
        // Option 1: Use OpenTelemetry Activity for distributed tracing
        // This creates a span that appears as a dependency in Application Insights
        using var activity = ActivitySource.StartActivity("ProcessOrder");
        activity?.SetTag("order.id", orderId);
        
        try
        {
            await ProcessPaymentAsync(orderId);
            await ShipOrderAsync(orderId);
            
            activity?.SetStatus(ActivityStatusCode.Ok);
            
            // Option 2: Use TelemetryClient for business events
            // This creates a custom event in Application Insights
            _telemetryClient.TrackEvent("OrderProcessed", new Dictionary<string, string>
            {
                { "OrderId", orderId }
            });
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}
```

**Don't forget to register your ActivitySource:**

```csharp
configuration.ConfigureOpenTelemetryBuilder(builder =>
{
    // Wildcards supported - this registers all ActivitySources starting with "MyApp."
    builder.AddSource("MyApp.*");
});
```

**Key Differences:**

| Aspect | OpenTelemetry Activity | TelemetryClient |
|--------|----------------------|-----------------|
| **Use Case** | Distributed tracing (spans, dependencies) | Events, metrics, compatibility |
| **Shows As** | Dependency or Request in App Insights | Event, Metric, or custom telemetry type |
| **Registration** | Requires `AddSource()` registration | Works immediately |
| **Child Spans** | Automatic parent-child relationships | Manual correlation |

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
    // Register by exact name
    builder.AddSource("MyApp.DataService");
    
    // Or use wildcards to register multiple sources at once
    builder.AddSource("MyApp.*");
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

Then register with: `builder.AddSource("MyCompany.*")`

---

## Using TelemetryClient

Now that you understand how Application Insights 3.x is built on OpenTelemetry, let's explore the `TelemetryClient` API. This familiar API from Application Insights 2.x is preserved as a compatibility layer, making it easy to track custom telemetry without directly interacting with OpenTelemetry primitives.

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
var metric = new MetricTelemetry
{
    Name = "RequestDuration",
    Value = 123.45
};
metric.Properties["Endpoint"] = "/api/users";
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
    telemetryClient.TrackDependency(
        dependencyTypeName: "HTTP",
        dependencyName: "GET /data",
        data: "https://api.example.com/data",
        startTime: startTime,
        duration: timer.Elapsed,
        success: false);
    
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
    telemetryClient.TrackRequest(
        name: "ProcessMessage",
        startTime: startTime,
        duration: timer.Elapsed,
        responseCode: "500",
        success: false);
    
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
var configuration = new TelemetryConfiguration
{
    ConnectionString = "InstrumentationKey=...;IngestionEndpoint=...",
    
    // Disable telemetry collection (useful for testing)
    DisableTelemetry = false
};
```

#### Configuring OpenTelemetry Integration

In version 3.x, you can extend the SDK using OpenTelemetry's extensibility model. Use `ConfigureOpenTelemetryBuilder()` to access the underlying OpenTelemetry configuration:

```csharp
configuration.ConfigureOpenTelemetryBuilder(builder =>
{
    // Add custom ActivitySources (explained below)
    builder.AddSource("MyApp.*");
    
    // Configure sampling
    builder.SetSampler(new TraceIdRatioBasedSampler(0.1));
    
    // Add processors
    builder.AddProcessor<CustomEnrichmentProcessor>();
});
```

#### Understanding ActivitySource

**What is an ActivitySource?**

An `ActivitySource` is OpenTelemetry's mechanism for creating distributed trace spans (called `Activity` in .NET). Think of it as a factory that creates telemetry for a specific component or subsystem in your application.

**Why do I need to register ActivitySources?**

By default, OpenTelemetry only collects Activities from sources you explicitly register. When you create a custom `ActivitySource` in your code, you must register its name (or name pattern) so the SDK knows to collect telemetry from it.

**Example: Creating and Registering an ActivitySource**

```csharp
// 1. Create an ActivitySource in your service class
public class OrderService
{
    // Define a static ActivitySource with a unique name
    private static readonly ActivitySource ActivitySource = new ActivitySource("MyCompany.OrderService");
    
    public async Task ProcessOrderAsync(string orderId)
    {
        // 2. Use the ActivitySource to create Activities (spans)
        using var activity = ActivitySource.StartActivity("ProcessOrder");
        activity?.SetTag("order.id", orderId);
        
        // Your business logic here
        await ValidateOrderAsync(orderId);
        await ChargePaymentAsync(orderId);
        
        activity?.SetStatus(ActivityStatusCode.Ok);
    }
}
```

```csharp
// 3. Register the ActivitySource when configuring TelemetryConfiguration
configuration.ConfigureOpenTelemetryBuilder(builder =>
{
    // Option 1: Register specific source by exact name
    builder.AddSource("MyCompany.OrderService");
    
    // Option 2: Use wildcard patterns to register multiple sources
    builder.AddSource("MyCompany.*");  // Captures MyCompany.OrderService, MyCompany.InventoryService, etc.
});
```

**When to use ActivitySource vs TelemetryClient:**

- **Use `ActivitySource`** for distributed tracing (requests, dependencies, operations that span multiple services)
- **Use `TelemetryClient`** for events, metrics, and when you need the Application Insights 2.x compatibility API

See [Custom Instrumentation](#custom-instrumentation) for detailed examples.

### Setting Context Properties

Set properties that apply to all telemetry:

```csharp
// Set cloud role name (appears as "Cloud role" in Azure)
telemetryClient.Context.Cloud.RoleName = "OrderProcessingService";

// Set application version
telemetryClient.Context.Component.Version = "1.2.3";

// Set user context (be mindful of PII)
telemetryClient.Context.User.Id = userId;
telemetryClient.Context.Session.Id = sessionId;

// Set device context
telemetryClient.Context.Device.OperatingSystem = Environment.OSVersion.ToString();

// Set custom properties
telemetryClient.Context.GlobalProperties["Environment"] = "Production";
telemetryClient.Context.GlobalProperties["DataCenter"] = "WestUS";
```

**Important:** In version 3.x, many context properties are populated through OpenTelemetry Resource Detectors. See [Custom Resource Attributes](#custom-resource-attributes) for the recommended approach.

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
    var config = new TelemetryConfiguration
    {
        ConnectionString = "InstrumentationKey=...;IngestionEndpoint=..."
    };
    
    // Configure OpenTelemetry
    config.ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.AddSource("MyApp.*");
    });
    
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

## Advanced Scenarios

With the fundamentals of configuration in place, let's explore advanced scenarios for enriching, filtering, and optimizing your telemetry collection. These techniques leverage OpenTelemetry's extensibility model to give you fine-grained control over what telemetry is collected and how it's processed.

### Enriching Telemetry with Activity Processors

Activity Processors replace `ITelemetryInitializer` from version 2.x. They enrich or modify telemetry before export:

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

**Register the processor:**

```csharp
configuration.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.AddProcessor<CustomEnrichmentProcessor>();
});
```

### Filtering Telemetry

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

**Register the processor:**

```csharp
configuration.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.AddProcessor<FilteringProcessor>();
});
```

### Custom Resource Attributes

Resource Detectors add contextual information to all telemetry:

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

### Sampling

Sampling reduces telemetry volume while maintaining statistical accuracy:

```csharp
using OpenTelemetry.Trace;

configuration.ConfigureOpenTelemetryBuilder(builder =>
{
    // Sample 10% of traces
    builder.SetSampler(new TraceIdRatioBasedSampler(0.1));
    
    // Or use parent-based sampling (respects upstream sampling decisions)
    builder.SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(0.1)));
});
```

For more sophisticated sampling (e.g., sample errors at 100%, successful requests at 10%):

```csharp
public class AdaptiveSampler : Sampler
{
    private readonly TraceIdRatioBasedSampler _defaultSampler = new(0.1);
    
    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
    {
        // Always sample errors
        var tags = samplingParameters.Tags;
        if (tags?.Any(t => t.Key == "error" && (bool)t.Value) == true)
        {
            return new SamplingResult(SamplingDecision.RecordAndSample);
        }
        
        // Sample other requests at 10%
        return _defaultSampler.ShouldSample(samplingParameters);
    }
}
```

---

## Migrating from 2.x to 3.x

If you're upgrading from Application Insights 2.x, this section will guide you through the key changes and help you adapt your existing code to the new OpenTelemetry-based architecture.

### Configuration Changes

**2.x Configuration:**

```csharp
// Old way (2.x)
var configuration = new TelemetryConfiguration
{
    InstrumentationKey = "your-instrumentation-key",
    TelemetryInitializers = { new CustomInitializer() },
    TelemetryProcessors = { new CustomProcessor() }
};
```

**3.x Configuration:**

```csharp
// New way (3.x)
var configuration = new TelemetryConfiguration
{
    ConnectionString = "InstrumentationKey=...;IngestionEndpoint=..."
};

configuration.ConfigureOpenTelemetryBuilder(builder =>
{
    // Use Activity Processors instead of ITelemetryInitializer/ITelemetryProcessor
    builder.AddProcessor<CustomEnrichmentProcessor>();
    builder.AddProcessor<FilteringProcessor>();
});
```

### Replacing ITelemetryInitializer

`ITelemetryInitializer` is replaced by Activity Processors:

**2.x ITelemetryInitializer:**

```csharp
// Old way (2.x)
public class CustomInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.Cloud.RoleName = "OrderService";
        
        if (telemetry is RequestTelemetry request)
        {
            request.Properties["CustomProperty"] = "CustomValue";
        }
    }
}
```

**3.x Activity Processor:**

```csharp
// New way (3.x)
public class CustomEnrichmentProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Set service name (equivalent to Cloud.RoleName)
        activity?.SetTag("service.name", "OrderService");
        
        // Add custom properties
        if (activity?.Kind == ActivityKind.Server)
        {
            activity?.SetTag("CustomProperty", "CustomValue");
        }
    }
}
```

**Register the processor:**

```csharp
configuration.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.AddProcessor<CustomEnrichmentProcessor>();
    
    // Also set service name via Resource
    builder.ConfigureResource(r => r.AddService(
        serviceName: "OrderService",
        serviceVersion: "1.0.0"));
});
```

### Replacing ITelemetryProcessor

`ITelemetryProcessor` is replaced by filtering Activity Processors:

**2.x ITelemetryProcessor:**

```csharp
// Old way (2.x)
public class FilteringProcessor : ITelemetryProcessor
{
    private ITelemetryProcessor _next;
    
    public FilteringProcessor(ITelemetryProcessor next)
    {
        _next = next;
    }
    
    public void Process(ITelemetry item)
    {
        // Filter out health checks
        if (item is RequestTelemetry request && request.Name.Contains("/health"))
        {
            return; // Don't pass to next processor
        }
        
        _next.Process(item);
    }
}
```

**3.x Activity Processor:**

```csharp
// New way (3.x)
public class FilteringProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Filter out health checks
        if (activity?.DisplayName?.Contains("/health") == true)
        {
            // Mark as not recorded to prevent export
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
        }
    }
}
```

**Register the processor:**

```csharp
configuration.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.AddProcessor<FilteringProcessor>();
});
```

---

## Troubleshooting

### Telemetry Not Appearing in Portal

1. **Verify Connection String**: Ensure the connection string is correct and includes the IngestionEndpoint.

2. **Check Network Connectivity**: Verify your application can reach the ingestion endpoint (typically `https://*.in.applicationinsights.azure.com`).

3. **Call Flush()**: Telemetry is batched. Call `Flush()` and wait a few seconds before application exit.

4. **Enable Diagnostic Logging**: Configure OpenTelemetry diagnostic logging:

```csharp
using OpenTelemetry;
using System.Diagnostics;

// Enable detailed logging
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
Sdk.CreateTracerProviderBuilder()
    .AddSource("Microsoft.ApplicationInsights.*")
    .AddConsoleExporter()
    .Build();
```

5. **Check Sampling**: If sampling is enabled, only a percentage of telemetry is sent.

6. **Verify Processors**: Ensure Activity Processors aren't filtering out your telemetry.

### High Telemetry Volume

1. **Enable Sampling**: Use TraceIdRatioBasedSampler to reduce volume:

```csharp
configuration.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.SetSampler(new TraceIdRatioBasedSampler(0.1)); // 10% sampling
});
```

2. **Filter Unnecessary Telemetry**: Use Activity Processors to filter out health checks, internal calls, etc.

3. **Reduce Trace Verbosity**: Avoid tracking trace telemetry at `Verbose` level in production.

### Performance Issues

1. **Reuse TelemetryClient**: Don't create new instances for each operation.

2. **Avoid Blocking on Flush()**: Telemetry is sent asynchronously. Only call `Flush()` at application shutdown.

3. **Optimize Activity Processors**: Keep processor logic fast and non-blocking.

4. **Use Async APIs**: Prefer async patterns in your application code.

### Missing Context Properties

In 3.x, many context properties are set via Resource Detectors:

```csharp
configuration.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.ConfigureResource(resourceBuilder =>
    {
        resourceBuilder.AddService(serviceName: "MyService", serviceVersion: "1.0.0");
        resourceBuilder.AddAttributes(new[]
        {
            new KeyValuePair<string, object>("deployment.environment", "Production")
        });
    });
});
```

## SDK Layering

The Application Insights SDK is composed of multiple layers:

| Layer | Description | Extensibility |
|-------|-------------|---------------|
| **TelemetryClient API** | High-level API for tracking telemetry (events, metrics, dependencies, etc.) | Track custom telemetry using `TelemetryClient.Track*()` methods |
| **OpenTelemetry Shim** | Translates `TelemetryClient` calls to OpenTelemetry primitives (Activity, LogRecord, Metrics) | Use OpenTelemetry APIs directly (`ActivitySource`, `ILogger`, `Meter`) |
| **OpenTelemetry SDK** | Collects, processes, and exports telemetry using industry-standard OpenTelemetry | Add custom instrumentation, processors, samplers, exporters |
| **Activity Processors** | Enrich, filter, or modify telemetry before export | Implement `BaseProcessor<Activity>` |
| **Resource Detectors** | Add contextual information (service name, version, environment, etc.) | Implement `IResourceDetector` |
| **Azure Monitor Exporter** | Converts OpenTelemetry signals to Application Insights schema and sends to Azure Monitor | Configure exporter options (endpoint, retry policy, etc.) |

### Platform-Specific Packages

The core `Microsoft.ApplicationInsights` package provides the foundational `TelemetryClient` API. Platform-specific packages build on this foundation:

- **[Microsoft.ApplicationInsights.AspNetCore](../NETCORE/Readme.md)**: Automatic instrumentation for ASP.NET Core applications (requests, dependencies, exceptions, performance counters). Includes middleware, filters, and automatic context propagation.

- **[Microsoft.ApplicationInsights.WorkerService](../NETCORE/WorkerService.md)**: Optimized for Worker Services, background tasks, and console applications. Provides automatic dependency tracking and integrates with `ILogger`.

- **Microsoft.ApplicationInsights.Web**: Legacy package for ASP.NET Framework applications (not covered in this documentation).

These packages configure OpenTelemetry instrumentation libraries and the Azure Monitor Exporter automatically, eliminating most manual configuration.  

> **Note:** For classic Application Insights SDK (version 2.x), refer to the [2.x branch documentation](https://github.com/microsoft/ApplicationInsights-dotnet/tree/2.x).

## Contributing

We strongly welcome and encourage contributions to this project. Please read the general [contributor's guide][ContribGuide] located in the ApplicationInsights-Home repository and the [contributing guide](https://github.com/Microsoft/ApplicationInsights-dotnet/blob/develop/.github/CONTRIBUTING.md)  for this SDK. If making a large change we request that you open an [issue][GitHubIssue] first. We follow the [Git Flow][GitFlow] approach to branching. 

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

[AILandingPage]: https://azure.microsoft.com/services/application-insights/
[api-overview]: https://learn.microsoft.com/azure/azure-monitor/app/api-custom-events-metrics
[ContribGuide]: https://github.com/Microsoft/ApplicationInsights-Home/blob/master/CONTRIBUTING.md
[GitFlow]: http://nvie.com/posts/a-successful-git-branching-model/
[GitHubIssue]: https://github.com/Microsoft/ApplicationInsights-dotnet/issues
[master]: https://github.com/Microsoft/ApplicationInsights-dotnet/tree/master
[develop]: https://github.com/Microsoft/ApplicationInsights-dotnet/tree/development
[NuGetCore]: https://www.nuget.org/packages/Microsoft.ApplicationInsights
[WebGetStarted]: https://learn.microsoft.com/azure/azure-monitor/app/asp-net
[WinAppGetStarted]: https://learn.microsoft.com/azure/azure-monitor/app/windows-desktop
[DesktopGetStarted]: https://learn.microsoft.com/azure/azure-monitor/app/windows-desktop
[AIKey]: https://learn.microsoft.com/azure/azure-monitor/app/create-workspace-resource#copy-the-connection-string
