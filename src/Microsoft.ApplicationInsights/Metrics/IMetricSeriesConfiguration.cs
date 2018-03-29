namespace Microsoft.ApplicationInsights.Metrics
{
    using System;
    using Microsoft.ApplicationInsights.Metrics.Extensibility;

    /// <summary>ToDo: Complete documentation before stable release.</summary>
    public interface IMetricSeriesConfiguration : IEquatable<IMetricSeriesConfiguration>
    {
        /// <summary>Gets a value indicating whether toDo: Complete documentation before stable release.</summary>
        bool RequiresPersistentAggregation { get; }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="dataSeries">ToDo: Complete documentation before stable release.</param>
        /// <param name="aggregationCycleKind">ToDo: Complete documentation before stable release.</param>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        IMetricSeriesAggregator CreateNewAggregator(MetricSeries dataSeries, MetricAggregationCycleKind aggregationCycleKind);
    }
}
