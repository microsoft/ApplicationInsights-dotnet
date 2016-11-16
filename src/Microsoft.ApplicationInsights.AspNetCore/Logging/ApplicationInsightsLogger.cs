using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights.DataContracts;

namespace Microsoft.ApplicationInsights.AspNetCore.Logging
{
    internal class ApplicationInsightsLogger : ILogger
    {
        private string _categoryName;
        private readonly TelemetryClient _telemetryClient;

        public ApplicationInsightsLogger(string name, TelemetryClient telemetryClient)
        {
            _categoryName = name;
            _telemetryClient = telemetryClient;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _telemetryClient.IsEnabled();
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var dict = new Dictionary<string, string>();
            dict["CategoryName"] = _categoryName;

            if (exception != null)
            {
                _telemetryClient.TrackException(exception, dict);
            }
            else
            {
                _telemetryClient.TrackTrace(formatter(state, exception), GetSeverityLevel(logLevel), dict);
            }
        }

        private SeverityLevel GetSeverityLevel(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Critical:
                    return SeverityLevel.Critical;
                case LogLevel.Error:
                    return SeverityLevel.Error;
                case LogLevel.Warning:
                    return SeverityLevel.Warning;
                case LogLevel.Information:
                    return SeverityLevel.Information;
                case LogLevel.Debug:
                case LogLevel.Trace:
                default:
                    return SeverityLevel.Verbose;
            }
        }

    }
}
