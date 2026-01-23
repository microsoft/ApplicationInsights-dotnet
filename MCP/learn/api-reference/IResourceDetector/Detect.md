# IResourceDetector.Detect

**Category:** API Reference  
**Applies to:** OpenTelemetry .NET SDK (used in Application Insights 3.x)  
**Related:** [resource-detector.md](../../concepts/resource-detector.md), [TelemetryConfiguration.ConfigureOpenTelemetryBuilder](../TelemetryConfiguration/ConfigureOpenTelemetryBuilder.md)

## Overview

`Detect()` is the method called by OpenTelemetry SDK to retrieve resource attributes that describe the service identity and environment. Called **once** at startup, making it much more efficient than per-telemetry enrichment.

## Signature

```csharp
// From: OpenTelemetry .NET SDK
public interface IResourceDetector
{
    Resource Detect();
}
```

## Purpose

Returns a `Resource` object containing attributes that:
- Identify the service (service.name → Cloud.RoleName)
- Describe the service instance (service.instance.id → Cloud.RoleInstance)
- Provide environment metadata (deployment.environment, k8s.pod.name, etc.)

## Usage Pattern

```csharp
public class MyCustomResourceDetector : IResourceDetector
{
    public Resource Detect()
    {
        return ResourceBuilder.CreateEmpty()
            .AddService(
                serviceName: "MyService",
                serviceVersion: "1.0.0",
                serviceInstanceId: Environment.MachineName)
            .AddAttributes(new[]
            {
                new KeyValuePair<string, object>("deployment.environment", "production"),
                new KeyValuePair<string, object>("custom.attribute", "value")
            })
            .Build();
    }
}

// Registration
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.ConfigureResource(resource =>
            resource.AddDetector(new MyCustomResourceDetector()));
    });
```

## Real-World Examples

### Example 1: Cloud Role Name from Configuration

```csharp
public class CloudRoleNameDetector : IResourceDetector
{
    private readonly string _roleName;
    
    public CloudRoleNameDetector(IConfiguration configuration)
    {
        _roleName = configuration["ApplicationInsights:CloudRoleName"] 
                    ?? "DefaultServiceName";
    }
    
    public Resource Detect()
    {
        return ResourceBuilder.CreateEmpty()
            .AddService(serviceName: _roleName)
            .Build();
    }
}

// Result in Azure Monitor:
// Cloud.RoleName = "DefaultServiceName"
// Cloud.RoleInstance = hostname (automatically added)
```

### Example 2: Kubernetes Environment Detection

```csharp
public class KubernetesResourceDetector : IResourceDetector
{
    public Resource Detect()
    {
        var builder = ResourceBuilder.CreateEmpty();
        
        // Kubernetes environment variables
        var podName = Environment.GetEnvironmentVariable("HOSTNAME");
        var namespace = Environment.GetEnvironmentVariable("KUBERNETES_NAMESPACE");
        var podUid = Environment.GetEnvironmentVariable("KUBERNETES_POD_UID");
        
        if (!string.IsNullOrEmpty(podName))
        {
            builder.AddService(
                serviceName: namespace ?? "default",
                serviceInstanceId: podName);
                
            builder.AddAttributes(new[]
            {
                new KeyValuePair<string, object>("k8s.pod.name", podName),
                new KeyValuePair<string, object>("k8s.namespace.name", namespace ?? "default")
            });
            
            if (!string.IsNullOrEmpty(podUid))
            {
                builder.AddAttributes(new[]
                {
                    new KeyValuePair<string, object>("k8s.pod.uid", podUid)
                });
            }
        }
        
        return builder.Build();
    }
}

// Result in Azure Monitor:
// Cloud.RoleName = "default" (or namespace)
// Cloud.RoleInstance = "my-pod-abc123"
// CustomDimensions: k8s.pod.name, k8s.namespace.name, k8s.pod.uid
```

### Example 3: Azure App Service Detection

```csharp
public class AzureAppServiceResourceDetector : IResourceDetector
{
    public Resource Detect()
    {
        var builder = ResourceBuilder.CreateEmpty();
        
        // Azure App Service environment variables
        var websiteName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");
        var websiteInstanceId = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID");
        var websiteResourceGroup = Environment.GetEnvironmentVariable("WEBSITE_RESOURCE_GROUP");
        
        if (!string.IsNullOrEmpty(websiteName))
        {
            builder.AddService(
                serviceName: websiteName,
                serviceInstanceId: websiteInstanceId);
                
            builder.AddAttributes(new[]
            {
                new KeyValuePair<string, object>("azure.app.service.name", websiteName),
                new KeyValuePair<string, object>("azure.resource.group", websiteResourceGroup ?? "unknown")
            });
        }
        
        return builder.Build();
    }
}

// Result in Azure Monitor:
// Cloud.RoleName = "my-webapp"
// Cloud.RoleInstance = "abc123def456"
// CustomDimensions: azure.app.service.name, azure.resource.group
```

