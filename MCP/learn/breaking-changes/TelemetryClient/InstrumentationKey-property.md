# TelemetryClient.InstrumentationKey Property Removed

**Category:** Breaking Change  
**Applies to:** TelemetryClient API  
**Migration Effort:** Simple  
**Related:** [parameterless-constructor.md](parameterless-constructor.md), [connection-string.md](../../azure-monitor-exporter/connection-string.md)

## Change Summary

The `InstrumentationKey` property has been completely removed from `TelemetryClient` in 3.x. Configuration must now use `TelemetryConfiguration.ConnectionString` instead. Connection strings provide more flexibility by including both the instrumentation key and service endpoints.

## API Comparison

### 2.x API

```csharp
// Source: ApplicationInsights-dotnet-2x/BASE/src/Microsoft.ApplicationInsights/TelemetryClient.cs:80-86
public string InstrumentationKey
{
    get => this.Context.InstrumentationKey;

    [Obsolete("InstrumentationKey based global ingestion is being deprecated. Recommended to set TelemetryConfiguration.ConnectionString.")]
    set { this.Context.InstrumentationKey = value; }
}
```

### 3.x API

```csharp
// Source: ApplicationInsights-dotnet/BASE/src/Microsoft.ApplicationInsights/TelemetryClient.cs
// REMOVED: InstrumentationKey property does not exist

// Use TelemetryConfiguration.ConnectionString instead
public sealed class TelemetryClient
{
    // No InstrumentationKey property
    public TelemetryConfiguration TelemetryConfiguration { get; }
}
```

## Why It Changed

| Issue | Description |
|-------|-------------|
| **Limited Configuration** | InstrumentationKey alone couldn't specify custom endpoints (sovereign clouds, proxies) |
| **Connection String Standard** | Industry standard format that includes key + endpoints in one string |
| **Simplified Configuration** | Single property instead of multiple (InstrumentationKey, EndpointAddress, etc.) |
| **Cloud Compatibility** | Easier support for Azure Government, Azure China, custom endpoints |

## Migration Strategies

### Option 1: ASP.NET Core (ApplicationInsightsServiceOptions)

**When to use:** ASP.NET Core, Worker Service applications.

**2.x:**
```csharp
// Startup.cs or Program.cs
services.AddApplicationInsightsTelemetry(options =>
{
    options.InstrumentationKey = "abc123-def456-789ghi-012jkl-345mno";
});

// Or in appsettings.json
{
  "ApplicationInsights": {
    "InstrumentationKey": "abc123-def456-789ghi-012jkl-345mno"
  }
}
```

**3.x:**
```csharp
// Program.cs
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = "InstrumentationKey=abc123-def456-789ghi-012jkl-345mno;IngestionEndpoint=https://westus2-2.in.applicationinsights.azure.com/;LiveEndpoint=https://westus2.livediagnostics.monitor.azure.com/";
});

// Or in appsettings.json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=abc123-def456-789ghi-012jkl-345mno;IngestionEndpoint=https://westus2-2.in.applicationinsights.azure.com/"
  }
}
```

### Option 2: Console Application

**When to use:** Console apps, background services.

**2.x:**
```csharp
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

class Program
{
    static void Main()
    {
        var config = TelemetryConfiguration.CreateDefault();
        config.InstrumentationKey = "abc123-def456-789ghi-012jkl-345mno";
        
        var telemetryClient = new TelemetryClient(config);
        telemetryClient.TrackEvent("ApplicationStarted");
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
        var config = TelemetryConfiguration.CreateDefault();
        config.ConnectionString = "InstrumentationKey=abc123-def456-789ghi-012jkl-345mno;IngestionEndpoint=https://westus2-2.in.applicationinsights.azure.com/";
        
        var telemetryClient = new TelemetryClient(config);
        telemetryClient.TrackEvent("ApplicationStarted");
    }
}
```

### Option 3: Direct TelemetryClient Property Access (No Longer Possible)

