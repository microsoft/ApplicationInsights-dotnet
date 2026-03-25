# Application Insights 2.x → 3.x Code Migration

## Contents
- [Property changes](#property-changes)
- [Removed extension methods](#removed-extension-methods)
- [Removed types](#removed-types)
- [TelemetryClient changes](#telemetryclient-changes)
- [Migration steps](#migration-steps)
- [Behavior notes](#behavior-notes)

## What Changed

3.x uses OpenTelemetry under the hood. The main entry point is the same — `AddApplicationInsightsTelemetry()` — but several options and extension methods were removed.

Key changes:
- `InstrumentationKey` → use `ConnectionString`
- `EnableAdaptiveSampling` → use `TracesPerSecond` (default `5`) or `SamplingRatio`
- Logging is automatic — `ApplicationInsightsLoggerProvider` was removed
- New: `Credential` for AAD authentication, `EnableTraceBasedLogsSampler` for log sampling control

## Before / After

**2.x**
```csharp
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.InstrumentationKey = "your-ikey";          // Removed
    options.EnableAdaptiveSampling = false;             // Removed
    options.DeveloperMode = true;                       // Removed
});
```

**3.x**
```csharp
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = "InstrumentationKey=...;IngestionEndpoint=...";
    options.SamplingRatio = 1.0f;       // No sampling (collect everything)
    // DeveloperMode — no replacement, remove the line
});
```

## Property Changes

| Property | Status | Action |
|---|---|---|
| `ConnectionString` | Unchanged | None |
| `ApplicationVersion` | Unchanged | None |
| `EnableQuickPulseMetricStream` | Unchanged | None. Default `true` |
| `EnablePerformanceCounterCollectionModule` | Unchanged | None. Default `true` |
| `EnableDependencyTrackingTelemetryModule` | Unchanged | None. Default `true` |
| `EnableRequestTrackingTelemetryModule` | Unchanged | None. Default `true` |
| `AddAutoCollectedMetricExtractor` | Unchanged | None. Default `true` |
| `EnableAuthenticationTrackingJavaScript` | Unchanged | None. Default `false` |
| `InstrumentationKey` | **Removed** | Use `ConnectionString` |
| `EnableAdaptiveSampling` | **Removed** | Use `TracesPerSecond` or `SamplingRatio` |
| `DeveloperMode` | **Removed** | Delete the line |
| `EndpointAddress` | **Removed** | Endpoint is part of `ConnectionString` |
| `EnableHeartbeat` | **Removed** | Delete; heartbeat is automatic |
| `EnableDebugLogger` | **Removed** | Delete |
| `RequestCollectionOptions` | **Removed** | Delete |
| `DependencyCollectionOptions` | **Removed** | Delete |
| `TelemetryInitializers` | **Removed** | Delete |
| `Credential` | **New** | `TokenCredential`, default `null`. Set for AAD auth |
| `TracesPerSecond` | **New** | `double?`, effective default `5`. Rate-limited sampling |
| `SamplingRatio` | **New** | `float?`, default `null`. Fixed-rate sampling (0.0–1.0) |
| `EnableTraceBasedLogsSampler` | **New** | `bool?`, effective default `true` |

### Worker Service–Only Removed Properties

These apply only to `AddApplicationInsightsTelemetryWorkerService()`:

| Property | Status | Action |
|---|---|---|
| `EnableEventCounterCollectionModule` | **Removed** | Delete |
| `EnableAppServicesHeartbeatTelemetryModule` | **Removed** | Delete |
| `EnableAzureInstanceMetadataTelemetryModule` | **Removed** | Delete |
| `EnableDiagnosticsTelemetryModule` | **Removed** | Delete |

## Removed Extension Methods

| Method | Replacement |
|---|---|
| `AddApplicationInsightsTelemetry(string ikey)` | Parameterless overload + `ConnectionString` in options or env var |
| `AddApplicationInsightsTelemetryWorkerService(string ikey)` | Parameterless overload + `ConnectionString` in options or env var |
| `UseApplicationInsights()` (IWebHostBuilder) | `AddApplicationInsightsTelemetry()` on `IServiceCollection` |
| `AddApplicationInsightsTelemetryProcessor<T>()` | OpenTelemetry processors via `.AddProcessor<T>()` |
| `ConfigureTelemetryModule<T>()` | Removed; module functionality is built-in |

## Removed Types

| Type | Notes |
|---|---|
| `ITelemetryInitializer` | Convert to `BaseProcessor<Activity>` with `OnStart`. See [initializer-migration.md](initializer-migration.md) |
| `ITelemetryProcessor` | Convert to `BaseProcessor<Activity>` with `OnEnd`. See [processor-migration.md](processor-migration.md) |
| `ApplicationInsightsLoggerProvider` | Logging is automatic. No replacement needed |
| `ExceptionTrackingMiddleware` | Exception tracking is built-in |

## TelemetryClient Changes

| Change | Details |
|---|---|
| `TrackEvent` | 3-param overload (with `IDictionary<string,double>`) **removed**. Use 2-param and track metrics separately via `TrackMetric()` |
| `TrackException` | 3-param overload with metrics dict **removed**. Use 2-param |
| `TrackAvailability` | 8-param overload with metrics dict **removed**. Use 7-param |
| `TrackPageView` | **Removed entirely**. Use `TrackEvent` or `TrackRequest` |
| `GetMetric` | `MetricConfiguration` and `MetricAggregationScope` params **removed**. Use simplified `GetMetric(metricId, ...)` |
| `new TelemetryClient()` | Parameterless constructor **removed**. Use `TelemetryClient(TelemetryConfiguration)` via DI |
| `client.InstrumentationKey` | **Removed**. Use `TelemetryConfiguration.ConnectionString` |
| `TrackTrace`, `TrackMetric`, `TrackRequest`, `TrackDependency` (full overload), `Flush` | **Unchanged** |

## Migration Steps

1. Update the package (use the one matching your app type):

   ```xml
   <!-- ASP.NET Core -->
   <PackageReference Include="Microsoft.ApplicationInsights.AspNetCore" Version="3.*" />

   <!-- Worker Service -->
   <PackageReference Include="Microsoft.ApplicationInsights.WorkerService" Version="3.*" />

   <!-- Console (base SDK) -->
   <PackageReference Include="Microsoft.ApplicationInsights" Version="3.*" />
   ```

   For Classic ASP.NET, use Package Manager Console:
   ```
   Update-Package Microsoft.ApplicationInsights.Web
   ```

2. Find and replace:
   - `InstrumentationKey = "..."` → `ConnectionString = "InstrumentationKey=...;IngestionEndpoint=..."`
   - `EnableAdaptiveSampling = false` → `SamplingRatio = 1.0f`
   - Delete lines setting `DeveloperMode`, `EndpointAddress`, `EnableHeartbeat`, `EnableDebugLogger`, `RequestCollectionOptions`, `DependencyCollectionOptions`, `TelemetryInitializers`
   - Delete calls to `UseApplicationInsights()`, `AddApplicationInsightsTelemetryProcessor<T>()`, `ConfigureTelemetryModule<T>()`

3. Migrate custom types:
   - Convert `ITelemetryInitializer` → `BaseProcessor<Activity>` with `OnStart`
   - Convert `ITelemetryProcessor` → `BaseProcessor<Activity>` with `OnEnd`
   - Fix `TelemetryClient` breaking changes (see table above)

4. Build and verify.

## Console / Non-DI Migration

For console apps or other non-DI scenarios using `TelemetryConfiguration` directly:

**2.x**
```csharp
var config = TelemetryConfiguration.Active; // Deprecated
config.InstrumentationKey = "your-ikey";
config.TelemetryInitializers.Add(new MyInitializer());
var client = new TelemetryClient();
```

**3.x**
```csharp
using var config = TelemetryConfiguration.CreateDefault();
config.ConnectionString = "InstrumentationKey=...;IngestionEndpoint=...";
config.ConfigureOpenTelemetryBuilder(otel =>
{
    otel.WithTracing(t => t.AddProcessor<MyProcessor>());
    otel.WithMetrics(m => m.AddMeter("MyApp"));
});
var client = new TelemetryClient(config); // Must pass config
```

Key changes:
- `TelemetryConfiguration.Active` → `TelemetryConfiguration.CreateDefault()`
- `config.InstrumentationKey` → `config.ConnectionString`
- `config.TelemetryInitializers.Add(...)` → `config.ConfigureOpenTelemetryBuilder(otel => otel.WithTracing(t => t.AddProcessor<T>()))`
- `new TelemetryClient()` → `new TelemetryClient(config)`

## Behavior Notes

- `TracesPerSecond` is the default sampling mode (effective default `5`). No configuration needed for most apps.
- Connection string resolution order: `ApplicationInsightsServiceOptions.ConnectionString` → `APPLICATIONINSIGHTS_CONNECTION_STRING` env var → `ApplicationInsights:ConnectionString` in config.
