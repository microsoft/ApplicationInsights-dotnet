using System;
using System.Runtime.CompilerServices;
using Microsoft.ApplicationInsights.Metrics.Extensibility;

using Util = Microsoft.ApplicationInsights.Metrics.Extensions.Util;

namespace Microsoft.ApplicationInsights.Metrics
{
    internal sealed class AccumulatorAggregator : MetricSeriesAggregatorBase<double>
    {
        private static readonly Func<MetricValuesBufferBase<double>> MetricValuesBufferFactory = () => new MetricValuesBuffer_Double(capacity: 500);

        private readonly object _updateLock = new object();

        private readonly bool _restrictToUInt32Values;

        private double _min;
        private double _max;
        private double _sum;

        public AccumulatorAggregator(MetricSeriesConfigurationForAccumulator configuration, MetricSeries dataSeries, MetricAggregationCycleKind aggregationCycleKind)
            : base(MetricValuesBufferFactory, configuration, dataSeries, aggregationCycleKind)
        {
            Util.ValidateNotNull(configuration, nameof(configuration));

            _restrictToUInt32Values = configuration.RestrictToUInt32Values;
            ResetAggregate();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override double ConvertMetricValue(double metricValue)
        {
            if (_restrictToUInt32Values)
            {
                return Util.RoundAndValidateValue(metricValue);
            }
            else
            {
                return metricValue;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override double ConvertMetricValue(object metricValue)
        {
            double value = Util.ConvertToDoubleValue(metricValue);
            return ConvertMetricValue(value);
        }

        protected override MetricAggregate CreateAggregate(DateTimeOffset periodEnd)
        {
            double sum, min, max;

            lock (_updateLock)
            {
                sum = _sum;
                min = _min;
                max = _max;
            }

            if (_min > _max)
            {
                return null;
            }

            sum = Util.EnsureConcreteValue(sum);
            min = Util.EnsureConcreteValue(min);
            max = Util.EnsureConcreteValue(max);

            MetricAggregate aggregate = new MetricAggregate(
                                                DataSeries?.MetricIdentifier.MetricNamespace ?? Util.NullString,
                                                DataSeries?.MetricIdentifier.MetricId ?? Util.NullString,
                                                MetricConfigurations.Common.Accumulator().Constants().AggregateKindMoniker);

            aggregate.Data[MetricConfigurations.Common.Accumulator().Constants().AggregateKindDataKeys.Sum] = sum;
            aggregate.Data[MetricConfigurations.Common.Accumulator().Constants().AggregateKindDataKeys.Min] = min;
            aggregate.Data[MetricConfigurations.Common.Accumulator().Constants().AggregateKindDataKeys.Max] = max;

            AddInfo_Timing_Dimensions_Context(aggregate, periodEnd);

            return aggregate;
        }

        protected override void ResetAggregate()
        {
            lock (_updateLock)
            {
                _min = Double.MaxValue;
                _max = Double.MinValue;
                _sum = 0.0;
            }
        }

        protected override object UpdateAggregate_Stage1(MetricValuesBufferBase<double> buffer, int minFlushIndex, int maxFlushIndex)
        {
            lock (_updateLock)
            {
                for (int index = minFlushIndex; index <= maxFlushIndex; index++)
                {
                    double metricValue = buffer.GetAndResetValue(index);

                    if (Double.IsNaN(metricValue))
                    {
                        continue;
                    }

                    _sum += metricValue;
                    _max = (_sum > _max) ? _sum : _max;
                    _min = (_sum < _min) ? _sum : _min;
                }

                if (_restrictToUInt32Values)
                {
                    _sum = Math.Round(_sum);
                    _max = Math.Round(_max);
                    _min = Math.Round(_min);
                }
            }

            return null;
        }

        protected override void UpdateAggregate_Stage2(object stage1Result)
        {
        }
    }
}
