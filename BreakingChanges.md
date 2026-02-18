# Breaking Changes: Application Insights 2.x → 3.x

This document outlines the breaking changes when migrating from Application Insights SDK 2.x to 3.x (Shimmed version using OpenTelemetry).

> **IMPORTANT**: Mixing 2.x and 3.x packages is **not supported**. All Application Insights packages in your application must be upgraded to 3.x together.

## Architecture Overview

Version 3.x represents a fundamental architectural shift:
- **2.x**: Custom telemetry pipeline with processors, initializers, channels, and modules
- **3.x**: Built on OpenTelemetry with Azure Monitor Exporter as the backend

The 3.x version maintains most public APIs for backward compatibility but uses OpenTelemetry and Azure Monitor Exporter internally.

---

## Released Packages in 3.0.0-beta1

The following packages are part of the shimmed 3.x release:
1. **Microsoft.ApplicationInsights** (Core SDK)
2. **Microsoft.ApplicationInsights.AspNetCore** (ASP.NET Core integration)
3. **Microsoft.ApplicationInsights.WorkerService** (Worker Service integration)
4. **Microsoft.ApplicationInsights.NLogTarget** (NLog integration)
5. **Microsoft.ApplicationInsights.Web** (Classic ASP.NET integration)

---

# 1. Microsoft.ApplicationInsights (Core SDK)

## Connection String Breaking Change
3.x will throw an exception if a connection string is not provided. For test scenarios, one could supply a dummy string like: `InstrumentationKey=00000000-0000-0000-0000-000000000000`.

## TelemetryClient Breaking Changes

### Removed APIs

#### Constructor
- **Parameterless constructor removed**
  - **2.x**: `TelemetryClient()` - Used `TelemetryConfiguration.Active` (obsolete)
  - **3.x**: Removed - Must use `TelemetryClient(TelemetryConfiguration)`

#### Property
- **InstrumentationKey property setter removed**
  - **2.x**: `public string InstrumentationKey { get; set; }`
  - **3.x**: Property completely removed from public API
  - **Migration**: Use `TelemetryConfiguration.ConnectionString` instead

#### Methods
- **PageView tracking removed entirely**
  - `TrackPageView(string name)`
  - `TrackPageView(PageViewTelemetry telemetry)`
  - `TrackEvent` or `TrackRequest` can be used in lieu of TrackPageView in dotnet components.

### Methods with Changed Signatures

#### TrackEvent
- **2.x**: `TrackEvent(string eventName, IDictionary<string, string> properties, IDictionary<string, double> metrics)`
- **3.x**: `TrackEvent(string eventName, IDictionary<string, string> properties)` **Metrics parameter removed**

### Fixed: `TrackEvent` No Longer Mutates the `properties` Dictionary

In earlier 3.x pre-releases, `TrackEvent(string eventName, IDictionary<string, string> properties)` mutated the caller's `properties` dictionary by calling `properties.Add("microsoft.custom_event.name", eventName)`. This caused `System.NotSupportedException` when passing immutable `IDictionary` implementations (such as F#'s `dict`/`Map`, `ReadOnlyDictionary<TKey, TValue>`, or `ImmutableDictionary<TKey, TValue>`), and could also cause `ArgumentException` on repeated calls with the same dictionary instance due to duplicate keys.

This has been fixed. All `Track*` methods (`TrackEvent`, `TrackTrace`, `TrackException`, etc.) now create an internal copy of the properties dictionary before adding internal attributes. The caller's dictionary is never modified. Immutable and read-only dictionary implementations are fully supported.

#### TrackAvailability
- **2.x**: `TrackAvailability(string name, DateTimeOffset timeStamp, TimeSpan duration, string runLocation, bool success, string message, IDictionary<string, string> properties, IDictionary<string, double> metrics)`
- **3.x**: `TrackAvailability(string name, DateTimeOffset timeStamp, TimeSpan duration, string runLocation, bool success, string message, IDictionary<string, string> properties)` **Metrics parameter removed**

#### TrackException
- **2.x**: `TrackException(Exception exception, IDictionary<string, string> properties, IDictionary<string, double> metrics)`
- **3.x**: `TrackException(Exception exception, IDictionary<string, string> properties)` **Metrics parameter removed**

