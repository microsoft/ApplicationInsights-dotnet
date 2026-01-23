# TelemetryContext → Resource Mapping

**Category:** Mapping  
**Applies to:** Migration from Application Insights 2.x to 3.x  
**Related:** [resource-detector.md](../concepts/resource-detector.md), [IResourceDetector.Detect](../api-reference/IResourceDetector/Detect.md)

## Overview

In Application Insights 2.x, `TelemetryContext` provides service identity and environment metadata set on every telemetry item. In 3.x, this is replaced by OpenTelemetry **Resource** - metadata set **once at startup** and automatically attached to all telemetry.

## Core Concepts

### 2.x: TelemetryContext (Per-Item Metadata)

```csharp
// Set via ITelemetryInitializer - called for EVERY telemetry item
public class CloudRoleInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.Cloud.RoleName = "MyService";
        telemetry.Context.Cloud.RoleInstance = Environment.MachineName;
        telemetry.Context.Component.Version = "1.2.3";
    }
}
```

### 3.x: Resource (One-Time Metadata)

```csharp
// Set via IResourceDetector - called ONCE at startup
public class ServiceResourceDetector : IResourceDetector
{
    public Resource Detect()
    {
        return ResourceBuilder.CreateEmpty()
            .AddService(
                serviceName: "MyService",           // → Cloud.RoleName
                serviceVersion: "1.2.3",            // → SDK version
                serviceInstanceId: Environment.MachineName) // → Cloud.RoleInstance
            .Build();
    }
}
```

**Key Difference:** Resource is **significantly more efficient** - set once vs. per-telemetry-item overhead.

## Property Mappings

### Cloud Context

| 2.x TelemetryContext | 3.x Resource Attribute | Azure Monitor Field | Notes |
|---------------------|------------------------|---------------------|-------|
| `Context.Cloud.RoleName` | `service.name` | Cloud.RoleName | Service identifier |
| `Context.Cloud.RoleInstance` | `service.instance.id` | Cloud.RoleInstance | Instance identifier |
| N/A | `deployment.environment` | CustomDimensions | Environment (dev/prod) |

### Component Context

| 2.x TelemetryContext | 3.x Resource Attribute | Azure Monitor Field | Notes |
|---------------------|------------------------|---------------------|-------|
| `Context.Component.Version` | `service.version` | SDK version field | Application version |

### Device Context

| 2.x TelemetryContext | 3.x Resource Attribute | Azure Monitor Field | Notes |
|---------------------|------------------------|---------------------|-------|
| `Context.Device.Type` | `device.type` | CustomDimensions | Device type |
| `Context.Device.OperatingSystem` | `os.type`, `os.description` | CustomDimensions | OS info |

### Location Context

| 2.x TelemetryContext | 3.x Resource Attribute | Azure Monitor Field | Notes |
|---------------------|------------------------|---------------------|-------|
| `Context.Location.Ip` | N/A | Client IP | Set automatically by Azure Monitor |

### User/Session Context

