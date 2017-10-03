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
        //public static AggregationPeriodSummary StopAggregators(this MetricManager metricManager, MetricAggregationCycleKind aggregationCycleKind, DateTimeOffset tactTimestamp)
        //public static AggregationPeriodSummary CycleAggregators(this MetricManager metricManager, MetricAggregationCycleKind aggregationCycleKind, DateTimeOffset tactTimestamp, IMetricSeriesFilter filter)


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
            MetricAggregationCycleKind cycle = SafeConvertMetricAggregationCycleKind(aggregationCycleKind);

            AggregationPeriodSummary summary = MetricManagerExtensions.StopAggregators(manager, cycle, tactTimestamp);

            var result = Tuple.Create(summary.NonpersistentAggregates, summary.PersistentAggregates);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="metricManager"></param>
        /// <param name="aggregationCycleKind"></param>
        /// <param name="tactTimestamp"></param>
        /// <param name="metricSeriesFilter"></param>
        /// <returns></returns>
        public static
                    Tuple<IReadOnlyList<ITelemetry>, IReadOnlyList<ITelemetry>>
                    StartOrCycleAggregators(
                                     object metricManager,
                                     int aggregationCycleKind,
                                     DateTimeOffset tactTimestamp,
                                     Func<Tuple<bool,
                                                Tuple<Func<object, double, bool>,
                                                      Func<object, object, bool>>>> metricSeriesFilter)
        {
            MetricManager manager = (MetricManager) metricManager;
            MetricAggregationCycleKind cycle = SafeConvertMetricAggregationCycleKind(aggregationCycleKind);
            var filter = new MetricSeriesFilterAdapter(metricSeriesFilter);

            AggregationPeriodSummary summary = MetricManagerExtensions.StartOrCycleAggregators(manager, cycle, tactTimestamp, filter);

            var result = Tuple.Create(summary.NonpersistentAggregates, summary.PersistentAggregates);
            return result;
        }

        private static MetricAggregationCycleKind SafeConvertMetricAggregationCycleKind(int aggregationCycleKind)
        {
            MetricAggregationCycleKind cycle = (MetricAggregationCycleKind) aggregationCycleKind;

            if (cycle == MetricAggregationCycleKind.Default
                || cycle == MetricAggregationCycleKind.QuickPulse
                || cycle == MetricAggregationCycleKind.Custom)
            {
                return cycle;
            }

            throw new ArgumentException($"The specified number '{aggregationCycleKind}' is not a valid value for the {nameof(MetricAggregationCycleKind)} enumeration.");
        }
    }
}
