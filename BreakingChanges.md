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

### Methods Unchanged (Identical Signatures)
The following methods remain unchanged:
- `IsEnabled()` - Returns bool
- `TrackTrace(string message)`
- `TrackTrace(string message, SeverityLevel severityLevel)`
- `TrackTrace(string message, IDictionary<string, string> properties)`
- `TrackTrace(string message, SeverityLevel severityLevel, IDictionary<string, string> properties)`
- `TrackTrace(TraceTelemetry telemetry)`
- `TrackMetric(string name, double value, IDictionary<string, string> properties)`
- `TrackMetric(MetricTelemetry telemetry)`
- `TrackDependency(string dependencyTypeName, string dependencyName, string data, DateTimeOffset startTime, TimeSpan duration, bool success)`
- `TrackDependency(string dependencyTypeName, string target, string dependencyName, string data, DateTimeOffset startTime, TimeSpan duration, string resultCode, bool success)`
- `TrackDependency(DependencyTelemetry telemetry)`
- `Flush()`
- `FlushAsync(CancellationToken cancellationToken)`

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

#### Methods
- ❌ **`CreateDefault()` changed from public to internal** - No longer part of public API

### Properties with Changed Behavior
- ✅ **`ConnectionString`** - Still exists but behavior differs
  - **2.x**: String property
  - **3.x**: Setting this calls OpenTelemetry configuration internally
- ✅ **`DisableTelemetry`** - Still exists but implementation differs
  - **3.x**: Calls `IsEnabled(false)` in setter

### New APIs Added in 3.x
- ✅ **`ConfigureOpenTelemetryBuilder(Action<OpenTelemetryBuilder> configure)`** - Allows extending OpenTelemetry configuration
- ✅ `ApplicationInsightsActivitySource` (internal) - Gets the default ActivitySource
- ✅ `MetricsManager` (internal) - Gets the MetricsManager for metrics tracking
- ✅ `Build()` (internal) - Builds the OpenTelemetry SDK
- ✅ `ConfigureCloudRole(string roleName, string roleInstance)` (internal) - Configures cloud role

### Migration Impact
**Critical:** The removal of the telemetry pipeline infrastructure means:
1. Custom telemetry processors must be replaced with OpenTelemetry processors
2. Telemetry initializers must be replaced with OpenTelemetry enrichment patterns
3. Multi-sink scenarios are no longer supported
4. Endpoint customization must be done through OpenTelemetry configuration

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

### Folders/Subsystems Removed
- ❌ `DiagnosticListeners` folder (containing `HostingDiagnosticListener`, `MvcDiagnosticListener`, etc.)
- ❌ `Implementation` folder (various internal classes)
- ❌ `Logging` folder (logger infrastructure)

## ApplicationInsightsServiceOptions Changes

### Properties Removed
- ❌ **`InstrumentationKey`** - Obsolete property removed (use `ConnectionString`)
- ❌ **`EnableQuickPulseMetricStream`** - Replaced with OpenTelemetry live metrics
- ❌ **`DeveloperMode`** - No longer configurable
- ❌ **`EnableAuthenticationTrackingJavaScript`** - JavaScript config removed
- ❌ **`EnableDebugLogger`** - Debug logging handled by OpenTelemetry
- ❌ **`RequestCollectionOptions`** - Request tracking configuration removed
- ❌ **`TelemetryInitializers`** - Cannot configure initializers

### Properties Retained
- ✅ **`ConnectionString`** - Primary configuration method
- ✅ **`EnableAdaptiveSampling`** - Still configurable (but behavior changed)
  - **2.x**: Controls traditional adaptive sampling
  - **3.x**: Maps to rate-limit based sampling in Azure Monitor Exporter
- ✅ **`ApplicationVersion`**
- ✅ **`DependencyCollectionOptions`**
- ✅ **`EnableHeartbeat`**
- ✅ **`AddAutoCollectedMetricExtractor`**

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

