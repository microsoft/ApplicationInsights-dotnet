using System.Text.RegularExpressions;

namespace ApplicationInsights.Mcp.Server;

public class ProjectAnalyzer
{
    private readonly string[] _legacyPackages = 
    {
        "Microsoft.ApplicationInsights.AspNetCore",
        "Microsoft.ApplicationInsights",
        "Microsoft.ApplicationInsights.WorkerService",
        "Microsoft.ApplicationInsights.Web",
        "Microsoft.ApplicationInsights.WindowsServer"
    };

    private readonly string[] _openTelemetryPackages = 
    {
        "Azure.Monitor.OpenTelemetry.AspNetCore",
        "Azure.Monitor.OpenTelemetry.Exporter",
        "OpenTelemetry.Instrumentation.AspNetCore"
    };

    public async Task<ProjectAnalysis> AnalyzeProjectAsync(string projectPath)
    {
        var csprojFiles = FindCsprojFiles(projectPath);

        if (csprojFiles.Length == 0)
        {
            throw new InvalidOperationException("No .csproj files found in project");
        }

        // Analyze first ASP.NET Core project found
        foreach (var csproj in csprojFiles)
        {
            var analysis = await AnalyzeCsprojFileAsync(csproj);
            
            if (analysis.AppType == "aspnetcore")
            {
                return analysis;
            }
        }

        throw new InvalidOperationException("No ASP.NET Core projects found");
    }

    private async Task<ProjectAnalysis> AnalyzeCsprojFileAsync(string csprojPath)
    {
        var content = await File.ReadAllTextAsync(csprojPath);
        var projectDir = Path.GetDirectoryName(csprojPath)!;

        // Detect project type
        var isAspNetCore = content.Contains("Microsoft.NET.Sdk.Web") || 
                          content.Contains("Microsoft.AspNetCore");

        // Detect legacy AI SDK packages
        var legacyPackages = _legacyPackages
            .Where(pkg => content.Contains(pkg))
            .ToList();
        var hasLegacyAiSdk = legacyPackages.Any();

        // Detect OpenTelemetry packages
        var hasOpenTelemetry = _openTelemetryPackages
            .Any(pkg => content.Contains(pkg));

        // Extract target framework
        var frameworkMatch = Regex.Match(content, @"<TargetFramework>(.*?)</TargetFramework>");
        var targetFramework = frameworkMatch.Success ? frameworkMatch.Groups[1].Value : "unknown";

        // Determine scenario
        string scenario;
        if (isAspNetCore && !hasLegacyAiSdk && !hasOpenTelemetry)
        {
            scenario = "fresh_onboarding";
        }
        else if (isAspNetCore && hasLegacyAiSdk)
        {
            scenario = "migration_2x_to_3x";
        }
        else if (hasOpenTelemetry)
        {
            scenario = "already_configured";
        }
        else
        {
            scenario = "unknown";
        }

        return new ProjectAnalysis
        {
            Scenario = scenario,
            AppType = isAspNetCore ? "aspnetcore" : "unknown",
            TargetFramework = targetFramework,
            HasLegacyAiSdk = hasLegacyAiSdk,
            HasOpenTelemetry = hasOpenTelemetry,
            LegacyPackages = legacyPackages,
            ProgramCsPattern = DetectProgramCsPattern(projectDir),
            Confidence = 1.0
        };
    }

    private string DetectProgramCsPattern(string projectDir)
    {
        var programCsPath = Path.Combine(projectDir, "Program.cs");
        
        if (!File.Exists(programCsPath))
        {
            return "not_found";
        }

        var content = File.ReadAllText(programCsPath);

        // Minimal hosting model (.NET 6+)
        if (content.Contains("var builder = WebApplication.CreateBuilder") ||
            content.Contains("WebApplication.CreateBuilder"))
        {
            return "minimal_hosting";
        }

        // Traditional hosting model
        if (content.Contains("CreateHostBuilder") || content.Contains("IHostBuilder"))
        {
            return "traditional_hosting";
        }

        return "unknown";
    }

    private string[] FindCsprojFiles(string dir)
    {
        var results = new List<string>();
        var excludeDirs = new[] { "bin", "obj", "node_modules", ".git" };

        void Walk(string directory)
        {
            if (!Directory.Exists(directory)) return;

            foreach (var file in Directory.GetFiles(directory, "*.csproj"))
            {
                results.Add(file);
            }

            foreach (var subDir in Directory.GetDirectories(directory))
            {
                var dirName = Path.GetFileName(subDir);
                if (!excludeDirs.Contains(dirName))
                {
                    Walk(subDir);
                }
            }
        }

        Walk(dir);
        return results.ToArray();
    }
}

public class ProjectAnalysis
{
    public string Scenario { get; set; } = string.Empty;
    public string AppType { get; set; } = string.Empty;
    public string TargetFramework { get; set; } = string.Empty;
    public bool HasLegacyAiSdk { get; set; }
    public bool HasOpenTelemetry { get; set; }
    public List<string> LegacyPackages { get; set; } = new();
    public string ProgramCsPattern { get; set; } = string.Empty;
    public double Confidence { get; set; }
}
