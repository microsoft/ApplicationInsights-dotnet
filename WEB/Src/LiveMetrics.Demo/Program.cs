// using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights.WorkerService;

class Program
{
    static async Task Main(string[] args)
    {
        // Create the DI container.
        IServiceCollection services = new ServiceCollection();

        // Being a regular console app, there is no appsettings.json or configuration providers enabled by default.
        // Hence instrumentation key/ connection string and any changes to default logging level must be specified here.
        services.AddLogging(loggingBuilder => loggingBuilder.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>("Category", LogLevel.Information));
        services.AddApplicationInsightsTelemetryWorkerService((ApplicationInsightsServiceOptions options) => options.ConnectionString = "InstrumentationKey=<instrumentation key here>");

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

        while (true) // This app runs indefinitely. Replace with actual application termination logic.
        {
            logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            // Replace with a name which makes sense for this operation.
            using (telemetryClient.StartOperation<RequestTelemetry>("operation"))
            {
                logger.LogWarning("A sample warning message. By default, logs with severity Warning or higher is captured by Application Insights");
                logger.LogInformation("Calling bing.com");
                var res = await httpClient.GetAsync("https://bing.com");
                logger.LogInformation("Calling bing completed with status:" + res.StatusCode);
                telemetryClient.TrackEvent("Bing call event completed");
            }

            await Task.Delay(1000);
        }

        // Explicitly call Flush() followed by sleep is required in console apps.
        // This is to ensure that even if application terminates, telemetry is sent to the back-end.
        telemetryClient.Flush();
        Task.Delay(5000).Wait();
    }
}