## Migration Impact

### High Impact
1. **All custom telemetry processors and modules configuration removed** - Must use OpenTelemetry Processors
2. **All custom telemetry initializers removed** - Use OpenTelemetry Processors, resource detectors and enrichment
3. **Request tracking configuration removed** - Handled automatically by OpenTelemetry
4. **No more manual middleware registration** - OpenTelemetry handles it

### Low Impact
- Basic `AddApplicationInsightsTelemetry()` calls remain compatible
- `ConnectionString`-based configuration still works
- Most service options remain the same

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

### Properties Retained
- ✅ **`ConnectionString`** - Primary configuration method (maps to `AzureMonitorExporterOptions.ConnectionString`)
- ✅ **`ApplicationVersion`** - Still configurable
- ✅ **`DeveloperMode`** - Still configurable
- ✅ **`EndpointAddress`** - Still configurable
- ✅ **`EnableAdaptiveSampling`** - Now controls `SamplingRatio` in Azure Monitor Exporter (1.0F when disabled)
- ✅ **`EnableDependencyTrackingTelemetryModule`** - Still configurable
- ✅ **`EnablePerformanceCounterCollectionModule`** - Still configurable
- ✅ **`EnableQuickPulseMetricStream`** - Maps to `AzureMonitorExporterOptions.EnableLiveMetrics`
- ✅ **`EnableDebugLogger`** - Still configurable
- ✅ **`AddAutoCollectedMetricExtractor`** - Still configurable
- ✅ **`DependencyCollectionOptions`** - Still available

## Implementation Changes

### 2.x Architecture:
- Direct Application Insights SDK integration
- Manual registration of telemetry modules, initializers, channels
- Dependencies: ServerTelemetryChannel, DependencyCollector, PerformanceCollector, etc.

### 3.x Architecture:
- Built on OpenTelemetry with Azure Monitor Exporter
- Auto-instrumentation via:
  - `OpenTelemetry.Instrumentation.Runtime`
  - `OpenTelemetry.Instrumentation.Http`
  - `OpenTelemetry.Instrumentation.SqlClient`
- All traditional telemetry modules replaced with OpenTelemetry instrumentation

## Migration Impact

### Low Impact (Compatible)
- Basic service registration calls remain unchanged
- `ConnectionString`-based configuration still works
- Main extension methods have same signatures

### High Impact (Breaking)
- Any code depending on `InstrumentationKey` must migrate to `ConnectionString`
- Code checking or configuring the removed options will break
- Direct dependency on traditional AI SDK modules/processors will fail
- Custom telemetry modules or initializers need OpenTelemetry equivalents

---

# 4. Microsoft.ApplicationInsights.NLogTarget

## API Changes

### Property Removed
- ❌ **`InstrumentationKey` property** (string)
  - **2.x**: `public string InstrumentationKey { get; set; }`
  - **3.x**: Completely removed
  - **Migration**: Use `ConnectionString` property instead

### Configuration Breaking Changes

#### Authentication Configuration (CRITICAL)
| Aspect | 2.x | 3.x |
|--------|-----|-----|
| **Configuration Property** | `InstrumentationKey` | `ConnectionString` |
| **Required?** | No (optional with warning) | **YES** (throws exception if missing) |
| **Validation** | Warns if empty | **Throws `NLogRuntimeException`** |

**Migration Required:** Users MUST update their NLog configuration:

**2.x Configuration:**
```xml
<target type="ApplicationInsightsTarget" InstrumentationKey="xxx" />
```

**3.x Configuration:**
```xml
<target type="ApplicationInsightsTarget" ConnectionString="InstrumentationKey=xxx;IngestionEndpoint=https://..." />
```

### New Lifecycle Methods
```csharp
// 3.x only
protected override void CloseTarget()
protected override void Dispose(bool disposing)
```
- Properly disposes `TelemetryConfiguration` to prevent resource leaks
- Users relying on disposal behavior may see differences

