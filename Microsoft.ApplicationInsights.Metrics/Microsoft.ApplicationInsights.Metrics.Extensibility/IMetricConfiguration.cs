using System;

namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    public interface IMetricConfiguration
    {
        bool RequiresPersistentAggregation { get; }

        IMetricDataSeriesAggregator CreateNewAggregator(MetricDataSeries metricDataSeries, MetricConsumerKind consumerKind);
    }
}
