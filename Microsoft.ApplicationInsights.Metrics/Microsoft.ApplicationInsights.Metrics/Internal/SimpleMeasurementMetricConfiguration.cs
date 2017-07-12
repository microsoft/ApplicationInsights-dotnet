using System;
using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    internal class SimpleMeasurementMetricConfiguration : IMetricConfiguration
    {

        public bool RequiresPersistentAggregation { get { return false; } }


        public SimpleMeasurementMetricConfiguration()
        {
        }

        public IMetricDataSeriesAggregator CreateNewAggregator(MetricDataSeries dataSeries, MetricConsumerKind consumerKind)
        {
            IMetricDataSeriesAggregator aggregator = new SimpleDataSeriesAggregator(this, dataSeries, consumerKind);
            return aggregator;
        }
    }
}
