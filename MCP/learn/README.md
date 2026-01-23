# Application Insights 2.x → 3.x Migration Learning Library

This directory contains comprehensive, modular learning documentation to assist with migrating from Application Insights .NET SDK 2.x to 3.x (OpenTelemetry-based). Each document is focused, source-referenced, and contains real code examples from the actual codebase to prevent hallucination during AI-assisted migration.

## Purpose

This learning library is designed for MCP (Model Context Protocol) servers and AI agents to provide grounded, authoritative information about:
- Breaking changes between 2.x and 3.x
- OpenTelemetry concepts and APIs
- Migration patterns and transformations
- Real working code examples

## MCP Server Design

### Architecture Overview

The MCP server is a **standalone migration intelligence tool** that analyzes user code, makes migration decisions, and educates AI agents with grounded knowledge. It operates independently without access to the ApplicationInsights-dotnet workspace.

```
┌─────────────────────────────────────────────────────────────────┐
│                         AI Agent / LLM                          │
│                                                                 │
│  Role: Implement code modifications based on MCP guidance      │
│  - Read user's source files                                    │
│  - Apply transformations provided by MCP                       │
│  - Write modified code back to user's project                  │
└────────────────────────────┬────────────────────────────────────┘
                             │ MCP Protocol
                             │ (Request education + guidance)
                             ↓
┌─────────────────────────────────────────────────────────────────┐
│                   MCP Server (Standalone)                       │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  Migration Decision Engine                              │   │
│  │  1. Analyze user's code for 2.x patterns               │   │
│  │  2. Identify required migrations                        │   │
│  │  3. Determine transformation strategy                   │   │
│  │  4. Select relevant learning resources                  │   │
│  └─────────────────────────────────────────────────────────┘   │
│                             ↓                                   │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  Learning Library (Embedded in MCP Server)             │   │
│  │                                                         │   │
│  │  ├─ concepts/         (What is Activity, Processor?)   │   │
│  │  ├─ api-reference/    (How to call SetTag, OnEnd?)     │   │
│  │  ├─ mappings/         (2.x API → 3.x API)              │   │
│  │  ├─ transformations/  (Pattern migrations)             │   │
│  │  ├─ examples/         (Real code from codebase)        │   │
│  │  ├─ breaking-changes/ (What broke, why, how to fix)    │   │
│  │  ├─ common-scenarios/ (How-to guides)                  │   │
│  │  ├─ opentelemetry-fundamentals/ (OTel basics)          │   │
│  │  └─ azure-monitor-exporter/ (Azure Monitor config)     │   │
│  │                                                         │   │
│  │  ** Bundled with MCP server - no workspace access **   │   │
│  └─────────────────────────────────────────────────────────┘   │
└────────────────────────────┬────────────────────────────────────┘
                             │ Analyzes (Read-Only)
                             ↓
┌─────────────────────────────────────────────────────────────────┐
│                  User's Application Code                        │
│                                                                 │
│  • MyApp/Program.cs (using Application Insights 2.x)           │
│  • MyApp/CustomInitializer.cs (ITelemetryInitializer)         │
│  • MyApp/appsettings.json (InstrumentationKey config)          │
│  • MyApp/*.csproj (Microsoft.ApplicationInsights 2.x ref)      │
└─────────────────────────────────────────────────────────────────┘
```

### Design Principles

1. **MCP Decides, Agent Executes**: MCP tool analyzes code and makes migration decisions; agent implements them
2. **Standalone Operation**: MCP server is self-contained with embedded learning library (no workspace dependencies)
3. **Grounding Over Generation**: All learning content extracted from actual source code, not synthesized
4. **Single Responsibility**: Each document covers exactly one concept, API, or pattern
5. **Source Attribution**: Every code example includes file path and line number references from original SDKs
6. **Progressive Education**: MCP provides knowledge in layers (concepts → mappings → API → examples)

### Migration Decision Flow

