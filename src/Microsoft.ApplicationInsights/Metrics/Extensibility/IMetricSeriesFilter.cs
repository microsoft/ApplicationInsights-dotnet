namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    using System;

    /// <summary>@ToDo: Complete documentation before stable release.</summary>
    /// @PublicExposureCandidate
    internal interface IMetricSeriesFilter
    {
        /// <summary>@ToDo: Complete documentation before stable release.</summary>
        /// <param name="dataSeries">@ToDo: Complete documentation before stable release.</param>
        /// <param name="valueFilter">@ToDo: Complete documentation before stable release.</param>
        /// <returns>@ToDo: Complete documentation before stable release.</returns>
        bool WillConsume(MetricSeries dataSeries, out IMetricValueFilter valueFilter);
    }
}