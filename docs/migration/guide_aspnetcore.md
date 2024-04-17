# Guide for ASP.NET CORE applications

## Intro

The following guide details how to migrate from Application Insights SDK to OpenTelemetry based solution for an ASP.NET CORE application.

## Prerequisites

- An ASP.NET CORE application already instrumented with Application Insights
- A actively supported version of .NET (link)

## Steps to Migrate

### Step 1: Remove Application Insights SDK

- Remove any Microsoft.ApplicationInsights.* packages from you csproj and packages.config.
- Delete the ApplicationInsights.config file (if it exists).

### Step 1: Install the OpenTelemetry SDK and Enable at Application Startup

The OpenTelemery SDK must be configured at application startup. This is typically done in the `Global.asax.cs`.
OpenTelemetry has a concept of three signals; Traces (Requests and Dependencies), Metrics, and Logs.
Each of these signals will need to be configured as part of your application startup.

#### Program.cs or Startup.cs

Your application startup may be in a different file depending on if you have an older application or if you're using the minimal api.

Whichever file you use to configure your `ServiceCollection`, you will start here and use `AddOpenTelemetry` to 


```csharp
var appBuilder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .WithTracing(builder => {
      // Tracing specific configuration
    })
    .WithMetrics(builder => {
      // Metrics specific configuration
    })

appBuilder.Logging.AddOpenTelemetry(options => {
    // Logging specific configuration
});

// Build the application.
var app = builder.Build();

// Run the application.
app.Run();
```

For more examples, see the following guides:
- OpenTelemetry SDK's getting started guide: https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry
- ASP.NET Core example project: https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/examples/AspNetCore/Program.cs


### Step 1: Configure the Azure Monitor Distro 

TODO: REWRITE THIS INTRO

To send your telemetry to Application Insights, the Azure Monitor Exporter must be added to the configuration of all three signals.

See this doc for our getting started guide:
https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-enable?tabs=aspnetcore


### Step 1: Configure Instrumentation Libraries.

By default the Azure Monitor Distro includes some Instrumentation Libraries that we think will be applicable to your application.

TODO: These can be configured via.....


```csharp

// TODO: CODE SAMPLE

```




Any additional Instrumenattion Libraries can be added to your project to auto collect telemetry about specific components or dependencies.
