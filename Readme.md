# Please pardon our progress
We are currently in the process of consolidating our repos.
This Readme will be updated as we make significant changes.

As of October 25, The contents of each former repo can be found in the sub folders; BASE, WEB, LOGGING, NETCORE.


## NuGet packages

**Base SDKs**
- [Microsoft.ApplicationInsights](https://www.nuget.org/packages/Microsoft.ApplicationInsights/)
[![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.svg)](https://www.nuget.org/packages/Microsoft.ApplicationInsights/)
- [Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel](https://www.nuget.org/packages/Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel)
[![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.svg)](https://www.nuget.org/packages/Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel/)

**Web SDKs**
- [Microsoft.ApplicationInsights.Web](https://www.nuget.org/packages/Microsoft.ApplicationInsights.Web/)
[![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.Web.svg)](https://nuget.org/packages/Microsoft.ApplicationInsights.Web)
- [Microsoft.ApplicationInsights.DependencyCollector](https://www.nuget.org/packages/Microsoft.ApplicationInsights.DependencyCollector/)
[![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.DependencyCollector.svg)](https://nuget.org/packages/Microsoft.ApplicationInsights.DependencyCollector)
- [Microsoft.ApplicationInsights.EventCounterCollector](https://www.nuget.org/packages/Microsoft.ApplicationInsights.EventCounterCollector)
[![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.EventCounterCollector.svg)](https://nuget.org/packages/Microsoft.ApplicationInsights.EventCounterCollector)
- [Microsoft.ApplicationInsights.PerfCounterCollector](https://www.nuget.org/packages/Microsoft.ApplicationInsights.PerfCounterCollector/)
[![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.PerfCounterCollector.svg)](https://nuget.org/packages/Microsoft.ApplicationInsights.PerfCounterCollector)
- [Microsoft.ApplicationInsights.WindowsServer](https://www.nuget.org/packages/Microsoft.ApplicationInsights.WindowsServer/)
[![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.WindowsServer.svg)](https://nuget.org/packages/Microsoft.ApplicationInsights.WindowsServer)
- [Microsoft.AspNet.ApplicationInsights.HostingStartup](https://www.nuget.org/packages/Microsoft.AspNet.ApplicationInsights.HostingStartup/)
[![Nuget](https://img.shields.io/nuget/vpre/Microsoft.AspNet.ApplicationInsights.HostingStartup.svg)](https://nuget.org/packages/Microsoft.AspNet.ApplicationInsights.HostingStartup)

**Logging Adapters**
- For ILogger:
 [Microsoft.Extensions.Logging.ApplicationInsights](https://www.nuget.org/packages/Microsoft.Extensions.Logging.ApplicationInsights/)
[![Nuget](https://img.shields.io/nuget/vpre/Microsoft.Extensions.Logging.ApplicationInsights.svg)](https://www.nuget.org/packages/Microsoft.Extensions.Logging.ApplicationInsights/)
- For NLog:
 [Microsoft.ApplicationInsights.NLogTarget](http://www.nuget.org/packages/Microsoft.ApplicationInsights.NLogTarget/)
[![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.NLogTarget.svg)](https://www.nuget.org/packages/Microsoft.ApplicationInsights.NLogTarget/)
- For Log4Net: [Microsoft.ApplicationInsights.Log4NetAppender](http://www.nuget.org/packages/Microsoft.ApplicationInsights.Log4NetAppender/)
[![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.Log4NetAppender.svg)](https://www.nuget.org/packages/Microsoft.ApplicationInsights.Log4NetAppender/)
- For System.Diagnostics: [Microsoft.ApplicationInsights.TraceListener](http://www.nuget.org/packages/Microsoft.ApplicationInsights.TraceListener/)
[![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.TraceListener.svg)](https://www.nuget.org/packages/Microsoft.ApplicationInsights.TraceListener/)
- [Microsoft.ApplicationInsights.DiagnosticSourceListener](http://www.nuget.org/packages/Microsoft.ApplicationInsights.DiagnosticSourceListener/)
[![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.DiagnosticSourceListener.svg)](https://www.nuget.org/packages/Microsoft.ApplicationInsights.DiagnosticSourceListener/)
- [Microsoft.ApplicationInsights.EtwCollector](http://www.nuget.org/packages/Microsoft.ApplicationInsights.EtwCollector/)
[![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.EtwCollector.svg)](https://www.nuget.org/packages/Microsoft.ApplicationInsights.EtwCollector/)
- [Microsoft.ApplicationInsights.EventSourceListener](http://www.nuget.org/packages/Microsoft.ApplicationInsights.EventSourceListener/)
[![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.EventSourceListener.svg)](https://www.nuget.org/packages/Microsoft.ApplicationInsights.EventSourceListener/)

**NetCore SDKs**
- [Microsoft.ApplicationInsights.AspNetCore](https://www.nuget.org/packages/Microsoft.ApplicationInsights.AspNetCore/)
[![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.AspNetCore.svg)](https://nuget.org/packages/Microsoft.ApplicationInsights.AspNetCore)
- [Microsoft.ApplicationInsights.WorkerService](https://www.nuget.org/packages/Microsoft.ApplicationInsights.WorkerService/)
[![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.WorkerService.svg)](https://nuget.org/packages/Microsoft.ApplicationInsights.WorkerService)









# Application Insights for .NET

This repository has code for the base .NET SDK for Application Insights. [Application Insights][AILandingPage] is a service that allows developers ensure their application are available, performing, and succeeding. This SDK provides the base ability to send all Application Insights types from any .NET project. 

## Getting Started

If developing for a .Net project that is supported by one of our platform specific packages, [Web][WebGetStarted] or [Windows Apps][WinAppGetStarted], we strongly recommend to use one of those packages instead of this base library. If your project does not fall into one of those platforms you can use this library for any .Net code. This library should have no dependencies outside of the .Net framework. If you are building a [Desktop][DesktopGetStarted] or any other .Net project type this library will enable you to utilize Application Insights. More on SDK layering and extensibility [later](#sdk-layering).

### Get an Instrumentation Key

To use the Application Insights SDK you will need to provide it with an Instrumentation Key which can be [obtained from the portal][AIKey]. This Instrumentation Key will identify all the data flowing from your application instances as belonging to your account and specific application.

### Add the SDK library

We recommend consuming the library as a NuGet package. Make sure to look for the [Microsoft.ApplicationInsights][NuGetCore] package. Use the NuGet package manager to add a reference to your application code. 

### Initialize a TelemetryClient

The `TelemetryClient` object is the primary root object for the library. Almost all functionality around telemetry sending is located on this object. You must initialize an instance of this object and populate it with your Instrumentation Key to identify your data.

```C#
using Microsoft.ApplicationInsights;

var tc = new TelemetryClient();
tc.InstrumentationKey = "INSERT YOUR KEY";
```

### Use the TelemetryClient to send telemetry

This "base" library does not provide any automatic telemetry collection or any automatic meta-data properties. You can populate common context on the `TelemetryClient.context` property which will be automatically attached to each telemetry item sent. You can also attach additional property data to each telemetry item sent. The `TelemetryClient` also exposes a number of `Track...()` methods that can be used to send all telemetry types understood by the Application Insights service. Some example use cases are shown below.

```C#
tc.Context.User.Id = Environment.GetUserName(); // This is probably a bad idea from a PII perspective.
tc.Context.Device.OperatingSystem = Environment.OSVersion.ToString();

tc.TrackPageView("Form1");

tc.TrackEvent("PurchaseOrderSubmitted", new Dictionary<string, string>() { {"CouponCode", "JULY2015" } }, new Dictionary<string, double>() { {"OrderTotal", 68.99 }, {"ItemsOrdered", 5} });
	
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

This library makes use of the InMemoryChannel to send telemetry data. This is a very lightweight channel implementation. It stores all telemetry to an in-memory queue and batches and sends telemetry. As a result, if the process is terminated suddenly, you could lose telemetry that is stored in the queue but not yet sent. It is recommended to track the closing of your process and call the `TelemetryClient.Flush()` method to ensure no telemetry is lost.

### Full API Overview

Read about [how to use the API and see the results in the portal][api-overview].

## Branches

- [master][master] contains the *latest* published release located on [NuGet][NuGetCore].
- [develop][develop] contains the code for the *next* release. 

## Contributing

We strongly welcome and encourage contributions to this project. Please read the general [contributor's guide][ContribGuide] located in the ApplicationInsights-Home repository and the [contributing guide](https://github.com/Microsoft/ApplicationInsights-dotnet/blob/develop/.github/CONTRIBUTING.md)  for this SDK. If making a large change we request that you open an [issue][GitHubIssue] first. We follow the [Git Flow][GitFlow] approach to branching. 

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

[AILandingPage]: http://azure.microsoft.com/services/application-insights/
[api-overview]: https://azure.microsoft.com/documentation/articles/app-insights-api-custom-events-metrics/
[ContribGuide]: https://github.com/Microsoft/ApplicationInsights-Home/blob/master/CONTRIBUTING.md
[GitFlow]: http://nvie.com/posts/a-successful-git-branching-model/
[GitHubIssue]: https://github.com/Microsoft/ApplicationInsights-dotnet/issues
[master]: https://github.com/Microsoft/ApplicationInsights-dotnet/tree/master
[develop]: https://github.com/Microsoft/ApplicationInsights-dotnet/tree/development
[NuGetCore]: https://www.nuget.org/packages/Microsoft.ApplicationInsights
[WebGetStarted]: https://azure.microsoft.com/documentation/articles/app-insights-start-monitoring-app-health-usage/
[WinAppGetStarted]: https://azure.microsoft.com/documentation/articles/app-insights-windows-get-started/
[DesktopGetStarted]: https://azure.microsoft.com/documentation/articles/app-insights-windows-desktop/
[AIKey]: https://github.com/Microsoft/ApplicationInsights-Home/wiki#getting-an-application-insights-instrumentation-key

## Release Schedule

The following is our tentative release schedule for 2020.

| **2019H2**  	|       	|   	|            	|             	|
|-----------	|-------	|---	|------------	|-------------	|
| December  	| Early 	|   	|            	| 2.12 Stable 	|
|           	| Mid   	|   	| 2.13 Beta1 	|             	|
| **2020H1**   	|       	|   	|            	|             	|
| January   	| Early 	|   	| 2.13 Beta2 	|             	|
|           	| Mid   	|   	| 2.13 Beta3 	|             	|
| February  	| Early 	|   	|            	| 2.13 Stable 	|
|           	| Mid   	|   	| 2.14 Beta1 	|             	|
| March     	| Early 	|   	| 2.14 Beta2 	|             	|
|           	| Mid   	|   	| 2.14 Beta3 	|             	|
| April     	| Early 	|   	|            	| 2.14 Stable 	|
|           	| Mid   	|   	| 2.15 Beta1 	|             	|
| May       	| Early 	|   	| 2.15 Beta2 	|             	|
|           	| Mid   	|   	| 2.15 Beta3 	|             	|
| June      	| Early 	|   	|            	| 2.15 Stable 	|
|           	| Mid   	|   	| 2.16 Beta1 	|             	|
| **2020H2**   	|       	|   	|            	|             	|
| July      	| Early 	|   	| 2.16 Beta2 	|             	|
|           	| Mid   	|   	| 2.16 Beta3 	|             	|
| August    	| Early 	|   	|            	| 2.16 Stable 	|
|           	| Mid   	|   	| 2.17 Beta1 	|             	|
| September 	| Early 	|   	| 2.17 Beta2 	|             	|
|           	| Mid   	|   	| 2.17 Beta3 	|             	|
| October   	| Early 	|   	|            	| 2.17 Stable 	|
|           	| Mid   	|   	| 2.18 Beta1 	|             	|
| November  	| Early 	|   	| 2.18 Beta2 	|             	|
|           	| Mid   	|   	| 2.18 Beta3 	|             	|
| December  	| Early 	|   	|            	| 2.18 Stable 	|
|           	| Mid   	|   	| 2.19 Beta1 	|             	|
