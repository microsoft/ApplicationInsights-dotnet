using NLog;

Console.WriteLine("NLog Console App - Application Insights Example");
Console.WriteLine("================================================\n");

NLog.Common.InternalLogger.LogToConsole = true;
NLog.Common.InternalLogger.LogLevel = NLog.LogLevel.Warn;

/*
// Optional: Configure Azure Active Directory (AAD) authentication and exporter options
// Requires: Install-Package Azure.Identity

var config = new LoggingConfiguration();

// Add console target so you can see the output
var consoleTarget = new NLog.Targets.ConsoleTarget("console")
{
    Layout = "${longdate}|${level:uppercase=true}|${logger}|${message} ${exception:format=tostring}"
};
config.AddTarget(consoleTarget);
config.AddRule(LogLevel.Trace, LogLevel.Fatal, consoleTarget);

// Add AI target with AAD and exporter options
var aiTarget = new ApplicationInsightsTarget
{
    Name = "aiTarget",
    ConnectionString = "InstrumentationKey=YOUR_IKEY;IngestionEndpoint=https://ingestion-endpoint.applicationinsights.azure.com/",
    Credential = new DefaultAzureCredential(),
    EnableAdaptiveSampling = true,     // Default: true (enables adaptive sampling)
    EnableLiveMetrics = false,          // Default: false (live metrics stream disabled)
    DisableOfflineStorage = false       // Default: false (offline storage enabled)
};
config.AddTarget("aiTarget", aiTarget);
config.AddRule(LogLevel.Info, LogLevel.Fatal, aiTarget);

LogManager.Configuration = config;
*/

// Get NLog logger - the ApplicationInsightsTarget will handle telemetry
var logger = LogManager.GetCurrentClassLogger();

try
{
    // Example 1: Different log levels
    logger.Trace("This is a trace message");
    logger.Debug("This is a debug message");
    logger.Info("This is an info message");
    logger.Warn("This is a warning message");
    logger.Error("This is an error message");

    // Example 2: Logging with structured data
    logger.Info("User {UserId} logged in from {IpAddress}", 123, "192.168.1.1");

    // Example 3: Logging exceptions
    try
    {
        throw new InvalidOperationException("This is a test exception");
    }
    catch (Exception ex)
    {
        logger.Error(ex, "An error occurred while processing");
    }

    // Example 4: Logging with properties
    var logEvent = new LogEventInfo(LogLevel.Info, logger.Name, "Operation completed");
    logEvent.Properties["Duration"] = 1500;
    logEvent.Properties["Status"] = "Success";
    logger.Log(logEvent);

    Console.WriteLine("\n✓ All log messages sent successfully!");
}
finally
{
    // Flush and shutdown NLog (which will flush the TelemetryClient internally)
    LogManager.Shutdown();
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();