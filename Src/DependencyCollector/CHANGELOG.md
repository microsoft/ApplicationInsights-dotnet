# Changelog 

## Version 2.0.0-beta5

- Http dependency success is determined on the base of http status code. Before it was true if there was no exception. But when one uses HttpClient there is no exceptions so all dependencies were marked as successful. Also in case if response is not available status code was set to -1. Now now status code will be reported.

## Version 1.2.0

- Dependencies collected using runtime instrumentaiton agent (enabled via Status Monitor or Azure WebSite extension) will not be marked as asynchronous if there are no HttpContext.Current on the thread.
- Property ```SamplingRatio``` of ```DependencyTrackingTelemetryModule``` does nothing and marked as obsolete.


## Version 1.0.0

- Change namespace and assembly names from `Microsoft.ApplicationInsights.Extensibility.RuntimeTelemetry` to 
  `Microsoft.ApplicationInsights.Extensibility.DependencyCollector` for consistency with the name of the NuGet package.
- Rename `RemoteDependencyModule` to `DependencyTrackingTelemetryModule`.

## Version 0.17

- Removed dependency to EventSource NuGet for the framework 4.5 applications.

## Version 0.15

No release notes for older versions available. 