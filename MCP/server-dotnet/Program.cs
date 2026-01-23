using ApplicationInsights.Mcp.Server;

var learningLibraryPath = args.Length > 0 
    ? args[0] 
    : Path.Combine(Directory.GetCurrentDirectory(), "..", "learn");

Console.WriteLine($"Starting Application Insights Migration MCP Server");
Console.WriteLine($"Learning Library Path: {learningLibraryPath}");

var server = new McpServer(learningLibraryPath);
await server.RunAsync();
