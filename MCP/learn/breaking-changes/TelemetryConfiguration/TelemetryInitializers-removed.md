# TelemetryConfiguration.TelemetryInitializers Removed

**Category:** Breaking Change  
**Applies to:** TelemetryConfiguration API  
**Migration Effort:** Medium  
**Related:** [BaseProcessor-OnStart.md](../../api-reference/BaseProcessor/OnStart.md), [Activity-SetTag.md](../../api-reference/Activity/SetTag.md), [activity-processor.md](../../concepts/activity-processor.md)

## Change Summary

The `TelemetryInitializers` collection has been removed from `TelemetryConfiguration` in 3.x. The `ITelemetryInitializer` pattern no longer exists. Use OpenTelemetry `BaseProcessor<Activity>` with the `OnStart()` method to enrich telemetry with custom properties and context.

## API Comparison

### 2.x API

```csharp
// Source: ApplicationInsights-dotnet-2x/BASE/src/Microsoft.ApplicationInsights/Extensibility/ITelemetryInitializer.cs:13-19
public interface ITelemetryInitializer
{
    void Initialize(ITelemetry telemetry);
}

// TelemetryConfiguration usage
public class TelemetryConfiguration
{
    public IList<ITelemetryInitializer> TelemetryInitializers { get; }
}
```

### 3.x API

```csharp
// REMOVED: ITelemetryInitializer interface does not exist
// REMOVED: TelemetryInitializers collection does not exist

// Replacement: BaseProcessor<Activity> with OnStart
using OpenTelemetry;

public class MyProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        // Enrich activity with custom properties
        activity?.SetTag("custom.property", "value");
    }
}
```

## Why It Changed

| Reason | Description |
|--------|-------------|
| **OpenTelemetry Standard** | OpenTelemetry uses processors, not initializers |
| **Activity-Based Model** | Enrichment happens on Activity objects, not ITelemetry |
| **OnStart vs OnEnd** | Clear separation: OnStart for enrichment, OnEnd for filtering |
| **Better Performance** | Activity processors are more efficient than telemetry item iteration |

## Migration Strategies

### Option 1: Simple Property Enrichment

**When to use:** Adding custom properties/tags to all telemetry.

**2.x:**
```csharp
// Source: Example pattern from ApplicationInsightsDemo
public class CustomPropertiesInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.GlobalProperties["Environment"] = "Production";
        telemetry.Context.GlobalProperties["Version"] = "1.0.0";
        telemetry.Context.Cloud.RoleName = "MyService";
    }
}

// Registration
public void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton<ITelemetryInitializer, CustomPropertiesInitializer>();
}
```

**3.x:**
```csharp
using System.Diagnostics;
using OpenTelemetry;

public class CustomPropertiesProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        // Add custom properties as Activity tags
        activity?.SetTag("environment", "Production");
        activity?.SetTag("version", "1.0.0");
        activity?.SetTag("service.name", "MyService");
    }
}

// Registration in Program.cs
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.ConfigureOpenTelemetryTracerProvider(builder =>
{
    builder.AddProcessor<CustomPropertiesProcessor>();
});
```

### Option 2: Request-Specific Enrichment

**When to use:** Adding properties based on request context (HTTP headers, user info).

**2.x:**
```csharp
public class HttpContextInitializer : ITelemetryInitializer
{
    private readonly IHttpContextAccessor httpContextAccessor;
    
    public HttpContextInitializer(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }
    
    public void Initialize(ITelemetry telemetry)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            telemetry.Context.User.Id = httpContext.User?.Identity?.Name;
            telemetry.Context.Session.Id = httpContext.Session?.Id;
            
            if (telemetry is RequestTelemetry requestTelemetry)
            {
                requestTelemetry.Properties["ClientIP"] = httpContext.Connection.RemoteIpAddress?.ToString();
                requestTelemetry.Properties["UserAgent"] = httpContext.Request.Headers["User-Agent"].ToString();
            }
        }
    }
}
```

**3.x:**
```csharp
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using OpenTelemetry;

public class HttpContextProcessor : BaseProcessor<Activity>
{
    private readonly IHttpContextAccessor httpContextAccessor;
    
    public HttpContextProcessor(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }
    
    public override void OnStart(Activity activity)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext != null && activity != null)
        {
            // User information
            if (httpContext.User?.Identity?.IsAuthenticated == true)
            {
                activity.SetTag("enduser.id", httpContext.User.Identity.Name);
            }
            
            // HTTP-specific tags
            if (activity.Kind == ActivityKind.Server)
            {
                activity.SetTag("client.address", httpContext.Connection.RemoteIpAddress?.ToString());
                activity.SetTag("user_agent.original", httpContext.Request.Headers["User-Agent"].ToString());
                activity.SetTag("http.request.header.x-forwarded-for", httpContext.Request.Headers["X-Forwarded-For"].ToString());
            }
        }
    }
}

// Registration with DI
builder.Services.AddHttpContextAccessor();
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.ConfigureOpenTelemetryTracerProvider(builder =>
{
    builder.AddProcessor<HttpContextProcessor>();
});
```

### Option 3: Conditional Enrichment

**When to use:** Adding properties based on telemetry type or conditions.

**2.x:**
```csharp
// Source: ApplicationInsightsDemo/ClientErrorTelemetryInitializer.cs pattern
public class ClientErrorInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        if (telemetry is RequestTelemetry requestTelemetry && 
            int.TryParse(requestTelemetry.ResponseCode, out int code))
        {
            if (code >= 400 && code < 500)
            {
                requestTelemetry.Success = true;
                requestTelemetry.Properties["Overridden400s"] = "true";
            }
        }
    }
}
```

