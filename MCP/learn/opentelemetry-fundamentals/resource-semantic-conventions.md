# OpenTelemetry Resource and Semantic Conventions

**Category:** OpenTelemetry Fundamentals  
**Applies to:** Understanding Resource attributes and semantic conventions  
**Related:** [resource-detector.md](../concepts/resource-detector.md), [setting-cloud-role-name.md](../common-scenarios/setting-cloud-role-name.md)

## Overview

OpenTelemetry **Resources** represent the entity producing telemetry (your service, container, pod, etc.). **Semantic conventions** provide standardized attribute names for common scenarios, ensuring consistency across observability tools.

## What is a Resource?

A Resource is a set of attributes describing the source of telemetry:

```csharp
// Resource attributes
{
    "service.name": "OrderService",
    "service.version": "1.0.0",
    "service.instance.id": "pod-12345",
    "deployment.environment": "production",
    "host.name": "server01",
    "container.id": "abc123"
}
```

These attributes are automatically attached to **all** telemetry (traces, metrics, logs) from your service.

## Configuring Resources

### Basic Service Configuration

```csharp
builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        otel.ConfigureResource(resource =>
        {
            resource.AddService(
                serviceName: "OrderService",
                serviceVersion: "1.0.0",
                serviceInstanceId: Environment.MachineName);
        });
    });
```

**Azure Monitor Mapping:**
- `service.name` → `cloud_RoleName`
- `service.version` → `application_Version`
- `service.instance.id` → `cloud_RoleInstance`

### Custom Resource Attributes

```csharp
otel.ConfigureResource(resource =>
{
    resource
        .AddService("OrderService", "1.0.0")
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = "production",
            ["team.name"] = "OrderManagement",
            ["region"] = "westus2",
            ["datacenter"] = "dc01"
        });
});
```

### Environment-Based Configuration

```csharp
var environment = builder.Environment.EnvironmentName;
var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0";

otel.ConfigureResource(resource =>
{
    resource.AddService(
        serviceName: "OrderService",
        serviceVersion: version,
        serviceInstanceId: $"{Environment.MachineName}-{Environment.ProcessId}")
    .AddAttributes(new Dictionary<string, object>
    {
        ["deployment.environment"] = environment.ToLowerInvariant()
    });
});
```

## Resource Detectors

Resource detectors automatically discover environment information.

### Built-In Detectors

```csharp
using OpenTelemetry.Resources;

otel.ConfigureResource(resource =>
{
    resource
        .AddService("OrderService")
        // Detect container information
        .AddContainerDetector()
        // Detect host information
        .AddHostDetector()
        // Detect process information
        .AddProcessDetector()
        // Detect runtime information (.NET version)
        .AddProcessRuntimeDetector();
});
```

### Custom Resource Detector

```csharp
public class CustomResourceDetector : IResourceDetector
{
    public Resource Detect()
    {
        var attributes = new List<KeyValuePair<string, object>>
        {
            new("deployment.environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"),
            new("team.name", "OrderManagement"),
            new("custom.region", GetCurrentRegion())
        };
        
        return new Resource(attributes);
    }
    
    private string GetCurrentRegion()
    {
        // Logic to detect current region
        return "westus2";
    }
}
```

Register custom detector:

```csharp
otel.ConfigureResource(resource =>
{
    resource
        .AddService("OrderService")
        .AddDetector(new CustomResourceDetector());
});
```

## Semantic Conventions

Semantic conventions define standard attribute names for common scenarios.

### Service Attributes

| Attribute | Description | Example |
|-----------|-------------|---------|
| `service.name` | Service name | "OrderService" |
| `service.version` | Service version | "1.0.0" |
| `service.instance.id` | Unique instance ID | "pod-12345" |
| `service.namespace` | Logical namespace | "production" |

```csharp
resource.AddService(
    serviceName: "OrderService",
    serviceVersion: "1.0.0",
    serviceInstanceId: "pod-12345",
    serviceNamespace: "production");
```

### Deployment Attributes

