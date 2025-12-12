## NuGet packages

- [Microsoft.ApplicationInsights.Web](https://www.nuget.org/packages/Microsoft.ApplicationInsights.Web/)
[![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.Web.svg)](https://nuget.org/packages/Microsoft.ApplicationInsights.Web)




# Application Insights SDK for Classic ASP.NET Web Applications

The code in this repository is the classic ASP.NET web application SDK for Application Insights. [Application Insights][AILandingPage] is a service that lets you monitor your live application's performance and usage. 

This SDK version (3.x) is built on OpenTelemetry and provides a compatibility layer (shim) that maintains the familiar Application Insights API while using OpenTelemetry instrumentation and Azure Monitor Exporter internally. The SDK automatically collects:

- **HTTP Requests** - Web request timings, success rates, and response codes
- **Dependencies** - SQL queries, HTTP calls to external services
- **Exceptions** - Unhandled exceptions and their stack traces  
- **Performance Counters** - Server CPU, memory, and other metrics
- **Custom Telemetry** - Events, traces, and metrics you log via the API

The SDK uses OpenTelemetry instrumentation libraries and Activity Processors internally to collect and enrich telemetry, then sends it to Azure Monitor via the Azure Monitor Exporter.

## Get the SDK

Install the SDK using NuGet:

```powershell
Install-Package Microsoft.ApplicationInsights.Web
```

The NuGet package will automatically:
- Add `ApplicationInsights.config` to your project root
- Register the `ApplicationInsightsHttpModule` in your web.config

## Configure the SDK

1. **Get a Connection String** from your Application Insights resource in the [Azure Portal][AzurePortal]

2. **Update ApplicationInsights.config** (created by NuGet) with your connection string:

```xml
<?xml version="1.0" encoding="utf-8"?>
<ApplicationInsights xmlns="http://schemas.microsoft.com/ApplicationInsights/2013/Settings">
  <ConnectionString>InstrumentationKey=YOUR-KEY;IngestionEndpoint=https://...</ConnectionString>
</ApplicationInsights>
```

3. **Verify web.config entries** (added automatically by NuGet):

The NuGet package adds the HTTP module registration. Your web.config should contain:

```xml
<system.web>
  <httpModules>
    <add name="ApplicationInsightsWebTracking" 
         type="Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web" />
  </httpModules>
</system.web>

<system.webServer>
  <validation validateIntegratedModeConfiguration="false" />
  <modules>
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

4. **Track custom telemetry** (optional):

```csharp
using Microsoft.ApplicationInsights;

var telemetry = new TelemetryClient();
telemetry.TrackEvent("UserLoggedIn");
telemetry.TrackException(exception);
```

The SDK automatically collects HTTP requests, dependencies, and exceptions. Custom telemetry can be sent using the `TelemetryClient` API.


## Upgrading from 2.x to 3.x

⚠️ **Version 3.x contains breaking changes.** The SDK now uses OpenTelemetry internally. Key changes:

- **Connection String required** - Replace `InstrumentationKey` with `ConnectionString` in ApplicationInsights.config
- **Simplified configuration** - No more telemetry modules or initializers in config file  
- **Automatic instrumentation** - HTTP, SQL, and dependency tracking is automatic via OpenTelemetry
- **Minimum .NET Framework 4.6.2** - Upgraded from 4.5.2

See [BreakingChanges.md](../../BreakingChanges.md) for detailed migration guidance.

### Migration Steps:

1. Update NuGet package: `Update-Package Microsoft.ApplicationInsights.Web`
2. Replace `InstrumentationKey` with `ConnectionString` in ApplicationInsights.config
3. Remove `<TelemetryInitializers>` and `<TelemetryModules>` sections from config (no longer supported)
4. Update minimum .NET Framework to 4.6.2 if needed
5. Test thoroughly - internal behavior has changed significantly

## To build
Follow [contributor's guide](https://github.com/Microsoft/ApplicationInsights-dotnet-server/blob/develop/CONTRIBUTING.md)

## Branches
- [master][master] contains the *latest* published release located on [NuGet][WebNuGet].
- [develop][develop] contains the code for the *next* release.

## Architecture

The 3.x SDK is built on [OpenTelemetry](https://opentelemetry.io/) and uses the following components:

- **OpenTelemetry Instrumentation** - Automatically collects HTTP requests, SQL dependencies, and outgoing HTTP calls
- **Activity Processors** - Internal processors that enrich telemetry with web-specific context (user, session, synthetic traffic detection)
- **Azure Monitor Exporter** - Converts OpenTelemetry signals to Application Insights format and sends to Azure Monitor
- **TelemetryClient API** - Compatibility layer for sending custom telemetry

The SDK maintains backward compatibility with the TelemetryClient API while using OpenTelemetry internally for data collection and transmission.

## Contributing

We strongly welcome and encourage contributions to this project. Please read the [contributor's guide](https://github.com/Microsoft/ApplicationInsights-dotnet-server/blob/develop/CONTRIBUTING.md). If making a large change we request that you open an [issue][GitHubIssue] first. If we agree that an issue is a bug, we'll add the "bug" label, and issues that we plan to fix are labeled with an iteration number. We follow the [Git Flow][GitFlow] approach to branching.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

[Azure]: https://azure.com/
[AILandingPage]: https://azure.microsoft.com/services/application-insights/
[AzurePortal]: https://portal.azure.com/
[WebDocumentation]: https://learn.microsoft.com/azure/azure-monitor/app/asp-net
[master]: https://github.com/Microsoft/ApplicationInsights-dotnet/tree/master/
[develop]: https://github.com/Microsoft/ApplicationInsights-dotnet/tree/develop/
[GitFlow]: http://nvie.com/posts/a-successful-git-branching-model/
[ContribGuide]: https://github.com/Microsoft/ApplicationInsights-dotnet/blob/develop/CONTRIBUTING.md
[GitHubIssue]: https://github.com/Microsoft/ApplicationInsights-dotnet/issues/
[WebNuGet]: https://www.nuget.org/packages/Microsoft.ApplicationInsights.Web/
