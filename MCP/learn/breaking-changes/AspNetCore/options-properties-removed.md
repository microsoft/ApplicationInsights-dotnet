# ApplicationInsightsServiceOptions Properties Removed

**Category:** Breaking Change  
**Applies to:** ASP.NET Core Integration  
**Migration Effort:** Simple  
**Related:** [InstrumentationKey-property.md](../TelemetryClient/InstrumentationKey-property.md), [extension-methods-removed.md](extension-methods-removed.md)

## Change Summary

Multiple properties have been removed from `ApplicationInsightsServiceOptions` in 3.x, particularly those related to telemetry modules and legacy configuration. The `InstrumentationKey` property has been removed in favor of `ConnectionString`.

## Removed Properties

| Removed Property | 3.x Replacement |
|------------------|-----------------|
| `InstrumentationKey` | `ConnectionString` |
| `EndpointAddress` | Part of `ConnectionString` |
| `EnableHeartbeat` | Automatic via resource detectors |
| `EnableDiagnosticsTelemetryModule` | Built-in diagnostics |
| `RequestCollectionOptions` | OpenTelemetry instrumentation options |
| `DependencyCollectionOptions` | OpenTelemetry instrumentation options |

## Migration

**2.x:**
```csharp
services.AddApplicationInsightsTelemetry(options =>
{
    options.InstrumentationKey = "abc123-...";
    options.EndpointAddress = "https://custom-endpoint/v2/track";
    options.EnableHeartbeat = true;
    options.DeveloperMode = true;
});
```

**3.x:**
```csharp
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = "InstrumentationKey=abc123-...;IngestionEndpoint=https://custom-endpoint/";
    options.DeveloperMode = true;
    // Heartbeat automatic - no property needed
});
```

## Retained Properties

These properties still exist in 3.x:
- `ConnectionString`
- `DeveloperMode`
- `EnableAuthenticationTrackingJavaScript`
- `EnablePerformanceCounterCollectionModule` (ASP.NET Core only)

## Migration Checklist

- [ ] Replace `InstrumentationKey` with `ConnectionString`
- [ ] Move `EndpointAddress` into `ConnectionString` as `IngestionEndpoint`
- [ ] Remove `EnableHeartbeat` (automatic)
- [ ] Remove deprecated module-related properties
- [ ] Update configuration in appsettings.json

## See Also

- [InstrumentationKey-property.md](../TelemetryClient/InstrumentationKey-property.md) - InstrumentationKey migration
- [connection-string.md](../../azure-monitor-exporter/connection-string.md) - ConnectionString format
