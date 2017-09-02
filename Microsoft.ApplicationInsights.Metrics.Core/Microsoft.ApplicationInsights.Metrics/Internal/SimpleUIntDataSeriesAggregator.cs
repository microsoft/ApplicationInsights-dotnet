using System;
using System.Globalization;
using System.Threading;

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    internal class SimpleUIntDataSeriesAggregator : DataSeriesAggregatorBase, IMetricSeriesAggregator
    {
        private const double MicroOne = 0.000001;

        private int _count;
        private long _min;
        private long _max;
        private long _sum;                  // Int64 in order to use with Interlocked, but always use casts to get UInt64 semantics.
        private long _sumOfSquares;         // Int64 in order to use with Interlocked, but always use casts to get UInt64 semantics.

        public SimpleUIntDataSeriesAggregator(IMetricSeriesConfiguration configuration, MetricSeries dataSeries, MetricConsumerKind consumerKind)
            : base(configuration, dataSeries, consumerKind)
        {
            Reset();
        }

        public override ITelemetry CreateAggregateUnsafe(DateTimeOffset periodEnd)
        {
            int count = _count;
            ulong sum = (ulong) _sum;
            long min = 0;
            long max = 0;
            double stdDev = 0.0;

            if (count > 0)
            {
                min = _min;
                max = _max;
                double mean = ((double) sum) / (double) count;
                double variance = (((double) _sumOfSquares) / (double) count) - (mean * mean);
                stdDev = Math.Sqrt(variance);
            }

            MetricTelemetry aggregate = new MetricTelemetry(DataSeries.MetricId, count, sum, _min, _max, stdDev);
            Util.CopyTelemetryContext(DataSeries.Context, aggregate.Context);

            return aggregate;
        }
        
        protected override bool RecycleUnsafe()
        {
            Reset();
            return true;
        }

        protected override void TrackFilteredValue(uint metricValue)
        {
            {
                Interlocked.Increment(ref _count);
            }

            {
                long currentMax = _max;   // If we get a torn read, the below assignment will fail and we get to try again.
                while (metricValue > currentMax)
                {
                    long prevMax = Interlocked.CompareExchange(ref _max, metricValue, currentMax);
                    currentMax = (prevMax == currentMax) ? metricValue : prevMax;
                }
            }

            {
                long currentMin = _min;   // If we get a torn read, the below assignment will fail and we get to try again.
                while (metricValue > currentMin)
                {
                    long prevMin = Interlocked.CompareExchange(ref _min, metricValue, currentMin);
                    currentMin = (prevMin == currentMin) ? metricValue : prevMin;
                }
            }

            {
                long currentSum, prevSum;
                do
                {
                    currentSum =  _sum;  // If we get a torn read, the below assignment will fail and we get to try again.
                    ulong newSum = ((ulong) currentSum) + metricValue;
                    prevSum = Interlocked.CompareExchange(ref _sum, (long) newSum, currentSum);
                }
                while (prevSum != currentSum);
            }

            {
                long currentSumOfSquares, prevSumOfSquares;
                do
                {
                    currentSumOfSquares = _sumOfSquares;  // If we get a torn read, the below assignment will fail and we get to try again.
                    ulong newSumOfSquares = ((ulong) currentSumOfSquares) + (metricValue * metricValue);
                    prevSumOfSquares = Interlocked.CompareExchange(ref _sum, (long) newSumOfSquares, currentSumOfSquares);
                }
                while (prevSumOfSquares != currentSumOfSquares);
            }
        }

        protected override void TrackFilteredValue(double metricValue)
        {
            if (metricValue < -MicroOne)
            {
                throw new ArgumentException($"This aggregator cannot process the specified metric measurement."
                                          + $" The aggregator expects metric values of type {nameof(UInt32)}, but the specified {nameof(metricValue)} is"
                                          + $" a negative double value ({metricValue})."
                                          + $" Have you specified the correct metric configuration?");
            }

            double wholeValue = Math.Round(metricValue);
            double delta = Math.Abs(metricValue - wholeValue);
            if (delta > MicroOne)
            {
                throw new ArgumentException($"This aggregator cannot process the specified metric measurement."
                                          + $" The aggregator expects metric values of type {nameof(UInt32)}, but the specified {nameof(metricValue)} is"
                                          + $" a double value that does not equal to a whole number ({metricValue})."
                                          + $" Have you specified the correct metric configuration?");
            }

            TrackValue((uint) wholeValue);
        }

        protected override void TrackFilteredValue(object metricValue)
        {
            if (metricValue == null)
            {
                return;
            }

            if (metricValue is SByte)
            {
                var v = (SByte) metricValue;
                if (v < 0)
                {
                    throw new ArgumentException($"This aggregator cannot process the specified metric measurement."
                                              + $" The aggregator expects metric values of type {nameof(UInt32)}, but the specified {nameof(metricValue)} is"
                                              + $" a negative value of type {metricValue.GetType().FullName} ({metricValue})."
                                              + $" Have you specified the correct metric configuration?");
                }
                else
                {
                    TrackValue((uint) v);
                }
            }
            else if (metricValue is Byte)
            {
                TrackValue((uint) (Byte) metricValue);
            }
            else if (metricValue is Int16)
            {
                var v = (Int16) metricValue;
                if (v < 0)
                {
                    throw new ArgumentException($"This aggregator cannot process the specified metric measurement."
                                              + $" The aggregator expects metric values of type {nameof(UInt32)}, but the specified {nameof(metricValue)} is"
                                              + $" a negative value of type {metricValue.GetType().FullName} ({metricValue})."
                                              + $" Have you specified the correct metric configuration?");
                }
                else
                {
                    TrackValue((uint) v);
                }
            }
            else if (metricValue is UInt16)
            {
                TrackValue((uint) (UInt16) metricValue);
            }
            else if (metricValue is Int32)
            {
                var v = (Int32) metricValue;
                if (v < 0)
                {
                    throw new ArgumentException($"This aggregator cannot process the specified metric measurement."
                                              + $" The aggregator expects metric values of type {nameof(UInt32)}, but the specified {nameof(metricValue)} is"
                                              + $" a negative value of type {metricValue.GetType().FullName} ({metricValue})."
                                              + $" Have you specified the correct metric configuration?");
                }
                else
                {
                    TrackValue((uint) v);
                }
            }
            else if (metricValue is UInt32)
            {
                TrackValue((uint) (UInt32) metricValue);
            }
            else if (metricValue is Int64)
            {
                var v = (Int64) metricValue;
                if (v < 0)
                {
                    throw new ArgumentException($"This aggregator cannot process the specified metric measurement."
                                              + $" The aggregator expects metric values of type {nameof(UInt32)}, but the specified {nameof(metricValue)} is"
                                              + $" a negative value of type {metricValue.GetType().FullName} ({metricValue})."
                                              + $" Have you specified the correct metric configuration?");
                }
                else if (v > UInt32.MaxValue)
                {
                    throw new ArgumentException($"This aggregator cannot process the specified metric measurement."
                                             + $" The aggregator expects metric values of type {nameof(UInt32)}, but the specified {nameof(metricValue)} is"
                                             + $" a value of type {metricValue.GetType().FullName} that larger than the maximum accepted value ({metricValue})."
                                             + $" Have you specified the correct metric configuration?");
                }
                else
                {
                    TrackValue((uint) v);
                }
            }
            else if (metricValue is UInt64)
            {
                var v = (UInt64) metricValue;
                if (v > UInt32.MaxValue)
                {
                    throw new ArgumentException($"This aggregator cannot process the specified metric measurement."
                                             + $" The aggregator expects metric values of type {nameof(UInt32)}, but the specified {nameof(metricValue)} is"
                                             + $" a value of type {metricValue.GetType().FullName} that larger than the maximum accepted value ({metricValue})."
                                             + $" Have you specified the correct metric configuration?");
                }
                else
                {
                    TrackValue((uint) v);
                }
            }
            else if (metricValue is Single)
            {
                TrackValue((double) (Single) metricValue);
            }
            else if (metricValue is Double)
            {
                TrackValue((double) (Double) metricValue);
            }
            else
            {
                string stringValue = metricValue as string;
                if (stringValue != null)
                {
                    uint uintValue;
                    if (UInt32.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out uintValue))
                    {
                        TrackValue(uintValue);
                    }
                    else
                    {
                        throw new ArgumentException($"This aggregator cannot process the specified metric measurement."
                                                  + $" The aggregator expects metric values of type {nameof(UInt32)}, but the specified {nameof(metricValue)} is"
                                                  + $" a String that cannot be parsed into a {nameof(UInt32)} (\"{metricValue}\")."
                                                  + $" Have you specified the correct metric configuration?");
                    }
                }
                else
                {
                    throw new ArgumentException($"This aggregator cannot process the specified metric measurement."
                                                  + $" The aggregator expects metric values of type {nameof(UInt32)}, but the specified {nameof(metricValue)} is"
                                                  + $" of type ${metricValue.GetType().FullName}."
                                                  + $" Have you specified the correct metric configuration?");
                }
            }
        }

        private void Reset()
        {
            _count = 0;
            _min = UInt32.MaxValue;
            _max = UInt32.MinValue;
            _sum = 0;
            _sumOfSquares = 0;
        }
    }
}