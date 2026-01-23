# ASP.NET Core Extension Methods Removed

**Category:** Breaking Change  
**Applies to:** ASP.NET Core Integration  
**Migration Effort:** Simple  
**Related:** [options-properties-removed.md](options-properties-removed.md)

## Change Summary

Obsolete middleware extension methods (`UseApplicationInsightsRequestTelemetry`, `UseApplicationInsightsExceptionTelemetry`) have been removed in 3.x. The overload of `AddApplicationInsightsTelemetry(string)` accepting an instrumentation key has also been removed. All configuration is now done through `AddApplicationInsightsTelemetry(options => ...)`.

## Migration

**2.x:**
```csharp
app.UseApplicationInsightsRequestTelemetry();
app.UseApplicationInsightsExceptionTelemetry();
services.AddApplicationInsightsTelemetry("abc123-...");
```

**3.x:**
```csharp
// Middleware calls removed - built into AddApplicationInsightsTelemetry
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = "InstrumentationKey=abc123-...;IngestionEndpoint=https://...";
});
```

## Migration Checklist

- [ ] Remove `UseApplicationInsightsRequestTelemetry()` calls
- [ ] Remove `UseApplicationInsightsExceptionTelemetry()` calls  
- [ ] Replace `AddApplicationInsightsTelemetry(string)` with `AddApplicationInsightsTelemetry(options => ...)`
- [ ] Use `ConnectionString` instead of passing instrumentation key

## See Also

- [options-properties-removed.md](options-properties-removed.md) - Options property changes
