namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    using System;

    /// <summary>@ToDo: Complete documentation before stable release. {339}</summary>
    /// @PublicExposureCandidate
    internal interface IMetricSeriesFilter
    {
        /// <summary>@ToDo: Complete documentation before stable release. {600}</summary>
        /// <param name="dataSeries">@ToDo: Complete documentation before stable release. {025}</param>
        /// <param name="valueFilter">@ToDo: Complete documentation before stable release. {050}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {100}</returns>
        bool WillConsume(MetricSeries dataSeries, out IMetricValueFilter valueFilter);
    }
}