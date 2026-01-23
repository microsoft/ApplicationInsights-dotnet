# TelemetryClient Parameterless Constructor Removed

**Category:** Breaking Change  
**Applies to:** TelemetryClient API  
**Migration Effort:** Simple  
**Related:** [Active-removed.md](../TelemetryConfiguration/Active-removed.md), [CreateDefault-to-DI.md](../TelemetryConfiguration/CreateDefault-to-DI.md)

## Change Summary

The parameterless `TelemetryClient()` constructor has been removed in 3.x. You must now provide a `TelemetryConfiguration` instance. This aligns with dependency injection patterns and eliminates the singleton anti-pattern from `TelemetryConfiguration.Active`.

## API Comparison

### 2.x Signature

```csharp
// Source: ApplicationInsights-dotnet-2x/BASE/src/Microsoft.ApplicationInsights/TelemetryClient.cs:36-40
[Obsolete("We do not recommend using TelemetryConfiguration.Active on .NET Core")]
public TelemetryClient() : this(TelemetryConfiguration.Active)
{
}
```

### 3.x Signature

```csharp
// Source: ApplicationInsights-dotnet/BASE/src/Microsoft.ApplicationInsights/TelemetryClient.cs:35-40
// REMOVED: Parameterless constructor does not exist

// Only constructor available:
public TelemetryClient(TelemetryConfiguration configuration)
    : this(configuration, isFromDependencyInjection: false)
{
}
```

## Why It Changed

| Issue | Description |
|-------|-------------|
| **Singleton Anti-pattern** | Parameterless constructor relied on `TelemetryConfiguration.Active`, a global singleton that caused configuration conflicts in multi-tenant scenarios |
| **DI Incompatibility** | Prevented proper dependency injection lifecycle management |
| **Unclear Configuration** | Made it unclear which configuration instance was being used |
| **Testing Difficulty** | Hard to test code that relied on global state |

## Migration Strategies

### Option 1: ASP.NET Core (Recommended - Use DI)

**When to use:** ASP.NET Core, Worker Service, or any .NET application with DI support.

**2.x:**
```csharp
// Source: Typical 2.x pattern
public class OrderController : Controller
{
    private readonly TelemetryClient telemetryClient;

    public OrderController()
    {
        // Uses TelemetryConfiguration.Active
        this.telemetryClient = new TelemetryClient();
    }

    public IActionResult Create()
    {
        telemetryClient.TrackEvent("OrderCreated");
        return Ok();
    }
}
```

**3.x:**
```csharp
// Register in Program.cs or Startup.cs
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
});

// Inject via constructor
public class OrderController : Controller
{
    private readonly TelemetryClient telemetryClient;

    public OrderController(TelemetryClient telemetryClient)
    {
        this.telemetryClient = telemetryClient;
    }

    public IActionResult Create()
    {
        telemetryClient.TrackEvent("OrderCreated");
        return Ok();
    }
}
```

### Option 2: Console Applications (Manual Configuration)

**When to use:** Console apps, background services without DI.

**2.x:**
```csharp
// Source: Typical 2.x console app pattern
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

class Program
{
    static void Main()
    {
        // Uses TelemetryConfiguration.Active
        TelemetryConfiguration.Active.InstrumentationKey = "abc123-def456-...";
        var telemetryClient = new TelemetryClient();
        
        telemetryClient.TrackEvent("ApplicationStarted");
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
        // Create configuration explicitly
        var config = TelemetryConfiguration.CreateDefault();
        config.ConnectionString = "InstrumentationKey=abc123-def456-...;IngestionEndpoint=https://...";
        
        var telemetryClient = new TelemetryClient(config);
        
        telemetryClient.TrackEvent("ApplicationStarted");
        telemetryClient.Flush();
        Task.Delay(5000).Wait();
        
        // Dispose configuration when done
        config.Dispose();
    }
}
```

### Option 3: Testing Scenarios

**When to use:** Unit tests where you need a test-specific configuration.

**2.x:**
```csharp
[TestMethod]
public void TestOrderTracking()
{
    // Problem: Uses global Active configuration
    var telemetryClient = new TelemetryClient();
    
    // Test code...
}
```

**3.x:**
```csharp
[TestMethod]
public void TestOrderTracking()
{
    // Create isolated configuration for this test
    var config = new TelemetryConfiguration
    {
        ConnectionString = "InstrumentationKey=test-key;IngestionEndpoint=https://test/"
    };
    
    var telemetryClient = new TelemetryClient(config);
    
    // Test code...
    
    config.Dispose();
}
```

## Common Scenarios

### Scenario 1: Legacy Library Code

**Problem:** Existing library uses parameterless constructor.

**2.x:**
```csharp
public class OrderProcessor
{
    private readonly TelemetryClient telemetryClient = new TelemetryClient();
    
    public void ProcessOrder(Order order)
    {
        telemetryClient.TrackEvent("OrderProcessed");
    }
}
```

