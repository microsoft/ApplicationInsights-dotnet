using System;
using System.Globalization;
using System.Threading;

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    internal class SimpleDoubleDataSeriesAggregator : SimpleNumberSeriesAggregatorBase, IMetricSeriesAggregator
    {
        public SimpleDoubleDataSeriesAggregator(IMetricSeriesConfiguration configuration, MetricSeries dataSeries, MetricAggregationCycleKind aggregationCycleKind)
            : base(configuration, dataSeries, aggregationCycleKind)
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

        
        protected override void TrackFilteredValue(double metricValue)
        {
            UpdateAggregationWithTrackedValue(metricValue);
        }

        protected override void TrackFilteredValue(object metricValue)
        {
            if (metricValue == null)
            {
                return;
            }

            if (metricValue is SByte)
            {
                UpdateAggregationWithTrackedValue((double) (SByte) metricValue);
            }
            else if (metricValue is Byte)
            {
                UpdateAggregationWithTrackedValue((double) (Byte) metricValue);
            }
            else if (metricValue is Int16)
            {
                UpdateAggregationWithTrackedValue((double) (Int16) metricValue);
            }
            else if (metricValue is UInt16)
            {
                UpdateAggregationWithTrackedValue((double) (UInt16) metricValue);
            }
            else if (metricValue is Int32)
            {
                UpdateAggregationWithTrackedValue((double) (Int32) metricValue);
            }
            else if (metricValue is UInt32)
            {
                UpdateAggregationWithTrackedValue((double) (UInt32) metricValue);
            }
            else if (metricValue is Int64)
            {
                UpdateAggregationWithTrackedValue((double) (Int64) metricValue);
            }
            else if (metricValue is UInt64)
            {
                UpdateAggregationWithTrackedValue((double) (UInt64) metricValue);
            }
            else if (metricValue is Single)
            {
                UpdateAggregationWithTrackedValue((double) (Single) metricValue);
            }
            else if (metricValue is Double)
            {
                UpdateAggregationWithTrackedValue((double) (Double) metricValue);
            }
            else
            {
                string stringValue = metricValue as string;
                if (stringValue != null)
                {
                    double doubleValue;
                    if (Double.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out doubleValue))
                    {
                        UpdateAggregationWithTrackedValue(doubleValue);
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
    }
}