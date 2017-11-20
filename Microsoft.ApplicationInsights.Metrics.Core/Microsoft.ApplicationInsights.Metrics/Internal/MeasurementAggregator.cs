using System;
using System.Runtime.CompilerServices;
using Microsoft.ApplicationInsights.Metrics.Extensibility;
using System.Threading;

namespace Microsoft.ApplicationInsights.Metrics
{
    internal sealed class MeasurementAggregator : MetricSeriesAggregatorBase<double>
    {
        private class Data
        {
            public int Count;
            public double Min;
            public double Max;
            public double Sum;
            public double SumOfSquares;
        }

        private static readonly Func<MetricValuesBufferBase<double>> MetricValuesBufferFactory = () => new MetricValuesBuffer_Double(capacity: 500);

        private readonly object _updateLock = new object();

        private readonly bool _restrictToUInt32Values;

        private readonly Data _data = new Data();


        public MeasurementAggregator(MetricSeriesConfigurationForMeasurement configuration, MetricSeries dataSeries, MetricAggregationCycleKind aggregationCycleKind)
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
            if (metricValue == null)
            {
                return Double.NaN;
            }

            double value = Util.ConvertToDoubleValue(metricValue);
            return ConvertMetricValue(value);
        }

        protected override MetricAggregate CreateAggregate(DateTimeOffset periodEnd)
        {
            int count;
            double sum, min, max, stdDev;

            lock (_updateLock)
            {
                count = _data.Count;
                sum = _data.Sum;
                min = 0.0;
                max = 0.0;
                stdDev = 0.0;

                if (count > 0)
                {
                    min = _data.Min;
                    max = _data.Max;

                    if (Double.IsInfinity(_data.SumOfSquares) || Double.IsInfinity(sum))
                    {
                        stdDev = Double.NaN;
                    }
                    else
                    {
                        double mean = sum / count;
                        double variance = (_data.SumOfSquares / count) - (mean * mean);
                        stdDev = Math.Sqrt(variance);
                    }
                }
            }

            sum = Util.EnsureConcreteValue(sum);

            if (count > 0)
            {
                min = Util.EnsureConcreteValue(min);
                max = Util.EnsureConcreteValue(max);
                stdDev = Util.EnsureConcreteValue(stdDev);
            }

            MetricAggregate aggregate = new MetricAggregate(
                                                DataSeries?.MetricId ?? Util.NullString,
                                                MetricAggregateKinds.SimpleStatistics.Moniker);

            aggregate.AggregateData[MetricAggregateKinds.SimpleStatistics.DataKeys.Count] = count;
            aggregate.AggregateData[MetricAggregateKinds.SimpleStatistics.DataKeys.Sum] = sum;
            aggregate.AggregateData[MetricAggregateKinds.SimpleStatistics.DataKeys.Min] = min;
            aggregate.AggregateData[MetricAggregateKinds.SimpleStatistics.DataKeys.Max] = max;
            aggregate.AggregateData[MetricAggregateKinds.SimpleStatistics.DataKeys.StdDev] = stdDev;

            AddInfo_Timing_Dimensions_Context(aggregate, periodEnd);

            return aggregate;
        }

        protected override void ResetAggregate()
        {
            lock (_updateLock)
            {
                _data.Count = 0;
                _data.Min = Double.MaxValue;
                _data.Max = Double.MinValue;
                _data.Sum = 0.0;
                _data.SumOfSquares = 0.0;
            }
        }

        protected override object UpdateAggregate_Stage1(MetricValuesBufferBase<double> buffer, int minFlushIndex, int maxFlushIndex)
        {
            Data bufferData = new Data();
            bufferData.Count = 0;
            bufferData.Min = Double.MaxValue;
            bufferData.Max = Double.MinValue;
            bufferData.Sum = 0.0;
            bufferData.SumOfSquares = 0.0;

            for (int index = minFlushIndex; index <= maxFlushIndex; index++)
            {
                double metricValue = buffer.GetAndResetValue(index);

                if (Double.IsNaN(metricValue))
                {
                    continue;
                }

                bufferData.Count++;
                bufferData.Max = (metricValue > bufferData.Max) ? metricValue : bufferData.Max;
                bufferData.Min = (metricValue < bufferData.Min) ? metricValue : bufferData.Min;
                bufferData.Sum += metricValue;
                bufferData.SumOfSquares += (metricValue * metricValue);
            }

            if (_restrictToUInt32Values)
            {
                bufferData.Max = Math.Round(bufferData.Max);
                bufferData.Min = Math.Round(bufferData.Min);
            }

            return bufferData;
        }

        protected override void UpdateAggregate_Stage2(object stage1Result)
        {
            Data bufferData = (Data) stage1Result;

            if (bufferData.Count == 0)
            {
                return;
            }

            // Take a lock and update the aggregate:
            lock (_updateLock)
            {
                _data.Count += bufferData.Count;
                _data.Max = (bufferData.Max > _data.Max) ? bufferData.Max : _data.Max;
                _data.Min = (bufferData.Min < _data.Min) ? bufferData.Min : _data.Min;
                _data.Sum += bufferData.Sum;
                _data.SumOfSquares += bufferData.SumOfSquares;

                if (_restrictToUInt32Values)
                {
                    _data.Sum = Math.Round(_data.Sum);
                    _data.SumOfSquares = Math.Round(_data.SumOfSquares);
                }
            }
        }
    }
}
