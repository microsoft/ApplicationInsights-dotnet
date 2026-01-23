# Filtering Telemetry in 3.x

**Category:** Common Scenario  
**Applies to:** Application Insights .NET SDK 3.x  
**Related:** [filtering-with-onend.md](../transformations/ITelemetryProcessor/filtering-with-onend.md), [activity-processor.md](../concepts/activity-processor.md)

## Overview

Filtering reduces telemetry volume by dropping unwanted items before export. In 3.x, filtering is done using **BaseProcessor\<Activity\>** with the **OnEnd** method, setting `Activity.IsAllDataRequested = false` to drop telemetry.

## Quick Solution

```csharp
public class MyFilter : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (ShouldFilter(activity))
        {
            activity.IsAllDataRequested = false; // Drop this telemetry
            return;
        }
    }
}

// Registration
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.AddProcessor(new MyFilter());
    });
```

## Common Filtering Scenarios

### 1. Filter Health Check Endpoints

```csharp
public class HealthCheckFilter : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (activity.Kind == ActivityKind.Server)
        {
            var path = activity.GetTagItem("url.path") as string;
            
            // Drop health check and readiness endpoints
            if (path == "/health" || 
                path == "/healthz" || 
                path == "/ready" || 
                path == "/livez")
            {
                activity.IsAllDataRequested = false;
                return;
            }
        }
    }
}
```

### 2. Filter Successful Dependencies

```csharp
public class SuccessfulDependencyFilter : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Only keep failed dependencies
        if (activity.Kind == ActivityKind.Client)
        {
            if (activity.Status == ActivityStatusCode.Ok || 
                activity.Status == ActivityStatusCode.Unset)
            {
                activity.IsAllDataRequested = false;
                return;
            }
        }
    }
}
```

### 3. Filter by HTTP Status Code

```csharp
public class StatusCodeFilter : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (activity.Kind == ActivityKind.Server)
        {
            var statusCode = activity.GetTagItem("http.response.status_code") as int?;
            
            // Only keep errors (4xx, 5xx)
            if (statusCode.HasValue && statusCode.Value < 400)
            {
                activity.IsAllDataRequested = false;
                return;
            }
        }
    }
}
```

### 4. Filter Synthetic Traffic

```csharp
public class SyntheticTrafficFilter : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        var syntheticSource = activity.GetTagItem("ai.operation.synthetic_source");
        
        // Drop all synthetic traffic (availability tests, bots)
        if (syntheticSource != null)
        {
            activity.IsAllDataRequested = false;
            return;
        }
    }
}
```

### 5. Filter by User Agent

```csharp
public class UserAgentFilter : BaseProcessor<Activity>
{
    private static readonly string[] BotUserAgents = 
    {
        "bot", "crawler", "spider", "scraper", "curl", "wget"
    };
    
    public override void OnEnd(Activity activity)
    {
        if (activity.Kind == ActivityKind.Server)
        {
            var userAgent = activity.GetTagItem("http.request.header.user_agent") as string;
            
            if (!string.IsNullOrEmpty(userAgent))
            {
                foreach (var botPattern in BotUserAgents)
                {
                    if (userAgent.Contains(botPattern, StringComparison.OrdinalIgnoreCase))
                    {
                        activity.IsAllDataRequested = false;
                        return;
                    }
                }
            }
        }
    }
}
```

### 6. Filter by Duration (Slow Operations Only)

```csharp
public class SlowOperationsFilter : BaseProcessor<Activity>
{
    private readonly TimeSpan _threshold = TimeSpan.FromSeconds(5);
    
    public override void OnEnd(Activity activity)
    {
        // Only keep slow operations
        if (activity.Duration < _threshold)
        {
            activity.IsAllDataRequested = false;
            return;
        }
    }
}
```

### 7. Filter by Dependency Type

```csharp
public class DependencyTypeFilter : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (activity.Kind == ActivityKind.Client)
        {
            var httpMethod = activity.GetTagItem("http.request.method");
            var dbSystem = activity.GetTagItem("db.system");
            
            // Only keep HTTP and SQL dependencies
            if (httpMethod == null && dbSystem == null)
            {
                activity.IsAllDataRequested = false;
                return;
            }
        }
    }
}
```

