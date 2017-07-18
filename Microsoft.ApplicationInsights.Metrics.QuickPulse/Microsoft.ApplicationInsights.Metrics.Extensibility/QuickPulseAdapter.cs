using System;
using System.Collections.Generic;

using Microsoft.ApplicationInsights.Channel;

namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    /// <summary>
    /// Use for interfacing with QuickPulse (QP) in th eprototype phase.
    /// We will reflect into QP and pass pointers to the functions in this adapter. These functions hide any types declared in this package.
    /// So QP cwill be able to consume data without taking compile-time dependencies.
    /// </summary>
    public static class QuickPulseAdapter
    {
        // From MetricManagerExtensions:
        //public static bool StartAggregators(this MetricManager metricManager, MetricConsumerKind consumerKind, DateTimeOffset tactTimestamp, IMetricSeriesFilter filter)
        //public static AggregationPeriodSummary StopAggregators(this MetricManager metricManager, MetricConsumerKind consumerKind, DateTimeOffset tactTimestamp)
        //public static AggregationPeriodSummary CycleAggregators(this MetricManager metricManager, MetricConsumerKind consumerKind, DateTimeOffset tactTimestamp, IMetricSeriesFilter updatedFilter)

        /// <summary>
        /// 
        /// </summary>
        /// <param name="metricManager"></param>
        /// <param name="consumerKind"></param>
        /// <param name="tactTimestamp"></param>
        /// <param name="metricSeriesFilter"></param>
        /// <returns></returns>
        public static
                    bool
                    StartAggregators(object metricManager,
                                     int consumerKind,
                                     DateTimeOffset tactTimestamp,
                                     Func<Tuple<bool,
                                                Tuple<Func<object, uint, bool>,
                                                      Func<object, double, bool>,
                                                      Func<object, object, bool>>>> metricSeriesFilter)
        {
            MetricManager manager = (MetricManager) metricManager;
            MetricConsumerKind consumer = SafeConvertMetricConsumerKind(consumerKind);
            var filter = new MetricSeriesFilterAdapter(metricSeriesFilter);

            bool result = MetricManagerExtensions.StartAggregators(manager, consumer, tactTimestamp, filter);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="metricManager"></param>
        /// <param name="consumerKind"></param>
        /// <param name="tactTimestamp"></param>
        /// <returns></returns>
        public static
                    Tuple<IReadOnlyCollection<ITelemetry>, IReadOnlyCollection<ITelemetry>>
                    StopAggregators(object metricManager,
                                    int consumerKind,
                                    DateTimeOffset tactTimestamp)
        {
            MetricManager manager = (MetricManager) metricManager;
            MetricConsumerKind consumer = SafeConvertMetricConsumerKind(consumerKind);

            AggregationPeriodSummary summary = MetricManagerExtensions.StopAggregators(manager, consumer, tactTimestamp);

            var result = Tuple.Create(summary.FilteredAggregates, summary.UnfilteredValuesAggregates);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="metricManager"></param>
        /// <param name="consumerKind"></param>
        /// <param name="tactTimestamp"></param>
        /// <param name="updatedMetricSeriesFilter"></param>
        /// <returns></returns>
        public static
                    Tuple<IReadOnlyCollection<ITelemetry>, IReadOnlyCollection<ITelemetry>>
                    CycleAggregators(object metricManager,
                                     int consumerKind,
                                     DateTimeOffset tactTimestamp,
                                     Func<Tuple<bool,
                                                Tuple<Func<object, uint, bool>,
                                                      Func<object, double, bool>,
                                                      Func<object, object, bool>>>> updatedMetricSeriesFilter)
        {
            MetricManager manager = (MetricManager) metricManager;
            MetricConsumerKind consumer = SafeConvertMetricConsumerKind(consumerKind);
            var filter = new MetricSeriesFilterAdapter(updatedMetricSeriesFilter);

            AggregationPeriodSummary summary = MetricManagerExtensions.CycleAggregators(manager, consumer, tactTimestamp, filter);

            var result = Tuple.Create(summary.FilteredAggregates, summary.UnfilteredValuesAggregates);
            return result;
        }

        private static MetricConsumerKind SafeConvertMetricConsumerKind(int consumerKind)
        {
            MetricConsumerKind consumer = (MetricConsumerKind) consumerKind;

            if (consumer == MetricConsumerKind.Default
                || consumer == MetricConsumerKind.QuickPulse
                || consumer == MetricConsumerKind.Custom)
            {
                return consumer;
            }

            throw new ArgumentException($"The specified number '{consumerKind}' is not a valid value for the {nameof(MetricConsumerKind)} enumeration.");
        }

    }
}
