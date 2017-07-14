namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    public interface IMetricDataSeriesFilter
    {
        bool WillConsume(MetricDataSeries metricDataSeries, out IMetricValueFilter valueFilter);
    }
}