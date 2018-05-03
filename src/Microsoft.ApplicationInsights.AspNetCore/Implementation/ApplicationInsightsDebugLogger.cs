namespace Microsoft.ApplicationInsights.AspNetCore
{
    using System;
    using System.Diagnostics;
    using Extensions;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Class used to enable Application Insight loggers while debugging.
    /// </summary>
    internal class ApplicationInsightsDebugLogger
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationInsightsDebugLogger"/> class.
        /// </summary>
        public ApplicationInsightsDebugLogger(
            IOptions<ApplicationInsightsServiceOptions> options,
            ILoggerFactory loggerFactory,
            IServiceProvider serviceProvider)
        {
            // Add default logger factory for debug mode only if enabled and instrumentation key not set
            if (options.Value.EnableDebugLogger && string.IsNullOrEmpty(options.Value.InstrumentationKey))
            {
                // Do not use extension method here or it will disable debug logger we currently adding
                var enableDebugLogger = true;
                loggerFactory.AddApplicationInsights(serviceProvider, (s, level) => enableDebugLogger && Debugger.IsAttached, () => enableDebugLogger = false);
            }
        }
    }
}