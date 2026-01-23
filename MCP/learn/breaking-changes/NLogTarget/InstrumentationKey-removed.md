# NLog ApplicationInsightsTarget InstrumentationKey Property Removed

**Category:** Breaking Change  
**Applies to:** NLog Integration  
**Migration Effort:** Simple  
**Related:** [ConnectionString-required.md](ConnectionString-required.md), [InstrumentationKey-property.md](../TelemetryClient/InstrumentationKey-property.md)

## Change Summary

The `InstrumentationKey` property has been removed from `ApplicationInsightsTarget` in 3.x. Use the `ConnectionString` property instead.

## Migration

**2.x (NLog.config):**
```xml
<targets>
  <target type="ApplicationInsightsTarget" name="aiTarget">
    <instrumentationKey>${configsetting:APPINSIGHTS_INSTRUMENTATIONKEY}</instrumentationKey>
  </target>
</targets>
```

**3.x (NLog.config):**
```xml
<targets>
  <target type="ApplicationInsightsTarget" name="aiTarget">
    <connectionString>${configsetting:APPLICATIONINSIGHTS_CONNECTION_STRING}</connectionString>
  </target>
</targets>
```

## Environment Variables

**2.x:**
```bash
set APPINSIGHTS_INSTRUMENTATIONKEY=abc123-def456-...
```

**3.x:**
```bash
set APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=abc123-def456-...;IngestionEndpoint=https://...
```

## Migration Checklist

- [ ] Replace `<instrumentationKey>` with `<connectionString>` in NLog.config
- [ ] Update environment variable names
- [ ] Get full ConnectionString from Azure Portal (not just instrumentation key)
- [ ] Test NLog target sends logs to Application Insights

## See Also

- [ConnectionString-required.md](ConnectionString-required.md) - ConnectionString requirements
- [connection-string.md](../../azure-monitor-exporter/connection-string.md) - ConnectionString format
