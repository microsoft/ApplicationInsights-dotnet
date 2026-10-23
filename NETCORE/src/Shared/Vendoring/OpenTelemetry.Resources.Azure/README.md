# Vendored: OpenTelemetry.Resources.Azure

This folder contains a vendored copy of the
[OpenTelemetry.Resources.Azure](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/Resources.Azure-1.20.1-beta.1/src/OpenTelemetry.Resources.Azure)
resource detectors. The upstream package is still in beta, so the Application Insights SDK carries the source rather
than taking a package dependency.

The files are compiled into `Microsoft.ApplicationInsights.AspNetCore` and `Microsoft.ApplicationInsights.WorkerService`
through [`Shared.projitems`](../../Shared.projitems), and into `Microsoft.ApplicationInsights.Web` through explicit
`<Compile Include>` links in [`WEB/Src/Web/Web/Web.csproj`](../../../../../WEB/Src/Web/Web/Web.csproj). When adding or
removing a file, update **both**.

## Source

| | |
|---|---|
| Repository | [open-telemetry/opentelemetry-dotnet-contrib](https://github.com/open-telemetry/opentelemetry-dotnet-contrib) |
| Release tag | [`Resources.Azure-1.20.1-beta.1`](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/releases/tag/Resources.Azure-1.20.1-beta.1) |
| Commit | `cae14fd3d642fb7bc1c114116b0fff45a11698e8` |

All files from `src/OpenTelemetry.Resources.Azure/*.cs`, plus the following files from `src/Shared/`, are copied into
this folder:

| Upstream `src/Shared` file | Status |
|---|---|
| `ExceptionExtensions.cs` | Synced from the tag. |
| `HttpClientHelpers.cs` | Synced from the tag. |
| `ResourceSemanticConventions.cs` | Synced from the tag. |
| `SchemaUrls.cs` | Synced from the tag. |
| `StopwatchExtensions.cs` | Synced from the tag. |
| `Guard.cs` | **Not re-synced.** The existing copy already provides every member the detectors use (`ThrowIfNull`, `ThrowIfOutOfRange`). |

## Deviations from upstream

Keep this list up to date. Every change made to the upstream files must be listed here so that the next sync can
re-apply it. Apart from the items below, the code is identical to upstream.

| File | Deviation | Reason |
|---|---|---|
| All files | Namespace changed to `Microsoft.ApplicationInsights.Shared.Vendoring.OpenTelemetry.Resources.Azure` (`AzureResourceBuilderExtensions` uses `Microsoft.ApplicationInsights.Shared.Vendoring.OpenTelemetry.Resources`). | Avoids type conflicts with the `OpenTelemetry.Resources.Azure` package and other OpenTelemetry assemblies. |
| All files | File-scoped namespaces converted to block-scoped namespaces with `using` directives inside the namespace, explicit `using` directives added (no implicit usings), `#nullable enable` added where needed, and references to the `OpenTelemetry` namespace prefixed with `global::`. | Repository StyleCop rules and build settings; `global::` is required because `OpenTelemetry` would otherwise resolve to the vendored namespace. |
| `src/Shared/*` files | Moved from their upstream namespaces (`OpenTelemetry.Internal`, `System.Net.Http`, `System.Diagnostics`) into the vendored namespace, and `using OpenTelemetry.Internal;` removed. | Keeps every vendored type in one namespace and lets them resolve the vendored `Guard`. |
| Detectors | `Internal.SchemaUrls.Get(...)` changed to `SchemaUrls.Get(...)`. | Follows from moving `SchemaUrls` into the vendored namespace. |
| `AzureResourceBuilderExtensions.cs` | `public static class` changed to `internal static class`. | The detectors are an implementation detail and must not leak into the public API. |
| `AzureResourcesEventSource.cs` | EventSource name changed from `OpenTelemetry-Resources-Azure` to a per-package name selected by compilation symbol: `Microsoft-ApplicationInsights-AspNetCore-Resources-Azure` (`AI_ASPNETCORE_WEB`), `Microsoft-ApplicationInsights-WorkerService-Resources-Azure` (`AI_ASPNETCORE_WORKER`), `Microsoft-ApplicationInsights-Web-Resources-Azure` (`AI_CLASSIC_WEB`). | The same source is compiled into several assemblies that can be loaded together; distinct names avoid EventSource name collisions between them, other vendored copies, and the `OpenTelemetry.Resources.Azure` package. |
| [`.editorconfig`](.editorconfig) | Disables StyleCop `SA1000` and `SA1010` for this folder only. | The StyleCop version pinned in this repository misreports target-typed `new(...)` and collection expressions that upstream uses. |

## Syncing

1. Pick the latest `Resources.Azure-*` release tag in opentelemetry-dotnet-contrib.
2. Copy `src/OpenTelemetry.Resources.Azure/*.cs` and the `src/Shared` files listed above into this folder. Check the
   upstream `.csproj` for any new `src/Shared` includes.
3. Re-apply every deviation listed above and update the [Source](#source) table.
4. Update [`Shared.projitems`](../../Shared.projitems) and
   [`WEB/Src/Web/Web/Web.csproj`](../../../../../WEB/Src/Web/Web/Web.csproj) for any added or removed file.
5. Review the upstream `CHANGELOG.md` for attribute name or value changes and call out any customer-visible change in
   the repository [`CHANGELOG.md`](../../../../../CHANGELOG.md) (all resource attributes are exported on the
   `_OTELRESOURCE_` metric).
6. Run `AzureResourceDetectorTests` (`NETCORE/test/Shared`) in `IntegrationTests.Tests` and
   `WorkerIntegrationTests.Tests`.
