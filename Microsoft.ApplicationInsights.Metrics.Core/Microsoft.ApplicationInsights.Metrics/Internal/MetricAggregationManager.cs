using System;
using System.Collections.Generic;
using System.Threading;

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    internal class MetricAggregationManager
    {
        private class AggregatorCollection
        {
            public DateTimeOffset PeriodStart { get; }
            public DateTimeOffset PeriodEnd { get; private set; }
            public GrowingCollection<IMetricDataSeriesAggregator> Aggregators { get; }
            public IMetricDataSeriesFilter Filter { get; }

            public AggregatorCollection(DateTimeOffset periodStart, IMetricDataSeriesFilter filter)
            {
                this.PeriodStart = periodStart;
                this.PeriodEnd = DateTimeOffset.MinValue;
                this.Aggregators = new GrowingCollection<IMetricDataSeriesAggregator>();
                this.Filter = Filter;
            }
        }

        // We support 4 aggregation cycles. 2 of them can be accessed from the outside:

        private AggregatorCollection _aggregatorsForDefaultPersistent = null;
        private AggregatorCollection _aggregatorsForDefault = null;
        private AggregatorCollection _aggregatorsForQuickPulse = null;
        private AggregatorCollection _aggregatorsForCustom = null;

        internal MetricAggregationManager()
        {
            StartDefaultAggregators();
        }

        private void StartDefaultAggregators()
        {
            DateTimeOffset now = DateTimeOffset.Now;
            DateTimeOffset timestamp = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, 0, now.Offset);

            _aggregatorsForDefault = new AggregatorCollection(timestamp, filter: null);
            _aggregatorsForDefaultPersistent = new AggregatorCollection(timestamp, filter: null);
        }

        public bool StartAggregators(MetricConsumerKind consumerKind, DateTimeOffset tactTimestamp, IMetricDataSeriesFilter filter)
        {
            switch (consumerKind)
            {
                case MetricConsumerKind.QuickPulse:
                    AggregatorCollection prevQP = Interlocked.CompareExchange(ref _aggregatorsForQuickPulse, new AggregatorCollection(tactTimestamp, filter), comparand: null);
                    return (prevQP == null);

                case MetricConsumerKind.Custom:
                    AggregatorCollection prevC = Interlocked.CompareExchange(ref _aggregatorsForCustom, new AggregatorCollection(tactTimestamp, filter), comparand: null);
                    return (prevC == null);

                case MetricConsumerKind.Default:
                    throw new ArgumentException($"Cannot invoke {nameof(StartAggregators)} for Default {nameof(MetricConsumerKind)}: Default aggregators are always active.");

                default:
                    throw new ArgumentException($"Unexpected value of {nameof(consumerKind)}: {consumerKind}.");
            }
        }

        public AggregationPeriodSummary StopAggregators(MetricConsumerKind consumerKind, DateTimeOffset tactTimestamp)
        {
            switch (consumerKind)
            {
                case MetricConsumerKind.QuickPulse:
                    return CycleAggregators(ref _aggregatorsForQuickPulse, tactTimestamp, updatedFilter: null, stopAggregators: true);

                case MetricConsumerKind.Custom:
                    return CycleAggregators(ref _aggregatorsForCustom, tactTimestamp, updatedFilter: null, stopAggregators: true);

                case MetricConsumerKind.Default:
                    throw new ArgumentException($"Cannot invoke {nameof(StopAggregators)} for Default {nameof(MetricConsumerKind)}: Default aggregators are always active.");

                default:
                    throw new ArgumentException($"Unexpected value of {nameof(consumerKind)}: {consumerKind}.");
            }
        }

        internal bool IsConsumerActive(MetricConsumerKind consumerKind, out IMetricDataSeriesFilter filter)
        {
            switch (consumerKind)
            {
                case MetricConsumerKind.Default:
                    filter = null;
                    return true;

                case MetricConsumerKind.QuickPulse:
                case MetricConsumerKind.Custom:
                    AggregatorCollection aggs = (consumerKind == MetricConsumerKind.QuickPulse) ? _aggregatorsForQuickPulse : _aggregatorsForCustom;
                    filter = aggs?.Filter;
                    return (aggs != null);

                default:
                    throw new ArgumentException($"Unexpected value of {nameof(consumerKind)}: {consumerKind}.");
            }
        }

        internal bool AddAggregator(IMetricDataSeriesAggregator aggregator, MetricConsumerKind consumerKind)
        {
            Util.ValidateNotNull(aggregator, nameof(aggregator));

            if (aggregator.DataSeries.Configuration.RequiresPersistentAggregation)
            {
                return AddAggregator(aggregator, _aggregatorsForDefaultPersistent);
            }

            switch (consumerKind)
            {
                case MetricConsumerKind.Default:
                    return AddAggregator(aggregator, _aggregatorsForDefault);

                case MetricConsumerKind.QuickPulse:
                    return AddAggregator(aggregator, _aggregatorsForQuickPulse);

                case MetricConsumerKind.Custom:
                    return AddAggregator(aggregator, _aggregatorsForCustom);

                default:
                    throw new ArgumentException($"Unexpected value of {nameof(consumerKind)}: {consumerKind}.");
            }
        }

        private bool AddAggregator(IMetricDataSeriesAggregator aggregator, AggregatorCollection aggregatorCollection)
        {
            if (aggregatorCollection == null)
            {
                return false;
            }

            IMetricValueFilter valueFilter = null;
            if (aggregatorCollection.Filter != null && ! aggregatorCollection.Filter.WillConsume(aggregator.DataSeries, out valueFilter))
            {
                return false;
            }

            aggregator.Initialize(aggregatorCollection.PeriodStart, valueFilter);
            aggregatorCollection.Aggregators.Add(aggregator);

            return true;
        }

        public AggregationPeriodSummary CycleAggregators(MetricConsumerKind consumerKind, DateTimeOffset tactTimestamp, IMetricDataSeriesFilter updatedFilter)
        {
            switch (consumerKind)
            {
                case MetricConsumerKind.Default:
                    if (updatedFilter != null)
                    {
                        throw new ArgumentException($"Cannot specify non-null {nameof(updatedFilter)} when {nameof(consumerKind)} is {consumerKind}.");
                    }
                    return CycleAggregators(ref _aggregatorsForDefault, tactTimestamp, updatedFilter, stopAggregators: false);

                case MetricConsumerKind.QuickPulse:
                    return CycleAggregators(ref _aggregatorsForQuickPulse, tactTimestamp, updatedFilter, stopAggregators: false);

                case MetricConsumerKind.Custom:
                    return CycleAggregators(ref _aggregatorsForCustom, tactTimestamp, updatedFilter, stopAggregators: false);

                default:
                    throw new ArgumentException($"Unexpected value of {nameof(consumerKind)}: {consumerKind}.");
            }
        }

        private AggregationPeriodSummary CycleAggregators(ref AggregatorCollection aggregators, DateTimeOffset tactTimestamp, IMetricDataSeriesFilter updatedFilter, bool stopAggregators)
        {
            // For non-persistent aggregators: create empty holder for the next aggregation period and swap for the previous holder:
            AggregatorCollection nextAggregators = stopAggregators
                                                            ? null
                                                            : new AggregatorCollection(tactTimestamp, updatedFilter);

            AggregatorCollection prevAggregators = Interlocked.Exchange(ref aggregators, nextAggregators);
            IMetricDataSeriesFilter prevFilter = prevAggregators.Filter;

            // Complete each persistent aggregator:
            // The Enumerator of GrowingCollection is a thread-safe lock-free implementation that operates on a "snapshot" of a collection taken at the
            // time when the enumerator is created. We expand the foreach statement (like the compiler normally does) so that we can use the typed
            // enumerator's Count property which is constsent with the data in the snapshot.
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
                                               (prevFilter.WillConsume(aggregator.DataSeries, out unusedValueFilter));
                        if (satisfiesFilter)
                        {
                            ITelemetry aggregate = aggregator.CompleteAggregation(tactTimestamp);
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
                    ITelemetry aggregate = aggregator.CompleteAggregation(tactTimestamp);
                    filteredAggregations.Add(aggregate);
                }
            }

            var summary = new AggregationPeriodSummary(unfilteredValsAggregations, filteredAggregations);
            return summary;
        }
    }
}
