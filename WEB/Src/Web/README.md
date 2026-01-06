Microsoft.ApplicationInsights.Web
==============================

Application Insights Web SDK is available on [Nuget.org](http://www.nuget.org/packages/Microsoft.ApplicationInsights.Web/).

This package provides functionality for collecting server requests and exceptions. Server requests and associated metadata are collected automatically when this package is installed on a web application.

## Configuration

### Connection String
The connection string can be configured in the `applicationinsights.config` file:

```xml
<ApplicationInsights xmlns="http://schemas.microsoft.com/ApplicationInsights/2013/Settings">
  <ConnectionString>InstrumentationKey=YOUR_IKEY;IngestionEndpoint=https://ingestion-endpoint.applicationinsights.azure.com/</ConnectionString>
</ApplicationInsights>
```

### Azure Active Directory (AAD) Authentication

To use AAD authentication instead of instrumentation key-based authentication, configure the credential in your `Global.asax.cs` file using the `SetAzureTokenCredential` method:

```csharp
using Microsoft.ApplicationInsights.Extensibility;
using Azure.Identity;

public class Global : HttpApplication
{
    void Application_Start(object sender, EventArgs e)
    {
        var telemetryConfig = TelemetryConfiguration.CreateDefault();
        telemetryConfig.ConnectionString = "InstrumentationKey=YOUR_IKEY;IngestionEndpoint=https://ingestion-endpoint.applicationinsights.azure.com/";
        telemetryConfig.SetAzureTokenCredential(new DefaultAzureCredential());
    }
}
```

**Note:** You need to install the `Azure.Identity` NuGet package to use AAD authentication.

For more information, see the [Azure.Identity documentation](https://learn.microsoft.com/dotnet/api/overview/azure/identity-readme).

### Sampling, Offline Storage, and other configuration
Other options can also be set via the TelemetryConfiguration in the `Global.asax.cs` file. See [documentation for Microsoft.ApplicationInsights](../../../BASE/README.smd#azure-monitor-exporter-options).