## Migration from 2.x

### Before (2.x): TelemetryContext in Initializer

```csharp
public class CloudRoleInitializer : ITelemetryInitializer
{
    private readonly string _roleName;
    
    public CloudRoleInitializer(IConfiguration configuration)
    {
        _roleName = configuration["ApplicationInsights:CloudRoleName"];
    }
    
    public void Initialize(ITelemetry telemetry)
    {
        // Called for EVERY telemetry item
        telemetry.Context.Cloud.RoleName = _roleName;
    }
}

services.AddSingleton<ITelemetryInitializer, CloudRoleInitializer>();
```

### After (3.x): IResourceDetector

```csharp
public class CloudRoleNameDetector : IResourceDetector
{
    private readonly string _roleName;
    
    public CloudRoleNameDetector(IConfiguration configuration)
    {
        _roleName = configuration["ApplicationInsights:CloudRoleName"];
    }
    
    public Resource Detect()
    {
        // Called ONCE at startup - much more efficient
        return ResourceBuilder.CreateEmpty()
            .AddService(serviceName: _roleName)
            .Build();
    }
}

services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.ConfigureResource(resource =>
            resource.AddDetector(new CloudRoleNameDetector(
                builder.Services.BuildServiceProvider()
                    .GetRequiredService<IConfiguration>())));
    });
```

## Performance Characteristics

- **Called:** Once at application startup
- **Thread Safety:** Must be thread-safe (may be called from multiple contexts)
- **Blocking:** Should return quickly; avoid expensive I/O operations
- **Caching:** Results are cached by OpenTelemetry SDK

## Common Attributes

### OpenTelemetry Semantic Conventions

| Attribute | Azure Monitor Mapping | Purpose |
|-----------|----------------------|---------|
| `service.name` | Cloud.RoleName | Service identifier |
| `service.version` | SDK version | Service version |
| `service.instance.id` | Cloud.RoleInstance | Instance identifier |
| `deployment.environment` | CustomDimensions | Environment (dev/staging/prod) |
| `k8s.pod.name` | CustomDimensions | Kubernetes pod name |
| `k8s.namespace.name` | CustomDimensions | Kubernetes namespace |

### Azure-Specific Attributes

| Attribute | Purpose |
|-----------|---------|
| `azure.app.service.name` | App Service name |
| `azure.resource.group` | Azure resource group |
| `azure.vm.id` | Virtual machine ID |

## Decision Tree: Resource vs Processor

Use **IResourceDetector** when:
- ✅ Attribute value is **constant** for application lifetime
- ✅ Same value applies to **all telemetry** (traces, metrics, logs)
- ✅ Describes **service identity** or **environment**
- ✅ Examples: Cloud.RoleName, deployment.environment, k8s.namespace

Use **BaseProcessor\<Activity\>** when:
- ❌ Attribute value is **per-request** (user ID, session ID, request path)
- ❌ Value depends on **telemetry content** (filtering, conditional enrichment)
- ❌ Only applies to **specific telemetry types** (only Server activities)
- ❌ Examples: Filtering by user agent, enriching with HTTP headers

## Return Value

Returns `Resource` object built using `ResourceBuilder`. May return `Resource.Empty` if no attributes detected.

## Error Handling

```csharp
public Resource Detect()
{
    try
    {
        // Detection logic
        return ResourceBuilder.CreateEmpty()
            .AddService(serviceName: DetectServiceName())
            .Build();
    }
    catch (Exception ex)
    {
        // Log error but don't crash application startup
        Console.WriteLine($"Resource detection failed: {ex.Message}");
        return Resource.Empty;
    }
}
```

## Related APIs

- [ResourceBuilder](../OpenTelemetry/ResourceBuilder.md) - Fluent builder for Resource
- [Resource](../OpenTelemetry/Resource.md) - Resource attributes container
- [ConfigureOpenTelemetryBuilder](../TelemetryConfiguration/ConfigureOpenTelemetryBuilder.md) - Registration point

## See Also

- [resource-detector.md](../../concepts/resource-detector.md) - Detailed concept guide
- [cloud-role-name.md](../../common-scenarios/setting-cloud-role-name.md) - Cloud role configuration
- [OpenTelemetry Semantic Conventions](https://opentelemetry.io/docs/specs/semconv/resource/)
