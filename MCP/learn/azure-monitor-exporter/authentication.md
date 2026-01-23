# Authentication Methods

**Category:** Azure Monitor Exporter  
**Applies to:** Authenticating to Azure Monitor  
**Related:** [configuration-options.md](configuration-options.md)

## Overview

Azure Monitor OpenTelemetry Exporter supports multiple authentication methods: connection strings (with instrumentation keys), Azure AD authentication, and managed identities.

## Connection String Authentication (Default)

The simplest method using an instrumentation key embedded in the connection string.

### Basic Setup

```csharp
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://westus2-1.in.applicationinsights.azure.com/";
});
```

### From Configuration

**appsettings.json:**

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://westus2-1.in.applicationinsights.azure.com/"
  }
}
```

**Program.cs:**

```csharp
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
});
```

### From Environment Variable

```bash
# Set environment variable
export APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=..."
```

```csharp
// Automatically discovered from environment
builder.Services.AddApplicationInsightsTelemetry();
```

## Azure AD Authentication (Recommended for Production)

Use Azure Active Directory for enhanced security and RBAC.

### Prerequisites

1. Application Insights resource with AAD enabled
2. Service principal or managed identity with "Monitoring Metrics Publisher" role
3. Azure.Identity NuGet package

```bash
dotnet add package Azure.Identity
```

### Using DefaultAzureCredential

Works with multiple credential types (managed identity, Visual Studio, Azure CLI, etc.).

```csharp
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;

builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        otel.UseAzureMonitor(options =>
        {
            options.ConnectionString = "IngestionEndpoint=https://westus2-1.in.applicationinsights.azure.com/";
            options.Credential = new DefaultAzureCredential();
        });
    });
```

**Credential Chain (DefaultAzureCredential tries in order):**
1. Environment variables (AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_CLIENT_SECRET)
2. Managed Identity
3. Visual Studio credential
4. Azure CLI credential
5. Azure PowerShell credential

### Using Managed Identity (Azure VM, App Service, AKS)

#### System-Assigned Managed Identity

```csharp
using Azure.Identity;

builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        otel.UseAzureMonitor(options =>
        {
            options.ConnectionString = "IngestionEndpoint=https://westus2-1.in.applicationinsights.azure.com/";
            options.Credential = new ManagedIdentityCredential();
        });
    });
```

#### User-Assigned Managed Identity

```csharp
builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        otel.UseAzureMonitor(options =>
        {
            options.ConnectionString = "IngestionEndpoint=https://westus2-1.in.applicationinsights.azure.com/";
            options.Credential = new ManagedIdentityCredential(
                clientId: "00000000-0000-0000-0000-000000000000");
        });
    });
```

### Using Service Principal

For applications running outside Azure.

```csharp
using Azure.Identity;

builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        otel.UseAzureMonitor(options =>
        {
            options.ConnectionString = "IngestionEndpoint=https://westus2-1.in.applicationinsights.azure.com/";
            options.Credential = new ClientSecretCredential(
                tenantId: "your-tenant-id",
                clientId: "your-client-id",
                clientSecret: "your-client-secret");
        });
    });
```

**Better: Use configuration:**

```json
{
  "AzureAd": {
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret"
  },
  "ApplicationInsights": {
    "ConnectionString": "IngestionEndpoint=https://westus2-1.in.applicationinsights.azure.com/"
  }
}
```

```csharp
var azureAdConfig = builder.Configuration.GetSection("AzureAd");

builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        otel.UseAzureMonitor(options =>
        {
            options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
            options.Credential = new ClientSecretCredential(
                azureAdConfig["TenantId"],
                azureAdConfig["ClientId"],
                azureAdConfig["ClientSecret"]);
        });
    });
```

## Environment-Specific Authentication

### Development (Connection String)

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=..."
  }
}
```

### Production (Managed Identity)

```csharp
if (builder.Environment.IsProduction())
{
    builder.Services.AddApplicationInsightsTelemetry()
        .ConfigureOpenTelemetryBuilder(otel =>
        {
            otel.UseAzureMonitor(options =>
            {
                options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
                options.Credential = new DefaultAzureCredential();
            });
        });
}
else
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    });
}
```

## Azure Key Vault Integration

