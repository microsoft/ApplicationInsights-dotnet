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
    /// Counts the number of distinct values tracked using this aggregator;
    /// produces aggregates where Sum = the number of distinct values tracked during the aggregation period,
    /// and Count = total number of tracked values (Man, Max and StdDev are always zero).
    /// 
    /// !! This aggregator is not intended for general production systems !!
    /// It uses memory inefficiently by keeping a concurrent dictionary of all unique values seen during the ongoing
    /// aggregation period.
    /// Moreover, aggregates produced by this aggregator cannot be combined across multiple application instances.
    /// Therefore, this aggregator should only be used in single-instance-applications 
    /// and for metrics where the number of distinct values is relatively small.
    /// 
    /// The primary purpose of this aggregator is to validate API usage scenarios where object values are tracked by a
    /// metric series (rather than numeric values).
    /// In unique count / distinct count scenarios, the most common values tracked are strings. This aggregator will
    /// process any object, but it will convert it to a string (using .ToString()) before tracking. Numbers are also
    /// converted to strings in this manner. Nulls are tracked using the string <c>"null"</c>.
    /// </summary>
    internal class NaiveDistinctCountMetricSeriesAggregator : DataSeriesAggregatorBase, IMetricSeriesAggregator
    {
        private readonly bool _caseSensitive;
        private readonly ConcurrentDictionary<string, bool> _uniqueValues = new ConcurrentDictionary<string, bool>();
        private int _totalValuesCount = 0;

        public NaiveDistinctCountMetricSeriesAggregator(
                                    NaiveDistinctCountMetricSeriesConfiguration configuration,
                                    MetricSeries dataSeries,
                                    MetricAggregationCycleKind aggregationCycleKind)
            : base(configuration, dataSeries, aggregationCycleKind)
        {
            _caseSensitive = configuration.IsCaseSensitiveDistinctions;
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

            if (! _caseSensitive)
            {
                metricValue = metricValue.ToLowerInvariant();
            }
            
            _uniqueValues.TryAdd(metricValue, true);
            Interlocked.Increment(ref _totalValuesCount);
        }
    }
}