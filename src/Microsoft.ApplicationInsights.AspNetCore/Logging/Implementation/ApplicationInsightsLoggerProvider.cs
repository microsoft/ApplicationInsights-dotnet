namespace Microsoft.ApplicationInsights.AspNetCore.Logging
{
    using System;
    using Microsoft.Extensions.Logging;

    internal class ApplicationInsightsLoggerProvider : ILoggerProvider
    {
        private readonly TelemetryClient telemetryClient;
        private readonly Func<string, LogLevel, bool> filter;

        public ApplicationInsightsLoggerProvider(TelemetryClient telemetryClient, Func<string, LogLevel, bool> filter)
        {
            this.telemetryClient = telemetryClient;
            this.filter = filter;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new ApplicationInsightsLogger(categoryName, this.telemetryClient, filter);
        }

        public void Dispose()
        {
        }
    }
}