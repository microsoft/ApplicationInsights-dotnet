# Brownfield Analysis Template

## Purpose

Before migrating from Application Insights 2.x to 3.x, scan the codebase to understand what needs to change. Fill in each section below by searching the code.

## Section 1: Service Configuration Options

Search for `ApplicationInsightsServiceOptions` or `AddApplicationInsightsTelemetry(options =>` in the codebase.

List every property being set. Mark each as:
- **Safe** — property is unchanged in 3.x (`ConnectionString`, `EnableQuickPulseMetricStream`, `EnablePerformanceCounterCollectionModule`, `EnableDependencyTrackingTelemetryModule`, `EnableRequestTrackingTelemetryModule`, `AddAutoCollectedMetricExtractor`, `ApplicationVersion`, `EnableAuthenticationTrackingJavaScript`)
- **Removed** — must be deleted or replaced (`InstrumentationKey`, `EnableAdaptiveSampling`, `DeveloperMode`, `EndpointAddress`, `EnableHeartbeat`, `EnableDebugLogger`, `RequestCollectionOptions`, `DependencyCollectionOptions`, `TelemetryInitializers`)

## Section 2: Telemetry Initializers

Search for classes implementing `ITelemetryInitializer` and registrations like `services.AddSingleton<ITelemetryInitializer, ...>()`.

For each initializer found, note:
- Class name and file location
- What it does (adds tags? modifies properties?)
- Which telemetry types it touches (requests, dependencies, traces, all?)

These must be converted to `BaseProcessor<Activity>` with `OnStart`. See [initializer-migration.md](initializer-migration.md).

## Section 3: Telemetry Processors

Search for classes implementing `ITelemetryProcessor` and calls to `AddApplicationInsightsTelemetryProcessor<T>()`.

For each processor found, note:
- Class name and file location
- Whether it filters (drops telemetry) or enriches (adds data)
- Which telemetry types it checks (`RequestTelemetry`, `DependencyTelemetry`, `TraceTelemetry`, etc.)

These must be converted to `BaseProcessor<Activity>` with `OnEnd`. See [processor-migration.md](processor-migration.md).

## Section 4: TelemetryClient Usage

Search for `TelemetryClient` usage that has **breaking changes** in 3.x:

- `TrackEvent` — check for 3-param overload with `IDictionary<string, double>` (removed)
- `TrackException` — check for 3-param overload (removed)
- `TrackAvailability` — check for 8-param overload (removed)
- `TrackPageView` — removed entirely, replace with `TrackEvent` or `TrackRequest`
- `GetMetric` — check for `MetricConfiguration` or `MetricAggregationScope` params (removed)
- `new TelemetryClient()` — parameterless constructor removed, use DI
- `.InstrumentationKey` — removed, use `TelemetryConfiguration.ConnectionString`

The following `TelemetryClient` methods are **unchanged in 3.x** and do not need migration:
`TrackTrace`, `TrackMetric`, `TrackRequest`, `TrackDependency` (full overload), `TrackEvent` (2-param), `TrackException` (2-param), `Flush`.

See [telemetryclient-migration.md](telemetryclient-migration.md) for breaking change details.

## Section 5: Sampling Configuration

Search for `EnableAdaptiveSampling`, `SamplingPercentage`, `SamplingTelemetryProcessor`, `AdaptiveSamplingTelemetryProcessor`, or custom `ITelemetryProcessorFactory` for sampling.

3.x uses `TracesPerSecond` (default 5) or `SamplingRatio` instead. See [sampling-migration.md](sampling-migration.md).

## Section 5a: AAD / Token Credential Authentication

Search for:
- `SetAzureTokenCredential` calls on `TelemetryConfiguration`
- `IConfigureOptions<TelemetryConfiguration>` classes that configure credentials
- Class names or code containing "credential", "aad", "token", "Entra", or `DefaultAzureCredential`

If found, see [aad-authentication-migration.md](aad-authentication-migration.md).

## Section 6: Removed Extension Methods

Search for these method calls — all removed in 3.x:
- `UseApplicationInsights()` (any `IWebHostBuilder` overload)
- `AddApplicationInsightsTelemetryProcessor<T>()`
- `ConfigureTelemetryModule<T>()`
- `AddApplicationInsightsTelemetry(string instrumentationKey)` (string overload)

## Section 7: Telemetry Pipeline

Search for custom `ITelemetryChannel`, `TelemetrySinks`, `DefaultTelemetrySink`, or `TelemetryProcessorChainBuilder` usage. These are all removed in 3.x.

## Section 8: Logging Configuration

Search specifically for:
- `AddApplicationInsights()` on `ILoggingBuilder` — must be removed
- `AddFilter<ApplicationInsightsLoggerProvider>()` — must be replaced with `AddFilter<OpenTelemetryLoggerProvider>()`

See [ilogger-migration.md](ilogger-migration.md).

## Section 9: Classic ASP.NET Specific (if applicable)

If this is a Classic ASP.NET project:
- Check `applicationinsights.config` for `<TelemetryInitializers>`, `<TelemetryModules>`, `<TelemetryProcessors>`, `<TelemetryChannel>`, `<InstrumentationKey>` — all must be migrated
- Check `Web.config` for `TelemetryCorrelationHttpModule` — must be removed
- Check for satellite packages in `packages.config`: `WindowsServer`, `WindowsServer.TelemetryChannel`, `DependencyCollector`, `PerfCounterCollector`, `Agent.Intercept`, `AspNet.TelemetryCorrelation`
- Check for `TelemetryConfiguration.Active` usage — replaced with `TelemetryConfiguration.CreateDefault()` in 3.x

## Decision

If **none** of sections 2-6 found matches → no code changes needed, just upgrade the package. See [no-code-change-migration.md](no-code-change-migration.md).

If **any** section found matches → code changes required. Start with [code-migration.md](code-migration.md).
