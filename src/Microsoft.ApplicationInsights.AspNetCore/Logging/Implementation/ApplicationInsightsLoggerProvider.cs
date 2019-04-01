namespace Microsoft.ApplicationInsights.AspNetCore.Logging
{
    using System;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// <see cref="ILoggerProvider"/> implementation that creates returns instances of <see cref="ApplicationInsightsLogger"/>
    /// </summary>
#if !NETSTANDARD2_0
    // For NETSTANDARD2.0 We take dependency on Microsoft.Extensions.Logging.ApplicationInsights which has ApplicationInsightsProvider having the same ProviderAlias and don't want to clash with this ProviderAlias.
    [ProviderAlias("ApplicationInsights")]
#endif
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