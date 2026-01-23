# NLog ApplicationInsightsTarget ConnectionString Required

**Category:** Breaking Change  
**Applies to:** NLog Integration  
**Migration Effort:** Simple  
**Related:** [InstrumentationKey-removed.md](InstrumentationKey-removed.md)

## Change Summary

The `ConnectionString` property is now **required** in `ApplicationInsightsTarget` configuration in 3.x. The target will throw `ArgumentNullException` if ConnectionString is null or empty. There is no fallback to `TelemetryConfiguration.Active` like in 2.x.

## Migration

**2.x (optional ConnectionString):**
```xml
<targets>
  <!-- Would fall back to TelemetryConfiguration.Active if not specified -->
  <target type="ApplicationInsightsTarget" name="aiTarget"/>
</targets>
```

**3.x (required ConnectionString):**
```xml
<targets>
  <target type="ApplicationInsightsTarget" name="aiTarget">
    <connectionString>${environment:APPLICATIONINSIGHTS_CONNECTION_STRING}</connectionString>
  </target>
</targets>
```

## Configuration Sources

### Environment Variable
```xml
<connectionString>${environment:APPLICATIONINSIGHTS_CONNECTION_STRING}</connectionString>
```

### App Settings
```xml
<connectionString>${configsetting:ApplicationInsights:ConnectionString}</connectionString>
```

### Hard-Coded (Not Recommended)
```xml
<connectionString>InstrumentationKey=abc123-...;IngestionEndpoint=https://...</connectionString>
```

## Migration Checklist

- [ ] Add `<connectionString>` element to all ApplicationInsightsTarget configurations
- [ ] Set `APPLICATIONINSIGHTS_CONNECTION_STRING` environment variable
- [ ] Or configure in appsettings.json/app.config
- [ ] Verify target initializes without throwing exception
- [ ] Test logs flow to Application Insights

## See Also

- [InstrumentationKey-removed.md](InstrumentationKey-removed.md) - InstrumentationKey property removal
- [connection-string.md](../../azure-monitor-exporter/connection-string.md) - ConnectionString format
