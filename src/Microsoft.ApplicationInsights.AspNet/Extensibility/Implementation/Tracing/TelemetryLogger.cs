//-----------------------------------------------------------------------
// <copyright file="TelemetryLogger.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Microsoft.ApplicationInsights.AspNet.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics.Tracing;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Application Insights internal telemetry logging implementation.
    /// </summary>
    internal sealed class TelemetryLogger : ILogger
    {
        /// <summary>The underlying event source that internal telemetry is logged to.</summary>
        private AspNetEventSource eventSource;

        /// <summary>Lookup table of event source log calls indexed by LogLevel.</summary>
        private EventSourceLogCall[] eventSourceLogCalls;

        /// <summary>Instance of </summary>
        private static TelemetryLogger instance = new TelemetryLogger();

        /// <summary>
        /// Initializes a new instance of the TelemetryLogger class.
        /// </summary>
        public TelemetryLogger() : this(LogLevel.Error)
        {
        }

        /// <summary>
        /// Initializes a new instance of the TelemetryLogger class.
        /// </summary>
        /// <param name="level">The logging level.</param>
        public TelemetryLogger(LogLevel level)
        {
            this.Level = level;
            this.eventSource = AspNetEventSource.Instance;

            // The event levels are reordered for indexing by the log levels, this is a quick but potentially brittle way to do translation.
            this.eventSourceLogCalls = new EventSourceLogCall[]
            {
                null,
                this.eventSource.LogAlways,        // EventLevel.LogAlways = 0     | LogLevel.Debug = 1
                this.eventSource.LogVerbose,       // EventLevel.Verbose = 5       | LogLevel.Verbose = 2
                this.eventSource.LogInformational, // EventLevel.Informational = 4 | LogLevel.Information = 3
                this.eventSource.LogWarning,       // EventLevel.Warning = 3       | LogLevel.Warning = 4
                this.eventSource.LogError,         // EventLevel.Error = 2         | LogLevel.Error = 5
                this.eventSource.LogCritical,      // EventLevel.Critical = 1      | LogLevel.Critical = 6
            };
        }

        /// <summary>
        /// Gets an instance of the telemetry logger.
        /// </summary>
        public static TelemetryLogger Instance
        {
            get
            {
                return TelemetryLogger.instance;
            }
        }

        /// <summary>
        /// Delegate of basic event source function calls.
        /// </summary>
        /// <param name="message">The message to log.</param>
        private delegate void EventSourceLogCall(string message);

        /// <summary>Gets or sets the currently configured logging level.</summary>
        public LogLevel Level { get; set; }

        /// <summary>
        /// Begins a logical operation scope.
        /// </summary>
        /// <param name="state">The identifier for the scope.</param>
        /// <returns>An IDisposable that ends the logical operation scope on dispose.</returns>
        public IDisposable BeginScopeImpl(object state)
        {
            return new TelemetryLoggerScope();
        }

        /// <summary>
        /// Checks if the given log level is enabled.
        /// </summary>
        /// <param name="logLevel">The leg level to check.</param>
        /// <returns>True if the specified log level is enabled, otherwise false.</returns>
        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= this.Level && this.Level != LogLevel.None && logLevel != LogLevel.None;
        }

        /// <summary>
        /// Aggregates most logging patterns to a single method.
        /// </summary>
        /// <param name="logLevel">The level of logging to log this call at.</param>
        /// <param name="eventId">An event id.</param>
        /// <param name="state">Correlation state.</param>
        /// <param name="exception">An exception to log.</param>
        /// <param name="formatter">Formatter for output.</param>
        public void Log(LogLevel logLevel, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
        {
            if (this.IsEnabled(logLevel) && this.eventSource.IsEnabled())
            {
                if (state == null && exception == null)
                {
                    throw new ArgumentNullException("Either a 'state' or an 'exception' must be provided.");
                }

                // TODO: Add in metadata like eventId (which is not the event source eventId) or rework event source to use that Id and have more messages.
                string message = formatter(state, exception);
                this.eventSourceLogCalls[(int)logLevel](message);
            }
        }
    }
}
