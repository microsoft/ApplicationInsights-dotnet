## NuGet Packages

- [Microsoft.ApplicationInsights.Web](https://www.nuget.org/packages/Microsoft.ApplicationInsights.Web/)
[![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.Web.svg)](https://nuget.org/packages/Microsoft.ApplicationInsights.Web)

# Application Insights for ASP.NET Framework Web Applications

This package provides Application Insights telemetry collection for classic ASP.NET Framework web applications (.NET Framework 4.6.2+). [Application Insights][AILandingPage] is a service that monitors the availability, performance, and usage of your web applications.

> **For ASP.NET Core applications:** Use the [Microsoft.ApplicationInsights.AspNetCore](../NETCORE/Readme.md) package instead.  
> **For console apps or background services:** Use the [Microsoft.ApplicationInsights](../BASE/README.md) or [Microsoft.ApplicationInsights.WorkerService](../NETCORE/WorkerService.md) packages.

## What's New in Version 3.x

Version 3.x represents a major architectural shift:

- **Built on OpenTelemetry**: The SDK now uses [OpenTelemetry](https://opentelemetry.io/) as the underlying telemetry collection framework with the [Azure Monitor Exporter](https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/monitor/Azure.Monitor.OpenTelemetry.Exporter) for transmission.
- **TelemetryClient as Shim**: The familiar `TelemetryClient` API is preserved as a compatibility layer that translates calls into OpenTelemetry primitives.
- **Automatic Instrumentation**: The SDK automatically collects:

  - **HTTP Requests** - Web request timings, success rates, and response codes
  - **Dependencies** - SQL queries, HTTP calls to external services
  - **Exceptions** - Unhandled exceptions and their stack traces  
  - **Performance Counters** - Server CPU, memory, and other metrics
  - **Custom Telemetry** - Events, traces, and metrics you log via the API

**Breaking Changes from 2.x:**
- Minimum .NET Framework version is now 4.6.2 (upgraded from 4.5.2)
- Configuration uses `ConnectionString` instead of `InstrumentationKey`
- `TelemetryInitializers` and `TelemetryModules` in config file are no longer supported (use OpenTelemetry processors instead)
- Internal instrumentation now uses OpenTelemetry libraries

See the [Migration Guide](#migrating-from-2x-to-3x) for detailed upgrade instructions.

## Table of Contents

- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Installation](#installation)
  - [Configuration](#configuration)
- [Using TelemetryClient](#using-telemetryclient)
- [Advanced Configuration](#advanced-configuration)
  - [Customizing Telemetry Collection](#customizing-telemetry-collection)
  - [Adding Custom Properties](#adding-custom-properties)
  - [Performance Counters](#performance-counters)
- [Migrating from 2.x to 3.x](#migrating-from-2x-to-3x)
  - [Migration Steps](#migration-steps)
  - [Configuration Changes](#configuration-changes)
  - [Replacing Telemetry Initializers](#replacing-telemetry-initializers)
- [Architecture](#architecture)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)

## Getting Started

### Prerequisites

- .NET Framework 4.6.2 or later
- ASP.NET Framework web application (Web Forms, MVC, Web API, etc.)
- An Azure Application Insights resource ([create one in the portal][AzurePortal])

### Installation

Install the SDK using NuGet:

```powershell
Install-Package Microsoft.ApplicationInsights.Web
```

Or using the .NET CLI:

```bash
dotnet add package Microsoft.ApplicationInsights.Web
```

The NuGet package will automatically:
- Add `ApplicationInsights.config` to your project root
- Register the required HTTP modules in your `web.config`
- Add necessary assembly references

### Configuration

#### 1. Get a Connection String

Get your Connection String from your Application Insights resource in the [Azure Portal][AzurePortal]:

1. Navigate to your Application Insights resource
2. In the "Overview" section, copy the Connection String
3. The format is: `InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://...`

#### 2. Update ApplicationInsights.config

The NuGet package creates `ApplicationInsights.config` in your project root. Update it with your connection string:

```xml
<?xml version="1.0" encoding="utf-8"?>
<ApplicationInsights xmlns="http://schemas.microsoft.com/ApplicationInsights/2013/Settings">
  <ConnectionString>InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://...</ConnectionString>
</ApplicationInsights>
```

> **Tip:** For security, consider storing the connection string in environment variables or Azure App Configuration instead of committing it to source control.

#### 3. Verify web.config Entries

The NuGet package automatically adds HTTP module registrations to your `web.config`. Verify these entries exist:

```xml
<system.web>
  <httpModules>
    <!-- For IIS Classic mode -->
    <add name="ApplicationInsightsWebTracking" 
         type="Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web" />
  </httpModules>
</system.web>

<system.webServer>
  <validation validateIntegratedModeConfiguration="false" />
  <modules>
    <!-- For IIS Integrated mode -->
    <remove name="ApplicationInsightsWebTracking" />
    <add name="ApplicationInsightsWebTracking" 
         type="Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web" 
         preCondition="integratedMode,managedHandler" />
    <add name="TelemetryHttpModule"
         type="OpenTelemetry.Instrumentation.AspNet.TelemetryHttpModule, OpenTelemetry.Instrumentation.AspNet.TelemetryHttpModule"
         preCondition="integratedMode,managedHandler"/>
  </modules>
</system.webServer>
```

#### 4. Run Your Application

That's it! Start your application and:
- Navigate through your site to generate telemetry
- Wait a few minutes for data to appear
- View telemetry in the [Azure Portal][AzurePortal] under your Application Insights resource

The SDK automatically tracks:
- **Requests**: All HTTP requests with URL, duration, response code
- **Dependencies**: SQL queries, HTTP calls to external services  
- **Exceptions**: Unhandled exceptions with full stack traces
- **Performance Counters**: CPU, memory, request rate, etc.

## Using TelemetryClient

For custom telemetry beyond automatic collection, use the `TelemetryClient` API.

**Important:** In version 3.x, you must create a `TelemetryConfiguration` first (using `TelemetryConfiguration.CreateDefault()` to load from `ApplicationInsights.config`), then pass it to the `TelemetryClient` constructor. Create one instance at application startup and reuse it throughout your application - `TelemetryClient` is thread-safe.

> **⚠️ CRITICAL: Single Instance Pattern Required**
> 
> Always create **ONE** `TelemetryClient` instance at application startup and reuse it throughout your application's lifetime. Creating multiple instances causes:
> - **Memory leaks** - Each instance creates internal buffers and timers that are never released
> - **Performance degradation** - Each instance spawns background threads for batching and transmission
> - **Duplicate telemetry** - Multiple instances may send the same data multiple times
> - **Configuration inconsistencies** - Each instance may have different settings
>
> **Never** create `TelemetryClient` instances per request, per controller, or in constructors that execute repeatedly.

### Recommended Pattern: Initialize in Global.asax.cs

Create a single `TelemetryClient` instance in `Application_Start` and reuse it throughout your application:

```csharp
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

public class MvcApplication : System.Web.HttpApplication
{
    public static TelemetryClient TelemetryClient { get; private set; }
    
    protected void Application_Start()
    {
        // CreateDefault() loads configuration from ApplicationInsights.config
        var configuration = TelemetryConfiguration.CreateDefault();
        
        // Create TelemetryClient once at application startup
        TelemetryClient = new TelemetryClient(configuration);
        
        // Optionally set properties that apply to all telemetry
        TelemetryClient.Context.Cloud.RoleName = "MyWebApp";
        TelemetryClient.Context.Component.Version = "1.0.0";
        TelemetryClient.Context.GlobalProperties["Environment"] = "Production";
        
        // Your other startup code
        AreaRegistration.RegisterAllAreas();
        RouteConfig.RegisterRoutes(RouteTable.Routes);
    }
    
    protected void Application_End()
    {
        // Flush telemetry before application shutdown
        TelemetryClient.Flush();
        System.Threading.Tasks.Task.Delay(1000).Wait();
    }
}
```

### Use the Shared Instance in Controllers

```csharp
using Microsoft.ApplicationInsights;

public class OrderController : Controller
{
    public ActionResult ProcessOrder(int orderId)
    {
        try
        {
            // Use the shared TelemetryClient instance
            MvcApplication.TelemetryClient.TrackEvent("OrderProcessed");
            
            // Track events with properties
            MvcApplication.TelemetryClient.TrackEvent("OrderCompleted", new Dictionary<string, string>
            {
                { "OrderId", orderId.ToString() },
                { "Category", "Electronics" }
            });
            
            // Track custom metrics
            MvcApplication.TelemetryClient.TrackMetric("OrderValue", 299.99);
            
            return View();
        }
        catch (Exception ex)
        {
            // Exceptions are automatically tracked, but you can add custom properties
            MvcApplication.TelemetryClient.TrackException(ex, new Dictionary<string, string>
            {
                { "OrderId", orderId.ToString() }
            });
            
            throw;
        }
    }
}
```

> **Why not create TelemetryClient per request?** Creating multiple `TelemetryClient` instances can lead to configuration inconsistencies and unnecessary overhead. Each instance initializes its own context and configuration, which is wasteful. Always reuse a single instance.

**Available tracking methods:**
- `TrackEvent()` - Business events (user actions, milestones)
- `TrackMetric()` - Numeric measurements
- `TrackException()` - Exceptions and errors
- `TrackTrace()` - Diagnostic log messages
- `TrackRequest()` - Custom request tracking (usually automatic)
- `TrackDependency()` - Custom dependency calls (usually automatic)
- `TrackAvailability()` - Availability test results

For more details, see the [TelemetryClient API documentation](../BASE/README.md#using-telemetryclient).


## Advanced Configuration

### Customizing Telemetry Collection

> **Note:** For most scenarios, the automatic telemetry collection is sufficient. Advanced customization using OpenTelemetry processors is optional.

If you need to add custom properties to all telemetry, use the `TelemetryClient.Context` properties:

```csharp
using Microsoft.ApplicationInsights;

protected void Application_Start()
{
    // Load configuration and create TelemetryClient
    var configuration = TelemetryConfiguration.CreateDefault();
    var telemetry = new TelemetryClient(configuration);
    
    // Set properties that apply to all telemetry
    telemetry.Context.Cloud.RoleName = "MyWebApp";
    telemetry.Context.Component.Version = "1.0.0";
    telemetry.Context.GlobalProperties["Environment"] = "Production";
}
```

### Filtering Telemetry

In version 3.x, telemetry filtering is handled through OpenTelemetry processors. The SDK automatically tracks all HTTP requests, but you can filter them by configuring the OpenTelemetry instrumentation.

> **Note:** For basic scenarios, automatic tracking is sufficient. Filtering is typically only needed to exclude health checks, internal diagnostics endpoints, or reduce telemetry volume.

To exclude specific URL patterns (like health checks), you would need to configure OpenTelemetry processors. For most ASP.NET Framework applications, the simplest approach is to handle filtering at the infrastructure level (load balancer health checks) rather than in code.

If you need custom filtering logic, see the [BASE SDK documentation](../BASE/README.md#enriching-telemetry-with-activity-processors) for details on implementing Activity Processors.

### Performance Counters

Performance counters are automatically collected in version 3.x via OpenTelemetry instrumentation. Collected counters include:

- **Processor Time** - CPU usage
- **Available Memory** - Free memory in bytes
- **Request Rate** - Requests per second
- **Request Duration** - Average request duration
- **Exception Rate** - Exceptions per second

No additional configuration is required for standard performance counters.

## Migrating from 2.x to 3.x

⚠️ **Version 3.x contains breaking changes.** The SDK now uses OpenTelemetry internally.

### Migration Steps

1. **Update the NuGet package:**

```powershell
Update-Package Microsoft.ApplicationInsights.Web
```

2. **Update ApplicationInsights.config:**

Replace `InstrumentationKey` with `ConnectionString`:

**Before (2.x):**
```xml
<InstrumentationKey>00000000-0000-0000-0000-000000000000</InstrumentationKey>
```

**After (3.x):**
```xml
<ConnectionString>InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://...</ConnectionString>
```

3. **Remove deprecated configuration sections:**

Remove `<TelemetryInitializers>`, `<TelemetryModules>`, and `<TelemetryProcessors>` from ApplicationInsights.config. These are no longer supported in 3.x.

4. **Update project target framework:**

Ensure your project targets .NET Framework 4.6.2 or later (minimum requirement for 3.x).

5. **Test thoroughly:**

The internal instrumentation has changed significantly. Test your application to ensure telemetry is collected as expected.

### Configuration Changes

**2.x Configuration (ApplicationInsights.config):**
```xml
<ApplicationInsights>
  <InstrumentationKey>...</InstrumentationKey>
  <TelemetryInitializers>
    <Add Type="MyApp.CustomInitializer, MyApp" />
  </TelemetryInitializers>
  <TelemetryModules>
    <Add Type="Microsoft.ApplicationInsights.Web.RequestTrackingTelemetryModule, Microsoft.AI.Web" />
  </TelemetryModules>
</ApplicationInsights>
```

**3.x Configuration (ApplicationInsights.config):**
```xml
<ApplicationInsights>
  <ConnectionString>InstrumentationKey=...;IngestionEndpoint=https://...</ConnectionString>
  <!-- TelemetryInitializers and TelemetryModules no longer supported -->
</ApplicationInsights>
```

### Replacing Telemetry Initializers and Processors

In 2.x, `ITelemetryInitializer` and `ITelemetryProcessor` were used to enrich and filter telemetry. In 3.x, these are replaced by OpenTelemetry Activity Processors.

#### Simple Property Enrichment

For basic property setting, use `TelemetryClient.Context` properties:

**2.x Telemetry Initializer:**
```csharp
public class CustomInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.Cloud.RoleName = "WebApp";
        telemetry.Context.GlobalProperties["Environment"] = "Production";
    }
}
```

**3.x Equivalent (Global.asax.cs):**
```csharp
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

protected void Application_Start()
{
    var configuration = TelemetryConfiguration.CreateDefault();
    var telemetry = new TelemetryClient(configuration);
    
    // Set properties that apply to all telemetry
    telemetry.Context.Cloud.RoleName = "WebApp";
    telemetry.Context.Component.Version = "1.0.0";
    telemetry.Context.GlobalProperties["Environment"] = "Production";
}
```

#### Advanced Custom Processing with OpenTelemetry

For complex enrichment or filtering logic (equivalent to custom `ITelemetryInitializer` or `ITelemetryProcessor`), implement an OpenTelemetry Activity Processor:

**2.x Telemetry Processor:**
```csharp
public class CustomProcessor : ITelemetryProcessor
{
    private ITelemetryProcessor Next { get; set; }
    
    public CustomProcessor(ITelemetryProcessor next)
    {
        Next = next;
    }
    
    public void Process(ITelemetry item)
    {
        // Filter out health check requests
        if (item is RequestTelemetry request && 
            request.Url.AbsolutePath.Contains("/health"))
        {
            return; // Don't send to next processor
        }
        
        // Enrich telemetry
        item.Context.GlobalProperties["CustomProperty"] = "CustomValue";
        
        Next.Process(item);
    }
}
```

**3.x Equivalent (OpenTelemetry Activity Processor):**

First, create the processor:

```csharp
using System.Diagnostics;
using OpenTelemetry;

public class CustomActivityProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Filter out health check requests
        if (activity.DisplayName.Contains("/health") || 
            activity.GetTagItem("http.target")?.ToString()?.Contains("/health") == true)
        {
            // Set to not record this activity
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
            return;
        }
        
        // Enrich with custom properties
        activity.SetTag("custom.property", "CustomValue");
        activity.SetTag("environment", "Production");
        
        base.OnEnd(activity);
    }
}
```

Then register it in `Global.asax.cs`:

```csharp
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using OpenTelemetry;
using OpenTelemetry.Trace;

public class MvcApplication : System.Web.HttpApplication
{
    public static TelemetryClient TelemetryClient { get; private set; }
    
    protected void Application_Start()
    {
        // Create TelemetryConfiguration
        var configuration = TelemetryConfiguration.CreateDefault();
        
        // Configure OpenTelemetry with custom processor
        configuration.ConfigureOpenTelemetryBuilder(builder =>
        {
            builder.WithTracing(tracing =>
            {
                tracing.AddProcessor(new CustomActivityProcessor()); // Register custom processor
            });
        });
        
        // Create TelemetryClient with configured TelemetryConfiguration
        TelemetryClient = new TelemetryClient(configuration);
        
        // Set common properties
        TelemetryClient.Context.Cloud.RoleName = "MyWebApp";
    }
    
    protected void Application_End()
    {
        TelemetryClient?.Flush();
        System.Threading.Tasks.Task.Delay(1000).Wait();
    }
}
```

**Required NuGet Packages:**

The `Microsoft.ApplicationInsights.Web` package already includes the necessary OpenTelemetry dependencies. No additional packages are required for basic Activity Processor functionality.

**Key Differences:**
- **2.x**: Used `ITelemetryProcessor` and `ITelemetryInitializer` with Application Insights SDK
- **3.x**: Uses OpenTelemetry `BaseProcessor<Activity>` with Activity-based telemetry
- **Filtering**: In 2.x, return early; in 3.x, clear `ActivityTraceFlags.Recorded` flag
- **Enrichment**: In 2.x, set properties on `ITelemetry`; in 3.x, use `Activity.SetTag()`
- **Registration**: In 2.x, configured in `ApplicationInsights.config`; in 3.x, registered via OpenTelemetry SDK builder

> **Note:** For most scenarios, simple property enrichment using `TelemetryClient.Context` is sufficient. Only implement custom Activity Processors if you need complex filtering, sampling, or dynamic enrichment logic.

See the [BASE SDK documentation](../BASE/README.md#enriching-telemetry-with-activity-processors) for more examples of Activity Processors.

See [BreakingChanges.md](../../BreakingChanges.md) for additional migration details.

## Architecture

The 3.x SDK is built on [OpenTelemetry](https://opentelemetry.io/) and uses the following architecture:

```
┌─────────────────────────────────────────────────────────┐
│  ASP.NET Framework Web Application                      │
├─────────────────────────────────────────────────────────┤
│  HTTP Modules (ApplicationInsightsHttpModule)           │
│  ↓ Tracks incoming requests                             │
├─────────────────────────────────────────────────────────┤
│  TelemetryClient API (Compatibility Layer)              │
│  ↓ Translates to OpenTelemetry primitives               │
├─────────────────────────────────────────────────────────┤
│  OpenTelemetry SDK                                      │
│  • Activity (Traces/Spans)                              │
│  • LogRecord (Logs)                                     │
│  • Metrics                                              │
├─────────────────────────────────────────────────────────┤
│  OpenTelemetry Instrumentation                          │
│  • ASP.NET (HTTP requests)                              │
│  • SqlClient (SQL dependencies)                         │
│  • HttpClient (HTTP dependencies)                       │
├─────────────────────────────────────────────────────────┤
│  Activity Processors                                    │
│  • Enrichment (user, session, synthetic traffic)        │
│  • Filtering                                            │
│  • Sampling                                             │
├─────────────────────────────────────────────────────────┤
│  Azure Monitor Exporter                                 │
│  ↓ Converts to Application Insights schema             │
├─────────────────────────────────────────────────────────┤
│  Azure Monitor / Application Insights                   │
└─────────────────────────────────────────────────────────┘
```

**Key Components:**

- **HTTP Modules**: Intercept HTTP requests and responses for automatic tracking
- **OpenTelemetry Instrumentation**: Automatically collects HTTP requests, SQL dependencies, and outgoing HTTP calls
- **Activity Processors**: Enrich telemetry with web-specific context (user ID, session ID, synthetic traffic detection)
- **Azure Monitor Exporter**: Converts OpenTelemetry signals to Application Insights format and sends to Azure Monitor
- **TelemetryClient API**: Compatibility layer for sending custom telemetry

The SDK maintains backward compatibility with the TelemetryClient API while using OpenTelemetry internally for data collection and transmission.

## Troubleshooting

### Telemetry Not Appearing in Portal

1. **Verify Connection String**: Check that ApplicationInsights.config contains the correct connection string with both InstrumentationKey and IngestionEndpoint.

2. **Check HTTP Module Registration**: Ensure the HTTP modules are registered in web.config (should be added automatically by NuGet).

3. **Verify Network Connectivity**: Ensure your server can reach the ingestion endpoint (typically `https://*.in.applicationinsights.azure.com`).

4. **Check Application Pool Identity**: Ensure the application pool identity has network access to send telemetry.

5. **Review IIS Logs**: Check IIS logs for any errors during module initialization.

## Contributing

We strongly welcome and encourage contributions to this project. Please read the general [contributor's guide][ContribGuide] located in the ApplicationInsights-Home repository. If making a large change we request that you open an [issue][GitHubIssue] first.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

> **Note:** For classic Application Insights SDK (version 2.x), refer to the [2.x branch documentation](https://github.com/microsoft/ApplicationInsights-dotnet/tree/2.x).

[AILandingPage]: https://azure.microsoft.com/services/application-insights/
[AzurePortal]: https://portal.azure.com/
[ContribGuide]: https://github.com/Microsoft/ApplicationInsights-Home/blob/master/CONTRIBUTING.md
[GitHubIssue]: https://github.com/Microsoft/ApplicationInsights-dotnet/issues/
