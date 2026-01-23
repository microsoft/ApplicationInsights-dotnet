---
title: Fresh ASP.NET Core Onboarding (No Migration)
category: scenario
applies-to: 3.x
related:
  - azure-monitor-exporter/connection-string.md
  - examples/configuration/basic-setup-aspnetcore.md
source: Azure.Monitor.OpenTelemetry.AspNetCore
---

# Fresh ASP.NET Core Onboarding (No Migration)

**Category:** Common Scenario  
**Applies to:** 3.x (OpenTelemetry-based)  
**Related:** Connection String Configuration, Basic ASP.NET Core Setup

## Overview

This guide covers **fresh onboarding** of Azure Monitor for ASP.NET Core applications that have **never used Application Insights SDK**. This is NOT a migration scenario - use this when starting with OpenTelemetry from scratch.

## When to Use This Guide

✅ **Use this guide if:**
- ASP.NET Core application (.NET 6+)
- No `Microsoft.ApplicationInsights.*` packages in project
- Starting fresh with Azure Monitor observability
- No legacy telemetry code to migrate

❌ **Don't use this guide if:**
- Application has existing `Microsoft.ApplicationInsights.*` packages → Use [migration guide](../breaking-changes/README.md)
- Non-ASP.NET Core app (console, worker service) → Use Azure.Monitor.OpenTelemetry.Exporter directly

## Package to Use

**Install:** `Azure.Monitor.OpenTelemetry.AspNetCore`

**Do NOT install:**
- ❌ `Microsoft.ApplicationInsights.AspNetCore` (legacy 2.x SDK)
- ❌ `Microsoft.ApplicationInsights` (legacy SDK)
- ❌ `Microsoft.ApplicationInsights.WorkerService` (legacy SDK)

## Installation

```bash
dotnet add package Azure.Monitor.OpenTelemetry.AspNetCore
```

Or via Package Manager:

```powershell
Install-Package Azure.Monitor.OpenTelemetry.AspNetCore
```

## Basic Setup

### Step 1: Get Connection String

Get your Application Insights connection string from Azure Portal:

1. Navigate to your Application Insights resource
2. Go to **Overview** → Copy **Connection String**
3. Format: `InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://...`

### Step 2: Set Environment Variable

Set the connection string as an environment variable:

**Windows (PowerShell):**
```powershell
$env:APPLICATIONINSIGHTS_CONNECTION_STRING = "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://..."
```

**Linux/macOS:**
```bash
export APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://..."
```

**Or in `launchSettings.json`:**
```json
{
  "profiles": {
    "MyApp": {
      "commandName": "Project",
      "environmentVariables": {
        "APPLICATIONINSIGHTS_CONNECTION_STRING": "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://..."
      }
    }
  }
}
```

**Or in `appsettings.json`:**
```json
{
  "AzureMonitor": {
    "ConnectionString": "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://..."
  }
}
```

### Step 3: Add to Program.cs

**Minimal Setup (reads from environment variable):**

```csharp
using OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

// Enable Azure Monitor with OpenTelemetry
builder.Services.AddOpenTelemetry().UseAzureMonitor();

// Add your services
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();
app.Run();
```

**With Connection String in Code:**

```csharp
using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

// Enable Azure Monitor with connection string
builder.Services.AddOpenTelemetry().UseAzureMonitor(options =>
{
    options.ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://...";
});

builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
```

**With Connection String from Configuration:**

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry().UseAzureMonitor(options =>
{
    options.ConnectionString = builder.Configuration["AzureMonitor:ConnectionString"];
});

builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
```

## What Gets Automatically Instrumented

The `UseAzureMonitor()` call automatically enables:

### ✅ Traces (Distributed Tracing)
- **Incoming HTTP requests** to your ASP.NET Core app (requests table)
- **Outgoing HTTP calls** via HttpClient (dependencies table)
- **SQL queries** via Microsoft.Data.SqlClient or System.Data.SqlClient (dependencies table)
- **W3C Trace Context** propagation to downstream services

### ✅ Metrics
- **Application Insights Standard Metrics**
- **HTTP request metrics** (request rate, duration, failures)
- **HTTP dependency metrics** (dependency call duration, failures)
- **.NET Runtime metrics** (GC, thread pool, exceptions)

### ✅ Logs
- **ILogger logs** from Microsoft.Extensions.Logging
- **Azure SDK logs** (as subset of ILogger)
- Logs are correlated with traces (same operation ID)

### ✅ Resource Detection
- **Azure App Service** (adds cloud role name, instance)
- **Azure Virtual Machines** (adds VM resource attributes)
- **Azure Container Apps** (adds container resource attributes)

### ✅ Live Metrics
- Real-time monitoring in Azure Portal
- Live request/dependency rates
- Live performance counters

## Verify It's Working

### 1. Run Your Application

```bash
dotnet run
```

### 2. Generate Some Traffic

```bash
curl https://localhost:5001/api/values
```

### 3. Check Azure Portal

- Go to **Application Insights** resource
- Navigate to **Live Metrics** (should see data immediately)
- Check **Transaction Search** (traces appear within 1-2 minutes)
- Check **Performance** blade (requests and dependencies)
- Check **Failures** blade (exceptions and failed requests)

## Advanced Configuration

### Sampling

By default, 100% of telemetry is sampled. To reduce volume:

```csharp
builder.Services.AddOpenTelemetry().UseAzureMonitor(options =>
{
    options.SamplingRatio = 0.5F; // 50% sampling
});
```

### Add Custom ActivitySource

To instrument your own code with custom traces:

```csharp
// In Program.cs
builder.Services.AddOpenTelemetry().UseAzureMonitor();

