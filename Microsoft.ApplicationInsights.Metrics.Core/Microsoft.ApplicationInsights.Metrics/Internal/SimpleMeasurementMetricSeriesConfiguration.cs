using System;
using System.Runtime.CompilerServices;

using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    internal class SimpleMeasurementMetricSeriesConfiguration : IMetricSeriesConfiguration
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
    }
}
