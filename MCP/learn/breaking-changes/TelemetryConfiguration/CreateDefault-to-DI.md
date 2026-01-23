# TelemetryConfiguration: CreateDefault and Dependency Injection

**Category:** Breaking Change  
**Applies to:** TelemetryConfiguration API  
**Migration Effort:** Simple  
**Related:** [Active-removed.md](Active-removed.md), [parameterless-constructor.md](../TelemetryClient/parameterless-constructor.md)

## Change Summary

In 3.x, `TelemetryConfiguration.Active` has been replaced by `CreateDefault()` for simple scenarios and dependency injection for ASP.NET Core/Worker Service applications. This change eliminates the singleton anti-pattern and provides better lifecycle management.

## API Comparison

### 2.x Pattern

```csharp
// Singleton accessed via Active property
TelemetryConfiguration.Active.InstrumentationKey = "abc123-...";
var client = new TelemetryClient();  // Uses Active
```

### 3.x Patterns

```csharp
// Option 1: CreateDefault (lazy singleton)
var config = TelemetryConfiguration.CreateDefault();
config.ConnectionString = "InstrumentationKey=...";
var client = new TelemetryClient(config);

// Option 2: Dependency Injection (recommended)
builder.Services.AddApplicationInsightsTelemetry();
// Inject TelemetryClient via constructor
```

## Migration Strategies

### ASP.NET Core (Recommended Pattern)

**2.x:**
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddApplicationInsightsTelemetry("abc123-...");
}
```

**3.x:**
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
});
```

### Console Application

**2.x:**
```csharp
TelemetryConfiguration.Active.InstrumentationKey = "abc123-...";
var client = new TelemetryClient();
```

**3.x:**
```csharp
var config = TelemetryConfiguration.CreateDefault();
config.ConnectionString = "InstrumentationKey=abc123-...;IngestionEndpoint=https://...";
var client = new TelemetryClient(config);
config.Dispose();  // Dispose when done
```

## Key Differences

| Aspect | 2.x Active | 3.x CreateDefault | 3.x DI |
|--------|-----------|-------------------|--------|
| **Pattern** | Global singleton | Lazy singleton | Scoped/Singleton |
| **Lifecycle** | Application lifetime | Manual disposal | DI managed |
| **Testing** | Difficult | Moderate | Easy |
| **Multi-tenant** | Not supported | Possible | Recommended |

## Migration Checklist

- [ ] Replace `TelemetryConfiguration.Active` with `CreateDefault()` or DI
- [ ] Use `ConnectionString` instead of `InstrumentationKey`
- [ ] For DI scenarios, call `AddApplicationInsightsTelemetry()`
- [ ] For non-DI scenarios, dispose configuration when done

## See Also

- [Active-removed.md](Active-removed.md) - Active property removal details
- [parameterless-constructor.md](../TelemetryClient/parameterless-constructor.md) - Constructor changes
