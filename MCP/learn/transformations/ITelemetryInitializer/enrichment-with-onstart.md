# ITelemetryInitializer → BaseProcessor\<Activity\> (Enrichment with OnStart)

**Category:** Transformation Pattern  
**Applies to:** Migration from Application Insights 2.x to 3.x  
**Related:** [activity-processor.md](../../concepts/activity-processor.md), [BaseProcessor OnStart](../../api-reference/BaseProcessor/OnStart.md)

## Overview

`ITelemetryInitializer` in 2.x is used to **enrich telemetry** with additional properties. In 3.x, this is replaced by `BaseProcessor<Activity>` using the **OnStart** method for enrichment.

## Key Differences

| 2.x ITelemetryInitializer | 3.x BaseProcessor\<Activity\> OnStart |
|--------------------------|--------------------------------------|
| Called after telemetry created | Called when Activity starts |
| Works with all telemetry types | Works with Activity (traces) only |
| Can set properties, context | Can set tags on Activity |
| Registered via AddSingleton | Added via ConfigureOpenTelemetryBuilder |

## Basic Pattern

### 2.x: ITelemetryInitializer

```csharp
public class MyInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        // Add custom property
        telemetry.Properties["customProperty"] = "value";
        
        // Set context
        telemetry.Context.Cloud.RoleName = "MyService";
    }
}

// Registration
services.AddSingleton<ITelemetryInitializer, MyInitializer>();
```

### 3.x: BaseProcessor OnStart

```csharp
public class MyProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        // Add custom tag (equivalent to property)
        activity.SetTag("customProperty", "value");
        
        // Context.Cloud.RoleName → Resource (not processor)
        // See context-to-resource.md
    }
}

// Registration
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.AddProcessor(new MyProcessor());
    });
```

## Migration Examples

### Example 1: Add Custom Properties

**2.x:**
```csharp
public class CustomPropertiesInitializer : ITelemetryInitializer
{
    private readonly IConfiguration _configuration;
    
    public CustomPropertiesInitializer(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Properties["environment"] = _configuration["Environment"];
        telemetry.Properties["version"] = Assembly.GetExecutingAssembly()
            .GetName().Version.ToString();
        telemetry.Properties["machineName"] = Environment.MachineName;
    }
}

services.AddSingleton<ITelemetryInitializer, CustomPropertiesInitializer>();
```

**3.x:**
```csharp
public class CustomPropertiesProcessor : BaseProcessor<Activity>
{
    private readonly IConfiguration _configuration;
    
    public CustomPropertiesProcessor(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public override void OnStart(Activity activity)
    {
        activity.SetTag("environment", _configuration["Environment"]);
        activity.SetTag("version", Assembly.GetExecutingAssembly()
            .GetName().Version.ToString());
        activity.SetTag("machineName", Environment.MachineName);
    }
}

services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        // Resolve from DI
        var configuration = builder.Services.BuildServiceProvider()
            .GetRequiredService<IConfiguration>();
        builder.AddProcessor(new CustomPropertiesProcessor(configuration));
    });
```

**Better 3.x:** Use Resource for constant values
```csharp
// Constant values should use Resource instead of Processor
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        var configuration = builder.Services.BuildServiceProvider()
            .GetRequiredService<IConfiguration>();
            
        builder.ConfigureResource(resource =>
            resource.AddAttributes(new[]
            {
                new KeyValuePair<string, object>("environment", 
                    configuration["Environment"]),
                new KeyValuePair<string, object>("version",
                    Assembly.GetExecutingAssembly().GetName().Version.ToString()),
                new KeyValuePair<string, object>("machineName",
                    Environment.MachineName)
            }));
    });
```

### Example 2: User Context Enrichment

**2.x:**
```csharp
// From: ApplicationInsightsDemo/ClientErrorTelemetryInitializer.cs
public class UserContextInitializer : ITelemetryInitializer
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public UserContextInitializer(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public void Initialize(ITelemetry telemetry)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            telemetry.Context.User.Id = httpContext.User.Identity.Name;
            telemetry.Context.User.AuthenticatedUserId = httpContext.User.Identity.Name;
            
            var role = httpContext.User.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(role))
            {
                telemetry.Properties["userRole"] = role;
            }
        }
    }
}

services.AddSingleton<ITelemetryInitializer, UserContextInitializer>();
```

