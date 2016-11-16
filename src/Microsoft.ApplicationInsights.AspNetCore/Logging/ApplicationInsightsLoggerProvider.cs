using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Microsoft.ApplicationInsights.AspNetCore.Logging
{
    internal class ApplicationInsightsLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, ApplicationInsightsLogger> _loggers = new ConcurrentDictionary<string, ApplicationInsightsLogger>();
        private TelemetryClient _telemetryClient;

        public ApplicationInsightsLoggerProvider(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
        }
        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, CreateLoggerImplementation);
        }

        private ApplicationInsightsLogger CreateLoggerImplementation(string name)
        {
            return new ApplicationInsightsLogger(name, _telemetryClient);
        }

        public void Dispose()
        {
        }
    }
}