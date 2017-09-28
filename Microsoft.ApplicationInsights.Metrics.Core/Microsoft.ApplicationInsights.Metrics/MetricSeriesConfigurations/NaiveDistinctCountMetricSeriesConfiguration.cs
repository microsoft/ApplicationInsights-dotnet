using System;

using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Performance",
            "CA1812: Avoid uninstantiated internal classes",
            Justification = "Needed for internal verification.")]
    public class NaiveDistinctCountMetricSeriesConfiguration : IMetricSeriesConfiguration
    {
        public NaiveDistinctCountMetricSeriesConfiguration()
        {
        }

        public bool RequiresPersistentAggregation { get { return false; } }

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

        public override int GetHashCode()
        {
            return ((17 * 23) + RequiresPersistentAggregation.GetHashCode());
        }
    }
}
