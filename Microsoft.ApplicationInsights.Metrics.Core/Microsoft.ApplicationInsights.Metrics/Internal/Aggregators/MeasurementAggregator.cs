using System;
using Microsoft.ApplicationInsights.Metrics.Extensibility;
using System.Runtime.CompilerServices;
using System.Globalization;

namespace Microsoft.ApplicationInsights.Metrics
{
    internal class MeasurementAggregator : MetricSeriesAggregatorBase<double>
    {
        private const double MicroOne = 0.000001;

        private readonly object _updateLock = new object();

        private readonly bool _restrictToUInt32Values;

        protected int _count;
        protected double _min;
        protected double _max;
        protected double _sum;
        protected double _sumOfSquares;

        public MeasurementAggregator(IMetricSeriesConfiguration configuration, MetricSeries dataSeries, MetricAggregationCycleKind aggregationCycleKind)
            : base(configuration, dataSeries, aggregationCycleKind)
        {
            Util.ValidateNotNull(configuration, nameof(configuration));

            SimpleMetricSeriesConfiguration simpleSeriesConfig = configuration as SimpleMetricSeriesConfiguration;
            if (simpleSeriesConfig == null)
            {
                throw new ArgumentException(
                                        $"{nameof(MeasurementAggregator)} expects a configuration of type {nameof(SimpleMetricSeriesConfiguration)},"
                                      + $" however the specified configuration is {configuration?.GetType()?.FullName ?? Util.NullString}.",
                                        nameof(configuration));
            }

            _restrictToUInt32Values = simpleSeriesConfig.RestrictToUInt32Values;
            ResetAggregate();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override double ConvertMetricValue(double metricValue)
        {
            if (!_restrictToUInt32Values)
            {
                return metricValue;
            }
            else
            {
                return RoundAndValidateValue(metricValue);
            }
        }

        private double RoundAndValidateValue(double metricValue)
        {
            if (Double.IsNaN(metricValue))
            {
                throw new ArgumentException($"This aggregator cannot process the specified metric value."
                                          + $" The aggregator expects metric values of type {nameof(UInt32)}, but the specified {nameof(metricValue)} is Double.NaN."
                                          + $" Have you specified the correct metric configuration?");
            }

            if (metricValue < -MicroOne)
            {
                throw new ArgumentException($"This aggregator cannot process the specified metric value."
                                          + $" The aggregator expects metric values of type {nameof(UInt32)}, but the specified {nameof(metricValue)} is"
                                          + $" a negative double value ({metricValue})."
                                          + $" Have you specified the correct metric configuration?");
            }

            double wholeValue = Math.Round(metricValue);

            if (wholeValue > UInt32.MaxValue)
            {
                throw new ArgumentException($"This aggregator cannot process the specified metric value."
                                             + $" The aggregator expects metric values of type {nameof(UInt32)}, but the specified {nameof(metricValue)} is"
                                             + $" larger than the maximum accepted value ({metricValue})."
                                             + $" Have you specified the correct metric configuration?");
            }

            double delta = Math.Abs(metricValue - wholeValue);
            if (delta > MicroOne)
            {
                throw new ArgumentException($"This aggregator cannot process the specified metric measurement."
                                          + $" The aggregator expects metric values of type {nameof(UInt32)}, but the specified {nameof(metricValue)} is"
                                          + $" a double value that does not equal to a whole number ({metricValue})."
                                          + $" Have you specified the correct metric configuration?");
            }

            return wholeValue;
        }

        protected override double ConvertMetricValue(object metricValue)
        {
            if (metricValue == null)
            {
                return Double.NaN;
            }

            if (metricValue is SByte)
            {
                return ConvertMetricValue((double) (SByte) metricValue);
            }
            else if (metricValue is Byte)
            {
                return ConvertMetricValue((double) (Byte) metricValue);
            }
            else if (metricValue is Int16)
            {
                return ConvertMetricValue((double) (Int16) metricValue);
            }
            else if (metricValue is UInt16)
            {
                return ConvertMetricValue((double) (UInt16) metricValue);
            }
            else if (metricValue is Int32)
            {
                return ConvertMetricValue((double) (Int32) metricValue);
            }
            else if (metricValue is UInt32)
            {
                return ConvertMetricValue((double) (UInt32) metricValue);
            }
            else if (metricValue is Int64)
            {
                return ConvertMetricValue((double) (Int64) metricValue);
            }
            else if (metricValue is UInt64)
            {
                return ConvertMetricValue((double) (UInt64) metricValue);
            }
            else if (metricValue is Single)
            {
                return ConvertMetricValue((double) (Single) metricValue);
            }
            else if (metricValue is Double)
            {
                return ConvertMetricValue((double) (Double) metricValue);
            }
            else
            {
                string stringValue = metricValue as string;
                if (stringValue != null)
                {
                    double doubleValue;
                    if (Double.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out doubleValue))
                    {
                        return ConvertMetricValue(doubleValue);
                    }
                    else
                    {
                        throw new ArgumentException($"This aggregator cannot process the specified metric value."
                                                  + $" The aggregator expects metric values of a numeric type , but the specified {nameof(metricValue)} is"
                                                  + $" a String that cannot be parsed into a number (\"{metricValue}\")."
                                                  + $" Have you specified the correct metric configuration?");
                    }
                }
                else
                {
                    throw new ArgumentException($"This aggregator cannot process the specified metric value."
                                                  + $" The aggregator expects metric values of a numeric type, but the specified {nameof(metricValue)} is"
                                                  + $" of type ${metricValue.GetType().FullName}."
                                                  + $" Have you specified the correct metric configuration?");
                }
            }
        }

