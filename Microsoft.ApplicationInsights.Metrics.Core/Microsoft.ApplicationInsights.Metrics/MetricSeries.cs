using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;

using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class MetricSeries
    {
        private readonly MetricAggregationManager _aggregationManager;
        private readonly IMetricSeriesConfiguration _configuration;
        private readonly bool _requiresPersistentAggregator;
        private readonly string _metricId;
        private readonly TelemetryContext _context;

        private IMetricSeriesAggregator _aggregatorPersistent;
        private WeakReference<IMetricSeriesAggregator> _aggregatorDefault;
        private WeakReference<IMetricSeriesAggregator> _aggregatorQuickPulse;
        private WeakReference<IMetricSeriesAggregator> _aggregatorCustom;

        private IMetricSeriesAggregator _aggregatorRecycleCacheDefault;
        private IMetricSeriesAggregator _aggregatorRecycleCacheQuickPulse;
        private IMetricSeriesAggregator _aggregatorRecycleCacheCustom;

        internal MetricSeries(MetricAggregationManager aggregationManager, string metricId, IMetricSeriesConfiguration configuration)
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

        /// <summary>
        /// 
        /// </summary>
        public IMetricSeriesConfiguration Configuration { get { return _configuration; } }

        /// <summary>
        /// 
        /// </summary>
        public TelemetryContext Context { get { return _context; } }

        /// <summary>
        /// 
        /// </summary>
        public string MetricId { get { return _metricId; } }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IMetricSeriesAggregator CurrentAggregator
        {
            get
            {
                IMetricSeriesAggregator aggregator = _configuration.RequiresPersistentAggregation
                                                                ? _aggregatorPersistent
                                                                : UnwrapAggregator(_aggregatorDefault);
                return aggregator;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="metricValue"></param>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="metricValue"></param>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="metricValue"></param>
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
        
        internal void ClearAggregator(MetricConsumerKind consumerKind)
        {
            if (_requiresPersistentAggregator)
            {
                return;
            }

            WeakReference<IMetricSeriesAggregator> aggregatorWeakRef;
            switch (consumerKind)
            {
                case MetricConsumerKind.Default:
                    aggregatorWeakRef = Interlocked.Exchange(ref _aggregatorDefault, null);
                    _aggregatorRecycleCacheDefault = UnwrapAggregator(aggregatorWeakRef);
                    break;

                case MetricConsumerKind.QuickPulse:
                    aggregatorWeakRef = Interlocked.Exchange(ref _aggregatorQuickPulse, null);
                    _aggregatorRecycleCacheQuickPulse = UnwrapAggregator(aggregatorWeakRef);
                    break;

                case MetricConsumerKind.Custom:
                    aggregatorWeakRef = Interlocked.Exchange(ref _aggregatorCustom, null);
                    _aggregatorRecycleCacheCustom = UnwrapAggregator(aggregatorWeakRef);
                    break;

                default:
                    throw new ArgumentException($"Unexpected value of {nameof(consumerKind)}: {consumerKind}.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aggregator"></param>
        /// <param name="metricValue"></param>
        /// <param name="errors"></param>
        private static void TrackValue(IMetricSeriesAggregator aggregator, uint metricValue, ref List<Exception> errors)
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

        private static void TrackValue(IMetricSeriesAggregator aggregator, double metricValue, ref List<Exception> errors)
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

        private static void TrackValue(IMetricSeriesAggregator aggregator, object metricValue, ref List<Exception> errors)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IMetricSeriesAggregator UnwrapAggregator(WeakReference<IMetricSeriesAggregator> aggregatorWeakRef)
        {
            if (aggregatorWeakRef != null)
            {
                IMetricSeriesAggregator aggregatorHardRef = null;
                if (aggregatorWeakRef.TryGetTarget(out aggregatorHardRef))
                {
                    return aggregatorHardRef;
                }
            }

            return null;
        }

        private IMetricSeriesAggregator GetOrCreatePersistentAggregator()
        {
            IMetricSeriesAggregator aggregator = _aggregatorPersistent;

            if (aggregator == null)
            {
                aggregator = _configuration.CreateNewAggregator(this, MetricConsumerKind.Default);
                IMetricSeriesAggregator prevAggregator = Interlocked.CompareExchange(ref _aggregatorPersistent, aggregator, null);

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
        
        private IMetricSeriesAggregator GetOrCreateAggregator(MetricConsumerKind consumerKind, ref WeakReference<IMetricSeriesAggregator> aggregatorWeakRef)
        {
            while (true)
            {
                // Local cache for the reference in case of concurrnet updates:
                WeakReference<IMetricSeriesAggregator> currentAggregatorWeakRef = aggregatorWeakRef;

                // Try to dereference the weak reference:
                IMetricSeriesAggregator aggregatorHardRef = UnwrapAggregator(currentAggregatorWeakRef);
                if (aggregatorHardRef != null)
                {
                    return aggregatorHardRef;
                }

                // END OF FAST PATH. Could not dereference aggregator. Will attempt to create it...

                // So aggretator is NULL. For non-default consumers, see if the there is even a consumer hooked up:

                if (consumerKind != MetricConsumerKind.Default)
                { 
                    IMetricSeriesFilter dataSeriesFilter;
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
                        // Protect against errors in user's implemenmtation of IMetricSeriesFilter.IsInterestedIn(..).
                        return null;
                    }
                }

                // Ok, they want to consume us. Create new aggregator and a weak reference to it:

                IMetricSeriesAggregator newAggregator = GetNewOrRecycledAggregatorInstance(consumerKind);
                WeakReference<IMetricSeriesAggregator> newAggregatorWeakRef = new WeakReference<IMetricSeriesAggregator>(newAggregator, trackResurrection: false);

                // Store the weak reference to the aggregator. However, there is a race on doing it, so check for it:
                WeakReference<IMetricSeriesAggregator> prevAggregatorWeakRef = Interlocked.CompareExchange(ref aggregatorWeakRef, newAggregatorWeakRef, currentAggregatorWeakRef);
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

        private IMetricSeriesAggregator GetNewOrRecycledAggregatorInstance(MetricConsumerKind consumerKind)
        {
            IMetricSeriesAggregator aggregator = GetRecycledAggregatorInstance(consumerKind);
            return (aggregator ?? _configuration.CreateNewAggregator(this, consumerKind));
        }

        /// <summary>
        /// The lifetime of an aggragator can easily be a minute or so. So, it is a relatively small object that can easily get into Gen-2 GC heap,
        /// but then will need to be reclaimed from there relatively quickly. This can lead to a fragmentation of Gen-2 heap. To avoid this we employ
        /// a simple form of object pooling: Each data series keeps an instance of a past aggregator and tries to reuse it.
        /// Aggregator implementations which believe that they are too expensive to recycle for this, can opt out of this strategy by returning FALSE from
        /// their CanRecycle property.
        /// </summary>
        /// <param name="consumerKind"></param>
        /// <returns></returns>
        private IMetricSeriesAggregator GetRecycledAggregatorInstance(MetricConsumerKind consumerKind)
        {
            if (_requiresPersistentAggregator)
            {
                return null;
            }

            IMetricSeriesAggregator aggregator = null;
            switch (consumerKind)
            {
                case MetricConsumerKind.Default:
                    aggregator = Interlocked.Exchange(ref _aggregatorRecycleCacheDefault, null);
                    break;

                case MetricConsumerKind.QuickPulse:
                    aggregator = Interlocked.Exchange(ref _aggregatorRecycleCacheQuickPulse, null);
                    break;

                case MetricConsumerKind.Custom:
                    aggregator = Interlocked.Exchange(ref _aggregatorRecycleCacheCustom, null);
                    break;
            }

            if (aggregator == null)
            {
                return null;
            }

            if (! aggregator.SupportsRecycle)
            {
                return null;
            }

            if (aggregator.TryRecycle())
            {
                return aggregator;
            }

            return null;
        }
    }
}
