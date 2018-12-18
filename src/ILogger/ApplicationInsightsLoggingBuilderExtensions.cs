// -----------------------------------------------------------------------
// <copyright file="ApplicationInsightsLoggingBuilderExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. 
// All rights reserved.  2013
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Extensions.Logging
{
    using System;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Logging.ApplicationInsights;

    /// <summary>
    /// Extensions methods to add and configure application insights logger.
    /// </summary>
    public static class ApplicationInsightsLoggingBuilderExtensions
    {
        /// <summary>
        /// Adds an ApplicationInsights logger named 'ApplicationInsights' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        public static ILoggingBuilder AddApplicationInsights(this ILoggingBuilder builder)
        {
            return builder.AddApplicationInsights((applicationInsightsOptions) => { });
        }

        /// <summary>
        /// Adds an ApplicationInsights logger named 'ApplicationInsights' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        /// <param name="configureApplicationInsightsOptions">Action to configure ApplicationInsights logger.</param>
        public static ILoggingBuilder AddApplicationInsights(
            this ILoggingBuilder builder,
            Action<ApplicationInsightsLoggerOptions> configureApplicationInsightsOptions)
        {
            if (configureApplicationInsightsOptions == null)
            {
                throw new ArgumentNullException(nameof(configureApplicationInsightsOptions));
            }

            // Initialize IOptions<TelemetryConfiguration> user can keep on configuring it furthur if they want to.
            builder.Services.Configure<TelemetryConfiguration>((telemetryConfiguration) => { });

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, ApplicationInsightsLoggerProvider>());
            builder.Services.Configure(configureApplicationInsightsOptions);

            return builder;
        }
    }
}
