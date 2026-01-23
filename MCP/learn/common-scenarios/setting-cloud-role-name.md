# Setting Cloud Role Name in 3.x

**Category:** Common Scenario  
**Applies to:** Application Insights .NET SDK 3.x  
**Related:** [resource-detector.md](../concepts/resource-detector.md), [context-to-resource.md](../mappings/context-to-resource.md)

## Overview

Cloud Role Name (`Cloud.RoleName` in Azure Monitor) identifies your service in Application Map and other views. In 3.x, this is set via **Resource** using `service.name` attribute.

## Quick Solution

```csharp
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.ConfigureResource(resource =>
            resource.AddService(serviceName: "MyServiceName"));
    });
```

**Result in Azure Monitor:**
- `Cloud.RoleName` = "MyServiceName"
- `Cloud.RoleInstance` = hostname (automatic)

## Method 1: Hardcoded Service Name

```csharp
// Program.cs or Startup.cs
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.ConfigureResource(resource =>
            resource.AddService(
                serviceName: "OrderProcessingService",
                serviceVersion: "1.0.0",
                serviceInstanceId: Environment.MachineName));
    });
```

## Method 2: From Configuration

```json
// appsettings.json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=...",
    "CloudRoleName": "OrderProcessingService"
  }
}
```

```csharp
// Program.cs
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        var configuration = builder.Services.BuildServiceProvider()
            .GetRequiredService<IConfiguration>();
        var roleName = configuration["ApplicationInsights:CloudRoleName"];
        
        builder.ConfigureResource(resource =>
            resource.AddService(serviceName: roleName ?? "DefaultService"));
    });
```

## Method 3: Custom Resource Detector

```csharp
public class CloudRoleNameDetector : IResourceDetector
{
    private readonly string _roleName;
    
    public CloudRoleNameDetector(IConfiguration configuration)
    {
        _roleName = configuration["ApplicationInsights:CloudRoleName"]
                    ?? Assembly.GetExecutingAssembly().GetName().Name
                    ?? "UnknownService";
    }
    
    public Resource Detect()
    {
        return ResourceBuilder.CreateEmpty()
            .AddService(
                serviceName: _roleName,
                serviceVersion: GetVersion(),
                serviceInstanceId: Environment.MachineName)
            .Build();
    }
    
    private static string GetVersion()
    {
        return Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "1.0.0";
    }
}

// Registration
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        var configuration = builder.Services.BuildServiceProvider()
            .GetRequiredService<IConfiguration>();
        builder.ConfigureResource(resource =>
            resource.AddDetector(new CloudRoleNameDetector(configuration)));
    });
```

## Method 4: Environment-Based (Kubernetes)

```csharp
public class KubernetesRoleNameDetector : IResourceDetector
{
    public Resource Detect()
    {
        // Kubernetes sets HOSTNAME to pod name
        var podName = Environment.GetEnvironmentVariable("HOSTNAME");
        var namespace_ = Environment.GetEnvironmentVariable("KUBERNETES_NAMESPACE") ?? "default";
        var deploymentName = Environment.GetEnvironmentVariable("KUBERNETES_DEPLOYMENT_NAME");
        
        // Use deployment name as service name, pod name as instance
        var serviceName = !string.IsNullOrEmpty(deploymentName)
            ? deploymentName
            : (!string.IsNullOrEmpty(namespace_) ? namespace_ : "default");
        
        return ResourceBuilder.CreateEmpty()
            .AddService(
                serviceName: serviceName,
                serviceInstanceId: podName ?? Environment.MachineName)
            .AddAttributes(new[]
            {
                new KeyValuePair<string, object>("k8s.pod.name", podName ?? "unknown"),
                new KeyValuePair<string, object>("k8s.namespace.name", namespace_),
                new KeyValuePair<string, object>("k8s.deployment.name", deploymentName ?? "unknown")
            })
            .Build();
    }
}
```

## Method 5: Azure App Service

```csharp
public class AppServiceRoleNameDetector : IResourceDetector
{
    public Resource Detect()
    {
        // Azure App Service sets WEBSITE_SITE_NAME
        var siteName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");
        var instanceId = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID");
        
        if (string.IsNullOrEmpty(siteName))
        {
            return Resource.Empty; // Not running in App Service
        }
        
        return ResourceBuilder.CreateEmpty()
            .AddService(
                serviceName: siteName,
                serviceInstanceId: instanceId ?? Environment.MachineName)
            .AddAttributes(new[]
            {
                new KeyValuePair<string, object>("azure.app.service.name", siteName),
                new KeyValuePair<string, object>("cloud.provider", "azure"),
                new KeyValuePair<string, object>("cloud.platform", "azure_app_service")
            })
            .Build();
    }
}
```

## Migration from 2.x

### 2.x: ITelemetryInitializer

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
        if (string.IsNullOrEmpty(telemetry.Context.Cloud.RoleName))
        {
            telemetry.Context.Cloud.RoleName = _roleName;
        }
    }
}

