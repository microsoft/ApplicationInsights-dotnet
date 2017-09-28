using System;
using System.Collections.Generic;

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Metrics.Extensibility;
using System.Collections.Concurrent;
using System.Threading;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary>
    /// This class is intended for internal verification only.
    /// It computes distinct counts in a manner that is very resource intensive and its aggregates cannot be combined across machines.
    /// </summary>
    internal class NaiveDistinctCountMetricSeriesAggregator : DataSeriesAggregatorBase, IMetricSeriesAggregator
    {
        private readonly ConcurrentDictionary<string, bool> _uniqueValues = new ConcurrentDictionary<string, bool>();
        private int _totalValuesCount = 0;

        public NaiveDistinctCountMetricSeriesAggregator(NaiveDistinctCountMetricSeriesConfiguration configuration, MetricSeries dataSeries, MetricConsumerKind consumerKind)
            : base(configuration, dataSeries, consumerKind)
        {
        }

        public override ITelemetry CreateAggregateUnsafe(DateTimeOffset periodEnd)
        {
            int uniqueValuesCount = _uniqueValues.Count;
            int totalValuesCount = Volatile.Read(ref _totalValuesCount);

            MetricTelemetry aggregate = new MetricTelemetry(
                                                    name: DataSeries?.MetricId ?? Util.NullString,
                                                    count: totalValuesCount,
                                                    sum: uniqueValuesCount,
                                                    min: 0,
                                                    max: 0,
                                                    standardDeviation: 0);

            StampTimingInfo(aggregate, periodEnd);
            StampVersionAndContextInfo(aggregate);

            return aggregate;
        }


        protected override void ReinitializeAggregation()
        {
            _uniqueValues.Clear();
            Volatile.Write(ref _totalValuesCount, 0);
        }

        protected override void TrackFilteredValue(double metricValue)
        {
            TrackFilteredValue(metricValue.ToString());
        }

        protected override void TrackFilteredValue(object metricValue)
        {
            if (metricValue == null)
            {
                TrackFilteredValue((string) null);
            }
            else
            {
                string stringValue = metricValue as string;
                TrackFilteredValue(stringValue ?? metricValue.ToString());
            }
        }

        private void TrackFilteredValue(string metricValue)
        {
            if (metricValue == null)
            {
                metricValue = Util.NullString;
            }
            else
            {
                metricValue = metricValue.Trim();
            }
            
            _uniqueValues.TryAdd(metricValue, true);
            Interlocked.Increment(ref _totalValuesCount);
        }
    }
}