Store connection string or credentials in Key Vault.

### Setup

```bash
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
```

### Program.cs

```csharp
var keyVaultEndpoint = builder.Configuration["KeyVault:Endpoint"];

builder.Configuration.AddAzureKeyVault(
    new Uri(keyVaultEndpoint),
    new DefaultAzureCredential());

// Connection string loaded from Key Vault
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights-ConnectionString"];
});
```

## RBAC Setup for AAD Authentication

### Required Role

Application needs **"Monitoring Metrics Publisher"** role on Application Insights resource.

### Azure CLI Commands

```bash
# Get Application Insights resource ID
AI_RESOURCE_ID=$(az monitor app-insights component show \
    --app myapp \
    --resource-group mygroup \
    --query id -o tsv)

# Assign role to managed identity
az role assignment create \
    --assignee <managed-identity-client-id> \
    --role "Monitoring Metrics Publisher" \
    --scope $AI_RESOURCE_ID

# Or assign to service principal
az role assignment create \
    --assignee <service-principal-client-id> \
    --role "Monitoring Metrics Publisher" \
    --scope $AI_RESOURCE_ID
```

### PowerShell Commands

```powershell
# Get Application Insights resource
$aiResource = Get-AzApplicationInsights -ResourceGroupName "mygroup" -Name "myapp"

# Assign role to managed identity
New-AzRoleAssignment `
    -ObjectId <managed-identity-object-id> `
    -RoleDefinitionName "Monitoring Metrics Publisher" `
    -Scope $aiResource.Id
```

## Testing Authentication

### Local Development

```csharp
// Use Azure CLI credential for local testing
builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        otel.UseAzureMonitor(options =>
        {
            options.ConnectionString = "IngestionEndpoint=...";
            options.Credential = new AzureCliCredential();
        });
    });
```

Login with Azure CLI:

```bash
az login
az account set --subscription <subscription-id>
```

### Unit Testing

Mock credential for testing:

```csharp
public class MockCredential : TokenCredential
{
    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1));
    }

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return new ValueTask<AccessToken>(GetToken(requestContext, cancellationToken));
    }
}

// In test
builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        otel.UseAzureMonitor(options =>
        {
            options.Credential = new MockCredential();
        });
    });
```

## Migration from 2.x

### Before (2.x): Instrumentation Key Only

```csharp
services.AddApplicationInsightsTelemetry(options =>
{
    options.InstrumentationKey = "00000000-0000-0000-0000-000000000000";
});
```

### After (3.x): Connection String or AAD

```csharp
// Option 1: Connection string (compatible)
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000";
});

// Option 2: AAD authentication (recommended)
builder.Services.AddApplicationInsightsTelemetry()
    .ConfigureOpenTelemetryBuilder(otel =>
    {
        otel.UseAzureMonitor(options =>
        {
            options.ConnectionString = "IngestionEndpoint=https://westus2-1.in.applicationinsights.azure.com/";
            options.Credential = new DefaultAzureCredential();
        });
    });
```

## Troubleshooting

### Authentication Errors

```csharp
// Enable logging to diagnose credential issues
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Debug);
});

// DefaultAzureCredential will log each credential attempt
```

### Managed Identity Not Working

1. Verify managed identity is enabled on resource
2. Check RBAC role assignment
3. Verify endpoint is correct
4. Check network connectivity to Application Insights endpoint

### Connection String vs AAD

| Method | Use Case | Security | Setup Complexity |
|--------|----------|----------|------------------|
| Connection String | Development, simple scenarios | Lower (key in config) | Low |
| AAD + Managed Identity | Production in Azure | High (no secrets) | Medium |
| AAD + Service Principal | Production outside Azure | Medium (secret required) | Medium |

## Best Practices

1. **Use Managed Identity in Azure:** No secrets to manage
2. **Use DefaultAzureCredential:** Works across environments
3. **Store secrets in Key Vault:** Never in source control
4. **Use AAD in production:** Better security and auditing
5. **Test locally with Azure CLI:** Matches production auth

## See Also

- [configuration-options.md](configuration-options.md)
- [Azure Identity Library](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/identity-readme)
- [Managed Identity Documentation](https://learn.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/)
- [DefaultAzureCredential](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential)
