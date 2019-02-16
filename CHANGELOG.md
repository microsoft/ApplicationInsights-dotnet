# Changelog 
### Version 2.9.1
- Update Base SDK to version 2.9.1

### Version 2.9.0
- Update Base SDK to version 2.9.0

### Version 2.9.0-beta3
- Update Base SDK to version 2.9.0-beta3
- [ILogger implementation for ApplicationInsights](https://github.com/Microsoft/ApplicationInsights-dotnet-logging/pull/239)
- Update log4net reference to [2.0.7](https://www.nuget.org/packages/log4net/2.0.7)

### Version 2.8.1
- Update BaseSdk reference to 2.8.1. See [release notes](https://github.com/Microsoft/ApplicationInsights-dotnet/releases) for more information.

### Version 2.7.2
- [NLog can perform Layout of InstrumentationKey](https://github.com/Microsoft/ApplicationInsights-dotnet-logging/pull/203)
- Upgrade `System.Diagnostics.DiagnosticSource` to version 4.5.0
- [Event Source telemetry module: Microsoft-ApplicationInsights-Data id disabled by default to work around CLR bug](https://github.com/Microsoft/ApplicationInsights-dotnet-logging/pull/206)

### Version 2.6.4
- [Log4Net new supports NetStandard 1.3!](https://github.com/Microsoft/ApplicationInsights-dotnet-logging/pull/167)
- [NLog Flush should include async delay](https://github.com/Microsoft/ApplicationInsights-dotnet-logging/pull/176)
- [NLog can include additional ContextProperties](https://github.com/Microsoft/ApplicationInsights-dotnet-logging/pull/183)
- [DiagnosticSourceTelemetryModule supports onEventWrittenHandler](https://github.com/Microsoft/ApplicationInsights-dotnet-logging/pull/184)
- [Fix: Prevent double telemetry if DiagnosticSourceTelemetryModule is initialized twice](https://github.com/Microsoft/ApplicationInsights-dotnet-logging/pull/181)

### Version 2.6.0-beta3
- [NetStandard Support for TraceListener](https://github.com/Microsoft/ApplicationInsights-dotnet-logging/pull/166)
- [NetStandard Support for NLog and log4net](https://github.com/Microsoft/ApplicationInsights-dotnet-logging/pull/167)
- [NLog and log4net can Flush](https://github.com/Microsoft/ApplicationInsights-dotnet-logging/pull/167)
- Update log4net reference to [2.0.6](https://www.nuget.org/packages/log4net/2.0.6)

### Version 2.6.0-beta2
- [Include NLog GlobalDiagnosticsContext properties](https://github.com/Microsoft/ApplicationInsights-dotnet-logging/pull/152)
- [Remove automatic collection of User Id](https://github.com/Microsoft/ApplicationInsights-dotnet-logging/issues/153)

### Version 2.5.0
- Update Application Insights API reference to 2.5.0
- Removed framework 4.0 support
- For EventSourceTelemetryModule, allows black list the event sources. Drops the events to those in the list.
- [Fix Deadlock over EventSourceTelemetryModule](https://github.com/Microsoft/ApplicationInsights-dotnet-logging/issues/109)
- [Extensibel payload handler](https://github.com/Microsoft/ApplicationInsights-dotnet-logging/pull/111)
- [Add ProviderName and ProviderGuid properties to TraceTelemetry](https://github.com/Microsoft/ApplicationInsights-dotnet-logging/pull/120)
- [Add support for disabledEventSourceNamePrefix configuration](https://github.com/Microsoft/ApplicationInsights-dotnet-logging/issues/122)
- [Fix ApplicationInsights TraceListener does not respect Flush](https://github.com/Microsoft/ApplicationInsights-dotnet-logging/issues/67)
- [Fix NullReferenceException in DiagnosticSourceListener](https://github.com/Microsoft/ApplicationInsights-dotnet-logging/pull/143)
- [Use InvariantCulture to convert property values](https://github.com/Microsoft/ApplicationInsights-dotnet-logging/pull/144)
- Update NLog reference to [4.4.12](https://github.com/NLog/NLog/releases/tag/v4.4.12)

### Version 2.4.0
- Update Application Insights API reference to [2.4.0]

### Version 2.4.0-beta1/2
Update Application Insights API reference to [2.4.0-beta3]
Added support for logs from EventSource, ETW and Diagnostic Source.

### Version 2.1.1

- Update NLog reference to [4.3.8](https://github.com/NLog/NLog/releases/tag/4.3.8)

### Version 2.1.0

- For NLog and Log4Net when exception is traced with a custom message, custom message is added to the properties collection and reported to ApplicationInsights.
- Update Application Insights API reference to [2.1.0](https://github.com/Microsoft/ApplicationInsights-dotnet/releases/tag/v2.1.0)
- Update NLog reference to [4.3.5](https://github.com/NLog/NLog/releases/tag/4.3.5)

### Version 2.0.0

- Update Application Insights API reference to [2.0.0](https://github.com/Microsoft/ApplicationInsights-dotnet/releases/tag/v2.0.0)
- Update NLog reference to [4.2.3](https://github.com/NLog/NLog/releases/tag/4.2.3)
- Update Log4Net reference to [2.0.5 (1.2.15)](http://logging.apache.org/log4net/release/release-notes.html)
- NLog: support [Layout](https://github.com/nlog/NLog/wiki/Layouts)

### Version 1.2.6

- Bug fixes
- log4Net: Collect log4net properties as custom properties. UserName is not a custom property any more (It is collected as telemetry.Context.User.Id). Timestamp is not a custom property any more.
- NLog: Collect NLog properties as custom properties. SequenceID is not a custom property any more (It is collected as telemetry.Sequence). Timestamp is not a custom property any more. 

### Version 1.2.5
- First open source version: References Application Insights API version 1.2.3 or higher.

