# Guidelines for instrumenting partner libraries with Diagnostic Source 

This document provides guidelines for adding Diagnostic Source instrumentation to external libraries in a way that will allow to automatically collect high quality telemetry in Application Insights SDK.

## Diagnostic Source and Activities

[Diagnostic Source][DiagnosticSourceGuide] is a simple module that allows code to be instrumented for production-time logging of rich data payloads for consumption within the process that was instrumented. At runtime consumers can dynamically discover data sources and subscribe to the ones of interest.

[Activity][ActivityGuide] is a class that allows storing and accessing diagnostics context and consuming it with logging system.

### Instrumentation code

The following code sample shows how to instrument the operation logic enclosed in ```ProcessOperationImplAsync()``` method, in the most efficient way which will ensure no performance overhead is added if there are no listeners for that particular Activity.

```C#
private const string DiagnosticSourceName = "Microsoft.ApplicationInsights.Samples";
private const string ActivityName = DiagnosticSourceName + ".ProcessOperation";
private const string ActivityStartName = ActivityName + ".Start";
private const string ActivityStopName = ActivityName + ".Stop";
private const string ActivityExceptionName = ActivityName + ".Exception";

private static readonly DiagnosticListener DiagnosticListener = new DiagnosticListener(DiagnosticSourceName); 

private async Task<OperationOutput> ProcessOperationImplAsync(OperationInput input)
{
    // original code to instrument
}

public Task<OperationOutput> ProcessOperationAsync(OperationInput input)
{
    if (DiagnosticListener.IsEnabled() && 
        DiagnosticListener.IsEnabled(ActivityName) &&
        DiagnosticListener.IsEnabled(ActivityName, input))
    {
        // instrument operation only if there is an active listener
        return this.ProcessOperationInstrumentedAsync(input);
    }

    return this.ProcessOperationImplAsync(input);
}

private async Task<OperationOutput> ProcessOperationInstrumentedAsync(OperationInput input)
{
    Activity activity = new Activity(ActivityName);

    activity.AddTag("component", "Microsoft.ApplicationInsights.Samples");
    activity.AddTag("span.kind", "client");
    // TODO extract activity tags from input

    if (DiagnosticListener.IsEnabled(ActivityStartName))
    {
        DiagnosticListener.StartActivity(activity, new {Input = input});
    }
    else
    {
        activity.Start();
    }

    Task<OperationOutput> outputTask = null;
    OperationOutput output = null;

    try
    {
        outputTask = this.ProcessOperationImplAsync(input);;
        output = await outputTask;

        activity.AddTag("error", "false");
        // TODO extract activity tags from output
    }
    catch (Exception ex)
    {
        activity.AddTag("error", "true");

        if (DiagnosticListener.IsEnabled(ActivityExceptionName))
        {
            DiagnosticListener.Write(ActivityExceptionName, new {Input = input, Exception = ex});
        }
    }
    finally
    {
        if (DiagnosticListener.IsEnabled(ActivityStopName))
        {
            DiagnosticListener.StopActivity(activity,
                new
                {
                    Output = outputTask?.Status == TaskStatus.RanToCompletion ? output : null,
                    Input = input,
                    TaskStatus = outputTask?.Status
                });
        }
        else
        {
            activity.Stop();
        }
    }

    return output;
}
```

## Naming conventions

When populating activity tags it is recommended to follow the [OpenTracing naming convention][OpenTracingNamingConvention].

In addition this guidance defines new tag names which can be used to improve quality of telemetry captured by Application Insights SDK.

## Translating activities to telemetry

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

| Dependency field name | Notes and examples |
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
[OpenTracingNamingConvention]: https://github.com/opentracing/specification/blob/master/semantic_conventions.md#span-tags-table
[AIDataModelRdd]: https://docs.microsoft.com/en-us/azure/application-insights/application-insights-data-model-dependency-telemetry

