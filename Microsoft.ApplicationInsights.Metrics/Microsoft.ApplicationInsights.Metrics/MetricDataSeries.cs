using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.ApplicationInsights.Metrics
{
    public sealed class MetricDataSeries
    {
        private readonly MetricAggregationManager _aggregationManager;
        private readonly IMetricDataSeriesConfiguration _configuration;

        private WeakReference<IMetricDataSeriesAggregator> _aggregatorDefault;
        private WeakReference<IMetricDataSeriesAggregator> _aggregatorQuickPulse;
        private WeakReference<IMetricDataSeriesAggregator> _aggregatorCustom;

        public IMetricDataSeriesConfiguration Configuration { get { return _configuration; } }

        internal MetricDataSeries(MetricAggregationManager aggregationManager, IMetricDataSeriesConfiguration configuration)
        {
            Util.ValidateNotNull(aggregationManager, nameof(aggregationManager));
            Util.ValidateNotNull(configuration, nameof(configuration));

            _aggregationManager = aggregationManager;
            _configuration = configuration;

            _aggregatorDefault = null;
            _aggregatorQuickPulse = null;
            _aggregatorCustom = null;
        }

        public object Context { get; }

        public void TrackValue(double metricValue)
        {
            List<Exception> errors = null;

            try
            {
                IMetricDataSeriesAggregator aggregator = GetAggregator(MetricConsumerKind.Default);
                if (aggregator != null)
                {
                    aggregator.TrackValue(metricValue);
                }
            }
            catch (Exception ex)
            {
                errors = errors ?? new List<Exception>();
                errors.Add(ex);
            }

            try
            {
                IMetricDataSeriesAggregator aggregator = GetAggregator(MetricConsumerKind.QuickPulse);
                if (aggregator != null)
                {
                    aggregator.TrackValue(metricValue);
                }
            }
            catch (Exception ex)
            {
                errors = errors ?? new List<Exception>();
                errors.Add(ex);
            }
        }

        private IMetricDataSeriesAggregator GetAggregator(MetricConsumerKind consumerKind)
        {
            while (true)
            {
                // Alias for the correct reference for the specified consumer kind:
                ref WeakReference<IMetricDataSeriesAggregator> aggregatorRef = ref PickAggregatorReference(consumerKind);

                // Local cache for the reference in case of cuncurrnet updates:
                WeakReference<IMetricDataSeriesAggregator> currentAggregatorRef = Volatile.Read(ref aggregatorRef);

                // Try to dereference the weak reference:
                IMetricDataSeriesAggregator aggregator = null;
                if (currentAggregatorRef != null)
                {
                    if (! currentAggregatorRef.TryGetTarget(out aggregator))
                    {
                        aggregator = null;
                    }
                }

                // Succesfully dereferenced the weak reference. Just use the aggregator:
                if (aggregator != null)
                {
                    return aggregator;
                }

                // END OF FAST PATH.

                // So aggretator is NULL. See if the there is even a consumer hooked up:

                IMetricDataSeriesFilter dataSeriesFilter;
                if (! _aggregationManager.IsConsumerActive(consumerKind, out dataSeriesFilter))
                {
                    return null;
                }

                // Ok, a consumer is, indeed, hooked up. See if the consumer's filter is interested in this series:

                IMetricValueFilter valuesFilter;
                if (! dataSeriesFilter.IsInterestedIn(this, out valuesFilter))
                {
                    return null;
                }

                // Ok, they want to consume us. Create new aggregator and a weak reference to it:

                aggregator = _configuration.CreateNewAggregator(this, valuesFilter);
                WeakReference<IMetricDataSeriesAggregator> newAggregatorRef = new WeakReference<IMetricDataSeriesAggregator>(aggregator, trackResurrection: false);

                // Store the weak reference to the aggregator. However, there is a race on doing it, so check for it:
                WeakReference<IMetricDataSeriesAggregator> prevAggregatorRef = Interlocked.CompareExchange(ref aggregatorRef, newAggregatorRef, currentAggregatorRef);
                if (prevAggregatorRef == currentAggregatorRef)
                {
                    // We won the race and stored the aggrgator. Now tell the manager about it and go on using it.
                    // Note that the status of IsConsumerActive and the dataSeriesFilter may have changed concurrently.
                    // If adding succeeds, AddAggrgator will have set the correct filter.
                    bool added = _aggregationManager.AddAggregator(aggregator, consumerKind);

                    // If the manager does not accept the addition, it means that the consumerKind is disabled or that the filter is not interested in this data series.
                    // Either way we need to give up.
                    if (added)
                    {
                        return aggregator;
                    }
                    else
                    {
                        // We could have accepted some values into this aggregator in the short time it was set in this series. We will loose those values.
                        Interlocked.CompareExchange(ref aggregatorRef, null, newAggregatorRef);
                        return null;
                    }
                }

                // We lost the race and a different aggregator was used. Loop again. Doing so will attempt to dereference the latest aggregator reference.
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref WeakReference<IMetricDataSeriesAggregator> PickAggregatorReference(MetricConsumerKind consumerKind)
        {
            switch (consumerKind)
            {
                case MetricConsumerKind.Default:
                    return ref _aggregatorDefault;

                case MetricConsumerKind.QuickPulse:
                    return ref _aggregatorQuickPulse;

                case MetricConsumerKind.Custom:
                    return ref _aggregatorCustom;

                default:
                    throw new ArgumentException($"Unexpected value of {nameof(consumerKind)}: {consumerKind}.");
            }
        }

    }
}
