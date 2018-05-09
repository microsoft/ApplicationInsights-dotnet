# Changelog

This changelog will be used to generate documentation on [release notes page](http://azure.microsoft.com/en-us/documentation/articles/app-insights-release-notes-dotnet/).

## Version 2.7.0-beta1 
- [Extend the Beta period for Metrics Pre-Aggregation features shipped in 2.6.0-beta3.](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/785)
- [New: Added TryGetOperationDetail to DependencyTelemetry to facilitate advanced ITelemetryInitializer scenarios.  Allows ITelemetryInitializer implementations to specify fields that would otherwise not be sent automatically to the backend.] (https://github.com/Microsoft/ApplicationInsights-dotnet-server/issues/900)

## Version 2.6.0
- [Fix: changed namespace SamplingPercentageEstimatorSettings](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/727)
- [Extend the Beta period for Metrics Pre-Aggregation features shipped in 2.6.0-beta3.](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/785)

## Version 2.6.0-beta4
- [New: Enable ExceptionTelemetry.SetParsedStack for .Net Standard](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/763)
- [Fix: TelemetryClient throws NullReferenceException on Flush if the underlying configuration was disposed](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/755)

## Version 2.6.0-beta3
- [Report internal errors from Microsoft.AspNet.TelemteryCorrelation module](https://github.com/Microsoft/ApplicationInsights-dotnet/pull/744)
- [Fix: Telemetry tracked with StartOperation is tracked outside of corresponding activity's scope](https://github.com/Microsoft/ApplicationInsights-dotnet-server/issues/864)
- [Fix: TelemetryProcessor chain building should also initialize Modules.](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/549)
- [Fix: Wrong error message in AutocollectedMetricsExtractor.](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/611)
- [NEW: Interface and Configuration: IApplicationIdProvider.](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/739)
- [NEW: Metrics Pre-Aggregation: New `TelemetryClient.GetMetric(..).TrackValue(..)` and related APIs always locally pre-aggregate metrics before sending. They are replacing the legacy `TelemetryClient.TrackMetric(..)` APIs.](https://github.com/Microsoft/ApplicationInsights-dotnet/pull/735) ([More info](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/643).)

## Version 2.6.0-beta2
- [Changed signature of TelemetryClient.TrackDependency](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/684)
- [Added overload of TelemetryClientExtensions.StartOperation(Activity activity).] (https://github.com/Microsoft/ApplicationInsights-dotnet/issues/644)
- [Finalize the architecture for adding default heartbeat properties (supporting proposal from Issue #636).](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/666).

## Version 2.5.1
- Fix for missing TelemetryContext. Thank you to our community for discovering and reporting this issue! 
  [Logic bug within Initialize() in TelemetryContext](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/692),
  [Dependency correlation is broken after upgrade to .NET SDK 2.5](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/706),
  [Lost many context fields in 2.5.0](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/708)

## Version 2.5.0-beta2
- Remove calculation of sampling-score based on Context.User.Id [Issue #625](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/625)
- New sdk-driven "heartbeat" functionality added which sends health status at pre-configured intervals. See [extending heartbeat properties doc for more information](./docs/ExtendingHeartbeatProperties.md)
- Fixes a bug in ServerTelemetryChannel which caused application to crash on non-windows platforms. 
			[Details on fix and workaround #654] (https://github.com/Microsoft/ApplicationInsights-dotnet/issues/654)
			Original issue (https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/551)
- [Fixed a bug with the `AdaptiveSamplingTelemetryProcessor` that would cause starvation over time. Issue #756 (dotnet-server)](https://github.com/Microsoft/ApplicationInsights-dotnet-server/issues/756)
- Updated solution to build on Mac!

## Version 2.5.0-beta1
- Method `Sanitize` on classes implementing `ITelemetry` no longer modifies the `TelemetryContext` fields. Serialized event json and ETW event will still have context tags sanitized.
- Application Insights SDK supports multiple telemetry sinks. You can configure more than one channel for telemetry now.
- New method `DeepClone` on `ITelemetry` interface. Implemented by all supported telemetry items.
- Server telemetry channel NuGet support a netstandard1.3 target with fixed rate sampling and adaptive sampling telemetry processors.
- Instrumentation key is no longer required for TelemetryClient to send data to channel(s). This makes it easier to use the SDK with channels other than native Application Insights channels.
- .NET 4.0 targets were removed. Please use the version 2.4.0 if you cannot upgrade your application to the latest framework version.
- Removed `wp8`, `portable-win81+wpa81` and `uap10.0` targets.

## Version 2.4.0
- Updated version of DiagnosticSource to 4.4.0 stable

## Version 2.4.0-beta5
- Updated version of DiagnosticSource referenced.

## Version 2.4.0-beta4
- Made Metric class private and fixed various metrics related issues.

## Version 2.4.0-beta3

## Version 2.4.0-beta2
- Removed metric aggregation functionality as there is not enough feedback on the API surface yet.

## Version 2.4.0-beta1
- Event telemetry is set to be sampled separately from all other telemetry types. It potentially can double the bill. The reason for this change is that Events are mostly used for usage analysis and should not be subject to sampling on high load of requests and dependencies. Edit `ApplicationInsights.config` file to revert to the previous behavior.
- Added dependency on System.Diagnostics.DiagnosticsSource package. It is still possible to use standalone Microsoft.ApplicationInsights.dll to track telemetry.
- StartOperation starts a new System.Diagnostics.Activity and stores operation context in it. StartOperation overwrites OperationTelemetry.Id set before or during telemetry initialization for the dependency correlation purposes.
- OperationCorrelationTelemetryInitializer initializes telemetry from the Activity.Current. Please refer to https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/ActivityUserGuide.md for more details about Activity and how to use it
- `Request.Success` field will not be populated based on `ResponseCode`. It needs to be set explicitly.
- New "ProblemId" property on ExceptionTelemetry. It can be used to set a custom ProblemId value.
- Metric Aggregation functionality (originally added in 2.3.0-beta1 but removed in 2.3.0) is re-introduced.
- Improved exception stack trace data collection for .NET Core applications.

## Version 2.3.0
- Includes all changes since 2.2.0 stable release.
- Removed metric aggregation functionality added in 2.3.0-beta1 release.
- [Fixed a bug which caused SDK to stop sending telemetry.](https://github.com/Microsoft/ApplicationInsights-dotnet-server/issues/480)

## Version 2.3.0-beta3
- [Added overloads of TelemetryClientExtensions.StartOperation.] (https://github.com/Microsoft/ApplicationInsights-dotnet/issues/163)
- Fire new ETW events for Operation Start/Stop.

## Version 2.3.0-beta2
- Added constructor overloads for TelemetryConfiguration and added creation of a default InMemoryChannel when no channel is specified for a new instance.
  TelemetryClient will no longer create an InMemoryChannel on the configuration instance if TelemetryChannel is null, instead the configuration instances will always have a channel when created.
- TelemetryConfiguration will no longer dispose of user provided ITelemetryChannel instances.  Users must properly dispose of any channel instances which they create, the configuration will only auto-dispose of default channel instances it creates when none are specified by the user.

## Version 2.3.0-beta1
- Added metric aggregation functionality via MetricManager and Metric classes.
- Exposed a source field on RequestTelemetry. This can be used to store a representation of the component that issued the incoming http request. 

## Version 2.2.0
- Includes all changes since 2.1.0 stable release.

## Version 2.2.0-beta6
- Added serialization of the "source" property.
- Downgraded package dependencies to Microsoft.NETCore.App 1.0.1 level.
- [Fixed the priority of getting an iKey from an environment variable](https://github.com/Microsoft/ApplicationInsights-dotnet-server/issues/236)

## Version 2.2.0-beta5
- Moved from PCL dotnet5.4 to .NET Core NetStandard1.5.
- Updated dependency versions.

## Version 2.2.0-beta4
- Payload sanitization for RichPayloadEventSource.
- Fix to fallback to an environment variable for instrumentation key when not specified when initializing TelemetryConfiguration.
- RoleInstance and NodeName are initialized with the machine name by default.

## Version 2.2.0-beta3

- Read InstrumentationKey from environment variable APPINSIGHTS_INSTRUMENTATIONKEY if it is was not provided inline. If provided it overrides what is set though configuration file. (Feature is not available in PCL version of SDK).
- Context properties `NetworkType`, `ScreenResolution` and `Language` marked as obsolete. Please use custom properties to report network type, screen resolution and language. Values stored in these properties will be send as custom properties. 
- Dependency type was updated to reflect the latest developments in Application Insights Application Map feature. You can set a new field - `Target`. `CommandName` was renamed to `Data` for consistency with the Application Analytics schema. `DependencyKind` will never be send any more and will not be set to "Other" by default. Also there are two more constructors for `DependencyTelemetry` item.
- Type `SessionStateTelemetry` was marked obsolete. Use `IsFirst` flag in `SessionContext` to indicate that the session is just started.
- Type `PerformanceCounterTelemetry` was marked obsolete. Use `MetricTelemetry` instead.
- Marked `RequestTelemetry.HttpMethod` as obsolete. Put http verb as part of the name for the better grouping by name and use custom properties to report http verb as a dimension.
- Marked `RequestTelemetry.StartTime` as obsolete. Use `TimeStamp` instead.
- [Removed BCL dependency](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/175)
- [Added IPv6 support](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/316)
- [Fixed an issue where channels sent expired data from storage](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/278)
- [Fixed an issue where the clock implementation would accumulate error](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/271)
- [Fixed an issue where telemetry with emptry properties would be dropped](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/319)
- [Added support for SDK-side throttling](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/318)

## Version 2.2.0-beta2

- InMemoryChannel has a new override for Flush method that accepts timeout.
- Local storage folder name was changed. That means that when the application stopped, and the application was updated to the new SDK, then the telemetry from the old local folder will not be send.
- Allow all characters in property names and measurements names.
- AdaptiveTelemetryProcessor has a new property IncludedTypes. It gets or sets a semicolon separated list of telemetry types that should be sampled. If left empty all types are included implicitly. Types are not included if they are set in ExcludedTypes.
- Richpayload event source event is generated for all framework versions of SDK (before it was supported in 4.6 only)
- TelemetryClient has a new method TrackAvailability. Data posted using this method would be available in AppAnalitics only, Azure portal UI is not available at this moment.

## Version 2.2.0-beta1

- Add ExceptionTelemetry.Message property. If it is provided it is used instead of Exception.Message property for the outer-most exception.
- Telemetry types can be excluded from sampling by specifing ExcludedTypes property. 
- ServerTelemetryChannel: changed backoff logic to be less aggressive, added diagnostics event when backoff logic kicks in and added more tracing. (Done to address issues when data stops flowing till application gets restarted)

## Version 2.1.0-beta4
- [Bug fix](https://github.com/Microsoft/ApplicationInsights-dotnet-server/issues/76)

## Version 2.1.0-beta3
- Support partial success (206) from the Application Insights backend. Before this change SDK may have lost data because some items of the batch were accepted and some items of the batch were asked to be retried (because of burst throttling or intermittent issues).
- Bug fixes

## Version 2.1.0-beta1

- Upgraded to depend on EventSource.Redist nuget version 1.1.28
- Upgraded to depend on Microsoft.Bcl nuget version 1.1.10

## Version 2.0.1

- Add Win Phone, Win Store and UWP targets that include 1.2.3 version of ApplicationInsights.dll. It is included to prevent applications that upgrade to 2.0.0 from crashing. In any case using this nuget for Win Phone, Win Store and UWP targets is not recommended and not supported. 

## Version 2.0.0

- Disallow Nan, +-Infinity measurements. Value will be replaced on 0.
- Disallow Nan, +-Infinity metrics (Value, Min, Max and StandardDeviation). Values will be replaced on 0.

## Version 2.0.0-rc1

- Writing telemetry items to debug output can be disabled with ```IsTracingDisabled``` property on ```TelemetryDebugWriter```. 
Telemetry items that were filtered out by sampling are now indicated in debug output. Custom telemetry processors can now invoke
method ```WriteTelemetry``` on ```TelemetryDebugWriter``` with ```filteredBy``` parameter to indicate in debug output that an
item is being filtered out.
- DependencyTelemetry.Async property was removed.
- DependencyTelemetry.Count property was removed.
- When configuration is loaded from ApplicationInsights.config incorrect and broken elements are skipped. That includes both high level elements like TelemetryInitializers as well as individual properties.  
- Internal Application Insights SDK traces will be marked as synthetic and have `SyntheticSource` equals to 'SDKTelemetry'.
- UserContext.AcquisitionDate property was removed.
- UserContext.StoreRegion property was removed.
- InMemoryChannel.DataUploadIntervalInSeconds was removed. Use SendingInterval instead.
- DeviceContext.RoleName was removed. Use DeviceContext.Cloud.RoleName instead.
- DeviceContext.RoleInstance was removed. Use DeviceContext.Cloud.RoleInstance instead.

## Version 2.0.0-beta4

- UseSampling and UseAdaptiveSampling extensions were moved to Microsoft.ApplicationInsights.Extensibility
- Cut Phone and Store support
- Updated ```DependencyTelemetry``` to have new properties ```ResultCode``` and ```Id```
- If ``ServerTelemetryChannel`` is initialized programmatically it is required to call ServerTelemetryChannel.Initialize() method. Otherwise persistent storage will not be initialized (that means that if telemetry cannot be sent because of temporary connectivity issues it will be dropped).
- ``ServerTelemetryChannel`` has new property ``StorageFolder`` that can be set either through code or though configuration. If this property is set ApplicationInsights uses provided location to store telemetry that was not sent because of temporary connectivity issues. If property is not set or provided folder is inaccessible ApplicationInsights will try to use LocalAppData or Temp as it was done before.
- TelemetryConfiguration.GetTelemetryProcessorChainBuilder extension method is removed. Instead of this method use TelemetryConfiguration.TelemetryProcessorChainBuilder instance method.
- TelemetryConfiguration has a new property TelemetryProcessors that gives readonly access to TelemetryProcessors collection.
- `Use`, `UseSampling` and `UseAdaptiveSampling` preserves TelemetryProcessors loaded from configuration.

## Version 2.0.0-beta3
- Adaptive sampling turned on by default in server telemetry channel. Details can be found in [#80](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/80).
- Fixed signature of ```UseSampling``` to allow chaining with other calls to ```Use``` of telemetry processors.
- Property ```Request.ID``` returned back. ```OperationContext``` now has a property ```ParentId``` for end-to-end correlation.
- ```TimestampTelemetryInitializer``` is removed. Timestamp will be added automatically by ```TelemetryClient```.
- ```OperationCorrelationTelemetryInitializer``` is added by default to enable operaitons correlation.

## Version 2.0.0-beta2
- Fix UI thread locking when initializing InMemoryChannel (default channel) from UI thread.
- Added support for ```ITelemetryProcessor``` and ability to construct chain of TelemetryProcessors via code or config.
- Version of ```Microsoft.ApplicationInsights.dll``` for the framework 4.6 is now part of the package.
- IContextInitializer interface is not supported any longer. ContextInitializers collection was removed from TelemetryConfiguration object.
- The max length limit for the ```Name``` property of ```EventTelemetry``` was set to 512.
- Property ```Name``` of ```OperationContext``` was renamed to ```RootName```
- Property ```Id``` of ```RequestTelemetry``` was removed.
- Property ```Id``` and ```Context.Operation.Id``` of ```RequestTelemetry``` would not be initialized when creating new ```RequestTelemetry```.
- New properties of ```OperationContext```: ```CorrelationVector```, ```ParentId``` and ```RootId``` to support end-to-end telemetry items correlation.

## Version 2.0.0-beta1

- TrackDependency will produce valid JSON when not all required fields were specified.
- Redundant property ```RequestTelemetry.ID``` is now just a proxy for ```RequestTelemetry.Operation.Id```.
- New interface ```ISupportSampling``` and explicit implementation of it by most of data item types.
- ```Count``` property on DependencyTelemetry marked as Obsolete. Use ```SamplingPercentage``` instead.
- New ```CloudContext``` introduced and properties ```RoleName``` and ```RoleInstance``` moved to it from ```DeviceContext```.
- New property ```AuthenticatedUserId``` on ```UserContext``` to specify authenticated user identity.

## Version 1.2.3
- Bug fixes.
- Telemetry item will be serialized to Debug Output even when Instrumentation Key was not set.

## Version 1.2
- First version shipped from github

## Version 1.1

- SDK now introduces new telemetry type ```DependencyTelemetry``` which contains information about dependency call from application
- New method ```TelemetryClient.TrackDependency``` allows to send information about dependency calls from application

## Version 0.17

- Application Insights now distributes separate binaries for framework 4.0 and 4.5. Library for the framework 4.5 will not require EventSource and BCL nuget dependencies. You need to ensure you refer the correct library in ```packages.config```. It should be ```<package id="Microsoft.ApplicationInsights" version="0.17.*" targetFramework="net45" />```
- Diagnostics telemetry module is not registered in ApplicationInsights.config and no self-diagnostics messages will be sent to portal for non-web applications. Insert ```<Add Type="Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.DiagnosticsTelemetryModule, Microsoft.ApplicationInsights" />``` to ```<TelemetryModules>``` node to get SDK self-diagnostics messages from your application.
- ApplicationInsights.config file search order was changed. File from the bin/ folder will no longer be used even if exists for the web applications.
- Nullable properties are now supported in ApplicationInsights.config.
- DeveloperMode property of ```ITelemetryChannel``` interface became a nullable bool.

## Version 0.16

- SDK now supports dnx target platform to enable monitoring of [.NET Core framework](http://www.dotnetfoundation.org/NETCore5) applications.
- Instance of ```TelemetryClient``` do not cache Instrumentation Key anymore. Now if instrumentation key wasn't set to ```TelemetryClient``` explicitly ```InstrumentationKey``` will return null. It fixes an issue when you set ```TelemetryConfiguration.Active.InstrumentationKey``` after some telemetry was already collected, telemetry modules like dependency collector, web requests data collection and performance counters collector will use new instrumentation key.

## Version 0.15

- New property ```Operation.SyntheticSource``` now available on ```TelemetryContext```. Now you can mark your telemetry items as “not a real user traffic” and specify how this traffic was generated. As an example by setting this property you can distinguish traffic from your test automation from load test traffic.
- Channel logic was moved to the separate NuGet called Microsoft.ApplicationInsights.PersistenceChannel. Default channel is now called InMemoryChannel
- New method ```TelemetryClient.Flush``` allows to flush telemetry items from the buffer synchronously

## Version 0.13

No release notes for older versions available.
