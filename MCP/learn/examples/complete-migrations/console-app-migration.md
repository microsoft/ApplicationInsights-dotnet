# Complete Console Application Migration Example

**Category:** Complete Migration Example  
**Applies to:** Migrating .NET Console App from Application Insights 2.x to 3.x  
**Related:** [activity-source.md](../../opentelemetry-fundamentals/activity-source.md)

## Overview

This example shows migrating a console application from Application Insights 2.x to 3.x, including manual instrumentation and dependency tracking.

## Before: Application Insights 2.x

### Program.cs (2.x)

```csharp
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

class Program
{
    private static TelemetryClient _telemetryClient;
    
    static async Task Main(string[] args)
    {
        // Initialize Application Insights
        var config = TelemetryConfiguration.CreateDefault();
        config.InstrumentationKey = "abc123-your-key-here";
        config.TelemetryInitializers.Add(new OperationCorrelatingTelemetryInitializer());
        
        _telemetryClient = new TelemetryClient(config);
        
        // Track application start
        _telemetryClient.TrackEvent("ApplicationStarted");
        
        try
        {
            await ProcessDataAsync();
            
            _telemetryClient.TrackEvent("ApplicationCompleted");
        }
        catch (Exception ex)
        {
            _telemetryClient.TrackException(ex);
            throw;
        }
        finally
        {
            // Flush before exit
            _telemetryClient.Flush();
            await Task.Delay(5000); // Wait for flush
        }
    }
    
    static async Task ProcessDataAsync()
    {
        using var operation = _telemetryClient.StartOperation<RequestTelemetry>("ProcessData");
        operation.Telemetry.Properties["recordCount"] = "1000";
        
        try
        {
            // Fetch data
            var data = await FetchDataAsync();
            
            // Transform data
            var transformed = await TransformDataAsync(data);
            
            // Save results
            await SaveResultsAsync(transformed);
            
            operation.Telemetry.Success = true;
        }
        catch (Exception ex)
        {
            operation.Telemetry.Success = false;
            _telemetryClient.TrackException(ex);
            throw;
        }
    }
    
    static async Task<string[]> FetchDataAsync()
    {
        using var operation = _telemetryClient.StartOperation<DependencyTelemetry>("FetchData");
        operation.Telemetry.Type = "HTTP";
        operation.Telemetry.Target = "api.example.com";
        
        try
        {
            using var client = new HttpClient();
            var response = await client.GetStringAsync("https://api.example.com/data");
            
            operation.Telemetry.Success = true;
            return response.Split('\n');
        }
        catch (Exception ex)
        {
            operation.Telemetry.Success = false;
            _telemetryClient.TrackException(ex);
            throw;
        }
    }
    
    static async Task<string[]> TransformDataAsync(string[] data)
    {
        using var operation = _telemetryClient.StartOperation<DependencyTelemetry>("TransformData");
        operation.Telemetry.Type = "InProc";
        
        try
        {
            await Task.Delay(100); // Simulate work
            var result = data.Select(d => d.ToUpper()).ToArray();
            
            operation.Telemetry.Success = true;
            return result;
        }
        catch (Exception ex)
        {
            operation.Telemetry.Success = false;
            _telemetryClient.TrackException(ex);
            throw;
        }
    }
    
    static async Task SaveResultsAsync(string[] data)
    {
        using var operation = _telemetryClient.StartOperation<DependencyTelemetry>("SaveResults");
        operation.Telemetry.Type = "SQL";
        operation.Telemetry.Target = "mydb.database.windows.net";
        
        try
        {
            await Task.Delay(200); // Simulate database write
            
            operation.Telemetry.Success = true;
        }
        catch (Exception ex)
        {
            operation.Telemetry.Success = false;
            _telemetryClient.TrackException(ex);
            throw;
        }
    }
}
```

## After: Application Insights 3.x

### Program.cs (3.x)

