# Breaking Changes: Application Insights 2.x → 3.x

This document outlines the breaking changes when migrating from Application Insights SDK 2.x to 3.x (Shimmed version using OpenTelemetry).

> **⚠️ IMPORTANT**: Mixing 2.x and 3.x packages is **not supported**. All Application Insights packages in your application must be upgraded to 3.x together.

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

## TelemetryClient Breaking Changes

### Removed APIs

#### Constructor
- ❌ **Parameterless constructor removed**
  - **2.x**: `TelemetryClient()` - Used `TelemetryConfiguration.Active` (obsolete)
  - **3.x**: Removed - Must use `TelemetryClient(TelemetryConfiguration)`

#### Property
- ❌ **InstrumentationKey property setter removed**
  - **2.x**: `public string InstrumentationKey { get; set; }`
  - **3.x**: Property completely removed from public API
  - **Migration**: Use `TelemetryConfiguration.ConnectionString` instead

#### Methods
- ❌ **PageView tracking removed entirely**
  - `TrackPageView(string name)`
  - `TrackPageView(PageViewTelemetry telemetry)`

**Note**: The following methods are **retained** in 3.x but with changed internal implementation:
- `Track(ITelemetry telemetry)` - Still exists with `[EditorBrowsable(EditorBrowsableState.Never)]` attribute. In 3.x, it routes to specific Track methods (TrackRequest, TrackDependency, etc.) instead of using the telemetry processor pipeline.
- `StartOperation<T>` and `StopOperation<T>` - Extension methods in `TelemetryClientExtensions` now use OpenTelemetry Activities instead of the legacy correlation system.

### Methods with Changed Signatures

#### TrackEvent
- **2.x**: `TrackEvent(string eventName, IDictionary<string, string> properties, IDictionary<string, double> metrics)`
- **3.x**: `TrackEvent(string eventName, IDictionary<string, string> properties)` ⚠️ **Metrics parameter removed**

#### TrackAvailability
- **2.x**: `TrackAvailability(string name, DateTimeOffset timeStamp, TimeSpan duration, string runLocation, bool success, string message, IDictionary<string, string> properties, IDictionary<string, double> metrics)`
- **3.x**: `TrackAvailability(string name, DateTimeOffset timeStamp, TimeSpan duration, string runLocation, bool success, string message, IDictionary<string, string> properties)` ⚠️ **Metrics parameter removed**

#### TrackException
- **2.x**: `TrackException(Exception exception, IDictionary<string, string> properties, IDictionary<string, double> metrics)`
- **3.x**: `TrackException(Exception exception, IDictionary<string, string> properties)` ⚠️ **Metrics parameter removed**

#### TrackDependency (Obsolete Overload)
- **2.x**: `TrackDependency(string dependencyName, string data, DateTimeOffset startTime, TimeSpan duration, bool success)` [Obsolete]
- **3.x**: ❌ **Overload removed entirely**

#### GetMetric Overloads
All `GetMetric` overloads have been simplified:
- ❌ **`MetricConfiguration` parameter removed from all overloads**
- ❌ **`MetricAggregationScope` parameter removed from all overloads**

**Examples:**
- **2.x**: `GetMetric(MetricIdentifier metricIdentifier, MetricConfiguration metricConfiguration, MetricAggregationScope aggregationScope)`
- **3.x**: `GetMetric(MetricIdentifier metricIdentifier)`

- **2.x**: `GetMetric(string metricId, string dimension1Name, MetricConfiguration metricConfiguration, MetricAggregationScope aggregationScope)`
- **3.x**: `GetMetric(string metricId, string dimension1Name)`

This applies to all dimension combinations (1D, 2D, 3D, 4D).

---

## TelemetryConfiguration Breaking Changes

### Removed APIs

#### Properties
- ❌ **`TelemetryConfiguration.Active`** (static property) - The singleton instance pattern removed
- ❌ **`InstrumentationKey`** - Replaced by `ConnectionString`
- ❌ **`TelemetryInitializers`** - Collection of telemetry initializers
- ❌ **`TelemetryProcessors`** - Readonly collection of processors
- ❌ **`TelemetryProcessorChainBuilder`** - Builder for processor chain
- ❌ **`TelemetryChannel`** - The telemetry channel for the default sink
- ❌ **`ApplicationIdProvider`** - Provider for application ID
- ❌ **`EndpointContainer`** - Container for service endpoints
- ❌ **`ExperimentalFeatures`** - Collection for experimental feature flags
- ❌ **`TelemetrySinks`** - Collection of telemetry sinks
- ❌ **`DefaultTelemetrySink`** - The default telemetry sink

#### Constructors
- ❌ `TelemetryConfiguration(string instrumentationKey)`
- ❌ `TelemetryConfiguration(string instrumentationKey, ITelemetryChannel channel)`

