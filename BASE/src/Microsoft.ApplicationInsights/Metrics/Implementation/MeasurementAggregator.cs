namespace Microsoft.ApplicationInsights.Metrics
{
    using System;
    using System.Runtime.CompilerServices;
    using Microsoft.ApplicationInsights.Metrics.Extensibility;

    internal sealed class MeasurementAggregator : MetricSeriesAggregatorBase<double>, IMetricSeriesAggregator
    {
        private static readonly Func<MetricValuesBufferBase<double>> MetricValuesBufferFactory = () => new MetricValuesBuffer_Double(capacity: 500);

        private readonly object updateLock = new object();

        private readonly bool restrictToUInt32Values;

        private readonly Data data;

        public MeasurementAggregator(MetricSeriesConfigurationForMeasurement configuration, MetricSeries dataSeries, MetricAggregationCycleKind aggregationCycleKind)
            : base(MetricValuesBufferFactory, configuration, dataSeries, aggregationCycleKind)
        {
            Util.ValidateNotNull(configuration, nameof(configuration));

            this.restrictToUInt32Values = configuration.RestrictToUInt32Values;
            this.data = new Data(this.restrictToUInt32Values);
            this.ResetAggregate();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override double ConvertMetricValue(double metricValue)
        {
            if (this.restrictToUInt32Values)
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
            return this.ConvertMetricValue(value);
        }

        protected override MetricAggregate CreateAggregate(DateTimeOffset periodEnd)
        {
            int count;
            double sum, min, max, stdDev;

            lock (this.updateLock)
            {
                count = this.data.Count;
                sum = this.data.Sum;
                min = 0.0;
                max = 0.0;
                stdDev = 0.0;

                if (count > 0)
                {
                    min = this.data.Min;
                    max = this.data.Max;

                    if (Double.IsInfinity(this.data.SumOfSquares) || Double.IsInfinity(sum))
                    {
                        stdDev = Double.NaN;
                    }
                    else
                    {
                        double mean = sum / count;
                        double variance = (this.data.SumOfSquares / count) - (mean * mean);
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
                                                this.DataSeries?.MetricIdentifier.MetricNamespace ?? String.Empty,
                                                this.DataSeries?.MetricIdentifier.MetricId ?? Util.NullString,
                                                MetricSeriesConfigurationForMeasurement.Constants.AggregateKindMoniker);

            aggregate.Data[MetricSeriesConfigurationForMeasurement.Constants.AggregateKindDataKeys.Count] = count;
            aggregate.Data[MetricSeriesConfigurationForMeasurement.Constants.AggregateKindDataKeys.Sum] = sum;
            aggregate.Data[MetricSeriesConfigurationForMeasurement.Constants.AggregateKindDataKeys.Min] = min;
            aggregate.Data[MetricSeriesConfigurationForMeasurement.Constants.AggregateKindDataKeys.Max] = max;
            aggregate.Data[MetricSeriesConfigurationForMeasurement.Constants.AggregateKindDataKeys.StdDev] = stdDev;

            this.AddInfo_Timing_Dimensions_Context(aggregate, periodEnd);

            return aggregate;
        }

        protected override void ResetAggregate()
        {
            lock (this.updateLock)
            {
                this.data?.ResetAggregate();
            }
        }

        protected override void UpdateAggregate(MetricValuesBufferBase<double> buffer, int minFlushIndex, int maxFlushIndex)
        {
            Data bufferData = new Data(this.restrictToUInt32Values);

            for (int index = minFlushIndex; index <= maxFlushIndex; index++)
            {
                double metricValue = buffer.GetAndResetValue(index);

                bufferData.UpdateAggregate(metricValue);
            }

            // Take a lock and update the aggregate:
            lock (this.updateLock)
            {
                this.data.UpdateAggregate(bufferData);
            }
        }

        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062", Justification = "buffer is validated by base")]
        //protected override object UpdateAggregate_Stage1(MetricValuesBufferBase<double> buffer, int minFlushIndex, int maxFlushIndex)
        //{
        //    Data bufferData = new Data(this.restrictToUInt32Values);

        //    for (int index = minFlushIndex; index <= maxFlushIndex; index++)
        //    {
        //        double metricValue = buffer.GetAndResetValue(index);

        //        bufferData.UpdateAggregate(metricValue);
        //    }

        //    return bufferData;
        //}

        //protected override void UpdateAggregate_Stage2(object stage1Result)
        //{
        //    Data bufferData = (Data)stage1Result;

        //    // Take a lock and update the aggregate:
        //    lock (this.updateLock)
        //    {
        //        this.data.UpdateAggregate(bufferData);
        //    }
        //}
    }
}