### Behavioral Changes

#### TelemetryClient Initialization
**2.x:** Uses deprecated `TelemetryConfiguration.Active` singleton
```csharp
this.telemetryClient = new TelemetryClient(); // Uses Active config
```

**3.x:** Creates explicit `TelemetryConfiguration` instance
```csharp
this.telemetryConfiguration = new TelemetryConfiguration();
this.telemetryConfiguration.ConnectionString = connectionString;
this.telemetryClient = new TelemetryClient(this.telemetryConfiguration);
```

#### FlushAsync
- **2.x**: Always attempts to flush
- **3.x**: Checks if `telemetryClient` is null before flushing (safer)

### Target Framework Changes
- **2.x**: `net452`, `netstandard2.0`
- **3.x**: `net462`, `netstandard2.0`
- **Breaking**: Minimum .NET Framework version raised from **4.5.2** → **4.6.2**

### Internal Implementation Changes
- **2.x**: `telemetryClient.Context.GetInternalContext().SdkVersion = ...`
- **3.x**: SDK version setting temporarily disabled (commented out with TODO)

## Migration Checklist

✅ **Critical Actions Required:**
1. Replace `InstrumentationKey` with `ConnectionString` in NLog configuration
2. Ensure connection string is always provided (no longer optional)
3. Update minimum .NET Framework from 4.5.2 to 4.6.2 if using .NET Framework

✅ **Recommended Actions:**
1. Test disposal behavior if relying on cleanup logic
2. Review any code expecting `InstrumentationKey` property
3. Enable NLog internal logging if debugging telemetry issues

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

| 2.x Telemetry Initializer (PUBLIC) | 3.x Status |
|-------------------------------------|-----------|
| `WebTestTelemetryInitializer` | ⚠️ Converted to internal `WebTestActivityProcessor` |
| `SyntheticUserAgentTelemetryInitializer` | ⚠️ Converted to internal `SyntheticUserAgentActivityProcessor` |
| `ClientIpHeaderTelemetryInitializer` | ⚠️ Converted to internal `ClientIpHeaderActivityProcessor` |
| `OperationNameTelemetryInitializer` | ❌ Removed (handled by OpenTelemetry) |
| `OperationCorrelationTelemetryInitializer` | ❌ Removed (handled by OpenTelemetry) |
| `UserTelemetryInitializer` | ⚠️ Converted to internal `UserActivityProcessor` |
| `AuthenticatedUserIdTelemetryInitializer` | ⚠️ Converted to internal `AuthenticatedUserIdActivityProcessor` |
| `AccountIdTelemetryInitializer` | ⚠️ Converted to internal `AccountIdActivityProcessor` |
| `SessionTelemetryInitializer` | ⚠️ Converted to internal `SessionActivityProcessor` |
| `ComponentVersionTelemetryInitializer` | ❌ Removed (handled by OpenTelemetry) |

**Impact**: 
- The public telemetry initializers are **removed from the public API**
- Many have been converted to **internal Activity Processors** that run automatically
- The functionality is preserved but no longer customizable through the same extensibility points
- Custom telemetry initializers must be replaced with OpenTelemetry's extensibility model (Activity Processors and Resource Detectors)

## Base Classes REMOVED
- ❌ `WebTelemetryInitializerBase` - Abstract base class for web telemetry initializers
- ❌ `TelemetryModuleBase` - Abstract base class for web telemetry modules
- **Impact**: No public extensibility model for creating custom web-specific initializers/modules

## Extension Methods Changes

### 2.x:
```csharp
// Two extension method classes (both public)
namespace System.Web
{
    public static class HttpContextExtension
    {
        public static RequestTelemetry GetRequestTelemetry(this HttpContext context);
    }
    
    public static class HttpContextBaseExtension  
    {
        [Obsolete]
        public static RequestTelemetry GetRequestTelemetry(this HttpContextBase context);
    }
}
```

