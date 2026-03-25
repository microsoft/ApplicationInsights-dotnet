# ITelemetryInitializer → BaseProcessor\<Activity\> Migration

## What Changed

`ITelemetryInitializer` is removed in 3.x. Convert to `BaseProcessor<Activity>` with `OnStart` for trace enrichment, or `BaseProcessor<LogRecord>` for log enrichment.

## Before (2.x)

```csharp
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

public class MyInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.GlobalProperties["environment"] = "production";
        telemetry.Context.Cloud.RoleName = "MyService";
    }
}

// Registration:
services.AddSingleton<ITelemetryInitializer, MyInitializer>();
```

## After (3.x)

```csharp
using System.Diagnostics;
using OpenTelemetry;

public class MyEnrichmentProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity data)
    {
        data.SetTag("deployment.environment", "production");
    }
}

// Registration:
builder.Services.ConfigureOpenTelemetryTracerProvider(tracing =>
    tracing.AddProcessor<MyEnrichmentProcessor>());
```

For cloud role name, `telemetry.Context.Cloud.RoleName` is still supported in 3.x. Alternatively, you can set it via OpenTelemetry resource configuration:
```csharp
// Option 1: TelemetryClient (still works in 3.x)
client.Context.Cloud.RoleName = "MyService";

// Option 2: OpenTelemetry Resource (DI)
builder.Services.ConfigureOpenTelemetryTracerProvider(tracing =>
    tracing.ConfigureResource(r => r.AddService("MyService")));

// Option 2: OpenTelemetry Resource (Non-DI)
config.ConfigureOpenTelemetryBuilder(otel =>
    otel.ConfigureResource(r => r.AddService("MyService")));
```

## Migrating TelemetryInitializerBase (HttpContext Access)

In 2.x ASP.NET Core apps, `TelemetryInitializerBase` provided access to `HttpContext` inside initializers. In 3.x, use `IHttpContextAccessor` injected into a `BaseProcessor<Activity>`:

**Before (2.x)**
```csharp
public sealed class MyInitializer : TelemetryInitializerBase
{
    protected override void OnInitializeTelemetry(HttpContext platformContext, RequestTelemetry requestTelemetry, ITelemetry telemetry)
    {
        telemetry.Context.GlobalProperties["user"] = platformContext.User?.Identity?.Name;
    }
}
```

**After (3.x)**
```csharp
public sealed class UserTelemetryEnrichment : BaseProcessor<Activity>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserTelemetryEnrichment(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override void OnEnd(Activity data)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return;
        }

        var userName = httpContext.User?.Identity?.Name;
        if (!string.IsNullOrEmpty(userName))
        {
            data.SetTag("enduser.id", userName);
        }

        if (httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantId))
        {
            data.SetTag("tenant.id", tenantId.ToString());
        }
    }
}

// Registration:
builder.Services.AddHttpContextAccessor();
builder.Services.ConfigureOpenTelemetryTracerProvider(tracing =>
    tracing.AddProcessor<UserTelemetryEnrichment>());
```

## Property Mapping

| 2.x Property | 3.x Equivalent |
|---|---|
| `telemetry.Context.GlobalProperties["key"]` | `data.SetTag("key", value)` |
| `telemetry.Properties["key"]` | `data.SetTag("key", value)` |
| `telemetry.Context.Cloud.RoleName` | Still works in 3.x. Also: `ConfigureResource(r => r.AddService("name"))` |
| `telemetry.Context.User.AuthenticatedUserId` | Still works in 3.x. Also: `data.SetTag("enduser.id", value)` |

## If Your Initializer Touches Both Traces and Logs

In 2.x, one `ITelemetryInitializer` handled all signal types. In 3.x, traces, logs, and metrics are separate pipelines. If the old initializer touched `TraceTelemetry` or `EventTelemetry` (logs), you need a second processor:

```csharp
public class LogEnrichmentProcessor : BaseProcessor<LogRecord>
{
    public override void OnEnd(LogRecord data)
    {
        // Example: add a custom attribute to all log records
        var attributes = new List<KeyValuePair<string, object?>>(data.Attributes ?? [])
        {
            new("deployment.environment", "production")
        };
        data.Attributes = attributes;
    }
}
```

## Non-DI Registration (Console / Classic ASP.NET)

For apps using `TelemetryConfiguration` directly:

```csharp
var config = TelemetryConfiguration.CreateDefault();
config.ConnectionString = "InstrumentationKey=...;IngestionEndpoint=...";
config.ConfigureOpenTelemetryBuilder(otel =>
    otel.WithTracing(t => t.AddProcessor<MyEnrichmentProcessor>()));
```

This replaces the 2.x pattern of `config.TelemetryInitializers.Add(new MyInitializer())`.
