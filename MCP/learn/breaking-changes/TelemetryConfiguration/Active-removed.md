# TelemetryConfiguration.Active Removed

**Category:** Breaking Change  
**Applies to:** TelemetryConfiguration API  
**Migration Effort:** Simple  
**Related:** [parameterless-constructor.md](../TelemetryClient/parameterless-constructor.md), [CreateDefault-to-DI.md](CreateDefault-to-DI.md)

## Change Summary

The static `TelemetryConfiguration.Active` property has been completely removed in 3.x. This singleton pattern was an anti-pattern that caused configuration conflicts in multi-tenant scenarios. Use `CreateDefault()` for simple scenarios or dependency injection for ASP.NET Core/Worker Service applications.

## API Comparison

### 2.x API

```csharp
// Source: ApplicationInsights-dotnet-2x/BASE/src/Microsoft.ApplicationInsights/Extensibility/TelemetryConfiguration.cs:118-141
#if NETSTANDARD
[Obsolete("We do not recommend using TelemetryConfiguration.Active on .NET Core.")]
#endif
public static TelemetryConfiguration Active
{
    get
    {
        if (active == null)
        {
            lock (syncRoot)
            {
                if (active == null)
                {
                    active = new TelemetryConfiguration();
                    TelemetryConfigurationFactory.Instance.Initialize(active, TelemetryModules.Instance);
                }
            }
        }
        return active;
    }
    
    internal set
    {
        lock (syncRoot)
        {
            active = value;
        }
    }
}
```

### 3.x API

```csharp
// Source: ApplicationInsights-dotnet/BASE/src/Microsoft.ApplicationInsights/Extensibility/TelemetryConfiguration.cs
// REMOVED: Active property does not exist

// Available alternatives:
public static TelemetryConfiguration CreateDefault()  // Line 126
{
    return DefaultInstance.Value;  // Lazy singleton
}
```

## Why It Changed

| Problem | Description |
|---------|-------------|
| **Global Singleton Anti-Pattern** | Single global instance caused conflicts in multi-tenant applications |
| **Testing Difficulty** | Global state made unit testing nearly impossible |
| **Configuration Confusion** | Unclear when configuration changes would affect existing clients |
| **Concurrency Issues** | Thread-safety problems with global mutable state |
| **DI Incompatibility** | Didn't work well with dependency injection patterns |

## Migration Strategies

### Option 1: ASP.NET Core / Worker Service (Recommended - Use DI)

**When to use:** ASP.NET Core, Azure Functions, Worker Service applications.

**2.x:**
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Modifying global singleton
        TelemetryConfiguration.Active.InstrumentationKey = "abc123-...";
        TelemetryConfiguration.Active.TelemetryInitializers.Add(new MyInitializer());
        
        services.AddApplicationInsightsTelemetry();
    }
}

public class MyService
{
    public void DoWork()
    {
        // Uses global Active configuration
        var client = new TelemetryClient();
        client.TrackEvent("Work");
    }
}
```

**3.x:**
```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
});

// Configure via DI - no global singleton
builder.Services.ConfigureOpenTelemetryTracerProvider(builder =>
{
    builder.AddProcessor<MyProcessor>();  // Replaces TelemetryInitializers
});

var app = builder.Build();

// Inject TelemetryClient via DI
public class MyService
{
    private readonly TelemetryClient telemetryClient;
    
    public MyService(TelemetryClient telemetryClient)
    {
        this.telemetryClient = telemetryClient;
    }
    
    public void DoWork()
    {
        telemetryClient.TrackEvent("Work");
    }
}
```

### Option 2: Console Application (Use CreateDefault)

**When to use:** Console apps, scripts, background services without DI.

**2.x:**
```csharp
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

class Program
{
    static void Main()
    {
        // Configure global singleton
        TelemetryConfiguration.Active.InstrumentationKey = "abc123-...";
        TelemetryConfiguration.Active.TelemetryInitializers.Add(new MyInitializer());
        
        var telemetryClient = new TelemetryClient();  // Uses Active
        telemetryClient.TrackEvent("AppStarted");
        telemetryClient.Flush();
        Task.Delay(5000).Wait();
    }
}
```

**3.x:**
```csharp
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

