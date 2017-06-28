using Microsoft.ApplicationInsights.Channel;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.Metrics
{
    public class MetricAggregationManager
    {
        private class MetricAggregatorCollection
        {
            public DateTimeOffset PeriodStart { get; }
            public DateTimeOffset PeriodEnd { get; private set; }
            public GrowingCollection<IMetricDataSeriesAggregator> Aggregators { get; }
            public IMetricDataSeriesFilter Filter { get; }

            public MetricAggregatorCollection(DateTimeOffset periodStart, IMetricDataSeriesFilter filter)
            {
                this.PeriodStart = periodStart;
                this.PeriodEnd = DateTimeOffset.MinValue;
                this.Aggregators = new GrowingCollection<IMetricDataSeriesAggregator>();
                this.Filter = Filter;
            }
        }

        public class AggregationPeriodSummary
        {
            public IReadOnlyCollection<ITelemetry> UnfilteredValuesAggregates { get; }
            public IReadOnlyCollection<ITelemetry> FilteredAggregates { get; }
            
            public AggregationPeriodSummary(IReadOnlyCollection<ITelemetry> unfilteredValuesAggregates, IReadOnlyCollection<ITelemetry> filteredAggregates)
            {
                UnfilteredValuesAggregates = unfilteredValuesAggregates;
                FilteredAggregates = filteredAggregates;
            }
        }


        private MetricAggregatorCollection _aggregatorsForDefaultPersistent = null;
        private MetricAggregatorCollection _aggregatorsForDefault = null;
        private MetricAggregatorCollection _aggregatorsForQuickPulse= null;
        private MetricAggregatorCollection _aggregatorsForCustom = null;

        public bool StartAggregators(MetricConsumerKind consumerKind, IMetricDataSeriesFilter filter, DateTimeOffset tactTimestamp)
        {
            switch (consumerKind)
            {
                case MetricConsumerKind.QuickPulse:
                case MetricConsumerKind.Custom:
                    ref MetricAggregatorCollection aggregatorsToStart = ref PickAggregatorCollection(consumerKind);
                    MetricAggregatorCollection prev = Interlocked.CompareExchange(ref aggregatorsToStart, new MetricAggregatorCollection(tactTimestamp, filter), null);
                    return (prev == null);

                case MetricConsumerKind.Default:
                    if (filter != null)
                    {
                        throw new ArgumentException($"Cannot specify non-null {nameof(filter)} when {nameof(consumerKind)} is {consumerKind}.");
                    }

                    MetricAggregatorCollection prevDef = Interlocked.CompareExchange(ref _aggregatorsForDefault, new MetricAggregatorCollection(tactTimestamp, null), null);
                    MetricAggregatorCollection prevDefPers = Interlocked.CompareExchange(ref _aggregatorsForDefaultPersistent, new MetricAggregatorCollection(tactTimestamp, null), null);
                    return (prevDef == null) && (prevDefPers == null);

                default:
                    throw new ArgumentException($"Unexpected value of {nameof(consumerKind)}: {consumerKind}.");
            }
        }

        public AggregationPeriodSummary StopAggregators(MetricConsumerKind consumerKind, DateTimeOffset tactTimestamp)
        {
            switch (consumerKind)
            {
                case MetricConsumerKind.QuickPulse:
                case MetricConsumerKind.Custom:
                    return CycleAggregators(consumerKind, null, tactTimestamp, stopAggregators: true);

                case MetricConsumerKind.Default:
                    throw new ArgumentException($"Cannot invoke {nameof(StopAggregators)} for default consumerKind: Default aggregators are always active.");

                default:
                    throw new ArgumentException($"Unexpected value of {nameof(consumerKind)}: {consumerKind}.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref MetricAggregatorCollection PickAggregatorCollection(MetricConsumerKind consumerKind, bool pickPersistentDefaults = false)
        {
            switch (consumerKind)
            {
                case MetricConsumerKind.Default:
                    if (pickPersistentDefaults)
                    {
                        return ref _aggregatorsForDefaultPersistent;
                    }
                    else
                    {
                        return ref _aggregatorsForDefault;
                    }
                    

                case MetricConsumerKind.QuickPulse:
                    return ref _aggregatorsForQuickPulse;

                case MetricConsumerKind.Custom:
                    return ref _aggregatorsForCustom;

                default:
                    throw new ArgumentException($"Unexpected value of {nameof(consumerKind)}: {consumerKind}.");
            }
        }

        internal bool IsConsumerActive(MetricConsumerKind consumerKind, out IMetricDataSeriesFilter filter)
        {
            ref MetricAggregatorCollection aggregatorsToCheckRef = ref PickAggregatorCollection(consumerKind);
            MetricAggregatorCollection aggregatorsToCheck = aggregatorsToCheckRef;

            if (aggregatorsToCheck == null)
            {
                filter = null;
                return false;
            }

            filter = aggregatorsToCheck.Filter;
            return true;
        }

        internal bool AddAggregator(IMetricDataSeriesAggregator aggregator, MetricConsumerKind consumerKind)
        {
            ref MetricAggregatorCollection aggregatorsRef = ref PickAggregatorCollection(consumerKind, pickPersistentDefaults: true);
            MetricAggregatorCollection aggregators = aggregatorsRef;

            if (aggregators == null)
            {
                return false;
            }

            IMetricValueFilter valueFilter = null;
            if (aggregators.Filter != null && ! aggregators.Filter.IsInterestedIn(aggregator.MetricDataSeries, out valueFilter))
            {
                return false;
            }

            aggregator.SetValueFilter(valueFilter);
            aggregators.Aggregators.Add(aggregator);

            return true;
        }

        public AggregationPeriodSummary CycleAggregators(MetricConsumerKind consumerKind, IMetricDataSeriesFilter updatedFilter, DateTimeOffset tactTimestamp)
        {
            return CycleAggregators(consumerKind, updatedFilter, tactTimestamp, stopAggregators: false);
        }

        private AggregationPeriodSummary CycleAggregators(MetricConsumerKind consumerKind, IMetricDataSeriesFilter updatedFilter, DateTimeOffset tactTimestamp, bool stopAggregators)
        {
            if (consumerKind == MetricConsumerKind.Default && updatedFilter != null)
            {
                throw new ArgumentException($"Cannot specify non-null {nameof(updatedFilter)} when {nameof(consumerKind)} is {consumerKind}.");
            }

            ref MetricAggregatorCollection aggregatorsToReplace = ref PickAggregatorCollection(consumerKind);

            // For non-persistent aggregators: create empty holder for the next aggregation period and swap for the previous holder:
            MetricAggregatorCollection nextAggregators = stopAggregators
                                                            ? null
                                                            : new MetricAggregatorCollection(tactTimestamp, updatedFilter);

            MetricAggregatorCollection prevAggregators = Interlocked.Exchange(ref aggregatorsToReplace, nextAggregators);
            IMetricDataSeriesFilter prevFilter = prevAggregators.Filter;

            // Complete each persistent aggregator:
            // (We expand the foreach statement so that we can use the typed enumerators Count property which is constsent with the data in the snapshot.)
            GrowingCollection<IMetricDataSeriesAggregator>.Enumerator unfilteredValsAggregators = _aggregatorsForDefaultPersistent.Aggregators.GetEnumerator(); 
            List<ITelemetry> unfilteredValsAggregations = new List<ITelemetry>(capacity: unfilteredValsAggregators.Count);
            try
            {
                while(unfilteredValsAggregators.MoveNext())
                {
                    IMetricDataSeriesAggregator aggregator = unfilteredValsAggregators.Current;
                    if (aggregator != null)
                    {
                        // Persistent aggregators are always active, regardless of filters for a particular consumer. But we can apply the cunsumer's filters to determine
                        // whether or not to pull the aggregator for a aggregate at this time. Of course, only series filters, not value filters, can be considered.
                        IMetricValueFilter unusedValueFilter;
                        bool satisfiesFilter = (prevFilter == null)
                                                ||
                                               (prevFilter.IsInterestedIn(aggregator.MetricDataSeries, out unusedValueFilter));
                        if (satisfiesFilter)
                        {
                            ITelemetry aggregate = aggregator.CompleteAggregationPeriod(tactTimestamp);
                            unfilteredValsAggregations.Add(aggregate);
                        }
                    }
                }
            }
            finally
            {
                unfilteredValsAggregators.Dispose();
            }

            // Complete each non-persistent aggregator:
            // (we snapshotted the entire collection, so Count is stable)
            List<ITelemetry> filteredAggregations = new List<ITelemetry>(capacity: prevAggregators.Aggregators.Count);
            foreach (IMetricDataSeriesAggregator aggregator in prevAggregators.Aggregators)
            {
                if (aggregator != null)
                {
                    ITelemetry aggregate = aggregator.CompleteAggregationPeriod(tactTimestamp);
                    filteredAggregations.Add(aggregate);
                }
            }

            var summary = new AggregationPeriodSummary(unfilteredValsAggregations, filteredAggregations);
            return summary;
        }

    }
}
