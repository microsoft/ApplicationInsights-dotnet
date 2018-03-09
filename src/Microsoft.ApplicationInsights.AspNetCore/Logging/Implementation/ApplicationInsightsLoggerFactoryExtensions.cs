namespace Microsoft.Extensions.Logging
{
    using System;
    using ApplicationInsights;
    using ApplicationInsights.AspNetCore.Logging;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Extension methods for <see cref="ILoggerFactory"/> that allow adding Application Insights logger.
    /// </summary>
    public static class ApplicationInsightsLoggerFactoryExtensions
    {
        /// <summary>
        /// Adds an ApplicationInsights logger that is enabled for <see cref="LogLevel.Warning"/> or higher.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="serviceProvider">The instance of <see cref="IServiceProvider"/> to use for service resolution.</param>
        public static ILoggerFactory AddApplicationInsights(this ILoggerFactory factory, IServiceProvider serviceProvider)
        {
            return factory.AddApplicationInsights(serviceProvider, LogLevel.Warning);
        }

        /// <summary>
        /// Adds an ApplicationInsights logger that is enabled for <see cref="LogLevel"/>s of minLevel or higher.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="serviceProvider">The instance of <see cref="IServiceProvider"/> to use for service resolution.</param>
        /// <param name="minLevel">The minimum <see cref="LogLevel"/> to be logged</param>
        public static ILoggerFactory AddApplicationInsights(
            this ILoggerFactory factory,
            IServiceProvider serviceProvider,
            LogLevel minLevel)
        {
            factory.AddApplicationInsights(serviceProvider, (category, logLevel) => logLevel >= minLevel);
            return factory;
        }

        /// <summary>
        /// Adds an ApplicationInsights logger that is enabled as defined by the filter function.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="filter"></param>
        /// <param name="serviceProvider">The instance of <see cref="IServiceProvider"/> to use for service resolution.</param>
        public static ILoggerFactory AddApplicationInsights(
            this ILoggerFactory factory,
            IServiceProvider serviceProvider,
            Func<string, LogLevel, bool> filter)
        {
            return factory.AddApplicationInsights(serviceProvider, filter, null);
        }

        /// <summary>
        /// Adds an ApplicationInsights logger that is enabled as defined by the filter function.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="filter"></param>
        /// <param name="serviceProvider">The instance of <see cref="IServiceProvider"/> to use for service resolution.</param>
        /// <param name="loggerAddedCallback">The callback that gets executed when another ApplicationInsights logger is added.</param>
        public static ILoggerFactory AddApplicationInsights(
            this ILoggerFactory factory,
            IServiceProvider serviceProvider,
            Func<string, LogLevel, bool> filter,
            Action loggerAddedCallback)
        {
            var client = serviceProvider.GetService<TelemetryClient>();
            var debugLoggerControl = serviceProvider.GetService<ApplicationInsightsLoggerEvents>();
            var options = serviceProvider.GetService<IOptions<ApplicationInsightsLoggerOptions>>();

            if (options == null)
            {
                options = Options.Create(new ApplicationInsightsLoggerOptions());
            }

            if (debugLoggerControl != null)
            {
                debugLoggerControl.OnLoggerAdded();

                if (loggerAddedCallback != null)
                {
                    debugLoggerControl.LoggerAdded += loggerAddedCallback;
                }
            }

            factory.AddProvider(new ApplicationInsightsLoggerProvider(client, filter, options));
            return factory;
        }
    }
}