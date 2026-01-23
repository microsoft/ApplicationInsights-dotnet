# Classic ASP.NET Telemetry Initializers Removed

**Category:** Breaking Change  
**Applies to:** Classic ASP.NET (.NET Framework)  
**Migration Effort:** Medium  
**Related:** [telemetry-modules-removed.md](telemetry-modules-removed.md), [TelemetryInitializers-removed.md](../TelemetryConfiguration/TelemetryInitializers-removed.md)

## Change Summary

Classic ASP.NET telemetry initializers (e.g., `ClientIpHeaderTelemetryInitializer`, `OperationCorrelationTelemetryInitializer`, `WebTestTelemetryInitializer`) have been removed in 3.x. Use `BaseProcessor<Activity>` for custom enrichment.

## Migration

**2.x (ApplicationInsights.config):**
```xml
<TelemetryInitializers>
  <Add Type="Microsoft.ApplicationInsights.Web.ClientIpHeaderTelemetryInitializer"/>
  <Add Type="Microsoft.ApplicationInsights.Web.OperationCorrelationTelemetryInitializer"/>
  <Add Type="Microsoft.ApplicationInsights.Web.UserTelemetryInitializer"/>
</TelemetryInitializers>
```

**3.x (Global.asax.cs):**
```csharp
protected void Application_Start()
{
    var config = TelemetryConfiguration.CreateDefault();
    config.ConnectionString = ConfigurationManager.AppSettings["APPLICATIONINSIGHTS_CONNECTION_STRING"];
    
    config.ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.AddProcessor<ClientIpProcessor>();
    });
}

public class ClientIpProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        var context = HttpContext.Current;
        if (context != null)
        {
            var ip = context.Request.UserHostAddress;
            activity?.SetTag("client.address", ip);
        }
    }
}
```

## Replaced Initializers

| 2.x Initializer | 3.x Replacement |
|-----------------|-----------------|
| `ClientIpHeaderTelemetryInitializer` | Custom processor with HttpContext |
| `OperationCorrelationTelemetryInitializer` | Automatic Activity correlation |
| `UserTelemetryInitializer` | Custom processor |
| `SessionTelemetryInitializer` | Custom processor |
| `WebTestTelemetryInitializer` | Custom processor |

## Migration Checklist

- [ ] Remove `ApplicationInsights.config` initializer entries
- [ ] Create `BaseProcessor<Activity>` classes for custom enrichment
- [ ] Register processors in code-based configuration
- [ ] Test enrichment appears in telemetry

## See Also

- [TelemetryInitializers-removed.md](../TelemetryConfiguration/TelemetryInitializers-removed.md) - General initializer migration
- [telemetry-modules-removed.md](telemetry-modules-removed.md) - Module migration
