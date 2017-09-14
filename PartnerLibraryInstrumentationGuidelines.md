# Guidelines for instrumenting partner libraries with Diagnostic Source 

This document provides guidelines for adding Diagnostic Source instrumentation to external libraries in a way that will allow to automatically collect high quality telemetry in Application Insights SDK.

## Diagnostic Source and Activities

[Diagnostic Source][DiagnosticSourceGuide] is a simple module that allows code to be instrumented for production-time logging of rich data payloads for consumption within the process that was instrumented. At runtime consumers can dynamically discover data sources and subscribe to the ones of interest.

[Activity][ActivityGuide] is a class that allows storing and accessing diagnostics context and consuming it with logging system.

Both Diagnostic Source and Activity have been used to instrument [System.Net.Http][SystemNetHttp] and [Microsoft.AspNetCore.Hosting][MicrosoftAspNetCoreHosting], although that instrumentation is not fully complaiant with this guidance.

[This document][DiagnosticSourceActivityHowto] can help to a better understanding on how to efficiently use Diagnostic Source.

### Instrumentation code

The following code sample shows how to instrument the operation logic enclosed in ```ProcessOperationImplAsync()``` method, in the most efficient way which will ensure no performance overhead is added if there are no listeners for that particular Activity.

```C#
private const string DiagnosticSourceName = "Microsoft.ApplicationInsights.Samples";
private const string ActivityName = DiagnosticSourceName + ".ProcessOperation";
private const string ActivityStartName = ActivityName + ".Start";
private const string ActivityExceptionName = ActivityName + ".Exception";

private static readonly DiagnosticListener DiagnosticListener = new DiagnosticListener(DiagnosticSourceName); 

private async Task<OperationOutput> ProcessOperationImplAsync(OperationInput input)
{
    // original code to instrument
}

public Task<OperationOutput> ProcessOperationAsync(OperationInput input)
{
    // any Diagnostic Source has any listeners
    if (DiagnosticListener.IsEnabled())
    {
        // is any listener interested in activity for this request?
        bool isActivityEnabled = DiagnosticListener.IsEnabled(ActivityName, input);

        // is any listener interested in activity exception?
        bool isExceptionEnabled = DiagnosticListener.IsEnabled(ActivityExceptionName);

        if (isActivityEnabled || isExceptionEnabled)
        {
            return this.ProcessOperationInstrumentedAsync(input, isActivityEnabled, isExceptionEnabled);
        }
    }

    // no one listens - run without instrumentation
    return this.ProcessOperationImplAsync(input);
}

private async Task<OperationOutput> ProcessOperationInstrumentedAsync(OperationInput input)
{
    Activity activity = null;

    // create and start activity if enabled
    if (isActivityEnabled)
    {
        activity = new Activity(ActivityName);

        activity.AddTag("component", "Microsoft.ApplicationInsights.Samples");
        activity.AddTag("span.kind", "client");
        // TODO extract activity tags from input

        // most of the times activity start event is not interesting, 
        // in such case start activity without firing event
        if (DiagnosticListener.IsEnabled(ActivityStartName))
        {
            DiagnosticListener.StartActivity(activity, new {Input = input});
        }
        else
        {
            activity.Start();
        }
    }

    Task<OperationOutput> outputTask = null;
    OperationOutput output = null;

    try
    {
        outputTask = this.ProcessOperationImplAsync(input);;
        output = await outputTask;

        if (activity != null)
        {
            // TODO extract activity tags from output
        }
    }
    catch (Exception ex)
    {
        if (isExceptionEnabled)
        {
            DiagnosticListener.Write(ActivityExceptionName, new { Input = input, Exception = ex });
        }
    }
    finally
    {
        if (activity != null)
        {
            // stop activity
            activity.AddTag("error", (outputTask?.Status == TaskStatus.RanToCompletion).ToString());
            DiagnosticListener.StopActivity(activity,
                new
                {
                    Output = outputTask?.Status == TaskStatus.RanToCompletion ? output : null,
                    Input = input,
                    TaskStatus = outputTask?.Status
                });
        }
    }

    return output;
}
```

### Performance considerations

In the sample code above different flavors of ```IsEnabled()``` method are called in this particular order: 

