using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApplicationInsights.Mcp.Server;

public class McpServer
{
    private readonly ProjectAnalyzer _analyzer;
    private readonly DecisionEngine _decisionEngine;
    private readonly ValidationEngine _validationEngine;
    private readonly string _learningLibraryPath;

    public McpServer(string learningLibraryPath)
    {
        _analyzer = new ProjectAnalyzer();
        _decisionEngine = new DecisionEngine();
        _validationEngine = new ValidationEngine();
        _learningLibraryPath = learningLibraryPath;
    }

    public async Task RunAsync()
    {
        // Simple stdio-based MCP implementation
        Console.Error.WriteLine("MCP Server started - waiting for requests...");
        
        while (true)
        {
            var line = Console.ReadLine();
            if (string.IsNullOrEmpty(line)) break;
            
            try
            {
                var request = JsonSerializer.Deserialize<McpRequest>(line);
                if (request == null) continue;

                var response = await HandleRequestAsync(request);
                Console.WriteLine(JsonSerializer.Serialize(response));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error processing request: {ex.Message}");
            }
        }
    }

    private async Task<McpResponse> HandleRequestAsync(McpRequest request)
    {
        return request.Method switch
        {
            "analyze_project" => await AnalyzeProjectAsync(request),
            "get_guide" => await GetGuideAsync(request),
            "validate_implementation" => await ValidateImplementationAsync(request),
            "get_code_template" => await GetCodeTemplateAsync(request),
            _ => new McpResponse { Success = false, Error = $"Unknown method: {request.Method}" }
        };
    }

    private async Task<McpResponse> AnalyzeProjectAsync(McpRequest request)
    {
        var projectPath = request.Parameters?["project_path"]?.ToString();
        if (string.IsNullOrEmpty(projectPath))
        {
            return new McpResponse { Success = false, Error = "project_path required" };
        }

        var analysis = await _analyzer.AnalyzeProjectAsync(projectPath);
        var decision = _decisionEngine.MakeDecision(analysis);

        return new McpResponse
        {
            Success = true,
            Result = new
            {
                analysis = new
                {
                    app_type = analysis.AppType,
                    has_legacy_ai_sdk = analysis.HasLegacyAiSdk,
                    has_opentelemetry = analysis.HasOpenTelemetry,
                    legacy_packages = analysis.LegacyPackages,
                    legacy_patterns = analysis.LegacyPatterns
                },
                scenario = decision.Action,
                next_steps = decision.ImplementationSteps.Select(s => new
                {
                    step = s.Step,
                    action = s.Action,
                    description = s.Description
                }),
                learning_resources = decision.LearningResources
            }
        };
    }

    private async Task<McpResponse> GetGuideAsync(McpRequest request)
    {
        var guidePath = request.Parameters?["guide_path"]?.ToString();
        if (string.IsNullOrEmpty(guidePath))
        {
            return new McpResponse { Success = false, Error = "guide_path required" };
        }

        var fullPath = Path.Combine(_learningLibraryPath, guidePath);
        if (!File.Exists(fullPath))
        {
            return new McpResponse { Success = false, Error = $"Guide not found: {guidePath}" };
        }

        var content = await File.ReadAllTextAsync(fullPath);
        return new McpResponse
        {
            Success = true,
            Result = new { content, path = guidePath }
        };
    }

    private async Task<McpResponse> ValidateImplementationAsync(McpRequest request)
    {
        var projectPath = request.Parameters?["project_path"]?.ToString();
        if (string.IsNullOrEmpty(projectPath))
        {
            return new McpResponse { Success = false, Error = "project_path required" };
        }

        var analysis = await _analyzer.AnalyzeProjectAsync(projectPath);
        var validation = _validationEngine.Validate(analysis);

        return new McpResponse
        {
            Success = true,
            Result = new
            {
                is_valid = validation.IsValid,
                issues = validation.Issues.Select(i => new
                {
                    severity = i.Severity,
                    category = i.Category,
                    message = i.Message,
                    suggestion = i.Suggestion
                })
            }
        };
    }

    private Task<McpResponse> GetCodeTemplateAsync(McpRequest request)
    {
        var templateName = request.Parameters?["template_name"]?.ToString();
        if (string.IsNullOrEmpty(templateName))
        {
            return Task.FromResult(new McpResponse { Success = false, Error = "template_name required" });
        }

        // For now, return basic templates
        var templates = new Dictionary<string, string>
        {
            ["fresh_onboarding_program_cs"] = @"var builder = WebApplication.CreateBuilder(args);

// Add Azure Monitor OpenTelemetry
builder.Services.AddOpenTelemetry()
    .UseAzureMonitor();

builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();",
            
            ["appsettings_connection_string"] = @"{
  ""ApplicationInsights"": {
    ""ConnectionString"": ""InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://...""
  }
}"
        };

        if (!templates.TryGetValue(templateName, out var template))
        {
            return Task.FromResult(new McpResponse { Success = false, Error = $"Template not found: {templateName}" });
        }

        return Task.FromResult(new McpResponse
        {
            Success = true,
            Result = new { template_name = templateName, code = template }
        });
    }
}

public class McpRequest
{
    [JsonPropertyName("method")]
    public string Method { get; set; } = "";
    
    [JsonPropertyName("parameters")]
    public Dictionary<string, object>? Parameters { get; set; }
}

public class McpResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("result")]
    public object? Result { get; set; }
    
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
