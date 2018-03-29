namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    using System;

    /// <summary>@ToDo: Complete documentation before stable release.</summary>
    /// @PublicExposureCandidate
    internal interface IMetricAggregateToTelemetryPipelineConverter
    {
        /// <summary>@ToDo: Complete documentation before stable release.</summary>
        /// <param name="aggregate">@ToDo: Complete documentation before stable release.</param>
        /// <returns>@ToDo: Complete documentation before stable release.</returns>
        object Convert(MetricAggregate aggregate);
    }
}
