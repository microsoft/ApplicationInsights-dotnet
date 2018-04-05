namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    /// <summary>@ToDo: Complete documentation before stable release. {265}</summary>
    public interface IMetricValueFilter
    {
        /// <summary>@ToDo: Complete documentation before stable release. {919}</summary>
        /// <param name="dataSeries">@ToDo: Complete documentation before stable release. {526}</param>
        /// <param name="metricValue">@ToDo: Complete documentation before stable release. {095}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {464}</returns>
        bool WillConsume(MetricSeries dataSeries, double metricValue);

        /// <summary>@ToDo: Complete documentation before stable release. {704}</summary>
        /// <param name="dataSeries">@ToDo: Complete documentation before stable release. {929}</param>
        /// <param name="metricValue">@ToDo: Complete documentation before stable release. {703}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {497}</returns>
        bool WillConsume(MetricSeries dataSeries, object metricValue);
    }
}