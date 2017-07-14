using System;
using System.Runtime.CompilerServices;

using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    internal class SimpleMeasurementMetricConfiguration : IMetricConfiguration
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

        public SimpleMeasurementMetricConfiguration(bool lifetimeCounter, bool supportDoubleValues)
        {
            _lifetimeCounter = lifetimeCounter;
            _supportDoubleValues = supportDoubleValues;
        }

        public IMetricDataSeriesAggregator CreateNewAggregator(MetricDataSeries dataSeries, MetricConsumerKind consumerKind)
        {
            if (_supportDoubleValues)
            {
                IMetricDataSeriesAggregator aggregator = new SimpleDoubleDataSeriesAggregator(this, dataSeries, consumerKind);
                return aggregator;
            }
            else
            {
                IMetricDataSeriesAggregator aggregator = new SimpleUIntDataSeriesAggregator(this, dataSeries, consumerKind);
                return aggregator;
            }
        }
    }
}
