/**
 * MCP Server: Application Insights Migration Assistant
 * Scenario Detection and Decision Engine
 */

import * as fs from 'fs';
import * as path from 'path';
import * as xml2js from 'xml2js';

export interface ProjectAnalysis {
  scenario: string;
  appType: string;
  targetFramework: string;
  hasLegacyAiSdk: boolean;
  hasOpenTelemetry: boolean;
  legacyPackages: string[];
  programCsPattern: string;
  confidence: number;
}

export interface ScenarioDecision {
  action: string;
  packageToInstall?: string;
  packagesToRemove?: string[];
  learningResources: string[];
  implementationSteps: ImplementationStep[];
}

export interface ImplementationStep {
  step: number;
  action: string;
  description: string;
  template?: string;
  validation?: string;
}

export class ScenarioDetector {
  private config: any;

  constructor(configPath: string) {
    this.config = JSON.parse(fs.readFileSync(configPath, 'utf-8')).mcp_server;
  }

  /**
   * Analyze ASP.NET Core project to determine scenario
   */
  async analyzeProject(projectPath: string): Promise<ProjectAnalysis> {
    const csprojFiles = this.findCsprojFiles(projectPath);
    
    if (csprojFiles.length === 0) {
      throw new Error('No .csproj files found in project');
    }

    // Analyze first ASP.NET Core project found
    for (const csproj of csprojFiles) {
      const analysis = await this.analyzeCsprojFile(csproj);
      
      if (analysis.appType === 'aspnetcore') {
        return analysis;
      }
    }

    throw new Error('No ASP.NET Core projects found');
  }

  /**
   * Make decision based on project analysis
   */
  makeDecision(analysis: ProjectAnalysis): ScenarioDecision {
    // Fresh onboarding scenario
    if (analysis.appType === 'aspnetcore' && 
        !analysis.hasLegacyAiSdk && 
        !analysis.hasOpenTelemetry) {
      return this.createFreshOnboardingDecision();
    }

    // Migration scenario
    if (analysis.appType === 'aspnetcore' && analysis.hasLegacyAiSdk) {
      return this.createMigrationDecision(analysis);
    }

    // Already configured
    if (analysis.hasOpenTelemetry) {
      return {
        action: 'no_action_needed',
        learningResources: [],
        implementationSteps: []
      };
    }

    throw new Error('Unable to determine scenario');
  }

  /**
   * Create decision for fresh onboarding
   */
  private createFreshOnboardingDecision(): ScenarioDecision {
    const scenario = this.config.scenarios.fresh_aspnetcore_onboarding;

    return {
      action: 'fresh_onboarding',
      packageToInstall: scenario.package,
      packagesToRemove: [],
      learningResources: scenario.learning_resources,
      implementationSteps: [
        {
          step: 1,
          action: 'install_package',
          description: 'Install Azure.Monitor.OpenTelemetry.AspNetCore package',
          template: 'install-package.template',
          validation: 'package_installed'
        },
        {
          step: 2,
          action: 'configure_connection_string',
          description: 'Set APPLICATIONINSIGHTS_CONNECTION_STRING environment variable',
          template: 'connection-string.template',
          validation: 'connection_string_configured'
        },
        {
          step: 3,
          action: 'modify_program_cs',
          description: 'Add UseAzureMonitor() to Program.cs',
          template: 'minimal-program-cs.template',
          validation: 'use_azure_monitor_called'
        },
        {
          step: 4,
          action: 'verify_setup',
          description: 'Run application and verify telemetry in Azure Portal',
          validation: 'telemetry_flowing'
        }
      ]
    };
  }

  /**
   * Create decision for migration scenario
   */
  private createMigrationDecision(analysis: ProjectAnalysis): ScenarioDecision {
    return {
      action: 'migration_2x_to_3x',
      packageToInstall: 'Azure.Monitor.OpenTelemetry.AspNetCore',
      packagesToRemove: analysis.legacyPackages,
      learningResources: [
        'breaking-changes/TelemetryClient/parameterless-constructor.md',
        'breaking-changes/TelemetryConfiguration/Active-removed.md',
        'transformations/ITelemetryInitializer/to-activity-processor.md'
      ],
      implementationSteps: [
        {
          step: 1,
          action: 'analyze_legacy_code',
          description: 'Identify legacy Application Insights patterns'
        },
        {
          step: 2,
          action: 'remove_legacy_packages',
          description: 'Remove Microsoft.ApplicationInsights.* packages'
        },
        {
          step: 3,
          action: 'install_opentelemetry',
          description: 'Install Azure.Monitor.OpenTelemetry.AspNetCore'
        },
        {
          step: 4,
          action: 'migrate_code',
          description: 'Transform legacy patterns to OpenTelemetry'
        }
      ]
    };
  }