        protected override MetricAggregate CreateAggregate(DateTimeOffset periodEnd)
        {
            int count;
            double sum, min, max, stdDev;

            lock (_updateLock)
            {
                count = _count;
                sum = _sum;
                min = 0.0;
                max = 0.0;
                stdDev = 0.0;

                if (count > 0)
                {
                    min = _min;
                    max = _max;

                    if (Double.IsInfinity(_sumOfSquares) || Double.IsInfinity(sum))
                    {
                        stdDev = Double.NaN;
                    }
                    else
                    {
                        double mean = sum / count;
                        double variance = (_sumOfSquares / count) - (mean * mean);
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
                _count = 0;
                _min = Double.MaxValue;
                _max = Double.MinValue;
                _sum = 0.0;
                _sumOfSquares = 0.0;
            }
        }

        protected override void UpdateAggregate(MetricValuesBuffer<double> buffer)
        {
            // Compute a summary of the buffer:
            int bufferCount = 0;
            double bufferMin = Double.MaxValue;
            double bufferMax = Double.MinValue;
            double bufferSum = 0.0;
            double bufferSumOfSquares = 0.0;

            int bufferLen = buffer.Count;
            for (int i = 0; i < bufferLen; i++)
            {
                double metricValue = buffer.Get(i);

                if (Double.IsNaN(metricValue))
                {
                    continue;
                }

                bufferCount++;
                bufferMax = (metricValue > bufferMax) ? metricValue : bufferMax;
                bufferMin = (metricValue < bufferMax) ? metricValue : bufferMin;
                bufferSum += metricValue;
                bufferSumOfSquares += (metricValue * metricValue);
            }

            if (_restrictToUInt32Values)
            {
                bufferMax = Math.Round(bufferMax);
                bufferMin = Math.Round(bufferMin);
            }

            // Take a lock and update the aggregate:
            lock(_updateLock)
            {
                _count += bufferCount;
                _max = (bufferMax > _max) ? bufferMax : _max;
                _min = (bufferMin < _min) ? bufferMin : _min;
                _sum += bufferSum;
                _sumOfSquares += bufferSumOfSquares;

                if (_restrictToUInt32Values)
                {
                    _sum = Math.Round(_sum);
                    _sumOfSquares = Math.Round(_sumOfSquares);
                }
            }
        }
    }
}
