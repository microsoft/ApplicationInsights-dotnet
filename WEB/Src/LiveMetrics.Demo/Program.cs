// using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights.WorkerService;
using System;

class Program
{
    private static Random _random = new();

    static async Task Main(string[] args)
    {
        // Create the DI container.
        IServiceCollection services = new ServiceCollection();

        // Being a regular console app, there is no appsettings.json or configuration providers enabled by default.
        // Hence instrumentation key/ connection string and any changes to default logging level must be specified here.
        services.AddLogging(loggingBuilder => loggingBuilder.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>("Category", LogLevel.Information));
        services.AddApplicationInsightsTelemetryWorkerService((ApplicationInsightsServiceOptions options) => options.ConnectionString = "InstrumentationKey=1277d97d-ec33-4461-8d03-b514986e685d;IngestionEndpoint=https://westus2-2.in.applicationinsights.azure.com/;LiveEndpoint=https://westus2.livediagnostics.monitor.azure.com/");

        // To pass a connection string
        // - aiserviceoptions must be created
        // - set connectionstring on it
        // - pass it to AddApplicationInsightsTelemetryWorkerService()

        // Build ServiceProvider.
        IServiceProvider serviceProvider = services.BuildServiceProvider();

        // Obtain logger instance from DI.
        ILogger<Program> logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        // Obtain TelemetryClient instance from DI, for additional manual tracking or to flush.
        var telemetryClient = serviceProvider.GetRequiredService<TelemetryClient>();

        var httpClient = new HttpClient();

        while (!Console.KeyAvailable) // This app runs indefinitely. Replace with actual application termination logic.
        {
            Console.WriteLine($"Worker running at: {DateTimeOffset.Now} in console");
            logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            if (GetRandomBool(percent: 70))
            {
                // Replace with a name which makes sense for this operation.
                using (telemetryClient.StartOperation<RequestTelemetry>("operation"))
                {
                    logger.LogWarning("A sample warning message. By default, logs with severity Warning or higher is captured by Application Insights");
                    logger.LogInformation("Calling bing.com");
                    var res = await httpClient.GetAsync("https://bing.com");
                    logger.LogInformation("Calling bing completed with status:" + res.StatusCode);
                    telemetryClient.TrackEvent("Bing call event completed");
                }
            }

            await Task.Delay(200);
        }

        // Explicitly call Flush() followed by sleep is required in console apps.
        // This is to ensure that even if application terminates, telemetry is sent to the back-end.
        telemetryClient.Flush();
        Task.Delay(5000).Wait();
    }

    private static bool GetRandomBool(int percent) => percent >= _random.Next(0, 100);
}