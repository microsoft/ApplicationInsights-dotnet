# Migration Guidance from 2.x to 3.x
This guide provides detailed explanations of how to migrate from Application Insights 2.x to 3.x.
One major shift is that 3.x now uses OpenTelemetry & Azure Monitor Exporter internally to emit telemetry to your workspace. As such, there is a large reduction in the number of packages and APIs we will support in 3.x. 

OpenTelemetry vocabulary will be referenced in this guidance. Definitions can be found [here](docs/concepts.md). 

## Previously supported packages that are now unsupported
These are packages from 2.x that are now out of support:
- `Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel`: 3.x internally depends on Azure Monitor Exporter, which serves the same purpose.
- `Microsoft.ApplicationInsights.DependencyCollector`: Microsoft.ApplicationInsights.WorkerService, Microsoft.ApplicationInsights.AspNetCore, and Microsoft.ApplicationInsights.Web have built in mechanisms to automatically collect this telemetry in 3.x
- `Microsoft.ApplicationInsights.EventCounterCollector`: This is discontinued and does not have a direct replacement.
- `Microsoft.ApplicationInsights.PerfCounterCollector`: 3.x packages have built in mechanisms to collect this telemetry automatically. Configuration also exists to disable this collection.
- `Microsoft.ApplicationInsights.WindowsServer`: Telemetry Modules and Initializers are removed in 3.x. See [replacement](#microsoftapplicationinsightswindowsserver-replacement) section below.
- `Microsoft.Extensions.Logging.ApplicationInsights`: 3.x AspNetCore/WorkerService packages autocollect ILogger logs as application insights traces. In addition, all TrackTrace/Exception/Event calls also now use ILogger, which the Azure Monitor Exporter is able to convert to the relevant telemetry payloads. This package is now unnecessary.
- `Microsoft.ApplicationInsights.Log4NetAppender`: This is discontinued due to Log4Net being a legacy framework. Customers should use TraceTrace() or ILogger.
- `Microsoft.ApplicationInsights.TraceListener`: This is discontinued due to System.Diagnostics.Trace being a legacy logging API. Customers should use TrackTrace() or ILogger.
- `Microsoft.ApplicationInsights.DiagnosticSourceListener`: The 3.x AspNetCore/WorkerService/Web packages all automatically collect telemetry from http and sql instrumentations.
- `Microsoft.ApplicationInsights.EtwCollector`: This is discontinued and does not have a direct replacement.
- `Microsoft.ApplicationInsights.EventSourceListener`: The 3.x AspNetCore/WorkerService/Web packages all automatically collect telemetry from http and sql instrumentations.

When upgrading to 3.x, please remove any references of these 2.x packages from your applications. Please also ensure the application does not contain packages that have transitive dependencies on these 2.x packages when upgrading to 3.x.

### Microsoft.ApplicationInsights.WindowsServer replacement
This package previously included below modules and initializers:

#### Telemetry Modules

- `AppServicesHeartbeatTelemetryModule`: AspNetCore/WorkerService/Web 3.x packages all automatically utilize an OpenTelemetry resource detector to send resource defining attributes to a metric called `_APPRESOURCEPREVIEW_`.
- `AzureInstanceMetadataTelemetryModule`: AspNetCore/WorkerService/Web 3.x packages all automatically utilize an OpenTelemetry resource detector to send resource defining attributes to a metric called `_APPRESOURCEPREVIEW_`.
- `DeveloperModeWithDebuggerAttachedTelemetryModule`: This does not have a direct replacement. The DeveloperMode setting has been removed from all packages as the exporter which 3.x relies on has its own batching mechanism for sending telemetry. To flush telemetry in unit test scenarios, one could use TelemetryClient.Flush().
- `FirstChanceExceptionStatisticsTelemetryModule`: This is discontinued and does not have a direct replacement.
- `UnhandledExceptionTelemetryModule`: This is discontinued and does not have a direct replacement.
- `UnobservedExceptionTelemetryModule`: This is discontinued and does not have a direct replacement.

#### Telemetry Initializers
The two telemetry initializers below are partially replaced by resource detectors that send the resource attributes to the `_APPRESOURCEPREVIEW_` metric: 
- `AzureRoleEnvironmentTelemetryInitializer`
- `AzureWebAppRoleEnvironmentTelemetryInitializer`

If one wishes to apply attributes to all telemetry, consider creating a [custom OpenTelemetry processor](#creating-a-custom-opentelemetry-processor) or using the [alternative](#familiar-api-alternative).
- `BuildInfoConfigComponentVersionTelemetryInitializer` 
- `DeviceTelemetryInitializer`
- `DomainNameRoleInstanceTelemetryInitializer`: Was previously marked obsolete and now completely removed.

In 3.x, these modules and initializers are all removed. 

## Initialization Guidance and InstrumentationKey removal
This section highlights changes to initialization of TelemetryConfiguration and TelemetryClient, and defines how to set the connection string once references to instrumentation key are removed.

### Use TelemetryConfiguration.CreateDefault() to create a configuration
This is the recommended way to create a telemetry configuration in 3.x.
```csharp
TelemetryConfiguration.CreateDefault()
```
### Replace InstrumentationKey with ConnectionString
InstrumentationKey configuration is not supported anymore in 3.x. Customers are advised to use their full connection string instead, as that contains endpoint information that is useful for the exporter. 
Additionally, 3.x will throw an exception if a connection string is not provided. For test scenarios, one could supply a dummy string like: `InstrumentationKey=00000000-0000-0000-0000-000000000000`.

See below for how to configure the connection string for each of the packages in 3.x:

#### Microsoft.ApplicationInsights
Via code:
```csharp
var config = TelemetryConfiguration.CreateDefault();
config.ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://...";
```

#### Microsoft.ApplicationInsights.AspNetCore & Microsoft.ApplicationInsights.WorkerService
Choose one of the mechanisms below to set the connection string.

**Via code (AspNetCore)**
```csharp
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://...";
});
```

**Via code (WorkerService)**
```csharp
builder.Services.AddApplicationInsightsTelemetryWorkerService(options =>
{
    options.ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://...";
});
```

**Via appsettings.json**
```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://..."
  }
}
```

**Via Environment Variable**

Set the `APPLICATIONINSIGHTS_CONNECTION_STRING` environment variable. The AspNetCore and WorkerService packages will automatically read it from configuration.


#### Microsoft.ApplicationInsights.Web
Choose one of the options below to set the connection string.

**Via code in Global.asax**
```csharp
protected void Application_Start()
{
    var config = TelemetryConfiguration.CreateDefault();
    config.ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://...";
}
```

**Via applicationinsights.config**
```xml
<?xml version="1.0" encoding="utf-8"?>
<ApplicationInsights xmlns="http://schemas.microsoft.com/ApplicationInsights/2013/Settings">
  <ConnectionString>InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://...</ConnectionString>
</ApplicationInsights>
```

**Via environment variable**

Set the `APPLICATIONINSIGHTS_CONNECTION_STRING` environment variable. The Web package will automatically read it from configuration.

#### NlogTarget
Choose one of the options below to set the connection string.

**Via code**
```csharp
var config = new LoggingConfiguration();
var aiTarget = new ApplicationInsightsTarget
{
    ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://..."
};
config.AddTarget("ai", aiTarget);
config.AddRuleForAllLevels(aiTarget);
LogManager.Configuration = config;
```

**Via NLog.config**
```xml
<nlog>
  <extensions>
    <add assembly="Microsoft.ApplicationInsights.NLogTarget" />
  </extensions>
  <targets>
    <target type="ApplicationInsightsTarget" name="ai"
            connectionString="InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://..." />
  </targets>
  <rules>
    <logger name="*" minlevel="Trace" writeTo="ai" />
  </rules>
</nlog>
```
### Create TelemetryClient using TelemetryConfiguration
Some TelemetryClient constructors have been removed. The recommended way to create a telemetry client is below:

```csharp
var config = TelemetryConfiguration.CreateDefault();
config.ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://...";
var client = new TelemetryClient(config);
```

### Mutable Dictionary Requirement Resolved for Track* Methods
In earlier 3.x pre-releases, several `Track*` methods mutated the `IDictionary<string, string> properties` parameter or the `Properties` dictionary on telemetry objects, which broke immutable dictionary implementations (e.g., F#'s `dict`/`Map`, `ReadOnlyDictionary`). This has been fixed — all `Track*` methods now create an internal copy before adding internal attributes. Immutable dictionaries are fully supported. No migration action is needed.

## Alternatives for Removed TelemetryConfiguration properties
The [breaking changes](BreakingChanges.md#properties) defined which properties were removed. For some properties, further guidance is described below:

### TelemetryInitializers
The purpose of this property in 2.x was to add a list of initializers that would enrich every telemetry item with more attributes. In 3.x, ITelemetryInitializer and all of the built in initializers were removed. Some initializers were internally replaced with OpenTelemetry constructs internally, while for others one may need to add global/custom properties, or create their own OpenTelemetry Processor to do the same. In addition, the way to register custom OpenTelemetry Processors is different based on the package in use. Below we will describe which initializers used to exist, their intended replacement, and how one could create their own OpenTelemetry processor and register it with the SDK.

#### Prior Art and Replacement
These were the built-in telemetry initializers that used to exist in 2.x and their intended replacements:

##### Base SDK
- `OperationCorrelationTelemetryInitializer`: No replacement needed. This is internally replaced by OpenTelemetry's telemetry correlation mechanism.
- `SequencePropertyInitializer`: This initializer was never relevant for external use; removed with no replacement as the sequencing construct does not apply to OpenTelemetry based data.
- `W3COperationCorrelationTelemetryInitializer`: This was previously marked obsolete, now completely removed.

##### Microsoft.ApplicationInsights.AspNetCore
- `AspNetCoreEnvironmentTelemetryInitializer`: This is internally replaced by a resource detector.
- `AzureAppServiceRoleNameFromHostNameHeaderInitializer`: In 3.x, the resource detection is not header based. It uses a resource detector that gets called internally to set the cloud role name.
- `ClientIpHeaderTelemetryInitializer`: See the section on [TelemetryContext](#telemetry-context).
- `ComponentVersionTelemetryInitializer` (shared with WorkerService): This is internally replaced by a call to Assembly.GetEntryAssembly().GetName().Version by default, or the ApplicationVersion property if set.
- `DomainNameRoleInstanceTelemetryInitializer` (shared with WorkerService): For app services, this is internally replaced by a resource detector. For other environments, one would need to append the `service.instance.id` resource attribute via [this](NETCORE/Readme.md#adding-custom-resource).
- `OperationNameTelemetryInitializer`: This is replaced internally via automatically called OpenTelemetry instrumentation.
- `SyntheticTelemetryInitializer`: For a future milestone we are planning to implement support to enable similar functionality. Until then, consider adding a custom dimension via [alternative](#familiar-api-alternative). 
- `WebSessionTelemetryInitializer`: There is not a cookie based replacement for this, however one could consider the [alternative](#familiar-api-alternative).
- `WebUserTelemetryInitializer`: See the section on [TelemetryContext](#telemetry-context).

Note that for any initializer that is replaced by a resource detector, or sets resource attributes, that attribute will appear on an `_APPRESOURCEPREVIEW_` metric instead of every telemetry item. To append attributes to all telemetry, consider the [alternative](#familiar-api-alternative) or creating a [custom OpenTelemetry processor](#creating-a-custom-opentelemetry-processor).

##### Microsoft.ApplicationInsights.Web
These are replaced by internal activity processors:
- `AccountIdTelemetryInitializer`
- `AuthenticatedUserIdTelemetryInitializer`
- `ClientIpHeaderTelemetryInitializer`
- `SessionTelemetryInitializer`
- `SyntheticUserAgentTelemetryInitializer`
- `UserTelemetryInitializer`
- `WebTestTelemetryInitializer`

The rest below are replaced by internally used OpenTelemetry constructs (instrumentations & resource detectors).
- `OperationNameTelemetryInitializer`
- `OperationCorrelationTelemetryInitializer`
- `AzureAppServiceRoleNameFromHostNameHeaderInitializer`

Customers do not explicitely need to find replacements for any of the above as it is automatically handled.

##### Microsoft.ApplicationInsights.DependencyCollector
Please see the section on [unsupported packages](#previously-supported-packages-that-are-now-unsupported)

##### Microsoft.ApplicationInsights.WindowsServer
Please see the section on [WindowsServer replacement](#microsoftapplicationinsightswindowsserver-replacement)

#### Familiar API Alternative
As mentioned previously, resource attributes would flow to an `_APPRESOURCEPREVIEW_` metric instead of being appended to every telemetry item. To append an attribute to every telemetry item, one could set a global property on the TelemetryClient context:

```csharp
var client = new TelemetryClient(config);
client.Context.GlobalProperties["MyCustomKey"] = "MyCustomValue";
```

Any key-value pairs added to `GlobalProperties` will automatically appear in `customDimensions` on every telemetry item sent by that client.

#### Creating a custom OpenTelemetry Processor
Previously, ITelemetryInitializer and the base classes provided an extensibility point for customers to configure more complex telemetry enrichment. While this is removed in 3.x, the intended replacement is OpenTelemetry Processors. See the following examples to learn how to create and register opentelemetry processors:
- [Activity Processor (filtering)](docs/concepts.md#filtering-telemetry-with-activity-processors)
- [Activity Processor (enrichment)](docs/concepts.md#enriching-telemetry-with-activity-processors)
- [LogRecord Processor (enrichment)](docs/concepts.md#log-processors)

### TelemetryProcessors
In 2.x, this property represent a collection of telemetry processors to apply to the configuration. The built in ones in 2.x are listed below:
- `SamplingTelemetryProcessor`: Please refer to the [sampling section](#sampling) to understand the replacement.
- `AdaptiveSamplingTelemetryProcessor`: Please refer to the [sampling section](#sampling) to understand the replacement.
- `QuickPulseTelemetryProcessor`: Please refer to the [quickpulse section](#quickpulse-configuration) to understand the replacement.

In 3.x, all of these and the extensible interface are removed. To add custom processing to telemetry items, one should use OpenTelmetry Processors. Please refer to the [preceding section](#creating-a-custom-opentelemetry-processor) to learn how to create and register a custom processor.
  
### TelemetryProcessorChainBuilder
OpenTelemetry Processors are meant to replace custom TelemetryProcessors. Multiple processors can be registered at once: 

##### Microsoft.ApplicationInsights (Base SDK) & Microsoft.ApplicationInsights.Web processor registration
```csharp
var configuration = TelemetryConfiguration.CreateDefault();
configuration.ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://...";

configuration.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.WithTracing(tracing =>
    {
        tracing
            .AddProcessor<CustomEnrichmentProcessor>()
            .AddProcessor<CustomFilteringProcessor>();
    });
});
```

##### Microsoft.ApplicationInsights.AspNetCore & Microsoft.ApplicationInsights.WorkerService processor registration
```csharp
builder.Services.ConfigureOpenTelemetryTracerProvider((sp, tracerBuilder) =>
{
    tracerBuilder
        .AddProcessor<CustomEnrichmentProcessor>()
        .AddProcessor<CustomFilteringProcessor>();
});
```
Processors execute in the order they are registered.

### ApplicationIdProvider
In 3.x, this functionality is automatically handled by the Azure Monitor Exporter and its dependence on OpenTelemetry's correlation mechanism. Customers do not need an explicit replacement.

### TelemetrySinks
In 2.x, `TelemetrySinks` was a collection of `TelemetrySink` objects on `TelemetryConfiguration`. Each sink had its own telemetry processor chain and telemetry channel, enabling a fan-out pattern where telemetry could be sent to multiple destinations with different processing pipelines.

In 3.x, this property is removed. The Azure Monitor Exporter is used internally to send telemetry to Application Insights. To send telemetry to additional backends, use OpenTelemetry exporters. For example, to also write telemetry to the console, install the `OpenTelemetry.Exporter.Console` NuGet package, and configure as follows:

##### Microsoft.ApplicationInsights (Base SDK) & Microsoft.ApplicationInsights.Web
```csharp
var configuration = TelemetryConfiguration.CreateDefault();
configuration.ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://...";

configuration.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.WithTracing(tracing => tracing.AddConsoleExporter());
    builder.WithLogging(logging => logging.AddConsoleExporter());
    builder.WithMetrics(metrics => metrics.AddConsoleExporter());
});
```

##### Microsoft.ApplicationInsights.AspNetCore & Microsoft.ApplicationInsights.WorkerService
```csharp
builder.Services.ConfigureOpenTelemetryTracerProvider((sp, tracerBuilder) =>
{
    tracerBuilder.AddConsoleExporter();
});

builder.Services.ConfigureOpenTelemetryMeterProvider((sp, meterBuilder) =>
{
    meterBuilder.AddConsoleExporter();
});

builder.Services.ConfigureOpenTelemetryLoggerProvider((sp, loggerBuilder) =>
{
    loggerBuilder.AddConsoleExporter();
});
```

### DefaultTelemetrySink
In 3.x, the collection of sinks is removed, so this is removed also. The internally used Azure Monitor Exporter is meant to act as a default sink.

### TelemetryChannels
In 2.x, telemetry channels were the transport layer responsible for buffering and sending telemetry to Application Insights. The `ITelemetryChannel` interface defined the contract, and there were two built-in implementations:

- **`InMemoryChannel`** (from `Microsoft.ApplicationInsights`): A simple in-memory buffer with configurable `SendingInterval`, `MaxTelemetryBufferCapacity`, and `BacklogSize`. No disk persistence — data was lost on crash. This was the default channel.
- **`ServerTelemetryChannel`** (from `Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel`): A production-grade channel with local disk persistence, retry logic with exponential back-off, and configurable storage. This was the recommended channel for server applications.

In 3.x, `ITelemetryChannel`, `InMemoryChannel`, `ServerTelemetryChannel`, and the `TelemetryConfiguration.TelemetryChannel` property are all removed. The Azure Monitor Exporter (used internally) is most analogous to the functionality of the ServerTelemetryChannel, as it maintains its own mechanism for batching and persistence of data. One could configure where data is located persisted during network issues via `StorageDirectory` property on `TelemetryConfiguration`, `ApplicationInsightsServiceOptions`, or in `applicationinsights.config`. One can also disable disk persistence entirely via the `DisableOfflineStorage` property on `TelemetryConfiguration`, `ApplicationInsightsServiceOptions`, or in `applicationinsights.config`. TelemetryClient.Flush() can be used to flush data.

There is not a direct replacement for the InMemoryChannel. However, OpenTelemetry has an InMemory exporter that can be used in unit testing scenarios - see example [here](BASE/Test/Microsoft.ApplicationInsights.Test/Microsoft.ApplicationInsights.Tests/TelemetryClientTest.cs). 

Customers who implemented custom `ITelemetryChannel` for sending telemetry to additional backends should use OpenTelemetry exporters instead. See the [TelemetrySinks](#telemetrysinks) section for a console exporter example.

## Telemetry Context
As mentioned in [breaking changes](BreakingChanges.md#telemetrycontext-breaking-changes), many context classes and properties have been marked as internal. For each of the removed classes, workarounds are listed:

### Cloud: RoleName, RoleInstance & Component Version
To set these values on all telemetry items, configure the OpenTelemetry service attributes `service.name` (maps to Cloud RoleName) and `service.instance.id` (maps to Cloud RoleInstance) and `service.version` (maps to application version). 

#### Microsoft.ApplicationInsights (Base SDK) & Microsoft.ApplicationInsights.Web
```csharp
var configuration = TelemetryConfiguration.CreateDefault();
configuration.ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://...";

configuration.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.ConfigureResource(r => r
        .AddService(
            serviceName: "MyRoleName",                   // Maps to Cloud.RoleName
            serviceInstanceId: Environment.MachineName,  // Maps to Cloud.RoleInstance
            serviceVersion: "1.0.0")                     // Maps to Component.Version
    );
});
```

#### Microsoft.ApplicationInsights.AspNetCore & Microsoft.ApplicationInsights.WorkerService
```csharp
builder.Services.ConfigureOpenTelemetryTracerProvider((sp, tracerBuilder) =>
{
    tracerBuilder.ConfigureResource(r => r
        .AddService(
            serviceName: "MyRoleName",                   // Maps to Cloud.RoleName
            serviceInstanceId: Environment.MachineName,  // Maps to Cloud.RoleInstance
            serviceVersion: "1.0.0")                     // Maps to Component.Version
    );
});

builder.Services.ConfigureOpenTelemetryLoggerProvider((sp, loggerBuilder) =>
{
    loggerBuilder.ConfigureResource(r => r
        .AddService(
            serviceName: "MyRoleName",                   // Maps to Cloud.RoleName
            serviceInstanceId: Environment.MachineName,  // Maps to Cloud.RoleInstance
            serviceVersion: "1.0.0")                     // Maps to Component.Version
    );
});
```
If one wishes to set service attributes only on specific telemetry items, that can be done via Track* overloads that take in custom properties, or via setting the property on the telemetry item before passing it to a Track* call.

> [!NOTE]
> There is a future work item to enable the setting of CloudContext and ComponentVersion properties without having to explicitly configure OpenTelemetry providers.

### Device and Session Context & User.AccountId
Currently, the Azure Monitor Exporter does not contain the mapping for these context attributes to appear in the correct part of the payload if a customer explicitly specifies it.
The current workaround is to add custom dimensions to your telemetry items instead. 
For individual items, this can be done via Track* overloads that take in custom properties, or it can be set on the individual telemetry item via `Properties` before being passed to Track* calls.
To add dimensions to all telemetry items, consider this [alternative](#familiar-api-alternative) or a [custom OpenTelemetry processor](#creating-a-custom-opentelemetry-processor).

> [!NOTE]
> There is a future work item to enable the setting of these context properties via their original Context attributes.

### Setting TelemetryContext via TelemetryClient
In 2.x it was possible to set TelemetryContext on the TelemetryClient, such that the context appeared on every telemetry item.
In 3.x, this functionality is not completely implemented - ie, only the `GlobalProperties` will propagate. We are working on a fix to ensure other properties will also propagate.

Intended syntax:

```csharp
var client = new TelemetryClient(config);

// GlobalProperties - this works today and will appear in customDimensions on all telemetry
client.Context.GlobalProperties["MyCustomGlobalProperty"] = "Production";

// The following are public and settable on individual telemetry items today.
// There is a known issue where these do not yet propagate when set on TelemetryClient.Context.
// Once fixed, setting them here will apply to all telemetry sent by this client:
client.Context.User.Id = "anonymous-user-id";
client.Context.User.AuthenticatedUserId = "authenticated-user-id";
client.Context.User.UserAgent = "MyApp/1.0";
client.Context.Operation.Name = "MyOperation";
client.Context.Location.Ip = "127.0.0.1";
```

## Changes to ApplicationInsightsServiceOptions
The [breaking changes](BreakingChanges.md#applicationinsightsserviceoptions-changes) documents define which `ApplicationInsightsServiceOptions` properties were removed for both AspNetCore and WorkerService. Below is further guidance for properties whose migration path was not explained in the breaking changes doc.

### TelemetryInitializers
In 2.x, `ApplicationInsightsServiceOptions.TelemetryInitializers` was a list that allowed adding custom `ITelemetryInitializer` instances via options. In 3.x, this property is removed. Please see the [TelemetryInitializers section](#telemetryinitializers) to determine if a specific initializer needs replacing, or for how to replace initializers with OpenTelemetry Processors or the [Familiar API Alternative](#familiar-api-alternative).

### RequestCollectionOptions
In 2.x, `ApplicationInsightsServiceOptions.RequestCollectionOptions` controlled request tracking behavior with three sub-properties:
- `InjectResponseHeaders` (default: `true`) — Controlled whether correlation response headers were injected.
- `TrackExceptions` (default: `false`) — Controlled whether unhandled exceptions were tracked as request telemetry.
- `EnableW3CDistributedTracing` (default: `true`) — Enabled W3C distributed tracing standard.

In 3.x, this property is removed entirely. W3C tracing is natively handled by OpenTelemetry, response header injection is managed internally, and exception tracking is covered by auto-collection. No customer action is needed.

### DependencyCollectionOptions
In 2.x, `ApplicationInsightsServiceOptions.DependencyCollectionOptions` had one sub-property:
- `EnableLegacyCorrelationHeadersInjection` (default: `false`) — Controlled whether legacy (pre-W3C) `Request-Id` and `x-ms-request-id` correlation headers were injected into outgoing dependency calls.

In 3.x, this property is removed from both AspNetCore and WorkerService packages. OpenTelemetry's built-in propagation handles all correlation natively using W3C TraceContext, so legacy header injection is no longer supported. No customer action is needed.

### EnableDebugLogger
In 3.x, self-diagnostics logs messages to a file instead. To enable see [self-diagnostics documentation](troubleshooting/Readme.md#self-diagnostics). It is not integrated with the Visual Studio output window.

## Sampling
In 2.x, sampling was handled by two telemetry processors that could be added to the pipeline:
- **`SamplingTelemetryProcessor`** — Fixed-rate sampling that sent a configured percentage of telemetry (e.g., 25%). Customers could also configure which telemetry types to include or exclude from sampling (e.g., sample requests at 25% but always send 100% of exceptions).
- **`AdaptiveSamplingTelemetryProcessor`** — Dynamically adjusted the sampling rate to stay within a target volume of telemetry items per second. Like fixed-rate sampling, it also supported per-telemetry-type inclusion/exclusion lists.

Both processors were registered via `TelemetryProcessorChainBuilder` (Base SDK / Web) or `EnableAdaptiveSampling` on `ApplicationInsightsServiceOptions` (AspNetCore / WorkerService).

### What changed in 3.x
In 3.x, `SamplingTelemetryProcessor`, `AdaptiveSamplingTelemetryProcessor`, and the `EnableAdaptiveSampling` option are all removed. They are replaced by two new properties available on `TelemetryConfiguration`, `ApplicationInsightsServiceOptions`, and in `applicationinsights.config`:

- **`TracesPerSecond`** (double?) — Rate-limited sampling that caps the number of OpenTelemetry traces sent per second. **This is the default sampling mode in 3.x.** When neither property is set, rate-limited sampling is active with a default value.
- **`SamplingRatio`** (float?) — Fixed-ratio sampling where the value represents the proportion of telemetry to send (0.0 to 1.0). A value of `1.0` means all telemetry is sent (no sampling). Setting this property disables rate-limited sampling.

Additionally, **`EnableTraceBasedLogsSampler`** (bool?, default: `true`) controls whether logs are sampled based on the sampling decision of their associated trace. When enabled, if a trace is sampled out, its associated logs are also sampled out.

> [!IMPORTANT]
> Per-telemetry-type sampling is no longer supported. In 2.x it was possible to configure different sampling ratios or inclusion/exclusion lists for specific telemetry types (e.g., always send all exceptions while sampling requests at 25%). In 3.x, the sampling decision applies uniformly to all trace telemetry.

### Configuration examples

#### Microsoft.ApplicationInsights (Base SDK) & Microsoft.ApplicationInsights.Web
```csharp
var configuration = TelemetryConfiguration.CreateDefault();
configuration.ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://...";

// Option 1: Rate-limited sampling (default behavior)
configuration.TracesPerSecond = 5; // Cap at 5 traces per second

// Option 2: Fixed-ratio sampling (overrides rate-limited)
configuration.SamplingRatio = 0.25f; // Send 25% of telemetry
```

For Web, these can also be set via `applicationinsights.config`:
```xml
<ApplicationInsights>
  <ConnectionString>InstrumentationKey=...;IngestionEndpoint=https://...</ConnectionString>

  <!-- Option 1: Rate-limited sampling -->
  <TracesPerSecond>5</TracesPerSecond>

  <!-- Option 2: Fixed-ratio sampling -->
  <SamplingRatio>0.25</SamplingRatio>
</ApplicationInsights>
```

#### Microsoft.ApplicationInsights.AspNetCore & Microsoft.ApplicationInsights.WorkerService

**Via code (AspNetCore)**
```csharp
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    // Option 1: Rate-limited sampling (default behavior)
    options.TracesPerSecond = 5;

    // Option 2: Fixed-ratio sampling
    options.SamplingRatio = 0.25f;
});
```

**Via code (WorkerService)**
```csharp
builder.Services.AddApplicationInsightsTelemetryWorkerService(options =>
{
    // Option 1: Rate-limited sampling (default behavior)
    options.TracesPerSecond = 5;

    // Option 2: Fixed-ratio sampling
    options.SamplingRatio = 0.25f;
});
```

**Via appsettings.json**
```json
{
  "ApplicationInsights": {
    "TracesPerSecond": 5,
    "SamplingRatio": 0.25
  }
}
```

## Appendix
### QuickPulse Configuration
This section describes configuration related to quickpulse:

##### Microsoft.ApplicationInsights (Base SDK)
  ```csharp
  var configuration = TelemetryConfiguration.CreateDefault();
  configuration.ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://...";
  configuration.EnableLiveMetrics = false; // disable Live Metrics
  ```

  ##### Microsoft.ApplicationInsights.AspNetCore & Microsoft.ApplicationInsights.WorkerService
  The `ApplicationInsightsServiceOptions.EnableQuickPulseMetricStream` property controls Live Metrics. It is a `bool` that defaults to `true` (enabled).

  **Via code (AspNetCore)**
  ```csharp
  builder.Services.AddApplicationInsightsTelemetry(options =>
  {
      options.EnableQuickPulseMetricStream = false; // disable Live Metrics
  });
  ```

  **Via code (WorkerService)**
  ```csharp
  builder.Services.AddApplicationInsightsTelemetryWorkerService(options =>
  {
      options.EnableQuickPulseMetricStream = false; // disable Live Metrics
  });
  ```

  **Via appsettings.json**
  ```json
  {
    "ApplicationInsights": {
      "EnableQuickPulseMetricStream": false
    }
  }
  ```

  ##### Microsoft.ApplicationInsights.Web
  The `EnableQuickPulseMetricStream` XML element controls Live Metrics. When absent, Live Metrics is enabled by default.

  **Via applicationinsights.config**
  ```xml
  <ApplicationInsights xmlns="http://schemas.microsoft.com/ApplicationInsights/2013/Settings">
    <ConnectionString>InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://...</ConnectionString>
    <EnableQuickPulseMetricStream>false</EnableQuickPulseMetricStream>
  </ApplicationInsights>
  ```