### 8. Filter Static Resources

```csharp
public class StaticResourceFilter : BaseProcessor<Activity>
{
    private static readonly string[] StaticExtensions = 
    {
        ".js", ".css", ".jpg", ".jpeg", ".png", ".gif", ".ico", 
        ".svg", ".woff", ".woff2", ".ttf", ".eot"
    };
    
    public override void OnEnd(Activity activity)
    {
        if (activity.Kind == ActivityKind.Server)
        {
            var path = activity.GetTagItem("url.path") as string;
            
            if (!string.IsNullOrEmpty(path))
            {
                foreach (var ext in StaticExtensions)
                {
                    if (path.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                    {
                        activity.IsAllDataRequested = false;
                        return;
                    }
                }
            }
        }
    }
}
```

## Advanced Filtering

### Conditional Filtering by Environment

```csharp
public class EnvironmentBasedFilter : BaseProcessor<Activity>
{
    private readonly IHostEnvironment _environment;
    
    public EnvironmentBasedFilter(IHostEnvironment environment)
    {
        _environment = environment;
    }
    
    public override void OnEnd(Activity activity)
    {
        // In development, only send errors
        if (_environment.IsDevelopment())
        {
            if (activity.Status == ActivityStatusCode.Ok || 
                activity.Status == ActivityStatusCode.Unset)
            {
                activity.IsAllDataRequested = false;
                return;
            }
        }
        
        // In production, send everything (no filtering)
    }
}
```

### Complex Multi-Condition Filter

```csharp
public class ComplexFilter : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Keep if:
        // 1. It's an error OR
        // 2. It's slow (>3s) OR
        // 3. It's a specific important endpoint
        
        bool isError = activity.Status == ActivityStatusCode.Error;
        bool isSlow = activity.Duration.TotalSeconds > 3;
        
        var path = activity.GetTagItem("url.path") as string;
        bool isImportant = path == "/api/orders" || path == "/api/payment";
        
        // Drop if none of the conditions are met
        if (!isError && !isSlow && !isImportant)
        {
            activity.IsAllDataRequested = false;
            return;
        }
    }
}
```

### Filter with Configuration

```csharp
public class ConfigurableFilter : BaseProcessor<Activity>
{
    private readonly HashSet<string> _excludedPaths;
    private readonly bool _filterSuccessful;
    
    public ConfigurableFilter(IConfiguration configuration)
    {
        _excludedPaths = new HashSet<string>(
            configuration.GetSection("Telemetry:ExcludedPaths").Get<string[]>() ?? Array.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);
        
        _filterSuccessful = configuration.GetValue<bool>("Telemetry:FilterSuccessfulDependencies");
    }
    
    public override void OnEnd(Activity activity)
    {
        // Filter by configured paths
        if (activity.Kind == ActivityKind.Server)
        {
            var path = activity.GetTagItem("url.path") as string;
            if (_excludedPaths.Contains(path))
            {
                activity.IsAllDataRequested = false;
                return;
            }
        }
        
        // Filter successful dependencies if configured
        if (_filterSuccessful && activity.Kind == ActivityKind.Client)
        {
            if (activity.Status != ActivityStatusCode.Error)
            {
                activity.IsAllDataRequested = false;
                return;
            }
        }
    }
}

// appsettings.json
{
  "Telemetry": {
    "ExcludedPaths": ["/health", "/metrics", "/swagger"],
    "FilterSuccessfulDependencies": true
  }
}
```

## Multiple Filters

```csharp
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        // Filters execute in registration order
        builder.AddProcessor(new HealthCheckFilter());
        builder.AddProcessor(new StaticResourceFilter());
        builder.AddProcessor(new SyntheticTrafficFilter());
        builder.AddProcessor(new SuccessfulDependencyFilter());
    });
```

**Note:** If any processor sets `IsAllDataRequested = false`, telemetry is dropped. Subsequent processors won't see it.

## Filtering vs Sampling

### Filtering (Deterministic)
```csharp
// Always drop specific telemetry
public override void OnEnd(Activity activity)
{
    if (activity.GetTagItem("url.path") == "/health")
    {
        activity.IsAllDataRequested = false; // Always drop
    }
}
```