**Important:** User and Session data are **NOT** Resource attributes (they're per-request, not per-service). Use Activity tags instead.

| 2.x TelemetryContext | 3.x Activity Tag | Azure Monitor Field |
|---------------------|------------------|---------------------|
| `Context.User.Id` | `activity.SetTag("enduser.id", userId)` | User.Id |
| `Context.Session.Id` | `activity.SetTag("session.id", sessionId)` | Session.Id |

## Migration Examples

### Example 1: Cloud Role Name

**2.x:**
```csharp
public class CloudRoleNameInitializer : ITelemetryInitializer
{
    private readonly string _roleName;
    
    public CloudRoleNameInitializer(IConfiguration configuration)
    {
        _roleName = configuration["ApplicationInsights:CloudRoleName"] 
                    ?? "DefaultService";
    }
    
    public void Initialize(ITelemetry telemetry)
    {
        // Called for EVERY telemetry item (inefficient)
        telemetry.Context.Cloud.RoleName = _roleName;
        telemetry.Context.Cloud.RoleInstance = Environment.MachineName;
    }
}

// Registration
services.AddSingleton<ITelemetryInitializer, CloudRoleNameInitializer>();
```

**3.x:**
```csharp
public class CloudRoleNameDetector : IResourceDetector
{
    private readonly string _roleName;
    
    public CloudRoleNameDetector(string roleName)
    {
        _roleName = roleName ?? "DefaultService";
    }
    
    public Resource Detect()
    {
        // Called ONCE at startup (efficient)
        return ResourceBuilder.CreateEmpty()
            .AddService(
                serviceName: _roleName,
                serviceInstanceId: Environment.MachineName)
            .Build();
    }
}

// Registration
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        var roleName = builder.Services.BuildServiceProvider()
            .GetRequiredService<IConfiguration>()["ApplicationInsights:CloudRoleName"];
            
        builder.ConfigureResource(resource =>
            resource.AddDetector(new CloudRoleNameDetector(roleName)));
    });
```

### Example 2: Kubernetes Environment

**2.x:**
```csharp
public class KubernetesContextInitializer : ITelemetryInitializer
{
    private readonly string _podName;
    private readonly string _namespace;
    
    public KubernetesContextInitializer()
    {
        _podName = Environment.GetEnvironmentVariable("HOSTNAME");
        _namespace = Environment.GetEnvironmentVariable("KUBERNETES_NAMESPACE");
    }
    
    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.Cloud.RoleName = _namespace ?? "default";
        telemetry.Context.Cloud.RoleInstance = _podName;
        telemetry.Properties["k8s.pod.name"] = _podName;
        telemetry.Properties["k8s.namespace.name"] = _namespace;
    }
}
```

**3.x:**
```csharp
public class KubernetesResourceDetector : IResourceDetector
{
    public Resource Detect()
    {
        var podName = Environment.GetEnvironmentVariable("HOSTNAME");
        var namespace_ = Environment.GetEnvironmentVariable("KUBERNETES_NAMESPACE");
        var podUid = Environment.GetEnvironmentVariable("KUBERNETES_POD_UID");
        
        var builder = ResourceBuilder.CreateEmpty();
        
        if (!string.IsNullOrEmpty(podName))
        {
            builder.AddService(
                serviceName: namespace_ ?? "default",
                serviceInstanceId: podName);
                
            builder.AddAttributes(new[]
            {
                new KeyValuePair<string, object>("k8s.pod.name", podName),
                new KeyValuePair<string, object>("k8s.namespace.name", namespace_ ?? "default")
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

// Registration
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.ConfigureResource(resource =>
            resource.AddDetector(new KubernetesResourceDetector()));
    });
```

### Example 3: Azure App Service Context

**2.x:**
```csharp
public class AppServiceContextInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        var siteName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");
        var instanceId = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID");
        
        if (!string.IsNullOrEmpty(siteName))
        {
            telemetry.Context.Cloud.RoleName = siteName;
            telemetry.Context.Cloud.RoleInstance = instanceId;
            telemetry.Properties["azure.app.service.name"] = siteName;
        }
    }
}
```

**3.x:**
```csharp
public class AppServiceResourceDetector : IResourceDetector
{
    public Resource Detect()
    {
        var siteName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");
        var instanceId = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID");
        var resourceGroup = Environment.GetEnvironmentVariable("WEBSITE_RESOURCE_GROUP");
        
        if (string.IsNullOrEmpty(siteName))
        {
            return Resource.Empty;
        }
        
        return ResourceBuilder.CreateEmpty()
            .AddService(
                serviceName: siteName,
                serviceInstanceId: instanceId)
            .AddAttributes(new[]
            {
                new KeyValuePair<string, object>("azure.app.service.name", siteName),
                new KeyValuePair<string, object>("azure.resource.group", resourceGroup ?? "unknown")
            })
            .Build();
    }
}
```

### Example 4: Component Version

**2.x:**
```csharp
public class ComponentVersionInitializer : ITelemetryInitializer
{
    private readonly string _version;
    
    public ComponentVersionInitializer()
    {
        _version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "0.0.0";
    }
    
    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.Component.Version = _version;
    }
}
```

**3.x:**
```csharp
public class VersionResourceDetector : IResourceDetector
{
    public Resource Detect()
    {
        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "0.0.0";
            
        return ResourceBuilder.CreateEmpty()
            .AddService(
                serviceName: "MyService", // Must provide service name
                serviceVersion: version)
            .Build();
    }
}
```

## Decision Tree: Resource vs. Processor

Use **Resource** (IResourceDetector) when:
- ✅ Value is **constant** for application lifetime
- ✅ Describes **service identity** or **environment**
- ✅ Same for **all telemetry types** (traces, metrics, logs)
- ✅ Examples: Cloud.RoleName, deployment.environment, k8s.namespace, service.version

Use **Activity Processor** when:
- ❌ Value is **per-request** or **dynamic**
- ❌ Depends on **request context** (user, session, headers)
- ❌ Only applies to **specific activities** (e.g., only Server kind)
- ❌ Examples: User.Id, Session.Id, custom request headers

### Quick Reference

| Context Property | Use Resource | Use Processor | Notes |
|-----------------|--------------|---------------|-------|
| Cloud.RoleName | ✅ | | Service name is constant |
| Cloud.RoleInstance | ✅ | | Instance ID is constant |
| Component.Version | ✅ | | Version is constant |
| deployment.environment | ✅ | | Environment is constant |
| User.Id | | ✅ | User varies per request |
| Session.Id | | ✅ | Session varies per request |
| Custom headers | | ✅ | Headers vary per request |

## Performance Comparison

### 2.x: TelemetryContext (Per-Item Cost)

```csharp
// ITelemetryInitializer called for EVERY telemetry item
// If application sends 1000 telemetry items/sec:
//   - Initialize() called 1000 times/sec
//   - String assignments performed 1000 times/sec
//   - Overhead: ~1000 function calls + memory allocations

public void Initialize(ITelemetry telemetry)
{
    // Called 1000x per second
    telemetry.Context.Cloud.RoleName = _roleName;      // 1000 assignments/sec
    telemetry.Context.Cloud.RoleInstance = _instance;  // 1000 assignments/sec
}
```

### 3.x: Resource (One-Time Cost)

```csharp
// IResourceDetector.Detect() called ONCE at startup
// If application sends 1000 telemetry items/sec:
//   - Detect() called 1 time total
//   - Resource attributes attached automatically to all telemetry
//   - Overhead: ~1 function call at startup

public Resource Detect()
{
    // Called ONCE at startup
    return ResourceBuilder.CreateEmpty()
        .AddService(serviceName: _roleName)
        .Build();
}
```

**Efficiency Gain:** ~99.9% reduction in overhead for constant metadata.

## Built-in Resource Detectors

OpenTelemetry provides built-in detectors for common environments:

```csharp
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.ConfigureResource(resource =>
        {
            // Built-in detectors
            resource.AddDetector(new AppServiceResourceDetector());
            resource.AddDetector(new AzureVMResourceDetector());
            resource.AddDetector(new ContainerResourceDetector());
            resource.AddDetector(new HostResourceDetector());
            resource.AddDetector(new ProcessResourceDetector());
            
            // Custom detector
            resource.AddDetector(new MyCustomResourceDetector());
        });
    });
```

## OpenTelemetry Semantic Conventions

When setting Resource attributes, follow [OpenTelemetry semantic conventions](https://opentelemetry.io/docs/specs/semconv/resource/):

### Service Attributes
```csharp
resource.AddService(
    serviceName: "my-service",              // service.name → Cloud.RoleName
    serviceVersion: "1.2.3",                // service.version
    serviceInstanceId: "instance-123");     // service.instance.id → Cloud.RoleInstance
```

### Deployment Attributes
```csharp
resource.AddAttributes(new[]
{
    new KeyValuePair<string, object>("deployment.environment", "production")
});
```

### Container Attributes
```csharp
resource.AddAttributes(new[]
{
    new KeyValuePair<string, object>("container.id", containerId),
    new KeyValuePair<string, object>("container.name", containerName),
    new KeyValuePair<string, object>("container.image.name", imageName)
});
```

### Kubernetes Attributes
```csharp
resource.AddAttributes(new[]
{
    new KeyValuePair<string, object>("k8s.namespace.name", namespace_),
    new KeyValuePair<string, object>("k8s.pod.name", podName),
    new KeyValuePair<string, object>("k8s.pod.uid", podUid),
    new KeyValuePair<string, object>("k8s.deployment.name", deploymentName)
});
```

### Cloud Provider Attributes
```csharp
resource.AddAttributes(new[]
{
    new KeyValuePair<string, object>("cloud.provider", "azure"),
    new KeyValuePair<string, object>("cloud.platform", "azure_app_service"),
    new KeyValuePair<string, object>("cloud.region", "westus2")
});
```

## See Also

- [resource-detector.md](../concepts/resource-detector.md) - Resource concept guide
- [IResourceDetector.Detect](../api-reference/IResourceDetector/Detect.md) - API reference
- [cloud-role-name.md](../common-scenarios/setting-cloud-role-name.md) - Cloud role scenario
- [telemetry-to-activity.md](./telemetry-to-activity.md) - Telemetry mapping
- [OpenTelemetry Resource Semantic Conventions](https://opentelemetry.io/docs/specs/semconv/resource/)
