# Changelog

This changelog will be used to generate documentation on [release notes page](http://azure.microsoft.com/en-us/documentation/articles/app-insights-release-notes-dotnet/).

## Version vNext

- Property ```Id``` of ```RequestTelemetry``` was marked obsolete.
- New properties of ```OperationContext```: ```ParentId``` and ```RootId``` to support end-to-end telemetry items correlation.
- TrackDependency will produce valid JSON when not all required fields were specified.
- Redundant property ```RequestTelemetry.ID``` is now just a proxy for ```RequestTelemetry.Operation.Id```.
- New interface ```ISupportSampling``` and explicit implementation of it by most of data item types.
- ```Count``` property on DependencyTelemetry marked as Obsolete. Use ```SamplingPercentage``` instead.
- New ```CloudContext``` introduced and properties ```RoleName``` and ```RoleInstance``` moved to it from ```DeviceContext```.
- New property ```AuthenticatedUserId``` on ```UserContext``` to specify authenticated user identity.

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
