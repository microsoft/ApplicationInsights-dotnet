using System;

namespace Microsoft.ApplicationInsights.Metrics
{
    public interface IMetricDataSeriesConfiguration
    {
        bool RequiresStatefulAggregation { get; }

        IMetricDataSeriesAggregator CreateNewAggregator(MetricDataSeries metricDataSeries, IMetricValueFilter valuesFilter);
    }
}
