# Application Insights for .NET Apps
[![Build And Test, SHIM](https://github.com/microsoft/ApplicationInsights-dotnet/actions/workflows/build-and-test-SHIM.yml/badge.svg)](https://github.com/microsoft/ApplicationInsights-dotnet/actions/workflows/build-and-test-SHIM.yml)

This is the .NET SDK for sending data to [Azure Monitor](https://docs.microsoft.com/azure/azure-monitor/overview) & [Application Insights](https://docs.microsoft.com/azure/azure-monitor/app/app-insights-overview).


- **Built on OpenTelemetry**: The SDK now uses [OpenTelemetry](https://opentelemetry.io/) as the underlying telemetry collection framework with the [Azure Monitor Exporter](https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/monitor/Azure.Monitor.OpenTelemetry.Exporter) for transmission.
- **OpenTelemetry Extensibility**: You can extend telemetry collection using standard OpenTelemetry patterns (Activity Processors, Resource Detectors, custom instrumentation).
- **Unified Observability**: Seamlessly integrates with the broader OpenTelemetry ecosystem, allowing you to send telemetry to multiple backends.

See [breaking changes](BreakingChanges.md) for more information on what has changed between versions 2.x and 3.x.

## Quick Start: Choose Your Path

Select the option that best describes your situation:

- **Building an ASP.NET Core web application?** → Use the [ASP.NET Core SDK](../NETCORE/Readme.md) for automatic instrumentation
- **Building a Worker Service, console app, or background service?** → Use the [Worker Service SDK](../NETCORE/WorkerService.md) for simplified configuration
- **Need the core TelemetryClient API for custom scenarios?** → Use the [base SDK](/BASE/README.md)
- **Need compatibility with NLog?** -> Use the [Logging SDK](/LOGGING/README.md)

### Understanding our SDK

We've gathered a list of concepts [here](docs/concepts.md).

## Contributing

We strongly welcome and encourage contributions to this project.
Please review our [Contributing guide](.github/CONTRIBUTING.md).

## Branches

- [main](https://github.com/Microsoft/ApplicationInsights-dotnet/tree/master) is the default branch for all development and releases.
- [2.x] (https://github.com/microsoft/ApplicationInsights-dotnet/tree/2.x) is the branch for the previous 2.x release.

## Releases

- [Releases](https://github.com/microsoft/ApplicationInsights-dotnet/releases)
- [Tags](https://github.com/microsoft/ApplicationInsights-dotnet/tags) all releases have a matching tag.

## NuGet packages

The following packages are published from this repository:

|  | Latest Official Release | Latest Official Release (including pre-release) |
|---|---|---|
| **Base SDKs** |  |  |
| - [Microsoft.ApplicationInsights](https://www.nuget.org/packages/Microsoft.ApplicationInsights/) | [![Nuget](https://img.shields.io/nuget/v/Microsoft.ApplicationInsights.svg)](https://www.nuget.org/packages/Microsoft.ApplicationInsights/) | [![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.svg)](https://www.nuget.org/packages/Microsoft.ApplicationInsights/) |
| **Auto Collectors (ASP.NET)** |  |  |  |
| - [Microsoft.ApplicationInsights.Web](https://www.nuget.org/packages/Microsoft.ApplicationInsights.Web/) | [![Nuget](https://img.shields.io/nuget/v/Microsoft.ApplicationInsights.Web.svg)](https://nuget.org/packages/Microsoft.ApplicationInsights.Web) | [![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.Web.svg)](https://nuget.org/packages/Microsoft.ApplicationInsights.Web) |
| **Auto Collectors (ASP.NET Core)** |  |  |
| - [Microsoft.ApplicationInsights.AspNetCore](https://www.nuget.org/packages/Microsoft.ApplicationInsights.AspNetCore/) | [![Nuget](https://img.shields.io/nuget/v/Microsoft.ApplicationInsights.AspNetCore.svg)](https://nuget.org/packages/Microsoft.ApplicationInsights.AspNetCore) | [![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.AspNetCore.svg)](https://nuget.org/packages/Microsoft.ApplicationInsights.AspNetCore) |
| **Auto Collectors (WorkerService, Console Application, etc.)** |  |  |
| - [Microsoft.ApplicationInsights.WorkerService](https://www.nuget.org/packages/Microsoft.ApplicationInsights.WorkerService/) | [![Nuget](https://img.shields.io/nuget/v/Microsoft.ApplicationInsights.WorkerService.svg)](https://nuget.org/packages/Microsoft.ApplicationInsights.WorkerService) | [![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.WorkerService.svg)](https://nuget.org/packages/Microsoft.ApplicationInsights.WorkerService) |
| **Logging Adapters** |  |  |
| - For `NLog`: [Microsoft.ApplicationInsights.NLogTarget](http://www.nuget.org/packages/Microsoft.ApplicationInsights.NLogTarget/) | [![Nuget](https://img.shields.io/nuget/v/Microsoft.ApplicationInsights.NLogTarget.svg)](https://www.nuget.org/packages/Microsoft.ApplicationInsights.NLogTarget/) | [![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.NLogTarget.svg)](https://www.nuget.org/packages/Microsoft.ApplicationInsights.NLogTarget/) |

Customers are encouraged to use the latest stable version as that is the version that will get fixes and updates. 
Beta packages are not recommended for use in production & support is limited. 
Upon release of the new stable version, the old Beta packages will be unlisted & the old minor version will be marked as deprecated. 
Application Insights follows the [Azure SDK Lifecycle & support policy](https://azure.github.io/azure-sdk/policies_support.html). 
(For example: When 2.20.0 is released, 2.20.0-beta1 will be unlisted and 2.19.0 will be deprecated.)

## Support

A guide on common troubleshooting topics is available [here](troubleshooting).

For immediate support relating to the Application Insights .NET SDK we encourage you to file an [Azure Support Request](https://docs.microsoft.com/azure/azure-portal/supportability/how-to-create-azure-support-request) with Microsoft Azure instead of filing a GitHub Issue in this repository. 
You can do so by going online to the [Azure portal](https://portal.azure.com/) and submitting a support request. Access to subscription management and billing support is included with your Microsoft Azure subscription, and technical support is provided through one of the [Azure Support Plans](https://azure.microsoft.com/support/plans/). For step-by-step guidance for the Azure portal, see [How to create an Azure support request](https://docs.microsoft.com/azure/azure-portal/supportability/how-to-create-azure-support-request). Alternatively, you can create and manage your support tickets programmatically using the [Azure Support ticket REST API](https://docs.microsoft.com/rest/api/support/).

