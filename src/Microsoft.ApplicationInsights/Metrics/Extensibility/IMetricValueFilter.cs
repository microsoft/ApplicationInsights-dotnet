namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    /// <summary>@ToDo: Complete documentation before stable release.</summary>
    public interface IMetricValueFilter
    {
        /// <summary>@ToDo: Complete documentation before stable release.</summary>
        /// <param name="dataSeries">@ToDo: Complete documentation before stable release.</param>
        /// <param name="metricValue">@ToDo: Complete documentation before stable release.</param>
        /// <returns>@ToDo: Complete documentation before stable release.</returns>
        bool WillConsume(MetricSeries dataSeries, double metricValue);

        /// <summary>@ToDo: Complete documentation before stable release.</summary>
        /// <param name="dataSeries">@ToDo: Complete documentation before stable release.</param>
        /// <param name="metricValue">@ToDo: Complete documentation before stable release.</param>
        /// <returns>@ToDo: Complete documentation before stable release.</returns>
        bool WillConsume(MetricSeries dataSeries, object metricValue);
    }
}