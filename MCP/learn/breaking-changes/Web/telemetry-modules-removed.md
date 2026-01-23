# Classic ASP.NET Telemetry Modules Removed

**Category:** Breaking Change  
**Applies to:** Classic ASP.NET (.NET Framework)  
**Migration Effort:** Medium  
**Related:** [telemetry-initializers-removed.md](telemetry-initializers-removed.md), [minimum-framework-changed.md](minimum-framework-changed.md)

## Change Summary

Classic ASP.NET telemetry modules (`RequestTrackingTelemetryModule`, `ExceptionTrackingTelemetryModule`, `AspNetDiagnosticTelemetryModule`) have been removed in 3.x. Functionality is now provided by OpenTelemetry HTTP instrumentation and Activity-based tracking.

## Migration

**2.x (ApplicationInsights.config):**
```xml
<TelemetryModules>
  <Add Type="Microsoft.ApplicationInsights.Web.RequestTrackingTelemetryModule"/>
  <Add Type="Microsoft.ApplicationInsights.Web.ExceptionTrackingTelemetryModule"/>
  <Add Type="Microsoft.ApplicationInsights.Web.AspNetDiagnosticTelemetryModule"/>
</TelemetryModules>
```

**3.x (Global.asax.cs):**
```csharp
protected void Application_Start()
{
    var config = TelemetryConfiguration.CreateDefault();
    config.ConnectionString = ConfigurationManager.AppSettings["APPLICATIONINSIGHTS_CONNECTION_STRING"];
    
    // HTTP module provides request/exception tracking automatically
    var client = new TelemetryClient(config);
}
```

## Replaced Modules

| 2.x Module | 3.x Replacement |
|------------|-----------------|
| `RequestTrackingTelemetryModule` | OpenTelemetry HTTP instrumentation |
| `ExceptionTrackingTelemetryModule` | Activity exception events |
| `AspNetDiagnosticTelemetryModule` | Activity sources |

## Migration Checklist

- [ ] Remove `ApplicationInsights.config` file
- [ ] Move configuration to code (Global.asax or Startup)
- [ ] Set `ConnectionString` in code or Web.config appSettings
- [ ] Remove module-specific configuration
- [ ] Test request and exception tracking still works

## See Also

- [telemetry-initializers-removed.md](telemetry-initializers-removed.md) - Initializer migration
- [minimum-framework-changed.md](minimum-framework-changed.md) - Framework version requirements
