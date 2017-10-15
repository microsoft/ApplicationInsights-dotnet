using System;
using System.Globalization;
using System.Threading;

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    internal class SimpleUInt32DataSeriesAggregator : SimpleNumberSeriesAggregatorBase, IMetricSeriesAggregator
    {
        private const double MicroOne = 0.000001;

        public SimpleUInt32DataSeriesAggregator(IMetricSeriesConfiguration configuration, MetricSeries dataSeries, MetricAggregationCycleKind aggregationCycleKind)
            : base(configuration, dataSeries, aggregationCycleKind)
        {
            Util.ValidateNotNull(configuration, nameof(configuration));

            SimpleMetricSeriesConfiguration simpleSeriesConfig = configuration as SimpleMetricSeriesConfiguration;
            if (simpleSeriesConfig == null)
            {
                throw new ArgumentException(
                                        $"{nameof(SimpleUInt32DataSeriesAggregator)} expects a configuration of type {nameof(SimpleMetricSeriesConfiguration)},"
                                      + $" however the specified configuration is {configuration?.GetType()?.FullName ?? Util.NullString}.",
                                        nameof(configuration));
            }

            if (false == simpleSeriesConfig.RestrictToUInt32Values)
            {
                throw new ArgumentException(
                                        $"{nameof(SimpleUInt32DataSeriesAggregator)} expects a configuration of type {nameof(SimpleMetricSeriesConfiguration)}"
                                      + $" where 'RestrictToUInt32Values' is TRUE, however it is False.",
                                        nameof(configuration));
            }
        }

        public override ITelemetry CreateAggregateUnsafe(DateTimeOffset periodEnd)
        {
            SnapAggragetionToIntValues();
            return base.CreateAggregateUnsafe(periodEnd);
        }

        protected override void UpdateAggregationWithTrackedValue(double metricValue)
        {
            base.UpdateAggregationWithTrackedValue(metricValue);
            SnapAggragetionToIntValues();
        }

        protected override void TrackFilteredValue(double metricValue)
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

            UpdateAggregationWithTrackedValue(wholeValue);
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
                    throw new ArgumentException($"This aggregator cannot process the specified metric value."
                                              + $" The aggregator expects metric values of type {nameof(UInt32)}, but the specified {nameof(metricValue)} is"
                                              + $" a negative value of type {metricValue.GetType().FullName} ({metricValue})."
                                              + $" Have you specified the correct metric configuration?");
                }
                else
                {
                    UpdateAggregationWithTrackedValue((double) v);
                }
            }
            else if (metricValue is Byte)
            {
                UpdateAggregationWithTrackedValue((double) (Byte) metricValue);
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
                    UpdateAggregationWithTrackedValue((double) v);
                }
            }
            else if (metricValue is UInt16)
            {
                UpdateAggregationWithTrackedValue((double) (UInt16) metricValue);
            }
            else if (metricValue is Int32)
            {
                var v = (Int32) metricValue;
                if (v < 0)
                {
                    throw new ArgumentException($"This aggregator cannot process the specified metric value."
                                              + $" The aggregator expects metric values of type {nameof(UInt32)}, but the specified {nameof(metricValue)} is"
                                              + $" a negative value of type {metricValue.GetType().FullName} ({metricValue})."
                                              + $" Have you specified the correct metric configuration?");
                }
                else
                {
                    UpdateAggregationWithTrackedValue((double) v);
                }
            }
            else if (metricValue is UInt32)
            {
                UpdateAggregationWithTrackedValue((double) (UInt32) metricValue);
            }
            else if (metricValue is Int64)
            {
                var v = (Int64) metricValue;
                if (v < 0)
                {
                    throw new ArgumentException($"This aggregator cannot process the specified metric value."
                                              + $" The aggregator expects metric values of type {nameof(UInt32)}, but the specified {nameof(metricValue)} is"
                                              + $" a negative value of type {metricValue.GetType().FullName} ({metricValue})."
                                              + $" Have you specified the correct metric configuration?");
                }
                else if (v > UInt32.MaxValue)
                {
                    throw new ArgumentException($"This aggregator cannot process the specified metric value."
                                             + $" The aggregator expects metric values of type {nameof(UInt32)}, but the specified {nameof(metricValue)} is"
                                             + $" a value of type {metricValue.GetType().FullName} that larger than the maximum accepted value ({metricValue})."
                                             + $" Have you specified the correct metric configuration?");
                }
                else
                {
                    UpdateAggregationWithTrackedValue((double) v);
                }
            }
            else if (metricValue is UInt64)
            {
                var v = (UInt64) metricValue;
                if (v > UInt32.MaxValue)
                {
                    throw new ArgumentException($"This aggregator cannot process the specified metric value."
                                             + $" The aggregator expects metric values of type {nameof(UInt32)}, but the specified {nameof(metricValue)} is"
                                             + $" a value of type {metricValue.GetType().FullName} that larger than the maximum accepted value ({metricValue})."
                                             + $" Have you specified the correct metric configuration?");
                }
                else
                {
                    UpdateAggregationWithTrackedValue((double) v);
                }
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
                    uint uintValue;
                    if (UInt32.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out uintValue))
                    {
                        TrackFilteredValue(uintValue);
                    }
                    else
                    {
                        throw new ArgumentException($"This aggregator cannot process the specified metric value."
                                                  + $" The aggregator expects metric values of type {nameof(UInt32)}, but the specified {nameof(metricValue)} is"
                                                  + $" a String that cannot be parsed into a {nameof(UInt32)} (\"{metricValue}\")."
                                                  + $" Have you specified the correct metric configuration?");
                    }
                }
                else
                {
                    throw new ArgumentException($"This aggregator cannot process the specified metric value."
                                                  + $" The aggregator expects metric values of type {nameof(UInt32)}, but the specified {nameof(metricValue)} is"
                                                  + $" of type ${metricValue.GetType().FullName}."
                                                  + $" Have you specified the correct metric configuration?");
                }
            }
        }

        private void SnapAggragetionToIntValues()
        {
            double currentMin = _min;
            if (currentMin != Math.Round(currentMin))
            {
                Interlocked.CompareExchange(ref _min, Math.Round(currentMin), currentMin);
            }

            double currentMax = _max;
            if (currentMax != Math.Round(currentMax))
            {
                Interlocked.CompareExchange(ref _max, Math.Round(currentMax), currentMax);
            }

            double currentSum = _sum;
            if (currentSum != Math.Round(currentSum))
            {
                Interlocked.CompareExchange(ref _sum, Math.Round(currentSum), currentSum);
            }

            double currentSumOfSquares = _sumOfSquares;
            if (currentSumOfSquares != Math.Round(currentSumOfSquares))
            {
                Interlocked.CompareExchange(ref _sumOfSquares, Math.Round(currentSumOfSquares), currentSumOfSquares);
            }
        }
    }
}