  /**
   * Analyze .csproj file
   */
  private async analyzeCsprojFile(csprojPath: string): Promise<ProjectAnalysis> {
    const content = fs.readFileSync(csprojPath, 'utf-8');
    
    // Detect project type
    const isAspNetCore = this.config.detection.aspnetcore_indicators.some(
      (indicator: string) => content.includes(indicator)
    );

    // Detect legacy AI SDK packages
    const legacyPackages: string[] = [];
    const hasLegacyAiSdk = this.config.detection.legacy_packages.some(
      (pkg: string) => {
        if (content.includes(pkg)) {
          legacyPackages.push(pkg);
          return true;
        }
        return false;
      }
    );

    // Detect OpenTelemetry packages
    const hasOpenTelemetry = this.config.detection.opentelemetry_packages.some(
      (pkg: string) => content.includes(pkg)
    );

    // Extract target framework
    const frameworkMatch = content.match(/<TargetFramework>(.*?)<\/TargetFramework>/);
    const targetFramework = frameworkMatch ? frameworkMatch[1] : 'unknown';

    // Determine scenario
    let scenario = 'unknown';
    let confidence = 1.0;

    if (isAspNetCore && !hasLegacyAiSdk && !hasOpenTelemetry) {
      scenario = 'fresh_onboarding';
    } else if (isAspNetCore && hasLegacyAiSdk) {
      scenario = 'migration_2x_to_3x';
    } else if (hasOpenTelemetry) {
      scenario = 'already_configured';
    }

    return {
      scenario,
      appType: isAspNetCore ? 'aspnetcore' : 'unknown',
      targetFramework,
      hasLegacyAiSdk,
      hasOpenTelemetry,
      legacyPackages,
      programCsPattern: this.detectProgramCsPattern(path.dirname(csprojPath)),
      confidence
    };
  }

  /**
   * Detect Program.cs pattern (minimal hosting vs traditional)
   */
  private detectProgramCsPattern(projectDir: string): string {
    const programCsPath = path.join(projectDir, 'Program.cs');
    
    if (!fs.existsSync(programCsPath)) {
      return 'not_found';
    }

    const content = fs.readFileSync(programCsPath, 'utf-8');

    // Minimal hosting model (.NET 6+)
    if (content.includes('var builder = WebApplication.CreateBuilder') ||
        content.includes('WebApplication.CreateBuilder')) {
      return 'minimal_hosting';
    }

    // Traditional hosting model
    if (content.includes('CreateHostBuilder') || content.includes('IHostBuilder')) {
      return 'traditional_hosting';
    }

    return 'unknown';
  }

  /**
   * Find all .csproj files in directory
   */
  private findCsprojFiles(dir: string): string[] {
    const results: string[] = [];

    function walk(directory: string) {
      const files = fs.readdirSync(directory);

      for (const file of files) {
        const filePath = path.join(directory, file);
        const stat = fs.statSync(filePath);

        if (stat.isDirectory()) {
          // Skip common non-project directories
          if (!['bin', 'obj', 'node_modules', '.git'].includes(file)) {
            walk(filePath);
          }
        } else if (file.endsWith('.csproj')) {
          results.push(filePath);
        }
      }
    }

    walk(dir);
    return results;
  }
}

/**
 * Validation engine
 */
export class ValidationEngine {
  private config: any;

  constructor(configPath: string) {
    this.config = JSON.parse(fs.readFileSync(configPath, 'utf-8')).mcp_server;
  }

  /**
   * Validate fresh onboarding implementation
   */
  async validateFreshOnboarding(projectPath: string): Promise<ValidationResult> {
    const checks: ValidationCheck[] = [];

    // Check 1: Package installed
    checks.push(await this.checkPackageInstalled(
      projectPath,
      'Azure.Monitor.OpenTelemetry.AspNetCore'
    ));

    // Check 2: No legacy packages
    checks.push(await this.checkNoLegacyPackages(projectPath));

    // Check 3: UseAzureMonitor() called
    checks.push(await this.checkUseAzureMonitorCalled(projectPath));

    // Check 4: Connection string configured
    checks.push(await this.checkConnectionStringConfigured(projectPath));

    const isValid = checks.every(check => check.passed);

    return {
      isValid,
      checks,
      warnings: checks.filter(c => c.warning).map(c => c.message),
      errors: checks.filter(c => !c.passed).map(c => c.message)
    };
  }

