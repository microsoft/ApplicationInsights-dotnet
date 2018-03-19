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
        private readonly TelemetryClient telemetryClient;
        private readonly Func<string, LogLevel, bool> filter;
        private readonly ApplicationInsightsLoggerOptions options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationInsightsLoggerProvider"/> class.
        /// </summary>
        public ApplicationInsightsLoggerProvider(TelemetryClient telemetryClient, Func<string, LogLevel, bool> filter, IOptions<ApplicationInsightsLoggerOptions> options)
        {
            this.telemetryClient = telemetryClient;
            this.filter = filter;
            this.options = options.Value;

        }

        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName)
        {
            return new ApplicationInsightsLogger(categoryName, this.telemetryClient, filter, options);
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}