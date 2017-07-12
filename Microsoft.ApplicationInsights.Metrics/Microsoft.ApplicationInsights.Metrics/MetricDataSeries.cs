using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;

using Microsoft.ApplicationInsights.Metrics.Extensibility;
using Microsoft.ApplicationInsights.DataContracts;

namespace Microsoft.ApplicationInsights.Metrics
{
    public sealed class MetricDataSeries
    {
        private readonly MetricAggregationManager _aggregationManager;
        private readonly IMetricConfiguration _configuration;
        private readonly bool _requiresPersistentAggregator;
        private readonly string _metricId;
        private readonly TelemetryContext _context;

        private IMetricDataSeriesAggregator _aggregatorPersistent;
        private WeakReference<IMetricDataSeriesAggregator> _aggregatorDefault;
        private WeakReference<IMetricDataSeriesAggregator> _aggregatorQuickPulse;
        private WeakReference<IMetricDataSeriesAggregator> _aggregatorCustom;

        public IMetricConfiguration Configuration { get { return _configuration; } }

        public TelemetryContext Context { get { return _context; } }

        public string MetricId { get { return _metricId; } }

        internal MetricDataSeries(MetricAggregationManager aggregationManager, string metricId, IMetricConfiguration configuration)
        {
            Util.ValidateNotNull(aggregationManager, nameof(aggregationManager));
            Util.ValidateNotNull(metricId, nameof(metricId));
            Util.ValidateNotNull(configuration, nameof(configuration));

            _aggregationManager = aggregationManager;
            _metricId = metricId;
            _configuration = configuration;
            _requiresPersistentAggregator = configuration.RequiresPersistentAggregation;
            _context = new TelemetryContext();

            _aggregatorPersistent = null;
            _aggregatorDefault = null;
            _aggregatorQuickPulse = null;
            _aggregatorCustom = null;
        }

        public IMetricDataSeriesAggregator GetCurrentAggregator()
        {
            IMetricDataSeriesAggregator aggregator = _configuration.RequiresPersistentAggregation
                                                            ? _aggregatorPersistent
                                                            : UnwrapAggregator(_aggregatorDefault);
            return aggregator;
        }

        public void TrackValue(uint metricValue)
        {
            List<Exception> errors = null;

            if (_requiresPersistentAggregator)
            {
                TrackValue(GetOrCreatePersistentAggregator(), metricValue, ref errors);
            }
            else
            {
                TrackValue(GetOrCreateAggregator(MetricConsumerKind.Default, ref _aggregatorDefault), metricValue, ref errors);
                TrackValue(GetOrCreateAggregator(MetricConsumerKind.QuickPulse, ref _aggregatorQuickPulse), metricValue, ref errors);
                TrackValue(GetOrCreateAggregator(MetricConsumerKind.Custom, ref _aggregatorCustom), metricValue, ref errors);
            }

            if (errors != null)
            {
                if (errors.Count == 1)
                {
                    ExceptionDispatchInfo.Capture(errors[0]).Throw();
                }
                else
                {
                    throw new AggregateException(errors);
                }
            }
        }

        public void TrackValue(double metricValue)
        {
            List<Exception> errors = null;

            if (_requiresPersistentAggregator)
            {
                TrackValue(GetOrCreatePersistentAggregator(), metricValue, ref errors);
            }
            else
            {
                TrackValue(GetOrCreateAggregator(MetricConsumerKind.Default, ref _aggregatorDefault), metricValue, ref errors);
                TrackValue(GetOrCreateAggregator(MetricConsumerKind.QuickPulse, ref _aggregatorQuickPulse), metricValue, ref errors);
                TrackValue(GetOrCreateAggregator(MetricConsumerKind.Custom, ref _aggregatorCustom), metricValue, ref errors);
            }

            if (errors != null)
            {
                if (errors.Count == 1)
                {
                    ExceptionDispatchInfo.Capture(errors[0]).Throw();
                }
                else
                {
                    throw new AggregateException(errors);
                }
            }
        }

        public void TrackValue(object metricValue)
        {
            List<Exception> errors = null;

            if (_requiresPersistentAggregator)
            {
                TrackValue(GetOrCreatePersistentAggregator(), metricValue, ref errors);
            }
            else
            {
                TrackValue(GetOrCreateAggregator(MetricConsumerKind.Default, ref _aggregatorDefault), metricValue, ref errors);
                TrackValue(GetOrCreateAggregator(MetricConsumerKind.QuickPulse, ref _aggregatorQuickPulse), metricValue, ref errors);
                TrackValue(GetOrCreateAggregator(MetricConsumerKind.Custom, ref _aggregatorCustom), metricValue, ref errors);
            }

            if (errors != null)
            {
                if (errors.Count == 1)
                {
                    ExceptionDispatchInfo.Capture(errors[0]).Throw();
                }
                else
                {
                    throw new AggregateException(errors);
                }
            }
        }

        private void TrackValue(IMetricDataSeriesAggregator aggregator, uint metricValue, ref List<Exception> errors)
        {
            if (aggregator != null)
            {
                try
                {
                    aggregator.TrackValue(metricValue);
                }
                catch (Exception ex)
                {
                    (errors = errors ?? new List<Exception>()).Add(ex);
                }
            }
        }

        private void TrackValue(IMetricDataSeriesAggregator aggregator, double metricValue, ref List<Exception> errors)
        {
            if (aggregator != null)
            {
                try
                {
                    aggregator.TrackValue(metricValue);
                }
                catch (Exception ex)
                {
                    (errors = errors ?? new List<Exception>()).Add(ex);
                }
            }
        }

