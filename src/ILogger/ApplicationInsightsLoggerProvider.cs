// -----------------------------------------------------------------------
// <copyright file="ApplicationInsightsLoggerProvider.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. 
// All rights reserved.  2013
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Extensions.Logging.ApplicationInsights
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Application insights logger provider.
    /// </summary>
    /// <seealso cref="Microsoft.Extensions.Logging.ILoggerProvider" />
    /// <seealso cref="Microsoft.Extensions.Logging.ISupportExternalScope" />
    [ProviderAlias("ApplicationInsights")]
    public class ApplicationInsightsLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        /// <summary>
        /// The application insights logger options.
        /// </summary>
        private readonly ApplicationInsightsLoggerOptions applicationInsightsLoggerOptions;

        /// <summary>
        /// The telemetry client to be used to log messages to Application Insights.
        /// </summary>
        private readonly TelemetryClient telemetryClient;

        /// <summary>
        /// The collection of application insights loggers stored against categoryName.
        /// categoryName -> <see cref="ApplicationInsightsLogger"/> map.
        /// </summary>
        private readonly ConcurrentDictionary<string, ApplicationInsightsLogger> applicationInsightsLoggers
            = new ConcurrentDictionary<string, ApplicationInsightsLogger>();

        /// <summary>
        /// The external scope provider to allow setting scope data in messages.
        /// </summary>
        private IExternalScopeProvider externalScopeProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationInsightsLoggerProvider"/> class.
        /// </summary>
        /// <param name="telemetryConfiguration">The telemetry configuration.</param>
        /// <param name="applicationInsightsLoggerOptions">The application insights logger options.</param>
        /// <exception cref="System.ArgumentNullException">
        /// telemetryConfiguration
        /// or
        /// loggingFilter
        /// or
        /// applicationInsightsLoggerOptions.
        /// </exception>
        public ApplicationInsightsLoggerProvider(
            TelemetryConfiguration telemetryConfiguration,
            IOptions<ApplicationInsightsLoggerOptions> applicationInsightsLoggerOptions)
        {
            this.telemetryClient = new TelemetryClient(telemetryConfiguration) ?? throw new ArgumentNullException(nameof(telemetryConfiguration));
            this.applicationInsightsLoggerOptions = applicationInsightsLoggerOptions.Value ?? throw new ArgumentNullException(nameof(applicationInsightsLoggerOptions));
        }

        /// <summary>
        /// Creates a new <see cref="ILogger" /> instance.
        /// </summary>
        /// <param name="categoryName">The category name for messages produced by the logger.</param>
        /// <returns>An <see cref="ILogger"/> instance to be used for logging.</returns>
        public ILogger CreateLogger(string categoryName)
        {
            // Word of caution : GetOrAdd function is not fully threadsafe. The delegate to create
            // new objects is not run under lock so we might create multiple ApplicationInsightsLoggers.
            // However this will only typically during first time a specific code path is getting hit.
            // Since ApplicationInsightsLoggers are harmless we are ready to live with this. However
            // if in future this changes, Lazy<> approach can be used.
            return this.applicationInsightsLoggers.GetOrAdd(
                categoryName,
                loggerName => new ApplicationInsightsLogger(
                    loggerName,
                    this.telemetryClient,
                    this.applicationInsightsLoggerOptions)
                {
                    ExternalScopeProvider = this.externalScopeProvider,
                });
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Sets the scope provider. This method also updates all the existing logger to also use the new ScopeProvider.
        /// </summary>
        /// <param name="externalScopeProvider">The external scope provider.</param>
        public void SetScopeProvider(IExternalScopeProvider externalScopeProvider)
        {
            // First set the ScopeProvider to ensure the newer instances get the newer instance.
            this.externalScopeProvider = externalScopeProvider;

            // Then update all the existing loggers regardless of their current scope provider.
            foreach (var logger in this.applicationInsightsLoggers)
            {
                logger.Value.ExternalScopeProvider = externalScopeProvider;
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            // Dispose all appInsights loggers.
            foreach (var logger in this.applicationInsightsLoggers)
            {
                logger.Value.Dispose();
            }
        }
    }
}
