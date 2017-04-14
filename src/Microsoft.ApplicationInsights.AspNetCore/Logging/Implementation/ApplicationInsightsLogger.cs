namespace Microsoft.ApplicationInsights.AspNetCore.Logging
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// <see cref="ILogger"/> implementation that forwards log messages as Application Insight trace events.
    /// </summary>
    internal class ApplicationInsightsLogger : ILogger
    {
        private readonly string categoryName;
        private readonly TelemetryClient telemetryClient;
        private readonly Func<string, LogLevel, bool> filter;
        private readonly string sdkVersion;

        /// <summary>
        /// Creates a new instance of <see cref="ApplicationInsightsLogger"/>
        /// </summary>
        public ApplicationInsightsLogger(string name, TelemetryClient telemetryClient, Func<string, LogLevel, bool> filter)
        {
            this.categoryName = name;
            this.telemetryClient = telemetryClient;
            this.filter = filter;
            this.sdkVersion = SdkVersionUtils.VersionPrefix + SdkVersionUtils.GetAssemblyVersion();
        }

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel)
        {
            return this.filter != null && this.telemetryClient != null && this.filter(categoryName, logLevel) && this.telemetryClient.IsEnabled();
        }

        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (this.IsEnabled(logLevel))
            {
                TraceTelemetry traceTelemetry = new TraceTelemetry(formatter(state, exception), this.GetSeverityLevel(logLevel));
                IDictionary<string, string> dict = traceTelemetry.Context.Properties;
                dict["CategoryName"] = this.categoryName;
                dict["Exception"] = exception?.ToString();
                IReadOnlyList<KeyValuePair<string, object>> stateDictionary = state as IReadOnlyList<KeyValuePair<string, object>>;
                if (stateDictionary != null)
                {
                    foreach (KeyValuePair<string, object> item in stateDictionary)
                    {
                        dict[item.Key] = Convert.ToString(item.Value);
                    }
                }

                traceTelemetry.Context.GetInternalContext().SdkVersion = this.sdkVersion;
                this.telemetryClient.TrackTrace(traceTelemetry);
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