### 3.x:
```csharp
// Only HttpContextExtension remains
namespace System.Web
{
    public static class HttpContextExtension
    {
        public static RequestTelemetry GetRequestTelemetry(this HttpContext context);
    }
}
```

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
</ApplicationInsights>
```

**Breaking Changes:**
- ❌ `<TelemetryInitializers>` section no longer supported
- ❌ `<TelemetryModules>` section no longer supported
- ❌ `<InstrumentationKey>` replaced with `<ConnectionString>`
- ✅ Only `ConnectionString` is read from config
- All instrumentation now **auto-configured via OpenTelemetry**

## Target Framework Changes
- **2.x**: `net452` (Targets .NET Framework 4.5.2)
- **3.x**: `net462` (Targets .NET Framework 4.6.2)
- **Breaking**: Minimum framework version raised from **4.5.2** → **4.6.2**

## Dependency Changes

### 2.x Dependencies:
```xml
<PackageReference Include="Microsoft.AspNet.TelemetryCorrelation" Version="1.0.8" />
<ProjectReference Include="WindowsServer.csproj" />
```

### 3.x Dependencies:
```xml
<PackageReference Include="OpenTelemetry.Exporter.Console" Version="1.14.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNet" Version="1.14.0-rc.1" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.14.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.SqlClient" Version="1.14.0-beta.1" />
<PackageReference Include="OpenTelemetry.Resources.Azure" Version="1.14.0-beta.1" />
```

- ❌ Removed `Microsoft.AspNet.TelemetryCorrelation` dependency
- ❌ Removed `WindowsServer` project reference
- ✅ Now based entirely on **OpenTelemetry instrumentation libraries**

## Migration Impact Summary

### Public API Removals:
1. ❌ All **10 public TelemetryInitializer classes** removed
2. ❌ All **3 public TelemetryModule classes** removed
3. ❌ **2 public base classes** removed
4. ❌ `HttpContextBaseExtension` class removed
5. ❌ Configuration via `applicationinsights.config` greatly simplified

### Architecture Changes:
- **Moved from**: Custom telemetry modules/initializers
- **Moved to**: OpenTelemetry Activity Processors (internal)
- All instrumentation handled automatically via OpenTelemetry libraries
- Telemetry configuration is no longer XML-based, but code-based through OpenTelemetry

### Migration Path:
- Users must **remove all custom TelemetryInitializers/Modules** from config file
- Customization must be done through **OpenTelemetry SDK** extensibility
- Update `applicationinsights.config` to use `ConnectionString` instead of `InstrumentationKey`
- Minimum .NET Framework version must be **4.6.2 or higher**

---

## General Migration Guidance

### Key Principles
1. **ConnectionString is mandatory** - All packages now require `ConnectionString` instead of `InstrumentationKey`
2. **OpenTelemetry-first** - Customization uses OpenTelemetry extensibility patterns
3. **Simplified configuration** - Reduced surface area with automatic instrumentation
4. **Minimum framework versions increased** - .NET Framework 4.6.2+ required

### Common Migration Steps
1. Update all Application Insights packages to 3.x together
2. Replace `InstrumentationKey` with `ConnectionString` everywhere
3. Remove custom telemetry initializers/processors and reimplement using OpenTelemetry patterns
4. Remove telemetry module configurations from XML config files
5. Test thoroughly - internal behavior has changed significantly despite similar APIs

### OpenTelemetry Extensibility
For customization in 3.x, use:
- **Activity Processors** (instead of Telemetry Processors)
- **Resource Detectors** (instead of Telemetry Initializers)
- **OpenTelemetry Builder extensions** via `TelemetryConfiguration.ConfigureOpenTelemetryBuilder()`

### Additional Resources
- [Shim Breaking Changes Documentation](docs/shim_breaking_changes.md)
- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/instrumentation/net/)
- [Azure Monitor OpenTelemetry Exporter](https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-enable)