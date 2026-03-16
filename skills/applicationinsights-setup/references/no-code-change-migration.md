# No-Code-Change Migration (2.x → 3.x)

## When This Applies

Your migration requires **only a package upgrade** if both are true:

1. You call `AddApplicationInsightsTelemetry()` with no arguments, an `IConfiguration`, or options that only set **unchanged properties**.
2. You do not use any **removed extension methods** (`UseApplicationInsights()`, `AddApplicationInsightsTelemetryProcessor<T>()`, `ConfigureTelemetryModule<T>()`).

### Unchanged Properties (safe to keep)

| Property | Default |
|---|---|
| `ConnectionString` | `null` |
| `ApplicationVersion` | Entry assembly version |
| `EnableQuickPulseMetricStream` | `true` |
| `EnablePerformanceCounterCollectionModule` | `true` |
| `EnableDependencyTrackingTelemetryModule` | `true` |
| `EnableRequestTrackingTelemetryModule` | `true` |
| `AddAutoCollectedMetricExtractor` | `true` |
| `EnableAuthenticationTrackingJavaScript` | `false` |

If your code only sets these properties, and does not use `ITelemetryInitializer` or `ITelemetryProcessor`, no code changes are needed.

## Migration Steps

### 1. Update the package

Use the package that matches your app type:

```xml
<!-- ASP.NET Core -->
<PackageReference Include="Microsoft.ApplicationInsights.AspNetCore" Version="3.*" />

<!-- Worker Service -->
<PackageReference Include="Microsoft.ApplicationInsights.WorkerService" Version="3.*" />

<!-- Console (base SDK) -->
<PackageReference Include="Microsoft.ApplicationInsights" Version="3.*" />
```

For Classic ASP.NET, this path does not apply — Classic always requires config changes.

### 2. Build and run

That's it. No code changes required.

## What Changes Under the Hood

Even though your code stays the same, 3.x brings improvements automatically:
- Telemetry collected via OpenTelemetry — better standards alignment
- `TracesPerSecond` (effective default `5`) provides rate-limited sampling out of the box
- Logging integrated automatically — `ILogger` output exported to Application Insights
- Azure resource detection (App Service, VM) happens automatically