* ```DiagnosticListener.IsEnabled()``` - checks if there is any listener for this diagnostic source. This is a very efficient preliminary check for listeners.
* ```DiagnosticListener.IsEnabled(ActivityName, input1, input2, ...)``` - checks if there is any listener for this activity and allows the listener to inspect the input parameters to make the decision. The input parameters passed in this method should be useful for the listeners to determine whether the activity would be interesting or not
* ```DiagnosticListener.IsEnabled(ActivityStartName)``` - checks if there is any listener for activity `Start` event. Typically only the activity `Stop` event is interesting   
* ```DiagnosticListener.IsEnabled(ActivityExceptionName)``` - checks if there is any listener for activity `Exception`. The code sample above supports a scenario where there is an active listener for the exception but none for the activity itself

It is also worth to note that in case when there is no listener for given activity the asynchronous operation task is not being awaited and is directly returned to the caller.  

All of these checks are made to ensure that no performance overhead is added in case when there are no active listeners for the given activity.  

## Activity tags

When populating activity tags it is recommended to follow the [OpenTracing naming convention][OpenTracingNamingConvention].

A couple of tags defined by that convention have significant meaning and should be present in all activities:

| Tag | Notes and examples |
|:--------------|:-------------------|
| `span.kind` | Indicates the role in the processing flow that activity is representing - e.g. it can be `client` vs `server` which corresondingly denote performing outgoing operation or processing incoming request.  |
| `error` | Indicates whether activity completed successfully or not. |
| `component`  | Indicates the source component from which the activities originate. This can be the library or service name. The difference between this tag and Diagnostic Source name is that a single library may use more than one Diagnostic Sources (in fact this is recommended in certain scenarios), however it should consistently use the same `component` tag  |


In addition, in the later sections, this guidance defines new tag names which can be used to improve quality of telemetry captured by Application Insights SDK.


## Capturing activities as Application Insights telemetry

Upon being notified of Diagnostic Source activity event that Application Insights SDK is listening for, the event is attempted to be converted into one of the standard telemetry types. Below are the details specific to conversion for those supported types  

### Common telemetry context

All telemetry items will have the following context properties populated.

| Telemetry field name | Notes and examples |
|:--------------|:-------------------|
| `Operation ID` | ```Activity.Current.RootId``` |
| `Parent Operation ID` | ```Activity.Current.ParentId``` |
| `Timestamp` | ```Activity.Current.StartTimeUtc``` |
| `DiagnosticSource` context property | The name of the originating Diagnostic Source |
| `Activity` context property | ```Activity.Current.OperationName``` |

In addition all activity ```Activity.Current.Baggage``` properties will be added to context properties.

### Dependency telemetry

Dependency telemetry is collected based on all Activity `Stop` events. 

If the activity specifies `span.kind` tag with value matching `server` or `consumer` then the dependency telemetry will not be collected as those typically should be treated as Request telemetry.

The table below presents how each [Dependency telemetry][AIDataModelRdd] field is being populated based on the captured activity 

| Dependency field name | How obtained? |
|:--------------|:-------------------|
| `Name` | `operation.name` tag, or <br/> `http.method` + `http.url` tags, or <br/> ```Activity.Current.OperationName``` |
| `ID` | ```Activity.Current.Id``` |
| `Data` | `operation.data` tag |
| `Type` | `operation.type` tag, or <br/> `peer.service` tag, or <br/> `component` tag |
| `Target` | `peer.hostname` tag, or <br/> host name parsed out of `http.url` tag, or <br/> `peer.address` tag |
| `Duration` | ```Activity.Current.Duration``` |
| `Result code` | `http.status_code` tag |
| `Success` | negated value of `error` tag (if can be parsed as ```bool```) |
| `Custom properties` | all tags and baggage properties |
| `Custom measurements` | not populated |


[DiagnosticSourceGuide]: https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/DiagnosticSourceUsersGuide.md
[ActivityGuide]: https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/ActivityUserGuide.md
[DiagnosticSourceActivityHowto]: https://github.com/lmolkova/correlation/wiki/How-to-instrument-library-with-Activity-and-DiagnosticSource
[OpenTracingNamingConvention]: https://github.com/opentracing/specification/blob/master/semantic_conventions.md#span-tags-table
[AIDataModelRdd]: https://docs.microsoft.com/en-us/azure/application-insights/application-insights-data-model-dependency-telemetry
[SystemNetHttp]: https://github.com/dotnet/corefx/blob/master/src/System.Net.Http/src/System/Net/Http/DiagnosticsHandler.cs
[MicrosoftAspNetCoreHosting]: https://github.com/aspnet/Hosting/blob/dev/src/Microsoft.AspNetCore.Hosting/Internal/HostingApplicationDiagnostics.cs
