namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    using System;

    /// <summary>ToDo: Complete documentation before stable release.</summary>
    public interface IMetricSeriesAggregator
    {
        /// <summary>ToDo: Complete documentation before stable release.</summary>
        MetricSeries DataSeries { get; }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        bool TryRecycle();

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="periodStart">ToDo: Complete documentation before stable release.</param>
        /// <param name="valueFilter">ToDo: Complete documentation before stable release.</param>
        /// @PublicExposureCandidate
        void Reset(DateTimeOffset periodStart, IMetricValueFilter valueFilter);

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="periodStart">ToDo: Complete documentation before stable release.</param>
        void Reset(DateTimeOffset periodStart);

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="periodEnd">ToDo: Complete documentation before stable release.</param>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        MetricAggregate CompleteAggregation(DateTimeOffset periodEnd);

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="periodEnd">ToDo: Complete documentation before stable release.</param>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        MetricAggregate CreateAggregateUnsafe(DateTimeOffset periodEnd);

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="metricValue">ToDo: Complete documentation before stable release.</param>
        void TrackValue(double metricValue);

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="metricValue">ToDo: Complete documentation before stable release.</param>
        void TrackValue(object metricValue);
    }
}