**3.x Option A (Pass configuration):**
```csharp
public class OrderProcessor
{
    private readonly TelemetryClient telemetryClient;
    
    public OrderProcessor(TelemetryConfiguration configuration)
    {
        this.telemetryClient = new TelemetryClient(configuration);
    }
    
    public void ProcessOrder(Order order)
    {
        telemetryClient.TrackEvent("OrderProcessed");
    }
}
```

**3.x Option B (Inject TelemetryClient directly):**
```csharp
public class OrderProcessor
{
    private readonly TelemetryClient telemetryClient;
    
    public OrderProcessor(TelemetryClient telemetryClient)
    {
        this.telemetryClient = telemetryClient;
    }
    
    public void ProcessOrder(Order order)
    {
        telemetryClient.TrackEvent("OrderProcessed");
    }
}
```

### Scenario 2: Static Utility Class

**Problem:** Static utility methods used parameterless constructor.

**2.x:**
```csharp
public static class TelemetryHelper
{
    public static void TrackError(string message)
    {
        var client = new TelemetryClient();
        client.TrackTrace(message, SeverityLevel.Error);
    }
}
```

**3.x Option A (Require configuration parameter):**
```csharp
public static class TelemetryHelper
{
    public static void TrackError(TelemetryClient client, string message)
    {
        client.TrackTrace(message, SeverityLevel.Error);
    }
}

// Usage
TelemetryHelper.TrackError(telemetryClient, "Error occurred");
```

**3.x Option B (Use singleton pattern explicitly):**
```csharp
public static class TelemetryHelper
{
    private static readonly Lazy<TelemetryClient> LazyClient = new Lazy<TelemetryClient>(() =>
    {
        var config = TelemetryConfiguration.CreateDefault();
        config.ConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
        return new TelemetryClient(config);
    });
    
    public static TelemetryClient Client => LazyClient.Value;
    
    public static void TrackError(string message)
    {
        Client.TrackTrace(message, SeverityLevel.Error);
    }
}
```

### Scenario 3: Azure Functions

**2.x:**
```csharp
public class OrderFunction
{
    [FunctionName("ProcessOrder")]
    public async Task Run([QueueTrigger("orders")] Order order)
    {
        var telemetryClient = new TelemetryClient();
        telemetryClient.TrackEvent("OrderReceived");
        
        // Process order...
    }
}
```

**3.x:**
```csharp
public class OrderFunction
{
    private readonly TelemetryClient telemetryClient;
    
    public OrderFunction(TelemetryClient telemetryClient)
    {
        this.telemetryClient = telemetryClient;
    }
    
    [FunctionName("ProcessOrder")]
    public async Task Run([QueueTrigger("orders")] Order order)
    {
        telemetryClient.TrackEvent("OrderReceived");
        
        // Process order...
    }
}
```

## Configuration Registration Patterns

### ASP.NET Core / Worker Service

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Register Application Insights (registers TelemetryClient and TelemetryConfiguration)
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
});

var app = builder.Build();
```

### Console Application with DI

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ApplicationInsights.Extensibility;

class Program
{
    static void Main()
    {
        var services = new ServiceCollection();
        
        // Register Application Insights
        services.AddApplicationInsightsTelemetryWorkerService(options =>
        {
            options.ConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
        });
        
        var serviceProvider = services.BuildServiceProvider();
        var telemetryClient = serviceProvider.GetRequiredService<TelemetryClient>();
        
        telemetryClient.TrackEvent("ApplicationStarted");
        telemetryClient.Flush();
        Task.Delay(5000).Wait();
    }
}
```

## Migration Checklist

- [ ] Identify all `new TelemetryClient()` calls in your codebase
- [ ] For ASP.NET Core/Worker Service apps:
  - [ ] Add `services.AddApplicationInsightsTelemetry()` to Program.cs
  - [ ] Inject `TelemetryClient` via constructor parameters
  - [ ] Remove parameterless constructor calls
- [ ] For console apps without DI:
  - [ ] Create `TelemetryConfiguration` using `CreateDefault()`
  - [ ] Set `ConnectionString` property
  - [ ] Pass configuration to `TelemetryClient` constructor
  - [ ] Dispose configuration when application exits
- [ ] For libraries:
  - [ ] Accept `TelemetryClient` or `TelemetryConfiguration` as constructor parameter
  - [ ] Document that consumers must provide configuration
- [ ] Update unit tests to provide isolated configurations
- [ ] Remove any references to `TelemetryConfiguration.Active`

## See Also

- [Active-removed.md](../TelemetryConfiguration/Active-removed.md) - Why Active was removed
- [CreateDefault-to-DI.md](../TelemetryConfiguration/CreateDefault-to-DI.md) - Using CreateDefault and DI patterns
- [connection-string.md](../../azure-monitor-exporter/connection-string.md) - ConnectionString format and usage