**3.x:**
```csharp
public class UserContextProcessor : BaseProcessor<Activity>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public UserContextProcessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public override void OnStart(Activity activity)
    {
        // Only enrich server activities (requests)
        if (activity.Kind != ActivityKind.Server)
            return;
            
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            // OpenTelemetry semantic conventions
            activity.SetTag("enduser.id", httpContext.User.Identity.Name);
            
            var role = httpContext.User.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(role))
            {
                activity.SetTag("user.role", role);
            }
        }
    }
}

services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        var httpContextAccessor = builder.Services.BuildServiceProvider()
            .GetRequiredService<IHttpContextAccessor>();
        builder.AddProcessor(new UserContextProcessor(httpContextAccessor));
    });
```

### Example 3: Request Header Enrichment

**2.x:**
```csharp
public class RequestHeaderInitializer : ITelemetryInitializer
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public void Initialize(ITelemetry telemetry)
    {
        if (telemetry is RequestTelemetry request)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var correlationId = httpContext.Request.Headers["X-Correlation-Id"].ToString();
                if (!string.IsNullOrEmpty(correlationId))
                {
                    request.Properties["correlationId"] = correlationId;
                }
                
                var clientVersion = httpContext.Request.Headers["X-Client-Version"].ToString();
                if (!string.IsNullOrEmpty(clientVersion))
                {
                    request.Properties["clientVersion"] = clientVersion;
                }
            }
        }
    }
}
```

**3.x:**
```csharp
public class RequestHeaderProcessor : BaseProcessor<Activity>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public RequestHeaderProcessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public override void OnStart(Activity activity)
    {
        // Only enrich server activities (requests)
        if (activity.Kind != ActivityKind.Server)
            return;
            
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var correlationId = httpContext.Request.Headers["X-Correlation-Id"].ToString();
            if (!string.IsNullOrEmpty(correlationId))
            {
                activity.SetTag("correlation.id", correlationId);
            }
            
            var clientVersion = httpContext.Request.Headers["X-Client-Version"].ToString();
            if (!string.IsNullOrEmpty(clientVersion))
            {
                activity.SetTag("client.version", clientVersion);
            }
        }
    }
}
```

### Example 4: Conditional Enrichment

**2.x:**
```csharp
public class ConditionalEnrichmentInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        // Only enrich requests
        if (telemetry is RequestTelemetry request)
        {
            // Only for specific paths
            if (request.Url?.AbsolutePath.StartsWith("/api/") == true)
            {
                request.Properties["apiVersion"] = "v2";
            }
        }
        
        // Only enrich dependencies
        if (telemetry is DependencyTelemetry dependency)
        {
            if (dependency.Type == "Http")
            {
                dependency.Properties["category"] = "external";
            }
        }
    }
}
```

**3.x:**
```csharp
public class ConditionalEnrichmentProcessor : BaseProcessor<Activity>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public ConditionalEnrichmentProcessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public override void OnStart(Activity activity)
    {
        // Enrich server activities (requests)
        if (activity.Kind == ActivityKind.Server)
        {
            var path = _httpContextAccessor.HttpContext?.Request.Path.Value;
            if (path?.StartsWith("/api/") == true)
            {
                activity.SetTag("api.version", "v2");
            }
        }
        
        // Enrich client activities (dependencies)
        if (activity.Kind == ActivityKind.Client)
        {
            // Check if HTTP dependency
            var method = activity.GetTagItem("http.request.method");
            if (method != null)
            {
                activity.SetTag("category", "external");
            }
        }
    }
}
```

## Real-World Example: From ApplicationInsights-dotnet

**2.x Pattern:**
```csharp
// Similar to patterns in ApplicationInsights-dotnet-2x
public class CloudRoleInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        if (string.IsNullOrEmpty(telemetry.Context.Cloud.RoleName))
        {
            telemetry.Context.Cloud.RoleName = "MyDefaultRoleName";
        }
    }
}
```

