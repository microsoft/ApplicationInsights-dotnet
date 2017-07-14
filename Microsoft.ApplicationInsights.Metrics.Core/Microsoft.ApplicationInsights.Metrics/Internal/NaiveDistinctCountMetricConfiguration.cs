using System;

using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    internal class NaiveDistinctCountMetricConfiguration : IMetricConfiguration
    {

        public bool RequiresPersistentAggregation { get { return false; } }


        public NaiveDistinctCountMetricConfiguration()
        {
        }

        public IMetricDataSeriesAggregator CreateNewAggregator(MetricDataSeries dataSeries, MetricConsumerKind consumerKind)
        {
            IMetricDataSeriesAggregator aggregator = new NaiveDistinctCountMetricDataSeriesAggregator(this, dataSeries, consumerKind);
            return aggregator;
        }
    }
}
