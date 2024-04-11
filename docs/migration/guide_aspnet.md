# Guide for Asp.Net applications

## Intro

The following guide details how to remove Application Insights from an ASP.NET web app and how to set up the OpenTelemetry SDK.

## Prerequisites

- An ASP.NET web application already instrumented with Application Insights
- A actively supported version of .NET (link)

## Steps to Migrate

### Step 1: Remove Application Insights SDK

When you first added ApplicationInsights to your project, the SDK would have added a config file and made some edits to the web.config.
If using Nuget tools to remove the Application Insights, some of this will be cleaned up. 
If you're manually removing the package reference from your csproj, you'll need to manually cleanup these artifacts.

- Remove any Microsoft.ApplicationInsights.* packages from you csproj and packages.config.
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

### Step 1: Install the OpenTelemetry SDK and Enable at Application Startup

The OpenTelemery SDK must be configured at application startup. This is typically done in the Global.asax.cs.
OpenTelemetry has a concept of three signals; Traces (Requests and Dependencies), Metrics, and Logs.
Each of these signals will need to be configured as part of your application startup.

#### Global.asax.cs

TODO: NEED A SAMPLE THAT INCLUDES LOGGING.

```csharp
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

public class MvcApplication : System.Web.HttpApplication
{
    private TracerProvider? tracerProvider;
    private MeterProvider? meterProvider;

    protected void Application_Start()
    {
        this.tracerProvider = Sdk.CreateTracerProviderBuilder()
            .Build();

        this.meterProvider = Sdk.CreateMeterProviderBuilder()
            .Build();
    }

    protected void Application_End()
    {
        this.tracerProvider?.Dispose();
        this.meterProvider?.Dispose();
    }
}
```

For more examples, see the following guides:
- OpenTelemetry SDK's getting started guide: https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry
- ASP.NET example project: https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/examples/AspNet/Global.asax.cs

### Step 1: Configure Instrumentation Libraries.

Instrumentation libraries can be added to your project to auto collect telemetry about specific components or dependencies.

- To collect telemetry for incoming requests, you should add the [OpenTelemetry.Instrumentation.AspNet](https://www.nuget.org/packages/OpenTelemetry.Instrumentation.AspNet) library to your application.
This includes adding a new reference to your Web.config and adding the Instrumentation to your OpenTelemetry SDK configuration.
A getting started guide is available here: https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.AspNet

- To collect telemetry for outbound http dependencies, you should add the [OpenTelemetry.Instrumentation.Http](https://www.nuget.org/packages/OpenTelemetry.Instrumentation.Http) library to your application.
A getting started guide is available here: https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Instrumentation.Http

### Step 1: Configure the Azure Monitor Exporter 

To send your telemetry to Application Insights, the Azure Monitor Exporter must be added to the configuration of all three signals.

See this doc for our getting started guide:
https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-enable?tabs=net
