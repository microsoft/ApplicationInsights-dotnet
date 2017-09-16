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
                double mean = sum / count;
                double variance = (_sumOfSquares / count) - (mean * mean);
                stdDev = Math.Sqrt(variance);
            }

            MetricTelemetry aggregate = new MetricTelemetry(DataSeries.MetricId, count, sum, min, max, stdDev);
            Util.CopyTelemetryContext(DataSeries.Context, aggregate.Context);
            aggregate.Timestamp = this.PeriodStart;

            return aggregate;
        }

        protected override void TrackFilteredValue(double metricValue)
        {
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
                }
                while (prevSum != currentSum);
            }

            {
                double currentSumOfSquares, prevSumOfSquares;
                do
                {
                    currentSumOfSquares = _sumOfSquares;  // If we get a torn read, the below assignment will fail and we get to try again.
                    double newSumOfSquares = currentSumOfSquares + (metricValue * metricValue);
                    prevSumOfSquares = Interlocked.CompareExchange(ref _sumOfSquares, newSumOfSquares, currentSumOfSquares);
                }
                while (prevSumOfSquares != currentSumOfSquares);
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
                        throw new ArgumentException($"This aggregator cannot process the specified metric measurement."
                                                  + $" The aggregator expects metric values of type Double, but the specified {nameof(metricValue)} is"
                                                  + $" a String that cannot be parsed into a Double (\"{metricValue}\")."
                                                  + $" Have you specified the correct metric configuration?");
                    }
                }
                else
                {
                    throw new ArgumentException($"This aggregator cannot process the specified metric measurement."
                                                  + $" The aggregator expects metric values of type Double, but the specified {nameof(metricValue)} is"
                                                  + $" of type ${metricValue.GetType().FullName}."
                                                  + $" Have you specified the correct metric configuration?");
                }
            }
        }
        
        public override void ReinitializeAggregatedValues()
        {
            _count = 0;
            _min = Double.MaxValue;
            _max = Double.MinValue;
            _sum = 0.0;
            _sumOfSquares = 0.0;
        }
    }
}