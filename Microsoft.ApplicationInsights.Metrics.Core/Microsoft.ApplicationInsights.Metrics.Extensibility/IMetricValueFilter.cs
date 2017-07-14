namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    public interface IMetricValueFilter
    {
        bool WillConsume(MetricSeries dataSeries, uint metricValue);
        bool WillConsume(MetricSeries dataSeries, double metricValue);
        bool WillConsume(MetricSeries dataSeries, object metricValue);
    }
}