### Properties with Changed Behavior
- ✅ **`ConnectionString`** - Still exists but behavior differs
  - **2.x**: String property
  - **3.x**: Setting this calls OpenTelemetry configuration internally
- ✅ **`DisableTelemetry`** - Still exists but does not disable flow of telemetry (will be fixed later)

### Methods with changed Behavior
- CreateDefault() returns an internal static configuration instead of a new TelemetryConfiguration()

### New APIs Added in 3.x
- ✅ **`SamplingRatio`** (float?) - Gets or sets the sampling ratio for traces (0.0 to 1.0). A value of 1.0 means all telemetry is sent.
- ✅ **`TracesPerSecond`** (double?) - Gets or sets the number of traces per second for rate-limited sampling (default sampling mode).
- ✅ **`StorageDirectory`** (string) - Gets or sets the directory for offline telemetry storage.
- ✅ **`DisableOfflineStorage`** (bool?) - Gets or sets whether offline storage is disabled.
- ✅ **`EnableLiveMetrics`** (bool?) - Gets or sets whether Live Metrics is enabled.
- ✅ **`EnableTraceBasedLogsSampler`** (bool?) - Gets or sets whether trace-based log sampling is enabled.
- ✅ **`ConfigureOpenTelemetryBuilder(Action<IOpenTelemetryBuilder> configure)`** - Allows extending OpenTelemetry configuration
- ✅ **`SetAzureTokenCredential(TokenCredential tokenCredential)`** - Call this method to enable Azure Active Directory (AAD) authentication for Application Insights ingestion

### Migration Impact
1. Custom telemetry processors and initializers should be replaced by OpenTelemetry BaseProcessors or attributes added to the OpenTelemetry records.
2. Multi-sink scenarios are no longer supported
3. Endpoint customization is removed. Endpoints are parsed from the connection string.

---

## TelemetryContext Breaking Changes

Most TelemetryContext modules have now been marked internal or removed. The properties that have been retained are listed below.

### Properties Retained

The following remain **public**:
- ✅ `Cloud` (RoleName, RoleInstance)
    - Note: These are settable via resource attributes (service.name & service.instance.id) in OpenTelemetry; we are working on fixing functionality for setting the same via CloudContext.
- ✅ `User` (Id, AuthenticatedUserId, UserAgent)
- ✅ `Operation` (Name, SyntheticSource)
    - Note: A future work item is to make sure SyntheticSource can be read from properly and emitted in the telemetry item.
- ✅ `Location` (Ip)
- ✅ `GlobalProperties`

### Removed Properties

- ❌ **`Properties`** (obsolete) - Was obsoleted in 2.x in favor of `GlobalProperties`, now removed entirely

### Migration Guidance
- Please check your application code to make sure it does not make reference to removed or internalized Context properties.
- To set the cloud role name and instance, it is best to do so by utilizing the service.name & service.instance.id resource attributes when calling ConfigureOpenTelemetryBuilder.
- OpenTelemetry resource attributes can replace many of the context fields that are now internal. An alternative path is to add custom properties to the telemetry item.


---

# 2. Microsoft.ApplicationInsights.AspNetCore

## Extension Methods Removed

### From IServiceCollection
- ❌ **`AddApplicationInsightsTelemetry(string instrumentationKey)`** - Obsolete overload removed
- ❌ **`AddApplicationInsightsKubernetesEnricher()`** - Kubernetes enrichment removed

### From IApplicationBuilder
- ❌ **`UseApplicationInsightsRequestTelemetry()`** - Obsolete middleware removed
- ❌ **`UseApplicationInsightsExceptionTelemetry()`** - Obsolete middleware removed

### From IWebHostBuilder
- ❌ **`UseApplicationInsights()`** - All overloads removed (were obsolete in 2.x)

### From Shared Extensions
- ❌ **`AddApplicationInsightsTelemetryProcessor<T>()`** - Generic telemetry processor extension
- ❌ **`AddApplicationInsightsTelemetryProcessor(Type telemetryProcessorType)`** - Type-based telemetry processor
- ❌ **`AddTelemetryModule<T>()`** - Module configuration without options
- ❌ **`AddTelemetryModule<T>(Action<T> configure)`** - Module configuration with options
- ❌ **Configuration builder extensions** (both overloads)

## Extension Methods Retained
The following extension methods remain with identical signatures:
- ✅ `AddApplicationInsightsTelemetry()`
- ✅ `AddApplicationInsightsTelemetry(IConfiguration configuration)`
- ✅ `AddApplicationInsightsTelemetry(Action<ApplicationInsightsServiceOptions> configureOptions)`
- ✅ `AddApplicationInsightsTelemetry(ApplicationInsightsServiceOptions options)`

## Classes/Components Removed

