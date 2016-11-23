// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Logging
{
    using ApplicationInsights;
    using ApplicationInsights.AspNetCore.Logging;

    /// <summary>
    /// Extension methods for <see cref="ILoggerFactory"/> that allow adding Application Insights logger.
    /// </summary>
    public static class ApplicationInsightsLoggerFactoryExtensions
    {
        /// <summary>
        /// Adds an ApplicationInsights logger that is enabled for <see cref="LogLevel.Warning"/> or higher.
        /// </summary>
        public static ILoggerFactory AddApplicationInsights(this ILoggerFactory factory,
            TelemetryClient client)
        {
            return factory.AddApplicationInsights(client, LogLevel.Warning);
        }

        /// <summary>
        /// Adds an ApplicationInsights logger that is enabled for <see cref="LogLevel"/>s of minLevel or higher.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="client">The instance of <see cref="TelemetryClient"/> to use for logging.</param>
        /// <param name="minLevel">The minimum <see cref="LogLevel"/> to be logged</param>
        public static ILoggerFactory AddApplicationInsights(
            this ILoggerFactory factory,
            TelemetryClient client,
            LogLevel minLevel)
        {
            factory.AddApplicationInsights(client, (category, logLevel) => logLevel >= minLevel);
            return factory;
        }

        /// <summary>
        /// Adds an ApplicationInsights logger that is enabled as defined by the filter function.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="filter"></param>
        /// <param name="client">The instance of <see cref="TelemetryClient"/> to use for logging.</param>
        public static ILoggerFactory AddApplicationInsights(
            this ILoggerFactory factory,
            TelemetryClient client,
            Func<string, LogLevel, bool> filter)
        {
            factory.AddProvider(new ApplicationInsightsLoggerProvider(client, filter));
            return factory;
        }
    }
}