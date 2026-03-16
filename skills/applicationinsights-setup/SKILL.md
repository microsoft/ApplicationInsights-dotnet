---
name: applicationinsights-setup
description: >
  Sets up, migrates, or enhances Azure Monitor Application Insights in .NET
  applications. Detects application type and existing instrumentation
  automatically. Use when adding Application Insights, Azure Monitor,
  observability, or telemetry to ASP.NET Core, Worker Service, ASP.NET Classic,
  or Console apps, upgrading from Application Insights 2.x to 3.x, migrating
  from TelemetryClient to OpenTelemetry, or adding Entity Framework, Redis, SQL,
  or OTLP telemetry to apps already using Application Insights.
metadata:
  author: Microsoft
  version: 1.0.0
---

# Application Insights Setup

This skill detects your .NET application type and instrumentation state, then guides you through the correct setup, migration, or enhancement path.

## Step 1 — Detect Application Type

Find all `.csproj` files (also `.fsproj`, `.vbproj`) in the workspace. If multiple non-library projects exist, ask the user which one to instrument before proceeding. Also check for `global.json` as a .NET workspace indicator.

First, check the `<Project Sdk="...">` attribute in the `.csproj`:

### SDK-style projects (modern format — has `Sdk` attribute)

| Signal | App Type |
|---|---|
| `Sdk="Microsoft.NET.Sdk.Web"` | **ASP.NET Core** |
| `Sdk="Microsoft.NET.Sdk.Worker"` | **Worker Service** |
| `Microsoft.Azure.Functions.Worker` or `Microsoft.NET.Sdk.Functions` in PackageReference | **Azure Functions** — not supported by this skill; refer to Azure Functions monitoring docs |
| `Sdk="Microsoft.NET.Sdk"` with `<OutputType>Exe</OutputType>` | **Console App** |
| `Sdk="Microsoft.NET.Sdk"` with `<OutputType>Library</OutputType>` or no OutputType | **Library** — skip; libraries are not instrumented directly |

For ASP.NET Core / Worker Service, read `Program.cs` to confirm hosting pattern:
- `WebApplication.CreateBuilder` → ASP.NET Core minimal APIs
- `Host.CreateDefaultBuilder` or `Host.CreateApplicationBuilder` → Generic Host
- `CreateWebHostBuilder` / `WebHost.CreateDefaultBuilder` → Legacy ASP.NET Core host

### Legacy projects (no `Sdk` attribute — old .csproj format)

If the `<Project>` element has no `Sdk` attribute, this is a legacy .NET Framework project. Detect the type using file and reference patterns:

| Signal (ANY ONE is sufficient) | App Type |
|---|---|
| `Web.config` with `<system.web>` section | **ASP.NET Classic** |
| `System.Web` in assembly references (`<Reference Include="System.Web" />`) | **ASP.NET Classic** |
| `Microsoft.AspNet.*` packages in `packages.config` | **ASP.NET Classic** |
| `.svc` files OR `System.ServiceModel` reference OR `[ServiceContract]` attributes | **WCF Service** — not supported by this skill; manual onboarding required |
| `Microsoft.Owin` or `Owin` in `packages.config`, or `IAppBuilder`/`IOwinContext` usage | **OWIN App** — not supported by this skill; manual onboarding required |
| `<OutputType>Exe</OutputType>` or `<OutputType>WinExe</OutputType>` with no web signals | **Console App** |

For ASP.NET Classic, further sub-type (all use the same setup):
- `Microsoft.AspNet.Mvc` package + `Controllers/` folder → **ASP.NET MVC**
- `.aspx` or `.ascx` files → **ASP.NET WebForms**
- Otherwise → **ASP.NET Classic (generic)**

**Entry point**: ASP.NET Classic uses `Global.asax.cs` as its entry point (not `Program.cs`).

## Step 2 — Detect Existing Instrumentation

Check multiple sources for evidence of existing instrumentation:

**Source 1 — Package references**: Scan `PackageReference` nodes in `.csproj` (check both `Version` and `VersionOverride` attributes for central package management). For legacy projects, scan `<package>` elements in `packages.config`. Version check: major version ≥ 3 → target version; handles wildcards (`3.*`), pre-release suffixes, and `v` prefix.

**Source 2 — Config files**: Check for `applicationinsights.config` (its presence indicates existing Classic SDK). Scan `appsettings*.json` for `InstrumentationKey` or `ApplicationInsights` sections.