**2.x:**
```csharp
var telemetryClient = new TelemetryClient();
telemetryClient.InstrumentationKey = "abc123-def456-789ghi-012jkl-345mno";
telemetryClient.TrackEvent("UserAction");
```

**3.x:**
```csharp
// Configuration must be set on TelemetryConfiguration, not TelemetryClient
var config = new TelemetryConfiguration
{
    ConnectionString = "InstrumentationKey=abc123-def456-789ghi-012jkl-345mno;IngestionEndpoint=https://westus2-2.in.applicationinsights.azure.com/"
};

var telemetryClient = new TelemetryClient(config);
telemetryClient.TrackEvent("UserAction");
```

## Connection String Format

### Minimal Connection String (Public Azure)

```csharp
// Just instrumentation key - endpoints auto-discovered
config.ConnectionString = "InstrumentationKey=abc123-def456-789ghi-012jkl-345mno";
```

### Full Connection String (Custom Endpoints)

```csharp
// Explicit endpoints for sovereign clouds or custom proxies
config.ConnectionString = "InstrumentationKey=abc123-def456-789ghi-012jkl-345mno;" +
                         "IngestionEndpoint=https://westus2-2.in.applicationinsights.azure.com/;" +
                         "LiveEndpoint=https://westus2.livediagnostics.monitor.azure.com/;" +
                         "ProfilerEndpoint=https://westus2-2.agent.azureserviceprofiler.net/;" +
                         "SnapshotEndpoint=https://westus2-2.snapshot.monitor.azure.com/";
```

### Azure Government Cloud

```csharp
config.ConnectionString = "InstrumentationKey=abc123-def456-789ghi-012jkl-345mno;" +
                         "IngestionEndpoint=https://usgovvirginia-2.in.applicationinsights.us/";
```

### Azure China Cloud

```csharp
config.ConnectionString = "InstrumentationKey=abc123-def456-789ghi-012jkl-345mno;" +
                         "IngestionEndpoint=https://chinaeast2-2.in.applicationinsights.azure.cn/";
```

## Common Scenarios

### Scenario 1: Environment-Based Configuration

**2.x:**
```csharp
public void ConfigureServices(IServiceCollection services)
{
    var instrumentationKey = Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"];
    
    services.AddApplicationInsightsTelemetry(options =>
    {
        options.InstrumentationKey = instrumentationKey;
    });
}
```

**3.x:**
```csharp
public void ConfigureServices(IServiceCollection services)
{
    var connectionString = Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
    
    services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = connectionString;
    });
}
```

### Scenario 2: Multiple Telemetry Clients

**2.x:**
```csharp
// Different instrumentation keys for different components
var config1 = new TelemetryConfiguration { InstrumentationKey = "key1" };
var config2 = new TelemetryConfiguration { InstrumentationKey = "key2" };

var client1 = new TelemetryClient(config1);
var client2 = new TelemetryClient(config2);
```

**3.x:**
```csharp
// Different connection strings for different components
var config1 = new TelemetryConfiguration 
{ 
    ConnectionString = "InstrumentationKey=key1;IngestionEndpoint=https://..." 
};
var config2 = new TelemetryConfiguration 
{ 
    ConnectionString = "InstrumentationKey=key2;IngestionEndpoint=https://..." 
};

var client1 = new TelemetryClient(config1);
var client2 = new TelemetryClient(config2);
```

### Scenario 3: Azure Functions

**2.x (appsettings.json):**
```json
{
  "Values": {
    "APPINSIGHTS_INSTRUMENTATIONKEY": "abc123-def456-789ghi-012jkl-345mno"
  }
}
```

**3.x (local.settings.json):**
```json
{
  "Values": {
    "APPLICATIONINSIGHTS_CONNECTION_STRING": "InstrumentationKey=abc123-def456-789ghi-012jkl-345mno;IngestionEndpoint=https://westus2-2.in.applicationinsights.azure.com/"
  }
}
```

### Scenario 4: Web.config / ApplicationInsights.config

