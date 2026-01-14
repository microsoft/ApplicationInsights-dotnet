// <copyright file="OpenTelemetryBuilderExtensions.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// </copyright>

namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Collections.Generic;
    using Azure.Monitor.OpenTelemetry.Exporter;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Internals;
    using OpenTelemetry;
    using OpenTelemetry.Resources;

    /// <summary>
    /// Extension methods for configuring Application Insights with OpenTelemetry.
    /// </summary>
    internal static class OpenTelemetryBuilderExtensions
    {
        /// <summary>
        /// Configures OpenTelemetry with default Application Insights settings.
        /// </summary>
        /// <param name="builder">The OpenTelemetry builder.</param>
        /// <returns>The configured builder for chaining.</returns>
        public static IOpenTelemetryBuilder WithApplicationInsights(this IOpenTelemetryBuilder builder)
        {
            builder
                .ConfigureResource(r => r.AddAttributes(new[]
                {
                    new KeyValuePair<string, object>("telemetry.distro.name", "Microsoft.ApplicationInsights"),
                    new KeyValuePair<string, object>("telemetry.distro.version", VersionUtils.GetVersion(typeof(OpenTelemetryBuilderExtensions))),
                }))
                .WithLogging()
                .WithMetrics(metrics => metrics.AddMeter(TelemetryConfiguration.ApplicationInsightsMeterName))
                .WithTracing(tracing => tracing.AddSource(TelemetryConfiguration.ApplicationInsightsActivitySourceName));

            // Note: Connection string should be set via UseAzureMonitor() 
            // when TelemetryConfiguration.ConnectionString is provided

            return builder;
        }

        /// <summary>
        /// Configures Azure Monitor exporter with the specified connection string.
        /// </summary>
        /// <param name="builder">The OpenTelemetry builder.</param>
        /// <param name="configure">Action to configure Azure Monitor options.</param>
        /// <returns>The configured builder for chaining.</returns>
        public static IOpenTelemetryBuilder SetAzureMonitorExporter(
            this IOpenTelemetryBuilder builder,
            Action<AzureMonitorExporterOptions> configure)
        {
            builder.UseAzureMonitorExporter(configure);
            return builder;
        }
    }
}
