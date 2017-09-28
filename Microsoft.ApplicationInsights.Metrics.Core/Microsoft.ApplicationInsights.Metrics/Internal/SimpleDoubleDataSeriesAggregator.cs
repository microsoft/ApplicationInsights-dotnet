using System;
using System.Globalization;
using System.Threading;

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    internal class SimpleDoubleDataSeriesAggregator : DataSeriesAggregatorBase, IMetricSeriesAggregator
    {
        private int _count;
        private double _min;
        private double _max;
        private double _sum;
        private double _sumOfSquares;

        public SimpleDoubleDataSeriesAggregator(IMetricSeriesConfiguration configuration, MetricSeries dataSeries, MetricConsumerKind consumerKind)
            : base(configuration, dataSeries, consumerKind)
        {
            Util.ValidateNotNull(configuration, nameof(configuration));

            SimpleMetricSeriesConfiguration simpleSeriesConfig = configuration as SimpleMetricSeriesConfiguration;
            if (simpleSeriesConfig == null)
            {
                throw new ArgumentException(
                                        $"{nameof(SimpleDoubleDataSeriesAggregator)} expects a configuration of type {nameof(SimpleMetricSeriesConfiguration)},"
                                      + $" however the specified configuration is {configuration?.GetType()?.FullName ?? Util.NullString}.",
                                        nameof(configuration));
            }

            if (true == simpleSeriesConfig.RestrictToUInt32Values)
            {
                throw new ArgumentException(
                                        $"{nameof(SimpleDoubleDataSeriesAggregator)} expects a configuration of type {nameof(SimpleMetricSeriesConfiguration)}"
                                      + $" where 'RestrictToUInt32Values' is FALSE, however it is True.",
                                        nameof(configuration));
            }
        }

        public override ITelemetry CreateAggregateUnsafe(DateTimeOffset periodEnd)
        { 
            int count = _count;
            double sum = _sum;
            double min = 0.0;
            double max = 0.0;
            double stdDev = 0.0;

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

                min = Util.EnsureConcreteValue(min);
                max = Util.EnsureConcreteValue(max);
                stdDev = Util.EnsureConcreteValue(stdDev);
                
            }

            sum = Util.EnsureConcreteValue(sum);

            MetricTelemetry aggregate = new MetricTelemetry(DataSeries?.MetricId ?? Util.NullString, count, sum, min, max, stdDev); ;

            StampTimingInfo(aggregate, periodEnd);
            StampVersionAndContextInfo(aggregate);

            return aggregate;
        }

        protected override void TrackFilteredValue(double metricValue)
        {
            if (Double.IsNaN(metricValue))
            {
                return;
            }

            {
                Interlocked.Increment(ref _count);
            }

            {
                double currentMax = _max;   // If we get a torn read, the below assignment will fail and we get to try again.
                while (metricValue > currentMax)
                {
                    double prevMax = Interlocked.CompareExchange(ref _max, metricValue, currentMax);
                    currentMax = (prevMax == currentMax) ? metricValue : prevMax;
                }
            }

            {
                double currentMin = _min;   // If we get a torn read, the below assignment will fail and we get to try again.
                while (metricValue < currentMin)
                {
                    double prevMin = Interlocked.CompareExchange(ref _min, metricValue, currentMin);
                    currentMin = (prevMin == currentMin) ? metricValue : prevMin;
                }
            }

            {
                double currentSum, prevSum;
                do
                {
                    currentSum = _sum;  // If we get a torn read, the below assignment will fail and we get to try again.
                    double newSum = currentSum + metricValue;
                    prevSum = Interlocked.CompareExchange(ref _sum, newSum, currentSum);

                    if ( (prevSum == currentSum)
                        || (Double.IsNaN(prevSum) && Double.IsNaN(currentSum))
                        || (Double.IsNegativeInfinity(prevSum) && Double.IsNegativeInfinity(currentSum))
                        || (Double.IsPositiveInfinity(prevSum) && Double.IsPositiveInfinity(currentSum)))
                    {
                        break;
                    }
                }
                while (true);
            }

            {
                double squaredValue = metricValue * metricValue;
                double currentSumOfSquares, prevSumOfSquares;
                do
                {
                    currentSumOfSquares = _sumOfSquares;  // If we get a torn read, the below assignment will fail and we get to try again.
                    double newSumOfSquares = currentSumOfSquares + squaredValue;
                    prevSumOfSquares = Interlocked.CompareExchange(ref _sumOfSquares, newSumOfSquares, currentSumOfSquares);

                    if ( (prevSumOfSquares == currentSumOfSquares)
                        || (Double.IsNaN(prevSumOfSquares) && Double.IsNaN(currentSumOfSquares))
                        || (Double.IsNegativeInfinity(prevSumOfSquares) && Double.IsNegativeInfinity(currentSumOfSquares))
                        || (Double.IsPositiveInfinity(prevSumOfSquares) && Double.IsPositiveInfinity(currentSumOfSquares)))
                    {
                        break;
                    }
                }
                while (true);
            }
        }

        protected override void TrackFilteredValue(object metricValue)
        {
            if (metricValue == null)
            {
                return;
            }

            if (metricValue is SByte)
            {
                TrackFilteredValue((double) (SByte) metricValue);
            }
            else if (metricValue is Byte)
            {
                TrackFilteredValue((double) (Byte) metricValue);
            }
            else if (metricValue is Int16)
            {
                TrackFilteredValue((double) (Int16) metricValue);
            }
            else if (metricValue is UInt16)
            {
                TrackFilteredValue((double) (UInt16) metricValue);
            }
            else if (metricValue is Int32)
            {
                TrackFilteredValue((double) (Int32) metricValue);
            }
            else if (metricValue is UInt32)
            {
                TrackFilteredValue((double) (UInt32) metricValue);
            }
            else if (metricValue is Int64)
            {
                TrackFilteredValue((double) (Int64) metricValue);
            }
            else if (metricValue is UInt64)
            {
                TrackFilteredValue((double) (UInt64) metricValue);
            }
            else if (metricValue is Single)
            {
                TrackFilteredValue((double) (Single) metricValue);
            }
            else if (metricValue is Double)
            {
                TrackFilteredValue((double) (Double) metricValue);
            }
            else
            {
                string stringValue = metricValue as string;
                if (stringValue != null)
                {
                    double doubleValue;
                    if (Double.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out doubleValue))
                    {
                        TrackFilteredValue(doubleValue);
                    }
                    else
                    {
                        throw new ArgumentException($"This aggregator cannot process the specified metric value."
                                                  + $" The aggregator expects metric values of type Double, but the specified {nameof(metricValue)} is"
                                                  + $" a String that cannot be parsed into a Double (\"{metricValue}\")."
                                                  + $" Have you specified the correct metric configuration?");
                    }
                }
                else
                {
                    throw new ArgumentException($"This aggregator cannot process the specified metric value."
                                                  + $" The aggregator expects metric values of type Double, but the specified {nameof(metricValue)} is"
                                                  + $" of type ${metricValue.GetType().FullName}."
                                                  + $" Have you specified the correct metric configuration?");
                }
            }
        }
        
        protected override void ReinitializeAggregatedValues()
        {
            _count = 0;
            _min = Double.MaxValue;
            _max = Double.MinValue;
            _sum = 0.0;
            _sumOfSquares = 0.0;
        }
    }
}