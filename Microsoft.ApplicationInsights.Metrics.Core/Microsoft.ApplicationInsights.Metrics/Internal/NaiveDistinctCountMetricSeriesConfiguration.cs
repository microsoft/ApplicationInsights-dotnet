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

        public override bool Equals(object other)
        {
            if (other != null)
            {
                var otherConfig = other as NaiveDistinctCountMetricSeriesConfiguration;
                if (otherConfig != null)
                {
                    return Equals(otherConfig);
                }
            }

            return false;
        }

        public bool Equals(IMetricSeriesConfiguration other)
        {
            return Equals((object) other);
        }

        public bool Equals(NaiveDistinctCountMetricSeriesConfiguration other)
        {
            if (other == null)
            {
                return false;
            }

            if (Object.ReferenceEquals(this, other))
            {
                return true;
            }

            return (this.RequiresPersistentAggregation == other.RequiresPersistentAggregation);
        }
    }
}