#### TrackDependency (Obsolete Overload)
- **2.x**: `TrackDependency(string dependencyName, string data, DateTimeOffset startTime, TimeSpan duration, bool success)` [Obsolete]
- **3.x**: **Overload removed entirely**

Recommendation for metrics: Track metrics separately via `TrackMetric()`.

#### GetMetric Overloads
All `GetMetric` overloads have been simplified:
- **`MetricConfiguration` parameter removed from all overloads**
- **`MetricAggregationScope` parameter removed from all overloads**
Metrics configuration and aggregation is now internally managed by OpenTelemetry meters.

**Examples:**
- **2.x**: `GetMetric(MetricIdentifier metricIdentifier, MetricConfiguration metricConfiguration, MetricAggregationScope aggregationScope)`
- **3.x**: `GetMetric(MetricIdentifier metricIdentifier)`

- **2.x**: `GetMetric(string metricId, string dimension1Name, MetricConfiguration metricConfiguration, MetricAggregationScope aggregationScope)`
- **3.x**: `GetMetric(string metricId, string dimension1Name)`

This applies to all dimension combinations (1D, 2D, 3D, 4D).

#### Methods with changed internal implementation
- `Track(ITelemetry telemetry)` - Still exists with `[EditorBrowsable(EditorBrowsableState.Never)]` attribute. In 3.x, it routes to specific Track methods (TrackRequest, TrackDependency, etc.) instead of using the telemetry processor pipeline.
- `StartOperation<T>` and `StopOperation<T>` - Extension methods in `TelemetryClientExtensions` now use OpenTelemetry Activities instead of the legacy correlation system.

---

## TelemetryConfiguration Breaking Changes

### Removed APIs

