using System;
using System.Runtime.CompilerServices;
using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    internal sealed class GaugeAggregator : MetricSeriesAggregatorBase<double>
    {
        private class Data
        {
            public bool HasValues;
            public double Min;
            public double Max;
            public double Last;
        }

        private static readonly Func<MetricValuesBufferBase<double>> MetricValuesBufferFactory = () => new MetricValuesBuffer_Double(capacity: 500);

        private readonly object _updateLock = new object();

        private readonly bool _restrictToUInt32Values;

        private readonly Data _data = new Data();

        public GaugeAggregator(GaugeMetricSeriesConfiguration configuration, MetricSeries dataSeries, MetricAggregationCycleKind aggregationCycleKind)
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
            double last, min, max;

            lock (_updateLock)
            {
                if (!_data.HasValues)
                {
                    return null;
                }

                min = _data.Min;
                max = _data.Max;
                last = _data.Last;
            }

            last = Util.EnsureConcreteValue(last);
            min = Util.EnsureConcreteValue(min);
            max = Util.EnsureConcreteValue(max);

            MetricAggregate aggregate = new MetricAggregate(
                                                DataSeries?.MetricId ?? Util.NullString,
                                                MetricAggregateKinds.Gauge.Moniker);

            aggregate.AggregateData[MetricAggregateKinds.Gauge.DataKeys.Last] = last;
            aggregate.AggregateData[MetricAggregateKinds.Gauge.DataKeys.Min] = min;
            aggregate.AggregateData[MetricAggregateKinds.Gauge.DataKeys.Max] = max;

            AddInfo_Timing_Dimensions_Context(aggregate, periodEnd);

            return aggregate;
        }

        protected override void ResetAggregate()
        {
            lock (_updateLock)
            {
                _data.Min = Double.MaxValue;
                _data.Max = Double.MinValue;
                _data.Last = Double.NaN;
                _data.HasValues = false;
            }
        }

        protected override object UpdateAggregate_Stage1(MetricValuesBufferBase<double> buffer, int minFlushIndex, int maxFlushIndex)
        {
            // Compute a summary of the buffer:
            Data bufferData = new Data();
            bufferData.HasValues = false;
            bufferData.Min = Double.MaxValue;
            bufferData.Max = Double.MinValue;
            bufferData.Last = Double.NaN;

            for (int index = minFlushIndex; index <= maxFlushIndex; index++)
            {
                double metricValue = buffer.GetAndResetValue(index);

                if (Double.IsNaN(metricValue))
                {
                    continue;
                }

                bufferData.HasValues = true;
                bufferData.Max = (metricValue > bufferData.Max) ? metricValue : bufferData.Max;
                bufferData.Min = (metricValue < bufferData.Min) ? metricValue : bufferData.Min;
                bufferData.Last = metricValue;
            }

            return bufferData;
        }

        protected override void UpdateAggregate_Stage2(object stage1Result)
        {
            Data bufferData = (Data) stage1Result;

            if (! bufferData.HasValues)
            {
                return;
            }

            if (_restrictToUInt32Values)
            {
                bufferData.Max = Math.Round(bufferData.Max);
                bufferData.Min = Math.Round(bufferData.Min);
                bufferData.Last = Math.Round(bufferData.Last);
            }

            // Take a lock and update the aggregate:
            lock(_updateLock)
            {
                _data.HasValues = true;
                _data.Max = (bufferData.Max > _data.Max) ? bufferData.Max : _data.Max;
                _data.Min = (bufferData.Min < _data.Min) ? bufferData.Min : _data.Min;
                _data.Last = bufferData.Last;
            }
        }
    }
}
