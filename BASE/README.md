## NuGet packages

- [Microsoft.ApplicationInsights](https://www.nuget.org/packages/Microsoft.ApplicationInsights/)
[![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.svg)](https://www.nuget.org/packages/Microsoft.ApplicationInsights/)

# Application Insights for .NET

This repository has code for the base .NET SDK for Application Insights. [Application Insights][AILandingPage] is a service that allows developers ensure their application are available, performing, and succeeding. This SDK provides the base ability to send all Application Insights types from any .NET project. 

## Getting Started

If developing for a .Net project that is supported by one of our platform specific packages, [Web][WebGetStarted] or [Windows Apps][WinAppGetStarted], we strongly recommend to use one of those packages instead of this base library. If your project does not fall into one of those platforms you can use this library for any .Net code. This library should have no dependencies outside of the .Net framework. If you are building a [Desktop][DesktopGetStarted] or any other .Net project type this library will enable you to utilize Application Insights. More on SDK layering and extensibility [later](#sdk-layering).

### Get a Connection String

To use the Application Insights SDK you will need to provide it with a Connection String which can be [obtained from the portal][AIKey]. This Connection String will identify all the data flowing from your application instances as belonging to your account and specific application.

### Add the SDK library

We recommend consuming the library as a NuGet package. Make sure to look for the [Microsoft.ApplicationInsights][NuGetCore] package. Use the NuGet package manager to add a reference to your application code. 

### Initialize a TelemetryClient

The `TelemetryClient` object is the primary root object for the library. Almost all functionality around telemetry sending is located on this object. You must initialize an instance of this object with a TelemetryConfiguration that includes your Connection String to identify your data.

```C#
using Microsoft.ApplicationInsights;

var config = new TelemetryConfiguration();
config.ConnectionString = "InstrumentationKey=YOUR-KEY;IngestionEndpoint=https://...";
var tc = new TelemetryClient(config);
```

### Use the TelemetryClient to send telemetry

This "base" library does not provide any automatic telemetry collection or any automatic meta-data properties. You can populate common context on the `TelemetryClient.context` property which will be automatically attached to each telemetry item sent. You can also attach additional property data to each telemetry item sent. The `TelemetryClient` also exposes a number of `Track...()` methods that can be used to send all telemetry types understood by the Application Insights service. Some example use cases are shown below.

```C#
tc.Context.User.Id = Environment.GetUserName(); // This is probably a bad idea from a PII perspective.
tc.Context.Device.OperatingSystem = Environment.OSVersion.ToString();

tc.TrackEvent("PurchaseOrderSubmitted", new Dictionary<string, string>() { {"CouponCode", "JULY2015" } });
	
try
{
	...
}
catch(Exception e)
{
	tc.TrackException(e);
}
``` 

### Ensure you don't lose telemetry

This library uses OpenTelemetry with the Azure Monitor Exporter to send telemetry data. Telemetry is stored in an in-memory queue and batched before sending. As a result, if the process is terminated suddenly, you could lose telemetry that is stored in the queue but not yet sent. It is recommended to track the closing of your process and call the `TelemetryClient.Flush()` method to ensure no telemetry is lost.

### Full API Overview

Read about [how to use the API and see the results in the portal][api-overview].

## SDK layering

This repository builds the `Microsoft.ApplicationInsights` package. This package provides a compatibility layer (shim) over OpenTelemetry, maintaining the familiar Application Insights API while using OpenTelemetry and Azure Monitor Exporter internally for telemetry collection and transmission.

The Application Insights 3.x SDK is built on OpenTelemetry and defines the following layers: data collection, public API, enrichment, processing, and export.

**Data collection** is handled by OpenTelemetry instrumentation libraries. These libraries automatically collect telemetry for common scenarios like HTTP requests, database calls, and more. For example, the HTTP instrumentation creates Activities (spans) for outgoing HTTP calls, which are converted to Application Insights dependency telemetry.

**Enrichment and processing** is done through OpenTelemetry's extensibility model using Activity Processors and Resource Detectors. Activity Processors can modify or filter telemetry, while Resource Detectors add contextual information like cloud role, application version, and environment details.

**Export** is handled by the Azure Monitor Exporter, which converts OpenTelemetry signals (traces, metrics, logs) into the Application Insights data model and sends them to Azure Monitor. The exporter handles batching, retries, and reliable delivery.

Here is the Application Insights SDK layering and extensibility points:

| Layer | Extensibility |
|-------|---------------|
| **Data Collection** | Use OpenTelemetry instrumentation libraries or manually instrument code using the TelemetryClient API |
| **Public API** | Track [custom operations](https://docs.microsoft.com/azure/application-insights/application-insights-custom-operations-tracking) and other [telemetry](https://docs.microsoft.com/azure/application-insights/app-insights-api-custom-events-metrics) using TelemetryClient |
| **Enrichment** | Use OpenTelemetry Resource Detectors to add contextual information, or Activity Processors to enrich telemetry |
| **Processing** | Configure sampling through Azure Monitor Exporter options, or create custom Activity Processors to filter/modify telemetry |
| **Export** | Data is sent to Azure Monitor via the Azure Monitor Exporter. Configure [EventFlow](https://github.com/Azure/diagnostics-eventflow) for alternative destinations |

Packages like `Microsoft.ApplicationInsights.Web` or `Microsoft.ApplicationInsights.AspNetCore` configure OpenTelemetry instrumentation and the Azure Monitor Exporter automatically. The SDK uses `TelemetryConfiguration.ConfigureOpenTelemetryBuilder()` to allow customization of the underlying OpenTelemetry pipeline.  

## Branches

- [master][master] contains the *latest* published release located on [NuGet][NuGetCore].
- [develop][develop] contains the code for the *next* release. 

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
