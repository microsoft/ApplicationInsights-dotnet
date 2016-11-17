using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Microsoft.ApplicationInsights.AspNetCore.Logging
{
    internal class ApplicationInsightsLoggerProvider : ILoggerProvider
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly LogLevel _minimumLevel;

        public ApplicationInsightsLoggerProvider(TelemetryClient telemetryClient, LogLevel minimumLevel)
        {
            _telemetryClient = telemetryClient;
            _minimumLevel = minimumLevel;
        }
        public ILogger CreateLogger(string categoryName)
        {
            return new ApplicationInsightsLogger(categoryName, _telemetryClient, _minimumLevel);
        }

        public void Dispose()
        {
        }
    }
}