**3.x Pattern:**
```csharp
// From: ApplicationInsights-dotnet/WEB/Src/Web/SyntheticUserAgentActivityProcessor.cs
internal sealed class SyntheticUserAgentActivityProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (activity.Kind == ActivityKind.Server)
        {
            var userAgent = activity.GetTagItem("http.request.header.user_agent") as string;
            if (!string.IsNullOrEmpty(userAgent))
            {
                if (userAgent.Contains("AlwaysOn") || userAgent.Contains("AppInsights"))
                {
                    // Enrichment in OnEnd (could also be OnStart)
                    activity.SetTag("ai.operation.synthetic_source", 
                        "Application Insights Availability Monitoring");
                }
            }
        }
    }
}
```

## When to Use OnStart vs OnEnd

### Use OnStart for:
- ✅ Adding properties available at start
- ✅ Enriching with static/contextual data
- ✅ Setting initial values
- ✅ Reading request headers, user context

### Use OnEnd for:
- ❌ Not typically for enrichment
- ✅ Use OnEnd for enrichment based on outcome (see filtering pattern)
- ✅ Better for: Filtering, conditional modification

**Rule of Thumb:** If you're adding data that's available when operation starts, use **OnStart**.

## Dependency Injection

### 2.x: Constructor Injection

```csharp
public class MyInitializer : ITelemetryInitializer
{
    private readonly IMyService _service;
    
    public MyInitializer(IMyService service)
    {
        _service = service;
    }
    
    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Properties["data"] = _service.GetData();
    }
}

services.AddSingleton<ITelemetryInitializer, MyInitializer>();
```

### 3.x: Manual Resolution

```csharp
public class MyProcessor : BaseProcessor<Activity>
{
    private readonly IMyService _service;
    
    public MyProcessor(IMyService service)
    {
        _service = service;
    }
    
    public override void OnStart(Activity activity)
    {
        activity.SetTag("data", _service.GetData());
    }
}

services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        var service = builder.Services.BuildServiceProvider()
            .GetRequiredService<IMyService>();
        builder.AddProcessor(new MyProcessor(service));
    });
```

## Performance Considerations

### 2.x: Called for Every Telemetry Item

```csharp
// ITelemetryInitializer called for:
// - Every Request
// - Every Dependency
// - Every Trace (ILogger)
// - Every Exception
// - Every Metric

// If adding constant values → inefficient
telemetry.Properties["environment"] = "production"; // Set 1000s of times
```

### 3.x: Use Resource for Constants

```csharp
// Constant values: Use Resource (set once)
builder.ConfigureResource(resource =>
    resource.AddAttributes(new[]
    {
        new KeyValuePair<string, object>("environment", "production")
    }));

// Dynamic values: Use Processor
builder.AddProcessor(new DynamicEnrichmentProcessor());
```

## Common Patterns

### Pattern 1: Add Timestamp
```csharp
// 2.x
telemetry.Properties["processedAt"] = DateTime.UtcNow.ToString("o");

// 3.x
activity.SetTag("processed.at", DateTime.UtcNow);
```

### Pattern 2: Add Machine Info
```csharp
// 2.x
telemetry.Properties["machineName"] = Environment.MachineName;
telemetry.Properties["processId"] = Process.GetCurrentProcess().Id.ToString();

// 3.x - Better: Use Resource (constant for app lifetime)
builder.ConfigureResource(resource =>
    resource.AddAttributes(new[]
    {
        new KeyValuePair<string, object>("host.name", Environment.MachineName),
        new KeyValuePair<string, object>("process.pid", Process.GetCurrentProcess().Id)
    }));
```

### Pattern 3: Add Request-Specific Data
```csharp
// 2.x
if (telemetry is RequestTelemetry request)
{
    request.Properties["requestId"] = Guid.NewGuid().ToString();
}

// 3.x
if (activity.Kind == ActivityKind.Server)
{
    activity.SetTag("request.id", Guid.NewGuid().ToString());
}
```

## See Also

- [activity-processor.md](../../concepts/activity-processor.md) - Processor concept guide
- [OnStart.md](../../api-reference/BaseProcessor/OnStart.md) - OnStart API reference
- [filtering-with-onend.md](./filtering-with-onend.md) - Filtering pattern (OnEnd)
- [properties-to-tags.md](../../mappings/properties-to-tags.md) - Properties mapping
- [context-to-resource.md](../../mappings/context-to-resource.md) - Context to Resource
