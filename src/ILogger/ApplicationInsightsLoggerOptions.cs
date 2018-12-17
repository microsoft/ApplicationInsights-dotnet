// -----------------------------------------------------------------------
// <copyright file="ApplicationInsightsLoggerOptions.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. 
// All rights reserved.  2013
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Extensions.Logging.ApplicationInsights
{
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// <see cref="ApplicationInsightsLoggerOptions"/> defines the custom behavior of the tracing information sent to Application Insights.
    /// </summary>
    public class ApplicationInsightsLoggerOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether to track exceptions as <see cref="ExceptionTelemetry"/>.
        /// </summary>
        public bool TrackExceptionsAsExceptionTelemetry { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether EventId and EventName properties should be excluded from telemetry.
        /// </summary>
        public bool ExcludeEventId { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the Scope information is excluded from telemetry or not.
        /// </summary>
        public bool ExcludeScopeData { get; set; } = false;
    }
}