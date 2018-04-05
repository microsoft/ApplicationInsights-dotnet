namespace Microsoft.ApplicationInsights.Metrics
{
    using System;
    using Microsoft.ApplicationInsights.Metrics.Extensibility;

    /// <summary>@ToDo: Complete documentation before stable release. {510}</summary>
    public interface IMetricSeriesConfiguration : IEquatable<IMetricSeriesConfiguration>
    {
        /// <summary>Gets a value indicating whether @ToDo: Complete documentation before stable release. {016}</summary>
        bool RequiresPersistentAggregation { get; }

        /// <summary>@ToDo: Complete documentation before stable release. {149}</summary>
        /// <param name="dataSeries">@ToDo: Complete documentation before stable release. {201}</param>
        /// <param name="aggregationCycleKind">@ToDo: Complete documentation before stable release. {841}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {127}</returns>
        IMetricSeriesAggregator CreateNewAggregator(MetricSeries dataSeries, MetricAggregationCycleKind aggregationCycleKind);
    }
}