```csharp
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.WorkerService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

class Program
{
    private static readonly ActivitySource ActivitySource = new("MyConsoleApp");
    
    static async Task Main(string[] args)
    {
        // Build host with Application Insights
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddApplicationInsightsTelemetryWorkerService(options =>
                {
                    options.ConnectionString = context.Configuration["ApplicationInsights:ConnectionString"];
                })
                .ConfigureOpenTelemetryBuilder(otel =>
                {
                    // Register custom ActivitySource
                    otel.AddSource("MyConsoleApp");
                    
                    // Configure Resource (Cloud Role Name)
                    otel.ConfigureResource(resource =>
                    {
                        resource.AddService(
                            serviceName: "MyConsoleApp",
                            serviceVersion: "1.0.0");
                    });
                });
                
                services.AddHostedService<ConsoleAppService>();
            })
            .Build();
        
        await host.RunAsync();
    }
}

class ConsoleAppService : BackgroundService
{
    private static readonly ActivitySource ActivitySource = new("MyConsoleApp");
    private readonly ILogger<ConsoleAppService> _logger;
    private readonly TelemetryClient _telemetryClient;
    
    public ConsoleAppService(
        ILogger<ConsoleAppService> logger,
        TelemetryClient telemetryClient)
    {
        _logger = logger;
        _telemetryClient = telemetryClient;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var activity = ActivitySource.StartActivity("ApplicationStarted", ActivityKind.Internal);
        _telemetryClient.TrackEvent("ApplicationStarted");
        
        try
        {
            await ProcessDataAsync();
            
            _telemetryClient.TrackEvent("ApplicationCompleted");
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Application failed");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
        finally
        {
            // Flush before exit
            _telemetryClient.Flush();
            await Task.Delay(5000, stoppingToken);
        }
    }
    
    async Task ProcessDataAsync()
    {
        using var activity = ActivitySource.StartActivity("ProcessData", ActivityKind.Internal);
        activity?.SetTag("record.count", 1000);
        
        try
        {
            // Fetch data
            var data = await FetchDataAsync();
            
            // Transform data
            var transformed = await TransformDataAsync(data);
            
            // Save results
            await SaveResultsAsync(transformed);
            
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ProcessData failed");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
    
    async Task<string[]> FetchDataAsync()
    {
        using var activity = ActivitySource.StartActivity("FetchData", ActivityKind.Client);
        activity?.SetTag("http.url", "https://api.example.com/data");
        activity?.SetTag("server.address", "api.example.com");
        
        try
        {
            using var client = new HttpClient();
            var response = await client.GetStringAsync("https://api.example.com/data");
            
            activity?.SetStatus(ActivityStatusCode.Ok);
            return response.Split('\n');
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FetchData failed");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
    
    async Task<string[]> TransformDataAsync(string[] data)
    {
        using var activity = ActivitySource.StartActivity("TransformData", ActivityKind.Internal);
        activity?.SetTag("input.count", data.Length);
        
        try
        {
            await Task.Delay(100); // Simulate work
            var result = data.Select(d => d.ToUpper()).ToArray();
            
            activity?.SetTag("output.count", result.Length);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TransformData failed");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
    
    async Task SaveResultsAsync(string[] data)
    {
        using var activity = ActivitySource.StartActivity("SaveResults", ActivityKind.Client);
        activity?.SetTag("db.system", "mssql");
        activity?.SetTag("server.address", "mydb.database.windows.net");
        activity?.SetTag("record.count", data.Length);
        
        try
        {
            await Task.Delay(200); // Simulate database write
            
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SaveResults failed");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}
```

### appsettings.json (3.x)

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=abc123...;IngestionEndpoint=https://...",
    "CloudRoleName": "MyConsoleApp"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

## Key Differences

### Removed
- `TelemetryConfiguration.CreateDefault()` - Use DI with Host
- `new TelemetryClient(config)` - Inject TelemetryClient
- `StartOperation<T>()` - Use ActivitySource.StartActivity()
- Manual success tracking - Automatic via ActivityStatusCode
- Manual flush delay - Still needed before app exit

### Added
- `Host.CreateDefaultBuilder()` - Standard .NET hosting
- `AddApplicationInsightsTelemetryWorkerService()` - Worker service setup
- `ActivitySource` - Custom instrumentation
- `ActivityKind` - Explicit operation types
- `BackgroundService` - Proper hosted service pattern
- `ILogger` - Structured logging

### Simplified
- Dependency injection for TelemetryClient
- Automatic correlation
- No operation disposal tracking
- ActivityStatusCode vs manual Success flag

## ActivityKind Selection

| Operation Type | 2.x Type | 3.x ActivityKind |
|----------------|----------|------------------|
| HTTP call | DependencyTelemetry (HTTP) | ActivityKind.Client |
| Database call | DependencyTelemetry (SQL) | ActivityKind.Client |
| Internal operation | DependencyTelemetry (InProc) | ActivityKind.Internal |
| Main process | RequestTelemetry | ActivityKind.Internal |

## Benefits of 3.x Approach

1. **Dependency Injection**: Proper DI for TelemetryClient and configuration
2. **Structured Hosting**: Uses standard .NET Host for console apps
3. **Automatic Correlation**: Activity hierarchy automatic
4. **Better Semantics**: ActivityKind clearly indicates operation type
5. **OpenTelemetry Standard**: Compatible with other OTel tools

## Testing

```csharp
[Fact]
public async Task ProcessData_TracksActivity()
{
    var listener = new ActivityListener
    {
        ShouldListenTo = source => source.Name == "MyConsoleApp",
        Sample = (ref ActivityCreationOptions<ActivityContext> _) => 
            ActivitySamplingResult.AllData
    };
    
    ActivitySource.AddActivityListener(listener);
    
    var service = new ConsoleAppService(logger, telemetryClient);
    
    // Exercise
    await service.StartAsync(CancellationToken.None);
    
    // Verify Activity created
    // Verify tags present
}
```

## See Also

- [activity-source.md](../../opentelemetry-fundamentals/activity-source.md)
- [worker-service-migration.md](worker-service-migration.md)
- [activity-kinds.md](../../concepts/activity-kinds.md)
