# Application Insights DotNet SDK Concepts
This lists the high level concepts of the AI DotNet SDK and links to detailed guides to help you get started.

To use the Application Insights SDK you must configure an Instrumentation Key which can be [obtained from an Application Insights resource](https://docs.microsoft.com/azure/azure-monitor/app/create-new-resource).
Or you can use a [Connection string](https://docs.microsoft.com/azure/azure-monitor/app/sdk-connection-string?tabs=net) to simplify the configuration of our endpoints.
Both an Instrumentation Key and Connection String are provided for you on the Overivew Dashboard of your Application Insights resource.

## TelemetryClient
The `TelemetryClient` object is the primary root object for the library. 
Almost all functionality around telemetry sending is located on this object. 

### Initialization
You must initialize an instance of this object and populate it with your Instrumentation Key to identify your data.

```C#
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

var configuration = new TelemetryConfiguration
{
    ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000",
};
var tc = new TelemetryClient(configuration);
```

### Using the TelemetryClient to send telemetry
You can populate common context on the `TelemetryClient.context` property which will be automatically attached to each telemetry item sent. 
You can also attach additional property data to each telemetry item sent. 
The `TelemetryClient` also exposes several `Track` methods that can be used to send all telemetry types understood by the Application Insights service. Some example use cases are shown below.

Please review the full [API summary for custom events and metrics](https://docs.microsoft.com/azure/azure-monitor/app/api-custom-events-metrics) for more examples.

```C#
tc.TrackTrace(message: "Custom message.");

tc.TrackEvent(
    eventName: "PurchaseOrderSubmitted", 
    properties: new Dictionary<string, string>() { { "CouponCode", "JULY2015" } }
    );
	
try
{
    // do something
}
catch(Exception ex)
{
    tc.TrackException(ex);
}
``` 

## Telemetry correlation
Application Insights supports distributed telemetry correlation, which you use to detect which component is responsible for failures or performance degradation.

Please review our full guide on [Telemetry correlation in Application Insights](https://docs.microsoft.com/azure/azure-monitor/app/correlation).


## Sampling
Sampling is the recommended way to reduce telemetry traffic, data costs, and storage costs, while preserving a statistically correct analysis of application data.

Please review our full guide on [Sampling in Application Insights](https://docs.microsoft.com/azure/azure-monitor/app/sampling).