### Entire Classes
- ❌ **`ApplicationInsightsLoggerProvider`** - Entire class removed
- ❌ **`ApplicationInsightsLogger`** - Logger implementation removed
- ❌ **`RequestTrackingMiddleware`** - Middleware class removed
- ❌ **`ExceptionTrackingMiddleware`** - Middleware class removed
- ❌ **`HostingDiagnosticListener`** - Diagnostic listener removed
- ❌ **`HostingStartupOptions`** - Configuration class removed

### TelemetryInitializers Removed (All 7)
- ❌ `AspNetCoreEnvironmentTelemetryInitializer`
- ❌ `AzureAppServiceRoleNameFromHostNameHeaderInitializer`
- ❌ `ClientIpHeaderTelemetryInitializer`
- ❌ `DomainNameRoleInstanceTelemetryInitializer`
- ❌ `HttpDependenciesParsingTelemetryInitializer`
- ❌ `OperationNameTelemetryInitializer`
- ❌ `WebSessionTelemetryInitializer`

## ApplicationInsightsServiceOptions Changes

### Properties Removed
- ❌ **`InstrumentationKey`** - Obsolete property removed (use `ConnectionString`)
- ❌ **`DeveloperMode`** - No longer configurable
- ❌ **`EndpointAddress`** - No longer configurable (`ConnectionString` contains endpoint information)
- ❌ **`TelemetryInitializers`** - Cannot configure initializers
- ❌ **`EnableHeartbeat`** - Heartbeat configuration removed
- ❌ **`RequestCollectionOptions`** - Removed (non-functional, use OpenTelemetry instrumentation options)
- ❌ **`DependencyCollectionOptions`** - Removed (non-functional, use OpenTelemetry instrumentation options)
- ❌ `EnableAdaptiveSampling`** - Removed, rate limited sampling is now the default.

### Properties Retained
- ✅ **`ConnectionString`** - Primary configuration method
- ✅ **`ApplicationVersion`**
- ✅ **`AddAutoCollectedMetricExtractor`**
- ✅ **`EnableQuickPulseMetricStream`** 
- ✅ **`EnableDebugLogger`** - Retained but has no effect
- ✅ **`EnableAuthenticationTrackingJavaScript`** - JavaScript auth tracking config
- ✅ **`EnableDependencyTrackingTelemetryModule`** - Dependency tracking toggle
- ✅ **`EnablePerformanceCounterCollectionModule`** - Performance counter toggle
- ✅ **`EnableRequestTrackingTelemetryModule`** - Request tracking toggle

### New Properties Added in 3.x
- ✅ **`Credential`** (Azure.Core.TokenCredential) - Enables Azure Active Directory (AAD) authentication
- ✅ **`TracesPerSecond`** (double?) - Gets or sets the number of traces per second for rate-limited sampling (default sampling mode). Replaces `EnableAdaptiveSampling`.
- ✅ **`SamplingRatio`** (float?) - Gets or sets the sampling ratio for traces (0.0 to 1.0). A value of 1.0 means all telemetry is sent. Replaces `EnableAdaptiveSampling`.

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

## Migration Guidance
- Any code depending on `InstrumentationKey` must migrate to `ConnectionString`
- Code checking or configuring the removed options will break
- Direct dependency on traditional AI SDK modules/processors/initializers will fail - consider learning about OpenTelemetry processors, resource detectors, and enrichment of telemetry. 

---

# 3. Microsoft.ApplicationInsights.WorkerService

## Extension Methods Removed
- ❌ **`AddApplicationInsightsTelemetryWorkerService(string instrumentationKey)`** - Instrumentation key overload removed (was obsolete in 2.x)

## Extension Methods Retained
The following extension methods remain with identical signatures:
- ✅ `AddApplicationInsightsTelemetryWorkerService()`
- ✅ `AddApplicationInsightsTelemetryWorkerService(IConfiguration configuration)`
- ✅ `AddApplicationInsightsTelemetryWorkerService(Action<ApplicationInsightsServiceOptions> configureOptions)`
- ✅ `AddApplicationInsightsTelemetryWorkerService(ApplicationInsightsServiceOptions options)`

## ApplicationInsightsServiceOptions Changes

### Properties Removed
- ❌ **`InstrumentationKey`** - Obsolete property removed (use `ConnectionString`)
- ❌ **`EnableEventCounterCollectionModule`** - EventCounter module configuration removed
- ❌ **`EnableAppServicesHeartbeatTelemetryModule`** - App Services heartbeat removed
- ❌ **`EnableAzureInstanceMetadataTelemetryModule`** - Azure instance metadata module removed
- ❌ **`EnableHeartbeat`** - Heartbeat configuration removed
- ❌ **`EnableDiagnosticsTelemetryModule`** - Diagnostics telemetry module removed
- ❌ **`DeveloperMode`** - No longer configurable
- ❌ **`EndpointAddress`** - No longer configurable (`ConnectionString` contains endpoints)
- ❌ **`DependencyCollectionOptions`** - Removed (non-functional, use OpenTelemetry instrumentation options)
- ❌ **`EnableAdaptiveSampling`** - Removed, rate limited sampling is now the default.

