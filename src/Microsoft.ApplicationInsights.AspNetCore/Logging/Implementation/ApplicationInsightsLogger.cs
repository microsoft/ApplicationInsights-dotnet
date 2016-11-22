namespace Microsoft.ApplicationInsights.AspNetCore.Logging
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Extensions.Logging;

    internal class ApplicationInsightsLogger : ILogger
    {
        private readonly string categoryName;
        private readonly TelemetryClient telemetryClient;
        private readonly Func<string, LogLevel, bool> filter;

        public ApplicationInsightsLogger(string name, TelemetryClient telemetryClient, Func<string, LogLevel, bool> filter)
        {
            this.categoryName = name;
            this.telemetryClient = telemetryClient;
            this.filter = filter;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return this.filter(categoryName, logLevel) && this.telemetryClient.IsEnabled();
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (this.IsEnabled(logLevel))
            {
                var dict = new Dictionary<string, string>();
                dict["CategoryName"] = this.categoryName;
                dict["Exception"] = exception?.ToString();
                var stateDictionary = state as IReadOnlyList<KeyValuePair<string, object>>;
                if (stateDictionary != null)
                {
                    foreach (var item in stateDictionary)
                    {
                        dict[item.Key] = Convert.ToString(item.Value);
                    }
                }

                this.telemetryClient.TrackTrace(formatter(state, exception), this.GetSeverityLevel(logLevel), dict);
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
