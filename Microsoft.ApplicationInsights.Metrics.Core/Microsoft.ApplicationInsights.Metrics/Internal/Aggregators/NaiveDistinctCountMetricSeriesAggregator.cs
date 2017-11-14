using System;
using System.Collections.Generic;
using System.Threading;

using Microsoft.ApplicationInsights.Metrics.Extensibility;
using System.Runtime.CompilerServices;

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
    internal class NaiveDistinctCountMetricSeriesAggregator : MetricSeriesAggregatorBase<object>
    {
        private static readonly Func<MetricValuesBufferBase<object>> MetricValuesBufferFactory = () => new MetricValuesBuffer_Object(capacity: 500);

        private readonly bool _caseSensitive;

        private readonly object _updateLock = new object();

        private readonly HashSet<string> _uniqueValues = new HashSet<string>();
        private int _totalValuesCount = 0;

        public NaiveDistinctCountMetricSeriesAggregator(
                                    NaiveDistinctCountMetricSeriesConfiguration configuration,
                                    MetricSeries dataSeries,
                                    MetricAggregationCycleKind aggregationCycleKind)
            : base(MetricValuesBufferFactory, configuration, dataSeries, aggregationCycleKind)
        {
            _caseSensitive = configuration.IsCaseSensitiveDistinctions;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override object ConvertMetricValue(double metricValue)
        {
            return metricValue.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override object ConvertMetricValue(object metricValue)
        {
            if (metricValue == null)
            {
                return Util.NullString;
            }

            string stringValue = metricValue as string;

            if (stringValue == null)
            {
                stringValue = metricValue.ToString();
            }

            return stringValue;
        }

        protected override MetricAggregate CreateAggregate(DateTimeOffset periodEnd)
        {
            int uniqueValuesCount, totalValuesCount;
            lock (_updateLock)
            {
                uniqueValuesCount = _uniqueValues.Count;
                totalValuesCount = _totalValuesCount;
            }

            MetricAggregate aggregate = new MetricAggregate(
                                                DataSeries?.MetricId ?? Util.NullString,
                                                MetricAggregateKinds.NaiveDistinctCount.Moniker);

            aggregate.AggregateData[MetricAggregateKinds.NaiveDistinctCount.DataKeys.TotalCount] = totalValuesCount;
            aggregate.AggregateData[MetricAggregateKinds.NaiveDistinctCount.DataKeys.DistinctCount] = uniqueValuesCount;

            AddInfo_Timing_Dimensions_Context(aggregate, periodEnd);

            return aggregate;
        }

        protected override void ResetAggregate()
        {
            lock (_updateLock)
            {
                _uniqueValues.Clear();
                _totalValuesCount = 0;
            }
        }

        protected override object UpdateAggregate_Stage1(MetricValuesBufferBase<object> buffer, int minFlushIndex, int maxFlushIndex)
        {
            lock (_updateLock)
            {
                for (int index = minFlushIndex; index <= maxFlushIndex; index++)
                {
                    object metricValue = buffer.GetAndResetValue(index);

                    if (metricValue == null)
                    {
                        continue;
                    }

                    string stringValue = metricValue.ToString();

                    if (!_caseSensitive)
                    {
                        stringValue = stringValue.ToLowerInvariant();
                    }

                    _uniqueValues.Add(stringValue);
                    Interlocked.Increment(ref _totalValuesCount);
                }
            }

            return null;
        }

        protected override void UpdateAggregate_Stage2(object stage1Result)
        {
        }
    }
}