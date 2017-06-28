namespace Microsoft.ApplicationInsights.Metrics
{
    public interface IMetricDataSeriesFilter
    {
        bool IsInterestedIn(MetricDataSeries metricDataSeries, out IMetricValueFilter valueFilter);
    }
}