#### Properties
- **`TelemetryConfiguration.Active`**: Initialize via TelemetryConfiguration.CreateDefault() instead
- **`InstrumentationKey`** - Replaced by `ConnectionString`
- **`TelemetryInitializers`** - See detailed migration information [here](MigrationGuidance.md#telemetryinitializers)
- **`TelemetryProcessors`** - See detailed migration information [here](MigrationGuidance.md#telemetryprocessors)
- **`TelemetryProcessorChainBuilder`** - See detailed migration information [here](MigrationGuidance.md#telemetryprocessorchainbuilder)
- **`TelemetryChannel`** - See detailed migration information [here](MigrationGuidance.md#telemetrychannels)
- **`ApplicationIdProvider`** - See detailed migration information [here](MigrationGuidance.md#applicationidprovider).
- **`EndpointContainer`** - In 3.x, the connection string is parsed to determine where to send telemetry.
- **`ExperimentalFeatures`** - No longer needed
- **`TelemetrySinks`** - See detailed migration information [here](MigrationGuidance.md#telemetrysinks)
- **`DefaultTelemetrySink`** - See detailed migration information [here](MigrationGuidance.md#defaulttelemetrysink)

#### Constructors
- `TelemetryConfiguration(string instrumentationKey)`
- `TelemetryConfiguration(string instrumentationKey, ITelemetryChannel channel)`
Migration note: TelemetryConfiguration.CreateDefault is the recommended way to initialize.

#### Methods
- **`CreateFromConfiguration(string config)`** - Static method that created a TelemetryConfiguration from XML configuration string. Use `CreateDefault()` and set properties directly.

### Methods with changed Behavior
- CreateDefault() returns an internal static configuration instead of a new TelemetryConfiguration()

### New APIs Added in 3.x
- **`SamplingRatio`** (float?) - Gets or sets the sampling ratio for traces (0.0 to 1.0). A value of 1.0 means all telemetry is sent.
- **`TracesPerSecond`** (double?) - Gets or sets the number of traces per second for rate-limited sampling (default sampling mode).
- **`StorageDirectory`** (string) - Gets or sets the directory for offline telemetry storage.
- **`DisableOfflineStorage`** (bool?) - Gets or sets whether offline storage is disabled.
- **`EnableLiveMetrics`** (bool?) - Gets or sets whether Live Metrics is enabled.
- **`EnableTraceBasedLogsSampler`** (bool?) - Gets or sets whether trace-based log sampling is enabled.
- **`ConfigureOpenTelemetryBuilder(Action<IOpenTelemetryBuilder> configure)`** - Allows extending OpenTelemetry configuration
- **`SetAzureTokenCredential(TokenCredential tokenCredential)`** - Call this method to enable Azure Active Directory (AAD) authentication for Application Insights ingestion

---

## TelemetryContext Breaking Changes

Several TelemetryContext sub-context classes have been made internal, and some properties on the remaining public sub-contexts have also been made internal.

### TelemetryContext Properties Removed
- **`InstrumentationKey`** - Removed. Use `TelemetryConfiguration.ConnectionString` instead.
- **`Flags`** - Removed from public API as there isn't equivalent configuration in the underlying exporter.
- **`Properties`** (obsolete) - Was obsoleted in 2.x in favor of `GlobalProperties`.
- **`TryGetRawObject()`** / **`StoreRawObject()`** - Removed. These methods were used to pass raw objects between collectors and initializers, which no longer exist.

### Sub-Context Classes Made Internal
The following sub-context classes were **public** in 2.x and are now **internal** in 3.x. Their properties are no longer accessible:
- **`Cloud`** (`CloudContext`) — Had `RoleName`, `RoleInstance`. 
- **`Component`** (`ComponentContext`) — Had `Version`. 
- **`Device`** (`DeviceContext`) — Had `Type`, `Id`, `OperatingSystem`, `OemName`, `Model`. 
- **`Session`** (`SessionContext`) — Had `Id`, `IsFirst`. 

See detailed migration guidance [here](MigrationGuidance.md#telemetry-context)

### Sub-Context Properties Made Internal
The following properties on **still-public** sub-context classes have been made internal:
- **`User.AccountId`** — Was public in 2.x, now internal. This can be set via adding properties to Track() calls or creating custom OpenTelemetry processors.
- **`Operation.Id`** — Was public in 2.x, now internal. Correlation IDs are managed automatically by OpenTelemetry.
- **`Operation.ParentId`** — Was public in 2.x, now internal. Correlation IDs are managed automatically by OpenTelemetry.
- **`Operation.CorrelationVector`** — No longer needed due to shift to OpenTelemetry correlation.
- **`Operation.SyntheticSource`** — There is future work planned to reset this to public.

### Properties Retained
The following remain **public**:
- `User` (`Id`, `AuthenticatedUserId`, `UserAgent`)
- `Operation` (`Name`)
- `Location` (`Ip`)
- `GlobalProperties`

Note that these properties are currently settable on individual telemetry items; there is future work planned to make these settable via TelemetryClient.
---

# 2. Microsoft.ApplicationInsights.AspNetCore

## Extension Methods Removed

### From IServiceCollection
- **`AddApplicationInsightsTelemetry(string instrumentationKey)`** - Obsolete overload removed
- **`AddApplicationInsightsKubernetesEnricher()`** - Kubernetes enrichment removed; there is a resource detector for enriching telemetry that is implemented internally.

### From IApplicationBuilder
- **`UseApplicationInsightsRequestTelemetry()`** - Obsolete middleware removed
- **`UseApplicationInsightsExceptionTelemetry()`** - Obsolete middleware removed

### From IWebHostBuilder
- **`UseApplicationInsights()`** - All overloads removed (were obsolete in 2.x)

### From Shared Extensions
- **`AddApplicationInsightsTelemetryProcessor<T>()`** - See more detailed migration guidance [here](MigrationGuidance.md#microsoftapplicationinsightsaspnetcore--microsoftapplicationinsightsworkerservice-processor-registration)
- **`AddApplicationInsightsTelemetryProcessor(Type telemetryProcessorType)`** - See more detailed migration guidance [here](MigrationGuidance.md#microsoftapplicationinsightsaspnetcore--microsoftapplicationinsightsworkerservice-processor-registration)
- **`ConfigureTelemetryModule<T>(Action<T> configModule)`** - Module configuration without options (was already obsolete in 2.x)
- **`ConfigureTelemetryModule<T>(Action<T, ApplicationInsightsServiceOptions> configModule)`** - Use one of the retained methods or set options via appsettings.json.
- **`AddApplicationInsightsSettings(bool? developerMode, string endpointAddress, string instrumentationKey)`** - Was already obsolete in 2.x
- **`AddApplicationInsightsSettings(string connectionString, bool? developerMode, string endpointAddress, string instrumentationKey)`** - Use one of the retained methods or set options via appsettings.json. The connection string specifies endpoints and batching of telemetry is internally managed by the underlying exporter. However, TelemetryClient.Flush() can still be used.

## Extension Methods Retained
The following extension methods remain with identical signatures:
- `AddApplicationInsightsTelemetry()`
- `AddApplicationInsightsTelemetry(IConfiguration configuration)`
- `AddApplicationInsightsTelemetry(Action<ApplicationInsightsServiceOptions> configureOptions)`
- `AddApplicationInsightsTelemetry(ApplicationInsightsServiceOptions options)`

## Classes/Components Removed

### Entire Classes
- **`ExceptionTrackingMiddleware`** - Middleware class removed, was already obsolete
- **`HostingDiagnosticListener`** - this was previously internal
- **`Resources`** - Previously public APIs now set to internal. These were autogenerated APIs not directly used by customers in 2.x.
  - `Resources.Culture` (get/set)
  - `Resources.JavaScriptAuthSnippet` (get)
  - `Resources.JavaScriptSnippet` (get)
  - `Resources.ResourceManager` (get)

### TelemetryInitializers Removed (All 7)
- `AspNetCoreEnvironmentTelemetryInitializer`
- `AzureAppServiceRoleNameFromHostNameHeaderInitializer`
- `ClientIpHeaderTelemetryInitializer`
- `DomainNameRoleInstanceTelemetryInitializer`
- `HttpDependenciesParsingTelemetryInitializer`
- `OperationNameTelemetryInitializer`
- `WebSessionTelemetryInitializer`
- `TelemetryInitializerBase`

Please see detailed migration guidance [here](MigrationGuidance.md#microsoftapplicationinsightsaspnetcore)

### Telemetry Modules Removed
The following `ITelemetryModule` implementations were previously registered via `AddApplicationInsightsTelemetry()` in 2.x. In 3.x, their functionality is either handled internally or removed:
- **`RequestTrackingTelemetryModule`** — Request tracking is now handled internally via OpenTelemetry ASP.NET Core instrumentation. Configurable via `EnableRequestTrackingTelemetryModule`.
- **`DiagnosticsTelemetryModule`** — Removed. Self-diagnostics and heartbeat are internally implemented in 3.x.
- **`AppServicesHeartbeatTelemetryModule`** — Removed. 3.x internally uses a resource detector to emit resource attributes.
- **`AzureInstanceMetadataTelemetryModule`** — Removed. 3.x internally uses a resource detector to emit resource attributes.
- **`PerformanceCollectorModule`** — Performance counter collection is now internally managed. Configurable via `EnablePerformanceCounterCollectionModule`.
- **`QuickPulseTelemetryModule`** — Live Metrics is now internally managed. Configurable via `EnableQuickPulseMetricStream`.
- **`DependencyTrackingTelemetryModule`** — Dependency tracking is now handled internally via OpenTelemetry instrumentation. Configurable via `EnableDependencyTrackingTelemetryModule`.
- **`EventCounterCollectionModule`** — Discontinued. There is no direct replacement.

The `ConfigureTelemetryModule<T>()` extension method has also been removed. Use `ApplicationInsightsServiceOptions` properties or `appsettings.json` to configure the retained toggles listed above.

## ApplicationInsightsServiceOptions Changes

### Properties Removed
- **`InstrumentationKey`** - Obsolete property removed (use `ConnectionString`)
- **`DeveloperMode`** - Batching of telemetry is now internally managed by the underlying exporter, with no equivalent toggle. However, TelemetryClient.Flush() can still be used in testing scenarios to flush telemetry.
- **`EndpointAddress`** - No longer needed (`ConnectionString` contains endpoint information)
- **`TelemetryInitializers`** - See detailed migration guidance [here](MigrationGuidance.md#telemetryinitializers-1)
- **`EnableHeartbeat`** - Heartbeat configuration removed as the 3.x internally maintains its own heartbeating mechanism.
- **`RequestCollectionOptions`** - See detailed migration guidance [here](MigrationGuidance.md#requestcollectionoptions)
- **`DependencyCollectionOptions`** - See detailed migration guidance [here](MigrationGuidance.md#dependencycollectionoptions)
- **`EnableAdaptiveSampling`** - Removed in favor of our internal rate limited sampler, which is now the default sampling behavior.
- **`EnableDebugLogger`** - Removed as internal self diagnostics accomplishes the same goal. Instructions to enable [here](MigrationGuidance.md#enabledebuglogger).

### Properties Retained
- **`ConnectionString`** - Primary configuration method
- **`ApplicationVersion`**
- **`AddAutoCollectedMetricExtractor`**
- **`EnableQuickPulseMetricStream`** 
- **`EnableAuthenticationTrackingJavaScript`** - JavaScript auth tracking config
- **`EnableDependencyTrackingTelemetryModule`** - Dependency tracking toggle
- **`EnablePerformanceCounterCollectionModule`** - Performance counter toggle
- **`EnableRequestTrackingTelemetryModule`** - Request tracking toggle

### New Properties Added in 3.x
- **`Credential`** (Azure.Core.TokenCredential) - Enables Azure Active Directory (AAD) authentication
- **`TracesPerSecond`** (double?) - Gets or sets the number of traces per second for rate-limited sampling (default sampling mode). Replaces `EnableAdaptiveSampling`.
- **`SamplingRatio`** (float?) - Gets or sets the sampling ratio for traces (0.0 to 1.0). A value of 1.0 means all telemetry is sent. 
- **`EnableTraceBasedLogsSampler`** (bool?) - Gets or sets whether trace-based log sampling is enabled (default: true). When enabled, logs are sampled based on the sampling decision of the associated trace.

Please see our [migration guide](MigrationGuidance.md#sampling) for detailed guidance on sampling.

### JavaScriptSnippet Constructor Change
**2.x:**
```csharp
public JavaScriptSnippet(
    TelemetryConfiguration telemetryConfiguration,
    IOptions<ApplicationInsightsServiceOptions> serviceOptions,
    IHttpContextAccessor httpContextAccessor,
    JavaScriptEncoder encoder = null)
```

**3.x:**
```csharp
public JavaScriptSnippet(
    TelemetryConfiguration telemetryConfiguration,
    IOptions<ApplicationInsightsServiceOptions> serviceOptions,
    IHttpContextAccessor httpContextAccessor = null,  // Now optional
    JavaScriptEncoder encoder = null)
```

---

# 3. Microsoft.ApplicationInsights.WorkerService

## Extension Methods Removed
- **`AddApplicationInsightsTelemetryWorkerService(string instrumentationKey)`** - Instrumentation key overload removed (was obsolete in 2.x)

## Extension Methods Retained
The following extension methods remain with identical signatures:
- `AddApplicationInsightsTelemetryWorkerService()`
- `AddApplicationInsightsTelemetryWorkerService(IConfiguration configuration)`
- `AddApplicationInsightsTelemetryWorkerService(Action<ApplicationInsightsServiceOptions> configureOptions)`
- `AddApplicationInsightsTelemetryWorkerService(ApplicationInsightsServiceOptions options)`

## Telemetry Modules Removed
The following `ITelemetryModule` implementations were previously registered via `AddApplicationInsightsTelemetryWorkerService()` in 2.x. In 3.x, their functionality is either handled internally or removed:
- **`DiagnosticsTelemetryModule`** — Removed. Self-diagnostics and heartbeat are internally implemented in 3.x.
- **`AppServicesHeartbeatTelemetryModule`** — Removed. 3.x internally uses a resource detector to emit resource attributes.
- **`AzureInstanceMetadataTelemetryModule`** — Removed. 3.x internally uses a resource detector to emit resource attributes.
- **`PerformanceCollectorModule`** — Performance counter collection is now internally managed. Configurable via `EnablePerformanceCounterCollectionModule`.
- **`QuickPulseTelemetryModule`** — Live Metrics is now internally managed. Configurable via `EnableQuickPulseMetricStream`.
- **`DependencyTrackingTelemetryModule`** — Dependency tracking is now handled internally via OpenTelemetry instrumentation. Configurable via `EnableDependencyTrackingTelemetryModule`.
- **`EventCounterCollectionModule`** — Discontinued. There is no direct replacement.

The `ConfigureTelemetryModule<T>()` extension method has also been removed. Use `ApplicationInsightsServiceOptions` properties or `appsettings.json` to configure the retained toggles listed above.

## ApplicationInsightsServiceOptions Changes

### Properties Removed
- **`InstrumentationKey`** - Obsolete property removed (use `ConnectionString`)
- **`EnableEventCounterCollectionModule`** - EventCounter module configuration removed. There is no intended replacement.
- **`EnableAppServicesHeartbeatTelemetryModule`** - 3.x internally calls a resource detector that emits resource attributes to an internal resource metric.
- **`EnableAzureInstanceMetadataTelemetryModule`** - 3.x internally calls a resource detector that emits resource attributes to an internal resource metric.
- **`EnableHeartbeat`** - 3.x internally emits a heartbeat metric by default.
- **`EnableDiagnosticsTelemetryModule`** - Removed in favor of self diagnostics and internally implemented heartbeat.
- **`DeveloperMode`** - Batching of telemetry is now internally managed by the underlying exporter, with no equivalent toggle. However, TelemetryClient.Flush() can still be used in testing scenarios to flush telemetry.
- **`EndpointAddress`** - No longer configurable (`ConnectionString` contains endpoints)
- **`DependencyCollectionOptions`** - See detailed migration guidance [here](MigrationGuidance.md#dependencycollectionoptions)
- **`EnableAdaptiveSampling`** - Removed in favor of our internal rate limited sampler, which is now the default sampling mechanism.
- **`EnableDebugLogger`** - Removed in favor of self diagnostics. Learn how to enable [here](MigrationGuidance.md#enabledebuglogger).

### Properties Retained
- **`ConnectionString`** 
- **`ApplicationVersion`**
- **`EnableDependencyTrackingTelemetryModule`** - Enabled by default
- **`EnablePerformanceCounterCollectionModule`** - Enabled by default
- **`EnableQuickPulseMetricStream`** - Live metrics collection is enabled by default
- **`AddAutoCollectedMetricExtractor`** - Standard metrics collection is enabled by default.

### New Properties Added in 3.x
- **`Credential`** (Azure.Core.TokenCredential) - Enables Azure Active Directory (AAD) authentication
- **`TracesPerSecond`** (double?) - Gets or sets the number of traces per second for rate-limited sampling (default sampling mode). Replaces `EnableAdaptiveSampling`.
- **`SamplingRatio`** (float?) - Gets or sets the sampling ratio for traces (0.0 to 1.0). A value of 1.0 means all telemetry is sent.
- **`EnableTraceBasedLogsSampler`** (bool?) - Gets or sets whether trace-based log sampling is enabled (default: true). When enabled, logs are sampled based on the sampling decision of the associated trace.

Please see our [migration guide](MigrationGuidance.md#sampling) for detailed guidance on sampling.

---

# 4. Microsoft.ApplicationInsights.NLogTarget

## API Changes

### Property Removed
- **`InstrumentationKey` property** (string)
  - **2.x**: `public string InstrumentationKey { get; set; }`
  - **3.x**: Completely removed
  - **Migration**: Use `ConnectionString` property instead

### New Property Added in 3.x
- **`Credential`** (Azure.Core.TokenCredential) - Enables Azure Active Directory (AAD) authentication for Application Insights

### Configuration Breaking Changes
**Migration Required:** Users MUST update their NLog configuration:

**2.x Configuration:**
```xml
<target type="ApplicationInsightsTarget" InstrumentationKey="xxx" />
```

**3.x Configuration:**
```xml
<target type="ApplicationInsightsTarget" ConnectionString="InstrumentationKey=xxx;IngestionEndpoint=https://..." />
```

### Target Framework Changes
- **2.x**: `net452`, `netstandard2.0`
- **3.x**: `net462`, `netstandard2.0`
- **Breaking**: Minimum .NET Framework version raised from **4.5.2** → **4.6.2**

## Migration Checklist

**Critical Actions Required:**
1. Replace `InstrumentationKey` with `ConnectionString` in NLog configuration
2. Ensure connection string is always provided (no longer optional)
3. Update minimum .NET Framework from 4.5.2 to 4.6.2 if using .NET Framework

---

# 5. Microsoft.ApplicationInsights.Web (Classic ASP.NET)

## Telemetry Modules REMOVED
The following modules have been removed:
- `RequestTrackingTelemetryModule` - This is replaced by an internal ActivityFilterProcessor and is configurable via `<EnabledRequestTrackingTelemetryModule>`
- `ExceptionTrackingTelemetryModule` - Exception tracking is automatically included via OpenTelemetry instrumentation. No action needed from customer side.
- `AspNetDiagnosticTelemetryModule` - OpenTelemtry's ASPNET instrumentation is automatically enabled in 3.x

Note that the applicationinsights.config configuration for <TelemetryModule> and its sub-settings have been removed in favor of alternatives above.

## Telemetry Initializers REMOVED

All public **TelemetryInitializers** from 2.x are **REMOVED from the public API** in 3.x. Please refer to the [migration guidance](MigrationGuidance.md#telemetryinitializers) for the list previous initalizers and their replacements.

## Base Classes REMOVED
- `WebTelemetryInitializerBase` 
- `WebTelemetryModuleBase` 
OpenTelemetry Processors are meant to provide extensibility. See more detailed guidance in [migration documentation](MigrationGuidance.md#creating-a-custom-opentelemetry-processor).

## Extension Methods Changes
- **`HttpContextExtension` class removed from public API**: The intended replacement is for customers to register a custom activity processor via TelemetryConfiguration.ConfigureOpenTelemetryBuilder().
- **`HttpContextBaseExtension`**: was previously marked obsolete, now completely removed.

See [migration guidance](MigrationGuidance.md#creating-a-custom-opentelemetry-processor) for how to create a custom activity processor and register it.

## Configuration Model Changes

### 2.x Configuration (applicationinsights.config):
```xml
<ApplicationInsights>
  <InstrumentationKey>your-key-here</InstrumentationKey>
  
  <TelemetryInitializers>
    <Add Type="Microsoft.ApplicationInsights.Web.WebTestTelemetryInitializer, Microsoft.AI.Web" />
    <Add Type="Microsoft.ApplicationInsights.Web.SyntheticUserAgentTelemetryInitializer, Microsoft.AI.Web">
      <Filters>search|spider|crawl|Bot|Monitor|AlwaysOn</Filters>
    </Add>
    <Add Type="Microsoft.ApplicationInsights.Web.ClientIpHeaderTelemetryInitializer, Microsoft.AI.Web" />
    <!-- ... 7 more initializers ... -->
  </TelemetryInitializers>
  
  <TelemetryModules>
    <Add Type="Microsoft.ApplicationInsights.Web.RequestTrackingTelemetryModule, Microsoft.AI.Web">
      <Handlers>...</Handlers>
    </Add>
    <Add Type="Microsoft.ApplicationInsights.Web.ExceptionTrackingTelemetryModule, Microsoft.AI.Web" />
    <Add Type="Microsoft.ApplicationInsights.Web.AspNetDiagnosticTelemetryModule, Microsoft.AI.Web" />
  </TelemetryModules>
</ApplicationInsights>
```

### 3.x Configuration (applicationinsights.config):
```xml
<ApplicationInsights>
  <ConnectionString>InstrumentationKey=your-key;IngestionEndpoint=https://...</ConnectionString>
  <DisableTelemetry>false</DisableTelemetry>
  <ApplicationVersion>1.0.0</ApplicationVersion>
  <SamplingRatio>1.0</SamplingRatio>
  <TracesPerSecond>5</TracesPerSecond>
  <StorageDirectory>C:\Logs</StorageDirectory>
  <DisableOfflineStorage>false</DisableOfflineStorage>
  <EnableQuickPulseMetricStream>true</EnableQuickPulseMetricStream>
  <EnableTraceBasedLogsSampler>false</EnableTraceBasedLogsSampler>
  <EnablePerformanceCounterCollectionModule>true</EnablePerformanceCounterCollectionModule>
  <AddAutoCollectedMetricExtractor>true</AddAutoCollectedMetricExtractor>
  <EnableDependencyTrackingTelemetryModule>true</EnableDependencyTrackingTelemetryModule>
  <EnableRequestTrackingTelemetryModule>true</EnableRequestTrackingTelemetryModule>
</ApplicationInsights>
```

**Summary of config changes:**
- `<TelemetryInitializers>` section no longer supported
- `<TelemetryModules>` section no longer supported
- `<InstrumentationKey>` replaced with `<ConnectionString>`
- Multiple new configuration elements supported (see example above)
- ASPNET & exception telemetry modules are now autoconfigured to use OpenTelemetry instrumentation.

## Target Framework Changes
- **2.x**: `net452` (Targets .NET Framework 4.5.2)
- **3.x**: `net462` (Targets .NET Framework 4.6.2)
- **Breaking**: Minimum framework version raised from **4.5.2** → **4.6.2**