# Changelog

## Version 2.2.0-beta1

- Project is upgraded to work with Visual Studio 2017. Also projects are modified to use csproj instead of project.json.
- Adaptive sampling enabled for both - full framework and .NET Core applications.
- ServerTelemetryChannel is enabled and set as default channel for both - full framework and .NET Core applications.

## Version 2.1.1

- [Address the issue where DependencyCollection breaks Azure Storage Emulator calls](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/488)
- [Support setting request operation name based on executing Razor Page](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/430)
- [Fixed ITelemetryProcessor dependency injection failure when using 3rd party IoC Container](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/482)
- [Logging exceptions when using ILogger if an exception is present](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/393)
- [Syncronize access to HttpContext properties](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/373)
- Updated SDK version dependency to 2.4.1 for DependencyCollector.

## Version 2.1.0

- Updated SDK version dependency to 2.4.0.
- Fixed a minor logging message issue.
- Fixed unit test reliability issues.

## Version 2.1.0-beta6

- Updated SDK version dependency to 2.4.0-beta5.

## Version 2.1.0-beta5

- Added support for adding telemetry processors through dependency injection; see #344, #445, #447
- [Added support for environment specifc appsettings under default configuration](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/449)
- Updated SDK version dependency to 2.4.0-beta4.

## Version 2.1.0-beta4

- [Made package meta-data URLs use HTTPS](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/390)
- Updated SDK version dependency to 2.4.0-beta3.

## Version 2.1.0-beta3

- [Removed the use of Platform Abstractions](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/410)
- [Correlation header injection disabled for standard Azure storage calls](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/416)
- [Made UseApplicationInsights and AddApplicationInsightsTelemetry calls idempotent](https://github.com/Microsoft/ApplicationInsights-aspnetcore/pull/419)

## Version 2.1.0-beta2

- Updated to use the new correlation headers and changed the correlationId to use appId instead of hashed ikey to match other SDKs.
- Fixed null reference exception for unitialized ILogger.
- Unit test bug fixes.
- Upgraded NETStandard.Library dependency to 1.6.1.
- Updated to reference base SDK 2.4.0-beta2.
- Included Microsoft.ApplicationInsights.DependencyCollector for .NET Core.

## Version 2.1.0-beta1

- Bug fixes
- Removed UserAgentTelemetryInitializer and associated tests.
- Added instrumentation key header
- [Added OperationCorrelationTelemetryInitializer](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/333)
- [Set Id instead of OperationId for request dependency correlation](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/340)
- [Set Id in thread-safe location](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/342)
- Updated SDK version dependency to 2.3.0-beta3.

## Version 2.0.0

- Added a configuration overload for AddApplicationInsightsTelemetry.
- Updated test projects to reference .NET Core 1.1.0.
- [Fixed debug trace logging issue](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/315)
- [Stopped logging extra debug traces to AI](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/314)
- [JS snippet is empty if telemetry is disabled](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/313)
- [Added an initializer to provide the environment name as a custom property](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/312)
- [Added an option to emit JS to track authenticated users](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/311)
- Minor bug fixes and cleanup.

## Version 2.0.0-beta1

- This release contains a rewrite of the SDK internals for better .NET Core integration and initialization.
- The methods UseApplicationInsightsRequestTelemetry and UseApplicationInsightsExceptionTelemetry are obsolete, the work those methods did is handled automatically internally now.  You can just delete any existing references to them from Startup.cs.
- The MVC dependency for the JavaScript snippet has been removed so in order to include the JavaScript snippet now you need to insert the following lines at the very top of the _Layout.cshtml file:
```cshtml
    @using Microsoft.ApplicationInsights.AspNetCore
    @inject JavaScriptSnippet snippet
```
- and insert the following line before the closing `</head>` tag:
```cshtml
    @Html.Raw(snippet.FullScript)
```

## Version 1.0.3-beta1

- New ```AzureWebAppRoleEnvironmentTelemetryInitializer``` telemetry initializer that populates role name and role instance name for Azure Web Apps.

## Version 1.0.2

- Marked code analysis packages as only for build and not NuGet package dependencies.

## Version 1.0.1

- Added code analysis packages.
- Updated JavaScript snippet.
- Updated project link and added privacy statement link.
- Added culture to string operations.
- Switched TelemetryClient service registration to Singleton.
- Added after build target to patch XML doc files with language attribute.
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

