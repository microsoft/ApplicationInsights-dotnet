# OpenTelemetry Pipeline

## Overview

The OpenTelemetry pipeline is the data flow path for telemetry signals (traces, metrics, logs) from your application to observability backends. Understanding this pipeline is essential for customizing Application Insights 3.x.

## Pipeline Components

```
Trace Pipeline:    ActivitySource ──▶ BaseProcessor<Activity> ──▶ Exporters (Azure Monitor, OTLP, Console)
Metric Pipeline:   Meter ──────────▶ MetricReader ─────────────▶ Exporters (Azure Monitor, OTLP, Console)
Log Pipeline:      ILogger ────────▶ BaseProcessor<LogRecord> ─▶ Exporters (Azure Monitor, OTLP, Console)
```

Each signal (traces, metrics, logs) has its own independent pipeline. Processors only apply to their own signal — a trace processor never sees logs or metrics.

### 1. Instrumentation (Sources)
- **ActivitySource** — Creates spans/traces
- **Meter** — Creates metrics
- **ILogger** — Creates logs (via OpenTelemetry.Extensions.Logging)

### 2. Processors (Transform)
- **BaseProcessor\<Activity\>** — Enrich or filter spans
- **BaseProcessor\<LogRecord\>** — Enrich or filter logs
- Run in pipeline order before export

### 3. Exporters (Backends)
- **Azure Monitor Exporter** — Sends to Application Insights (auto-configured by 3.x SDK)
- **OTLP Exporter** — Sends to any OTLP-compatible backend
- **Console Exporter** — Debug output

## Key Differences from Application Insights 2.x

| 2.x Concept | 3.x Equivalent |
|---|---|
| TelemetryClient | ActivitySource / Meter / ILogger |
| ITelemetryInitializer | BaseProcessor\<Activity\>.OnStart |
| ITelemetryProcessor | BaseProcessor\<Activity\>.OnEnd |
| TelemetryChannel | Exporter |
| TelemetryConfiguration | OpenTelemetryBuilder |
| AddApplicationInsightsTelemetryProcessor\<T\>() | ConfigureOpenTelemetryTracerProvider + AddProcessor\<T\>() |

## Configuration API

The 3.x SDK sets up the OpenTelemetry pipeline internally. To customize it, use the `ConfigureOpenTelemetry*Provider` extension methods on `IServiceCollection`.

### Per-Signal Configuration Methods

| Method | Signal | Namespace |
|---|---|---|
| `services.ConfigureOpenTelemetryTracerProvider(...)` | Tracing | `OpenTelemetry.Trace` |
| `services.ConfigureOpenTelemetryMeterProvider(...)` | Metrics | `OpenTelemetry.Metrics` |
| `services.ConfigureOpenTelemetryLoggerProvider(...)` | Logging | `OpenTelemetry.Logs` |

These methods register configuration on the existing provider created by `AddApplicationInsightsTelemetry()`. **Do not call `AddOpenTelemetry()` separately** — the SDK already calls it.

### Tracing — Common Builder Methods

```csharp
builder.Services.ConfigureOpenTelemetryTracerProvider(tracing =>
{
    tracing.AddSource("MyApp");                    // Subscribe to an ActivitySource
    tracing.AddProcessor<MyProcessor>();            // Add a custom processor
    tracing.SetSampler(new TraceIdRatioBasedSampler(0.5)); // Custom sampler
    tracing.ConfigureResource(r => r.AddService("MyService")); // Set resource attributes
});
```

| Method | Purpose |
|---|---|
| `.AddSource(params string[] names)` | Subscribe to `ActivitySource` names — unregistered sources are dropped |
| `.AddProcessor<T>()` | Add processor resolved from DI |
| `.AddProcessor(BaseProcessor<Activity>)` | Add processor instance |
| `.SetSampler(Sampler)` | Set a custom sampler |
| `.ConfigureResource(Action<ResourceBuilder>)` | Set resource attributes (cloud role name, version, etc.) |

### Metrics — Common Builder Methods

