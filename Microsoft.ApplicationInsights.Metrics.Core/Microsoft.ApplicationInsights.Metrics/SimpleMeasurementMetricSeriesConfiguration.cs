using System;
using System.Runtime.CompilerServices;

using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    public class SimpleMeasurementMetricSeriesConfiguration : IMetricSeriesConfiguration
    {
        private readonly bool _lifetimeCounter;
        private readonly bool _supportDoubleValues;

        
        public bool RequiresPersistentAggregation
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _lifetimeCounter; }
        }

        public bool SupportDoubleValues
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _supportDoubleValues; }
        }

        public SimpleMeasurementMetricSeriesConfiguration(bool lifetimeCounter, bool supportDoubleValues)
        {
            _lifetimeCounter = lifetimeCounter;
            _supportDoubleValues = supportDoubleValues;
        }

        public IMetricSeriesAggregator CreateNewAggregator(MetricSeries dataSeries, MetricConsumerKind consumerKind)
        {
            if (_supportDoubleValues)
            {
                IMetricSeriesAggregator aggregator = new SimpleDoubleDataSeriesAggregator(this, dataSeries, consumerKind);
                return aggregator;
            }
            else
            {
                IMetricSeriesAggregator aggregator = new SimpleUIntDataSeriesAggregator(this, dataSeries, consumerKind);
                return aggregator;
            }
        }

        public override bool Equals(object other)
        {
            if (other != null)
            {
                var otherConfig = other as SimpleMeasurementMetricSeriesConfiguration;
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

        public bool Equals(SimpleMeasurementMetricSeriesConfiguration other)
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
