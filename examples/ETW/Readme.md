# Event Tracing for Windows (ETW)

Event Tracing for Windows (ETW) provides application programmers the ability to start and stop event tracing sessions, instrument an application to provide trace events, and consume trace events. Trace events contain an event header and provider-defined data that describes the current state of an application or operation. You can use the events to debug an application and perform capacity and performance analysis. [Source](https://docs.microsoft.com/windows/desktop/etw/event-tracing-portal)

The Application Insights .NET products use ETW to track exceptions and custom errors within our products.


## EventSources

Logs are emitted from [EventSource](https://docs.microsoft.com/dotnet/api/system.diagnostics.tracing.eventsource?view=netframework-4.8) classes.

Vance Morrison's blog has several articles for getting started:
- https://blogs.msdn.microsoft.com/vancem/2012/07/09/introduction-tutorial-logging-etw-events-in-c-system-diagnostics-tracing-eventsource/

## Application Insights EventSources
| Repo       	| Provider Name                                                              	| Provider Guid |
|------------	|----------------------------------------------------------------------------	|---------------|
| Base SDK    | Microsoft-ApplicationInsights-Core                                         	|74af9f20-af6a-5582-9382-f21f674fb271|
|             | Microsoft-ApplicationInsights-Data                                         	|a62adddb-6b4b-519d-7ba1-f983d81623e0|
|             | Microsoft-ApplicationInsights-WindowsServer-TelemetryChannel               	|4c4280fb-382a-56be-9a13-fab0d03395f6|
| | | |
| Web SDK     | Microsoft-ApplicationInsights-Extensibility-AppMapCorrelation-Dependency   	|08037ff3-aed4-5081-a6e0-f05fa0bd1f42|
|             | Microsoft-ApplicationInsights-Extensibility-AppMapCorrelation-Web          	|0a458c93-c7fb-5fbe-1135-21b01e192abc|
|             | Microsoft-ApplicationInsights-Extensibility-DependencyCollector            	|9e925f53-f61b-51a7-d10f-1148a547b70f|
|             | Microsoft-ApplicationInsights-Extensibility-EventCounterCollector         	|3a5cd921-6470-5a93-a62f-5827813b3968|
|             | Microsoft-ApplicationInsights-Extensibility-HostingStartup                 	|8be90d99-6348-569a-0517-642982a0623b|
|             | Microsoft-ApplicationInsights-Extensibility-PerformanceCollector           	|47e5de30-9965-58bd-dfc8-64c697aa1908|
|             | Microsoft-ApplicationInsights-Extensibility-PerformanceCollector-QuickPulse	|70faf222-f29d-5dee-433d-d3b77846888e|
|             | Microsoft-ApplicationInsights-Extensibility-Web                            	|d6a4f609-0e40-51c8-0344-8d1a0c91cb10|
|             | Microsoft-ApplicationInsights-Extensibility-WindowsServer                  	|5093fab5-865e-566a-cd34-857f08a430cf|
|             | Microsoft-ApplicationInsights-WindowsServer-Core                           	|b38dc757-fc28-52f1-9241-fd6310c28590|
| | | |
| Logging SDK | Microsoft-ApplicationInsights-Extensibility-EventSourceListener            	|e0b8ecfa-7c08-54f7-ac08-3cf0f7ba965e|
| | | |
| AspNetCore SDK    | Microsoft-ApplicationInsights-AspNetCore                              |dbf4c9d9-6cb3-54e3-0a54-9d138a74b116|
| | | |
| Extensions  | Microsoft-ApplicationInsights-FrameworkLightup                             	|323adc25-e39b-5c87-8658-2c1af1a92dc5   <sup>*1</sup>|
|             | Microsoft-ApplicationInsights-IIS-ManagedHttpModuleHelper                  	|61f6ca3b-4b5f-5602-fa60-759a2a2d1fbd   <sup>*1</sup>|
|             | Microsoft-ApplicationInsights-Redfield-Configurator                        	|090fc833-b744-4805-a6dd-4cb0b840a11f   <sup>*1</sup>|
|             	| Microsoft-ApplicationInsights-RedfieldIISModule                          	|252e28f4-43f9-5771-197a-e8c7e750a984   <sup>*1</sup>|
|             	| Microsoft-ApplicationInsights-Redfield-VmExtensionHandler                	|7014a441-75d7-444f-b1c6-4b2ec9b06f20   <sup>*1</sup>|



### Footnotes
1. These are custom defined GUIDS. Because they are not generated from the provider name they must be subscribed to via the GUID.



## Tools to collect ETW

### PerfView

[PerfView](https://github.com/Microsoft/perfview) is a free diagnostics and performance-analysis tool that help isolate CPU, memory, and other issues by collecting and visualizing diagnostics information from many sources.

For more information, see [Recording performance traces with PerfView.](https://github.com/dotnet/roslyn/wiki/Recording-performance-traces-with-PerfView)

#### Example
To collect logs run this command:
```
PerfView.exe collect -MaxCollectSec:300 -NoGui /onlyProviders=*Microsoft-ApplicationInsights-Core,*Microsoft-ApplicationInsights-Data,*Microsoft-ApplicationInsights-WindowsServer-TelemetryChannel,*Microsoft-ApplicationInsights-Extensibility-AppMapCorrelation-Dependency,*Microsoft-ApplicationInsights-Extensibility-AppMapCorrelation-Web,*Microsoft-ApplicationInsights-Extensibility-DependencyCollector,*Microsoft-ApplicationInsights-Extensibility-HostingStartup,*Microsoft-ApplicationInsights-Extensibility-PerformanceCollector,*Microsoft-ApplicationInsights-Extensibility-PerformanceCollector-QuickPulse,*Microsoft-ApplicationInsights-Extensibility-Web,*Microsoft-ApplicationInsights-Extensibility-WindowsServer,*Microsoft-ApplicationInsights-WindowsServer-Core,*Microsoft-ApplicationInsights-Extensibility-EventSourceListener,*Microsoft-ApplicationInsights-AspNetCore
```

#### Recommended parameters
- `MaxCollectSec` Set this parameter to prevent PerfView from running indefinitely and affecting the performance of your server.
- `OnlyProviders` Set this paramater to only collect logs from the SDK. You can customize this list based on your specific investigations.

### Logman

[Logman](https://docs.microsoft.com/windows-server/administration/windows-commands/logman) creates and manages Event Trace Session and Performance logs and supports many functions of Performance Monitor from the command line.

#### Example
To get started, create a txt of the providers you intend to collect (providers.txt):
```
{4c4280fb-382a-56be-9a13-fab0d03395f6}
{74af9f20-af6a-5582-9382-f21f674fb271}
{a62adddb-6b4b-519d-7ba1-f983d81623e0}
```
The following commands will collect traces:
```
logman -start ai-channel -pf providers.txt -ets -bs 1024 -nb 100 256
logman -stop ai-channel -ets
```
To inspect logs:
```
tracerpt ai-channel.etl -o ai-channel.etl.xml -of XML
.\PerfView.exe ai-channel.etl
```
#### Recommended parameters
- `-pf <filename>` File listing multiple Event Trace providers to enable.
- `-rf <[[hh:]mm:]ss>` Run the data collector for the specified period of time.
- `-ets` Send commands to Event Trace Sessions directly without saving or scheduling.

### FileDiagnosticsTelemetryModule

For more information, see: https://docs.microsoft.com/azure/azure-monitor/app/asp-net-troubleshoot-no-data#net-framework

### StatusMonitor v2

StatusMonitor v2 is a PowerShell module that enables codeless attach of .NET web applications.
SMv2 will ship with a cmdlet to capture ETW events.

For more information, see: https://docs.microsoft.com/en-us/azure/azure-monitor/app/status-monitor-v2-api-start-trace

StatusMonitor uses TraceEventSession to record ETW logs.
- https://github.com/microsoft/perfview/blob/master/documentation/TraceEvent/TraceEventProgrammersGuide.md
- https://github.com/dotnet/roslyn/wiki/Recording-performance-traces-with-PerfView
- https://github.com/microsoft/perfview/blob/master/src/TraceEvent/TraceEventSession.cs

## References

This document is referenced by: https://docs.microsoft.com/azure/azure-monitor/app/asp-net-troubleshoot-no-data#PerfView