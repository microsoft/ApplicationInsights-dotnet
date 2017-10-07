using System;
using System.Globalization;
using System.Threading;

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    internal abstract class SimpleNumberSeriesAggregatorBase : DataSeriesAggregatorBase, IMetricSeriesAggregator
    {
        protected int _count;
        protected double _min;
        protected double _max;
        protected double _sum;
        protected double _sumOfSquares;

        public SimpleNumberSeriesAggregatorBase(IMetricSeriesConfiguration configuration, MetricSeries dataSeries, MetricAggregationCycleKind aggregationCycleKind)
            : base(configuration, dataSeries, aggregationCycleKind)
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

        protected override void ReinitializeAggregation()
        {
            _count = 0;
            _min = Double.MaxValue;
            _max = Double.MinValue;
            _sum = 0.0;
            _sumOfSquares = 0.0;
        }

        protected virtual void UpdateAggregationWithTrackedValue(double metricValue)
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
    }
}