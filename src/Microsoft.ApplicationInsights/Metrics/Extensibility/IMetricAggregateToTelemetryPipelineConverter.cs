namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    using System;

    /// <summary>@ToDo: Complete documentation before stable release. {397}</summary>
    /// @PublicExposureCandidate
    internal interface IMetricAggregateToTelemetryPipelineConverter
    {
        /// <summary>@ToDo: Complete documentation before stable release. {360}</summary>
        /// <param name="aggregate">@ToDo: Complete documentation before stable release. {116}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {685}</returns>
        object Convert(MetricAggregate aggregate);
    }
}
