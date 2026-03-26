# AGENTS.md

## Overview

This is the .NET SDK for Azure Monitor / Application Insights, built on OpenTelemetry. It provides the `Microsoft.ApplicationInsights` family of NuGet packages for instrumenting .NET applications with distributed tracing, metrics, and logging.

## Repository Structure

| Directory | Contents |
|---|---|
| `BASE/` | Core SDK (`Microsoft.ApplicationInsights`) |
| `NETCORE/` | ASP.NET Core + Worker Service SDKs |
| `WEB/` | Classic ASP.NET Web SDK |
| `LOGGING/` | NLog logging adapter |
| `examples/` | 7 runnable example apps |
| `docs/` | Conceptual documentation |
| `skills/` | AI-assisted instrumentation skills (see below) |

## Build & Test

```bash
dotnet build Everything.sln
dotnet test Everything.sln
```

Platform-specific filters: `Everything.Windows.slnf`, `Everything.Linux.slnf`.

Key conventions:
- Central package management via `Directory.Packages.props`
- Public API tracking in `.publicApi/` (update `PublicAPI.Unshipped.txt` when adding public APIs)
- Strong-name signing required

## AI-Assisted Instrumentation Skills

The `skills/applicationinsights-setup/` folder contains a portable skill that helps developers set up, migrate, or enhance Application Insights 3.x SDK in their .NET applications.

### What It Does

- **Greenfield**: Guides new setup for ASP.NET Core, Worker Service, ASP.NET Classic, and Console apps using Application Insights 3.x SDK
- **Migration**: Walks through 2.x → 3.x upgrade with code change detection and step-by-step fixes
- **Enhancement**: Adds Entity Framework, Redis, SQL, HTTP, OTLP, custom processors, activities, logging, and custom metrics to apps already on 3.x

### Installation by Agent

**GitHub Copilot:**
Copilot natively supports Agent Skills. Copy the skill folder to your project:
```bash
cp -r skills/applicationinsights-setup .github/skills/applicationinsights-setup
```
Copilot discovers skills in `.github/skills/` (and `.claude/skills/`) automatically. Works with Copilot coding agent, GitHub Copilot CLI, and agent mode in VS Code.

**Cursor:**
Cursor natively supports Agent Skills. Copy the skill folder to your project:
```bash
cp -r skills/applicationinsights-setup .cursor/skills/applicationinsights-setup
```
Cursor discovers skills in `.cursor/skills/`, `.agents/skills/`, `.claude/skills/`, and `.codex/skills/` automatically.

**Claude Code:**
Claude Code natively supports Agent Skills. Copy the skill folder to your project:
```bash
cp -r skills/applicationinsights-setup .claude/skills/applicationinsights-setup
```
Or add as an additional directory without copying:
```bash
claude --add-dir /path/to/ApplicationInsights-dotnet/skills/applicationinsights-setup
```

**Codex:**
```bash
cp -r skills/applicationinsights-setup .agents/skills/applicationinsights-setup
```
Codex scans `.agents/skills/` from the current directory up to the repo root.

**Other agents:**
Copy the `skills/applicationinsights-setup/` folder into your project's agent skills directory (commonly `.agents/skills/`). If your agent doesn't support the Agent Skills standard, copy the `references/` markdown files into whatever instruction mechanism your agent uses.

### Usage

Once installed, ask your AI coding agent:
- "Add Application Insights to my app"
- "Migrate from Application Insights 2.x to 3.x"
- "Add Redis monitoring to my app"

The skill automatically detects your app type and instrumentation state, then provides the correct guidance.
