# ASP.NET Core — Greenfield Setup

## Overview

Add Azure Monitor / Application Insights to a new ASP.NET Core application using the Azure Monitor OpenTelemetry Distro.

## Step 1: Add Package

```bash
dotnet add package Azure.Monitor.OpenTelemetry.AspNetCore
```

Or in `.csproj`:

```xml
<PackageReference Include="Azure.Monitor.OpenTelemetry.AspNetCore" Version="1.3.0" />
```

## Step 2: Configure in Program.cs

### Minimal Setup

```csharp
using Azure.Monitor.OpenTelemetry.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add Azure Monitor — one line
builder.Services.AddOpenTelemetry().UseAzureMonitor();

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();
app.Run();
```

### With Configuration Options

```csharp
using Azure.Monitor.OpenTelemetry.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry().UseAzureMonitor(options =>
{
    options.ConnectionString = builder.Configuration["AzureMonitor:ConnectionString"];

    // Sample 50% of requests in production
    if (!builder.Environment.IsDevelopment())
    {
        options.SamplingRatio = 0.5f;
    }
});

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();
app.Run();
```

## Step 3: Configure Connection String

### Option A: Environment Variable (Recommended for Production)

```bash
export APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=xxx;IngestionEndpoint=https://..."
```

### Option B: appsettings.json

```json
{
  "AzureMonitor": {
    "ConnectionString": "InstrumentationKey=xxx;IngestionEndpoint=https://..."
  }
}
```

### Option C: User Secrets (Development)

```bash
dotnet user-secrets set "AzureMonitor:ConnectionString" "InstrumentationKey=xxx;..."
```

## What You Get Automatically

| Signal | Data |
|---|---|
| **Requests** | All incoming HTTP requests with timing, status codes |
| **Dependencies** | Outgoing HTTP calls, SQL queries, Azure SDK calls |
| **Exceptions** | Unhandled exceptions with stack traces |
| **Logs** | ILogger output (Information level and above) |
| **Metrics** | Request rate, response time, CPU, memory |

## Verify It Works

1. Run your application
2. Make some requests
3. Check Application Insights in Azure Portal (may take 2-5 minutes)
4. Look for:
   - Live Metrics (immediate)
   - Transaction Search (requests, dependencies)
   - Failures (exceptions)
