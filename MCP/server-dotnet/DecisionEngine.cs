namespace ApplicationInsights.Mcp.Server;

public class DecisionEngine
{
    public ScenarioDecision MakeDecision(ProjectAnalysis analysis)
    {
        // Fresh onboarding scenario
        if (analysis.AppType == "aspnetcore" && 
            !analysis.HasLegacyAiSdk && 
            !analysis.HasOpenTelemetry)
        {
            return CreateFreshOnboardingDecision();
        }

        // Migration scenario
        if (analysis.AppType == "aspnetcore" && analysis.HasLegacyAiSdk)
        {
            return CreateMigrationDecision(analysis);
        }

        // Already configured
        if (analysis.HasOpenTelemetry)
        {
            return new ScenarioDecision
            {
                Action = "no_action_needed",
                LearningResources = new List<string>(),
                ImplementationSteps = new List<ImplementationStep>()
            };
        }

        throw new InvalidOperationException("Unable to determine scenario");
    }

    private ScenarioDecision CreateFreshOnboardingDecision()
    {
        return new ScenarioDecision
        {
            Action = "fresh_onboarding",
            PackageToInstall = "Azure.Monitor.OpenTelemetry.AspNetCore",
            PackagesToRemove = new List<string>(),
            LearningResources = new List<string>
            {
                "common-scenarios/fresh-aspnetcore-onboarding.md",
                "azure-monitor-exporter/connection-string.md",
                "examples/configuration/basic-setup-aspnetcore.md",
                "concepts/opentelemetry-pipeline.md"
            },
            ImplementationSteps = new List<ImplementationStep>
            {
                new()
                {
                    Step = 1,
                    Action = "install_package",
                    Description = "Install Azure.Monitor.OpenTelemetry.AspNetCore package",
                    Template = "install-package.template",
                    Validation = "package_installed"
                },
                new()
                {
                    Step = 2,
                    Action = "configure_connection_string",
                    Description = "Set APPLICATIONINSIGHTS_CONNECTION_STRING environment variable",
                    Template = "connection-string.template",
                    Validation = "connection_string_configured"
                },
                new()
                {
                    Step = 3,
                    Action = "modify_program_cs",
                    Description = "Add UseAzureMonitor() to Program.cs",
                    Template = "minimal-program-cs.template",
                    Validation = "use_azure_monitor_called"
                },
                new()
                {
                    Step = 4,
                    Action = "verify_setup",
                    Description = "Run application and verify telemetry in Azure Portal",
                    Validation = "telemetry_flowing"
                }
            }
        };
    }

    private ScenarioDecision CreateMigrationDecision(ProjectAnalysis analysis)
    {
        return new ScenarioDecision
        {
            Action = "migration_2x_to_3x",
            PackageToInstall = "Azure.Monitor.OpenTelemetry.AspNetCore",
            PackagesToRemove = analysis.LegacyPackages,
            LearningResources = new List<string>
            {
                "breaking-changes/TelemetryClient/parameterless-constructor.md",
                "breaking-changes/TelemetryConfiguration/Active-removed.md",
                "transformations/ITelemetryInitializer/to-activity-processor.md",
                "transformations/ITelemetryProcessor/to-activity-processor.md",
                "examples/activity-processors/ClientErrorProcessor.md",
                "examples/activity-processors/SuccessfulDependencyFilter.md"
            },
            ImplementationSteps = new List<ImplementationStep>
            {
                new()
                {
                    Step = 1,
                    Action = "analyze_legacy_code",
                    Description = "Identify legacy Application Insights patterns"
                },
                new()
                {
                    Step = 2,
                    Action = "remove_legacy_packages",
                    Description = "Remove Microsoft.ApplicationInsights.* packages"
                },
                new()
                {
                    Step = 3,
                    Action = "install_opentelemetry",
                    Description = "Install Azure.Monitor.OpenTelemetry.AspNetCore"
                },
                new()
                {
                    Step = 4,
                    Action = "migrate_code",
                    Description = "Transform legacy patterns to OpenTelemetry"
                }
            }
        };
    }
}

public class ScenarioDecision
{
    public string Action { get; set; } = string.Empty;
    public string? PackageToInstall { get; set; }
    public List<string> PackagesToRemove { get; set; } = new();
    public List<string> LearningResources { get; set; } = new();
    public List<ImplementationStep> ImplementationSteps { get; set; } = new();
}

public class ImplementationStep
{
    public int Step { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Template { get; set; }
    public string? Validation { get; set; }
}