### Sampling (Probabilistic)
```csharp
// Drop randomly based on percentage
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        // Keep 10% of all telemetry
        builder.AddTraceIdRatioBasedSampler(0.1);
    });
```

### Combining Both
```csharp
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        // First: Filter unwanted telemetry
        builder.AddProcessor(new HealthCheckFilter());
        
        // Then: Sample remaining telemetry
        builder.AddTraceIdRatioBasedSampler(0.1);
    });
```

## Performance Considerations

### Efficient Filtering

```csharp
public class EfficientFilter : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Fast checks first (Kind check is very fast)
        if (activity.Kind != ActivityKind.Server)
            return;
        
        // Then tag lookups (slightly slower)
        var path = activity.GetTagItem("url.path") as string;
        if (path == null)
            return;
        
        // Finally: string operations (slower)
        if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
        {
            activity.IsAllDataRequested = false;
        }
    }
}
```

### Avoid Heavy Operations

```csharp
// ❌ BAD: Heavy regex in every OnEnd
private static readonly Regex PathRegex = new Regex(@"^/api/v\d+/health");

public override void OnEnd(Activity activity)
{
    var path = activity.GetTagItem("url.path") as string;
    if (PathRegex.IsMatch(path))
    {
        activity.IsAllDataRequested = false;
    }
}

// ✅ GOOD: Simple string operations
public override void OnEnd(Activity activity)
{
    var path = activity.GetTagItem("url.path") as string;
    if (path?.Contains("/health") == true)
    {
        activity.IsAllDataRequested = false;
    }
}
```

## Testing Filters

```csharp
[Fact]
public void HealthCheckFilter_FiltersHealthEndpoint()
{
    var filter = new HealthCheckFilter();
    var activity = new Activity("Test");
    activity.Start();
    activity.SetTag("url.path", "/health");
    activity.Stop();
    
    filter.OnEnd(activity);
    
    Assert.False(activity.IsAllDataRequested);
}
```

## Verification

### Check Filter Effectiveness in Azure Monitor

```kusto
// Before filtering
requests
| where timestamp > ago(1h)
| summarize count() by url

// After filtering  
requests
| where timestamp > ago(1h)
| summarize count() by url
// Health check endpoints should be absent
```

### Enable Diagnostics

```csharp
public class DiagnosticFilter : BaseProcessor<Activity>
{
    private readonly ILogger<DiagnosticFilter> _logger;
    
    public override void OnEnd(Activity activity)
    {
        var path = activity.GetTagItem("url.path") as string;
        
        if (path == "/health")
        {
            _logger.LogDebug("Filtering health check: {Path}", path);
            activity.IsAllDataRequested = false;
        }
    }
}
```

## Migration from 2.x

### 2.x: ITelemetryProcessor

```csharp
public class MyFilter : ITelemetryProcessor
{
    private ITelemetryProcessor _next;
    
    public void Process(ITelemetry telemetry)
    {
        if (ShouldFilter(telemetry))
            return; // Drop by not calling next
        
        _next.Process(telemetry);
    }
}
```

### 3.x: BaseProcessor

```csharp
public class MyFilter : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (ShouldFilter(activity))
        {
            activity.IsAllDataRequested = false; // Drop explicitly
            return;
        }
    }
}
```

## Best Practices

1. **Filter Early:** Apply filters to reduce downstream processing
2. **Order Matters:** Put most common filters first
3. **Be Specific:** Filter only what you need to, not everything
4. **Test Thoroughly:** Verify filters don't drop important telemetry
5. **Monitor Impact:** Track filtered vs. sent telemetry volume

## See Also

- [filtering-with-onend.md](../transformations/ITelemetryProcessor/filtering-with-onend.md) - Filtering transformation pattern
- [activity-processor.md](../concepts/activity-processor.md) - Processor concept
- [OnEnd.md](../api-reference/BaseProcessor/OnEnd.md) - OnEnd API reference
- [IsAllDataRequested.md](../api-reference/Activity/IsAllDataRequested.md) - Filtering property