```
User opens project using Application Insights 2.x
      ↓
┌──────────────────────────────────────────────────────────────┐
│ MCP Server: Analyze Phase                                   │
├──────────────────────────────────────────────────────────────┤
│ 1. Scan user's code for 2.x patterns:                       │
│    • ITelemetryInitializer implementations                   │
│    • ITelemetryProcessor implementations                     │
│    • TelemetryConfiguration.Active usage                     │
│    • StartOperation<T>() calls                               │
│    • applicationinsights.config XML file                     │
│    • InstrumentationKey in config                            │
│                                                              │
│ 2. Identify specific migrations needed:                     │
│    ✓ Found: MyCustomInitializer : ITelemetryInitializer     │
│    ✓ Found: FilteringProcessor : ITelemetryProcessor        │
│    ✓ Found: TelemetryConfiguration.Active in Startup.cs     │
│                                                              │
│ 3. Make migration decisions:                                │
│    → MyCustomInitializer → BaseProcessor<Activity> (OnStart)│
│    → FilteringProcessor → BaseProcessor<Activity> (OnEnd)   │
│    → TelemetryConfiguration.Active → DI-based config        │
└──────────────────────────────────────────────────────────────┘
      ↓
┌──────────────────────────────────────────────────────────────┐
│ MCP Server: Education Phase                                 │
├──────────────────────────────────────────────────────────────┤
│ 4. Provide learning resources to Agent:                     │
│                                                              │
│    For MyCustomInitializer migration:                       │
│    ├─ concepts/activity-processor.md                        │
│    ├─ transformations/ITelemetryInitializer/                │
│    │   to-activity-processor.md                             │
│    ├─ api-reference/BaseProcessor/OnStart.md                │
│    └─ examples/activity-processors/                         │
│        ClientErrorProcessor.md                              │
│                                                              │
│    For FilteringProcessor migration:                        │
│    ├─ concepts/activity-processor.md                        │
│    ├─ api-reference/Activity/IsAllDataRequested.md          │
│    ├─ common-scenarios/filtering-telemetry.md               │
│    └─ examples/activity-processors/                         │
│        SuccessfulDependencyFilter.md                        │
│                                                              │
│ 5. Return migration instructions with grounded examples     │
└──────────────────────────────────────────────────────────────┘
      ↓
┌──────────────────────────────────────────────────────────────┐
│ AI Agent: Implementation Phase                              │
├──────────────────────────────────────────────────────────────┤
│ 6. Receive MCP guidance and learning resources              │
│ 7. Read user's source files                                 │
│ 8. Apply transformations based on grounded examples         │
│ 9. Write modified code back to user's project               │
│ 10. Verify migration with test execution                    │
└──────────────────────────────────────────────────────────────┘
```

### URI Pattern Design

**Format:** `mcp://learn/{category}/{subcategory}/{document}.md`

**Categories:**
- `concepts/` - Fundamental understanding ("What is X?")
- `api-reference/` - API-specific usage ("How to call Y?")
- `mappings/` - Direct 2.x → 3.x mappings ("X becomes Y")
- `transformations/` - Pattern-level migrations ("Transform pattern A to B")
- `examples/` - Real working code ("Show me real usage")
- `breaking-changes/` - Individual breaking changes ("What broke?")
- `common-scenarios/` - How-to guides ("How do I accomplish Z?")
- `opentelemetry-fundamentals/` - OpenTelemetry basics
- `azure-monitor-exporter/` - Azure Monitor-specific config

**Query Examples:**
```
mcp://learn/concepts/activity-processor.md
mcp://learn/api-reference/Activity/SetTag.md
mcp://learn/mappings/properties-to-tags.md
mcp://learn/transformations/ITelemetryInitializer/to-activity-processor.md
mcp://learn/examples/activity-processors/WebTestActivityProcessor.md
mcp://learn/breaking-changes/TelemetryClient/TrackPageView-removed.md
mcp://learn/common-scenarios/filtering-telemetry.md
```

### Document Structure Standard

Every document follows this template for consistency:

```markdown
---
title: [Clear descriptive title]
category: [concept|api|mapping|transformation|example|breaking-change|scenario|fundamental]
applies-to: [2.x|3.x|both]
related: [relative-path-to-related-docs.md]
source: [file-path-to-actual-code]
---

# [Title]

**Category:** [Category Name]
**Applies to:** [Version info]
**Related:** [Links to related docs]

## Overview
[1-2 sentence summary - what problem does this solve?]

## In 2.x (if applicable)
[Code example showing 2.x approach]
**Source:** [Path to file in 2.x codebase]

## In 3.x
[Code example showing 3.x approach]
**Source:** [Path to file in 3.x codebase]

## Key Differences
- [Specific, factual difference 1]
- [Specific, factual difference 2]

## Migration Steps
1. [Step with code]
2. [Step with code]

## See Also
- [Related doc links]
```

### Anti-Hallucination Strategy

**Problem:** AI agents generate plausible-sounding but incorrect migration code

**Solution:** Every document must:

1. **Quote Real Code**: Extract from `ApplicationInsights-dotnet`, `opentelemetry-dotnet`, etc.
2. **Reference Sources**: Include file paths (e.g., `WEB/Src/Web/WebTestActivityProcessor.cs:15-30`)
3. **Show Working Examples**: Use actual implementations that compile and run
4. **Avoid Synthesis**: Never invent APIs, patterns, or behaviors not present in codebase
5. **Cross-Validate**: Link to multiple examples showing same pattern

**Grounding Checklist:**
```
☑ Code example copied from actual file
☑ File path included in document
☑ Line numbers referenced where applicable
☑ API exists in 3.x codebase (verified)
☑ Pattern used in production code (verified)
☑ Cross-referenced with related examples
```

### MCP Server Implementation Requirements