### Properties Retained
- ✅ **`ConnectionString`** - Primary configuration method (maps to `AzureMonitorExporterOptions.ConnectionString`)
- ✅ **`ApplicationVersion`** - Still configurable
- ✅ **`EnableDependencyTrackingTelemetryModule`** - Still configurable
- ✅ **`EnablePerformanceCounterCollectionModule`** - Still configurable
- ✅ **`EnableQuickPulseMetricStream`** - Maps to `AzureMonitorExporterOptions.EnableLiveMetrics`
- ✅ **`EnableDebugLogger`** - Still configurable though has no effect
- ✅ **`AddAutoCollectedMetricExtractor`** - Still configurable

### New Properties Added in 3.x
- ✅ **`Credential`** (Azure.Core.TokenCredential) - Enables Azure Active Directory (AAD) authentication
- ✅ **`TracesPerSecond`** (double?) - Gets or sets the number of traces per second for rate-limited sampling (default sampling mode). Replaces `EnableAdaptiveSampling`.
- ✅ **`SamplingRatio`** (float?) - Gets or sets the sampling ratio for traces (0.0 to 1.0). A value of 1.0 means all telemetry is sent. Replaces `EnableAdaptiveSampling`.

## Migration Impact
- Any code depending on `InstrumentationKey` must migrate to `ConnectionString`
- Code checking or configuring the removed options (`DeveloperMode`, `EndpointAddress`, etc.) will break
- Direct dependency on traditional AI SDK modules/processors/initializers will fail - consider learning about OpenTelemetry processors, resource detectors, and enrichment of telemetry. 

---

# 4. Microsoft.ApplicationInsights.NLogTarget

## API Changes

### Property Removed
- ❌ **`InstrumentationKey` property** (string)
  - **2.x**: `public string InstrumentationKey { get; set; }`
  - **3.x**: Completely removed
  - **Migration**: Use `ConnectionString` property instead

### New Property Added in 3.x
- ✅ **`Credential`** (Azure.Core.TokenCredential) - Enables Azure Active Directory (AAD) authentication for Application Insights

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

✅ **Critical Actions Required:**
1. Replace `InstrumentationKey` with `ConnectionString` in NLog configuration
2. Ensure connection string is always provided (no longer optional)
3. Update minimum .NET Framework from 4.5.2 to 4.6.2 if using .NET Framework

---

# 5. Microsoft.ApplicationInsights.Web (Classic ASP.NET)

## HttpModules and Telemetry Modules REMOVED

### 2.x Modules:
- `RequestTrackingTelemetryModule` - Main request tracking
- `ExceptionTrackingTelemetryModule` - Exception tracking
- `AspNetDiagnosticTelemetryModule` - ASP.NET diagnostic listener

### 3.x:
- ❌ **All telemetry modules removed**
- `ApplicationInsightsHttpModule` now directly handles telemetry using OpenTelemetry
- Configuration via `applicationinsights.config` drastically simplified

## Telemetry Initializers REMOVED

All public **TelemetryInitializers** from 2.x are **REMOVED from the public API** in 3.x:

**Impact**: 
- The public telemetry initializers are **removed from the public API**
- Many have been converted to **internal OpenTelemetry Processors** that run automatically
- Custom telemetry initializers must be replaced with OpenTelemetry's extensibility model (Activity Processors and Resource Detectors)

## Base Classes REMOVED
- ❌ `WebTelemetryInitializerBase` - Abstract base class for web telemetry initializers
- ❌ `TelemetryModuleBase` - Abstract base class for web telemetry modules
- **Impact**: No public extensibility model for creating custom web-specific initializers/modules

## Extension Methods Changes
- ❌ **`HttpContextExtension` class removed from public API**, though some functionality is maintained internally
- ❌ **`HttpContextBaseExtension` class removed entirely**

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
- ❌ `<TelemetryInitializers>` section no longer supported
- ❌ `<TelemetryModules>` section no longer supported
- ❌ `<InstrumentationKey>` replaced with `<ConnectionString>`
- ✅ Multiple new configuration elements supported (see example above)
- All instrumentation now **auto-configured via OpenTelemetry**

## Target Framework Changes
- **2.x**: `net452` (Targets .NET Framework 4.5.2)
- **3.x**: `net462` (Targets .NET Framework 4.6.2)
- **Breaking**: Minimum framework version raised from **4.5.2** → **4.6.2**