namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    public interface IMetricValueFilter
    {
        bool WillConsume(MetricDataSeries metricDataSeries, uint metricValue);
        bool WillConsume(MetricDataSeries metricDataSeries, double metricValue);
        bool WillConsume(MetricDataSeries metricDataSeries, object metricValue);
    }
}