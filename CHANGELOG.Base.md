# Changelog

This changelog will be used to generate documentation on [release notes page](http://azure.microsoft.com/documentation/articles/app-insights-release-notes-dotnet/).

## Version 2.12.0-beta2
- BASE: [Enable Metric DimensionCapping API for Internal use with standard metric aggregation.]https://github.com/microsoft/ApplicationInsights-dotnet/issues/1244)
- BASE: [Standard Metric extractor (Request,Dependency) populates all standard dimensions.]https://github.com/microsoft/ApplicationInsights-dotnet/issues/1233)

## Version 2.12.0-beta1
- BASE: [New: TelemetryConfiguration now supports Connection Strings]https://github.com/microsoft/ApplicationInsights-dotnet/issues/1221)

## Version 2.11.0
- BASE: Upgrade to System.Diagnostics.DiagnosticSource v4.6
- BASE: [Fix: StartOperation(Activity) does not check for Ids compatibility](https://github.com/microsoft/ApplicationInsights-dotnet/pull/1213)

## Version 2.11.0-beta2
- BASE: [Fix: Emit warning if user sets both Sampling IncludedTypes and ExcludedTypes. Excluded will take precedence.](https://github.com/microsoft/ApplicationInsights-dotnet/issues/1166)
- BASE: [Minor perf improvement by reading Actity.Tag only if required.](https://github.com/microsoft/ApplicationInsights-dotnet/pull/1170)
- BASE: [Fix: Channels not handling AggregateException, not logging full HttpRequestException.](https://github.com/microsoft/ApplicationInsights-dotnet/issues/1173)
- BASE: [Metric Aggregator background thread safeguards added to never throw unhandled exception.](https://github.com/microsoft/ApplicationInsights-dotnet/issues/1179)
- BASE: [Updated version of System.Diagnostics.DiagnosticSource to 4.6.0-preview7.19362.9. Also remove marking SDK as CLS-Compliant](https://github.com/microsoft/ApplicationInsights-dotnet/pull/1183)
- BASE: [Enhancement: Exceptions thrown by the TelemetryConfiguration will now specify the exact name of the property that could not be parsed from a config file.](https://github.com/microsoft/ApplicationInsights-dotnet/issues/1194)
- BASE: [Fix: ServerTelemetryChannel constructor exception when network info API throws.](https://github.com/microsoft/ApplicationInsights-dotnet/issues/1184)
- BASE: [Make BaseSDK use W3C Trace Context based correlation by default. Set TelemetryConfiguration.EnableW3CCorrelation=false to disable this.](https://github.com/microsoft/ApplicationInsights-dotnet/pull/1193)
- BASE: [Removed TelemetryConfiguration.EnableW3CCorrelation. Users should do Activity.DefaultIdFormat = ActivityIdFormat.Hierarchical; Activity.ForceDefaultIdFormat = true; to disable W3C Format](https://github.com/microsoft/ApplicationInsights-dotnet/issues/1198)
- BASE: [Enable sampling based on upstream sampling decision for adaptive sampling](https://github.com/microsoft/ApplicationInsights-dotnet/pull/1200)
- BASE: [Fix: StartOperation ignores user-provided custom Ids in scope of Activity](https://github.com/microsoft/ApplicationInsights-dotnet/pull/1205)
- BASE: [Set tracestate if available on requests and dependencies](https://github.com/microsoft/ApplicationInsights-dotnet/pull/1207)

## Version 2.11.0-beta1
- BASE: [Performance fixes: Support Head Sampling; Remove NewGuid(); Sampling Flags; etc... ](https://github.com/microsoft/ApplicationInsights-dotnet/pull/1158)
- BASE: [Deprecate TelemetryConfiguration.Active on .NET Core in favor of dependency injection pattern](https://github.com/microsoft/ApplicationInsights-dotnet/pull/1152)
- BASE: [Make TrackMetric() visible. This method is not recommended unless you are sending pre-aggregated metrics.](https://github.com/microsoft/ApplicationInsights-dotnet/pull/1149)

## Version 2.10.0
- BASE: [SDKVersion modified to be dotnetc for NetCore. This helps identify the source of code path, as implementations are slightly different for NetCore.](https://github.com/microsoft/ApplicationInsights-dotnet/pull/1125)
- BASE: [Fix telemetry timestamp precision on .NET Framework](https://github.com/microsoft/ApplicationInsights-dotnet-server/issues/1175)
- BASE: [Fix: catch SecurityException when ApplicationInsights config is read in partial trust](https://github.com/microsoft/ApplicationInsights-dotnet/pull/1119)

## Version 2.10.0-beta4
- BASE: [Fix NullReferenceException in DiagnosticsEventListener.OnEventWritten](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/1106)
- BASE: [Fix RichPayloadEventSource can get enabled at Verbose level](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/1108)
- BASE: [Fix DiagnosticsTelemetryModule can get added more than once](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/1111)
- BASE: [Modify default settings for ServerTelemetryChannel to reduce disk usage. MaxTransmissionSenderCapacity is changed from 3 to 10, MaxTransmissionBufferCapacity is changed from 1MB to 5MB](https://github.com/Microsoft/ApplicationInsights-dotnet/pull/1113)

## Version 2.10.0-beta3
- BASE: No changes. Bumping version to match WebSDK release.

## Version 2.10.0-beta2
- BASE: [Fix Transmission in NETCORE to handle partial success (206) response from backend.](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/1047)
- BASE: [Fix data losses in ServerTelemetryChannel.](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/1049)
- BASE: [InitialSamplingRate is now correctly applied if set in applicationInsights.config] (https://github.com/Microsoft/ApplicationInsights-dotnet/pull/1048)
- BASE: Added new target for NetStandard2.0.

## Version 2.9.1
- BASE: [Aggregation thread is now a background thread](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/1080)

## Version 2.9.0
- BASE: [Move W3C methods from Web SDK](https://github.com/Microsoft/ApplicationInsights-dotnet/pull/1064)

## Version 2.9.0-beta3
- BASE: [Flatten IExtension and Unknown ITelemetry implementations for Rich Payload Event Source consumption](https://github.com/Microsoft/ApplicationInsights-dotnet/pull/1017)
- BASE: [Fix: Start/StopOperation with W3C distributed tracing enabled does not track telemetry](https://github.com/Microsoft/ApplicationInsights-dotnet/pull/1031)
- BASE: [Fix: Do not run metric aggregation on a thread pool thread](https://github.com/Microsoft/ApplicationInsights-dotnet/pull/1028)

## Version 2.9.0-beta2
- BASE: [Remove unused reference to System.Web.Extensions](https://github.com/Microsoft/ApplicationInsights-dotnet/pull/956)
- BASE: [PageViewTelemetry](https://github.com/Microsoft/ApplicationInsights-dotnet/blob/8673ed1d15005713755e0bb9594acfe0ee00b869/src/Microsoft.ApplicationInsights/DataContracts/PageViewTelemetry.cs) now supports [ISupportMetrics](https://github.com/Microsoft/ApplicationInsights-dotnet/blob/39a5ef23d834777eefdd72149de705a016eb06b0/src/Microsoft.ApplicationInsights/DataContracts/ISupportMetrics.cs)
- BASE: [Fixed a bug in TelemetryContext which prevented rawobject store to be not available in all sinks.](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/974)
- BASE: [Fixed a bug where TelemetryContext would have missing values on secondary sinks](https://github.com/Microsoft/ApplicationInsights-dotnet/pull/993)
- BASE: [Fixed race condition in BroadcastProcessor which caused it to drop TelemetryItems](https://github.com/Microsoft/ApplicationInsights-dotnet/pull/995)
- BASE: [Custom Telemetry Item that implements ITelemetry is no longer dropped, bur rather serialized as EventTelemetry and handled by the channels accordingly](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/988)
- BASE: [IExtension is now serialized into the Properties and Metrics](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/1000)

Perf Improvements.
- BASE: [Improved Perf of ITelemetry JsonSerialization](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/997)
- BASE: [Added new method on TelemetryClient to initialize just instrumentation. This is to be used by autocollectors to avoid calling TelemetryInitializers twice.](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/966)
- BASE: [RequestTelemetry modified to lazily instantiate ConcurrentDictionary for Properties](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/969)
- BASE: [RequestTelemetry modified to not service public fields with data class to avoid converting between types.](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/965)
- BASE: [Dependency Telemetry modified to lazily instantiate ConcurrentDictionary for Properties](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/1002)
- BASE: [Avoid string allocations in Metrics hot path](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/1004)

## Version 2.8.1
[Patch release addressing perf regression.](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/952)

## Version 2.8.0
- BASE: [New API to store/retrieve any raw objects on TelemetryContext to enable AutoCollectors to pass additional information for use by TelemetryInitializers.](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/937)
- BASE: Perf Improvements
	https://github.com/Microsoft/ApplicationInsights-dotnet/issues/927
	https://github.com/Microsoft/ApplicationInsights-dotnet/issues/930
	https://github.com/Microsoft/ApplicationInsights-dotnet/issues/934
- BASE: Fix: [Response code shouldn't be overwritten to 200 if not set](https://github.com/Microsoft/ApplicationInsights-dotnet/pull/918)

## Version 2.8.0-beta2
- BASE: [TelemetryProcessors (sampling, autocollectedmetricaggregator), TelemetryChannel (ServerTelemetryChannel) added automatically to the default ApplicationInsights.config are moved under the default telemetry sink.](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/907)
	If you are upgrading, and have added/modified TelemetryProcessors/TelemetryChannel, make sure to copy them to the default sink section.

## Version 2.8.0-beta1
- BASE: [Add a new distinct properties collection, GlobalProperties, on TelemetryContext, and obsolete the Properties on TelemetryContext.](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/820)
- BASE: [Added support for strongly typed extensibility for Telemetry types using IExtension.](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/871)
- BASE: [New method SerializeData(ISerializationWriter writer) defined in ITelemetry. All existing types implement this method to emit information about it's fields to channels who can serialize this data]
   (continuation of https://github.com/Microsoft/ApplicationInsights-dotnet/issues/871)
- BASE: [Allow to track PageViewPerformance data type](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/673).
- BASE: Added method `ExceptionDetailsInfoList` on `ExceptionTelemetry` class that gives control to user to update exception
message and exception type of underlying `System.Exception` object that user wants to send to telemetry. Related discussion is [here](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/498).
- BASE: Added an option of creating ExceptionTelemetry object off of custom exception information rather than a System.Exception object.
- BASE: [Add support for hex values in config](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/551)

## Version 2.7.2
- BASE: Metrics: Renamed TryTrackValue(..) into TrackValue(..).
- BASE: Metrics: Removed some superfluous public constants.

## Version 2.7.0-beta3
- BASE: [Allow to set flags on event](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/844). It will be used in conjunction with the feature that will allow to keep IP addresses.
- BASE: [Fix: SerializationException resolving Activity in cross app-domain calls](https://github.com/Microsoft/ApplicationInsights-dotnet-server/issues/613)
- BASE: [Make HttpClient instance static to avoid re-creating with every transmission. This had caused connection/memory leaks in .net core 2.1](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/594)
  Related: (https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/690)

## Version 2.7.0-beta2
- BASE: [Fix: NullReferenceException if telemetry is tracked after TelemetryConfiguration is disposed](https://github.com/Microsoft/ApplicationInsights-dotnet-server/issues/928)
- BASE: [Move the implementation of the extraction of auto-collected (aka standard) metrics from internal legacy APIs to the recently shipped metric aggregation APIs.](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/806)
- BASE: [Fix: NullReferenceException in ExceptionConverter.GetStackFrame if StackFrame.GetMethod() is null](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/819)

## Version 2.7.0-beta1 
- BASE: [Extend the Beta period for Metrics Pre-Aggregation features shipped in 2.6.0-beta3.](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/785)
- BASE: [New: Added TryGetOperationDetail to DependencyTelemetry to facilitate advanced ITelemetryInitializer scenarios.  Allows ITelemetryInitializer implementations to specify fields that would otherwise not be sent automatically to the backend.](https://github.com/Microsoft/ApplicationInsights-dotnet-server/issues/900)

## version 2.6.4
- BASE: [Revert changed namespace: `SamplingPercentageEstimatorSettings`, `AdaptiveSamplingPercentageEvaluatedCallback`](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/727)
- BASE: [Add netstandard2.0 target for TelemetryChannel which doesn't have a dependency on Newtonsoft.Json ](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/800)

## Version 2.6.1 
- BASE: [Extend the Beta period for Metrics Pre-Aggregation features shipped in 2.6.0-beta3.](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/785)
- BASE: [Fix: changed namespace SamplingPercentageEstimatorSettings](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/727)

## Version 2.6.0-beta4
- BASE: [New: Enable ExceptionTelemetry.SetParsedStack for .Net Standard](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/763)
- BASE: [Fix: TelemetryClient throws NullReferenceException on Flush if the underlying configuration was disposed](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/755)

## Version 2.6.0-beta3
- BASE: [Report internal errors from Microsoft.AspNet.TelemteryCorrelation module](https://github.com/Microsoft/ApplicationInsights-dotnet/pull/744)
- BASE: [Fix: Telemetry tracked with StartOperation is tracked outside of corresponding activity's scope](https://github.com/Microsoft/ApplicationInsights-dotnet-server/issues/864)
- BASE: [Fix: TelemetryProcessor chain building should also initialize Modules.](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/549)
- BASE: [Fix: Wrong error message in AutocollectedMetricsExtractor.](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/611)
- BASE: [NEW: Interface and Configuration: IApplicationIdProvider.](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/739)
- BASE: [NEW: Metrics Pre-Aggregation: New `TelemetryClient.GetMetric(..).TrackValue(..)` and related APIs always locally pre-aggregate metrics before sending. They are replacing the legacy `TelemetryClient.TrackMetric(..)` APIs.](https://github.com/Microsoft/ApplicationInsights-dotnet/pull/735) ([More info](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/643).)

## Version 2.6.0-beta2
- BASE: [Changed signature of TelemetryClient.TrackDependency](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/684)
- BASE: [Added overload of TelemetryClientExtensions.StartOperation(Activity activity).](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/644)
- BASE: [Finalize the architecture for adding default heartbeat properties (supporting proposal from Issue #636).](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/666).

## Version 2.5.1
- BASE: Fix for missing TelemetryContext. Thank you to our community for discovering and reporting this issue! 
  [Logic bug within Initialize() in TelemetryContext](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/692),
  [Dependency correlation is broken after upgrade to .NET SDK 2.5](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/706),
  [Lost many context fields in 2.5.0](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/708)

## Version 2.5.0-beta2
- BASE: Remove calculation of sampling-score based on Context.User.Id [Issue #625](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/625)
- BASE: New sdk-driven "heartbeat" functionality added which sends health status at pre-configured intervals. See [extending heartbeat properties doc for more information](./docs/ExtendingHeartbeatProperties.md)
- BASE: Fixes a bug in ServerTelemetryChannel which caused application to crash on non-windows platforms. 
			[Details on fix and workaround #654](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/654)
			Original issue (https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/551)
- BASE: [Fixed a bug with the `AdaptiveSamplingTelemetryProcessor` that would cause starvation over time. Issue #756 (dotnet-server)](https://github.com/Microsoft/ApplicationInsights-dotnet-server/issues/756)
- BASE: Updated solution to build on Mac!

## Version 2.5.0-beta1
- BASE: Method `Sanitize` on classes implementing `ITelemetry` no longer modifies the `TelemetryContext` fields. Serialized event json and ETW event will still have context tags sanitized.
- BASE: Application Insights SDK supports multiple telemetry sinks. You can configure more than one channel for telemetry now.
- BASE: New method `DeepClone` on `ITelemetry` interface. Implemented by all supported telemetry items.
- BASE: Server telemetry channel NuGet support a netstandard1.3 target with fixed rate sampling and adaptive sampling telemetry processors.
- BASE: Instrumentation key is no longer required for TelemetryClient to send data to channel(s). This makes it easier to use the SDK with channels other than native Application Insights channels.
- BASE: .NET 4.0 targets were removed. Please use the version 2.4.0 if you cannot upgrade your application to the latest framework version.
- BASE: Removed `wp8`, `portable-win81+wpa81` and `uap10.0` targets.

## Version 2.4.0
- BASE: Updated version of DiagnosticSource to 4.4.0 stable

## Version 2.4.0-beta5
- BASE: Updated version of DiagnosticSource referenced.

## Version 2.4.0-beta4
- BASE: Made Metric class private and fixed various metrics related issues.

## Version 2.4.0-beta3

## Version 2.4.0-beta2
- BASE: Removed metric aggregation functionality as there is not enough feedback on the API surface yet.

## Version 2.4.0-beta1
- BASE: Event telemetry is set to be sampled separately from all other telemetry types. It potentially can double the bill. The reason for this change is that Events are mostly used for usage analysis and should not be subject to sampling on high load of requests and dependencies. Edit `ApplicationInsights.config` file to revert to the previous behavior.
- BASE: Added dependency on System.Diagnostics.DiagnosticsSource package. It is still possible to use standalone Microsoft.ApplicationInsights.dll to track telemetry.
- BASE: StartOperation starts a new System.Diagnostics.Activity and stores operation context in it. StartOperation overwrites OperationTelemetry.Id set before or during telemetry initialization for the dependency correlation purposes.
- BASE: OperationCorrelationTelemetryInitializer initializes telemetry from the Activity.Current. Please refer to https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/ActivityUserGuide.md for more details about Activity and how to use it
- BASE: `Request.Success` field will not be populated based on `ResponseCode`. It needs to be set explicitly.
- BASE: New "ProblemId" property on ExceptionTelemetry. It can be used to set a custom ProblemId value.
- BASE: Metric Aggregation functionality (originally added in 2.3.0-beta1 but removed in 2.3.0) is re-introduced.
- BASE: Improved exception stack trace data collection for .NET Core applications.

## Version 2.3.0
- BASE: Includes all changes since 2.2.0 stable release.
- BASE: Removed metric aggregation functionality added in 2.3.0-beta1 release.
- BASE: [Fixed a bug which caused SDK to stop sending telemetry.](https://github.com/Microsoft/ApplicationInsights-dotnet-server/issues/480)

## Version 2.3.0-beta3
- BASE: [Added overloads of TelemetryClientExtensions.StartOperation.](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/163)
- BASE: Fire new ETW events for Operation Start/Stop.

## Version 2.3.0-beta2
- BASE: Added constructor overloads for TelemetryConfiguration and added creation of a default InMemoryChannel when no channel is specified for a new instance.
  TelemetryClient will no longer create an InMemoryChannel on the configuration instance if TelemetryChannel is null, instead the configuration instances will always have a channel when created.
- BASE: TelemetryConfiguration will no longer dispose of user provided ITelemetryChannel instances.  Users must properly dispose of any channel instances which they create, the configuration will only auto-dispose of default channel instances it creates when none are specified by the user.

## Version 2.3.0-beta1
- BASE: Added metric aggregation functionality via MetricManager and Metric classes.
- BASE: Exposed a source field on RequestTelemetry. This can be used to store a representation of the component that issued the incoming http request. 

## Version 2.2.0
- BASE: Includes all changes since 2.1.0 stable release.

## Version 2.2.0-beta6
- BASE: Added serialization of the "source" property.
- BASE: Downgraded package dependencies to Microsoft.NETCore.App 1.0.1 level.
- BASE: [Fixed the priority of getting an iKey from an environment variable](https://github.com/Microsoft/ApplicationInsights-dotnet-server/issues/236)

## Version 2.2.0-beta5
- BASE: Moved from PCL dotnet5.4 to .NET Core NetStandard1.5.
- BASE: Updated dependency versions.

## Version 2.2.0-beta4
- BASE: Payload sanitization for RichPayloadEventSource.
- BASE: Fix to fallback to an environment variable for instrumentation key when not specified when initializing TelemetryConfiguration.
- BASE: RoleInstance and NodeName are initialized with the machine name by default.

## Version 2.2.0-beta3

- BASE: Read InstrumentationKey from environment variable APPINSIGHTS_INSTRUMENTATIONKEY if it is was not provided inline. If provided it overrides what is set though configuration file. (Feature is not available in PCL version of SDK).
- BASE: Context properties `NetworkType`, `ScreenResolution` and `Language` marked as obsolete. Please use custom properties to report network type, screen resolution and language. Values stored in these properties will be send as custom properties. 
- BASE: Dependency type was updated to reflect the latest developments in Application Insights Application Map feature. You can set a new field - `Target`. `CommandName` was renamed to `Data` for consistency with the Application Analytics schema. `DependencyKind` will never be send any more and will not be set to "Other" by default. Also there are two more constructors for `DependencyTelemetry` item.
- BASE: Type `SessionStateTelemetry` was marked obsolete. Use `IsFirst` flag in `SessionContext` to indicate that the session is just started.
- BASE: Type `PerformanceCounterTelemetry` was marked obsolete. Use `MetricTelemetry` instead.
- BASE: Marked `RequestTelemetry.HttpMethod` as obsolete. Put http verb as part of the name for the better grouping by name and use custom properties to report http verb as a dimension.
- BASE: Marked `RequestTelemetry.StartTime` as obsolete. Use `TimeStamp` instead.
- BASE: [Removed BCL dependency](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/175)
- BASE: [Added IPv6 support](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/316)
- BASE: [Fixed an issue where channels sent expired data from storage](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/278)
- BASE: [Fixed an issue where the clock implementation would accumulate error](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/271)
- BASE: [Fixed an issue where telemetry with emptry properties would be dropped](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/319)
- BASE: [Added support for SDK-side throttling](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/318)

## Version 2.2.0-beta2

- BASE: InMemoryChannel has a new override for Flush method that accepts timeout.
- BASE: Local storage folder name was changed. That means that when the application stopped, and the application was updated to the new SDK, then the telemetry from the old local folder will not be send.
- BASE: Allow all characters in property names and measurements names.
- BASE: AdaptiveTelemetryProcessor has a new property IncludedTypes. It gets or sets a semicolon separated list of telemetry types that should be sampled. If left empty all types are included implicitly. Types are not included if they are set in ExcludedTypes.
- BASE: Richpayload event source event is generated for all framework versions of SDK (before it was supported in 4.6 only)
- BASE: TelemetryClient has a new method TrackAvailability. Data posted using this method would be available in AppAnalitics only, Azure portal UI is not available at this moment.

## Version 2.2.0-beta1

- BASE: Add ExceptionTelemetry.Message property. If it is provided it is used instead of Exception.Message property for the outer-most exception.
- BASE: Telemetry types can be excluded from sampling by specifing ExcludedTypes property. 
- BASE: ServerTelemetryChannel: changed backoff logic to be less aggressive, added diagnostics event when backoff logic kicks in and added more tracing. (Done to address issues when data stops flowing till application gets restarted)

## Version 2.1.0-beta4
- BASE: [Bug fix](https://github.com/Microsoft/ApplicationInsights-dotnet-server/issues/76)

## Version 2.1.0-beta3
- BASE: Support partial success (206) from the Application Insights backend. Before this change SDK may have lost data because some items of the batch were accepted and some items of the batch were asked to be retried (because of burst throttling or intermittent issues).
- BASE: Bug fixes

## Version 2.1.0-beta1

- BASE: Upgraded to depend on EventSource.Redist nuget version 1.1.28
- BASE: Upgraded to depend on Microsoft.Bcl nuget version 1.1.10

## Version 2.0.1

- BASE: Add Win Phone, Win Store and UWP targets that include 1.2.3 version of ApplicationInsights.dll. It is included to prevent applications that upgrade to 2.0.0 from crashing. In any case using this nuget for Win Phone, Win Store and UWP targets is not recommended and not supported. 

## Version 2.0.0

- BASE: Disallow Nan, +-Infinity measurements. Value will be replaced on 0.
- BASE: Disallow Nan, +-Infinity metrics (Value, Min, Max and StandardDeviation). Values will be replaced on 0.

## Version 2.0.0-rc1

- BASE: Writing telemetry items to debug output can be disabled with ```IsTracingDisabled``` property on ```TelemetryDebugWriter```. 
Telemetry items that were filtered out by sampling are now indicated in debug output. Custom telemetry processors can now invoke
method ```WriteTelemetry``` on ```TelemetryDebugWriter``` with ```filteredBy``` parameter to indicate in debug output that an
item is being filtered out.
- BASE: DependencyTelemetry.Async property was removed.
- BASE: DependencyTelemetry.Count property was removed.
- BASE: When configuration is loaded from ApplicationInsights.config incorrect and broken elements are skipped. That includes both high level elements like TelemetryInitializers as well as individual properties.  
- BASE: Internal Application Insights SDK traces will be marked as synthetic and have `SyntheticSource` equals to 'SDKTelemetry'.
- BASE: UserContext.AcquisitionDate property was removed.
- BASE: UserContext.StoreRegion property was removed.
- BASE: InMemoryChannel.DataUploadIntervalInSeconds was removed. Use SendingInterval instead.
- BASE: DeviceContext.RoleName was removed. Use DeviceContext.Cloud.RoleName instead.
- BASE: DeviceContext.RoleInstance was removed. Use DeviceContext.Cloud.RoleInstance instead.

## Version 2.0.0-beta4

- BASE: UseSampling and UseAdaptiveSampling extensions were moved to Microsoft.ApplicationInsights.Extensibility
- BASE: Cut Phone and Store support
- BASE: Updated ```DependencyTelemetry``` to have new properties ```ResultCode``` and ```Id```
- BASE: If ``ServerTelemetryChannel`` is initialized programmatically it is required to call ServerTelemetryChannel.Initialize() method. Otherwise persistent storage will not be initialized (that means that if telemetry cannot be sent because of temporary connectivity issues it will be dropped).
- BASE: ``ServerTelemetryChannel`` has new property ``StorageFolder`` that can be set either through code or though configuration. If this property is set ApplicationInsights uses provided location to store telemetry that was not sent because of temporary connectivity issues. If property is not set or provided folder is inaccessible ApplicationInsights will try to use LocalAppData or Temp as it was done before.
- BASE: TelemetryConfiguration.GetTelemetryProcessorChainBuilder extension method is removed. Instead of this method use TelemetryConfiguration.TelemetryProcessorChainBuilder instance method.
- BASE: TelemetryConfiguration has a new property TelemetryProcessors that gives readonly access to TelemetryProcessors collection.
- BASE: `Use`, `UseSampling` and `UseAdaptiveSampling` preserves TelemetryProcessors loaded from configuration.

## Version 2.0.0-beta3
- BASE: Adaptive sampling turned on by default in server telemetry channel. Details can be found in [#80](https://github.com/Microsoft/ApplicationInsights-dotnet/issues/80).
- BASE: Fixed signature of ```UseSampling``` to allow chaining with other calls to ```Use``` of telemetry processors.
- BASE: Property ```Request.ID``` returned back. ```OperationContext``` now has a property ```ParentId``` for end-to-end correlation.
- BASE: ```TimestampTelemetryInitializer``` is removed. Timestamp will be added automatically by ```TelemetryClient```.
- BASE: ```OperationCorrelationTelemetryInitializer``` is added by default to enable operaitons correlation.

## Version 2.0.0-beta2
- BASE: Fix UI thread locking when initializing InMemoryChannel (default channel) from UI thread.
- BASE: Added support for ```ITelemetryProcessor``` and ability to construct chain of TelemetryProcessors via code or config.
- BASE: Version of ```Microsoft.ApplicationInsights.dll``` for the framework 4.6 is now part of the package.
- BASE: IContextInitializer interface is not supported any longer. ContextInitializers collection was removed from TelemetryConfiguration object.
- BASE: The max length limit for the ```Name``` property of ```EventTelemetry``` was set to 512.
- BASE: Property ```Name``` of ```OperationContext``` was renamed to ```RootName```
- BASE: Property ```Id``` of ```RequestTelemetry``` was removed.
- BASE: Property ```Id``` and ```Context.Operation.Id``` of ```RequestTelemetry``` would not be initialized when creating new ```RequestTelemetry```.
- BASE: New properties of ```OperationContext```: ```CorrelationVector```, ```ParentId``` and ```RootId``` to support end-to-end telemetry items correlation.

## Version 2.0.0-beta1

- BASE: TrackDependency will produce valid JSON when not all required fields were specified.
- BASE: Redundant property ```RequestTelemetry.ID``` is now just a proxy for ```RequestTelemetry.Operation.Id```.
- BASE: New interface ```ISupportSampling``` and explicit implementation of it by most of data item types.
- BASE: ```Count``` property on DependencyTelemetry marked as Obsolete. Use ```SamplingPercentage``` instead.
- BASE: New ```CloudContext``` introduced and properties ```RoleName``` and ```RoleInstance``` moved to it from ```DeviceContext```.
- BASE: New property ```AuthenticatedUserId``` on ```UserContext``` to specify authenticated user identity.

## Version 1.2.3
- BASE: Bug fixes.
- BASE: Telemetry item will be serialized to Debug Output even when Instrumentation Key was not set.

## Version 1.2
- BASE: First version shipped from github

## Version 1.1

- BASE: SDK now introduces new telemetry type ```DependencyTelemetry``` which contains information about dependency call from application
- BASE: New method ```TelemetryClient.TrackDependency``` allows to send information about dependency calls from application

## Version 0.17

- BASE: Application Insights now distributes separate binaries for framework 4.0 and 4.5. Library for the framework 4.5 will not require EventSource and BCL nuget dependencies. You need to ensure you refer the correct library in ```packages.config```. It should be ```<package id="Microsoft.ApplicationInsights" version="0.17.*" targetFramework="net45" />```
- BASE: Diagnostics telemetry module is not registered in ApplicationInsights.config and no self-diagnostics messages will be sent to portal for non-web applications. Insert ```<Add Type="Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.DiagnosticsTelemetryModule, Microsoft.ApplicationInsights" />``` to ```<TelemetryModules>``` node to get SDK self-diagnostics messages from your application.
- BASE: ApplicationInsights.config file search order was changed. File from the bin/ folder will no longer be used even if exists for the web applications.
- BASE: Nullable properties are now supported in ApplicationInsights.config.
- BASE: DeveloperMode property of ```ITelemetryChannel``` interface became a nullable bool.

## Version 0.16

- BASE: SDK now supports dnx target platform to enable monitoring of [.NET Core framework](http://www.dotnetfoundation.org/NETCore5) applications.
- BASE: Instance of ```TelemetryClient``` do not cache Instrumentation Key anymore. Now if instrumentation key wasn't set to ```TelemetryClient``` explicitly ```InstrumentationKey``` will return null. It fixes an issue when you set ```TelemetryConfiguration.Active.InstrumentationKey``` after some telemetry was already collected, telemetry modules like dependency collector, web requests data collection and performance counters collector will use new instrumentation key.

## Version 0.15

- BASE: New property ```Operation.SyntheticSource``` now available on ```TelemetryContext```. Now you can mark your telemetry items as “not a real user traffic” and specify how this traffic was generated. As an example by setting this property you can distinguish traffic from your test automation from load test traffic.
- BASE: Channel logic was moved to the separate NuGet called Microsoft.ApplicationInsights.PersistenceChannel. Default channel is now called InMemoryChannel
- BASE: New method ```TelemetryClient.Flush``` allows to flush telemetry items from the buffer synchronously

## Version 0.13

No release notes for older versions available.
