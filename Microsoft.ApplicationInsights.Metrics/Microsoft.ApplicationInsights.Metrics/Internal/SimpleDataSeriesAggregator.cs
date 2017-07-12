using System;
using System.Threading;

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Metrics.Extensibility;
using Microsoft.ApplicationInsights.DataContracts;

namespace Microsoft.ApplicationInsights.Metrics
{
    internal class SimpleDataSeriesAggregator : DataSeriesAggregatorBase, IMetricDataSeriesAggregator
    {
        private int _count = 0;
        private double _min = Double.MaxValue;
        private double _max = Double.MinValue;
        private double _sum = 0;
        private double _sumOfSquares = 0;

        public SimpleDataSeriesAggregator(IMetricConfiguration configuration, MetricDataSeries dataSeries, MetricConsumerKind consumerKind)
            : base(configuration, dataSeries, consumerKind)
        {
        }

        public override ITelemetry CreateAggregateUnsafe(DateTimeOffset periodEnd)
        {
            double stdDev = 0.0;
            int count = _count;
            double sum = _sum;

            if (count > 0)
            {
                double mean = sum / count;
                double variance = (_sumOfSquares / count) - (mean * mean);
                stdDev = Math.Sqrt(variance);
            }

            MetricTelemetry aggregate = new MetricTelemetry(DataSeries.MetricId, count, sum, _min, _max, stdDev);
            Util.CopyTelemetryContext(DataSeries.Context, aggregate.Context);

            return aggregate;
        }

        protected override void TrackFilteredValue(uint metricValue)
        {
            throw new NotImplementedException();
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
                while (metricValue > currentMin)
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
                    prevSumOfSquares = Interlocked.CompareExchange(ref _sum, newSumOfSquares, currentSumOfSquares);
                }
                while (prevSumOfSquares != currentSumOfSquares);
            }
        }

        protected override void TrackFilteredValue(object metricValue)
        {
            throw new NotImplementedException();
        }
    }
}