/**
 * MCP Server Implementation
 * Application Insights Migration Assistant
 */

import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import {
  CallToolRequestSchema,
  ListResourcesRequestSchema,
  ListToolsRequestSchema,
  ReadResourceRequestSchema,
} from '@modelcontextprotocol/sdk/types.js';
import * as fs from 'fs';
import * as path from 'path';
import { ScenarioDetector, ValidationEngine } from './scenario-detector.js';

const CONFIG_PATH = path.join(__dirname, 'config.json');
const LEARN_BASE_PATH = path.join(__dirname, '../learn');

/**
 * MCP Server for Application Insights Migration
 */
class ApplicationInsightsMcpServer {
  private server: Server;
  private scenarioDetector: ScenarioDetector;
  private validationEngine: ValidationEngine;

  constructor() {
    this.server = new Server(
      {
        name: 'applicationinsights-migration-assistant',
        version: '1.0.0',
      },
      {
        capabilities: {
          resources: {},
          tools: {},
        },
      }
    );

    this.scenarioDetector = new ScenarioDetector(CONFIG_PATH);
    this.validationEngine = new ValidationEngine(CONFIG_PATH);

    this.setupHandlers();
  }

  private setupHandlers() {
    // List available tools
    this.server.setRequestHandler(ListToolsRequestSchema, async () => ({
      tools: [
        {
          name: 'analyze_aspnetcore_project',
          description: 'Analyze ASP.NET Core project to detect scenario (fresh onboarding, migration, or already configured)',
          inputSchema: {
            type: 'object',
            properties: {
              project_path: {
                type: 'string',
                description: 'Absolute path to the project directory containing .csproj file',
              },
            },
            required: ['project_path'],
          },
        },
        {
          name: 'get_onboarding_guide',
          description: 'Get fresh onboarding guide and implementation steps for ASP.NET Core',
          inputSchema: {
            type: 'object',
            properties: {
              scenario: {
                type: 'string',
                enum: ['fresh_onboarding', 'migration_2x_to_3x', 'already_configured'],
                description: 'The detected scenario type',
              },
            },
            required: ['scenario'],
          },
        },
        {
          name: 'validate_onboarding',
          description: 'Validate that fresh onboarding implementation is correct',
          inputSchema: {
            type: 'object',
            properties: {
              project_path: {
                type: 'string',
                description: 'Absolute path to the project directory',
              },
            },
            required: ['project_path'],
          },
        },
        {
          name: 'get_code_template',
          description: 'Get code template for fresh onboarding implementation',
          inputSchema: {
            type: 'object',
            properties: {
              template_name: {
                type: 'string',
                enum: ['minimal', 'with_config', 'launch_settings', 'appsettings'],
                description: 'Template type to retrieve',
              },
            },
            required: ['template_name'],
          },
        },
      ],
    }));

    // Handle tool calls
    this.server.setRequestHandler(CallToolRequestSchema, async (request) => {
      const { name, arguments: args } = request.params;

      switch (name) {
        case 'analyze_aspnetcore_project':
          return await this.handleAnalyzeProject(args);

        case 'get_onboarding_guide':
          return await this.handleGetOnboardingGuide(args);

        case 'validate_onboarding':
          return await this.handleValidateOnboarding(args);

        case 'get_code_template':
          return await this.handleGetCodeTemplate(args);

        default:
          throw new Error(`Unknown tool: ${name}`);
      }
    });

    // List available learning resources
    this.server.setRequestHandler(ListResourcesRequestSchema, async () => {
      const resources = this.discoverLearningResources(LEARN_BASE_PATH);

      return {
        resources: resources.map((resourcePath) => ({
          uri: `mcp://learn/${path.relative(LEARN_BASE_PATH, resourcePath)}`,
          name: path.basename(resourcePath),
          mimeType: 'text/markdown',
        })),
      };
    });

    // Read learning resource content
    this.server.setRequestHandler(ReadResourceRequestSchema, async (request) => {
      const uri = request.params.uri;
      const relativePath = uri.replace('mcp://learn/', '');
      const fullPath = path.join(LEARN_BASE_PATH, relativePath);

      if (!fs.existsSync(fullPath)) {
        throw new Error(`Resource not found: ${uri}`);
      }

      const content = fs.readFileSync(fullPath, 'utf-8');

      return {
        contents: [
          {
            uri,
            mimeType: 'text/markdown',
            text: content,
          },
        ],
      };
    });
  }

