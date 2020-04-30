# Application Insights for .NET
This is the .NET SDK for [Azure Monitor](https://docs.microsoft.com/azure/azure-monitor/overview) & [Application Insights](https://docs.microsoft.com/azure/azure-monitor/app/app-insights-overview).



## Getting Started
Please review our How-to guides to review which packages are appropriate for your project:
- [.NET Console](https://docs.microsoft.com/azure/azure-monitor/app/console)
- [ASP.NET](https://docs.microsoft.com/azure/azure-monitor/app/asp-net)
- [ASP.NET Core](https://docs.microsoft.com/azure/azure-monitor/app/asp-net-core)
- [ILogger](https://docs.microsoft.com/azure/azure-monitor/app/ilogger)
- [WorkerService](https://docs.microsoft.com/azure/azure-monitor/app/worker-service)




## Understanding our SDK
We've gathered a list of concepts, code examples, and links to full guides [here](.docs\concepts.md).



## NuGet packages
The following packages are published from this repository: 

|                                                                                                                                                               	| Nightly Build                                                                                                                                                                                                                                                                                         | Official Release                                                                                                                                                                                              |
|---------------------------------------------------------------------------------------------------------------------------------------------------------------	|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------	|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------	|
| **Base SDKs**                                                                                                                                                 	|                                                                                                                                                                                                                                                                                                      	|                                                                                                                                                                                                              	|
| - [Microsoft.ApplicationInsights](https://www.nuget.org/packages/Microsoft.ApplicationInsights/)                                                              	| [![Nightly](https://img.shields.io/myget/applicationinsights-dotnet-nightly/v/Microsoft.ApplicationInsights?label=)](https://www.myget.org/feed/applicationinsights-dotnet-nightly/package/nuget/Microsoft.ApplicationInsights)                                                                   	| [![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.svg)](https://www.nuget.org/packages/Microsoft.ApplicationInsights/)                                                               	|
| - [Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel](https://www.nuget.org/packages/Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel) 	| [![Nightly](https://img.shields.io/myget/applicationinsights-dotnet-nightly/v/Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel?label=)](https://www.myget.org/feed/applicationinsights-dotnet-nightly/package/nuget/Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel)     	| [![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.svg)](https://www.nuget.org/packages/Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel/) 	|
| **Web SDKs**                                                                                                                                                  	|                                                                                                                                                                                                                                                                                                      	|                                                                                                                                                                                                              	|
| - [Microsoft.ApplicationInsights.Web](https://www.nuget.org/packages/Microsoft.ApplicationInsights.Web/)                                                      	| [![Nightly](https://img.shields.io/myget/applicationinsights-dotnet-nightly/v/Microsoft.ApplicationInsights.Web?label=)](https://www.myget.org/feed/applicationinsights-dotnet-nightly/package/nuget/Microsoft.ApplicationInsights.Web)                                                       	    | [![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.Web.svg)](https://nuget.org/packages/Microsoft.ApplicationInsights.Web)                                                            	|
| - [Microsoft.ApplicationInsights.DependencyCollector](https://www.nuget.org/packages/Microsoft.ApplicationInsights.DependencyCollector/)                      	| [![Nightly](https://img.shields.io/myget/applicationinsights-dotnet-nightly/v/Microsoft.ApplicationInsights.DependencyCollector?label=)](https://www.myget.org/feed/applicationinsights-dotnet-nightly/package/nuget/Microsoft.ApplicationInsights.DependencyCollector)                           	| [![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.DependencyCollector.svg)](https://nuget.org/packages/Microsoft.ApplicationInsights.DependencyCollector)                            	|
| - [Microsoft.ApplicationInsights.EventCounterCollector](https://www.nuget.org/packages/Microsoft.ApplicationInsights.EventCounterCollector)                   	| [![Nightly](https://img.shields.io/myget/applicationinsights-dotnet-nightly/v/Microsoft.ApplicationInsights.EventCounterCollector?label=)](https://www.myget.org/feed/applicationinsights-dotnet-nightly/package/nuget/Microsoft.ApplicationInsights.EventCounterCollector)                   	    | [![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.EventCounterCollector.svg)](https://nuget.org/packages/Microsoft.ApplicationInsights.EventCounterCollector)                        	|
| - [Microsoft.ApplicationInsights.PerfCounterCollector](https://www.nuget.org/packages/Microsoft.ApplicationInsights.PerfCounterCollector/)                    	| [![Nightly](https://img.shields.io/myget/applicationinsights-dotnet-nightly/v/Microsoft.ApplicationInsights.PerfCounterCollector?label=)](https://www.myget.org/feed/applicationinsights-dotnet-nightly/package/nuget/Microsoft.ApplicationInsights.PerfCounterCollector)                         	| [![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.PerfCounterCollector.svg)](https://nuget.org/packages/Microsoft.ApplicationInsights.PerfCounterCollector)                          	|
| - [Microsoft.ApplicationInsights.WindowsServer](https://www.nuget.org/packages/Microsoft.ApplicationInsights.WindowsServer/)                                  	| [![Nightly](https://img.shields.io/myget/applicationinsights-dotnet-nightly/v/Microsoft.ApplicationInsights.WindowsServer?label=)](https://www.myget.org/feed/applicationinsights-dotnet-nightly/package/nuget/Microsoft.ApplicationInsights.WindowsServer)                                       	| [![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.WindowsServer.svg)](https://nuget.org/packages/Microsoft.ApplicationInsights.WindowsServer)                                        	|
| - [Microsoft.AspNet.ApplicationInsights.HostingStartup](https://www.nuget.org/packages/Microsoft.AspNet.ApplicationInsights.HostingStartup/)                  	| [![Nightly](https://img.shields.io/myget/applicationinsights-dotnet-nightly/v/Microsoft.AspNet.ApplicationInsights.HostingStartup?label=)](https://www.myget.org/feed/applicationinsights-dotnet-nightly/package/nuget/Microsoft.AspNet.ApplicationInsights.HostingStartup)                       	| [![Nuget](https://img.shields.io/nuget/vpre/Microsoft.AspNet.ApplicationInsights.HostingStartup.svg)](https://nuget.org/packages/Microsoft.AspNet.ApplicationInsights.HostingStartup)                        	|
| **NetCore SDKs**                                                                                                                                              	|                                                                                                                                                                                                                                                                                                      	|                                                                                                                                                                                                              	|
| - [Microsoft.ApplicationInsights.AspNetCore](https://www.nuget.org/packages/Microsoft.ApplicationInsights.AspNetCore/)                                        	| [![Nightly](https://img.shields.io/myget/applicationinsights-dotnet-nightly/v/Microsoft.ApplicationInsights.AspNetCore?label=)](https://www.myget.org/feed/applicationinsights-dotnet-nightly/package/nuget/Microsoft.ApplicationInsights.AspNetCore)                                         	    | [![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.AspNetCore.svg)](https://nuget.org/packages/Microsoft.ApplicationInsights.AspNetCore)                                              	|
| - [Microsoft.ApplicationInsights.WorkerService](https://www.nuget.org/packages/Microsoft.ApplicationInsights.WorkerService/)                                  	| [![Nightly](https://img.shields.io/myget/applicationinsights-dotnet-nightly/v/Microsoft.ApplicationInsights.WorkerService?label=)](https://www.myget.org/feed/applicationinsights-dotnet-nightly/package/nuget/Microsoft.ApplicationInsights.WorkerService)                                       	| [![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.WorkerService.svg)](https://nuget.org/packages/Microsoft.ApplicationInsights.WorkerService)                                        	|
| **Logging Adapters**                                                                                                                                          	|                                                                                                                                                                                                                                                                                                      	|                                                                                                                                                                                                              	|
| - For ILogger:  [Microsoft.Extensions.Logging.ApplicationInsights](https://www.nuget.org/packages/Microsoft.Extensions.Logging.ApplicationInsights/)          	| [![Nightly](https://img.shields.io/myget/applicationinsights-dotnet-nightly/v/Microsoft.Extensions.Logging.ApplicationInsights?label=)](https://www.myget.org/feed/applicationinsights-dotnet-nightly/package/nuget/Microsoft.Extensions.Logging.ApplicationInsights)                             	| [![Nuget](https://img.shields.io/nuget/vpre/Microsoft.Extensions.Logging.ApplicationInsights.svg)](https://www.nuget.org/packages/Microsoft.Extensions.Logging.ApplicationInsights/)                         	|
| - For NLog:  [Microsoft.ApplicationInsights.NLogTarget](http://www.nuget.org/packages/Microsoft.ApplicationInsights.NLogTarget/)                              	| [![Nightly](https://img.shields.io/myget/applicationinsights-dotnet-nightly/v/Microsoft.ApplicationInsights.NLogTarget?label=)](https://www.myget.org/feed/applicationinsights-dotnet-nightly/package/nuget/Microsoft.ApplicationInsights.NLogTarget)                                             	| [![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.NLogTarget.svg)](https://www.nuget.org/packages/Microsoft.ApplicationInsights.NLogTarget/)                                         	|
| - For Log4Net: [Microsoft.ApplicationInsights.Log4NetAppender](http://www.nuget.org/packages/Microsoft.ApplicationInsights.Log4NetAppender/)                  	| [![Nightly](https://img.shields.io/myget/applicationinsights-dotnet-nightly/v/Microsoft.ApplicationInsights.Log4NetAppender?label=)](https://www.myget.org/feed/applicationinsights-dotnet-nightly/package/nuget/Microsoft.ApplicationInsights.Log4NetAppender)                                   	| [![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.Log4NetAppender.svg)](https://www.nuget.org/packages/Microsoft.ApplicationInsights.Log4NetAppender/)                               	|
| - For System.Diagnostics: [Microsoft.ApplicationInsights.TraceListener](http://www.nuget.org/packages/Microsoft.ApplicationInsights.TraceListener/)           	| [![Nightly](https://img.shields.io/myget/applicationinsights-dotnet-nightly/v/Microsoft.ApplicationInsights.TraceListener?label=)](https://www.myget.org/feed/applicationinsights-dotnet-nightly/package/nuget/Microsoft.ApplicationInsights.TraceListener)                                       	| [![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.TraceListener.svg)](https://www.nuget.org/packages/Microsoft.ApplicationInsights.TraceListener/)                                   	|
| - [Microsoft.ApplicationInsights.DiagnosticSourceListener](http://www.nuget.org/packages/Microsoft.ApplicationInsights.DiagnosticSourceListener/)             	| [![Nightly](https://img.shields.io/myget/applicationinsights-dotnet-nightly/v/Microsoft.ApplicationInsights.DiagnosticSourceListener?label=)](https://www.myget.org/feed/applicationinsights-dotnet-nightly/package/nuget/Microsoft.ApplicationInsights.DiagnosticSourceListener)                 	| [![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.DiagnosticSourceListener.svg)](https://www.nuget.org/packages/Microsoft.ApplicationInsights.DiagnosticSourceListener/)             	|
| - [Microsoft.ApplicationInsights.EtwCollector](http://www.nuget.org/packages/Microsoft.ApplicationInsights.EtwCollector/)                                     	| [![Nightly](https://img.shields.io/myget/applicationinsights-dotnet-nightly/v/Microsoft.ApplicationInsights.EtwCollector?label=)](https://www.myget.org/feed/applicationinsights-dotnet-nightly/package/nuget/Microsoft.ApplicationInsights.EtwCollector)                                         	| [![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.EtwCollector.svg)](https://www.nuget.org/packages/Microsoft.ApplicationInsights.EtwCollector/)                                     	|
| - [Microsoft.ApplicationInsights.EventSourceListener](http://www.nuget.org/packages/Microsoft.ApplicationInsights.EventSourceListener/)                       	| [![Nightly](https://img.shields.io/myget/applicationinsights-dotnet-nightly/v/Microsoft.ApplicationInsights.EventSourceListener?label=)](https://www.myget.org/feed/applicationinsights-dotnet-nightly/package/nuget/Microsoft.ApplicationInsights.EventSourceListener)                           	| [![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.EventSourceListener.svg)](https://www.nuget.org/packages/Microsoft.ApplicationInsights.EventSourceListener/)                       	|


Nightly Builds are available on our MyGet feed:
`https://www.myget.org/F/applicationinsights-dotnet-nightly/api/v3/index.json`
These builds come from the develop branch. These are not signed and are not intended for production workloads.



## Branches
- [master][https://github.com/Microsoft/ApplicationInsights-dotnet/tree/master] contains the *latest* published release located on [NuGet](https://www.nuget.org/packages/Microsoft.ApplicationInsights).
- [develop][https://github.com/Microsoft/ApplicationInsights-dotnet/tree/develop] contains the code for the *next* release. 



## Contributing
We strongly welcome and encourage contributions to this project. 

Please review our [Contributing guide](.github\CONTRIBUTING.md).



## Release Schedule
The following is our tentative release schedule for 2020.

| **Release Schedule** |       	|   	|            	|             	|
|-----------	|-------	|---	|------------	|-------------	|
| **2020H1**   	|       	|   	|            	|             	|
| January   	| Early 	|   	| 2.13 Beta2 	|             	|
|           	| Mid   	|   	| 2.13 Beta3 	|             	|
| February  	| Early 	|   	|            	| 2.13 Stable 	|
|           	| Mid   	|   	| 2.14 Beta1 	|             	|
| March     	| Early 	|   	| 2.14 Beta2 	|             	|
|           	| Mid   	|   	| 2.14 Beta3 	|             	|
| April     	| Early 	|   	|            	| 2.14 Stable 	|
|           	| Mid   	|   	|           	|             	|
| May       	| Early 	|   	| 2.15 Beta1 	|             	|
|           	| Mid   	|   	| 2.15 Beta2 	|             	|
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