  private async checkPackageInstalled(
    projectPath: string,
    packageName: string
  ): Promise<ValidationCheck> {
    const csprojFiles = this.findCsprojFiles(projectPath);
    
    for (const csproj of csprojFiles) {
      const content = fs.readFileSync(csproj, 'utf-8');
      if (content.includes(packageName)) {
        return {
          name: 'package_installed',
          passed: true,
          message: `${packageName} is installed`
        };
      }
    }

    return {
      name: 'package_installed',
      passed: false,
      message: `${packageName} is not installed`
    };
  }

  private async checkNoLegacyPackages(projectPath: string): Promise<ValidationCheck> {
    const csprojFiles = this.findCsprojFiles(projectPath);
    const foundLegacyPackages: string[] = [];

    for (const csproj of csprojFiles) {
      const content = fs.readFileSync(csproj, 'utf-8');
      
      for (const legacyPkg of this.config.detection.legacy_packages) {
        if (content.includes(legacyPkg)) {
          foundLegacyPackages.push(legacyPkg);
        }
      }
    }

    if (foundLegacyPackages.length > 0) {
      return {
        name: 'no_legacy_packages',
        passed: false,
        message: `Legacy packages found: ${foundLegacyPackages.join(', ')}`
      };
    }

    return {
      name: 'no_legacy_packages',
      passed: true,
      message: 'No legacy Application Insights packages found'
    };
  }

  private async checkUseAzureMonitorCalled(projectPath: string): Promise<ValidationCheck> {
    const programCsPath = path.join(projectPath, 'Program.cs');

    if (!fs.existsSync(programCsPath)) {
      return {
        name: 'use_azure_monitor_called',
        passed: false,
        message: 'Program.cs not found'
      };
    }

    const content = fs.readFileSync(programCsPath, 'utf-8');

    if (content.includes('UseAzureMonitor')) {
      return {
        name: 'use_azure_monitor_called',
        passed: true,
        message: 'UseAzureMonitor() is called in Program.cs'
      };
    }

    return {
      name: 'use_azure_monitor_called',
      passed: false,
      message: 'UseAzureMonitor() is not called in Program.cs'
    };
  }

  private async checkConnectionStringConfigured(projectPath: string): Promise<ValidationCheck> {
    // Check environment variable (this is aspirational - would need process access)
    // Check appsettings.json
    const appsettingsPath = path.join(projectPath, 'appsettings.json');
    if (fs.existsSync(appsettingsPath)) {
      const content = fs.readFileSync(appsettingsPath, 'utf-8');
      if (content.includes('ConnectionString') || content.includes('APPLICATIONINSIGHTS_CONNECTION_STRING')) {
        return {
          name: 'connection_string_configured',
          passed: true,
          message: 'Connection string found in appsettings.json'
        };
      }
    }

    // Check launchSettings.json
    const launchSettingsPath = path.join(projectPath, 'Properties', 'launchSettings.json');
    if (fs.existsSync(launchSettingsPath)) {
      const content = fs.readFileSync(launchSettingsPath, 'utf-8');
      if (content.includes('APPLICATIONINSIGHTS_CONNECTION_STRING')) {
        return {
          name: 'connection_string_configured',
          passed: true,
          message: 'Connection string found in launchSettings.json'
        };
      }
    }

    // Check Program.cs for hard-coded connection string
    const programCsPath = path.join(projectPath, 'Program.cs');
    if (fs.existsSync(programCsPath)) {
      const content = fs.readFileSync(programCsPath, 'utf-8');
      if (content.includes('ConnectionString =') || content.includes('ConnectionString=')) {
        return {
          name: 'connection_string_configured',
          passed: true,
          message: 'Connection string found in Program.cs',
          warning: true
        };
      }
    }

    return {
      name: 'connection_string_configured',
      passed: false,
      message: 'Connection string not found. Set APPLICATIONINSIGHTS_CONNECTION_STRING environment variable'
    };
  }

  private findCsprojFiles(dir: string): string[] {
    const results: string[] = [];

    function walk(directory: string) {
      if (!fs.existsSync(directory)) return;
      
      const files = fs.readdirSync(directory);

      for (const file of files) {
        const filePath = path.join(directory, file);
        const stat = fs.statSync(filePath);

        if (stat.isDirectory()) {
          if (!['bin', 'obj', 'node_modules', '.git'].includes(file)) {
            walk(filePath);
          }
        } else if (file.endsWith('.csproj')) {
          results.push(filePath);
        }
      }
    }

    walk(dir);
    return results;
  }
}

export interface ValidationResult {
  isValid: boolean;
  checks: ValidationCheck[];
  warnings: string[];
  errors: string[];
}

export interface ValidationCheck {
  name: string;
  passed: boolean;
  message: string;
  warning?: boolean;
}