  /**
   * Handle analyze_aspnetcore_project tool
   */
  private async handleAnalyzeProject(args: any) {
    const projectPath = args.project_path;

    if (!projectPath) {
      throw new Error('project_path is required');
    }

    const analysis = await this.scenarioDetector.analyzeProject(projectPath);
    const decision = this.scenarioDetector.makeDecision(analysis);

    return {
      content: [
        {
          type: 'text',
          text: JSON.stringify(
            {
              analysis,
              decision,
            },
            null,
            2
          ),
        },
      ],
    };
  }

  /**
   * Handle get_onboarding_guide tool
   */
  private async handleGetOnboardingGuide(args: any) {
    const scenario = args.scenario;

    if (scenario === 'fresh_onboarding') {
      const guidePath = path.join(
        LEARN_BASE_PATH,
        'common-scenarios',
        'fresh-aspnetcore-onboarding.md'
      );

      const guide = fs.readFileSync(guidePath, 'utf-8');

      return {
        content: [
          {
            type: 'text',
            text: JSON.stringify(
              {
                scenario: 'fresh_onboarding',
                guide_uri: 'mcp://learn/common-scenarios/fresh-aspnetcore-onboarding.md',
                package: 'Azure.Monitor.OpenTelemetry.AspNetCore',
                quick_start: {
                  install: 'dotnet add package Azure.Monitor.OpenTelemetry.AspNetCore',
                  code: 'builder.Services.AddOpenTelemetry().UseAzureMonitor();',
                  env_var: 'APPLICATIONINSIGHTS_CONNECTION_STRING',
                },
                full_guide: guide,
              },
              null,
              2
            ),
          },
        ],
      };
    }

    throw new Error(`Scenario ${scenario} not yet implemented`);
  }

  /**
   * Handle validate_onboarding tool
   */
  private async handleValidateOnboarding(args: any) {
    const projectPath = args.project_path;

    if (!projectPath) {
      throw new Error('project_path is required');
    }

    const validation = await this.validationEngine.validateFreshOnboarding(projectPath);

    return {
      content: [
        {
          type: 'text',
          text: JSON.stringify(validation, null, 2),
        },
      ],
    };
  }

  /**
   * Handle get_code_template tool
   */
  private async handleGetCodeTemplate(args: any) {
    const templateName = args.template_name;

    const templateMap: Record<string, string> = {
      minimal: 'minimal-program-cs.template',
      with_config: 'with-config-program-cs.template',
      launch_settings: 'launch-settings.template',
      appsettings: 'appsettings.template',
    };

    const templateFile = templateMap[templateName];
    if (!templateFile) {
      throw new Error(`Unknown template: ${templateName}`);
    }

    const templatePath = path.join(
      __dirname,
      'templates',
      'fresh-onboarding',
      templateFile
    );

    const template = fs.readFileSync(templatePath, 'utf-8');

    return {
      content: [
        {
          type: 'text',
          text: template,
        },
      ],
    };
  }

  /**
   * Discover all learning resources
   */
  private discoverLearningResources(dir: string): string[] {
    const results: string[] = [];

    function walk(directory: string) {
      const files = fs.readdirSync(directory);

      for (const file of files) {
        const filePath = path.join(directory, file);
        const stat = fs.statSync(filePath);

        if (stat.isDirectory()) {
          walk(filePath);
        } else if (file.endsWith('.md')) {
          results.push(filePath);
        }
      }
    }

    walk(dir);
    return results;
  }

  /**
   * Start the MCP server
   */
  async run() {
    const transport = new StdioServerTransport();
    await this.server.connect(transport);
    console.error('Application Insights MCP Server running on stdio');
  }
}

// Run the server
const server = new ApplicationInsightsMcpServer();
server.run().catch(console.error);