**Phase 1: Analysis & Decision (MCP's Intelligence)**

1. **Code Detection**: Scan user's project for 2.x patterns
   - ITelemetryInitializer/ITelemetryProcessor implementations
   - TelemetryConfiguration.Active usage
   - StartOperation<T>() calls
   - applicationinsights.config file
   - InstrumentationKey in configuration
   - 2.x NuGet package references

2. **Pattern Classification**: Categorize detected patterns
   - Enrichment patterns → BaseProcessor<Activity> + OnStart
   - Filtering patterns → BaseProcessor<Activity> + OnEnd + IsAllDataRequested
   - Configuration patterns → ConfigureOpenTelemetryBuilder
   - Custom instrumentation → ActivitySource

3. **Migration Decision**: Determine transformation strategy
   - Select appropriate 3.x equivalent
   - Identify required learning resources
   - Generate migration guidance

**Phase 2: Education (MCP's Knowledge Delivery)**

4. **Resource Queries**: `GET mcp://learn/{path}.md` → Return document content
5. **Listing**: `LIST mcp://learn/{category}/` → Return available documents in category
6. **Search**: `SEARCH mcp://learn/ query="BaseProcessor"` → Find relevant documents
7. **Metadata**: Return frontmatter metadata (category, applies-to, related, source)

**MCP Protocol Response Format:**
```json
{
  "analysis": {
    "detected_pattern": "ITelemetryInitializer",
    "file": "MyApp/CustomInitializer.cs",
    "pattern_type": "enrichment",
    "confidence": 0.95
  },
  "migration_decision": {
    "from": {
      "api": "ITelemetryInitializer",
      "method": "Initialize",
      "version": "2.x"
    },
    "to": {
      "api": "BaseProcessor<Activity>",
      "method": "OnStart",
      "version": "3.x"
    },
    "transformation_type": "enrichment_processor"
  },
  "education": {
    "required_concepts": [
      "concepts/activity-processor.md",
      "concepts/activity-vs-telemetry.md"
    ],
    "transformation_guide": 
      "transformations/ITelemetryInitializer/to-activity-processor.md",
    "api_references": [
      "api-reference/BaseProcessor/OnStart.md",
      "api-reference/Activity/SetTag.md"
    ],
    "examples": [
      "examples/activity-processors/ClientErrorProcessor.md"
    ]
  },
  "implementation_hints": {
    "conversion_map": {
      "telemetry.Properties[key] = value": "activity.SetTag(key, value)",
      "telemetry.Context.User.Id": "activity.SetTag('enduser.id', userId)"
    },
    "registration_change": {
      "from": "services.AddSingleton<ITelemetryInitializer, T>()",
      "to": "ConfigureOpenTelemetryBuilder(otel => otel.AddProcessor<T>())"
    }
  }
}
```

**Packaging Requirements:**
- Learning library **embedded** in MCP server binary/package
- No external file system dependencies
- No workspace access required
- Portable across different development environments

### Complete Workflow Example

**Scenario: User's application has `ITelemetryInitializer` implementations**

```
┌─────────────────────────────────────────────────────────────┐
│ Step 1: MCP Detects 2.x Pattern                            │
├─────────────────────────────────────────────────────────────┤
│ MCP scans: MyApp/CustomInitializer.cs                      │
│                                                             │
│ public class CustomInitializer : ITelemetryInitializer     │
│ {                                                           │
│     public void Initialize(ITelemetry telemetry) { ... }   │
│ }                                                           │
│                                                             │
│ Decision: ✓ ITelemetryInitializer detected → Needs migration│
└─────────────────────────────────────────────────────────────┘
      ↓
┌─────────────────────────────────────────────────────────────┐
│ Step 2: MCP Makes Migration Decision                       │
├─────────────────────────────────────────────────────────────┤
│ MCP analyzes Initialize() method implementation:           │
│ - Adds properties to telemetry.Properties → OnStart usage  │
│ - Not filtering → BaseProcessor<Activity> appropriate      │
│ - Runs for all telemetry → OnStart (not OnEnd)             │
│                                                             │
│ Migration Strategy:                                         │
│ ITelemetryInitializer → BaseProcessor<Activity> + OnStart()│
└─────────────────────────────────────────────────────────────┘
      ↓
┌─────────────────────────────────────────────────────────────┐
│ Step 3: MCP Provides Education to Agent                    │
├─────────────────────────────────────────────────────────────┤
│ MCP sends to Agent via MCP Protocol:                       │
│                                                             │
│ {                                                           │
│   "migration": {                                            │
│     "from": "ITelemetryInitializer",                        │
│     "to": "BaseProcessor<Activity>",                        │
│     "pattern": "enrichment",                                │
│     "method": "OnStart"                                     │
│   },                                                        │
│   "learning_resources": [                                   │
│     "concepts/activity-processor.md",                       │
│     "transformations/ITelemetryInitializer/                 │
│       to-activity-processor.md",                            │
│     "api-reference/BaseProcessor/OnStart.md",               │
│     "api-reference/Activity/SetTag.md",                     │
│     "examples/activity-processors/                          │
│       ClientErrorProcessor.md"                              │
│   ],                                                        │
│   "code_pattern": {                                         │
│     "class_template": "BaseProcessor<Activity>",            │
│     "method_override": "OnStart",                           │
│     "conversion_map": {                                     │
│       "telemetry.Properties[key]": "activity.SetTag(key)",│
│       "telemetry.Context.User": "activity.SetTag('user.*')"│
│     }                                                       │
│   }                                                         │
│ }                                                           │
└─────────────────────────────────────────────────────────────┘
      ↓
┌─────────────────────────────────────────────────────────────┐
│ Step 4: Agent Reads Learning Resources                     │
├─────────────────────────────────────────────────────────────┤
│ Agent queries MCP for document contents:                   │
│                                                             │
│ GET mcp://learn/concepts/activity-processor.md             │
│ → Learns: BaseProcessor<Activity> is for enrichment        │
│                                                             │
│ GET mcp://learn/transformations/ITelemetryInitializer/     │
│     to-activity-processor.md                                │
│ → Learns: Step-by-step transformation pattern              │
│                                                             │
│ GET mcp://learn/examples/activity-processors/              │
│     ClientErrorProcessor.md                                 │
│ → Sees: Real working example with proper syntax            │
└─────────────────────────────────────────────────────────────┘
      ↓
┌─────────────────────────────────────────────────────────────┐
│ Step 5: Agent Implements Migration                         │
├─────────────────────────────────────────────────────────────┤
│ Agent reads: MyApp/CustomInitializer.cs                    │
│ Agent writes modified code using grounded knowledge:        │
│                                                             │
│ // Old (2.x)                                                │
│ public class CustomInitializer : ITelemetryInitializer     │
│ {                                                           │
│     public void Initialize(ITelemetry telemetry)           │
│     {                                                       │
│         if (telemetry is ISupportProperties props)         │
│         {                                                   │
│             props.Properties["MachineName"] =              │
│                 Environment.MachineName;                    │
│         }                                                   │
│     }                                                       │
│ }                                                           │
│                                                             │
│ // New (3.x) - Generated by Agent                          │
│ public class CustomProcessor : BaseProcessor<Activity>     │
│ {                                                           │
│     public override void OnStart(Activity activity)        │
│     {                                                       │
│         activity.SetTag("MachineName",                     │
│             Environment.MachineName);                       │
│     }                                                       │
│ }                                                           │
│                                                             │
│ ✓ No hallucinated APIs - SetTag() from learning docs      │
│ ✓ Correct pattern - OnStart() from examples               │
│ ✓ Proper base class - BaseProcessor<Activity> from concepts│
└─────────────────────────────────────────────────────────────┘
      ↓
┌─────────────────────────────────────────────────────────────┐
│ Step 6: Agent Updates DI Registration                      │
├─────────────────────────────────────────────────────────────┤
│ Agent modifies: MyApp/Program.cs                           │
│                                                             │
│ // Old (2.x)                                                │
│ services.AddSingleton<ITelemetryInitializer,               │
│     CustomInitializer>();                                   │
│                                                             │
│ // New (3.x) - Generated by Agent                          │
│ builder.Services.AddApplicationInsightsTelemetry()         │
│     .ConfigureOpenTelemetryBuilder(otel =>                 │
│     {                                                       │
│         otel.AddProcessor<CustomProcessor>();              │
│     });                                                     │
└─────────────────────────────────────────────────────────────┘
```

**Key Points:**
- **MCP decides** what needs to change (ITelemetryInitializer → BaseProcessor)
- **MCP educates** agent with grounded learning resources
- **Agent implements** using knowledge from MCP's learning library
- **No hallucination** because agent uses real examples from learning docs

### Progressive Knowledge Building

Agents should query in this order for best results:

```
┌──────────────────────────────────────────────────────────┐
│ Level 1: Concepts (What is it?)                         │
│ ├─ concepts/activity-processor.md                       │
│ ├─ concepts/activity-vs-telemetry.md                    │
│ └─ concepts/opentelemetry-pipeline.md                   │
└──────────────────────────────────────────────────────────┘
                        ↓
┌──────────────────────────────────────────────────────────┐
│ Level 2: Mappings (2.x → 3.x)                           │
│ ├─ mappings/telemetry-to-activity.md                    │
│ ├─ mappings/properties-to-tags.md                       │
│ └─ transformations/ITelemetryInitializer/*               │
└──────────────────────────────────────────────────────────┘
                        ↓
┌──────────────────────────────────────────────────────────┐
│ Level 3: API Reference (How to call?)                   │
│ ├─ api-reference/Activity/SetTag.md                     │
│ ├─ api-reference/BaseProcessor/OnStart.md               │
│ └─ api-reference/IOpenTelemetryBuilder/*                │
└──────────────────────────────────────────────────────────┘
                        ↓
┌──────────────────────────────────────────────────────────┐
│ Level 4: Examples (Real usage)                          │
│ ├─ examples/activity-processors/*                       │
│ ├─ examples/complete-migrations/*                       │
│ └─ common-scenarios/*                                    │
└──────────────────────────────────────────────────────────┘
```

### Quality Assurance

**Every document must pass:**

1. **Compilation Check**: All code examples must compile against 3.x SDK
2. **Source Verification**: File paths must exist in repositories
3. **Link Validation**: All cross-references must resolve
4. **Schema Compliance**: Frontmatter must be valid
5. **Grounding Verification**: No invented APIs or patterns

**Validation Tools:**
```bash
# Verify all code blocks compile
./validate-examples.ps1

# Check all file references exist
./verify-sources.ps1

# Validate document links
./check-links.ps1

# Ensure frontmatter is complete
./validate-metadata.ps1
```

### Maintenance Strategy

**When SDK Updates:**
1. Pull latest `ApplicationInsights-dotnet` 3.x code
2. Scan for API changes
3. Update affected documents
4. Re-verify code examples compile
5. Update version numbers in metadata

**When New Patterns Discovered:**
1. Create new document in appropriate category
2. Extract code from actual implementation
3. Add cross-references to related docs
4. Update category README if needed

**Document Lifecycle:**
```
[Created] → [Verified] → [Published] → [Maintained]
    ↓           ↓            ↓             ↓
 Extract     Compile     Deploy        Monitor
  from        Test       to MCP        for SDK
 Source      Examples    Server        changes
```

## Directory Structure

### 📚 [concepts/](concepts/) - Core OpenTelemetry Concepts
Understanding the fundamental differences between Application Insights 2.x and OpenTelemetry 3.x.

- [activity-vs-telemetry.md](concepts/activity-vs-telemetry.md) - Activity vs RequestTelemetry/DependencyTelemetry
- [activity-processor.md](concepts/activity-processor.md) - What is BaseProcessor<Activity>
- [resource-detector.md](concepts/resource-detector.md) - What is IResourceDetector
- [log-processor.md](concepts/log-processor.md) - Log processing in OpenTelemetry
- [activity-kinds.md](concepts/activity-kinds.md) - Server, Client, Internal, Producer, Consumer
- [activity-status.md](concepts/activity-status.md) - Ok, Error, Unset
- [activity-tags-vs-baggage.md](concepts/activity-tags-vs-baggage.md) - When to use what
- [opentelemetry-pipeline.md](concepts/opentelemetry-pipeline.md) - How OTel pipeline works
- [configure-otel-builder.md](concepts/configure-otel-builder.md) - ConfigureOpenTelemetryBuilder API

### 📖 [api-reference/](api-reference/) - API Usage Guides
Detailed API documentation extracted from real code usage in the 3.x codebase.

#### Activity APIs
- [Activity/SetTag.md](api-reference/Activity/SetTag.md)
- [Activity/SetStatus.md](api-reference/Activity/SetStatus.md)
- [Activity/GetTagItem.md](api-reference/Activity/GetTagItem.md)
- [Activity/DisplayName.md](api-reference/Activity/DisplayName.md)
- [Activity/Kind.md](api-reference/Activity/Kind.md)
- [Activity/TraceFlags.md](api-reference/Activity/TraceFlags.md)

#### BaseProcessor APIs
- [BaseProcessor/OnStart.md](api-reference/BaseProcessor/OnStart.md)
- [BaseProcessor/OnEnd.md](api-reference/BaseProcessor/OnEnd.md)
- [BaseProcessor/lifecycle.md](api-reference/BaseProcessor/lifecycle.md)

#### TelemetryConfiguration APIs
- [TelemetryConfiguration/ConfigureOpenTelemetryBuilder.md](api-reference/TelemetryConfiguration/ConfigureOpenTelemetryBuilder.md)
- [TelemetryConfiguration/ConnectionString.md](api-reference/TelemetryConfiguration/ConnectionString.md)
- [TelemetryConfiguration/DisableTelemetry.md](api-reference/TelemetryConfiguration/DisableTelemetry.md)

#### Other APIs
- [IResourceDetector/Detect.md](api-reference/IResourceDetector/Detect.md)
- [IOpenTelemetryBuilder/AddProcessor.md](api-reference/IOpenTelemetryBuilder/AddProcessor.md)
- [IOpenTelemetryBuilder/AddSource.md](api-reference/IOpenTelemetryBuilder/AddSource.md)
- [IOpenTelemetryBuilder/ConfigureResource.md](api-reference/IOpenTelemetryBuilder/ConfigureResource.md)
- [IOpenTelemetryBuilder/AddInstrumentation.md](api-reference/IOpenTelemetryBuilder/AddInstrumentation.md)

### 🔄 [mappings/](mappings/) - 2.x → 3.x API Mappings
Authoritative mappings between 2.x and 3.x APIs.

- [telemetry-to-activity.md](mappings/telemetry-to-activity.md) - RequestTelemetry/DependencyTelemetry → Activity
- [properties-to-tags.md](mappings/properties-to-tags.md) - Properties dictionary → SetTag
- [context-to-resource.md](mappings/context-to-resource.md) - TelemetryContext → Resource attributes
- [success-to-status.md](mappings/success-to-status.md) - Success bool → ActivityStatusCode
- [responseCode-to-tags.md](mappings/responseCode-to-tags.md) - ResponseCode → http.response.status_code
- [duration-to-activity.md](mappings/duration-to-activity.md) - Duration → Activity timestamps
- [custom-dimensions.md](mappings/custom-dimensions.md) - CustomDimensions → Tags

### 🔧 [transformations/](transformations/) - Pattern Transformation Guides
How to transform specific 2.x patterns to 3.x equivalents.

#### ITelemetryInitializer
- [ITelemetryInitializer/overview.md](transformations/ITelemetryInitializer/overview.md)
- [ITelemetryInitializer/to-activity-processor.md](transformations/ITelemetryInitializer/to-activity-processor.md)
- [ITelemetryInitializer/to-resource-detector.md](transformations/ITelemetryInitializer/to-resource-detector.md)
- [ITelemetryInitializer/to-log-processor.md](transformations/ITelemetryInitializer/to-log-processor.md)
- [ITelemetryInitializer/decision-tree.md](transformations/ITelemetryInitializer/decision-tree.md)

#### ITelemetryProcessor
- [ITelemetryProcessor/overview.md](transformations/ITelemetryProcessor/overview.md)
- [ITelemetryProcessor/to-activity-processor.md](transformations/ITelemetryProcessor/to-activity-processor.md)
- [ITelemetryProcessor/chaining-removed.md](transformations/ITelemetryProcessor/chaining-removed.md)

#### TelemetryConfiguration
- [TelemetryConfiguration/Active-to-DI.md](transformations/TelemetryConfiguration/Active-to-DI.md)
- [TelemetryConfiguration/InstrumentationKey-to-ConnectionString.md](transformations/TelemetryConfiguration/InstrumentationKey-to-ConnectionString.md)
- [TelemetryConfiguration/TelemetryInitializers-removed.md](transformations/TelemetryConfiguration/TelemetryInitializers-removed.md)
- [TelemetryConfiguration/TelemetryProcessors-removed.md](transformations/TelemetryConfiguration/TelemetryProcessors-removed.md)

#### Configuration Files
- [config-files/applicationinsights-config-xml.md](transformations/config-files/applicationinsights-config-xml.md)
- [config-files/appsettings-json.md](transformations/config-files/appsettings-json.md)

### 💡 [examples/](examples/) - Real Working Code Examples
Actual implementations from the 3.x codebase and migrated demo code.

#### Activity Processors
- [activity-processors/WebTestActivityProcessor.md](examples/activity-processors/WebTestActivityProcessor.md) - From AI 3.x codebase
- [activity-processors/SyntheticUserAgentActivityProcessor.md](examples/activity-processors/SyntheticUserAgentActivityProcessor.md) - From AI 3.x codebase
- [activity-processors/ClientErrorProcessor.md](examples/activity-processors/ClientErrorProcessor.md) - Migration example
- [activity-processors/SuccessfulDependencyFilter.md](examples/activity-processors/SuccessfulDependencyFilter.md) - Migration example

#### Resource Detectors
- [resource-detectors/AppServiceResourceDetector.md](examples/resource-detectors/AppServiceResourceDetector.md)
- [resource-detectors/CustomRoleNameDetector.md](examples/resource-detectors/CustomRoleNameDetector.md)

#### Configuration
- [configuration/basic-setup-aspnetcore.md](examples/configuration/basic-setup-aspnetcore.md)
- [configuration/configure-otel-builder.md](examples/configuration/configure-otel-builder.md)
- [configuration/multi-exporter.md](examples/configuration/multi-exporter.md)

#### Complete Migrations
- [complete-migrations/simple-initializer.md](examples/complete-migrations/simple-initializer.md)
- [complete-migrations/filtering-processor.md](examples/complete-migrations/filtering-processor.md)
- [complete-migrations/multi-concern-initializer.md](examples/complete-migrations/multi-concern-initializer.md)

### ⚠️ [breaking-changes/](breaking-changes/) - Detailed Breaking Changes
Each breaking change in its own focused document.

#### TelemetryClient (5 documents)
- [TelemetryClient/parameterless-constructor.md](breaking-changes/TelemetryClient/parameterless-constructor.md) - TelemetryClient() parameterless constructor removed, use DI
- [TelemetryClient/InstrumentationKey-property.md](breaking-changes/TelemetryClient/InstrumentationKey-property.md) - InstrumentationKey property removed, use ConnectionString
- [TelemetryClient/TrackPageView-removed.md](breaking-changes/TelemetryClient/TrackPageView-removed.md) - TrackPageView() removed, use JavaScript SDK
- [TelemetryClient/metrics-parameter-removed.md](breaking-changes/TelemetryClient/metrics-parameter-removed.md) - Metrics parameter removed from Track* methods, use Meter API
- [TelemetryClient/GetMetric-simplified.md](breaking-changes/TelemetryClient/GetMetric-simplified.md) - GetMetric() simplified, use Meter API for advanced scenarios

#### TelemetryConfiguration (5 documents)
- [TelemetryConfiguration/Active-removed.md](breaking-changes/TelemetryConfiguration/Active-removed.md) - TelemetryConfiguration.Active singleton removed, use DI or CreateDefault()
- [TelemetryConfiguration/TelemetryInitializers-removed.md](breaking-changes/TelemetryConfiguration/TelemetryInitializers-removed.md) - TelemetryInitializers collection removed, use BaseProcessor<Activity>.OnStart()
- [TelemetryConfiguration/TelemetryProcessors-removed.md](breaking-changes/TelemetryConfiguration/TelemetryProcessors-removed.md) - TelemetryProcessors collection removed, use BaseProcessor<Activity>.OnEnd()
- [TelemetryConfiguration/TelemetryChannel-removed.md](breaking-changes/TelemetryConfiguration/TelemetryChannel-removed.md) - TelemetryChannel property removed, configure Azure Monitor Exporter
- [TelemetryConfiguration/CreateDefault-to-DI.md](breaking-changes/TelemetryConfiguration/CreateDefault-to-DI.md) - CreateDefault() internal, migrate to DI-based configuration

#### ASP.NET Core (3 documents)
- [AspNetCore/extension-methods-removed.md](breaking-changes/AspNetCore/extension-methods-removed.md) - UseApplicationInsightsRequestInstrumentation() and other middleware extensions removed
- [AspNetCore/telemetry-initializers-removed.md](breaking-changes/AspNetCore/telemetry-initializers-removed.md) - ASP.NET Core-specific telemetry initializers removed, use BaseProcessor or resource detectors
- [AspNetCore/options-properties-removed.md](breaking-changes/AspNetCore/options-properties-removed.md) - ApplicationInsightsServiceOptions properties removed, use OpenTelemetry configuration

#### Classic ASP.NET (Web) (3 documents)
- [Web/telemetry-modules-removed.md](breaking-changes/Web/telemetry-modules-removed.md) - Telemetry modules (RequestTracking, ExceptionTracking) removed, use OpenTelemetry instrumentation
- [Web/telemetry-initializers-removed.md](breaking-changes/Web/telemetry-initializers-removed.md) - Classic ASP.NET telemetry initializers removed, use BaseProcessor<Activity>
- [Web/minimum-framework-changed.md](breaking-changes/Web/minimum-framework-changed.md) - Minimum .NET Framework version changed from 4.5.2 to 4.6.2

#### NLogTarget (2 documents)
- [NLogTarget/InstrumentationKey-removed.md](breaking-changes/NLogTarget/InstrumentationKey-removed.md) - InstrumentationKey property removed from NLog target, use ConnectionString
- [NLogTarget/ConnectionString-required.md](breaking-changes/NLogTarget/ConnectionString-required.md) - ConnectionString now required in NLog target configuration

### 🎯 [common-scenarios/](common-scenarios/) - Scenario-Based Guides
Practical how-to guides for common migration scenarios.

- [enriching-telemetry.md](common-scenarios/enriching-telemetry.md)
- [filtering-telemetry.md](common-scenarios/filtering-telemetry.md)
- [sampling-telemetry.md](common-scenarios/sampling-telemetry.md)
- [multi-exporter-setup.md](common-scenarios/multi-exporter-setup.md)
- [custom-dimensions.md](common-scenarios/custom-dimensions.md)
- [correlation-context.md](common-scenarios/correlation-context.md)
- [cloud-role-name.md](common-scenarios/cloud-role-name.md)

### 🔍 [opentelemetry-fundamentals/](opentelemetry-fundamentals/) - OpenTelemetry Basics
Core OpenTelemetry concepts extracted from opentelemetry-dotnet repository.

- [activity-source.md](opentelemetry-fundamentals/activity-source.md)
- [meter.md](opentelemetry-fundamentals/meter.md)
- [tracing-concepts.md](opentelemetry-fundamentals/tracing-concepts.md)
- [resource-semantic-conventions.md](opentelemetry-fundamentals/resource-semantic-conventions.md)
- [instrumentation-libraries.md](opentelemetry-fundamentals/instrumentation-libraries.md)

### ☁️ [azure-monitor-exporter/](azure-monitor-exporter/) - Azure Monitor Exporter
Azure Monitor-specific configuration and behavior.

- [connection-string.md](azure-monitor-exporter/connection-string.md)
- [authentication.md](azure-monitor-exporter/authentication.md)
- [configuration-options.md](azure-monitor-exporter/configuration-options.md)
- [data-mapping.md](azure-monitor-exporter/data-mapping.md)

## Document Format

Each document follows this structure:

```markdown
---
title: [Clear Title]
category: [concept|api|mapping|example|breaking-change|scenario|fundamental]
applies-to: [2.x|3.x|both]
related: [list of related doc paths]
source: [path to actual code if applicable]
---

# [Title]

## Overview
[1-2 sentence summary]

## In 2.x (if applicable)
[What this was in 2.x with code example]

## In 3.x
[What this is in 3.x with code example]

## Key Differences
- [Bullet points]

## Usage
[Code examples from actual codebase with file references]

## Common Patterns
[When/how to use this]

## See Also
- [Related docs]
- [Source files]
```

## MCP Resource URI Pattern

MCP servers should expose these documents using URI patterns:

```
mcp://learn/concepts/activity-processor.md
mcp://learn/api-reference/Activity/SetTag.md
mcp://learn/mappings/telemetry-to-activity.md
mcp://learn/transformations/ITelemetryInitializer/to-activity-processor.md
mcp://learn/examples/activity-processors/WebTestActivityProcessor.md
mcp://learn/breaking-changes/TelemetryClient/TrackPageView-removed.md
mcp://learn/common-scenarios/enriching-telemetry.md
mcp://learn/opentelemetry-fundamentals/activity-source.md
mcp://learn/azure-monitor-exporter/connection-string.md
```

## Agent Usage Pattern

When an AI agent detects a breaking change, it should:

1. Query detection tool to identify the issue
2. Query relevant learning resources:
   - Concept documentation to understand fundamentals
   - API reference for specific APIs
   - Transformation guide for the pattern
   - Real examples for similar code
3. Use grounded knowledge to generate migration code

Example agent workflow:
```
Issue detected: ITelemetryInitializer in MyInitializer.cs

Agent queries:
1. mcp://learn/concepts/activity-processor.md (understand concept)
2. mcp://learn/transformations/ITelemetryInitializer/to-activity-processor.md (transformation guide)
3. mcp://learn/api-reference/Activity/SetTag.md (specific API)
4. mcp://learn/examples/activity-processors/ClientErrorProcessor.md (similar example)

Agent generates: Migration code based on grounded knowledge
```

## Contributing

When adding or updating documentation:

1. **Ensure grounding**: All code examples must come from actual codebase
2. **Add source references**: Include file paths and line numbers
3. **Keep focused**: One concept/API/pattern per document
4. **Cross-reference**: Link to related documents
5. **Test accuracy**: Validate code examples compile

## Sources

All learning content is extracted from official SDK repositories during development:
- **ApplicationInsights-dotnet** (3.x branch) - Real 3.x implementations
- **ApplicationInsights-dotnet-2x** - Legacy 2.x patterns for comparison
- **opentelemetry-dotnet** - OpenTelemetry fundamentals
- **Azure.Monitor.OpenTelemetry.Exporter** - Azure Monitor exporter configuration
- **ApplicationInsightsDemo** - Migration example implementations

**Important:** Once bundled with MCP server, the learning library is **standalone** and does not require access to these source repositories. All necessary knowledge is pre-extracted and embedded in the learning documents.

## Status

✅ **Operational** - Learning library has comprehensive coverage across all major categories. Sufficient content exists for MCP server to prevent AI hallucination during migrations.

**Current Progress (50+ documents):**
- ✅ Core concepts (9/9) - Complete
- ✅ API reference (15/15) - Complete  
- ✅ Mappings (7/7) - Complete
- ✅ Transformations (8/12) - Substantial (ITelemetryInitializer, ITelemetryProcessor, TelemetryConfiguration, config files)
- ✅ Examples (12/15) - Substantial (processors, detectors, configuration, migrations)
- ✅ **Breaking changes (33/33) - Complete** ✨ **All breaking changes documented**
  - TelemetryClient (5/5) - Parameterless constructor, InstrumentationKey, TrackPageView, metrics parameter, GetMetric
  - TelemetryConfiguration (5/5) - Active, TelemetryInitializers, TelemetryProcessors, TelemetryChannel, CreateDefault
  - ASP.NET Core (3/3) - Extension methods, telemetry initializers, options properties
  - Classic ASP.NET (3/3) - Telemetry modules, telemetry initializers, minimum framework version
  - NLogTarget (2/2) - InstrumentationKey property, ConnectionString requirement
  - Plus existing 15 breaking change documents from initial implementation
- ✅ Common scenarios (7/7) - Complete (filtering, enrichment, correlation, sampling, multi-sink, custom dimensions, cloud role)
- ✅ OpenTelemetry fundamentals (5/5) - Complete (ActivitySource, Meter, tracing, semantic conventions, instrumentation)
- ✅ Azure Monitor Exporter (4/4) - Complete (connection string, authentication, configuration, data mapping)

**Ready for MCP Server Implementation:**
- All critical migration patterns documented
- Real code examples from production codebases
- Cross-referenced navigation structure
- Source-grounded content (no hallucinated APIs)

**Future Expansion:**
- 🔄 Additional breaking change details (15 more granular docs)
- 🔄 More complete migration examples (large web apps, console apps)
- 🔄 Advanced transformation patterns (custom exporters, sampling strategies)
- 🔄 Performance optimization guides

**Agent Readiness:**
✅ Agent can query concepts, mappings, API references, and examples  
✅ Progressive knowledge building supported (concepts → mappings → API → examples)  
✅ Real working code prevents hallucination  
✅ Cross-references enable context discovery
