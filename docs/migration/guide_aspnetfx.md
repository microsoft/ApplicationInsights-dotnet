# Guide for Asp.Net applications

## Intro

The following guide details how to remove Application Insights from an ASP.NET web app and how to set up the OpenTelemetry SDK.

## Prerequisites

- An ASP.NET web application
- A actively supported version of .NET (link)

## Steps to ...

### Step 1: Remove Application Insights SDK

When you first added ApplicationInsights to your project, the SDK would have added a config file and made some edits to the web.config.
If using Nuget tools to remove the Application Insights, some of this will be cleaned up. If you're manually removing the package reference from your csproj, 

- Remove any Microsoft.ApplicationInsights.* packages.
- Delete the ApplicationInsights.config file.
- Clean up your application's Web.Config file.

    The following sections would have been automatically added to your web.config when you first added ApplicationInsights to your project. References to the `TelemetryCorrelationHttpModule` and the `ApplicationInsightsWebTracking` should be removed.

    ```xml
    <configuration>
      <system.web>
        <httpModules>
          <add name="TelemetryCorrelationHttpModule" type="Microsoft.AspNet.TelemetryCorrelation.TelemetryCorrelationHttpModule, Microsoft.AspNet.TelemetryCorrelation" />
          <add name="ApplicationInsightsWebTracking" type="Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web" />
        </httpModules>
      </system.web>
      <system.webServer>
        <modules>
          <remove name="TelemetryCorrelationHttpModule" />
          <add name="TelemetryCorrelationHttpModule" type="Microsoft.AspNet.TelemetryCorrelation.TelemetryCorrelationHttpModule, Microsoft.AspNet.TelemetryCorrelation" preCondition="managedHandler" />
          <remove name="ApplicationInsightsWebTracking" />
          <add name="ApplicationInsightsWebTracking" type="Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web" preCondition="managedHandler" />
        </modules>
      </system.webServer>
    </configuration>
    ```

### Step 1: Install the OpenTelemetry SDK and Enable at application startup

The OpenTelemery SDK must be configured at application startup. This is typically done in the Global.asax.cs.
OpenTelemetry has a concept of three signals; Traces (Requests and Dependencies), Metrics, and Logs.
Each of these signals will need to be configured as part of your application startup.

There is a full getting started for the OpenTelemetry SDK guide here: https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry

See also this ASP.NET example project here: https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/examples/AspNet/Global.asax.cs

TODO: NEED A SAMPLE THAT INCLUDES LOGGING.

### Step 1: Configure instrumentation libraries.

Instrumentation libraries can be added to your project to auto collect telemetry about specific components or dependencies.

To collect telemetry for incoming requests, you should add the OpenTelemetry.Instrumentation.AspNet library to your application.
A full guide for adding AspNet instrumentation is available here: https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.AspNet
This includes adding a new reference to your Web.config and adding the Instrumentation to your OpenTelemetry SDK configuration.

```
dotnet add package OpenTelemetry.Instrumentation.AspNet
```

### Step 1: Configure the AzureMonitor exporter 

To send your telemetry to AzureMonitor, the AzureMonitor exporter must be added to the configuration of all three signals.

See this doc for our getting started guide:
https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-enable?tabs=net
