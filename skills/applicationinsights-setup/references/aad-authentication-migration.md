# AAD Authentication Migration (2.x → 3.x)

## What Changed

In 2.x, `SetAzureTokenCredential(object)` existed on `TelemetryConfiguration` but accepted an `object` parameter (used reflection internally). Some teams used `IConfigureOptions<TelemetryConfiguration>` workarounds for DI scenarios.

In 3.x:
- `TelemetryConfiguration.SetAzureTokenCredential(TokenCredential)` — signature changed from `object` to strongly typed `TokenCredential`
- `ApplicationInsightsServiceOptions.Credential` — **New**. Preferred for DI scenarios. Set `TokenCredential` directly in options.
- `TelemetryConfiguration.Active` — **Removed**. Use `TelemetryConfiguration.CreateDefault()`.

## DI Scenario (ASP.NET Core / Worker Service)

### Before (2.x)

```csharp
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Options;
using Azure.Identity;

public class TelemetryConfigurationEnricher : IConfigureOptions<TelemetryConfiguration>
{
    public void Configure(TelemetryConfiguration options)
    {
        object credential = new DefaultAzureCredential();
        options.SetAzureTokenCredential(credential); // object param
    }
}

// Registration:
services.AddSingleton<IConfigureOptions<TelemetryConfiguration>, TelemetryConfigurationEnricher>();
services.AddApplicationInsightsTelemetry();
```

### After (3.x)

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Azure.Identity;

builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = "InstrumentationKey=...;IngestionEndpoint=...";
    options.Credential = new DefaultAzureCredential();
});
```

### Migration Steps (DI)

1. **Remove** the `IConfigureOptions<TelemetryConfiguration>` class that calls `SetAzureTokenCredential()`. Delete the entire class file if it only handled AAD auth.
2. **Remove** the DI registration: `services.AddSingleton<IConfigureOptions<TelemetryConfiguration>, ...>();`
3. **Set** `options.Credential` in your `AddApplicationInsightsTelemetry()` or `AddApplicationInsightsTelemetryWorkerService()` options:
   ```csharp
   options.Credential = new DefaultAzureCredential();
   ```

## Non-DI Scenario (Console / Classic ASP.NET)

### Before (2.x)

```csharp
var config = TelemetryConfiguration.Active; // Removed in 3.x
config.ConnectionString = "InstrumentationKey=...;IngestionEndpoint=...";
config.SetAzureTokenCredential((object)new DefaultAzureCredential()); // object cast
var client = new TelemetryClient(config);
```

### After (3.x)

```csharp
var config = TelemetryConfiguration.CreateDefault();
config.ConnectionString = "InstrumentationKey=...;IngestionEndpoint=...";
config.SetAzureTokenCredential(new DefaultAzureCredential()); // Strongly typed
var client = new TelemetryClient(config);
```

### Migration Steps (Non-DI)

1. **Replace** `TelemetryConfiguration.Active` with `TelemetryConfiguration.CreateDefault()`
2. **Remove** the `(object)` cast from `SetAzureTokenCredential()` — parameter is now `TokenCredential`
3. Call `SetAzureTokenCredential()` **before** creating a `TelemetryClient` from that configuration

## Notes

- `Azure.Identity` package is still required
- For custom credentials: `new ManagedIdentityCredential("your-client-id")` works directly
- Both DI and non-DI paths ultimately set `AzureMonitorExporterOptions.Credential` under the hood
