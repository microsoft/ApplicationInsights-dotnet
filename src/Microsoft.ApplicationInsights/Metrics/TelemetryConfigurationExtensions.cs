namespace Microsoft.ApplicationInsights.Metrics
{
    using System;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>@ToDo: Complete documentation before stable release. {737}</summary>
    public static class TelemetryConfigurationExtensions
    {
        /// <summary>@ToDo: Complete documentation before stable release. {923}</summary>
        /// <param name="telemetryPipeline">@ToDo: Complete documentation before stable release. {456}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {580}</returns>
        public static MetricManager GetMetricManager(this TelemetryConfiguration telemetryPipeline)
        {
            return telemetryPipeline?.MetricManager;
        }
    }
}
