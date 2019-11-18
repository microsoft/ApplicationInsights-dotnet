# Changelog

## Version 2.12.0-beta3
- [Standard Metric extractor for Dependency) add Dependency.ResultCode dimension.](https://github.com/microsoft/ApplicationInsights-dotnet/issues/1233)

## Version 2.12.0-beta2
- [Enable Metric DimensionCapping API for Internal use with standard metric aggregation.](https://github.com/microsoft/ApplicationInsights-dotnet/issues/1244)
- [ILogger - Flush TelemetryChannel when the ILoggerProvider is Disposed.](https://github.com/microsoft/ApplicationInsights-dotnet/pull/1289)
- [Standard Metric extractor (Request,Dependency) populates all standard dimensions.](https://github.com/microsoft/ApplicationInsights-dotnet/issues/1233)

## Version 2.12.0-beta1
- BASE: [New: TelemetryConfiguration now supports Connection Strings]https://github.com/microsoft/ApplicationInsights-dotnet/issues/1221)
- WEB: [Enhancement to how QuickPulseTelemetryModule shares its ServiceEndpoint with QuickPulseTelemetryProcessor.](https://github.com/microsoft/ApplicationInsights-dotnet-server/pull/1266)
- WEB: [QuickPulse will support SDK Connection String](https://github.com/microsoft/ApplicationInsights-dotnet/issues/1221)
- WEB: [Add support for storing EventCounter Metadata as properties of MetricTelemetry](https://github.com/microsoft/ApplicationInsights-dotnet-server/issues/1287)
- WEB: [New RoleName initializer for Azure Web App to accurately populate RoleName.](https://github.com/microsoft/ApplicationInsights-dotnet-server/issues/1207)
- NETCORE: Skipping version numbers to keep in sync with Base SDK.
- NETCORE: [Fix Null/Empty Ikey from ApplicationInsightsServiceOptions overrding one from appsettings.json](https://github.com/microsoft/ApplicationInsights-aspnetcore/issues/989)
- NETCORE: [Provide ApplicationInsightsServiceOptions for easy disabling of any default TelemetryModules](https://github.com/microsoft/ApplicationInsights-aspnetcore/issues/988)
- NETCORE: [Added support for SDK Connection String](https://github.com/microsoft/ApplicationInsights-dotnet/issues/1221)
- NETCORE: [New RoleName initializer for Azure Web App to accurately populate RoleName.](https://github.com/microsoft/ApplicationInsights-dotnet-server/issues/1207)
- NETCORE: Update to Base/Web/Logging SDK to 2.12.0-beta1


## OLDER

Our older changelogs have not been migrated to this file.

- [Base](.\CHANGELOG.Base.md)
- [Web](.\CHANGELOG.Web.md)
- [Logging](.\CHANGELOG.Logging.md)
- [NetCore](.\CHANGELOG.NetCore.md)