class Program
{
    static void Main()
    {
        // Create explicit configuration (lazy singleton)
        var config = TelemetryConfiguration.CreateDefault();
        config.ConnectionString = "InstrumentationKey=abc123-...;IngestionEndpoint=https://...";
        
        // Configure processors (replaces initializers)
        config.ConfigureOpenTelemetryBuilder(builder =>
        {
            builder.AddProcessor<MyProcessor>();
        });
        
        var telemetryClient = new TelemetryClient(config);
        telemetryClient.TrackEvent("AppStarted");
        telemetryClient.Flush();
        Task.Delay(5000).Wait();
        
        // Dispose when done
        config.Dispose();
    }
}
```

### Option 3: Multiple Configurations (Multi-Tenant)

**When to use:** Multi-tenant applications where different components need different Application Insights instances.

**2.x:**
```csharp
// Problem: Only one global Active configuration
public class TenantService
{
    private TelemetryClient GetClientForTenant(string tenantId)
    {
        // This doesn't work well - modifies global state
        TelemetryConfiguration.Active.InstrumentationKey = GetKeyForTenant(tenantId);
        return new TelemetryClient();  // Uses modified global config
    }
}
```

**3.x:**
```csharp
// Solution: Create separate configurations per tenant
public class TenantService
{
    private readonly Dictionary<string, TelemetryConfiguration> _tenantConfigs = new();
    
    private TelemetryClient GetClientForTenant(string tenantId)
    {
        if (!_tenantConfigs.ContainsKey(tenantId))
        {
            var config = new TelemetryConfiguration
            {
                ConnectionString = GetConnectionStringForTenant(tenantId)
            };
            _tenantConfigs[tenantId] = config;
        }
        
        return new TelemetryClient(_tenantConfigs[tenantId]);
    }
}
```

## Common Scenarios

### Scenario 1: Unit Testing

**2.x Problem:**
```csharp
[TestClass]
public class MyServiceTests
{
    [TestMethod]
    public void TestMethod1()
    {
        // Problem: Tests share global Active configuration
        TelemetryConfiguration.Active.InstrumentationKey = "test-key-1";
        var service = new MyService();
        service.DoWork();
        // Test assertions...
    }
    
    [TestMethod]
    public void TestMethod2()
    {
        // Problem: Previous test's key might still be set
        TelemetryConfiguration.Active.InstrumentationKey = "test-key-2";
        var service = new MyService();
        service.DoWork();
        // Test assertions...
    }
}
```

**3.x Solution:**
```csharp
[TestClass]
public class MyServiceTests
{
    [TestMethod]
    public void TestMethod1()
    {
        // Each test gets isolated configuration
        var config = new TelemetryConfiguration
        {
            ConnectionString = "InstrumentationKey=test-key-1;IngestionEndpoint=https://test/"
        };
        var telemetryClient = new TelemetryClient(config);
        var service = new MyService(telemetryClient);
        
        service.DoWork();
        // Test assertions...
        
        config.Dispose();
    }
    
    [TestMethod]
    public void TestMethod2()
    {
        // Completely independent configuration
        var config = new TelemetryConfiguration
        {
            ConnectionString = "InstrumentationKey=test-key-2;IngestionEndpoint=https://test/"
        };
        var telemetryClient = new TelemetryClient(config);
        var service = new MyService(telemetryClient);
        
        service.DoWork();
        // Test assertions...
        
        config.Dispose();
    }
}
```

### Scenario 2: Library Code

**2.x:**
```csharp
// Library that uses Application Insights
public class MyLibrary
{
    public void ProcessData()
    {
        // Problem: Depends on global Active being configured by host app
        var client = new TelemetryClient();
        client.TrackEvent("DataProcessed");
    }
}
```

**3.x:**
```csharp
// Library accepts TelemetryClient or TelemetryConfiguration
public class MyLibrary
{
    private readonly TelemetryClient telemetryClient;
    
