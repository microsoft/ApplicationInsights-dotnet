# Changelog

## Version 1.0.1

- Updated .NET Core references to 1.0.1.

## Version 1.0.0

[Stable 1.0.0 release](http://www.nuget.org/packages/Microsoft.ApplicationInsights.AspNetCore/1.0.0).
Supports .NET framework and [.NET Core](https://www.microsoft.com/net/core).

Features:
- request tracking
- exception tracking
- diagnostic tracing
- dependency collection (.NET framework only)
- performance counter collection (.NET framework only)
- adaptive sampling (.NET framework only)
- telemetry processors (.NET framework only)
- metrics stream (.NET framework only)

Depends on:
- [Application Insights Core 2.1.0 SDK](http://www.nuget.org/packages/Microsoft.ApplicationInsights/2.1.0)
- [AI Dependency Collector](http://www.nuget.org/packages/Microsoft.ApplicationInsights.DependencyCollector/2.1.0) (.NET framework only)
- [AI Performance Counter Collector](http://www.nuget.org/packages/Microsoft.ApplicationInsights.PerfCounterCollector/2.1.0) (.NET framework only)
- [AI Windows Server Telemetry Channel](http://www.nuget.org/packages/Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel/2.1.0) (.NET framework only)

## Version 1.0.0-rc2-final

All the changes from [1.0.0-rc1-update4](https://github.com/Microsoft/ApplicationInsights-aspnetcore/releases/tag/v1.0.0-rc1-update4), including the following updates:
- Renaming: Microsoft.ApplicationInsights.AspNet is changed to Microsoft.ApplicationInsights.AspNetCore
- Runtime: Supports .NET Core CLI runtime. Does not support DNX runtime and the associated RC1 bits.
- Supports ASP.NET Core on .NET Core and the .NET Framework
- Dependencies are updated to the latest RC2 bits.
- Metrics Stream functionality is enabled by default in .NET Framework
- Install from [https://www.nuget.org/packages/Microsoft.ApplicationInsights.AspNetCore](https://www.nuget.org/packages/Microsoft.ApplicationInsights.AspNetCore)

## Version 1.0.0-rc1-update4

- Windows Server Telemetry Channel is enabled in full framework to send telemetry, and it uses Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel, version: 2.1.0-beta3
- Diagnostic tracing using EventSource is enabled
- TelemetryConfiguration.Active is used as the default telemetry configuration
- Adaptive Sampling by default is enabled in full framework
- Using telemetry processors is enabled in full framework
- ApplicationInsightsServiceOptions is available to configure default adaptive sampling behavior
- ComponentVersionTelemetryInitializer is added, that reads the application version from project.json and assigns it to telemetry.Context.Component.Version
- All Microsoft.ApplicationInsights.* dependencies are updated to the latest version (2.1.0-beta3)

## Version 1.0.0-rc1-update3

- Update Application Insights Core dependency (Microsoft.ApplicationInsights) to the latest stable version (2.0.0).

## Version 1.0.0-rc1-update2

- Fix the dependencies of previously published NuGet package (v1.0.0-rc1-update1)

## Version 1.0.0-rc1-update1

- Support the latest version of Application Insights core sdk (2.0.0-beta4 or greater)
- Support dependency and performance counter collection in full framework (dnx 4.5.1)

## Version 1.0.0-rc1

- Support ASP.Net 5 RC1 release.
- Binaries are now strong name signed.

## Version 1.0.0-beta8

- Support Asp.Net 5 beta8

## Version 1.0.0-beta7

- Support ASP.Net5 Beta7
- Minor bug fixes

## Version 1.0.0-beta6

- Support ASP.Net 5 Beta6
- Updates to build infrastructure
- Switch to 1.1 version of Microsoft.ApplicationInsights API

## Version 1.0.0-beta5

- Support ASP.Net 5 Beta5
- Minor bug fixes
- Switch to 0.17 version of Microsoft.ApplicationInsights API

## Version 0.32.0-beta4

- Support dnxcore50 applications
- Change integration points with Visual Studio
- Minor bug fixes
- Switch to 0.16 version of Microsoft.ApplicationInsights API

## Version 0.31.0-beta4

- Fixed references to ASP.NET runtime packages.

## Version 0.30.0.1-beta

- Preview version of Application Insights. Supports only full framework. Will compile for core framework, but no events will be sent.

