# Changelog 

### Version 2.5.0
- Update Application Insights API reference to [2.5.0]
- Removed framework 4.0 support
- For EventSourceTelemetryModule, allows black list the event sources. Drops the events to those in the list.
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