    public MyLibrary(TelemetryClient telemetryClient)
    {
        this.telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
    }
    
    public void ProcessData()
    {
        telemetryClient.TrackEvent("DataProcessed");
    }
}

// Host application provides configuration
var config = TelemetryConfiguration.CreateDefault();
config.ConnectionString = "...";
var client = new TelemetryClient(config);
var library = new MyLibrary(client);
```

### Scenario 3: Global Configuration in ApplicationInsights.config

**2.x:**
```xml
<!-- ApplicationInsights.config -->
<ApplicationInsights xmlns="http://schemas.microsoft.com/ApplicationInsights/2013/Settings">
  <InstrumentationKey>abc123-def456-...</InstrumentationKey>
  <TelemetryInitializers>
    <Add Type="MyApp.CustomInitializer, MyApp" />
  </TelemetryInitializers>
</ApplicationInsights>
```

```csharp
// 2.x - Loaded automatically into Active
var client = new TelemetryClient();  // Uses config from file
```

**3.x:**
```csharp
// ApplicationInsights.config no longer used
// Configure in code (Program.cs or Startup.cs)

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
});

builder.Services.ConfigureOpenTelemetryTracerProvider(builder =>
{
    builder.AddProcessor<CustomProcessor>();  // Replaces TelemetryInitializer
});
```

## Replacement Patterns

### Pattern 1: Reading from Active

**2.x:**
```csharp
var key = TelemetryConfiguration.Active.InstrumentationKey;
var isDisabled = TelemetryConfiguration.Active.DisableTelemetry;
```

**3.x (via DI):**
```csharp
public class MyService
{
    private readonly TelemetryConfiguration configuration;
    
    public MyService(TelemetryConfiguration configuration)
    {
        this.configuration = configuration;
    }
    
    public void DoWork()
    {
        var connectionString = configuration.ConnectionString;
        var isDisabled = configuration.DisableTelemetry;
    }
}
```

**3.x (non-DI):**
```csharp
var config = TelemetryConfiguration.CreateDefault();
var connectionString = config.ConnectionString;
var isDisabled = config.DisableTelemetry;
```

### Pattern 2: Modifying Active

**2.x:**
```csharp
TelemetryConfiguration.Active.InstrumentationKey = "new-key";
TelemetryConfiguration.Active.DisableTelemetry = true;
```

**3.x:**
```csharp
// Create new configuration instead of modifying global
var config = new TelemetryConfiguration
{
    ConnectionString = "InstrumentationKey=new-key;...",
    DisableTelemetry = true
};
```

## Migration Checklist

- [ ] Search codebase for `TelemetryConfiguration.Active`
- [ ] For ASP.NET Core/Worker Service apps:
  - [ ] Add `services.AddApplicationInsightsTelemetry()` to Program.cs
  - [ ] Inject `TelemetryClient` and `TelemetryConfiguration` via DI
  - [ ] Remove all `Active` references
- [ ] For console apps:
  - [ ] Replace `Active` with `CreateDefault()`
  - [ ] Set `ConnectionString` on created configuration
  - [ ] Pass configuration to `TelemetryClient` constructor
  - [ ] Dispose configuration when application exits
- [ ] For unit tests:
  - [ ] Create isolated `TelemetryConfiguration` per test
  - [ ] Inject test configurations into services under test
- [ ] Remove `ApplicationInsights.config` files (no longer used)
- [ ] Update documentation and examples

## See Also

- [parameterless-constructor.md](../TelemetryClient/parameterless-constructor.md) - TelemetryClient() constructor removal
- [CreateDefault-to-DI.md](CreateDefault-to-DI.md) - CreateDefault() and DI patterns
- [InstrumentationKey-property.md](../TelemetryClient/InstrumentationKey-property.md) - ConnectionString migration
