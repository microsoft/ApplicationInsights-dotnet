using System;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary>
    /// Provides discoverable access to constants used by metric aggregates.
    /// Do not use directly. Instead, use: <c>MetricConfigurations.Common.AggregateKinds()</c>.
    /// </summary>
    public static class MetricAggregateKindsCoreExtensions
    {
        /// <summary>
        /// Groups constants used by different kinds of aggregates.
        /// A <see cref="MetricAggregate" /> is an object that encapsulates the aggregation results of metric series over a time period.
        /// </summary>
        /// <param name="metricConfigurations"></param>
        /// <returns></returns>
        public static MetricAggregateKinds AggregateKinds(this MetricConfigurations metricConfigurations)
        {
            return MetricAggregateKinds.Instance;
        }

        /// <summary>
        /// Groups constants used my metric aggregates produced by aggregators that are configured by metric configurations represented
        /// through instances of <see cref="MetricSeriesConfigurationForMeasurement"/>. See also <c>MetricConfigurations.Common.Measurement()</c>./>
        /// </summary>
        /// <param name="aggregateKinds"></param>
        /// <returns></returns>
        public static MetricAggregateKinds.Measurement Measurement(this MetricAggregateKinds aggregateKinds)
        {
            return MetricAggregateKinds.Measurement.Instance;
        }
    }
}
