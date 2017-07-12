using System;
using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    internal class SimpleLifetimeCounterMetricConfiguration : IMetricConfiguration
    {

        public bool RequiresPersistentAggregation { get { return true; } }


        public SimpleLifetimeCounterMetricConfiguration()
        {
        }

        public IMetricDataSeriesAggregator CreateNewAggregator(MetricDataSeries dataSeries, MetricConsumerKind consumerKind)
        {
            IMetricDataSeriesAggregator aggregator = new SimpleDataSeriesAggregator(this, dataSeries, consumerKind);
            return aggregator;
        }
    }
}
