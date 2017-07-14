using System;

using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    internal class NaiveDistinctCountMetricSeriesConfiguration : IMetricSeriesConfiguration
    {

        public bool RequiresPersistentAggregation { get { return false; } }


        public NaiveDistinctCountMetricSeriesConfiguration()
        {
        }

        public IMetricSeriesAggregator CreateNewAggregator(MetricSeries dataSeries, MetricConsumerKind consumerKind)
        {
            IMetricSeriesAggregator aggregator = new NaiveDistinctCountMetricSeriesAggregator(this, dataSeries, consumerKind);
            return aggregator;
        }
    }
}
