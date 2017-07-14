namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    public interface IMetricSeriesFilter
    {
        bool WillConsume(MetricSeries dataSeries, out IMetricValueFilter valueFilter);
    }
}