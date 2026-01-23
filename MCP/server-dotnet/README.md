# Application Insights Migration MCP Server (.NET)

MCP (Model Context Protocol) server for assisting with Application Insights SDK migration from 2.x to 3.x (OpenTelemetry-based) and fresh ASP.NET Core onboarding.

## Features

- **Project Analysis**: Detects Application Insights SDK version and determines migration scenario
- **Decision Engine**: Provides migration strategy and implementation steps
- **Validation**: Verifies correct implementation of fresh onboarding or migration
- **Learning Library Integration**: Serves 50+ migration guides and examples

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                   MCP Server (C#)                        │
├─────────────────────────────────────────────────────────┤
│  ProjectAnalyzer     │  Scans .csproj for packages      │
│                      │  Detects legacy patterns          │
├──────────────────────┼──────────────────────────────────┤
│  DecisionEngine      │  Determines scenario              │
│                      │  Generates implementation steps   │
├──────────────────────┼──────────────────────────────────┤
│  ValidationEngine    │  Validates package installation   │
│                      │  Checks for legacy patterns       │
└─────────────────────────────────────────────────────────┘
```

## MCP Tools

### 1. `analyze_project`
Analyzes an ASP.NET Core project to determine migration scenario.

**Input:**
```json
{
  "project_path": "/path/to/project"
}
```

**Output:**
```json
{
  "analysis": {
    "app_type": "aspnetcore",
    "has_legacy_ai_sdk": true,
    "has_opentelemetry": false,
    "legacy_packages": ["Microsoft.ApplicationInsights.AspNetCore"],
    "legacy_patterns": ["AddApplicationInsightsTelemetry", "ITelemetryInitializer"]
  },
  "scenario": "migration_2x_to_3x",
  "next_steps": [
    {
      "step": 1,
      "action": "analyze_legacy_code",
      "description": "Identify legacy Application Insights patterns"
    }
  ],
  "learning_resources": [
    "breaking-changes/TelemetryClient/parameterless-constructor.md",
    "transformations/ITelemetryInitializer/to-activity-processor.md"
  ]
}
```

### 2. `get_guide`
Retrieves a specific migration guide from the learning library.

**Input:**
```json
{
  "guide_path": "common-scenarios/fresh-aspnetcore-onboarding.md"
}
```

**Output:**
```json
{
  "path": "common-scenarios/fresh-aspnetcore-onboarding.md",
  "content": "# Fresh ASP.NET Core Onboarding\n\n..."
}
```

### 3. `validate_implementation`
Validates that migration or fresh onboarding was implemented correctly.

**Input:**
```json
{
  "project_path": "/path/to/project",
  "scenario": "fresh_onboarding"
}
```

**Output:**
```json
{
  "is_valid": true,
  "issues": [],
  "warnings": ["Connection string not found in environment variables"]
}
```

### 4. `get_code_template`
Gets code templates for common migration patterns.

**Input:**
```json
{
  "template_name": "minimal-program-cs"
}
```

**Output:**
```json
{
  "template_name": "minimal-program-cs",
  "code": "var builder = WebApplication.CreateBuilder(args);\n..."
}
```

## Scenarios Supported

### Fresh Onboarding
**Detection:** No Application Insights SDK, no OpenTelemetry

**Actions:**
1. Install `Azure.Monitor.OpenTelemetry.AspNetCore`
2. Configure connection string
3. Add `UseAzureMonitor()` to Program.cs
4. Verify telemetry flow

### Migration (2.x → 3.x)
**Detection:** Has `Microsoft.ApplicationInsights.*` packages

**Actions:**
1. Analyze legacy code patterns
2. Remove legacy packages
3. Install OpenTelemetry packages
4. Transform code patterns:
   - `ITelemetryInitializer` → `BaseProcessor<Activity>`
   - `ITelemetryProcessor` → `BaseProcessor<Activity>`
   - `TelemetryConfiguration.Active` → Dependency Injection
   - `AddApplicationInsightsTelemetry()` → `UseAzureMonitor()`

### Already Configured
**Detection:** Already has OpenTelemetry packages

**Actions:** None needed

## Building & Running

```bash
cd MCP/server-dotnet
dotnet build
dotnet run -- /path/to/learning/library
```

## Testing

Test on the ApplicationInsightsDemo project:
```bash
dotnet run -- ../learn
# Then use MCP client to call analyze_project with ApplicationInsightsDemo path
```

## Dependencies

- .NET 8.0
- ModelContextProtocol NuGet package
- System.Xml.Linq (for .csproj parsing)

## Learning Library

The server integrates with 50+ markdown documents covering:
- Breaking changes (33 docs)
- Transformation guides (8 docs)
- API reference (Activity, ActivityEvent, ActivityLink, etc.)
- Common scenarios (fresh onboarding, migration)
- Examples (code samples with source attribution)
