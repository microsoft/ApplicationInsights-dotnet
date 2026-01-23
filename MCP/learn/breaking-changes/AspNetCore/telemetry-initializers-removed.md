# ASP.NET Core Telemetry Initializers Removed

**Category:** Breaking Change  
**Applies to:** ASP.NET Core Integration  
**Migration Effort:** Medium  
**Related:** [TelemetryInitializers-removed.md](../TelemetryConfiguration/TelemetryInitializers-removed.md), [BaseProcessor-OnStart.md](../../api-reference/BaseProcessor/OnStart.md)

## Change Summary

ASP.NET Core-specific telemetry initializers (e.g., `ClientIpHeaderTelemetryInitializer`, `AspNetCoreEnvironmentTelemetryInitializer`, `OperationNameTelemetryInitializer`) have been removed in 3.x. Their functionality is now provided automatically by OpenTelemetry ASP.NET Core instrumentation or can be implemented via `BaseProcessor<Activity>`.

## Migration

**2.x:**
```csharp
services.AddSingleton<ITelemetryInitializer, ClientIpHeaderTelemetryInitializer>();
services.AddSingleton<ITelemetryInitializer, OperationNameTelemetryInitializer>();
```

**3.x:**
```csharp
// Built-in via AddApplicationInsightsTelemetry() - no action needed for most cases

// Custom enrichment via processor
public class ClientIpProcessor : BaseProcessor<Activity>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public ClientIpProcessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public override void OnStart(Activity activity)
    {
        var ip = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress;
        if (ip != null) activity?.SetTag("client.address", ip.ToString());
    }
}

builder.Services.ConfigureOpenTelemetryTracerProvider(builder =>
{
    builder.AddProcessor<ClientIpProcessor>();
});
```

## Replaced Initializers

| 2.x Initializer | 3.x Replacement |
|-----------------|-----------------|
| `ClientIpHeaderTelemetryInitializer` | Automatic HTTP instrumentation + custom processor |
| `OperationNameTelemetryInitializer` | Automatic from route templates |
| `AspNetCoreEnvironmentTelemetryInitializer` | Resource detectors |
| `SessionTelemetryInitializer` | Activity context propagation |
| `UserTelemetryInitializer` | Custom processor with HttpContext |

## Migration Checklist

- [ ] Identify ASP.NET Core-specific initializer registrations
- [ ] Verify automatic instrumentation covers requirements
- [ ] Create custom processors for additional enrichment
- [ ] Register processors in `ConfigureOpenTelemetryTracerProvider`

## See Also

- [TelemetryInitializers-removed.md](../TelemetryConfiguration/TelemetryInitializers-removed.md) - General initializer migration
- [BaseProcessor-OnStart.md](../../api-reference/BaseProcessor/OnStart.md) - Processor implementation
