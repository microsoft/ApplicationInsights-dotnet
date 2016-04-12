# Changelog

This changelog will be used to generate documentation on [release notes page](http://azure.microsoft.com/en-us/documentation/articles/app-insights-release-notes-dotnet/).

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
- When configuration is loaded from ApplicationInsights.config incorrect and broken elements are skiped. That includes both high level evelemts like TelemetryInitializers as well as individual properties.  
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
- Property ```Request.ID``` returned back. ```OperationContext``` now has a property ```ParentId``` for end-to-end coorrelation.
- ```TimestampTelemetryInitializer``` is removed. Timestamp will be added automatically by ```TelemetryClient```.
- ```OperationCorrelationTelemetryInitializer``` is added by default to enable operaitons correlation.

## Version 2.0.0-beta2
- Fix UI thread locking when initializing InMemoryChannel (default channel) from UI thread.
- Added support for ```ITelemetryProcessor``` and ability to construct chain of TelemetryProcessors via code or config.
- Version of ```Microsoft.ApplicationInsights.dll``` for the framework 4.6 is now part of the package.
- IContextInitializer interface is not supported any longer. ContextInitializers collection was removed from TelemetryConfiguraiton object.
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
- Telemetry item will be serialized to Debug Ouput even when Instrumentaiton Key was not set.

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