**Detection priority** (if multiple types found): Azure Monitor Distro > Application Insights SDK > plain OpenTelemetry.

| Package Found | Version | State |
|---|---|---|
| `Azure.Monitor.OpenTelemetry.AspNetCore` or `Azure.Monitor.OpenTelemetry.Exporter` | any | **Already on Distro** → go to Enhancement |
| `Microsoft.ApplicationInsights.AspNetCore` | ≥ 3.0 | **Already on 3.x** → go to Enhancement |
| `Microsoft.ApplicationInsights.AspNetCore` | < 3.0 | **Brownfield 2.x** → go to Migration |
| `Microsoft.ApplicationInsights.WorkerService` | ≥ 3.0 | **Already on 3.x** → go to Enhancement |
| `Microsoft.ApplicationInsights.WorkerService` | < 3.0 | **Brownfield 2.x** → go to Migration |
| `Microsoft.ApplicationInsights.Web` | ≥ 3.0 | **Already on 3.x** → go to Enhancement |
| `Microsoft.ApplicationInsights.Web` | < 3.0 | **Brownfield 2.x** → go to Migration |
| `Microsoft.ApplicationInsights` (base only) | ≥ 3.0 | **Already on 3.x** → go to Enhancement |
| `Microsoft.ApplicationInsights` (base only) | < 3.0 | **Brownfield 2.x** → go to Migration (Console path) |
| `OpenTelemetry` / `OpenTelemetry.Api` / `OpenTelemetry.Extensions.Hosting` only (no AI SDK) | any | **OpenTelemetry only** → go to Enhancement (add Azure Monitor exporter) |
| None of the above, no `applicationinsights.config` | — | **Greenfield** → go to New Setup |

## Step 3 — Route to the Correct Guide

### Greenfield (No existing Application Insights)

Before making any code changes, read [references/opentelemetry-pipeline.md](references/opentelemetry-pipeline.md) and [references/azure-monitor-distro.md](references/azure-monitor-distro.md) to understand the architecture.

Then follow the guide for your app type:
- **ASP.NET Core** → read [references/aspnetcore-greenfield.md](references/aspnetcore-greenfield.md)
- **Worker Service** → read [references/workerservice-greenfield.md](references/workerservice-greenfield.md)
- **ASP.NET Classic** → read [references/aspnet-classic-greenfield.md](references/aspnet-classic-greenfield.md)
- **Console App** → read [references/console-setup.md](references/console-setup.md)

### Migration (Application Insights 2.x → 3.x)

First, read [references/opentelemetry-pipeline.md](references/opentelemetry-pipeline.md) to understand how 3.x differs from 2.x.

Then scan the codebase using the template in [references/analysis-template.md](references/analysis-template.md). This identifies which migration guides are relevant.

Based on findings, read the applicable migration references:
- **All migrations**: start with [references/code-migration.md](references/code-migration.md) (property changes, removed APIs)
- **ITelemetryInitializer found** → [references/initializer-migration.md](references/initializer-migration.md)
- **ITelemetryProcessor found** → [references/processor-migration.md](references/processor-migration.md)
- **Custom sampling** → [references/sampling-migration.md](references/sampling-migration.md)
- **ILogger / logging config** → [references/ilogger-migration.md](references/ilogger-migration.md)
- **TelemetryClient direct usage** → [references/custom-events-migration.md](references/custom-events-migration.md)
- **Dependency tracking config** → [references/dependency-tracking.md](references/dependency-tracking.md)
- **AAD / Token Credential authentication** → [references/aad-authentication-migration.md](references/aad-authentication-migration.md)
- **Live Metrics config** → [references/live-metrics-migration.md](references/live-metrics-migration.md)

If the scan finds NO code changes needed (only unchanged properties used, no removed APIs), the migration is just a package upgrade — read [references/no-code-change-migration.md](references/no-code-change-migration.md). Note: this path does NOT apply to Classic ASP.NET — Classic always requires config changes.