        private void TrackValue(IMetricDataSeriesAggregator aggregator, object metricValue, ref List<Exception> errors)
        {
            if (aggregator != null)
            {
                try
                {
                    aggregator.TrackValue(metricValue);
                }
                catch (Exception ex)
                {
                    (errors = errors ?? new List<Exception>()).Add(ex);
                }
            }
        }

        private IMetricDataSeriesAggregator GetOrCreatePersistentAggregator()
        {
            IMetricDataSeriesAggregator aggregator = _aggregatorPersistent;

            if (aggregator == null)
            {
                aggregator = _configuration.CreateNewAggregator(this, MetricConsumerKind.Default);
                IMetricDataSeriesAggregator prevAggregator = Interlocked.CompareExchange(ref _aggregatorPersistent, aggregator, null);

                if (prevAggregator == null)
                {
                    bool added = _aggregationManager.AddAggregator(aggregator, MetricConsumerKind.Default);
                    if (added == false)
                    {
                        return GetOrCreatePersistentAggregator();
                    }
                }
                else
                {
                    aggregator = prevAggregator;
                }
            }

            return aggregator;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IMetricDataSeriesAggregator UnwrapAggregator(WeakReference<IMetricDataSeriesAggregator> aggregatorWeakRef)
        {
            if (aggregatorWeakRef != null)
            {
                IMetricDataSeriesAggregator aggregatorHardRef = null;
                if (aggregatorWeakRef.TryGetTarget(out aggregatorHardRef))
                {
                    return aggregatorHardRef;
                }
            }

            return null;
        }

        private IMetricDataSeriesAggregator GetOrCreateAggregator(MetricConsumerKind consumerKind, ref WeakReference<IMetricDataSeriesAggregator> aggregatorWeakRef)
        {
            while (true)
            {
                // Local cache for the reference in case of concurrnet updates:
                WeakReference<IMetricDataSeriesAggregator> currentAggregatorWeakRef = aggregatorWeakRef;

                // Try to dereference the weak reference:
                IMetricDataSeriesAggregator aggregatorHardRef = UnwrapAggregator(currentAggregatorWeakRef);
                if (aggregatorHardRef != null)
                {
                    return aggregatorHardRef;
                }

                // END OF FAST PATH. Could not dereference aggregator. Will attempt to create it...

                // So aggretator is NULL. For non-default consumers, see if the there is even a consumer hooked up:

                if (consumerKind != MetricConsumerKind.Default)
                { 
                    IMetricDataSeriesFilter dataSeriesFilter;
                    if (! _aggregationManager.IsConsumerActive(consumerKind, out dataSeriesFilter))
                    {
                        return null;
                    }

                    // Ok, a consumer is, indeed, hooked up. See if the consumer's filter is interested in this series:

                    IMetricValueFilter valuesFilter;
                    try
                    { 
                        if (! dataSeriesFilter.WillConsume(this, out valuesFilter))
                        {
                            return null;
                        }
                    }
                    catch
                    {
                        // Protect against errors in user's implemenmtation of IMetricDataSeriesFilter.IsInterestedIn(..).
                        return null;
                    }
                }

                // Ok, they want to consume us. Create new aggregator and a weak reference to it:

                IMetricDataSeriesAggregator newAggregator = _configuration.CreateNewAggregator(this, consumerKind);
                WeakReference<IMetricDataSeriesAggregator> newAggregatorWeakRef = new WeakReference<IMetricDataSeriesAggregator>(newAggregator, trackResurrection: false);

                // Store the weak reference to the aggregator. However, there is a race on doing it, so check for it:
                WeakReference<IMetricDataSeriesAggregator> prevAggregatorWeakRef = Interlocked.CompareExchange(ref aggregatorWeakRef, newAggregatorWeakRef, currentAggregatorWeakRef);
                if (prevAggregatorWeakRef == currentAggregatorWeakRef)
                {
                    // We won the race and stored the aggregator. Now tell the manager about it and go on using it.
                    // Note that the status of IsConsumerActive and the dataSeriesFilter may have changed concurrently.
                    // So it is essential that we do this after the above interlocked assignment of aggregator.
                    // It ensures that only objects are added to the aggregator collection that are also referenced by the data series.
                    // In addition, AddAggregator(..) always uses the current value of the aggregator-collection in a thread-safe manner.
                    // Becasue the aggregator collection reference is always updated before telling the aggregators to cycle,
                    // we know that any aggregator added will be eventually cycled.
                    // If adding succeeds, AddAggregator(..) will have set the correct filter.
                    bool added = _aggregationManager.AddAggregator(newAggregator, consumerKind);

                    // If the manager does not accept the addition, it means that the consumerKind is disabled or that the filter is not interested in this data series.
                    // Either way we need to give up.
                    if (added)
                    {
                        return newAggregator;
                    }
                    else
                    {
                        // We could have accepted some values into this aggregator in the short time it was set in this series. We will loose those values.
                        Interlocked.CompareExchange(ref aggregatorWeakRef, null, newAggregatorWeakRef);
                        return null;
                    }
                }

                // We lost the race and a different aggregator was used. Loop again. Doing so will attempt to dereference the latest aggregator reference.
            }
        }

        internal void ClearAggregator(MetricConsumerKind consumerKind)
        {
            if (_requiresPersistentAggregator)
            {
                return;
            }

            switch (consumerKind)
            {
                case MetricConsumerKind.Default:
                    Interlocked.Exchange(ref _aggregatorDefault, null);
                    break;

                case MetricConsumerKind.QuickPulse:
                    Interlocked.Exchange(ref _aggregatorQuickPulse, null);
                    break;

                case MetricConsumerKind.Custom:
                    Interlocked.Exchange(ref _aggregatorCustom, null);
                    break;

                default:
                    throw new ArgumentException($"Unexpected value of {nameof(consumerKind)}: {consumerKind}.");
            }
        }
    }
}
