using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;

using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Metrics.Extensibility;
using Microsoft.ApplicationInsights.Channel;

using CycleKind = Microsoft.ApplicationInsights.Metrics.Extensibility.MetricAggregationCycleKind;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class MetricSeries
    {
        private readonly MetricAggregationManager _aggregationManager;
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

        internal readonly IMetricSeriesConfiguration _configuration;

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
        public TelemetryContext Context { get { return _context; } }

        /// <summary>
        /// 
        /// </summary>
        public string MetricId { get { return _metricId; } }

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
                TrackValue(GetOrCreateAggregator(CycleKind.Default, ref _aggregatorDefault), metricValue, ref errors);
                TrackValue(GetOrCreateAggregator(CycleKind.QuickPulse, ref _aggregatorQuickPulse), metricValue, ref errors);
                TrackValue(GetOrCreateAggregator(CycleKind.Custom, ref _aggregatorCustom), metricValue, ref errors);
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
                TrackValue(GetOrCreateAggregator(CycleKind.Default, ref _aggregatorDefault), metricValue, ref errors);
                TrackValue(GetOrCreateAggregator(CycleKind.QuickPulse, ref _aggregatorQuickPulse), metricValue, ref errors);
                TrackValue(GetOrCreateAggregator(CycleKind.Custom, ref _aggregatorCustom), metricValue, ref errors);
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
        public void ResetAggregation()
        {
            ResetAggregation(periodStart: DateTimeOffset.Now, valueFilter: null);
        }

        /// <summary>
        /// 
        /// </summary>
        public void ResetAggregation(DateTimeOffset periodStart)
        {
            ResetAggregation(periodStart, valueFilter: null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="periodStart"></param>
        /// <param name="valueFilter"></param>
        public void ResetAggregation(DateTimeOffset periodStart, IMetricValueFilter valueFilter)
        {
            if (_requiresPersistentAggregator)
            {
                IMetricSeriesAggregator aggregator = _aggregatorPersistent;
                aggregator?.Reset(periodStart, valueFilter);
            }
            else
            {
                {
                    IMetricSeriesAggregator aggregator = UnwrapAggregator(_aggregatorDefault);
                    aggregator?.Reset(periodStart, valueFilter);
                }
                {
                    IMetricSeriesAggregator aggregator = UnwrapAggregator(_aggregatorQuickPulse);
                    aggregator?.Reset(periodStart, valueFilter);
                }
                {
                    IMetricSeriesAggregator aggregator = UnwrapAggregator(_aggregatorCustom);
                    aggregator?.Reset(periodStart, valueFilter);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ITelemetry GetCurrentAggregateUnsafe()
        {
            return GetCurrentAggregateUnsafe(CycleKind.Default, DateTimeOffset.Now);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aggregationCycleKind"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public ITelemetry GetCurrentAggregateUnsafe(MetricAggregationCycleKind aggregationCycleKind, DateTimeOffset dateTime)
        {
            IMetricSeriesAggregator aggregator = null;

            if (_requiresPersistentAggregator)
            {
                aggregator = _aggregatorPersistent;
            }
            else
            {
                switch (aggregationCycleKind)
                {
                    case CycleKind.Default:
                        aggregator = UnwrapAggregator(_aggregatorDefault);
                        break;

                    case CycleKind.QuickPulse:
                        aggregator = UnwrapAggregator(_aggregatorQuickPulse);
                        break;

                    case CycleKind.Custom:
                        aggregator = UnwrapAggregator(_aggregatorCustom);
                        break;

                    default:
                        throw new ArgumentException($"Unexpected value of {nameof(aggregationCycleKind)}: {aggregationCycleKind}.");
                }
            }

            ITelemetry aggregate = aggregator?.CreateAggregateUnsafe(dateTime);
            return aggregate;
        }

        internal void ClearAggregator(MetricAggregationCycleKind aggregationCycleKind)
        {
            if (_requiresPersistentAggregator)
            {
                return;
            }

            WeakReference<IMetricSeriesAggregator> aggregatorWeakRef;
            switch (aggregationCycleKind)
            {
                case CycleKind.Default:
                    aggregatorWeakRef = Interlocked.Exchange(ref _aggregatorDefault, null);
                    _aggregatorRecycleCacheDefault = UnwrapAggregator(aggregatorWeakRef);
                    break;

                case CycleKind.QuickPulse:
                    aggregatorWeakRef = Interlocked.Exchange(ref _aggregatorQuickPulse, null);
                    _aggregatorRecycleCacheQuickPulse = UnwrapAggregator(aggregatorWeakRef);
                    break;

                case CycleKind.Custom:
                    aggregatorWeakRef = Interlocked.Exchange(ref _aggregatorCustom, null);
                    _aggregatorRecycleCacheCustom = UnwrapAggregator(aggregatorWeakRef);
                    break;

                default:
                    throw new ArgumentException($"Unexpected value of {nameof(aggregationCycleKind)}: {aggregationCycleKind}.");
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
                aggregator = _configuration.CreateNewAggregator(this, CycleKind.Default);
                IMetricSeriesAggregator prevAggregator = Interlocked.CompareExchange(ref _aggregatorPersistent, aggregator, null);

                if (prevAggregator == null)
                {
                    bool added = _aggregationManager.AddAggregator(aggregator, CycleKind.Default);
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
        
        private IMetricSeriesAggregator GetOrCreateAggregator(MetricAggregationCycleKind aggregationCycleKind, ref WeakReference<IMetricSeriesAggregator> aggregatorWeakRef)
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

                if (aggregationCycleKind != CycleKind.Default)
                { 
                    IMetricSeriesFilter dataSeriesFilter;
                    if (! _aggregationManager.IsConsumerActive(aggregationCycleKind, out dataSeriesFilter))
                    {
                        return null;
                    }

                    // Ok, a consumer is, indeed, hooked up. See if the consumer's filter is interested in this series:

                    IMetricValueFilter valuesFilter;
                    try
                    { 
                        if (dataSeriesFilter != null && false == dataSeriesFilter.WillConsume(this, out valuesFilter))
                        {
                            return null;
                        }
                    }
                    catch
                    {
                        // Protect against errors in user's implemenmtation of IMetricSeriesFilter.IsInterestedIn(..).
                        // If it throws, assume that the filter is not functional => consumer will accept all values.
                    }
                }

                // Ok, they want to consume us. Create new aggregator and a weak reference to it:

                IMetricSeriesAggregator newAggregator = GetNewOrRecycledAggregatorInstance(aggregationCycleKind);
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
                    bool added = _aggregationManager.AddAggregator(newAggregator, aggregationCycleKind);

                    // If the manager does not accept the addition, it means that the aggregationCycleKind is disabled or that the filter is not interested in this data series.
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

        private IMetricSeriesAggregator GetNewOrRecycledAggregatorInstance(MetricAggregationCycleKind aggregationCycleKind)
        {
            IMetricSeriesAggregator aggregator = GetRecycledAggregatorInstance(aggregationCycleKind);
            return (aggregator ?? _configuration.CreateNewAggregator(this, aggregationCycleKind));
        }

        /// <summary>
        /// The lifetime of an aggragator can easily be a minute or so. So, it is a relatively small object that can easily get into Gen-2 GC heap,
        /// but then will need to be reclaimed from there relatively quickly. This can lead to a fragmentation of Gen-2 heap. To avoid this we employ
        /// a simple form of object pooling: Each data series keeps an instance of a past aggregator and tries to reuse it.
        /// Aggregator implementations which believe that they are too expensive to recycle for this, can opt out of this strategy by returning FALSE from
        /// their CanRecycle property.
        /// </summary>
        /// <param name="aggregationCycleKind"></param>
        /// <returns></returns>
        private IMetricSeriesAggregator GetRecycledAggregatorInstance(MetricAggregationCycleKind aggregationCycleKind)
        {
            if (_requiresPersistentAggregator)
            {
                return null;
            }

            IMetricSeriesAggregator aggregator = null;
            switch (aggregationCycleKind)
            {
                case CycleKind.Default:
                    aggregator = Interlocked.Exchange(ref _aggregatorRecycleCacheDefault, null);
                    break;

                case CycleKind.QuickPulse:
                    aggregator = Interlocked.Exchange(ref _aggregatorRecycleCacheQuickPulse, null);
                    break;

                case CycleKind.Custom:
                    aggregator = Interlocked.Exchange(ref _aggregatorRecycleCacheCustom, null);
                    break;
            }

            if (aggregator == null)
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
