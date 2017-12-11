namespace Microsoft.ApplicationInsights.AspNetCore.Logging
{
    using System;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// <see cref="ILoggerProvider"/> implementation that creates returns instances of <see cref="ApplicationInsightsLogger"/>
    /// </summary>
    [ProviderAlias("ApplicationInsights")]
    internal class ApplicationInsightsLoggerProvider : ILoggerProvider
    {
        private static readonly Func<string, LogLevel, bool> trueFilter = (cat, level) => true;

        private readonly TelemetryClient telemetryClient;
        private readonly Func<string, LogLevel, bool> filter;
        private readonly bool includeEventId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationInsightsLoggerProvider"/> class.
        /// </summary>
        public ApplicationInsightsLoggerProvider(TelemetryClient telemetryClient, Func<string, LogLevel, bool> filter)
        {
            this.telemetryClient = telemetryClient;
            this.filter = filter;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationInsightsLoggerProvider"/> class.
        /// </summary>
        public ApplicationInsightsLoggerProvider(TelemetryClient telemetryClient, IOptions<ApplicationInsightsLoggerOptions> options)
        {
            this.telemetryClient = telemetryClient;
            this.filter = trueFilter;
            this.includeEventId = (options.Value?.IncludeEventId).GetValueOrDefault();
        }

        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName)
        {
            return new ApplicationInsightsLogger(categoryName, this.telemetryClient, filter, includeEventId);
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}