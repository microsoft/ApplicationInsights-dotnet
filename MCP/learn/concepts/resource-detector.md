---
title: Resource Detector - Setting Service Identity and Metadata
category: concept
applies-to: 3.x
related:
  - concepts/configure-otel-builder.md
  - transformations/ITelemetryInitializer/to-resource-detector.md
  - api-reference/IResourceDetector/Detect.md
  - common-scenarios/cloud-role-name.md
source: OpenTelemetry.Resources.IResourceDetector, OpenTelemetry.Resources.Resource
---

# Resource Detector - Setting Service Identity and Metadata

## Overview

A **Resource Detector** in OpenTelemetry identifies and describes the service/environment generating telemetry. It replaces Application Insights 2.x's `TelemetryContext` properties like `Cloud.RoleName`, `Cloud.RoleInstance`, and device/environment information.

## In 2.x: TelemetryContext

Application Insights 2.x used `TelemetryContext` to set service identity:

```csharp
// 2.x: Set via TelemetryContext
telemetry.Context.Cloud.RoleName = "MyService";
telemetry.Context.Cloud.RoleInstance = Environment.MachineName;
telemetry.Context.Device.OperatingSystem = "Windows 10";
telemetry.Context.Component.Version = "1.2.3";

// Or via ITelemetryInitializer
public class CloudRoleInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.Cloud.RoleName = "MyService";
        telemetry.Context.Cloud.RoleInstance = "instance-1";
    }
}
```

## In 3.x: Resource Attributes

OpenTelemetry uses **Resource** - a set of attributes describing the service:

```csharp
// 3.x: Resource attributes (set once for all telemetry)
config.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.ConfigureResource(resource =>
    {
        // service.name → Cloud.RoleName
        // service.instance.id → Cloud.RoleInstance
        resource.AddService(
            serviceName: "MyService",
            serviceVersion: "1.2.3",
            serviceInstanceId: Environment.MachineName);
        
        // Custom attributes
        resource.AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = "production",
            ["team.name"] = "platform"
        });
    });
});
```

## Resource vs TelemetryContext

| Aspect | 2.x TelemetryContext | 3.x Resource |
|--------|---------------------|--------------|
| **Scope** | Per telemetry item | Per service (set once) |
| **When Set** | Every telemetry via Initializer | At application startup |
| **Performance** | Evaluated per item | Set once, reused |
| **Standard** | AI proprietary | OpenTelemetry standard |

## OpenTelemetry Semantic Conventions

Standard resource attributes map to Application Insights fields:

| OpenTelemetry Attribute | AI Field | Example |
|------------------------|----------|---------|
| `service.name` | `Cloud.RoleName` | "MyApiService" |
| `service.instance.id` | `Cloud.RoleInstance` | "prod-vm-01" |
| `service.version` | `Application.Ver` | "1.2.3" |
| `service.namespace` | _(prefix to RoleName)_ | "MyCompany.Production" |
| `deployment.environment` | Custom dimension | "production" |
| `host.name` | Custom dimension | "vm-eastus-01" |

## IResourceDetector Interface

```csharp
// Source: OpenTelemetry.Resources.IResourceDetector

public interface IResourceDetector
{
    Resource Detect();
}

// Usage
public class MyResourceDetector : IResourceDetector
{
    public Resource Detect()
    {
        var attributes = new Dictionary<string, object>
        {
            ["service.name"] = GetServiceName(),
            ["service.instance.id"] = GetInstanceId(),
            ["custom.attribute"] = GetCustomValue()
        };
        
        return new Resource(attributes);
    }
}
```

## Built-in Resource Detectors

OpenTelemetry provides several built-in detectors:

```csharp
config.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.ConfigureResource(resource =>
    {
        // Process detector - adds process.* attributes
        resource.AddDetector(new ProcessDetector());
        
        // Host detector - adds host.* attributes
        resource.AddDetector(new HostDetector());
        
        // Azure App Service detector
        resource.AddDetector(new AppServiceResourceDetector());
        
        // Custom detector
        resource.AddDetector(new MyCustomDetector());
    });
});
```

## Common Patterns

### Pattern 1: Set Cloud Role Name (Service Name)

```csharp
// Simple way - using AddService helper
config.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.ConfigureResource(resource =>
    {
        resource.AddService(
            serviceName: "MyApiService",        // → Cloud.RoleName
            serviceInstanceId: Environment.MachineName);  // → Cloud.RoleInstance
    });
});
```

### Pattern 2: Custom Resource Detector

```csharp
public class EnvironmentResourceDetector : IResourceDetector
{
    public Resource Detect()
    {
        var attributes = new Dictionary<string, object>
        {
            ["deployment.environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "unknown",
            ["deployment.region"] = Environment.GetEnvironmentVariable("AZURE_REGION") ?? "unknown",
            ["team.name"] = "platform",
            ["cost.center"] = "engineering"
        };
        
        return new Resource(attributes);
    }
}

// Register it
config.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.ConfigureResource(resource =>
    {
        resource.AddDetector(new EnvironmentResourceDetector());
    });
});
```

### Pattern 3: Azure App Service Detection

```csharp
public class AppServiceResourceDetector : IResourceDetector
{
    public Resource Detect()
    {
        var attributes = new Dictionary<string, object>();
        
        // Detect if running in Azure App Service
        var websiteName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");
        if (!string.IsNullOrEmpty(websiteName))
        {
            attributes["service.name"] = websiteName;
            attributes["azure.app_service.name"] = websiteName;
            
            var instanceId = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID");
            if (!string.IsNullOrEmpty(instanceId))
            {
                attributes["service.instance.id"] = instanceId;
            }
            
            var resourceGroup = Environment.GetEnvironmentVariable("WEBSITE_RESOURCE_GROUP");
            if (!string.IsNullOrEmpty(resourceGroup))
            {
                attributes["azure.resource_group"] = resourceGroup;
            }
        }
        
        return new Resource(attributes);
    }
}
```

