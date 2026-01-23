using System.Xml.Linq;

namespace ApplicationInsights.Mcp.Server;

public class ValidationEngine
{
    public async Task<ValidationResult> ValidateFreshOnboarding(string projectPath)
    {
        var result = new ValidationResult { IsValid = true };

        // Check package installation
        var packageInstalled = await ValidatePackageInstalled(projectPath, "Azure.Monitor.OpenTelemetry.AspNetCore");
        if (!packageInstalled)
        {
            result.IsValid = false;
            result.Issues.Add("Azure.Monitor.OpenTelemetry.AspNetCore package not found in project");
        }

        // Check Program.cs has UseAzureMonitor()
        var programCsValid = await ValidateProgramCsHasUseAzureMonitor(projectPath);
        if (!programCsValid)
        {
            result.IsValid = false;
            result.Issues.Add("Program.cs does not call UseAzureMonitor()");
        }

        // Check connection string configuration
        var connectionStringConfigured = await ValidateConnectionString(projectPath);
        if (!connectionStringConfigured)
        {
            result.Warnings.Add("Connection string not found in environment variables or appsettings.json");
        }

        return result;
    }

    public async Task<ValidationResult> ValidateMigration(string projectPath)
    {
        var result = new ValidationResult { IsValid = true };

        // Check legacy packages removed
        var legacyPackagesRemoved = await ValidateNoLegacyPackages(projectPath);
        if (!legacyPackagesRemoved)
        {
            result.IsValid = false;
            result.Issues.Add("Legacy Microsoft.ApplicationInsights packages still present");
        }

        // Check OpenTelemetry package installed
        var otPackageInstalled = await ValidatePackageInstalled(projectPath, "Azure.Monitor.OpenTelemetry.AspNetCore");
        if (!otPackageInstalled)
        {
            result.IsValid = false;
            result.Issues.Add("Azure.Monitor.OpenTelemetry.AspNetCore package not installed");
        }

        // Check for legacy patterns in code
        var legacyPatternsPresent = await DetectLegacyPatterns(projectPath);
        if (legacyPatternsPresent.Any())
        {
            result.Warnings.Add($"Legacy patterns still present: {string.Join(", ", legacyPatternsPresent)}");
        }

        return result;
    }

    private async Task<bool> ValidatePackageInstalled(string projectPath, string packageId)
    {
        var csprojFiles = Directory.GetFiles(projectPath, "*.csproj");
        foreach (var csproj in csprojFiles)
        {
            var doc = await XDocument.LoadAsync(File.OpenRead(csproj), LoadOptions.None, CancellationToken.None);
            var packageRefs = doc.Descendants("PackageReference")
                .Where(e => e.Attribute("Include")?.Value == packageId);
            if (packageRefs.Any())
            {
                return true;
            }
        }
        return false;
    }

    private async Task<bool> ValidateNoLegacyPackages(string projectPath)
    {
        var csprojFiles = Directory.GetFiles(projectPath, "*.csproj");
        foreach (var csproj in csprojFiles)
        {
            var doc = await XDocument.LoadAsync(File.OpenRead(csproj), LoadOptions.None, CancellationToken.None);
            var legacyPackages = doc.Descendants("PackageReference")
                .Where(e =>
                {
                    var include = e.Attribute("Include")?.Value ?? "";
                    return include.StartsWith("Microsoft.ApplicationInsights.") &&
                           include != "Microsoft.ApplicationInsights.WorkerService";
                });
            if (legacyPackages.Any())
            {
                return false;
            }
        }
        return true;
    }

    private async Task<bool> ValidateProgramCsHasUseAzureMonitor(string projectPath)
    {
        var programCsPath = Path.Combine(projectPath, "Program.cs");
        if (!File.Exists(programCsPath))
        {
            return false;
        }

        var content = await File.ReadAllTextAsync(programCsPath);
        return content.Contains("UseAzureMonitor()");
    }

    private async Task<bool> ValidateConnectionString(string projectPath)
    {
        // Check environment variable
        var envConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
        if (!string.IsNullOrEmpty(envConnectionString))
        {
            return true;
        }

        // Check appsettings.json
        var appsettingsPath = Path.Combine(projectPath, "appsettings.json");
        if (File.Exists(appsettingsPath))
        {
            var content = await File.ReadAllTextAsync(appsettingsPath);
            if (content.Contains("APPLICATIONINSIGHTS_CONNECTION_STRING"))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<List<string>> DetectLegacyPatterns(string projectPath)
    {
        var patterns = new List<string>();
        var csFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories);

        foreach (var csFile in csFiles)
        {
            var content = await File.ReadAllTextAsync(csFile);

            if (content.Contains("TelemetryConfiguration.Active"))
            {
                patterns.Add("TelemetryConfiguration.Active");
            }
            if (content.Contains("AddApplicationInsightsTelemetry()"))
            {
                patterns.Add("AddApplicationInsightsTelemetry()");
            }
            if (content.Contains("ITelemetryInitializer"))
            {
                patterns.Add("ITelemetryInitializer");
            }
            if (content.Contains("ITelemetryProcessor"))
            {
                patterns.Add("ITelemetryProcessor");
            }
        }

        return patterns.Distinct().ToList();
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Issues { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
