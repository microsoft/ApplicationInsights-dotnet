# Application Insights MCP Server

Model Context Protocol (MCP) server for Application Insights SDK migration and fresh OpenTelemetry onboarding.

## Overview

This MCP server provides intelligent guidance for:
- **Fresh ASP.NET Core onboarding** with OpenTelemetry (no existing Application Insights SDK)
- **Migration** from Application Insights SDK 2.x to 3.x (OpenTelemetry-based)
- **Validation** of implementation correctness

## Features

### Detection & Decision Engine
- Analyzes ASP.NET Core projects to detect scenarios
- Identifies legacy Application Insights SDK packages
- Detects existing OpenTelemetry configuration
- Makes intelligent decisions on appropriate actions

### Learning Library Integration
- 50+ learning documents covering:
  - Breaking changes (33 documents)
  - Transformations (ITelemetryInitializer → BaseProcessor<Activity>)
  - API reference (Activity, BaseProcessor, etc.)
  - Common scenarios and examples
  
### Code Templates
- Minimal Program.cs setup
- Configuration-based setup
- launchSettings.json templates
- appsettings.json templates

### Validation
- Verifies package installation
- Checks for legacy code patterns
- Validates connection string configuration
- Ensures OpenTelemetry proper registration

## Installation

```bash
npm install
npm run build
```

## Usage

### Start the Server

```bash
npm start
```

The server runs on stdio and communicates via the MCP protocol.

### Available Tools

#### 1. analyze_aspnetcore_project

Analyzes an ASP.NET Core project to determine scenario.

**Input:**
```json
{
  "project_path": "/path/to/MyApp"
}
```

**Output:**
```json
{
  "analysis": {
    "scenario": "fresh_onboarding",
    "appType": "aspnetcore",
    "targetFramework": "net8.0",
    "hasLegacyAiSdk": false,
    "hasOpenTelemetry": false,
    "confidence": 1.0
  },
  "decision": {
    "action": "fresh_onboarding",
    "packageToInstall": "Azure.Monitor.OpenTelemetry.AspNetCore",
    "learningResources": [
      "common-scenarios/fresh-aspnetcore-onboarding.md"
    ]
  }
}
```

#### 2. get_onboarding_guide

Retrieves the fresh onboarding guide with quick start info.

**Input:**
```json
{
  "scenario": "fresh_onboarding"
}
```

**Output:**
Includes full guide content, package name, installation command, and code snippet.

#### 3. validate_onboarding

Validates fresh onboarding implementation.

**Input:**
```json
{
  "project_path": "/path/to/MyApp"
}
```

**Output:**
```json
{
  "isValid": true,
  "checks": [
    {
      "name": "package_installed",
      "passed": true,
      "message": "Azure.Monitor.OpenTelemetry.AspNetCore is installed"
    }
  ],
  "warnings": [],
  "errors": []
}
```

#### 4. get_code_template

Gets code templates for implementation.

**Input:**
```json
{
  "template_name": "minimal"
}
```

**Output:**
Returns the requested code template.

### Learning Resources

All learning documents are exposed as MCP resources:

```
mcp://learn/common-scenarios/fresh-aspnetcore-onboarding.md
mcp://learn/breaking-changes/TelemetryClient/parameterless-constructor.md
mcp://learn/api-reference/Activity/SetTag.md
... (50+ documents)
```

## Integration with AI Agents

### GitHub Copilot / Claude Sonnet 4.5 / GPT-5

The MCP server is designed to work with advanced AI coding assistants:

1. **Agent detects user opening ASP.NET Core project**
2. **Agent queries MCP:** `analyze_aspnetcore_project`
3. **MCP responds:** Fresh onboarding scenario detected
4. **Agent queries learning resources** from MCP
5. **Agent implements:** Package installation + code changes
6. **Agent validates:** Using `validate_onboarding` tool
7. **Agent reports:** Status to user

### Example Flow

```typescript
// 1. User opens project in VS Code
// 2. Copilot queries MCP
const analysis = await mcp.callTool('analyze_aspnetcore_project', {
  project_path: '/path/to/project'
});

// 3. MCP responds: fresh_onboarding scenario
// 4. Copilot queries guide
const guide = await mcp.callTool('get_onboarding_guide', {
  scenario: 'fresh_onboarding'
});

// 5. Copilot reads learning resources
const resource = await mcp.readResource(
  'mcp://learn/common-scenarios/fresh-aspnetcore-onboarding.md'
);

// 6. Copilot implements changes using grounded knowledge
// - Installs package
// - Modifies Program.cs
// - Adds connection string config

// 7. Copilot validates
const validation = await mcp.callTool('validate_onboarding', {
  project_path: '/path/to/project'
});
```

## Configuration

Edit `config.json` to customize:
- Detection patterns
- Learning resource paths
- Validation rules
- Supported scenarios

## Development

```bash
# Watch mode
npm run dev

# Run tests
npm test

# Build
npm run build
```

## Architecture

```
MCP Server
├── Detection Engine (scenario-detector.ts)
│   ├── Analyzes .csproj files
│   ├── Detects project type
│   └── Identifies packages
├── Decision Engine
│   ├── Fresh onboarding
│   ├── Migration 2.x → 3.x
│   └── Already configured
├── Validation Engine
│   ├── Package checks
│   ├── Code pattern checks
│   └── Configuration checks
├── Learning Library (../learn)
│   ├── 50+ markdown documents
│   ├── Code examples with sources
│   └── Step-by-step guides
└── Templates
    ├── Program.cs templates
    ├── Configuration templates
    └── Launch settings templates
```

## License

MIT