**Classic ASP.NET migration extras**: In addition to the references above, Classic ASP.NET brownfield migration requires:
- Rewrite `applicationinsights.config` to 3.x format: remove `<TelemetryInitializers>`, `<TelemetryModules>`, `<TelemetryProcessors>`, `<TelemetryChannel>` sections; replace `<InstrumentationKey>` with `<ConnectionString>`
- Update `Web.config`: remove `TelemetryCorrelationHttpModule`; verify `ApplicationInsightsHttpModule` and `TelemetryHttpModule` are present in `<system.webServer><modules>`
- Remove satellite packages in order: `Microsoft.ApplicationInsights.WindowsServer`, `.WindowsServer.TelemetryChannel`, `.DependencyCollector`, `.PerfCounterCollector`, `.Agent.Intercept`, `Microsoft.AspNet.TelemetryCorrelation`
- Replace `TelemetryConfiguration.Active` with `TelemetryConfiguration.CreateDefault()`
- Connection string goes in `ApplicationInsights.config` `<ConnectionString>` element (not `appsettings.json`)
- Use `config.ConfigureOpenTelemetryBuilder(otel => ...)` for all extensibility (DI-based methods are not available)

### Enhancement (Already on 3.x)

Ask the user what they want to add, then read the relevant reference.

> **DI vs Non-DI:** The enhancement references show DI patterns (`builder.Services.Configure*`). For Console or Classic ASP.NET apps that use `TelemetryConfiguration` directly, replace `builder.Services.ConfigureOpenTelemetryTracerProvider(tracing => ...)` with `config.ConfigureOpenTelemetryBuilder(otel => otel.WithTracing(tracing => ...))`. Each reference file includes a "Non-DI Usage" section.

- **Entity Framework Core** → [references/entity-framework.md](references/entity-framework.md)
- **Redis (StackExchange.Redis)** → [references/redis-cache.md](references/redis-cache.md)
- **SQL Client** → [references/sql-client.md](references/sql-client.md)
- **HTTP Client/Server Enrichment** → `OpenTelemetry.Instrumentation.Http` package, customize with enrichment callbacks and filters
- **OTLP Exporter** → [references/otlp-exporter.md](references/otlp-exporter.md)
- **Console Exporter** (dev/debug only) → `OpenTelemetry.Exporter.Console` package, `.AddConsoleExporter()` on each signal
- **Sampling Configuration** → [references/sampling-migration.md](references/sampling-migration.md) (applies to enhancement too)
- **Custom Processors** (filtering/enrichment) → [references/custom-processors.md](references/custom-processors.md)
- **Custom Metrics** → [references/custom-metrics.md](references/custom-metrics.md)

## Important Rules

1. **Learn first, act second**: Always read the relevant concept or migration reference BEFORE making code changes.
2. **Connection string, not instrumentation key**: Always use `ConnectionString`, never `InstrumentationKey`. The environment variable is `APPLICATIONINSIGHTS_CONNECTION_STRING`. For Classic ASP.NET, connection string goes in `ApplicationInsights.config`, not `appsettings.json`.
3. **Do not mix approaches**: Use either the Azure Monitor Distro (`Azure.Monitor.OpenTelemetry.AspNetCore`) OR the classic SDK (`Microsoft.ApplicationInsights.AspNetCore`), not both.
4. **Classic ASP.NET uses Package Manager Console**: Use `Install-Package` in Visual Studio, not `dotnet add package`.
5. **Verify after changes**: Build the project, run it, and confirm telemetry appears in Azure Portal (Live Metrics for immediate feedback, Transaction Search for 2-5 minute delayed data).
6. **Unsupported app types**: If the detected type is WCF Service, OWIN App, or Azure Functions, inform the user that automated setup is not available and point them to the Azure Monitor documentation for manual onboarding.
7. **Error handling**: If no `.csproj`/`.fsproj`/`.vbproj` files are found, report that no .NET project was detected rather than guessing.

## Examples

Example queries that should use this skill:
- "Add Application Insights to my app"
- "Set up Azure Monitor telemetry"
- "Add observability to my ASP.NET Core project"
- "Migrate from Application Insights 2.x to 3.x"
- "Upgrade my Application Insights SDK"
- "I'm getting deprecated API warnings from Application Insights"
- "Add Redis monitoring to my app that already has Application Insights"
- "How do I add Entity Framework telemetry"
- "Set up OTLP exporter alongside Application Insights"

## Troubleshooting

**No telemetry appearing**: Check that `APPLICATIONINSIGHTS_CONNECTION_STRING` is set or connection string is configured in `appsettings.json`. Verify the app targets `net8.0` or later for ASP.NET Core 3.x packages.

**Package version conflicts**: Ensure all `Microsoft.ApplicationInsights.*` packages are on the same major version. Do not mix 2.x and 3.x packages.

**Build errors after migration**: Check for removed APIs listed in [references/code-migration.md](references/code-migration.md). Common: `InstrumentationKey` property, `ITelemetryInitializer`, `ITelemetryProcessor`, `TrackPageView`.
