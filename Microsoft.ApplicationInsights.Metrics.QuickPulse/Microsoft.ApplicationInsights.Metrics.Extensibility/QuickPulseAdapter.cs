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
        //public static bool StartAggregators(this MetricManager metricManager, MetricAggregationCycleKind aggregationCycleKind, DateTimeOffset tactTimestamp, IMetricSeriesFilter filter)
        //public static AggregationPeriodSummary StopAggregators(this MetricManager metricManager, MetricAggregationCycleKind aggregationCycleKind, DateTimeOffset tactTimestamp)
        //public static AggregationPeriodSummary CycleAggregators(this MetricManager metricManager, MetricAggregationCycleKind aggregationCycleKind, DateTimeOffset tactTimestamp, IMetricSeriesFilter updatedFilter)

        /// <summary>
        /// 
        /// </summary>
        /// <param name="metricManager"></param>
        /// <param name="aggregationCycleKind"></param>
        /// <param name="tactTimestamp"></param>
        /// <param name="metricSeriesFilter"></param>
        /// <returns></returns>
        public static
                    bool
                    StartAggregators(
                                     object metricManager,
                                     int aggregationCycleKind,
                                     DateTimeOffset tactTimestamp,
                                     Func<Tuple<bool,
                                                Tuple<Func<object, double, bool>,
                                                      Func<object, object, bool>>>> metricSeriesFilter)
        {
            MetricManager manager = (MetricManager) metricManager;
            MetricAggregationCycleKind consumer = SafeConvertMetricAggregationCycleKind(aggregationCycleKind);
            var filter = new MetricSeriesFilterAdapter(metricSeriesFilter);

            bool result = MetricManagerExtensions.StartAggregators(manager, consumer, tactTimestamp, filter);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="metricManager"></param>
        /// <param name="aggregationCycleKind"></param>
        /// <param name="tactTimestamp"></param>
        /// <returns></returns>
        public static
                    Tuple<IReadOnlyList<ITelemetry>, IReadOnlyList<ITelemetry>>
                    StopAggregators(
                                    object metricManager,
                                    int aggregationCycleKind,
                                    DateTimeOffset tactTimestamp)
        {
            MetricManager manager = (MetricManager) metricManager;
            MetricAggregationCycleKind consumer = SafeConvertMetricAggregationCycleKind(aggregationCycleKind);

            AggregationPeriodSummary summary = MetricManagerExtensions.StopAggregators(manager, consumer, tactTimestamp);

            var result = Tuple.Create(summary.NonpersistentAggregates, summary.PersistentAggregates);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="metricManager"></param>
        /// <param name="aggregationCycleKind"></param>
        /// <param name="tactTimestamp"></param>
        /// <param name="updatedMetricSeriesFilter"></param>
        /// <returns></returns>
        public static
                    Tuple<IReadOnlyList<ITelemetry>, IReadOnlyList<ITelemetry>>
                    CycleAggregators(
                                     object metricManager,
                                     int aggregationCycleKind,
                                     DateTimeOffset tactTimestamp,
                                     Func<Tuple<bool,
                                                Tuple<Func<object, double, bool>,
                                                      Func<object, object, bool>>>> updatedMetricSeriesFilter)
        {
            MetricManager manager = (MetricManager) metricManager;
            MetricAggregationCycleKind consumer = SafeConvertMetricAggregationCycleKind(aggregationCycleKind);
            var filter = new MetricSeriesFilterAdapter(updatedMetricSeriesFilter);

            AggregationPeriodSummary summary = MetricManagerExtensions.CycleAggregators(manager, consumer, tactTimestamp, filter);

            var result = Tuple.Create(summary.NonpersistentAggregates, summary.PersistentAggregates);
            return result;
        }

        private static MetricAggregationCycleKind SafeConvertMetricAggregationCycleKind(int aggregationCycleKind)
        {
            MetricAggregationCycleKind consumer = (MetricAggregationCycleKind) aggregationCycleKind;

            if (consumer == MetricAggregationCycleKind.Default
                || consumer == MetricAggregationCycleKind.QuickPulse
                || consumer == MetricAggregationCycleKind.Custom)
            {
                return consumer;
            }

            throw new ArgumentException($"The specified number '{aggregationCycleKind}' is not a valid value for the {nameof(MetricAggregationCycleKind)} enumeration.");
        }
    }
}