| Attribute | Description | Example |
|-----------|-------------|---------|
| `deployment.environment` | Deployment environment | "production", "staging", "dev" |

```csharp
resource.AddAttributes(new Dictionary<string, object>
{
    ["deployment.environment"] = "production"
});
```

### Host Attributes (Auto-Detected)

| Attribute | Description | Example |
|-----------|-------------|---------|
| `host.name` | Hostname | "server01.example.com" |
| `host.id` | Unique host ID | "abc123" |
| `host.type` | Host type | "virtual", "physical" |
| `host.arch` | CPU architecture | "amd64", "arm64" |

```csharp
resource.AddHostDetector();
```

### Container Attributes (Auto-Detected)

| Attribute | Description | Example |
|-----------|-------------|---------|
| `container.id` | Container ID | "abc123def456" |
| `container.name` | Container name | "order-service" |
| `container.image.name` | Image name | "myregistry/order-service" |
| `container.image.tag` | Image tag | "1.0.0" |

```csharp
resource.AddContainerDetector();
```

### Process Attributes (Auto-Detected)

| Attribute | Description | Example |
|-----------|-------------|---------|
| `process.pid` | Process ID | 12345 |
| `process.executable.name` | Executable name | "OrderService.dll" |
| `process.command_line` | Command line | "dotnet OrderService.dll" |
| `process.runtime.name` | Runtime name | ".NET" |
| `process.runtime.version` | Runtime version | "8.0.0" |

```csharp
resource
    .AddProcessDetector()
    .AddProcessRuntimeDetector();
```

### Kubernetes Attributes

For Kubernetes environments, use K8s resource detector:

```csharp
// Requires: OpenTelemetry.ResourceDetectors.Container package
otel.ConfigureResource(resource =>
{
    resource
        .AddService("OrderService")
        .AddContainerDetector();
});
```

Detected attributes:
- `k8s.pod.name`
- `k8s.namespace.name`
- `k8s.deployment.name`
- `k8s.node.name`
- `k8s.cluster.name`

## Complete Configuration Examples

### ASP.NET Core Application

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        otel.ConfigureResource(resource =>
        {
            var version = Assembly.GetEntryAssembly()
                ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "1.0.0";
            
            resource
                .AddService(
                    serviceName: "OrderService",
                    serviceVersion: version,
                    serviceInstanceId: $"{Environment.MachineName}-{Environment.ProcessId}")
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = builder.Environment.EnvironmentName.ToLowerInvariant(),
                    ["team"] = "OrderManagement",
                    ["repo"] = "https://github.com/company/order-service"
                })
                .AddHostDetector()
                .AddProcessDetector()
                .AddProcessRuntimeDetector()
                .AddContainerDetector();
        });
    });
```

### Worker Service

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplicationInsightsTelemetryWorkerService()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        otel.ConfigureResource(resource =>
        {
            resource
                .AddService("OrderProcessor", "2.1.0")
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = builder.Environment.EnvironmentName,
                    ["worker.type"] = "background-processor"
                })
                .AddHostDetector()
                .AddProcessDetector();
        });
    });
```

## Viewing Resources in Azure Monitor

Resource attributes appear in different places:

### Cloud Role Name

```kql
requests
| where timestamp > ago(1h)
| project 
    timestamp,
    cloud_RoleName,  // service.name
    cloud_RoleInstance,  // service.instance.id
    application_Version  // service.version
```

### Custom Dimensions

Custom resource attributes appear in `customDimensions`:

```kql
requests
| where timestamp > ago(1h)
| extend 
    environment = tostring(customDimensions.deployment_environment),
    team = tostring(customDimensions.team)
| summarize count() by cloud_RoleName, environment
```

## Migration from 2.x

### Before (2.x): Context Properties

```csharp
services.Configure<TelemetryConfiguration>(config =>
{
    config.Context.Cloud.RoleName = "OrderService";
    config.Context.Cloud.RoleInstance = Environment.MachineName;
    config.Context.Component.Version = "1.0.0";
});

// Or with initializer
public class CloudRoleInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.Cloud.RoleName = "OrderService";
        telemetry.Context.Cloud.RoleInstance = Environment.MachineName;
        telemetry.Context.Component.Version = "1.0.0";
    }
}
```

