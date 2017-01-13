// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


namespace Microsoft.Extensions.Logging
{
    using System;
    using ApplicationInsights;
    using ApplicationInsights.AspNetCore.Logging;
    using Microsoft.Extensions.DependencyInjection;

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
            var client = serviceProvider.GetService<TelemetryClient>();
            var debugLoggerControl = serviceProvider.GetService<DebugLoggerControl>();
            if (debugLoggerControl != null)
            {
                debugLoggerControl.EnableDebugLogger = false;
            }

            factory.AddProvider(new ApplicationInsightsLoggerProvider(client, filter));
            return factory;
        }
    }
}