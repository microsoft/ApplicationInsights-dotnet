using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights.DataContracts;

namespace Microsoft.ApplicationInsights.AspNetCore.Logging
{
    internal class ApplicationInsightsLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly TelemetryClient _telemetryClient;
        private readonly LogLevel _minimumLevel;

        public ApplicationInsightsLogger(string name, TelemetryClient telemetryClient, LogLevel minimumLevel)
        {
            _categoryName = name;
            _telemetryClient = telemetryClient;
            _minimumLevel = minimumLevel;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel > _minimumLevel && _telemetryClient.IsEnabled();
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                var dict = new Dictionary<string, string>();
                dict["CategoryName"] = _categoryName;
                dict["Exception"] = exception?.ToString();
                var stateDictionary = state as IReadOnlyList<KeyValuePair<string, object>>;
                if (stateDictionary != null)
                {
                    foreach (var item in stateDictionary)
                    {
                        dict[item.Key] = Convert.ToString(item.Value);
                    }
                }

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