```csharp
builder.Services.ConfigureOpenTelemetryMeterProvider(metrics =>
{
    metrics.AddMeter("MyApp");                     // Subscribe to a Meter
    metrics.AddView("request-duration",            // Customize aggregation
        new ExplicitBucketHistogramConfiguration
        {
            Boundaries = new double[] { 0.01, 0.05, 0.1, 0.5, 1, 5, 10 }
        });
});
```

| Method | Purpose |
|---|---|
| `.AddMeter(params string[] names)` | Subscribe to `Meter` names — unregistered meters are ignored |
| `.AddView(string, MetricStreamConfiguration)` | Customize aggregation for an instrument |
| `.SetMaxMetricStreams(int)` | Max metric streams (default: 1000) |

### Logging — Common Builder Methods

```csharp
builder.Services.ConfigureOpenTelemetryLoggerProvider(logging =>
{
    logging.AddProcessor<MyLogProcessor>();         // Add a log processor
});
```

| Method | Purpose |
|---|---|
| `.AddProcessor<T>()` | Add log processor resolved from DI |
| `.AddProcessor(BaseProcessor<LogRecord>)` | Add log processor instance |

### Configuring Options via DI

For instrumentation already registered by the SDK (SQL Client, HTTP Client, ASP.NET Core), configure options without re-adding instrumentation:

```csharp
// SQL Client options
builder.Services.Configure<SqlClientTraceInstrumentationOptions>(options =>
{
    options.SetDbStatementForText = true;
});

// HTTP Client options
builder.Services.Configure<HttpClientTraceInstrumentationOptions>(options =>
{
    options.RecordException = true;
});

// ASP.NET Core server options
builder.Services.Configure<AspNetCoreTraceInstrumentationOptions>(options =>
{
    options.RecordException = true;
});
```

### ConfigureResource — Setting Cloud Role Name and Service Info

```csharp
builder.Services.ConfigureOpenTelemetryTracerProvider(tracing =>
    tracing.ConfigureResource(r => r
        .AddService(
            serviceName: "MyService",
            serviceVersion: "1.0.0",
            serviceInstanceId: Environment.MachineName)));
```

Resource attributes are shared across all signals (traces, metrics, logs).

## Non-DI Configuration (Console / Classic ASP.NET)

For apps using `TelemetryConfiguration` directly, use `ConfigureOpenTelemetryBuilder`:

```csharp
using var config = TelemetryConfiguration.CreateDefault();
config.ConnectionString = "InstrumentationKey=...;IngestionEndpoint=...";
config.ConfigureOpenTelemetryBuilder(otel =>
{
    otel.WithTracing(tracing =>
    {
        tracing.AddSource("MyApp");
        tracing.AddProcessor<MyProcessor>();
    });
    otel.WithMetrics(metrics =>
    {
        metrics.AddMeter("MyApp");
    });

    // DI options pattern also works here via otel.Services
    otel.Services.Configure<SqlClientTraceInstrumentationOptions>(options =>
    {
        options.SetDbStatementForText = true;
    });
});
```

### WithTracing / WithMetrics / WithLogging

These methods on `IOpenTelemetryBuilder` configure each signal's provider:

| Method | Purpose |
|---|---|
| `.WithTracing(Action<TracerProviderBuilder>)` | Configure the tracing pipeline |
| `.WithMetrics(Action<MeterProviderBuilder>)` | Configure the metrics pipeline |
| `.WithLogging(Action<LoggerProviderBuilder>)` | Configure the logging pipeline |
| `.ConfigureResource(Action<ResourceBuilder>)` | Set resource attributes (shared across all signals) |

## Lifecycle

- **DI apps**: The Generic Host manages SDK lifecycle — do not call `Dispose()` manually
- **Non-DI apps**: You own `TelemetryConfiguration` — use `using var config` and call `Flush()` before exit
- **Safe to call multiple times**: `ConfigureOpenTelemetryTracerProvider` / `MeterProvider` / `LoggerProvider` can be called from multiple places — configuration is merged