builder.Services.ConfigureOpenTelemetryTracerProvider((sp, builder) => 
{
    builder.AddSource("MyCompany.MyProduct.MyLibrary");
});

// In your service code
public class OrderService
{
    private static readonly ActivitySource ActivitySource = new("MyCompany.MyProduct.MyLibrary");
    
    public async Task ProcessOrderAsync(Order order)
    {
        using var activity = ActivitySource.StartActivity("ProcessOrder");
        activity?.SetTag("order.id", order.Id);
        activity?.SetTag("order.amount", order.Amount);
        
        // Your business logic
        await SaveOrderAsync(order);
    }
}
```

### Add Custom Processors

To enrich or filter telemetry:

```csharp
builder.Services.AddOpenTelemetry().UseAzureMonitor();

builder.Services.ConfigureOpenTelemetryTracerProvider((sp, builder) =>
{
    builder.AddProcessor(new MyCustomProcessor());
});

public class MyCustomProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        // Enrich activity when it starts
        activity.SetTag("custom.property", "value");
    }
}
```

### Authentication with Azure Active Directory

Instead of using Instrumentation Key, authenticate with AAD:

```csharp
using Azure.Identity;

builder.Services.AddOpenTelemetry().UseAzureMonitor(options =>
{
    options.ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://...";
    options.Credential = new DefaultAzureCredential();
});
```

## Common Issues

### Issue 1: No Telemetry Appearing

**Check:**
1. `APPLICATIONINSIGHTS_CONNECTION_STRING` environment variable is set
2. Connection string format is correct (includes `InstrumentationKey=` and `IngestionEndpoint=`)
3. Application is generating traffic (requests, logs)
4. Wait 1-2 minutes for data to appear (not instant)

### Issue 2: Missing Dependencies

**Make sure:**
- `Azure.Monitor.OpenTelemetry.AspNetCore` package is installed
- Using `OpenTelemetry` namespace (not `Microsoft.ApplicationInsights`)
- Called `UseAzureMonitor()` before building the app

### Issue 3: Duplicate Telemetry

**Avoid:**
- DO NOT mix `Microsoft.ApplicationInsights.*` packages with `Azure.Monitor.OpenTelemetry.*`
- DO NOT call `AddApplicationInsightsTelemetry()` (legacy method) alongside `UseAzureMonitor()`

## Project File Example

Your `.csproj` should look like:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <!-- ✅ Correct package for fresh onboarding -->
    <PackageReference Include="Azure.Monitor.OpenTelemetry.AspNetCore" Version="1.2.0" />
    
    <!-- ❌ Do NOT include these legacy packages -->
    <!-- <PackageReference Include="Microsoft.ApplicationInsights.AspNetCore" Version="2.22.0" /> -->
    <!-- <PackageReference Include="Microsoft.ApplicationInsights" Version="2.22.0" /> -->
  </ItemGroup>
</Project>
```

## Migration Checklist

If you accidentally installed legacy packages:

- [ ] Remove `Microsoft.ApplicationInsights.*` packages from `.csproj`
- [ ] Remove `AddApplicationInsightsTelemetry()` calls from code
- [ ] Remove `applicationinsights.config` file (if present)
- [ ] Install `Azure.Monitor.OpenTelemetry.AspNetCore`
- [ ] Add `UseAzureMonitor()` to Program.cs
- [ ] Set `APPLICATIONINSIGHTS_CONNECTION_STRING` environment variable
- [ ] Remove legacy ITelemetryInitializer/ITelemetryProcessor implementations
- [ ] Migrate to BaseProcessor<Activity> if needed

## Complete Working Example

**Program.cs:**

```csharp
using OpenTelemetry;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Enable Azure Monitor (reads APPLICATIONINSIGHTS_CONNECTION_STRING env var)
builder.Services.AddOpenTelemetry().UseAzureMonitor();

// Optional: Add custom instrumentation
builder.Services.ConfigureOpenTelemetryTracerProvider((sp, builder) =>
{
    builder.AddSource("MyApp");
});

builder.Services.AddControllers();
builder.Services.AddSingleton<OrderService>();

var app = builder.Build();

app.MapControllers();
app.MapGet("/orders/{id}", async (int id, OrderService orderService) =>
{
    var order = await orderService.GetOrderAsync(id);
    return Results.Ok(order);
});

app.Run();

// Custom service with instrumentation
public class OrderService
{
    private static readonly ActivitySource ActivitySource = new("MyApp");
    
    public async Task<Order> GetOrderAsync(int id)
    {
        using var activity = ActivitySource.StartActivity("GetOrder");
        activity?.SetTag("order.id", id);
        
        // Simulate database call (automatically instrumented if using SqlClient)
        await Task.Delay(50);
        
        return new Order { Id = id, Amount = 99.99m };
    }
}

public record Order(int Id, decimal Amount);
```

## See Also

- [Connection String Configuration](../azure-monitor-exporter/connection-string.md) - Detailed connection string format
- [Activity Processors](../concepts/activity-processor.md) - Custom telemetry enrichment
- [Sampling Telemetry](sampling-telemetry.md) - Configure sampling rates
- [Custom ActivitySource](../opentelemetry-fundamentals/activity-source.md) - Instrument your own code

## References

- **Azure Monitor OpenTelemetry AspNetCore Package:** https://www.nuget.org/packages/Azure.Monitor.OpenTelemetry.AspNetCore
- **Azure Monitor Documentation:** https://learn.microsoft.com/azure/azure-monitor/app/opentelemetry-enable?tabs=aspnetcore
- **OpenTelemetry .NET:** https://github.com/open-telemetry/opentelemetry-dotnet
