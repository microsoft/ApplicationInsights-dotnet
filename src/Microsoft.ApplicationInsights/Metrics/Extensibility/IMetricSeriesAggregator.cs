namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    using System;

    /// <summary>@ToDo: Complete documentation before stable release. {558}</summary>
    public interface IMetricSeriesAggregator
    {
        /// <summary>Gets @ToDo: Complete documentation before stable release. {969}</summary>
        MetricSeries DataSeries { get; }

        /// <summary>@ToDo: Complete documentation before stable release. {792}</summary>
        /// <returns>@ToDo: Complete documentation before stable release. {235}</returns>
        bool TryRecycle();

        /// <summary>@ToDo: Complete documentation before stable release. {246}</summary>
        /// <param name="periodStart">@ToDo: Complete documentation before stable release. {781}</param>
        /// <param name="valueFilter">@ToDo: Complete documentation before stable release. {567}</param>
        /// @PublicExposureCandidate
        void Reset(DateTimeOffset periodStart, IMetricValueFilter valueFilter);

        /// <summary>@ToDo: Complete documentation before stable release. {734}</summary>
        /// <param name="periodStart">@ToDo: Complete documentation before stable release. {299}</param>
        void Reset(DateTimeOffset periodStart);

        /// <summary>@ToDo: Complete documentation before stable release. {099}</summary>
        /// <param name="periodEnd">@ToDo: Complete documentation before stable release. {193}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {573}</returns>
        MetricAggregate CompleteAggregation(DateTimeOffset periodEnd);

        /// <summary>@ToDo: Complete documentation before stable release. {221}</summary>
        /// <param name="periodEnd">@ToDo: Complete documentation before stable release. {203}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {615}</returns>
        MetricAggregate CreateAggregateUnsafe(DateTimeOffset periodEnd);

        /// <summary>@ToDo: Complete documentation before stable release. {574}</summary>
        /// <param name="metricValue">@ToDo: Complete documentation before stable release. {887}</param>
        void TrackValue(double metricValue);

        /// <summary>@ToDo: Complete documentation before stable release. {206}</summary>
        /// <param name="metricValue">@ToDo: Complete documentation before stable release. {266}</param>
        void TrackValue(object metricValue);
    }
}