### Pattern 4: Kubernetes Detection

```csharp
public class KubernetesResourceDetector : IResourceDetector
{
    public Resource Detect()
    {
        var attributes = new Dictionary<string, object>();
        
        // Read from Kubernetes downward API
        var podName = Environment.GetEnvironmentVariable("K8S_POD_NAME");
        var nodeName = Environment.GetEnvironmentVariable("K8S_NODE_NAME");
        var namespace = Environment.GetEnvironmentVariable("K8S_NAMESPACE");
        
        if (!string.IsNullOrEmpty(podName))
        {
            attributes["k8s.pod.name"] = podName;
            attributes["service.instance.id"] = podName;
        }
        
        if (!string.IsNullOrEmpty(nodeName))
        {
            attributes["k8s.node.name"] = nodeName;
        }
        
        if (!string.IsNullOrEmpty(namespace))
        {
            attributes["k8s.namespace.name"] = namespace;
        }
        
        return new Resource(attributes);
    }
}
```

## Migration from 2.x ITelemetryInitializer

### 2.x: Setting Cloud Role in Initializer

```csharp
// 2.x: ITelemetryInitializer sets context per telemetry
public class CloudRoleInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        // Called for EVERY telemetry item
        telemetry.Context.Cloud.RoleName = "MyService";
        telemetry.Context.Cloud.RoleInstance = Environment.MachineName;
        telemetry.Context.Component.Version = "1.2.3";
        telemetry.Context.Device.OperatingSystem = GetOS();
    }
}

// Registration
config.TelemetryInitializers.Add(new CloudRoleInitializer());
```

### 3.x: Resource Detector (Set Once)

```csharp
// 3.x: Resource Detector sets attributes ONCE at startup
public class ServiceResourceDetector : IResourceDetector
{
    public Resource Detect()
    {
        // Called ONCE during initialization
        return new Resource(new Dictionary<string, object>
        {
            ["service.name"] = "MyService",
            ["service.instance.id"] = Environment.MachineName,
            ["service.version"] = "1.2.3",
            ["os.type"] = GetOS()
        });
    }
}

// Registration
config.ConfigureOpenTelemetryBuilder(builder =>
{
    builder.ConfigureResource(resource =>
    {
        resource.AddDetector(new ServiceResourceDetector());
    });
});
```

## Decision Tree: Resource vs Activity Processor

**When to use Resource Detector:**
- Setting service identity (name, version, instance)
- Environment information (deployment, region)
- Infrastructure metadata (host, container, k8s)
- Values that are **constant for the lifetime of the process**

**When to use Activity Processor:**
- Request-specific data (user ID, session ID)
- Dynamic per-request information
- Filtering or sampling decisions
- Values that **change per operation**

```
Is this value constant for the entire process lifetime?
├─ Yes → Use Resource Detector
│  Examples: service name, version, environment, host
│
└─ No → Use Activity Processor
   Examples: user ID, request path, custom per-request data
```

## ASP.NET Core Integration

```csharp
// Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = Configuration["ApplicationInsights:ConnectionString"];
    });
    
    // Configure resource
    services.Configure<TelemetryConfiguration>(config =>
    {
        config.ConfigureOpenTelemetryBuilder(builder =>
        {
            builder.ConfigureResource(resource =>
            {
                resource.AddService(
                    serviceName: "MyApiService",
                    serviceVersion: GetVersion());
                    
                resource.AddDetector(new EnvironmentResourceDetector());
            });
        });
    });
}
```

## Viewing Resource Attributes

Resource attributes appear in Application Insights as:
- Standard fields (Cloud.RoleName, Cloud.RoleInstance)
- Custom dimensions/properties on all telemetry

```json
// In Application Insights
{
  "cloud": {
    "roleName": "MyService",        // from service.name
    "roleInstance": "prod-vm-01"    // from service.instance.id
  },
  "application": {
    "ver": "1.2.3"                  // from service.version
  },
  "customDimensions": {
    "deployment.environment": "production",
    "team.name": "platform"
  }
}
```

## Performance Benefits

Resource Detectors are more efficient than 2.x Initializers:

| Metric | 2.x Initializer | 3.x Resource Detector |
|--------|----------------|---------------------|
| **Execution** | Every telemetry item | Once at startup |
| **CPU Cost** | High (repeated) | Minimal (one-time) |
| **Memory** | Allocations per item | Single allocation |

For a service sending 1000 req/sec, this saves millions of initializer calls per hour.

## See Also

- [configure-otel-builder.md](configure-otel-builder.md) - ConfigureOpenTelemetryBuilder API
- [transformations/ITelemetryInitializer/to-resource-detector.md](../transformations/ITelemetryInitializer/to-resource-detector.md) - When to use Resource vs Processor
- [api-reference/IResourceDetector/Detect.md](../api-reference/IResourceDetector/Detect.md) - IResourceDetector API details
- [common-scenarios/cloud-role-name.md](../common-scenarios/cloud-role-name.md) - Setting cloud role
- [examples/resource-detectors/](../examples/resource-detectors/) - Real examples

## References

- **IResourceDetector**: `OpenTelemetry.Resources.IResourceDetector`
- **Resource**: `OpenTelemetry.Resources.Resource`
- **Semantic Conventions**: https://opentelemetry.io/docs/specs/semconv/resource/
