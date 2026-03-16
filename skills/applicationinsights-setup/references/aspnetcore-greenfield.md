# ASP.NET Core — Greenfield Setup

## Overview

Add Application Insights telemetry to a new ASP.NET Core application using `Microsoft.ApplicationInsights.AspNetCore` 3.x.

## Step 1: Add Package

```bash
dotnet add package Microsoft.ApplicationInsights.AspNetCore
```

Or in `.csproj`:

```xml
<PackageReference Include="Microsoft.ApplicationInsights.AspNetCore" Version="3.*" />
```

## Step 2: Configure in Program.cs

### Minimal Setup

```csharp
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add Application Insights telemetry
builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();
app.Run();
```

### With Configuration Options

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];

    // Rate-limited sampling (default is 5 traces/sec)
    options.TracesPerSecond = 10.0;
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
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=xxx;IngestionEndpoint=https://..."
  }
}
```

### Option C: User Secrets (Development)

```bash
dotnet user-secrets set "ApplicationInsights:ConnectionString" "InstrumentationKey=xxx;..."
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
