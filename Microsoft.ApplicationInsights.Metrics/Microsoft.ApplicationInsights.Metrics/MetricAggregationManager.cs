using Microsoft.ApplicationInsights.Channel;
using System;
using System.Collections.Generic;
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
            public IMetricAggregationFilter Filter { get; }

            public MetricAggregatorCollection(DateTimeOffset periodStart, IMetricAggregationFilter filter)
            {
                this.PeriodStart = periodStart;
                this.PeriodEnd = DateTimeOffset.MinValue;
                this.Aggregators = new GrowingCollection<IMetricDataSeriesAggregator>();
                this.Filter = Filter;
            }
        }

        public enum MetricConsumerKind
        {
            Default,
            QuickPulse,
            Custom
        }

        public class AggregationPeriodSummary
        {

        }


        private MetricAggregatorCollection _aggregatorsPersistent = null;
        private MetricAggregatorCollection _aggregatorsForDefault = null;
        private MetricAggregatorCollection _aggregatorsForQuickPulse= null;
        private MetricAggregatorCollection _aggregatorsForCustom = null;

        public bool StartAggregators(MetricConsumerKind consumerKind, IMetricAggregationFilter filter, DateTimeOffset tactTimestamp)
        {
            switch (consumerKind)
            {
                case MetricConsumerKind.QuickPulse:
                    if (_aggregatorsForQuickPulse == null)
                    {
                        MetricAggregatorCollection prev = Interlocked.CompareExchange(ref _aggregatorsForQuickPulse, new MetricAggregatorCollection(tactTimestamp, filter), null);
                        return (prev == null);
                    }
                    else
                    {
                        return false;
                    }

                case MetricConsumerKind.Custom:
                    if (_aggregatorsForQuickPulse == null)
                    {
                        MetricAggregatorCollection prev = Interlocked.CompareExchange(ref _aggregatorsForCustom, new MetricAggregatorCollection(tactTimestamp, filter), null);
                        return (prev == null);
                    }
                    else
                    {
                        return false;
                    }

                case MetricConsumerKind.Default:
                    throw new ArgumentException($"Cannot invoke {nameof(StartAggregators)} for default consumerKind: Default aggregators are always active.");

                default:
                    throw new ArgumentException($"Unexpected value of {nameof(consumerKind)}: {consumerKind}.");
            }

        }


        public void StopAggregators(MetricConsumerKind consumerKind)
        {

        }

        private ref MetricAggregatorCollection PickAggregatorCollection(MetricConsumerKind consumerKind)
        {
            switch (consumerKind)
            {
                case MetricConsumerKind.Default:
                    return ref _aggregatorsForDefault;

                case MetricConsumerKind.QuickPulse:
                    return ref _aggregatorsForQuickPulse;

                case MetricConsumerKind.Custom:
                    return ref _aggregatorsForCustom;

                default:
                    throw new ArgumentException($"Unexpected value of {nameof(consumerKind)}: {consumerKind}.");
            }
        }

        public AggregationPeriodSummary CycleAggregators(MetricConsumerKind consumerKind, IMetricAggregationFilter updatedFilter, DateTimeOffset tactTimestamp)
        {
            ref MetricAggregatorCollection aggregatorsToReplace = ref PickAggregatorCollection(consumerKind);

            // For non-persistent aggregators: create empty holder for the next aggregation period and swap for the previous holder:
            MetricAggregatorCollection nextAggregators = new MetricAggregatorCollection(tactTimestamp, updatedFilter);
            MetricAggregatorCollection prevAggregators = Interlocked.Exchange(ref aggregatorsToReplace, nextAggregators);

            // Complete each persistent aggregator:
            // (We expand the foreach statement so that we can use the typed enumerators Count property which is constsent with the data in the snapshot.)
            GrowingCollection<IMetricDataSeriesAggregator>.Enumerator unfilretedAggregators = _aggregatorsPersistent.Aggregators.GetEnumerator(); 
            List<ITelemetry> unfilteredAggregations = new List<ITelemetry>(capacity: unfilretedAggregators.Count);
            try
            {
                while(unfilretedAggregators.MoveNext())
                {
                    IMetricDataSeriesAggregator aggregator = unfilretedAggregators.Current;
                    if (aggregator != null)
                    {
                        ITelemetry aggregate = aggregator.CompleteAggregationPeriod(tactTimestamp);
                        unfilteredAggregations.Add(aggregate);
                    }
                }
            }
            finally
            {
                unfilretedAggregators.Dispose();
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

            var summary = new AggregationPeriodSummary(unfilretedAggregators, filteredAggregations);
            return summary;
        }

    }
}