### After (3.x): Resource Configuration

```csharp
otel.ConfigureResource(resource =>
{
    resource.AddService(
        serviceName: "OrderService",  // → cloud_RoleName
        serviceVersion: "1.0.0",      // → application_Version
        serviceInstanceId: Environment.MachineName);  // → cloud_RoleInstance
});
```

## Best Practices

### 1. Always Set Service Name

```csharp
// Minimum required
resource.AddService(serviceName: "OrderService");
```

### 2. Use Environment-Specific Values

```csharp
var serviceName = builder.Configuration["ServiceName"] ?? "DefaultService";
var environment = builder.Environment.EnvironmentName;

resource
    .AddService(serviceName)
    .AddAttributes(new Dictionary<string, object>
    {
        ["deployment.environment"] = environment.ToLowerInvariant()
    });
```

### 3. Include Version Information

```csharp
var version = Assembly.GetEntryAssembly()
    ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
    ?.InformationalVersion ?? "1.0.0";

resource.AddService("OrderService", serviceVersion: version);
```

### 4. Use Resource Detectors

```csharp
// Auto-detect host, process, container info
resource
    .AddService("OrderService")
    .AddHostDetector()
    .AddProcessDetector()
    .AddContainerDetector();
```

### 5. Avoid High-Cardinality Attributes

```csharp
// Good: Low cardinality
resource.AddAttributes(new Dictionary<string, object>
{
    ["deployment.environment"] = "production",  // Few unique values
    ["region"] = "westus2"
});

// Avoid: High cardinality
resource.AddAttributes(new Dictionary<string, object>
{
    ["request.id"] = Guid.NewGuid()  // DON'T: Many unique values
});
```

## Common Patterns

### Multi-Environment Configuration

```csharp
var environment = builder.Environment.EnvironmentName;

var resourceAttributes = environment switch
{
    "Production" => new Dictionary<string, object>
    {
        ["deployment.environment"] = "production",
        ["region"] = "westus2",
        ["team"] = "OrderManagement"
    },
    "Staging" => new Dictionary<string, object>
    {
        ["deployment.environment"] = "staging",
        ["region"] = "eastus",
        ["team"] = "OrderManagement"
    },
    _ => new Dictionary<string, object>
    {
        ["deployment.environment"] = "development",
        ["developer"] = Environment.UserName
    }
};

otel.ConfigureResource(resource =>
{
    resource
        .AddService("OrderService")
        .AddAttributes(resourceAttributes);
});
```

### Microservices Pattern

```csharp
// Each microservice sets its own service name
// Common attributes via configuration

otel.ConfigureResource(resource =>
{
    resource
        .AddService(
            serviceName: builder.Configuration["ServiceName"] ?? "UnknownService",
            serviceVersion: builder.Configuration["Version"] ?? "1.0.0")
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = builder.Configuration["Environment"] ?? "production",
            ["cluster"] = builder.Configuration["ClusterName"] ?? "default",
            ["namespace"] = builder.Configuration["K8sNamespace"] ?? "default"
        });
});
```

## Testing

```csharp
[Fact]
public void Resource_IncludesServiceName()
{
    var services = new ServiceCollection();
    
    services.AddApplicationInsightsTelemetry()
        .ConfigureOpenTelemetryBuilder(otel =>
        {
            otel.ConfigureResource(resource =>
            {
                resource.AddService("TestService", "1.0.0");
            });
        });
    
    // Verify resource configuration
    var provider = services.BuildServiceProvider();
    // ... validation logic
}
```

## See Also

- [resource-detector.md](../concepts/resource-detector.md)
- [setting-cloud-role-name.md](../common-scenarios/setting-cloud-role-name.md)
- [OpenTelemetry Resource Semantic Conventions](https://opentelemetry.io/docs/specs/semconv/resource/)
- [OpenTelemetry .NET Resource API](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Api)
