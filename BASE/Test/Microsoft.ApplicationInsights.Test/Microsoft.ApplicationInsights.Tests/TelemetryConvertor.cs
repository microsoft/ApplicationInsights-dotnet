namespace Microsoft.ApplicationInsights.Tests
{
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Extensions.Logging;
    using OpenTelemetry.Logs;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    public static class TelemetryConverter
    {
        /// <summary>
        /// Converts an Activity into a RequestTelemetry equivalent.
        /// </summary>
        public static RequestTelemetry ToRequestTelemetry(this Activity activity)
        {
            if (activity == null)
                throw new ArgumentNullException(nameof(activity));

            var telemetry = new RequestTelemetry
            {
                Name = activity.DisplayName ?? "Request",
                Id = activity.SpanId.ToHexString(),
                Timestamp = activity.StartTimeUtc == default
                    ? DateTimeOffset.UtcNow
                    : new DateTimeOffset(activity.StartTimeUtc),
                Duration = activity.Duration != TimeSpan.Zero
                    ? activity.Duration
                    : DateTime.UtcNow - activity.StartTimeUtc,
                ResponseCode = activity.GetTagItem("http.status_code")?.ToString(),
                Success = activity.Status == ActivityStatusCode.Ok,
            };

            telemetry.Context.Operation.Id = activity.TraceId.ToHexString();
            if (activity.ParentSpanId != default)
                telemetry.Context.Operation.ParentId = activity.ParentSpanId.ToHexString();

            foreach (var tag in activity.Tags)
                telemetry.Properties[tag.Key] = tag.Value?.ToString();

            return telemetry;
        }

        /// <summary>
        /// Converts an Activity into a DependencyTelemetry equivalent.
        /// </summary>
        public static DependencyTelemetry ToDependencyTelemetry(this Activity activity)
        {
            if (activity == null)
                throw new ArgumentNullException(nameof(activity));

            var telemetry = new DependencyTelemetry
            {
                Name = activity.DisplayName ?? "Dependency",
                Id = activity.SpanId.ToHexString(),
                Timestamp = activity.StartTimeUtc,
                Duration = activity.Duration,
            };

            telemetry.Context.Operation.Id = activity.TraceId.ToHexString();
            if (activity.ParentSpanId != default)
                telemetry.Context.Operation.ParentId = activity.ParentSpanId.ToHexString();

            foreach (var tag in activity.Tags)
                telemetry.Properties[tag.Key] = tag.Value?.ToString();

            return telemetry;
        }

        public static TraceTelemetry ToTraceTelemetry(this LogRecord log)
        {
            if (log == null)
                throw new ArgumentNullException(nameof(log));

            var telemetry = new TraceTelemetry
            {
                Message = log.FormattedMessage ?? log.Body?.ToString(),
                SeverityLevel = ConvertSeverity(log.LogLevel),
                Timestamp = log.Timestamp == default ? DateTimeOffset.UtcNow : log.Timestamp
            };

            if (log.TraceId != default)
                telemetry.Context.Operation.Id = log.TraceId.ToHexString();

            if (log.SpanId != default)
                telemetry.Context.Operation.ParentId = log.SpanId.ToHexString();

            // Copy attributes as custom properties
            if (log.Attributes != null)
            {
                foreach (var kvp in log.Attributes)
                {
                    telemetry.Properties[kvp.Key] = kvp.Value?.ToString();
                }
            }

            return telemetry;
        }

        private static SeverityLevel ConvertSeverity(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace or LogLevel.Debug => SeverityLevel.Verbose,
                LogLevel.Information => SeverityLevel.Information,
                LogLevel.Warning => SeverityLevel.Warning,
                LogLevel.Error => SeverityLevel.Error,
                LogLevel.Critical => SeverityLevel.Critical,
                _ => SeverityLevel.Verbose
            };
        }
    }
}
