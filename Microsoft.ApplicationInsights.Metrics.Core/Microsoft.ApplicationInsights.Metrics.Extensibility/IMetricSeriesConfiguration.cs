using System;

namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    public interface IMetricSeriesConfiguration
    {
        bool RequiresPersistentAggregation { get; }

        IMetricSeriesAggregator CreateNewAggregator(MetricSeries dataSeries, MetricConsumerKind consumerKind);
    }
}
