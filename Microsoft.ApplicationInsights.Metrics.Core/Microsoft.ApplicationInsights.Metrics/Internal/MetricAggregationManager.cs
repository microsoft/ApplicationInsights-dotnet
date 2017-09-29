using System;
using System.Collections.Generic;
using System.Threading;

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Metrics.Extensibility;

using CycleKind = Microsoft.ApplicationInsights.Metrics.Extensibility.MetricAggregationCycleKind;

namespace Microsoft.ApplicationInsights.Metrics
{
    internal class MetricAggregationManager
    {
        // We support 4 aggregation cycles. 2 of them can be accessed from the outside:

        private AggregatorCollection _aggregatorsForDefaultPersistent = null;
        private AggregatorCollection _aggregatorsForDefault = null;
        private AggregatorCollection _aggregatorsForQuickPulse = null;
        private AggregatorCollection _aggregatorsForCustom = null;

        internal MetricAggregationManager()
        {
            DateTimeOffset now = DateTimeOffset.Now;
            DateTimeOffset timestamp = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, 0, now.Offset);

            _aggregatorsForDefault = new AggregatorCollection(timestamp, filter: null);
            _aggregatorsForDefaultPersistent = new AggregatorCollection(timestamp, filter: null);
        }

        public bool StartAggregators(MetricAggregationCycleKind aggregationCycleKind, DateTimeOffset tactTimestamp, IMetricSeriesFilter filter)
        {
            switch (aggregationCycleKind)
            {
                case CycleKind.QuickPulse:
                    AggregatorCollection prevQP = Interlocked.CompareExchange(ref _aggregatorsForQuickPulse, new AggregatorCollection(tactTimestamp, filter), comparand: null);
                    return (prevQP == null);

                case CycleKind.Custom:
                    AggregatorCollection prevC = Interlocked.CompareExchange(ref _aggregatorsForCustom, new AggregatorCollection(tactTimestamp, filter), comparand: null);
                    return (prevC == null);

                case CycleKind.Default:
                    throw new ArgumentException($"Cannot invoke {nameof(StartAggregators)} for Default {nameof(MetricAggregationCycleKind)}: Default aggregators are always active.");

                default:
                    throw new ArgumentException($"Unexpected value of {nameof(aggregationCycleKind)}: {aggregationCycleKind}.");
            }
        }
        
        public AggregationPeriodSummary CycleAggregators(MetricAggregationCycleKind aggregationCycleKind, DateTimeOffset tactTimestamp, IMetricSeriesFilter updatedFilter)
        {
            switch (aggregationCycleKind)
            {
                case CycleKind.Default:
                    if (updatedFilter != null)
                    {
                        throw new ArgumentException($"Cannot specify non-null {nameof(updatedFilter)} when {nameof(aggregationCycleKind)} is {aggregationCycleKind}.");
                    }

                    return CycleAggregators(ref _aggregatorsForDefault, tactTimestamp, updatedFilter, stopAggregators: false);

                case CycleKind.QuickPulse:
                    return CycleAggregators(ref _aggregatorsForQuickPulse, tactTimestamp, updatedFilter, stopAggregators: false);

                case CycleKind.Custom:
                    return CycleAggregators(ref _aggregatorsForCustom, tactTimestamp, updatedFilter, stopAggregators: false);

                default:
                    throw new ArgumentException($"Unexpected value of {nameof(aggregationCycleKind)}: {aggregationCycleKind}.");
            }
        }

        public AggregationPeriodSummary StopAggregators(MetricAggregationCycleKind aggregationCycleKind, DateTimeOffset tactTimestamp)
        {
            switch (aggregationCycleKind)
            {
                case CycleKind.QuickPulse:
                    return CycleAggregators(ref _aggregatorsForQuickPulse, tactTimestamp, updatedFilter: null, stopAggregators: true);

                case CycleKind.Custom:
                    return CycleAggregators(ref _aggregatorsForCustom, tactTimestamp, updatedFilter: null, stopAggregators: true);

                case CycleKind.Default:
                    throw new ArgumentException($"Cannot invoke {nameof(StopAggregators)} for Default {nameof(MetricAggregationCycleKind)}: Default aggregators are always active.");

                default:
                    throw new ArgumentException($"Unexpected value of {nameof(aggregationCycleKind)}: {aggregationCycleKind}.");
            }
        }

        internal bool IsConsumerActive(MetricAggregationCycleKind aggregationCycleKind, out IMetricSeriesFilter filter)
        {
            switch (aggregationCycleKind)
            {
                case CycleKind.Default:
                    filter = null;
                    return true;

                case CycleKind.QuickPulse:
                case CycleKind.Custom:
                    AggregatorCollection aggs = (aggregationCycleKind == CycleKind.QuickPulse) ? _aggregatorsForQuickPulse : _aggregatorsForCustom;
                    filter = aggs?.Filter;
                    return (aggs != null);

                default:
                    throw new ArgumentException($"Unexpected value of {nameof(aggregationCycleKind)}: {aggregationCycleKind}.");
            }
        }

