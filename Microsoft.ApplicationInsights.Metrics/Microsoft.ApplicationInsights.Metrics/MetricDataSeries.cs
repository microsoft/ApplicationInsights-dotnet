using System;

namespace Microsoft.ApplicationInsights.Metrics
{
    public sealed class MetricDataSeries
    {
        private readonly WeakReference<IMetricDataSeriesAggregator> _currentAggregator;
        private readonly MetricManager _metricManager;

        internal MetricDataSeries(MetricManager metricManager)
        {
            if (metricManager == null)
            {
                throw new ArgumentNullException(nameof(metricManager));
            }

            _metricManager = metricManager;
            _currentAggregator = new WeakReference<IMetricDataSeriesAggregator>(null, trackResurrection: false);
        }

        public object Context { get; }
        public void TrackValue(double metricValue) { }

        public IMetricDataSeriesAggregator GetCurrentAggregator()
        {
            IMetricDataSeriesAggregator aggregator;
            bool hasDirectPointer = _currentAggregator.TryGetTarget(out aggregator);

            return null;
        }
    }
}
