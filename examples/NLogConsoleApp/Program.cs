using NLog;
// Uncomment these for Azure Active Directory (AAD) authentication
// using Microsoft.ApplicationInsights.Extensibility;
// using Azure.Identity;

Console.WriteLine("NLog Console App - Application Insights Example");
Console.WriteLine("================================================\n");

NLog.Common.InternalLogger.LogToConsole = true;
NLog.Common.InternalLogger.LogLevel = NLog.LogLevel.Warn;

// Optional: Configure Azure Active Directory (AAD) authentication
// This must be done BEFORE LogManager.GetCurrentClassLogger() is called
// Requires: Install-Package Azure.Identity
/*
var telemetryConfig = TelemetryConfiguration.CreateDefault();
telemetryConfig.ConnectionString = "InstrumentationKey=YOUR_IKEY;IngestionEndpoint=https://ingestion-endpoint.applicationinsights.azure.com/";
telemetryConfig.SetAzureTokenCredential(new DefaultAzureCredential());
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