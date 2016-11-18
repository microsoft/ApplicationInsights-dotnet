namespace Microsoft.ApplicationInsights.AspNetCore.Logging
{
    using Microsoft.Extensions.Logging;

    internal class ApplicationInsightsLoggerProvider : ILoggerProvider
    {
        private readonly TelemetryClient telemetryClient;
        private readonly LogLevel minimumLevel;

        public ApplicationInsightsLoggerProvider(TelemetryClient telemetryClient, LogLevel minimumLevel)
        {
            this.telemetryClient = telemetryClient;
            this.minimumLevel = minimumLevel;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new ApplicationInsightsLogger(categoryName, this.telemetryClient, this.minimumLevel);
        }

        public void Dispose()
        {
        }
    }
}