        internal bool AddAggregator(IMetricSeriesAggregator aggregator, MetricAggregationCycleKind aggregationCycleKind)
        {
            Util.ValidateNotNull(aggregator, nameof(aggregator));

            if (aggregator.DataSeries._configuration.RequiresPersistentAggregation)
            {
                return AddAggregator(aggregator, _aggregatorsForDefaultPersistent);
            }

            switch (aggregationCycleKind)
            {
                case CycleKind.Default:
                    return AddAggregator(aggregator, _aggregatorsForDefault);

                case CycleKind.QuickPulse:
                    return AddAggregator(aggregator, _aggregatorsForQuickPulse);

                case CycleKind.Custom:
                    return AddAggregator(aggregator, _aggregatorsForCustom);

                default:
                    throw new ArgumentException($"Unexpected value of {nameof(aggregationCycleKind)}: {aggregationCycleKind}.");
            }
        }

        private static bool AddAggregator(IMetricSeriesAggregator aggregator, AggregatorCollection aggregatorCollection)
        {
            if (aggregatorCollection == null)
            {
                return false;
            }

            IMetricSeriesFilter seriesFilter = aggregatorCollection.Filter;
            IMetricValueFilter valueFilter = null;
            if (seriesFilter != null && !seriesFilter.WillConsume(aggregator.DataSeries, out valueFilter))
            {
                return false;
            }

            aggregator.Reset(aggregatorCollection.PeriodStart, valueFilter);
            aggregatorCollection.Aggregators.Add(aggregator);

            return true;
        }

        private AggregationPeriodSummary CycleAggregators(ref AggregatorCollection aggregators, DateTimeOffset tactTimestamp, IMetricSeriesFilter updatedFilter, bool stopAggregators)
        {
            // For non-persistent aggregators: create empty holder for the next aggregation period and swap for the previous holder:
            AggregatorCollection nextAggregators = stopAggregators
                                                            ? null
                                                            : new AggregatorCollection(tactTimestamp, updatedFilter);

            AggregatorCollection prevAggregators = Interlocked.Exchange(ref aggregators, nextAggregators);
            IMetricSeriesFilter prevFilter = prevAggregators.Filter;

            // Complete each persistent aggregator:
            // The Enumerator of GrowingCollection is a thread-safe lock-free implementation that operates on a "snapshot" of a collection taken at the
            // time when the enumerator is created. We expand the foreach statement (like the compiler normally does) so that we can use the typed
            // enumerator's Count property which is constsent with the data in the snapshot.
            GrowingCollection<IMetricSeriesAggregator>.Enumerator persistentValsAggregators = _aggregatorsForDefaultPersistent.Aggregators.GetEnumerator(); 
            List<ITelemetry> persistentValsAggregations = new List<ITelemetry>(capacity: persistentValsAggregators.Count);
            try
            {
                while (persistentValsAggregators.MoveNext())
                {
                    IMetricSeriesAggregator aggregator = persistentValsAggregators.Current;
                    if (aggregator != null)
                    {
                        // Persistent aggregators are always active, regardless of filters for a particular consumer. But we can apply the consumer's filters to determine
                        // whether or not to pull the aggregator for a aggregate at this time. Of course, only series filters, not value filters, can be considered.
                        IMetricValueFilter unusedValueFilter;
                        bool satisfiesFilter = (prevFilter == null)
                                                ||
                                               (prevFilter.WillConsume(aggregator.DataSeries, out unusedValueFilter));
                        if (satisfiesFilter)
                        {
                            ITelemetry aggregate = aggregator.CompleteAggregation(tactTimestamp);
                            persistentValsAggregations.Add(aggregate);
                        }
                    }
                }
            }
            finally
            {
                persistentValsAggregators.Dispose();
            }

            // Complete each non-persistent aggregator:
            // (we snapshotted the entire collection, so Count is stable)
            List<ITelemetry> nonpersistentAggregations = new List<ITelemetry>(capacity: prevAggregators.Aggregators.Count);
            foreach (IMetricSeriesAggregator aggregator in prevAggregators.Aggregators)
            {
                if (aggregator != null)
                {
                    ITelemetry aggregate = aggregator.CompleteAggregation(tactTimestamp);
                    nonpersistentAggregations.Add(aggregate);
                }
            }

            var summary = new AggregationPeriodSummary(persistentValsAggregations, nonpersistentAggregations);
            return summary;
        }

        #region class AggregatorCollection

        private class AggregatorCollection
        {
            public AggregatorCollection(DateTimeOffset periodStart, IMetricSeriesFilter filter)
            {
                this.PeriodStart = periodStart;
                this.Aggregators = new GrowingCollection<IMetricSeriesAggregator>();
                this.Filter = Filter;
            }

            public DateTimeOffset PeriodStart { get; }

            public GrowingCollection<IMetricSeriesAggregator> Aggregators { get; }

            public IMetricSeriesFilter Filter { get; }
        }

        #endregion class AggregatorCollection
    }
}