services.AddSingleton<ITelemetryInitializer, CloudRoleNameInitializer>();
```

### 3.x: Resource

```csharp
public class CloudRoleNameDetector : IResourceDetector
{
    private readonly string _roleName;
    
    public CloudRoleNameDetector(IConfiguration configuration)
    {
        _roleName = configuration["ApplicationInsights:CloudRoleName"] 
                    ?? "DefaultService";
    }
    
    public Resource Detect()
    {
        // Called ONCE at startup (efficient)
        return ResourceBuilder.CreateEmpty()
            .AddService(serviceName: _roleName)
            .Build();
    }
}

services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        var configuration = builder.Services.BuildServiceProvider()
            .GetRequiredService<IConfiguration>();
        builder.ConfigureResource(resource =>
            resource.AddDetector(new CloudRoleNameDetector(configuration)));
    });
```

**Efficiency Gain:** Resource set once vs. per-telemetry-item (~99.9% reduction in overhead).

## Setting Both RoleName and RoleInstance

```csharp
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.ConfigureResource(resource =>
            resource.AddService(
                serviceName: "MyService",           // → Cloud.RoleName
                serviceInstanceId: "instance-123")); // → Cloud.RoleInstance
    });
```

**Default Instance ID:** If not specified, defaults to hostname.

## Multiple Services (Microservices)

```csharp
// Service A
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.ConfigureResource(resource =>
            resource.AddService("OrderService"));
    });

// Service B
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.ConfigureResource(resource =>
            resource.AddService("PaymentService"));
    });

// Service C
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.ConfigureResource(resource =>
            resource.AddService("ShippingService"));
    });
```

**Result:** Three separate nodes in Application Map.

## Verification

### Check in Azure Monitor

1. Navigate to Application Insights in Azure Portal
2. Go to "Application Map"
3. Verify your service name appears as expected
4. Check "Cloud role name" dimension in logs:

```kusto
requests
| take 10
| project timestamp, name, cloud_RoleName, cloud_RoleInstance
```

### Check in Code

```csharp
// In a processor
public override void OnEnd(Activity activity)
{
    var resource = Activity.Current?.Source.GetResource();
    var serviceName = resource?.Attributes
        .FirstOrDefault(kv => kv.Key == "service.name").Value;
    
    Console.WriteLine($"Service Name: {serviceName}");
}
```

## Common Issues

### Issue 1: Cloud.RoleName is Empty

**Cause:** Resource not configured or service.name not set.

**Solution:**
```csharp
services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.ConfigureResource(resource =>
        {
            // Ensure service.name is set
            resource.AddService("MyService");
        });
    });
```

### Issue 2: Cloud.RoleName Shows Assembly Name

**Cause:** OpenTelemetry defaults to assembly name if service.name not explicitly set.

**Solution:** Always explicitly set `serviceName`.

### Issue 3: Multiple Services Show Same RoleName

**Cause:** All services using same hardcoded name or same configuration.

**Solution:** Use different names per service or environment-based detection.

## Best Practices

1. **Use Configuration:** Store service name in appsettings.json for easy changes
2. **Include Version:** Set `serviceVersion` to track deployments
3. **Use Resource Detectors:** Leverage built-in detectors for cloud environments
4. **Unique Names:** Ensure each microservice has a unique Cloud.RoleName
5. **Test Early:** Verify Application Map shows correct service topology

## Complete Example

```csharp
// Startup.cs or Program.cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = Configuration["ApplicationInsights:ConnectionString"];
    })
    .ConfigureOpenTelemetryBuilder(builder =>
    {
        builder.ConfigureResource(resource =>
        {
            // Get service name from config
            var serviceName = Configuration["ApplicationInsights:CloudRoleName"]
                              ?? Assembly.GetExecutingAssembly().GetName().Name
                              ?? "UnknownService";
            
            // Get version from assembly
            var version = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "1.0.0";
            
            // Set service identity
            resource.AddService(
                serviceName: serviceName,
                serviceVersion: version,
                serviceInstanceId: Environment.MachineName);
            
            // Add environment attribute
            resource.AddAttributes(new[]
            {
                new KeyValuePair<string, object>("deployment.environment", 
                    Configuration["Environment"] ?? "Unknown")
            });
        });
    });
}
```

```json
// appsettings.json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=...",
    "CloudRoleName": "OrderProcessingService"
  },
  "Environment": "Production"
}
```

## See Also

- [resource-detector.md](../concepts/resource-detector.md) - Resource detector concept
- [IResourceDetector.Detect](../api-reference/IResourceDetector/Detect.md) - API reference
- [context-to-resource.md](../mappings/context-to-resource.md) - TelemetryContext mapping
- [ConfigureOpenTelemetryBuilder](../api-reference/TelemetryConfiguration/ConfigureOpenTelemetryBuilder.md) - Configuration API
