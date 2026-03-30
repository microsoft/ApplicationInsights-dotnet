# ASP.NET Classic (.NET Framework) — Greenfield Setup

## Prerequisites

- .NET Framework 4.6.2+ project (ASP.NET MVC, WebForms, or generic ASP.NET)
- Visual Studio or MSBuild for building

## Step 1: Install the NuGet Package

In Visual Studio, open the **Package Manager Console** and run:

```
Install-Package Microsoft.ApplicationInsights.Web
```

This automatically:
- Creates `ApplicationInsights.config` with default 3.x settings
- Adds `ApplicationInsightsHttpModule` and `TelemetryHttpModule` to `Web.config`
- Adds all required assembly references
- Updates `packages.config`

## Step 2: Set Connection String

In `ApplicationInsights.config`, replace the placeholder with your connection string:

```xml
<ConnectionString>InstrumentationKey=your-key;IngestionEndpoint=https://dc.applicationinsights.azure.com/</ConnectionString>
```

Or set the `APPLICATIONINSIGHTS_CONNECTION_STRING` environment variable.

## Step 3: Run and Verify

1. Build and run the application (F5 in Visual Studio)
2. Make a few HTTP requests
3. Check Azure Portal → Application Insights → Live Metrics
4. Check Transaction Search for request and dependency data

## What's Collected Automatically

- All incoming HTTP requests (path, status code, duration)
- SQL and HTTP outgoing dependency calls
- Unhandled exceptions
- Performance counters (CPU, memory, request rate)
- Live Metrics stream

## Optional: Custom Telemetry

```csharp
var client = new TelemetryClient(TelemetryConfiguration.CreateDefault());
client.TrackEvent("OrderCreated");
client.TrackMetric("ProcessingTime", elapsed);
```

## Optional: Custom Processors

In `Global.asax.cs`:

```csharp
protected void Application_Start()
{
    var config = TelemetryConfiguration.CreateDefault();
    config.ConnectionString = "InstrumentationKey=...;IngestionEndpoint=...";
    config.ConfigureOpenTelemetryBuilder(otel =>
    {
        otel.WithTracing(tracing => tracing.AddProcessor<MyCustomProcessor>());
    });

    AreaRegistration.RegisterAllAreas();
    RouteConfig.RegisterRoutes(RouteTable.Routes);
}
```