**3.x:**
```csharp
using System.Diagnostics;
using OpenTelemetry;

public class ClientErrorProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)  // Use OnEnd to see final status
    {
        if (activity?.Kind == ActivityKind.Server)
        {
            var statusCode = activity.GetTagItem("http.response.status_code");
            if (statusCode != null && int.TryParse(statusCode.ToString(), out int code))
            {
                if (code >= 400 && code < 500)
                {
                    // Mark as success despite 4xx status
                    activity.SetStatus(ActivityStatusCode.Ok);
                    activity.SetTag("overridden_4xx", true);
                }
            }
        }
    }
}
```

## Common Scenarios

### Scenario 1: Cloud Role Name

**2.x:**
```csharp
public class CloudRoleInitializer : ITelemetryInitializer
{
    private readonly string roleName;
    private readonly string roleInstance;
    
    public CloudRoleInitializer(string roleName, string roleInstance)
    {
        this.roleName = roleName;
        this.roleInstance = roleInstance;
    }
    
    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.Cloud.RoleName = roleName;
        telemetry.Context.Cloud.RoleInstance = roleInstance;
    }
}
```

**3.x:**
```csharp
// Use Resource attributes instead of processor
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.ConfigureOpenTelemetryTracerProvider(builder =>
{
    builder.ConfigureResource(resource => resource
        .AddService(
            serviceName: "MyService",
            serviceVersion: "1.0.0",
            serviceInstanceId: Environment.MachineName));
});
```

### Scenario 2: Component Version

**2.x:**
```csharp
public class ComponentVersionInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.Component.Version = Assembly.GetExecutingAssembly()
            .GetName().Version.ToString();
    }
}
```

**3.x:**
```csharp
// Use Resource attribute
builder.Services.ConfigureOpenTelemetryTracerProvider(builder =>
{
    builder.ConfigureResource(resource => resource
        .AddAttributes(new Dictionary<string, object>
        {
            ["service.version"] = Assembly.GetExecutingAssembly().GetName().Version.ToString()
        }));
});
```

### Scenario 3: Correlation Context

**2.x:**
```csharp
public class CorrelationInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        if (CallContext.LogicalGetData("CorrelationId") is string correlationId)
        {
            telemetry.Context.Operation.Id = correlationId;
        }
    }
}
```

**3.x:**
```csharp
// Activity context is automatic via W3C Trace Context
// For custom baggage:
public class CorrelationProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        // Read from Activity.Current.Baggage (distributed context)
        var correlationId = activity?.GetBaggageItem("CorrelationId");
        if (!string.IsNullOrEmpty(correlationId))
        {
            activity?.SetTag("correlation.id", correlationId);
        }
    }
}
```

### Scenario 4: Telemetry Type Filtering

**2.x:**
```csharp
public class TypeSpecificInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        switch (telemetry)
        {
            case RequestTelemetry request:
                request.Properties["TelemetryType"] = "Request";
                break;
            case DependencyTelemetry dependency:
                dependency.Properties["TelemetryType"] = "Dependency";
                break;
            case TraceTelemetry trace:
                trace.Properties["TelemetryType"] = "Trace";
                break;
        }
    }
}
```

**3.x:**
```csharp
using System.Diagnostics;
using OpenTelemetry;

public class ActivityKindProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        // ActivityKind already distinguishes types
        activity?.SetTag("activity.kind", activity.Kind.ToString());
        
        // Add semantic information based on kind
        switch (activity?.Kind)
        {
            case ActivityKind.Server:
                activity.SetTag("telemetry.type", "Request");
                break;
            case ActivityKind.Client:
                activity.SetTag("telemetry.type", "Dependency");
                break;
            case ActivityKind.Internal:
                activity.SetTag("telemetry.type", "Internal");
                break;
        }
    }
}
```

## Migration Checklist

- [ ] Identify all `ITelemetryInitializer` implementations
- [ ] For each initializer, determine its purpose:
  - [ ] Property enrichment → `BaseProcessor<Activity>.OnStart()`
  - [ ] Cloud role/version → Resource configuration
  - [ ] Correlation → Activity baggage or automatic W3C propagation
- [ ] Create `BaseProcessor<Activity>` classes:
  - [ ] Use `OnStart()` for enrichment
  - [ ] Use Activity.SetTag() instead of telemetry properties
  - [ ] Inject dependencies via constructor
- [ ] Update registration:
  - [ ] Remove `services.AddSingleton<ITelemetryInitializer, ...>()`
  - [ ] Add `builder.AddProcessor<MyProcessor>()` in `ConfigureOpenTelemetryTracerProvider`
- [ ] Update property names to OpenTelemetry semantic conventions:
  - [ ] `User.Id` → `enduser.id`
  - [ ] `Operation.Id` → Automatic via TraceId
  - [ ] `Cloud.RoleName` → `service.name`
- [ ] Test enrichment appears in Azure Monitor

## See Also

- [TelemetryProcessors-removed.md](TelemetryProcessors-removed.md) - Processor/filtering migration
- [BaseProcessor-OnStart.md](../../api-reference/BaseProcessor/OnStart.md) - OnStart method details
- [Activity-SetTag.md](../../api-reference/Activity/SetTag.md) - Adding custom properties
- [activity-processor.md](../../concepts/activity-processor.md) - BaseProcessor concept
- [custom-dimensions.md](../../common-scenarios/custom-dimensions.md) - Adding custom dimensions