**2.x (ApplicationInsights.config):**
```xml
<ApplicationInsights xmlns="http://schemas.microsoft.com/ApplicationInsights/2013/Settings">
  <InstrumentationKey>abc123-def456-789ghi-012jkl-345mno</InstrumentationKey>
</ApplicationInsights>
```

**3.x (Code-based configuration):**
```csharp
// ApplicationInsights.config no longer used in 3.x
// Configure in Global.asax.cs or Startup.cs
protected void Application_Start()
{
    var config = TelemetryConfiguration.CreateDefault();
    config.ConnectionString = ConfigurationManager.AppSettings["APPLICATIONINSIGHTS_CONNECTION_STRING"];
    
    var client = new TelemetryClient(config);
}
```

## Converting InstrumentationKey to ConnectionString

### Basic Conversion

```csharp
// 2.x
string instrumentationKey = "abc123-def456-789ghi-012jkl-345mno";

// 3.x - Minimal conversion (public Azure)
string connectionString = $"InstrumentationKey={instrumentationKey}";
```

### Extracting InstrumentationKey from ConnectionString

If you need the key for logging/debugging:

```csharp
// 3.x
var config = TelemetryConfiguration.CreateDefault();
config.ConnectionString = "InstrumentationKey=abc123-def456-789ghi-012jkl-345mno;IngestionEndpoint=https://...";

// Extract just the key portion
var parts = config.ConnectionString.Split(';');
var keyPart = parts.FirstOrDefault(p => p.StartsWith("InstrumentationKey="));
var instrumentationKey = keyPart?.Substring("InstrumentationKey=".Length);
// Result: "abc123-def456-789ghi-012jkl-345mno"
```

## Environment Variable Changes

### 2.x Environment Variables

```bash
# Windows
set APPINSIGHTS_INSTRUMENTATIONKEY=abc123-def456-789ghi-012jkl-345mno

# Linux/macOS
export APPINSIGHTS_INSTRUMENTATIONKEY=abc123-def456-789ghi-012jkl-345mno
```

### 3.x Environment Variables

```bash
# Windows
set APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=abc123-def456-789ghi-012jkl-345mno;IngestionEndpoint=https://westus2-2.in.applicationinsights.azure.com/

# Linux/macOS
export APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=abc123-def456-789ghi-012jkl-345mno;IngestionEndpoint=https://westus2-2.in.applicationinsights.azure.com/"
```

## Azure Portal - Getting Connection String

1. Navigate to your Application Insights resource in Azure Portal
2. Go to **Overview** or **Properties**
3. Copy the **Connection String** (not just the Instrumentation Key)
4. Full connection string includes all necessary endpoints

**Example from Portal:**
```
InstrumentationKey=abc123-def456-789ghi-012jkl-345mno;IngestionEndpoint=https://westus2-2.in.applicationinsights.azure.com/;LiveEndpoint=https://westus2.livediagnostics.monitor.azure.com/
```

## Migration Checklist

- [ ] Locate all `InstrumentationKey` property assignments
- [ ] Replace with `ConnectionString` on `TelemetryConfiguration`
- [ ] Update configuration files:
  - [ ] appsettings.json: `InstrumentationKey` → `ConnectionString`
  - [ ] Environment variables: `APPINSIGHTS_INSTRUMENTATIONKEY` → `APPLICATIONINSIGHTS_CONNECTION_STRING`
  - [ ] Azure Portal app settings
  - [ ] local.settings.json (Azure Functions)
- [ ] Get connection strings from Azure Portal (not just instrumentation keys)
- [ ] For sovereign clouds: Verify `IngestionEndpoint` is included
- [ ] Remove any `ApplicationInsights.config` files (no longer used in 3.x)
- [ ] Update CI/CD pipelines and deployment scripts
- [ ] Test with actual connection string to verify telemetry flows correctly

## See Also

- [connection-string.md](../../azure-monitor-exporter/connection-string.md) - Connection string format and endpoints
- [parameterless-constructor.md](parameterless-constructor.md) - TelemetryClient constructor changes
- [Active-removed.md](../TelemetryConfiguration/Active-removed.md) - TelemetryConfiguration.Active removal
