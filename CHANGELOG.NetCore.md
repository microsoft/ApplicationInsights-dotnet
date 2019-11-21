# Changelog


## Version 2.12.0-beta1
- NETCORE: Skipping version numbers to keep in sync with Base SDK.
- NETCORE: [Fix Null/Empty Ikey from ApplicationInsightsServiceOptions overrding one from appsettings.json](https://github.com/microsoft/ApplicationInsights-aspnetcore/issues/989)
- NETCORE: [Provide ApplicationInsightsServiceOptions for easy disabling of any default TelemetryModules](https://github.com/microsoft/ApplicationInsights-aspnetcore/issues/988)
- NETCORE: [Added support for SDK Connection String](https://github.com/microsoft/ApplicationInsights-dotnet/issues/1221)
- NETCORE: [New RoleName initializer for Azure Web App to accurately populate RoleName.](https://github.com/microsoft/ApplicationInsights-dotnet-server/issues/1207)
- NETCORE: Update to Base/Web/Logging SDK to 2.12.0-beta1

## Version 2.8.2
- NETCORE: Updated Web SDK to 2.11.2

## Version 2.8.1
- NETCORE: Updated Web SDK to 2.11.1

## Version 2.8.0
- NETCORE: Updated Base SDK/Web SDK/Logging Adaptor SDK to 2.11.0
- NETCORE: Updated System.Diagnostics.DiagnosticSource to 4.6.0

## Version 2.8.0-beta3
- NETCORE: [Make W3C Correlation default and leverage native W3C support from Activity.](https://github.com/microsoft/ApplicationInsights-aspnetcore/pull/958)
- NETCORE: [Make W3C Correlation default and leverage native W3C support from Activity for Asp.Net Core 3.0.](https://github.com/microsoft/ApplicationInsights-aspnetcore/pull/958)
- NETCORE: [Fix: Azure Functions performance degradation when W3C enabled.](https://github.com/microsoft/ApplicationInsights-aspnetcore/issues/900)
- NETCORE: [Fix: AppId is never set is Response Headers.](https://github.com/microsoft/ApplicationInsights-aspnetcore/issues/956)
- NETCORE: [Support correlation-context in absence of request-id or traceparent.](https://github.com/microsoft/ApplicationInsights-aspnetcore/issues/901)
- NETCORE: [Non Product - Asp.Net Core 3.0 Functional Tests Added. This leverages the built-in integration test capability of ASP.NET Core via Microsoft.AspNetCore.MVC.Testing](https://github.com/microsoft/ApplicationInsights-aspnetcore/issues/539)
- NETCORE: [Fix: System.NullReferenceException in WebSessionTelemetryInitializer.](https://github.com/microsoft/ApplicationInsights-aspnetcore/issues/903)
- NETCORE: Updated Base SDK/Web SDK/Logging Adaptor SDK version dependency to 2.11.0-beta2
- NETCORE: Updated System.Diagnostics.DiagnosticSource to 4.6.0-preview8.

- NETCORE: [Add new package for .NET Core WorkerServices (Adds GenericHost support)](https://github.com/microsoft/ApplicationInsights-aspnetcore/issues/708)

## Version 2.8.0-beta2
- NETCORE: [Fix MVCBeforeAction property fetcher to work with .NET Core 3.0 changes.](https://github.com/microsoft/ApplicationInsights-aspnetcore/issues/936)
- NETCORE: [Catch generic exception from DiagnosticSourceListeners and log instead of failing user request.](https://github.com/microsoft/ApplicationInsights-aspnetcore/issues/957)
- NETCORE: [Correct names for Asp.Net Core EventCounters.](https://github.com/microsoft/ApplicationInsights-aspnetcore/issues/945)
- NETCORE: [Obsolete extension methods on IWebHostBuilder in favor of AddApplicationInsights extension method on IServiceCollection.](https://github.com/microsoft/ApplicationInsights-aspnetcore/issues/919)
- NETCORE: [Remove support for deprecated x-ms based correlation headers.](https://github.com/microsoft/ApplicationInsights-aspnetcore/issues/939)
- NETCORE: [Uri for multiple hosts headers is set to "Multiple-Host".](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/862)
- NETCORE: [LogLevel changed to Error and stack trace added for generic unknown exception within SDK.](https://github.com/microsoft/ApplicationInsights-aspnetcore/pull/946)

## Version 2.8.0-beta1
- NETCORE: [Add EventCounter collection.](https://github.com/microsoft/ApplicationInsights-aspnetcore/issues/913)
- NETCORE: [Performance fixes: One DiagSource Listener; Head Sampling Feature; No Concurrent Dictionary; etc...](https://github.com/microsoft/ApplicationInsights-aspnetcore/pull/907)
- NETCORE: [Fix: Add `IJavaScriptSnippet` service interface and update the `IServiceCollection` extension to register it for `JavaScriptSnippet`.](https://github.com/microsoft/ApplicationInsights-aspnetcore/issues/890)
- NETCORE: [Make JavaScriptEncoder optional and Fallback to JavaScriptEncoder.Default.](https://github.com/microsoft/ApplicationInsights-aspnetcore/pull/918)
- NETCORE: Updated Web/Base SDK version dependency to 2.10.0-beta4
- NETCORE: Updated Microsoft.Extensions.Logging.ApplicationInsights to 2.10.0-beta4

## Version 2.7.1
- NETCORE: [Fix - ApplicationInsights StartupFilter should not swallow exceptions from downstream ApplicationBuilder.](https://github.com/microsoft/ApplicationInsights-aspnetcore/issues/897)

## Version 2.7.0
- NETCORE: Updated Web/Base SDK version dependency to 2.10.0
- NETCORE: [Remove unused reference to System.Net.Http](https://github.com/microsoft/ApplicationInsights-aspnetcore/pull/879)

## Version 2.7.0-beta4
- NETCORE: [RequestTrackingTelemetryModule is modified to stop tracking exceptions by default, as exceptions are captured by ApplicationInsightsLoggerProvider.](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/861)
- NETCORE: Updated Web/Base SDK version dependency to 2.10.0-beta4
- NETCORE: Updated Microsoft.Extensions.Logging.ApplicationInsights to 2.10.0-beta4
- NETCORE: Reliability improvements with additional exception handling.

## Version 2.7.0-beta3
- NETCORE: [Enables Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider by default. If ApplicationInsightsLoggerProvider was enabled previously using ILoggerFactory extension method, please remove it to prevent duplicate logs.](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/854)
- NETCORE: [Remove reference to Microsoft.Extensions.DiagnosticAdapter and use DiagnosticSource subscription APIs directly](https://github.com/Microsoft/ApplicationInsights-aspnetcore/pull/852) 
- NETCORE: [Fix: NullReferenceException in ApplicationInsightsLogger.Log when exception contains a Data entry with a null value](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/848)
- NETCORE: [Performance fixes for GetUri, SetKeyHeaderValue, ConcurrentDictionary use and Telemetry Initializers](https://github.com/Microsoft/ApplicationInsights-aspnetcore/pull/864)

## Version 2.7.0-beta2
- NETCORE: Added NetStandard2.0 target.
- NETCORE: Updated Web/Base SDK version dependency to 2.10.0-beta2

## Version 2.6.1
- NETCORE: Updated Web/Base SDK version dependency to 2.9.1

## Version 2.6.0
- NETCORE: Updated Web/Base SDK version dependency to 2.9.0
- NETCORE: [Fix: TypeInitializationException when Microsoft.AspNetCore.Hosting and Microsoft.AspNetCore.Hosting.Abstractions versions do not match](https://github.com/Microsoft/ApplicationInsights-aspnetcore/pull/821)

## Version 2.6.0-beta3
- NETCORE: Updated Web/Base SDK version dependency to 2.9.0-beta3
- NETCORE: [Deprecate ApplicationInsightsLoggerFactoryExtensions.AddApplicationInsights logging extensions in favor of Microsoft.Extensions.Logging.ApplicationInsights package](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/817)
- NETCORE: [Fix: Do not track requests by each host in the process](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/621)
- NETCORE: [Fix: Correlation doesn't work for localhost](https://github.com/Microsoft/ApplicationInsights-dotnet-server/issues/1120)

## Version 2.6.0-beta2
- NETCORE: Updated Web/Base SDK version dependency to 2.9.0-beta2

## Version 2.6.0-beta1
- NETCORE: Updated Web/Base SDK version dependency to 2.9.0-beta1

## Version 2.5.1
- NETCORE: Update Web/Base SDK version dependency to 2.8.1

## Version 2.5.0
- NETCORE: Traces logged via ILogger is marked with SDK version prefix ilc (.net core) or ilf (.net framework).
- NETCORE: Update Web/Base SDK version dependency to 2.8.0

## Version 2.5.0-beta2
- NETCORE: ComVisible attribute is set to false for the project for compliance reasons.
- NETCORE: [Log exception.Data properties as additional telemetry data](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/754)
- NETCORE: Update Web/Base SDK version dependency to 2.8.0-beta2
Applicable if using additional Sinks to forward telemetry to:
- NETCORE: [Default TelemetryProcessors are added to the DefaultSink instead of common TelemetryProcessor pipeline.](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/752)
- NETCORE: [TelemetryProcessors added via AddTelemetryProcesor extension method are added to the DefaultSink instead of common TelemetryProcessor pipeline.](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/752)
  

## Version 2.5.0-beta1
- NETCORE: [Adds opt-in support for W3C distributed tracing standard](https://github.com/Microsoft/ApplicationInsights-aspnetcore/pull/735)
- NETCORE: Updated Web/Base SDK version dependency to 2.8.0-beta1

## Version 2.4.1
- NETCORE: Patch release to update Web/Base SDK version dependency to 2.7.2 which fixed a bug (https://github.com/Microsoft/ApplicationInsights-dotnet-server/issues/970)

## Version 2.4.0
- NETCORE: Updated Web/Base SDK version dependency to 2.7.1

## Version 2.4.0-beta4
- NETCORE: [Generate W3C compatible operation Id when there is no parent operation](https://github.com/Microsoft/ApplicationInsights-dotnet-server/pull/952)
- NETCORE: Updated Web/Base SDK version dependency to 2.7.0-beta4

## Version 2.4.0-beta3
- NETCORE: [Allow configuring exception tracking in RequestTrackingTelemetryModule and merge OperationCorrelationTelemetryInitializer with RequestTrackingTelemetryModule](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/709)
- NETCORE: [Allow disabling response headers injection](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/613)
- NETCORE: Updated Web/Base SDK version dependency to 2.7.0-beta3
- NETCORE: The above referenced base SDK contains fix for leaky HttpConnections. (https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/690)

## Version 2.4.0-beta2
- NETCORE: Updated Web/Base SDK version dependency to 2.7.0-beta2

## Version 2.4.0-beta1
- NETCORE: Updated Web/Base SDK version dependency to 2.7.0-beta1
- NETCORE: Enables Performance Counters for Asp.Net Core Apps running in Azure Web Apps. (https://github.com/Microsoft/ApplicationInsights-dotnet-server/issues/889)
- NETCORE: Added null check on ContentRootPath of the hostingenvironment. (https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/696)

## Version 2.3.0
- NETCORE: [Fix a bug which caused Requests to fail when Hostname was empty.] (https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/278)
- NETCORE: [Fix reading of instrumentation key from appsettings.json file when using AddApplicationInsightsTelemetry() extension to add ApplicationInsights ](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/605)
- NETCORE: [Bring back DomainNameRoleInstanceTelemetryInitializer without which NodeName and RoleInstance will be empty in Ubuntu](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/671)
- NETCORE: [RequestTelemetry is no longer populated with HttpMethod which is obsolete.](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/675)
- NETCORE: Fixed a bug which caused AutoCollectedMetricExtractor flag to be always true.
- NETCORE: Updated Web/Base SDK version dependency to 2.6.4

## Version 2.3.0-beta2
- NETCORE: [Update System.Net.Http version referred to 4.3.2 as older version has known security vulnerability. ](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/666)
- NETCORE: [Added ApplicationInsightsServiceOptions flag to turn off AutoCollectedMetricExtractor. ](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/664)
- NETCORE: [Added two AdaptiveSamplingTelemetryProcessors one for Event and one for non Event types to be consistent with default Web SDK behaviour. ](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/663)
- NETCORE: [RequestCollection is refactored to be implemented as an ITelemetryModule. This makes it possible to configure it like every other auto-collection modules. ](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/650)
- NETCORE: [Fixed race condition on dispose to close #651](https://github.com/Microsoft/ApplicationInsights-aspnetcore/pull/652)
- NETCORE: Removed DomainNameRoleInstanceTelemetryInitializer as it is deprecated.
- NETCORE: Reuse AzureWebAppRoleEnvironmentTelemetryInitializer from WindowsServer repo instead of outdated implementation in this repo.
- NETCORE: Updated Web/Base SDK version dependency to 2.6.0-beta4

## Version 2.3.0-beta1
- NETCORE: Changed behavior for `TelemetryConfiguration.Active` and `TelemetryConfiguration` dependency injection singleton: with this version every WebHost has its own `TelemetryConfiguration` instance. Changes done for `TelemetryConfiguration.Active` do not affect telemetry reported by the SDK; use `TelemetryConfiguration` instance obtained through the dependency injection. [Fix NullReferenceException when sending http requests in scenario with multiple web hosts sharing the same process](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/613)
- NETCORE: Updated Javascript Snippet with latest from [Github/ApplicationInsights-JS](https://github.com/Microsoft/ApplicationInsights-JS)
- NETCORE: [Make all built-in TelemetryInitializers public to allow easy removal from DI Container.](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/351)
- NETCORE: [Enforced limits of values read from incoming http requests to prevent security vulnerability](https://github.com/Microsoft/ApplicationInsights-aspnetcore/pull/608)
- NETCORE: [ApplicationInsightsLogger adds EventId into telemetry properties. It is off by default for compatibility. It can be switched on by configuring ApplicationInsightsLoggerOptions.](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/569)
- NETCORE: [ApplicationInsightsLogger logs exceptions as ExceptionTelemetry by default. This can now be configured with ApplicationInsightsLoggerOptions.TrackExceptionsAsExceptionTelemetry] (https://github.com/Microsoft/ApplicationInsights-aspnetcore/pull/574)
- NETCORE: [Add App Services and Azure Instance Metedata heartbeat provider modules by default, allow user to disable via configuration object.](https://github.com/Microsoft/ApplicationInsights-aspnetcore/pull/627)
- NETCORE: [Added extension method to allow configuration of any Telemetry Module.](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/634)
- NETCORE: [Added ability to remove any default Telemetry Module.](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/633)
- NETCORE: [TelemetryChannel is configured via DI, making it easier to override channel](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/641)
- NETCORE: [Fixed a bug which caused QuickPulse and Sampling to be enabled only if ServerTelemetryChannel was used](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/642)
- NETCORE: [QuickPulseTelemetryModule is constructed via DI, make it possible for users to configure it.] (https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/639)
- NETCORE: [Remove CorrelationIdLookupHelper. Use TelemetryConfiguration.ApplicationIdProvider instead.](https://github.com/Microsoft/ApplicationInsights-aspnetcore/pull/636) With this change you can update URL to query application ID from which enables environments with reverse proxy configuration to access Application Insights ednpoints.
- NETCORE: [AutocollectedMetricsExtractor is added by default to the TelemetryConfiguration](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/604)
- NETCORE: Updated Web/Base SDK version dependency to 2.6.0-beta3

## Version 2.2.1
- NETCORE: Updated Web/Base SDK version dependency to 2.5.1 which addresses a bug.

## Version 2.2.0
- NETCORE: Updated Web/Base SDK version dependency to 2.5.0

## Version 2.2.0-beta3
- NETCORE: Updated Web/Base SDK version dependency to 2.5.0-beta2.
- NETCORE: This version of Base SDK referred contains fix to a bug in ServerTelemetryChannel which caused application to crash on non-windows platforms. Details on fix and workaround(https://github.com/Microsoft/ApplicationInsights-dotnet/issues/654) Original issue (https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/551)

## Version 2.2.0-beta2
- NETCORE: Same bits as beta1. Only change is that the symbols for the binaries are indexed in Microsoft symbol servers. Beta1 symbols will not be available.

## Version 2.2.0-beta1

- NETCORE: Project is upgraded to work with Visual Studio 2017. Also projects are modified to use csproj instead of project.json.
- NETCORE: Adaptive sampling enabled for both - full framework and .NET Core applications.
- NETCORE: ServerTelemetryChannel is enabled and set as default channel for both - full framework and .NET Core applications.
- NETCORE: Live metrics collection is enabled by default for .NET Core applications (was already enabled for full .NET applications).
- NETCORE: Updated Web/Base SDK version dependency to 2.5.0-beta1.
- NETCORE: DependencyCollector referred from 2.5.0-beta1 supports collecting SQL dependency calls in .NET Core Applications using EntityFramework.

## Version 2.1.1

- NETCORE: [Address the issue where DependencyCollection breaks Azure Storage Emulator calls](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/488)
- NETCORE: [Support setting request operation name based on executing Razor Page](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/430)
- NETCORE: [Fixed ITelemetryProcessor dependency injection failure when using 3rd party IoC Container](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/482)
- NETCORE: [Logging exceptions when using ILogger if an exception is present](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/393)
- NETCORE: [Syncronize access to HttpContext properties](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/373)
- NETCORE: Updated SDK version dependency to 2.4.1 for DependencyCollector.

## Version 2.1.0

- NETCORE: Updated SDK version dependency to 2.4.0.
- NETCORE: Fixed a minor logging message issue.
- NETCORE: Fixed unit test reliability issues.

## Version 2.1.0-beta6

- NETCORE: Updated SDK version dependency to 2.4.0-beta5.

## Version 2.1.0-beta5

- NETCORE: Added support for adding telemetry processors through dependency injection; see #344, #445, #447
- NETCORE: [Added support for environment specifc appsettings under default configuration](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/449)
- NETCORE: Updated SDK version dependency to 2.4.0-beta4.

## Version 2.1.0-beta4

- NETCORE: [Made package meta-data URLs use HTTPS](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/390)
- NETCORE: Updated SDK version dependency to 2.4.0-beta3.

## Version 2.1.0-beta3

- NETCORE: [Removed the use of Platform Abstractions](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/410)
- NETCORE: [Correlation header injection disabled for standard Azure storage calls](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/416)
- NETCORE: [Made UseApplicationInsights and AddApplicationInsightsTelemetry calls idempotent](https://github.com/Microsoft/ApplicationInsights-aspnetcore/pull/419)

## Version 2.1.0-beta2

- NETCORE: Updated to use the new correlation headers and changed the correlationId to use appId instead of hashed ikey to match other SDKs.
- NETCORE: Fixed null reference exception for unitialized ILogger.
- NETCORE: Unit test bug fixes.
- NETCORE: Upgraded NETStandard.Library dependency to 1.6.1.
- NETCORE: Updated to reference base SDK 2.4.0-beta2.
- NETCORE: Included Microsoft.ApplicationInsights.DependencyCollector for .NET Core.

## Version 2.1.0-beta1

- NETCORE: Bug fixes
- NETCORE: Removed UserAgentTelemetryInitializer and associated tests.
- NETCORE: Added instrumentation key header
- NETCORE: [Added OperationCorrelationTelemetryInitializer](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/333)
- NETCORE: [Set Id instead of OperationId for request dependency correlation](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/340)
- NETCORE: [Set Id in thread-safe location](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/342)
- NETCORE: Updated SDK version dependency to 2.3.0-beta3.

## Version 2.0.0

- NETCORE: Added a configuration overload for AddApplicationInsightsTelemetry.
- NETCORE: Updated test projects to reference .NET Core 1.1.0.
- NETCORE: [Fixed debug trace logging issue](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/315)
- NETCORE: [Stopped logging extra debug traces to AI](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/314)
- NETCORE: [JS snippet is empty if telemetry is disabled](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/313)
- NETCORE: [Added an initializer to provide the environment name as a custom property](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/312)
- NETCORE: [Added an option to emit JS to track authenticated users](https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/311)
- NETCORE: Minor bug fixes and cleanup.

## Version 2.0.0-beta1

- NETCORE: This release contains a rewrite of the SDK internals for better .NET Core integration and initialization.
- NETCORE: The methods UseApplicationInsightsRequestTelemetry and UseApplicationInsightsExceptionTelemetry are obsolete, the work those methods did is handled automatically internally now.  You can just delete any existing references to them from Startup.cs.
- NETCORE: The MVC dependency for the JavaScript snippet has been removed so in order to include the JavaScript snippet now you need to insert the following lines at the very top of the _Layout.cshtml file:
```cshtml
    @using Microsoft.ApplicationInsights.AspNetCore
    @inject JavaScriptSnippet snippet
```
- NETCORE: and insert the following line before the closing `</head>` tag:
```cshtml
    @Html.Raw(snippet.FullScript)
```

## Version 1.0.3-beta1

- NETCORE: New ```AzureWebAppRoleEnvironmentTelemetryInitializer``` telemetry initializer that populates role name and role instance name for Azure Web Apps.

## Version 1.0.2

- NETCORE: Marked code analysis packages as only for build and not NuGet package dependencies.

## Version 1.0.1

- NETCORE: Added code analysis packages.
- NETCORE: Updated JavaScript snippet.
- NETCORE: Updated project link and added privacy statement link.
- NETCORE: Added culture to string operations.
- NETCORE: Switched TelemetryClient service registration to Singleton.
- NETCORE: Added after build target to patch XML doc files with language attribute.
- NETCORE: Updated .NET Core references to 1.0.1.

## Version 1.0.0

- NETCORE: [Stable 1.0.0 release](http://www.nuget.org/packages/Microsoft.ApplicationInsights.AspNetCore/1.0.0).
- NETCORE: Supports .NET framework and [.NET Core](https://www.microsoft.com/net/core).

Features:
- NETCORE: request tracking
- NETCORE: exception tracking
- NETCORE: diagnostic tracing
- NETCORE: dependency collection (.NET framework only)
- NETCORE: performance counter collection (.NET framework only)
- NETCORE: adaptive sampling (.NET framework only)
- NETCORE: telemetry processors (.NET framework only)
- NETCORE: metrics stream (.NET framework only)

Depends on:
- NETCORE: [Application Insights Core 2.1.0 SDK](http://www.nuget.org/packages/Microsoft.ApplicationInsights/2.1.0)
- NETCORE: [AI Dependency Collector](http://www.nuget.org/packages/Microsoft.ApplicationInsights.DependencyCollector/2.1.0) (.NET framework only)
- NETCORE: [AI Performance Counter Collector](http://www.nuget.org/packages/Microsoft.ApplicationInsights.PerfCounterCollector/2.1.0) (.NET framework only)
- NETCORE: [AI Windows Server Telemetry Channel](http://www.nuget.org/packages/Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel/2.1.0) (.NET framework only)

## Version 1.0.0-rc2-final

All the changes from [1.0.0-rc1-update4](https://github.com/Microsoft/ApplicationInsights-aspnetcore/releases/tag/v1.0.0-rc1-update4), including the following updates:
- NETCORE: Renaming: Microsoft.ApplicationInsights.AspNet is changed to Microsoft.ApplicationInsights.AspNetCore
- NETCORE: Runtime: Supports .NET Core CLI runtime. Does not support DNX runtime and the associated RC1 bits.
- NETCORE: Supports ASP.NET Core on .NET Core and the .NET Framework
- NETCORE: Dependencies are updated to the latest RC2 bits.
- NETCORE: Metrics Stream functionality is enabled by default in .NET Framework
- NETCORE: Install from [https://www.nuget.org/packages/Microsoft.ApplicationInsights.AspNetCore](https://www.nuget.org/packages/Microsoft.ApplicationInsights.AspNetCore)

## Version 1.0.0-rc1-update4

- NETCORE: Windows Server Telemetry Channel is enabled in full framework to send telemetry, and it uses Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel, version: 2.1.0-beta3
- NETCORE: Diagnostic tracing using EventSource is enabled
- NETCORE: TelemetryConfiguration.Active is used as the default telemetry configuration
- NETCORE: Adaptive Sampling by default is enabled in full framework
- NETCORE: Using telemetry processors is enabled in full framework
- NETCORE: ApplicationInsightsServiceOptions is available to configure default adaptive sampling behavior
- NETCORE: ComponentVersionTelemetryInitializer is added, that reads the application version from project.json and assigns it to telemetry.Context.Component.Version
- NETCORE: All Microsoft.ApplicationInsights.* dependencies are updated to the latest version (2.1.0-beta3)

## Version 1.0.0-rc1-update3

- NETCORE: Update Application Insights Core dependency (Microsoft.ApplicationInsights) to the latest stable version (2.0.0).

## Version 1.0.0-rc1-update2

- NETCORE: Fix the dependencies of previously published NuGet package (v1.0.0-rc1-update1)

## Version 1.0.0-rc1-update1

- NETCORE: Support the latest version of Application Insights core sdk (2.0.0-beta4 or greater)
- NETCORE: Support dependency and performance counter collection in full framework (dnx 4.5.1)

## Version 1.0.0-rc1

- NETCORE: Support ASP.Net 5 RC1 release.
- NETCORE: Binaries are now strong name signed.

## Version 1.0.0-beta8

- NETCORE: Support Asp.Net 5 beta8

## Version 1.0.0-beta7

- NETCORE: Support ASP.Net5 Beta7
- NETCORE: Minor bug fixes

## Version 1.0.0-beta6

- NETCORE: Support ASP.Net 5 Beta6
- NETCORE: Updates to build infrastructure
- NETCORE: Switch to 1.1 version of Microsoft.ApplicationInsights API

## Version 1.0.0-beta5

- NETCORE: Support ASP.Net 5 Beta5
- NETCORE: Minor bug fixes
- NETCORE: Switch to 0.17 version of Microsoft.ApplicationInsights API

## Version 0.32.0-beta4

- NETCORE: Support dnxcore50 applications
- NETCORE: Change integration points with Visual Studio
- NETCORE: Minor bug fixes
- NETCORE: Switch to 0.16 version of Microsoft.ApplicationInsights API

## Version 0.31.0-beta4

- NETCORE: Fixed references to ASP.NET runtime packages.

## Version 0.30.0.1-beta

- NETCORE: Preview version of Application Insights. Supports only full framework. Will compile for core framework, but no events will be sent.

